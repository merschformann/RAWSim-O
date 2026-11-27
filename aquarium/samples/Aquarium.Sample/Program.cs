using System.Diagnostics;
using System.Numerics;
using Aquarium;
using Aquarium.Avalonia;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace Aquarium.Sample;

/// <summary>
/// Demo of the Aquarium control.
/// Usage: Aquarium.Sample [warehouse|container] [--scale N] [--screenshot file.png] [--benchmark seconds] [--allow-software-gl]
/// </summary>
public static class Program
{
    public static Options Options { get; private set; } = new();

    [STAThread]
    public static int Main(string[] args)
    {
        Options = Options.Parse(args);
        AppBuilder builder = AppBuilder.Configure<App>().UsePlatformDetect();
        if (Options.AllowSoftwareGl)
            // Avalonia refuses Mesa's software renderer by default (useful for screenshots on machines without GPU, e.g. Xvfb)
            builder = builder.With(new X11PlatformOptions { GlxRendererBlacklist = new List<string>() });
        return builder.WithInterFont().LogToTextWriter(Console.Out, global::Avalonia.Logging.LogEventLevel.Information, "OpenGL", "Visual", "Platform").StartWithClassicDesktopLifetime(args);
    }
}

public sealed record Options(string Demo = "warehouse", int Scale = 1, string? Screenshot = null, double Benchmark = 0, bool AllowSoftwareGl = false, float Zoom = 1)
{
    public static Options Parse(string[] args)
    {
        var options = new Options();
        for (int i = 0; i < args.Length; i++)
        {
            options = args[i] switch
            {
                "--scale" => options with { Scale = int.Parse(args[++i]) },
                "--zoom" => options with { Zoom = float.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture) },
                "--allow-software-gl" => options with { AllowSoftwareGl = true },
                "--screenshot" => options with { Screenshot = args[++i] },
                "--benchmark" => options with { Benchmark = double.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture) },
                _ => options with { Demo = args[i] },
            };
        }
        return options;
    }
}

public sealed class App : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow(Program.Options);
        base.OnFrameworkInitializationCompleted();
    }
}

public sealed class MainWindow : Window
{
    private readonly AquariumView _view = new();
    private readonly TextBlock _status = new() { Margin = new Thickness(8, 4) };
    private readonly Options _options;
    private IDemo _demo;
    private int _frames;
    private TimeSpan _cpuTime;

    public MainWindow(Options options)
    {
        _options = options;
        Title = "Aquarium sample";
        Width = 1280;
        Height = 800;
        _demo = options.Demo == "container" ? new ContainerDemo(options.Scale) : new WarehouseDemo(options.Scale);

        var demoBox = new ComboBox { ItemsSource = new[] { "warehouse", "container" }, SelectedItem = options.Demo == "container" ? "container" : "warehouse", Margin = new Thickness(4) };
        demoBox.SelectionChanged += (_, _) =>
        {
            _demo = (string?)demoBox.SelectedItem == "container" ? new ContainerDemo(_options.Scale) : new WarehouseDemo(_options.Scale);
            ShowDemo();
        };
        var fit = new Button { Content = "Fit", Margin = new Thickness(4) };
        fit.Click += (_, _) => _view.FitToScene();
        var ortho = new CheckBox { Content = "Orthographic", Margin = new Thickness(4) };
        ortho.IsCheckedChanged += (_, _) => { _view.Camera.Orthographic = ortho.IsChecked == true; _view.Invalidate(); };
        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Children = { demoBox, fit, ortho, _status } };
        _status.VerticalAlignment = VerticalAlignment.Center;
        DockPanel.SetDock(toolbar, Dock.Top);
        Content = new DockPanel { Children = { toolbar, _view } };

        _view.Picked += hit =>
        {
            if (hit is PickResult result)
            {
                Instance instance = result.Batch.Get(result.Index);
                _status.Text = $"Picked {result.Batch.Name} #{result.Index} at {instance.Position:F2}";
                _demo.Highlight(result);
            }
        };
        _view.GlInitialized += (info, error) => Dispatcher.UIThread.Post(() =>
        {
            _status.Text = error != null ? "OpenGL error: " + error.Message : "OpenGL: " + info;
            Console.WriteLine(_status.Text);
        });
        _view.FrameRendered += duration => { _frames++; _cpuTime += duration; };
        ShowDemo();

        // Animation
        var clock = Stopwatch.StartNew();
        var last = clock.Elapsed;
        DispatcherTimer.Run(() =>
        {
            var now = clock.Elapsed;
            _demo.Animate((float)(now - last).TotalSeconds);
            last = now;
            return true;
        }, TimeSpan.FromMilliseconds(15));

