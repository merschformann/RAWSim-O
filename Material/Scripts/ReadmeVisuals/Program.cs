using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Aquarium.Avalonia;
using RAWSimO.Core;
using RAWSimO.Core.Configurations;
using RAWSimO.Core.Generator;
using RAWSimO.Visualization.Avalonia;
using RAWSimO.Visualization.Avalonia.Controls;
using RAWSimO.Visualization.Avalonia.Rendering;

// Records the README visuals of the Avalonia visualization from an Xvfb screen (ffmpeg x11grab).
// Run via record.sh. Usage: ReadmeVisuals <outdir> <shot> with shot = 2d | 3d | large | multi | paths
class Visuals
{
    const int W = 1600, H = 900;
    static string _out = "";

    [STAThread]
    static int Main(string[] args) => AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont()
        .With(new X11PlatformOptions { GlxRendererBlacklist = new List<string>() })
        .AfterSetup(_ => Dispatcher.UIThread.Post(async () => { try { await Run(args); } catch (Exception ex) { Console.WriteLine(ex); } Environment.Exit(0); }))
        .StartWithClassicDesktopLifetime(Array.Empty<string>(), ShutdownMode.OnExplicitShutdown);

    static T C<T>(Window w, string name) where T : Control => w.FindControl<T>(name)!;
    static void Click(Window w, string name) => C<Button>(w, name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    static async Task Run(string[] args)
    {
        _out = args[0];
        string shot = args[1];
        var window = new MainWindow { Width = W, Height = H, Position = new PixelPoint(0, 0) };
        window.Show();
        var layout = new LayoutConfiguration();
        switch (shot)
        {
            case "large": layout.NrHorizontalAisles = 16; layout.NrVerticalAisles = 14; layout.BotCount = 120; layout.NPickStationEast = 8; layout.NReplenishmentStationWest = 6; break;
            case "multi": layout.TierCount = 2; layout.NElevatorsSouth = 1; layout.NElevatorsNorth = 1; layout.BotCount = 48; break;
            default: layout.NrHorizontalAisles = 10; layout.NrVerticalAisles = 8; layout.BotCount = 48; break;
        }
        var setting = new SettingConfiguration { VisualizationAttached = true };
        var control = new ControlConfiguration();
        if (shot == "paths") { control.PathPlanningConfig = new WHCAvStarPathPlanningConfiguration(); layout.BotCount = 40; }
        Instance instance = InstanceGenerator.GenerateLayout(layout, setting, control);
        instance.Name = "readme";
        window.SetInstance(instance);
        C<ToggleButton>(window, "ToggleLog").IsChecked = false;
        await Task.Delay(800);

        // Warm up (orders flowing, bots busy), then continue at a watchable speed
        C<NumericUpDown>(window, "NumericSpeed").Value = 60;
        Click(window, "ButtonPlayPause");
        // Wait for 10 simulated minutes (warm-up resets statistics after 5) (instance initialization takes differently long)
        for (int i = 0; i < 3000 && (instance.Controller?.CurrentTime ?? 0) < 600; i++) await Task.Delay(100);
        Console.WriteLine("Warm-up done at " + instance.Controller?.CurrentTime);
        C<NumericUpDown>(window, "NumericSpeed").Value = shot is "2d" or "3d" ? 3 : 1;
        var view = C<SimulationView>(window, "View");
        var view3D = C<AquariumView>(window, "View3D");

        switch (shot)
        {
            case "2d":
                view.FitToView();
                await Task.Delay(1500);
                await Record("rawsim-o-2d.mp4", 8);
                break;
            case "3d":
                C<ToggleButton>(window, "Toggle3D").IsChecked = true;
                await Task.Delay(1500);
                view3D.FitToScene();
                view3D.Camera.Zoom(1.15f);
                // Slowly orbit while recording
                var orbit = DispatcherTimer.Run(() => { view3D.Camera.Orbit(0.003f, 0); view3D.Invalidate(); return true; }, TimeSpan.FromMilliseconds(33));
                await Record("rawsim-o-3d.mp4", 8);
                orbit.Dispose();
                break;
            case "large":
            case "multi":
                C<ToggleButton>(window, "Toggle3D").IsChecked = true;
                C<ToggleButton>(window, "ToggleInfo").IsChecked = false;
                await Task.Delay(1500);
                view3D.FitToScene();
                await Task.Delay(300);
                view3D.Camera.Zoom(shot == "multi" ? 0.9f : 0.8f);
                if (shot == "multi") view3D.Camera.Orbit(0, 0.1f);
                view3D.Invalidate();
                Click(window, "ButtonPlayPause");
                await Task.Delay(1500);
                await Record(shot == "large" ? "larger-instance-3d.png" : "multi-floor-3d.png", 0);
                break;
            case "paths":
                C<ComboBox>(window, "ComboDetail").SelectedItem = Enum.GetValues<DetailLevel>().Max();
                C<CheckBox>(window, "CheckPath").IsChecked = true;
                C<ComboBox>(window, "ComboBotColors").SelectedItem = BotColorMode.RainbowBotSingleState;
                C<ToggleButton>(window, "ToggleInfo").IsChecked = false;
                await Task.Delay(500);
                view.ZoomAt(new Point(view.Bounds.Width * 0.99, view.Bounds.Height * 0.5), 1.6f);
                await Task.Delay(1500);
                Click(window, "ButtonPlayPause");
                await Task.Delay(1000);
                await Record("paths-2d.png", 0);
                break;
        }
        window.Close();
    }

    static async Task Record(string file, double seconds)
    {
        string display = Environment.GetEnvironmentVariable("DISPLAY")!;
        string target = Path.Combine(_out, file);
        string args = seconds > 0
            ? $"-loglevel error -y -f x11grab -draw_mouse 0 -framerate 15 -video_size {W}x{H} -i {display} -t {seconds} -c:v libx264 -crf 12 -pix_fmt yuv444p {target}"
            : $"-loglevel error -y -f x11grab -draw_mouse 0 -video_size {W}x{H} -i {display} -frames:v 1 {target}";
        using var p = Process.Start("ffmpeg", args)!;
        await p.WaitForExitAsync();
        Console.WriteLine($"Recorded {target} (exit {p.ExitCode})");
    }
}
