using RAWSimO.Core.Configurations;
using RAWSimO.Core.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace RAWSimO.Visualization
{
    /// <summary>
    /// This class enables the flexible generation of GUI elements and also supplies parse operations on them. Note: This class is doing some bad voodoo.
    /// </summary>
    public class VisualParameterInputHelper
    {
        private class DefaultGeneratorCommand : ICommand
        {
            public DefaultGeneratorCommand(Dispatcher uiDispatcher, TreeViewItem root, object rootObject, FieldInfo fieldInfo)
            {
                _dispatcher = uiDispatcher;
                _root = root;
                _rootObject = rootObject;
                _fieldInfo = fieldInfo;
            }

            private Dispatcher _dispatcher;
            private TreeViewItem _root;
            private object _rootObject;
            private FieldInfo _fieldInfo;
            private Button _clearButton;
            private Dictionary<Button, Func<object>> _ctors;
            private Button _clickedButton;
            internal const string BUTTON_CAPTION_INIT = "Init";
            internal const string BUTTON_CAPTION_CLEAR = "Clear";

#pragma warning disable 0067
            public event EventHandler CanExecuteChanged;
#pragma warning restore 0067

            public bool CanExecute(object parameter) { return true; }

            public void Init()
            {
                _clearButton = new Button() { Content = BUTTON_CAPTION_CLEAR, Command = this };
                _ctors =
                    AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
                    .Where(a => _fieldInfo.FieldType.IsAssignableFrom(a) && !a.IsAbstract)
                    .ToDictionary(k => new Button() { Content = BUTTON_CAPTION_INIT + " " + k.Name }, v => GetContructorFunction(v));
                foreach (var button in _ctors.Keys)
                {
                    button.Click += ButtonClicked;
                    button.Command = this;
                }
                // Execute for the first time
                var currentValue = _rootObject.GetType().GetField(_fieldInfo.Name).GetValue(_rootObject);
                if (currentValue != null)
                    FillParameterItems(_dispatcher, _root.Items, currentValue);
                AddButtons(currentValue != null);
            }

            private void ButtonClicked(object sender, RoutedEventArgs e) { _clickedButton = sender as Button; }

            public void AddButtons(bool hasValue)
            {
                _dispatcher.Invoke(() =>
                {
                    if (!hasValue)
                        // Add init buttons
                        foreach (var button in _ctors.Keys)
                            _root.Items.Add(button);
                    else
                        // Add clear button
                        _root.Items.Add(_clearButton);
                });
            }

            public void Execute(object parameter)
            {
                object currentValue = _rootObject.GetType().GetField(_fieldInfo.Name).GetValue(_rootObject);
                if (currentValue == null)
                {
                    // Field is currently null - add default content
                    var constructor = _ctors[_clickedButton];
                    object defaultObject = constructor();
                    _rootObject.GetType().GetField(_fieldInfo.Name).SetValue(_rootObject, defaultObject);
                    FillParameterItems(_dispatcher, _root.Items, defaultObject);
                    AddButtons(true);
                }
                else
                {
                    // Field has values - clear it
                    _rootObject.GetType().GetField(_fieldInfo.Name).SetValue(_rootObject, null);
                    _dispatcher.Invoke(() =>
                    {
                        _root.Items.Clear();
                    });
                    AddButtons(false);
                }
            }
        }

        private class ListHandlerCommand : ICommand
        {
            public ListHandlerCommand(Dispatcher uiDispatcher, TreeViewItem root, Type listType, IList listObject)
            {
                _dispatcher = uiDispatcher;
                _root = root;
                _listType = listType;
                _listObject = listObject;
            }

            private Dispatcher _dispatcher;
            private TreeViewItem _root;
            private IList _listObject;
            private Type _listType;
            private Button _addButton;
            private Button _removeButton;
            private Button _clickedButton;
            internal const string BUTTON_CAPTION_ADD = "Add";
            internal const string BUTTON_CAPTION_REMOVE = "Remove";

#pragma warning disable 0067
            public event EventHandler CanExecuteChanged;
#pragma warning restore 0067

            public bool CanExecute(object parameter) { return true; }

            public void Init()
            {
                _dispatcher.Invoke(() =>
                {
                    // Add buttons
                    StackPanel buttonPanel = new StackPanel() { Orientation = Orientation.Horizontal };
                    _addButton = new Button() { Content = BUTTON_CAPTION_ADD, Command = this };
                    _addButton.Click += ButtonClicked;
                    buttonPanel.Children.Add(_addButton);
                    _removeButton = new Button() { Content = BUTTON_CAPTION_REMOVE, Command = this };
                    _removeButton.Click += ButtonClicked;
                    buttonPanel.Children.Add(_removeButton);
                    _root.Items.Add(buttonPanel);
                    // Init list
                    for (int i = 0; i < _listObject.Count; i++)
                    {
                        // Simply add another content holder (take the two buttons into consideration)
                        _root.Items.Insert(_root.Items.Count - 1, GenerateContentHolder(i));
                    }
                });
            }

            private WrapPanel GenerateContentHolder(int index)
            {
                WrapPanel contentHolder = new WrapPanel();
                // Use simple types, if possible (fill in the values if an index is given)
                if (_listType == typeof(List<int>) ||
                    _listType == typeof(List<double>) ||
                    _listType == typeof(List<bool>) ||
                    _listType == typeof(List<string>) ||
                    _listType.GetGenericArguments().Length == 1 && _listType.GetGenericArguments()[0].IsEnum)
                {
                    if (_listType == typeof(List<int>)) contentHolder.Children.Add(new TextBox() { Text = index >= 0 ? _listObject[index].ToString() : "0" });
                    if (_listType == typeof(List<double>)) contentHolder.Children.Add(new TextBox() { Text = index >= 0 ? ((double)_listObject[index]).ToString(IOConstants.FORMATTER) : "0" });
                    if (_listType == typeof(List<bool>)) contentHolder.Children.Add(new CheckBox() { IsChecked = index >= 0 ? ((bool)_listObject[index]) : false });
                    if (_listType == typeof(List<string>)) contentHolder.Children.Add(new TextBox() { Text = index >= 0 ? ((string)_listObject[index]) : "" });
                    if (_listType.GetGenericArguments().Length == 1 && _listType.GetGenericArguments()[0].IsEnum)
                    {
                        ComboBox enumerationContent = new ComboBox();
                        foreach (var enumName in _listType.GetGenericArguments()[0].GetEnumNames())
                            enumerationContent.Items.Add(enumName);
                        enumerationContent.SelectedIndex = index >= 0 ? _listType.GetGenericArguments()[0].GetEnumNames().TakeWhile(n => !_listObject[index].ToString().Equals(n)).Count() : 0;
                        contentHolder.Children.Add(enumerationContent);
                    }
                    return contentHolder;
                }
                else
                {
                    // No simple types -> handling structs
                    var outerType = _listType.GetGenericArguments()[0]; // We are expecting lists here - they can only have one generic type argument
                    var innerFields = outerType.GetFields(); // Iterate all fields of the more complex element, but only expect simple types
                    for (int i = 0; i < innerFields.Length; i++)
                    {
                        if (innerFields[i].FieldType == typeof(int))
                            contentHolder.Children.Add(new TextBox() { Text = index >= 0 ? innerFields[i].GetValue(_listObject[index]).ToString() : "0" });
                        if (innerFields[i].FieldType == typeof(double))
                            contentHolder.Children.Add(new TextBox() { Text = index >= 0 ? ((double)innerFields[i].GetValue(_listObject[index])).ToString(IOConstants.FORMATTER) : "0" });
                        if (innerFields[i].FieldType == typeof(bool))
                            contentHolder.Children.Add(new CheckBox() { IsChecked = index >= 0 ? ((bool)innerFields[i].GetValue(_listObject[index])) : false });
                        if (innerFields[i].FieldType == typeof(string))
                            contentHolder.Children.Add(new TextBox() { Text = index >= 0 ? ((string)innerFields[i].GetValue(_listObject[index])) : "" });
                        if (innerFields[i].FieldType.IsEnum)
                        {
                            ComboBox enumerationContent = new ComboBox();
                            foreach (var enumName in innerFields[i].FieldType.GetEnumNames())
                                enumerationContent.Items.Add(enumName);
                            enumerationContent.SelectedIndex =
                                index >= 0 ?
                                innerFields[i].FieldType.GetEnumNames().TakeWhile(n => !innerFields[i].GetValue(_listObject[index]).ToString().Equals(n)).Count() :
                                0;
                            contentHolder.Children.Add(enumerationContent);
                        }
                    }
                }
                // Check for success
                if (contentHolder.Children.Count == 0)
                    throw new ArgumentException("Unknown list type: " + _listType.FullName);
                // Return the element
                return contentHolder;
            }

            private void ButtonClicked(object sender, RoutedEventArgs e) { _clickedButton = sender as Button; }

            public void Execute(object parameter)
            {
                if (_clickedButton == _removeButton)
                {
                    _dispatcher.Invoke(() =>
                    {
                        // Simply remove the last item (take the two buttons into consideration)
                        _root.Items.RemoveAt(_root.Items.Count - 2);
                    });
                }
                else
                {
                    _dispatcher.Invoke(() =>
                    {
                        // Simply add another content holder (take the two buttons into consideration)
                        _root.Items.Insert(_root.Items.Count - 1, GenerateContentHolder(-1));
                    });
                }
            }
        }

        /// <summary>
        /// Tries to retrieve a helping text for a given field.
        /// </summary>
        /// <param name="fieldInfo">The info about the field to find help for.</param>
        /// <returns>A helping string if there was one, otherwise null.</returns>
        private static string GetHelpString(FieldInfo fieldInfo)
        {
            // Try to find documentation of the corresponding assembly
            string docuPath = Path.ChangeExtension(fieldInfo.Module.Assembly.Location, ".XML");
            if (File.Exists(docuPath))
            {
                // Load the documentation xml
                var _docuDoc = new XmlDocument();
                _docuDoc.Load(docuPath);
                // Find the corresponding node
                string path = ("F:" + fieldInfo.DeclaringType.FullName + "." + fieldInfo.Name).Replace('+', '.');
                XmlNode xmlDocuOfMethod = _docuDoc.SelectSingleNode("//member[starts-with(@name, '" + path + "')]");
                // If there was a fitting node, clean and return it
                if (xmlDocuOfMethod != null)
                    return Regex.Replace(xmlDocuOfMethod.InnerXml, @"\s+", " ");
            }
            // We did not find anything - return null
            return null;
        }

        /// <summary>
        /// Tries to find a function that can by invoked in order to generate a default object of the given type.
        /// </summary>
        /// <param name="type">The type for which a default object shall be generated by the function.</param>
        /// <returns>The function returning a default object of the given type.</returns>
        private static Func<object> GetContructorFunction(Type type)
        {
            // Try to find a custom default constructor first
            ConstructorInfo constructor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(DefaultConstructorIdentificationClass) },
                null);
            // Return a function invoking the custom given constructor, if found
            if (constructor != null)
            {
                return () => { return constructor.Invoke(new DefaultConstructorIdentificationClass[] { new DefaultConstructorIdentificationClass() }); };
            }
            else
            {
                // Try to find the parameterless constructor
                constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                // Return a function invoking the constructor, if found
                if (constructor != null)
                {
                    return () => { return constructor.Invoke(new object[0]); };
                }
                else
                {
                    // No constructor found, we do not know how to deal with the type
                    throw new ArgumentException("Cannot find a suitable constructor for the type: " + type.FullName);
                }
            }
        }

        /// <summary>
        /// Parses an ui-element containing one list element.
        /// </summary>
        /// <param name="uiElement">The UI element containing the values.</param>
        /// <param name="listType">The type of the list.</param>
        /// <returns>The value parsed from the ui element.</returns>
        public static object ParseListElement(UIElement uiElement, Type listType)
        {
            // UI element has to be a wrap panel
            WrapPanel contentHolder = null;
            if (uiElement is WrapPanel)
                contentHolder = (uiElement as WrapPanel);
            else
                throw new ArgumentException("Expected a wrap panel as the container of the content!");
            // Parse simple type, if possible
            if (listType == typeof(List<int>))
            {
                string textValue = (contentHolder.Children[0] as TextBox).Text;
                return string.IsNullOrWhiteSpace(textValue) ? 0 : int.Parse(textValue);
            }
            if (listType == typeof(List<double>))
            {
                string textValue = (contentHolder.Children[0] as TextBox).Text;
                return string.IsNullOrWhiteSpace(textValue) ? 0 : double.Parse(textValue, IOConstants.FORMATTER);
            }
            if (listType == typeof(List<bool>))
            {
                return (contentHolder.Children[0] as CheckBox).IsChecked == true;
            }
            if (listType == typeof(List<string>))
            {
                return (contentHolder.Children[0] as TextBox).Text;
            }
            if (listType.GetGenericArguments().Length == 1 && listType.GetGenericArguments()[0].IsEnum)
            {
                return listType.GetGenericArguments()[0].GetEnumValues().GetValue((uiElement as ComboBox).SelectedIndex);
            }
            // Parse structs
            var outerType = listType.GetGenericArguments()[0]; // We are expecting lists here - they can only have one generic type argument
            object elementOuter = Activator.CreateInstance(outerType); // Instantiate the more complex list element
            var innerFields = outerType.GetFields(); // Iterate all fields of the more complex element, but only expect simple types
            for (int i = 0; i < innerFields.Length; i++)
            {
                if (innerFields[i].FieldType == typeof(int))
                {
                    innerFields[i].SetValue(elementOuter, int.Parse((contentHolder.Children[i] as TextBox).Text));
                }
                if (innerFields[i].FieldType == typeof(double))
                {
                    innerFields[i].SetValue(elementOuter, double.Parse((contentHolder.Children[i] as TextBox).Text, IOConstants.FORMATTER));
                }
                if (innerFields[i].FieldType == typeof(bool))
                {
                    innerFields[i].SetValue(elementOuter, (contentHolder.Children[i] as CheckBox).IsChecked == true);
                }
                if (innerFields[i].FieldType == typeof(string))
                {
                    innerFields[i].SetValue(elementOuter, (contentHolder.Children[i] as TextBox).Text);
                }
                if (innerFields[i].FieldType.IsEnum)
                {
                    innerFields[i].SetValue(elementOuter, innerFields[i].FieldType.GetEnumValues().GetValue((contentHolder.Children[i] as ComboBox).SelectedIndex));
                }
            }
            // Return the complex element
            return elementOuter;
        }

        private static void AddUIElement(Dispatcher uiDispatcher, IList rootCollection, object rootObject, FieldInfo fieldInfo, object fieldValue)
        {
            // Get a helping string
            string helpString = GetHelpString(fieldInfo);
            // If the field has to be ignored just add a placeholder
            if (fieldInfo.GetCustomAttributes(false).Any(a => a is LiveAttribute))
            {
                TextBlock placeholder = new TextBlock()
                {
                    Text = "LiveAttribute: " + fieldInfo.Name,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    ToolTip = helpString != null ? helpString : "This field cannot be modified"
                };
                rootCollection.Add(placeholder);
                return;
            }
            // If the field cannot be modified just add a placeholder
            if (fieldInfo.IsInitOnly)
            {
                TextBlock placeholder = new TextBlock()
                {
                    Text = fieldInfo.Name + ": (readonly)",
                    ToolTip = helpString != null ? helpString : "This field cannot be modified"
                };
                rootCollection.Add(placeholder);
                return;
            }
            // Handle simple fields
            if (fieldInfo.FieldType == typeof(int) ||
                fieldInfo.FieldType == typeof(double) ||
                fieldInfo.FieldType == typeof(string) ||
                fieldInfo.FieldType == typeof(bool) ||
                fieldInfo.FieldType.IsEnum)
            {
                // Begin with a description
                var wrapPane = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2, 1, 2, 1) };
                wrapPane.Children.Add(new TextBlock { Text = fieldInfo.Name + ": " });
                if (helpString != null)
                    wrapPane.ToolTip = helpString;
                // Determine detailed type and add the value
                if (fieldInfo.FieldType == typeof(int))
                {
                    // No formatter required for int - just "tostring" the value
                    wrapPane.Children.Add(new TextBox { Text = fieldValue.ToString() });
                    rootCollection.Add(wrapPane);
                    return;
                }
                if (fieldInfo.FieldType == typeof(double))
                {
                    // We need a formatter for double - get the suitable method and add an appropriate formatter then "tostring" it
                    double value = (double)fieldValue;
                    wrapPane.Children.Add(new TextBox { Text = value.ToString(IOConstants.FORMATTER) });
                    rootCollection.Add(wrapPane);
                    return;
                }
                if (fieldInfo.FieldType == typeof(string))
                {
                    // It is already a string - just "tostring" the object again
                    wrapPane.Children.Add(new TextBox { Text = fieldValue != null ? fieldValue.ToString() : "" });
                    rootCollection.Add(wrapPane);
                    return;
                }
                if (fieldInfo.FieldType == typeof(bool))
                {
                    // It's a bool - add a checkbox
                    bool value = (bool)fieldValue;
                    wrapPane.Children.Add(new CheckBox() { IsChecked = value });
                    rootCollection.Add(wrapPane);
                    return;
                }
                if (fieldInfo.FieldType.IsEnum)
                {
                    // It's an enum - add a combobox
                    string value = fieldValue.ToString();
                    var visualElement = new ComboBox() { };
                    foreach (var enumName in fieldInfo.FieldType.GetEnumNames())
                        visualElement.Items.Add(enumName);
                    visualElement.SelectedIndex = fieldInfo.FieldType.GetEnumNames().TakeWhile(n => !value.Equals(n)).Count();
                    wrapPane.Children.Add(visualElement);
                    rootCollection.Add(wrapPane);
                    return;
                }
            }
            // Handle lists
            if (typeof(IList).IsAssignableFrom(fieldInfo.FieldType))
            {
                // Generate a root for the field and a button to init and clear it
                TreeViewItem treeViewRoot = new TreeViewItem() { Header = fieldInfo.Name, };
                if (helpString != null)
                    treeViewRoot.ToolTip = helpString;
                if (fieldValue == null)
                {
                    // List is not instantiated - do it now
                    fieldValue = GetContructorFunction(fieldInfo.FieldType)();
                    fieldInfo.SetValue(rootObject, fieldValue);
                }
                ListHandlerCommand listHandleCommand = new ListHandlerCommand(uiDispatcher, treeViewRoot, fieldInfo.FieldType, fieldValue as IList);
                listHandleCommand.Init();
                rootCollection.Add(treeViewRoot);
                return;
            }
            // Handle complex fields
            if (!fieldInfo.FieldType.IsPrimitive && !fieldInfo.FieldType.Equals(typeof(string)))
            {
                // Generate a root for the field and a button to init and clear it
                TreeViewItem treeViewRoot = new TreeViewItem() { Header = fieldInfo.Name, };
                if (helpString != null)
                    treeViewRoot.ToolTip = helpString;
                DefaultGeneratorCommand initClearCommand = new DefaultGeneratorCommand(uiDispatcher, treeViewRoot, rootObject, fieldInfo);
                initClearCommand.Init();
                rootCollection.Add(treeViewRoot);
                return;
            }

            // We don't know this type - throw an exception
            throw new ArgumentException("Unknown field type: " + fieldInfo.FieldType.FullName);
        }

        private static void ParseUIElement<T>(UIElement uiElement, FieldInfo fieldInfo, int fieldIndex, T parentObject)
        {
            // Ignore live fields
            if (fieldInfo.GetCustomAttributes(false).Any(a => a is LiveAttribute))
            {
                return;
            }
            // Ignore readonly fields
            if (fieldInfo.IsInitOnly)
            {
                return;
            }
            // Handle simple fields
            if (fieldInfo.FieldType == typeof(int) ||
                fieldInfo.FieldType == typeof(double) ||
                fieldInfo.FieldType == typeof(string) ||
                fieldInfo.FieldType == typeof(bool) ||
                fieldInfo.FieldType.IsEnum)
            {
                // Determine detailed type and parse the value
                if (fieldInfo.FieldType == typeof(int))
                {
                    // Parse the value and submit it
                    string textValue = ((uiElement as WrapPanel).Children[1] as TextBox).Text;
                    fieldInfo.SetValue(parentObject, string.IsNullOrWhiteSpace(textValue) ? 0 : int.Parse(textValue));
                    return;
                }
                if (fieldInfo.FieldType == typeof(double))
                {
                    // Parse the value and submit it
                    string textValue = ((uiElement as WrapPanel).Children[1] as TextBox).Text;
                    fieldInfo.SetValue(parentObject, string.IsNullOrWhiteSpace(textValue) ? 0 : double.Parse(textValue, IOConstants.FORMATTER));
                    return;
                }
                if (fieldInfo.FieldType == typeof(string))
                {
                    // Get the value and submit it
                    fieldInfo.SetValue(parentObject, ((uiElement as WrapPanel).Children[1] as TextBox).Text);
                    return;
                }
                if (fieldInfo.FieldType == typeof(bool))
                {
                    // Check the value and submit it
                    fieldInfo.SetValue(parentObject, ((uiElement as WrapPanel).Children[1] as CheckBox).IsChecked == true);
                    return;
                }
                if (fieldInfo.FieldType.IsEnum)
                {
                    // Check the value and submit it
                    object enumVal = Enum.Parse(fieldInfo.FieldType, fieldInfo.FieldType.GetEnumNames()[((uiElement as WrapPanel).Children[1] as ComboBox).SelectedIndex]);
                    fieldInfo.SetValue(parentObject, enumVal);
                    return;
                }
            }
            // Handle lists
            if (typeof(IList).IsAssignableFrom(fieldInfo.FieldType))
            {
                // Ignore empty lists (if the first element is a button instead of some content element the value is assumed to be null)
                if ((uiElement as TreeViewItem).Items.Cast<object>().FirstOrDefault() is Button)
                {
                    return;
                }
                // Init list
                IList listObject = GetContructorFunction(fieldInfo.FieldType)() as IList;
                fieldInfo.SetValue(parentObject, listObject);
                // Parse the list elements
                foreach (var listUIElement in (uiElement as TreeViewItem).Items.OfType<WrapPanel>())
                {
                    listObject.Add(ParseListElement(listUIElement, fieldInfo.FieldType));
                }
                return;

            }
            // Handle complex fields
            if (!fieldInfo.FieldType.IsPrimitive && !(fieldInfo.FieldType == typeof(string)))
            {
                // Ignore empty ones (if the first element is a button instead of some content element the value is assumed to be null)
                if ((uiElement as TreeViewItem).Items.Cast<object>().FirstOrDefault() is Button)
                {
                    return;
                }
                // Get subfields to parse (the field itself was already set by the command)
                var fieldRoot = fieldInfo.GetValue(parentObject);
                FieldInfo[] fields = fieldRoot.GetType().GetFields();
                // Parse all fields of the current method type
                for (var fieldNo = 0; fieldNo < fields.Length; fieldNo++)
                {
                    // Ignore live fields
                    if (fields[fieldNo].GetCustomAttributes(false).Any(a => a is LiveAttribute))
                    {
                        continue;
                    }
                    // Parse the ui element - plus one for skipping the description ui-element
                    ParseUIElement(((uiElement as TreeViewItem).Items[fieldNo + 1] as UIElement), fields[fieldNo], fieldNo, fieldRoot);
                }
                return;
            }

            // We don't know this type - throw an exception
            throw new ArgumentException("Unknown field type: " + fieldInfo.FieldType.FullName);
        }

        public static void FillParameterItems<T>(Dispatcher uiDispatcher, IList rootCollection, T rootObject)
        {
            // --> Add UI configuration elements
            // Clear old UI elements
            uiDispatcher.Invoke(() => { rootCollection.Clear(); });
            // Add description field
            rootCollection.Add(new TextBlock()
            {
                Text = ">> " + rootObject.GetType().Name,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                ToolTip = "Element type"
            });
            // Build UI elements for parameter configuration for each configurable field
            List<UIElement> parameterList = new List<UIElement>();
            foreach (var p in rootObject.GetType().GetFields())
            {
                // Add an appropriate UIElement for the field
                AddUIElement(uiDispatcher, rootCollection, rootObject, p, p.GetValue(rootObject));
            }
            // Add root element as host
            uiDispatcher.Invoke(() => { foreach (var item in parameterList) rootCollection.Add(item); });
        }

        public static void ParseParameterItems<T>(Dispatcher uiDispatcher, UIElementCollection itemsCollection, T parameterObject)
        {
            // Get fields to parse
            FieldInfo[] fields = parameterObject.GetType().GetFields();
            // Parse all fields of the current method type
            for (var fieldNo = 0; fieldNo < fields.Length; fieldNo++)
            {
                // Ignore live fields
                if (fields[fieldNo].GetCustomAttributes(false).Any(a => a is LiveAttribute))
                    continue;
                // Parse the ui element - plus one for skipping the description ui-element
                ParseUIElement(itemsCollection[fieldNo + 1], fields[fieldNo], fieldNo, parameterObject);
            }
        }
    }
}
