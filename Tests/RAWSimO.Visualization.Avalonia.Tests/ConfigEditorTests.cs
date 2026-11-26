using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using RAWSimO.Core.Configurations;
using RAWSimO.Core.Generator;
using RAWSimO.Core.Items;
using RAWSimO.Visualization.Avalonia.Controls;
using RAWSimO.Visualization.Avalonia.Generation;
using Xunit;

namespace RAWSimO.Visualization.Avalonia.Tests
{
    public class ConfigEditorTests
    {
        /// <summary>
        /// Finds the editor control of the row with the given label (rows are a label and an editor in a Grid).
        /// </summary>
        private static T FindEditor<T>(Control root, string label) where T : Control =>
            root.GetLogicalDescendants().OfType<Grid>()
                .Where(p => p.Children.Count == 2 && p.Children[0] is TextBlock t && t.Text == label)
                .Select(p => p.Children[1]).OfType<T>().First();

        /// <summary>
        /// Finds the implementation selector of a nested configuration field.
        /// </summary>
        private static ComboBox FindSelector(Control root, string label) =>
            root.GetLogicalDescendants().OfType<Expander>()
                .Where(e => e.Header is Grid h && h.Children[0] is TextBlock t && t.Text == label)
                .Select(e => (ComboBox)((Grid)e.Header).Children[1]).First();

        [AvaloniaFact]
        public void CreatesEditorsForAllConfigurations()
        {
            foreach (object config in new object[] { new LayoutConfiguration(), new SettingConfiguration(), new ControlConfiguration(),
                new OrderGenerator.SimpleItemGeneratorPreConfiguration(), new AlphabetOrderParameters() })
            {
                Control editor = ConfigEditor.Create(config);
                Assert.NotEmpty(editor.GetLogicalDescendants());
            }
        }

        [AvaloniaFact]
        public void EditsSimpleValuesAndRejectsInvalidInput()
        {
            var layout = new LayoutConfiguration();
            Control editor = ConfigEditor.Create(layout);
            var window = new Window { Content = editor };
            window.Show();

            var botCount = FindEditor<TextBox>(editor, nameof(LayoutConfiguration.BotCount));
            botCount.Text = "7";
            Assert.Equal(7, layout.BotCount);
            botCount.Text = "seven";
            Assert.Equal(7, layout.BotCount); // invalid input keeps the last valid value
            Assert.True(DataValidationErrors.GetHasErrors(botCount));
            botCount.Text = "9";
            Assert.False(DataValidationErrors.GetHasErrors(botCount));
            Assert.Equal(9, layout.BotCount);

            var radius = FindEditor<TextBox>(editor, nameof(LayoutConfiguration.BotRadius));
            radius.Text = "0.25"; // invariant culture, as in the configuration files
            Assert.Equal(0.25, layout.BotRadius);

            var singleLane = FindEditor<CheckBox>(editor, nameof(LayoutConfiguration.SingleLane));
            singleLane.IsChecked = !layout.SingleLane;
            Assert.Equal(singleLane.IsChecked, layout.SingleLane);
            window.Close();
        }

        [AvaloniaFact]
        public void SwitchesPolymorphicConfigurations()
        {
            var control = new ControlConfiguration();
            Control editor = ConfigEditor.Create(control);
            var window = new Window { Content = editor };
            window.Show();

            ComboBox selector = FindSelector(editor, nameof(ControlConfiguration.PathPlanningConfig));
            string current = ConfigEditor.GetDisplayName(control.PathPlanningConfig.GetType());
            Assert.Equal(current, selector.SelectedItem.ToString());
            object other = selector.Items.Cast<object>().First(n => n.ToString() != "(none)" && n.ToString() != current);
            selector.SelectedItem = other;
            Assert.Equal(other.ToString(), ConfigEditor.GetDisplayName(control.PathPlanningConfig.GetType()));
            selector.SelectedItem = selector.Items.Cast<object>().First(n => n.ToString() == "(none)");
            Assert.Null(control.PathPlanningConfig);
            window.Close();
        }

        [AvaloniaFact]
        public void EditsListsOfStructs()
        {
            var config = new ColoredWordConfiguration();
            config.ColorProbabilities = new List<Skvp<LetterColors, double>>();
            Control editor = ConfigEditor.Create(config);
            var window = new Window { Content = editor };
            window.Show();

            var expander = editor.GetLogicalDescendants().OfType<Expander>().First(e => (e.Header as string)?.StartsWith(nameof(ColoredWordConfiguration.ColorProbabilities)) == true);
            Button Add() => ((StackPanel)expander.Content).Children.OfType<Button>().Single(b => (string)b.Content == "Add");
            Add().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Add().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(2, config.ColorProbabilities.Count);
            Assert.Equal("ColorProbabilities (2)", expander.Header);

            // Edit the value of the second element (struct elements have to be written back to the list)
            var secondRow = (StackPanel)((StackPanel)expander.Content).Children[1];
            ((TextBox)secondRow.Children[1]).Text = "0.75";
            ((ComboBox)secondRow.Children[0]).SelectedItem = LetterColors.Yellow;
            Assert.Equal(0.75, config.ColorProbabilities[1].Value);
            Assert.Equal(LetterColors.Yellow, config.ColorProbabilities[1].Key);

            // Remove the first element
            var firstRow = (StackPanel)((StackPanel)expander.Content).Children[0];
            firstRow.Children.OfType<Button>().Single().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Single(config.ColorProbabilities);
            Assert.Equal(LetterColors.Yellow, config.ColorProbabilities[0].Key);
            window.Close();
        }

        [AvaloniaFact]
        public void GeneratesInstanceFromEditedConfiguration()
        {
            var window = new MainWindow();
            window.Show();
            var layoutEditor = (Control)window.FindControl<ContentControl>("EditorLayout").Content;
            FindEditor<TextBox>(layoutEditor, nameof(LayoutConfiguration.BotCount)).Text = "5";

            window.FindControl<Button>("ButtonGenerate").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            // Generation runs in the background - wait for it
            for (int i = 0; i < 300 && window.FindControl<TextBlock>("TextStatus").Text.Contains("Generating"); i++)
            {
                System.Threading.Thread.Sleep(50);
                Dispatcher.UIThread.RunJobs();
            }
            Dispatcher.UIThread.RunJobs();
            Assert.Contains(" 5 bots", window.FindControl<TextBlock>("TextStatus").Text);
            window.Close();
        }

        [Fact]
        public void ConfigurationsSurviveXmlRoundTrip()
        {
            // The UI clones configurations via XML serialization before generating
            var control = new ControlConfiguration();
            var serializer = new XmlSerializer(typeof(ControlConfiguration));
            using var writer = new StringWriter();
            serializer.Serialize(writer, control);
            var copy = (ControlConfiguration)serializer.Deserialize(new StringReader(writer.ToString()));
            Assert.Equal(control.PathPlanningConfig.GetType(), copy.PathPlanningConfig.GetType());
            new XmlSerializer(typeof(SettingConfiguration)).Serialize(new StringWriter(), new SettingConfiguration());
            new XmlSerializer(typeof(LayoutConfiguration)).Serialize(new StringWriter(), new LayoutConfiguration());
        }
    }
}