        // Fps display
        DispatcherTimer.Run(() =>
        {
            if (_frames > 0 && _view.Error == null)
                _status.Text = $"{_demo.Description} | {_frames} fps (cpu {_cpuTime.TotalMilliseconds / _frames:F2} ms/frame) | {_view.GlInfo}";
            _frames = 0;
            _cpuTime = TimeSpan.Zero;
            return true;
        }, TimeSpan.FromSeconds(1));

        Opened += async (_, _) =>
        {
            await Task.Delay(500);
            _view.FitToScene();
            _view.Camera.Zoom(1 / _options.Zoom);
            if (_options.Benchmark > 0)
            {
                _view.Continuous = true;
                _view.Invalidate();
                int frames = 0;
                _view.FrameRendered += _ => frames++;
                await Task.Delay(TimeSpan.FromSeconds(_options.Benchmark));
                Console.WriteLine($"Benchmark: {_demo.Description}: {frames / _options.Benchmark:F1} fps, last status: {_status.Text}");
                _view.Continuous = false;
            }
            if (_options.Screenshot != null)
            {
                try
                {
                    await Task.Delay(300);
                    Screenshot shot = await _view.CaptureAsync().WaitAsync(TimeSpan.FromSeconds(10));
                    Save(shot, _options.Screenshot);
                    Console.WriteLine($"Screenshot written to {_options.Screenshot} ({shot.Width}x{shot.Height})");
                }
                catch (Exception ex) { Console.WriteLine("Screenshot failed: " + ex.Message); }
                Close();
            }
            else if (_options.Benchmark > 0)
                Close();
        };
    }

    private void ShowDemo()
    {
        _view.Scene = _demo.Scene;
        _view.FitToScene();
    }

    private static void Save(Screenshot shot, string file)
    {
        using var bitmap = new WriteableBitmap(new PixelSize(shot.Width, shot.Height), new global::Avalonia.Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Unpremul);
        using (var fb = bitmap.Lock())
            for (int y = 0; y < shot.Height; y++)
                System.Runtime.InteropServices.Marshal.Copy(shot.Rgba, y * shot.Width * 4, fb.Address + y * fb.RowBytes, shot.Width * 4);
        bitmap.Save(file, PngBitmapEncoderOptions.Default);
    }
}

internal interface IDemo
{
    Scene Scene { get; }
    string Description { get; }
    void Animate(float seconds);
    void Highlight(PickResult hit);
}

/// <summary>
/// A grid of storage pods with robots moving between them (RAWSim-O like).
/// </summary>
internal sealed class WarehouseDemo : IDemo
{
    private readonly InstanceBatch _pods, _bots, _floor;
    private readonly Instance[] _botInstances;
    private readonly Vector2[] _botTargets;
    private readonly Random _random = new(42);
    private readonly float _length, _width;

    public WarehouseDemo(int scale)
    {
        Scene = new Scene();
        int blocksX = 10 * scale, blocksY = 6 * scale;
        _length = blocksX * 5 + 2;
        _width = blocksY * 3 + 2;
        _floor = Scene.Add(new InstanceBatch(Mesh.Box, "Floor") { Pickable = false, ShowEdges = false });
        _floor.Set(new[] { new Instance(new Vector3(_length / 2, _width / 2, -0.05f), new Vector3(_length, _width, 0.1f), new Color4(250, 250, 250)) });

        // Pods: blocks of 4x2 storage locations
        var pods = new List<Instance>();
        for (int bx = 0; bx < blocksX; bx++)
            for (int by = 0; by < blocksY; by++)
                for (int x = 0; x < 4; x++)
                    for (int y = 0; y < 2; y++)
                        if (_random.NextDouble() < 0.85)
                            pods.Add(new Instance(new Vector3(2 + bx * 5 + x + 0.5f, 2 + by * 3 + y + 0.5f, 0.6f), new Vector3(0.9f, 0.9f, 1.2f),
                                Color4.FromHsv(215 + _random.NextDouble() * 20, 0.55, 0.92)));
        _pods = Scene.Add(new InstanceBatch(Mesh.Box, "Pods"));
        _pods.Set(pods.ToArray());

        // Bots
        int botCount = 40 * scale * scale;
        _botInstances = new Instance[botCount];
        _botTargets = new Vector2[botCount];
        for (int i = 0; i < botCount; i++)
        {
            _botInstances[i] = new Instance(new Vector3(RandomPosition(), 0.15f), new Vector3(0.7f, 0.7f, 0.3f), new Color4(86, 175, 54));
            _botTargets[i] = RandomPosition();
        }
        _bots = Scene.Add(new InstanceBatch(Mesh.Cylinder, "Bots"));
        _bots.Set(_botInstances);

        // Grid lines on the floor
        var lines = new List<Line>();
        for (int x = 0; x <= (int)_length; x++)
            lines.Add(new Line(new Vector3(x, 0, 0.01f), new Vector3(x, _width, 0.01f), new Color4(200, 200, 200)));
        for (int y = 0; y <= (int)_width; y++)
            lines.Add(new Line(new Vector3(0, y, 0.01f), new Vector3(_length, y, 0.01f), new Color4(200, 200, 200)));
        Scene.Add(new LineBatch("Grid")).Set(lines.ToArray());
        Description = $"Warehouse: {pods.Count} pods, {botCount} bots, {lines.Count} lines";
    }

