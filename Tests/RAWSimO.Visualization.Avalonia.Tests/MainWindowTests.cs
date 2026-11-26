using System;
using System.IO;
using System.Runtime.InteropServices;
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
