using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using RAWSimO.Core.Configurations;
using RAWSimO.Core.IO;

namespace RAWSimO.Visualization.Avalonia.Controls
{
    /// <summary>
    /// Generates editors for configuration objects via reflection (the Avalonia port of the WPF visualization's VisualParameterInputHelper).
    /// All public instance fields are editable: int, double, string, bool, enums, lists (of simple values or simple structs)
    /// and nested (possibly polymorphic) configuration objects. Values are written to the object immediately; invalid input is marked
    /// and keeps the previous value. Field documentation from the XML doc files is shown as tooltips.
    /// </summary>
    public static class ConfigEditor
    {
        /// <summary>
        /// Relative widths of the label and editor columns.
        /// </summary>
        private const string Columns = "4*,5*";
        private static readonly IBrush LabelBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));

        /// <summary>
        /// Creates an editor for all fields of the given object.
        /// </summary>
        /// <param name="target">The object to edit (in place).</param>
        /// <param name="changed">Invoked whenever a value was changed successfully.</param>
        public static Control Create(object target, Action changed = null)
        {
            var panel = new StackPanel { Spacing = 2 };
            foreach (FieldInfo field in target.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                Control editor = CreateFieldEditor(target, field, changed ?? (() => { }));
                if (editor != null)
                    panel.Children.Add(editor);
            }
            return panel;
        }

        #region Field editors

        private static Control CreateFieldEditor(object target, FieldInfo field, Action changed)
        {
            // Live fields are runtime only (e.g. callbacks) - not editable
            if (field.GetCustomAttributes(false).Any(a => a is LiveAttribute))
                return null;
            string help = GetHelp(field);
            Type type = field.FieldType;

            if (field.IsInitOnly)
                return Row(field.Name, help, new TextBlock { Text = Format(field.GetValue(target)) + " (read-only)", Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center });

            if (IsSimple(type))
                return Row(field.Name, help, CreateValueEditor(type, field.GetValue(target), value => { field.SetValue(target, value); changed(); }));

            if (typeof(IList).IsAssignableFrom(type))
            {
                // Lists are instantiated, if missing (same as the WPF version)
                if (field.GetValue(target) == null)
                    field.SetValue(target, CreateInstance(type));
                return CreateListEditor(field.Name, help, type, (IList)field.GetValue(target), changed);
            }

            return CreateObjectEditor(target, field, help, changed);
        }

        private static bool IsSimple(Type type) =>
            type == typeof(int) || type == typeof(double) || type == typeof(string) || type == typeof(bool) || type.IsEnum;

        /// <summary>
        /// Creates an editor for a single simple value.
        /// </summary>
        private static Control CreateValueEditor(Type type, object value, Action<object> setValue)
        {
            if (type == typeof(bool))
            {
                var check = new CheckBox { IsChecked = (bool)value };
                check.IsCheckedChanged += (_, _) => setValue(check.IsChecked == true);
                return check;
            }
            if (type.IsEnum)
            {
                var combo = new ComboBox { ItemsSource = Enum.GetValues(type), SelectedItem = value, MinWidth = 90 };
                combo.SelectionChanged += (_, _) => { if (combo.SelectedItem != null) setValue(combo.SelectedItem); };
                return combo;
            }
            var box = new TextBox { Text = Format(value), MinWidth = 90 };
            // Listen to the property itself (synchronous for user input and programmatic changes alike)
            box.PropertyChanged += (_, e) =>
            {
                if (e.Property != TextBox.TextProperty)
                    return;
                if (TryParse(type, box.Text, out object parsed))
                {
                    DataValidationErrors.ClearErrors(box);
                    setValue(parsed);
                }
                else
                {
                    DataValidationErrors.SetError(box, new FormatException(type == typeof(int) ? "Expected an integer" : "Expected a number (e.g. 1.5)"));
                }
            };
            return box;
        }

        private static bool TryParse(Type type, string text, out object value)
        {
            if (type == typeof(string)) { value = text ?? ""; return true; }
            text = text?.Trim();
            // Empty input is treated as 0 (same as the WPF version)
            if (type == typeof(int))
            {
                int i = 0;
                bool ok = string.IsNullOrEmpty(text) || int.TryParse(text, NumberStyles.Integer, IOConstants.FORMATTER, out i);
                value = i;
                return ok;
            }
            if (type == typeof(double))
            {
                double d = 0;
                bool ok = string.IsNullOrEmpty(text) || double.TryParse(text, NumberStyles.Float, IOConstants.FORMATTER, out d);
                value = d;
                return ok;
            }
            throw new ArgumentException("Unsupported type: " + type.FullName);
        }

        private static string Format(object value) => value switch
        {
            null => "",
            double d => d.ToString(IOConstants.FORMATTER),
            IFormattable f => f.ToString(null, IOConstants.FORMATTER),
            _ => value.ToString(),
        };

        private static Control Row(string label, string help, Control editor)
        {
            var row = new Grid { ColumnDefinitions = ColumnDefinitions.Parse(Columns), Margin = new Thickness(0, 1) };
            var text = new TextBlock
            {
                Text = label,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = LabelBrush,
            };
            row.Children.Add(text);
            editor.HorizontalAlignment = editor is CheckBox ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;
            Grid.SetColumn(editor, 1);
            row.Children.Add(editor);
            ToolTip.SetTip(row, string.IsNullOrWhiteSpace(help) ? label : label + "\n\n" + help);
            return row;
        }

        #endregion

        #region Lists

        private static Control CreateListEditor(string name, string help, Type listType, IList list, Action changed)
        {
            Type elementType = listType.IsArray ? listType.GetElementType() : listType.GetGenericArguments().Single();
            var items = new StackPanel { Spacing = 2 };
            var expander = new Expander { HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 2) };
            ToolTip.SetTip(expander, string.IsNullOrWhiteSpace(help) ? name : name + "\n\n" + help);

            void UpdateHeader() => expander.Header = $"{name} ({list.Count})";

            void Rebuild()
            {
                items.Children.Clear();
                for (int i = 0; i < list.Count; i++)
                {
                    int index = i;
                    var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
                    if (IsSimple(elementType))
                    {
                        row.Children.Add(CreateValueEditor(elementType, list[index], v => { list[index] = v; changed(); }));
                    }
                    else
                    {
                        // Simple struct / class with simple fields (e.g. Skvp<double, bool>)
                        foreach (FieldInfo f in elementType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                        {
                            if (!IsSimple(f.FieldType))
                                throw new ArgumentException($"Unsupported list element field type: {elementType.FullName}.{f.Name}");
                            var fieldEditor = CreateValueEditor(f.FieldType, f.GetValue(list[index]), v =>
                            {
                                // Structs are copies: modify and write back
                                object element = list[index];
                                f.SetValue(element, v);
                                list[index] = element;
                                changed();
                            });
                            fieldEditor.MinWidth = 90;
                            ToolTip.SetTip(fieldEditor, f.Name);
                            row.Children.Add(fieldEditor);
                        }
                    }
                    var remove = new Button { Content = "✕", Padding = new Thickness(6, 2) };
                    ToolTip.SetTip(remove, "Remove this element");
                    remove.Click += (_, _) => { list.RemoveAt(index); changed(); Rebuild(); };
                    row.Children.Add(remove);
                    items.Children.Add(row);
                }
                var add = new Button { Content = "Add", HorizontalAlignment = HorizontalAlignment.Left };
                add.Click += (_, _) =>
                {
                    list.Add(elementType == typeof(string) ? "" : CreateInstance(elementType));
                    changed();
                    Rebuild();
                };
                items.Children.Add(add);
                UpdateHeader();
            }

            Rebuild();
            expander.Content = items;
            return expander;
        }

        #endregion

        #region Nested objects

        private static Control CreateObjectEditor(object target, FieldInfo field, string help, Action changed)
        {
            Type type = field.FieldType;
            List<Type> implementations = GetImplementations(type);
            var content = new ContentControl { Margin = new Thickness(12, 4, 0, 4) };
            var choices = new[] { new Choice(null) }.Concat(implementations.Select(t => new Choice(t))).ToList();
            var selector = new ComboBox { ItemsSource = choices, HorizontalAlignment = HorizontalAlignment.Stretch };
            ToolTip.SetTip(selector, "Select the implementation to use (or none)");

            var header = new Grid { ColumnDefinitions = ColumnDefinitions.Parse(Columns), HorizontalAlignment = HorizontalAlignment.Stretch };
            var label = new TextBlock { Text = field.Name, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            header.Children.Add(label);
            Grid.SetColumn(selector, 1);
            header.Children.Add(selector);
            var expander = new Expander { Header = header, Content = content, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 2) };
            ToolTip.SetTip(label, string.IsNullOrWhiteSpace(help) ? field.Name : field.Name + "\n\n" + help);

            void ShowValue()
            {
                object value = field.GetValue(target);
                content.Content = value != null ? Create(value, changed) : new TextBlock { Text = "Not set", Foreground = Brushes.Gray };
            }

            object current = field.GetValue(target);
            selector.SelectedItem = choices.FirstOrDefault(c => c.Type == current?.GetType()) ?? choices[0];
            ShowValue();
            selector.SelectionChanged += (_, _) =>
            {
                if (selector.SelectedItem is not Choice selected || selected.Type == field.GetValue(target)?.GetType())
                    return;
                field.SetValue(target, selected.Type == null ? null : CreateInstance(selected.Type));
                changed();
                ShowValue();
                expander.IsExpanded = selected.Type != null;
            };
            return expander;
        }

        /// <summary>
        /// An entry of the implementation selector (null type = not set).
        /// </summary>
        private sealed class Choice
        {
            public Choice(Type type) { Type = type; }
            public Type Type { get; }
            public override string ToString() => Type == null ? "(none)" : GetDisplayName(Type);
        }

        /// <summary>
        /// Returns a short name for a configuration type (without the redundant "Configuration" suffix).
        /// </summary>
        public static string GetDisplayName(Type type)
        {
            string name = type.Name;
            foreach (string suffix in new[] { "Configuration", "Config" })
                if (name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal))
                    return name.Substring(0, name.Length - suffix.Length);
            return name;
        }

        private static readonly ConcurrentDictionary<Type, List<Type>> _implementations = new();

        /// <summary>
        /// Returns all non-abstract types assignable to the given type (from all loaded assemblies).
        /// </summary>
        private static List<Type> GetImplementations(Type type) => _implementations.GetOrAdd(type, t =>
            AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => { try { return a.GetTypes(); } catch (ReflectionTypeLoadException e) { return e.Types.Where(x => x != null); } })
                .Where(x => t.IsAssignableFrom(x) && !x.IsAbstract && !x.IsInterface && !x.ContainsGenericParameters)
                .OrderBy(x => x.Name)
                .ToList());

        /// <summary>
        /// Creates a default instance of the given type. Prefers the special "default" constructor used by RAWSim-O configurations.
        /// </summary>
        private static object CreateInstance(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            ConstructorInfo special = type.GetConstructor(flags, null, new[] { typeof(DefaultConstructorIdentificationClass) }, null);
            if (special != null)
                return special.Invoke(new object[] { new DefaultConstructorIdentificationClass() });
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            ConstructorInfo parameterless = type.GetConstructor(flags, null, Type.EmptyTypes, null);
            if (parameterless != null)
                return parameterless.Invoke(null);
            throw new ArgumentException("Cannot find a suitable constructor for the type: " + type.FullName);
        }

        #endregion

        #region Documentation

        private static readonly ConcurrentDictionary<Assembly, XmlDocument> _docs = new();

        /// <summary>
        /// Gets the documentation of a field from the XML documentation of its assembly (or a <see cref="DescriptionAttribute"/>).
        /// </summary>
        private static string GetHelp(FieldInfo field)
        {
            if (field.GetCustomAttribute<DescriptionAttribute>() is DescriptionAttribute description)
                return description.Description;
            XmlDocument doc = _docs.GetOrAdd(field.Module.Assembly, assembly =>
            {
                try
                {
                    string path = Path.ChangeExtension(assembly.Location, ".xml");
                    if (!File.Exists(path)) return null;
                    var xml = new XmlDocument();
                    xml.Load(path);
                    return xml;
                }
                catch (Exception) { return null; }
            });
            if (doc == null)
                return null;
            Type declaring = field.DeclaringType.IsGenericType ? field.DeclaringType.GetGenericTypeDefinition() : field.DeclaringType;
            string name = ("F:" + declaring.FullName + "." + field.Name).Replace('+', '.');
            XmlNode node = doc.SelectSingleNode($"//member[@name='{name}']/summary");
            return node == null ? null : Regex.Replace(node.InnerText, @"\s+", " ").Trim();
        }

        #endregion
    }
}
