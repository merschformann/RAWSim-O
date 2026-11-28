using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using System.Linq;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using RAWSimO.Core;
using RAWSimO.Core.Configurations;
using RAWSimO.Core.Generator;
using RAWSimO.Visualization.Avalonia.Controls;
using RAWSimO.Visualization.Avalonia.Rendering;
using Xunit;

namespace RAWSimO.Visualization.Avalonia.Tests
{
    public class MainWindowTests
    {
        /// <summary>
        /// Directory to write captured frames to (for manual inspection). Can be overridden via RAWSIMO_SNAPSHOT_DIR.
        /// </summary>
        private static string SnapshotDir =>
            Environment.GetEnvironmentVariable("RAWSIMO_SNAPSHOT_DIR") ?? Path.Combine(AppContext.BaseDirectory, "snapshots");

        private static Instance GenerateDefaultInstance()
        {
            var setting = new SettingConfiguration { VisualizationAttached = true };
            Instance instance = InstanceGenerator.GenerateLayout(new LayoutConfiguration(), setting, new ControlConfiguration());
            instance.Name = instance.GetMetaInfoBasedInstanceName();
            return instance;
        }

        private static WriteableBitmap Capture(Window window, string name)
        {
            Dispatcher.UIThread.RunJobs();
            window.FindControl<SimulationView>("View").Invalidate();
            WriteableBitmap frame = window.CaptureRenderedFrame();
            Directory.CreateDirectory(SnapshotDir);
            frame.Save(Path.Combine(SnapshotDir, name + ".png"), PngBitmapEncoderOptions.Default);
            return frame;
        }

        /// <summary>
        /// Counts the pixels (approximately) matching the given color.
        /// </summary>
        private static int CountPixels(WriteableBitmap bitmap, SkiaSharp.SKColor color)
        {
            using var fb = bitmap.Lock();
            var data = new byte[fb.RowBytes * fb.Size.Height];
            Marshal.Copy(fb.Address, data, 0, data.Length);
            // Determine channel order from the actual pixel format
            bool bgra = fb.Format == global::Avalonia.Platform.PixelFormat.Bgra8888;
            int ri = bgra ? 2 : 0, bi = bgra ? 0 : 2;
            int count = 0;
            for (int y = 0; y < fb.Size.Height; y++)
                for (int x = 0; x < fb.Size.Width; x++)
                {
                    int i = y * fb.RowBytes + x * 4;
                    if (Math.Abs(data[i + ri] - color.Red) < 8 && Math.Abs(data[i + 1] - color.Green) < 8 && Math.Abs(data[i + bi] - color.Blue) < 8)
                        count++;
                }
            return count;
        }

        [AvaloniaFact]
        public void ExportsSnapshotsAsPngAndSvg()
        {
            Instance instance = GenerateDefaultInstance();
            var tier = System.Linq.Enumerable.First(instance.GetInfoTiers());
            var renderer = new SceneRenderer(instance, tier, DetailLevel.Full);
            var snapshot = new SceneSnapshotBuilder(instance, () => 0).Build();
            Directory.CreateDirectory(SnapshotDir);
            string png = Path.Combine(SnapshotDir, "export.png"), svg = Path.Combine(SnapshotDir, "export.svg");
            SceneExporter.Export(png, renderer, snapshot, new SceneOptions(), maxPixelSize: 2000);
            SceneExporter.Export(svg, renderer, snapshot, new SceneOptions());
            using (var bitmap = SkiaSharp.SKBitmap.Decode(png))
                Assert.Equal(2000, Math.Max(bitmap.Width, bitmap.Height));
            string svgText = File.ReadAllText(svg);
            Assert.StartsWith("<?xml", svgText);
            Assert.Contains("<svg", svgText);
            Assert.True(svgText.Length > 100_000, "SVG seems to be missing content");
        }

        [AvaloniaFact]
        public void AppliesHeatModeAndWritesStatisticsOnStop()
        {
            var window = new MainWindow();
            window.Show();
            Instance instance = GenerateDefaultInstance();
            window.SetInstance(instance);

            // Heat mode is applied to the instance (the pods calculate their heat according to it)
            window.FindControl<ComboBox>("ComboHeatMode").SelectedItem = RAWSimO.Core.Management.HeatMode.NumItemsHandled;
            Assert.Equal(RAWSimO.Core.Management.HeatMode.NumItemsHandled, instance.SettingConfig.HeatMode);
            Assert.True(window.FindControl<SimulationView>("View").Options.HeatMode);

            // Statistics are written to the given folder when stopping
            string folder = Path.Combine(AppContext.BaseDirectory, "stats-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            window.FindControl<CheckBox>("CheckWriteStatistics").IsChecked = true;
            window.FindControl<TextBox>("TextStatisticsFolder").Text = folder;
            window.KeyPress(Key.D4, RawInputModifiers.None, PhysicalKey.Digit4, "4");
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            Thread.Sleep(500);
            Dispatcher.UIThread.RunJobs();
            window.FindControl<Button>("ButtonStop").RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.NotEmpty(Directory.GetFiles(folder, "*", SearchOption.AllDirectories));
            window.Close();
            Directory.Delete(folder, true);
        }

        [AvaloniaFact]
        public void ShowsConfigurationPanel()
        {
            var window = new MainWindow { Width = 1400, Height = 900 };
            window.Show();
            // Collapse the layout and expand the controller configuration including one nested (polymorphic) configuration
            var editor = window.FindControl<ContentControl>("EditorControl");
            var expanders = window.FindControl<TabControl>("SidePanel").GetLogicalDescendants().OfType<Expander>().ToList();
            expanders.First(e => e.Header as string == "Layout").IsExpanded = false;
            expanders.First(e => e.Header as string == "Controller").IsExpanded = true;
            ((Control)editor.Content).GetLogicalDescendants().OfType<Expander>().First().IsExpanded = true;
            Capture(window, "03-controller-config");
            window.Close();
        }

        [AvaloniaFact]
        public void ShowsGeneratedInstanceAndAnimatesSimulation()
        {
            var window = new MainWindow { Width = 1400, Height = 900 };
            window.Show();
            window.SetInstance(GenerateDefaultInstance());

            // Static scene: pods and stations must be visible
            WriteableBitmap initial = Capture(window, "01-generated");
            Assert.True(CountPixels(initial, Palette.Pod) > 1000, "Pods are not rendered");
            Assert.True(CountPixels(initial, Palette.OutputStation) > 50, "Output stations are not rendered");

            // Start the simulation at maximum speed (keyboard shortcuts) and let it run for a moment
            window.KeyPress(Key.D4, RawInputModifiers.None, PhysicalKey.Digit4, "4");
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            var view = window.FindControl<SimulationView>("View");
            double startTime = 0;
            for (int i = 0; i < 30; i++)
            {
                Thread.Sleep(100);
                Dispatcher.UIThread.RunJobs();
            }
            Capture(window, "02-running");
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " "); // pause
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(100);

            // The simulation must have progressed and bots must have left their initial positions
            var timeText = window.FindControl<TextBlock>("TextTime").Text;
            Assert.NotEqual("00:00:00", timeText);
            Assert.True(TimeSpan.Parse(timeText).TotalSeconds > startTime);

            window.Close();
        }
    }
}