    private Vector2 RandomPosition() => new((float)_random.NextDouble() * _length, (float)_random.NextDouble() * _width);

    public Scene Scene { get; }
    public string Description { get; }

    public void Animate(float seconds)
    {
        for (int i = 0; i < _botInstances.Length; i++)
        {
            var position = new Vector2(_botInstances[i].Position.X, _botInstances[i].Position.Y);
            Vector2 toTarget = _botTargets[i] - position;
            float distance = toTarget.Length();
            if (distance < 0.1f)
            {
                _botTargets[i] = RandomPosition();
                continue;
            }
            Vector2 step = toTarget / distance * MathF.Min(distance, 1.5f * seconds);
            _botInstances[i].Position += new Vector3(step, 0);
        }
        _bots.Set(_botInstances);
    }

    public void Highlight(PickResult hit) =>
        hit.Batch.Update(span => span[hit.Index].Color = new Color4(255, 140, 0));
}

/// <summary>
/// Boxes packed into containers (SardineCan like).
/// </summary>
internal sealed class ContainerDemo : IDemo
{
    private readonly InstanceBatch _pieces;
    private int _shown;
    private float _elapsed = 8; // Start with the completely packed containers
    private readonly Instance[] _all;

    public ContainerDemo(int scale)
    {
        Scene = new Scene();
        var random = new Random(7);
        var pieces = new List<Instance>();
        var lines = new List<Line>();
        var containerSize = new Vector3(12, 2.4f, 2.6f);
        int containers = 3 * scale;
        for (int c = 0; c < containers; c++)
        {
            var origin = new Vector3(0, c * 4, 0);
            LineBatch.AddBoxEdges(lines, new BoundingBox(origin, origin + containerSize), new Color4(60, 60, 60));
            // Simple layer-wise "packing" of random boxes
            for (float x = 0; x < containerSize.X - 0.3f;)
            {
                float depth = 0.4f + (float)random.NextDouble() * 0.8f;
                depth = MathF.Min(depth, containerSize.X - x);
                for (float y = 0; y < containerSize.Y - 0.2f;)
                {
                    float width = MathF.Min(0.4f + (float)random.NextDouble() * 0.8f, containerSize.Y - y);
                    for (float z = 0; z < containerSize.Z - 0.2f;)
                    {
                        float height = MathF.Min(0.3f + (float)random.NextDouble() * 0.7f, containerSize.Z - z);
                        var size = new Vector3(depth, width, height) * 0.98f;
                        pieces.Add(Instance.FromCorner(origin + new Vector3(x, y, z), size, Color4.FromHsv(random.NextDouble() * 360, 0.45, 0.95)));
                        z += height;
                    }
                    y += width;
                }
                x += depth;
            }
        }
        _all = pieces.ToArray();
        _pieces = Scene.Add(new InstanceBatch(Mesh.Box, "Pieces"));
        Scene.Add(new LineBatch("Containers")).Set(lines.ToArray());
        _shown = _all.Length;
        _pieces.Set(_all);
        Description = $"Containers: {containers} containers, {_all.Length} pieces";
    }

    public Scene Scene { get; }
    public string Description { get; }

    public void Animate(float seconds)
    {
        // Replay the packing order every few seconds
        _elapsed += seconds;
        int shown = Math.Min(_all.Length, (int)(_elapsed / 8 * _all.Length * 1.2));
        if (_elapsed > 10) _elapsed = 0;
        if (shown != _shown)
        {
            _shown = shown;
            _pieces.Set(_all.AsSpan(0, shown));
        }
    }

    public void Highlight(PickResult hit) =>
        hit.Batch.Update(span => span[hit.Index].Color = new Color4(255, 60, 60));
}
