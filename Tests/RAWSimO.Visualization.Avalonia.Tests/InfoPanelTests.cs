using System;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using RAWSimO.Core;
using RAWSimO.Core.Configurations;
using RAWSimO.Core.Generator;
using RAWSimO.Visualization.Avalonia.Controls;
using Xunit;

namespace RAWSimO.Visualization.Avalonia.Tests
{
    public class InfoPanelTests
    {
        private static string SnapshotDir =>
            Environment.GetEnvironmentVariable("RAWSIMO_SNAPSHOT_DIR") ?? Path.Combine(AppContext.BaseDirectory, "snapshots");

        private MainWindow _window;

        /// <summary>
        /// Lets time pass and refreshes the UI (the periodic UI timer does not tick in real time in headless tests).
        /// </summary>
        private void Pump(int ms) => WaitFor(() => false, ms);

        private void WaitFor(Func<bool> condition, int timeoutMs = 5000)
        {
            for (int waited = 0; !condition() && waited < timeoutMs; waited += 50)
            {
                Thread.Sleep(50);
                _window.RefreshUi();
                Dispatcher.UIThread.RunJobs();
            }
        }

        private static void Capture(Window window, string name)
        {
            Dispatcher.UIThread.RunJobs();
            window.FindControl<SimulationView>("View").Invalidate();
            WriteableBitmap frame = window.CaptureRenderedFrame();
            Directory.CreateDirectory(SnapshotDir);
            frame.Save(Path.Combine(SnapshotDir, name + ".png"), PngBitmapEncoderOptions.Default);
        }

        [AvaloniaFact]
        public void ShowsStationsOrdersAndSelectionDetails()
        {
            var window = _window = new MainWindow();
            window.Show();
            var setting = new SettingConfiguration { VisualizationAttached = true };
            Instance instance = InstanceGenerator.GenerateLayout(new LayoutConfiguration(), setting, new ControlConfiguration());
            window.SetInstance(instance);
            T Find<T>(string name) where T : Control => window.FindControl<T>(name);

            // Stations are listed right away (header row + one row per station)
            Pump(300);
            int stationCount = instance.InputStations.Count + instance.OutputStations.Count;
            Assert.Equal(stationCount + 1, Find<Grid>("GridStations").RowDefinitions.Count);

            // Run the simulation for a moment at full speed
            window.KeyPress(Key.D4, RawInputModifiers.None, PhysicalKey.Digit4, "4");
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            Pump(3000);
            Assert.False(Find<TabControl>("SidePanel").IsVisible, "Setup panel is collapsed when the simulation starts");
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " "); // pause
            Pump(500);
            Capture(window, "info-01-overview");
            Assert.True(Find<Grid>("GridStatistics").RowDefinitions.Count > 5, "Statistics are not shown");

            // Orders tab
            var tabs = Find<TabControl>("InfoPanel");
            tabs.SelectedIndex = 1;
            Pump(500);
            Assert.True(System.Text.RegularExpressions.Regex.IsMatch((string)Find<Expander>("ExpanderOpenOrders").Header, @"Open orders \([1-9]\d*\)"), Find<Expander>("ExpanderOpenOrders").Header + " @ " + instance.Controller.CurrentTime + "\n" + Find<TextBox>("TextLog").Text);
            Assert.True(Find<ChipListView>("ChipsOpenOrders").Rows.Count > 0);
            Capture(window, "info-02-orders");

            // Select an output station (while paused): its orders are shown
            var view = Find<SimulationView>("View");
            view.Select(instance.OutputStations.First());
            WaitFor(() => Find<TextBlock>("TextSelectionTitle").Text.StartsWith("Output station"));
            Assert.Same(Find<TabItem>("TabSelection"), tabs.SelectedItem);
            Assert.True(Find<TextBlock>("TextSelectionTitle").Text.StartsWith("Output station"), Find<TextBlock>("TextSelectionTitle").Text + " @ " + Find<TextBlock>("TextTime").Text + " / " + instance.Controller.CurrentTime + ": " + string.Join(", ", Find<Grid>("GridSelection").Children.OfType<TextBlock>().Select(t => t.Text)) + "\n" + Find<TextBox>("TextLog").Text);
            Assert.True(Find<ChipListView>("ChipsSelectionOpenOrders").IsVisible);
            Capture(window, "info-03-station");

            // Select a pod: its content is shown
            view.Select(instance.Pods.First(p => instance.GetInfoItemDescriptions().Any(i => p.GetInfoContent(i) > 0)));
            WaitFor(() => Find<TextBlock>("TextSelectionTitle").Text.StartsWith("Pod"));
            Assert.True(Find<ChipListView>("ChipsSelection").Rows.Count > 0, "Pod content is not shown");
            Capture(window, "info-04-pod");

            // Panels can be toggled (Ctrl+B / Ctrl+I / Ctrl+L)
            window.KeyPress(Key.L, RawInputModifiers.Control, PhysicalKey.L, "l");
            window.KeyPress(Key.I, RawInputModifiers.Control, PhysicalKey.I, "i");
            window.KeyPress(Key.B, RawInputModifiers.Control, PhysicalKey.B, "b");
            Dispatcher.UIThread.RunJobs();
            Assert.False(Find<TextBox>("TextLog").IsVisible);
            Assert.False(tabs.IsVisible);
            Assert.True(Find<TabControl>("SidePanel").IsVisible);
            Capture(window, "info-05-collapsed");
            window.KeyPress(Key.L, RawInputModifiers.Control, PhysicalKey.L, "l");
            Dispatcher.UIThread.RunJobs();
            Assert.True(Find<TextBox>("TextLog").IsVisible);
            window.Close();
        }
    }
}
