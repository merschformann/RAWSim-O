using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Aquarium.Avalonia;

/// <summary>
/// Displays a <see cref="Aquarium.Scene"/> using OpenGL.
/// Mouse: left drag = orbit, right / middle drag (or shift + left drag) = pan, wheel = zoom, click = pick, double click = fit.
/// </summary>
public class AquariumView : OpenGlControlBase, ICustomHitTest
{
    public static readonly StyledProperty<Scene?> SceneProperty = AvaloniaProperty.Register<AquariumView, Scene?>(nameof(Scene));

    private GlSceneRenderer? _renderer;
    private Scene? _subscribedScene;
    private int _renderRequested;
    private Point? _pointerDown, _lastPointer;
    private bool _dragged, _panning, _fitPending;
    private readonly List<TaskCompletionSource<Screenshot>> _pendingCaptures = new();
    private PixelSize _pixelSize;

    public AquariumView()
    {
        Focusable = true;
        ClipToBounds = true;
    }

    /// <summary>
    /// The OpenGL content is not part of the regular hit testing (the control itself draws nothing), hence, without this
    /// the control would never receive pointer events.
    /// </summary>
    public bool HitTest(Point point) => new Rect(Bounds.Size).Contains(point);

    /// <summary>
    /// The scene to display.
    /// </summary>
    public Scene? Scene { get => GetValue(SceneProperty); set => SetValue(SceneProperty, value); }

    /// <summary>
    /// The camera (call <see cref="Invalidate"/> after modifying it).
    /// </summary>
    public Camera Camera { get; } = new();

    /// <summary>
    /// Whether to redraw continuously (otherwise only when the scene or the camera changes).
    /// </summary>
    public bool Continuous { get; set; }

    /// <summary>
    /// Raised when the user clicked (null if nothing was hit).
    /// </summary>
    public event Action<PickResult?>? Picked;

    /// <summary>
    /// Raised once OpenGL is initialized (with a description of the context) or failed (with the error).
    /// </summary>
    public event Action<string?, Exception?>? GlInitialized;

    /// <summary>
    /// Raised after a frame was rendered (with the CPU time it took to submit it).
    /// </summary>
    public event Action<TimeSpan>? FrameRendered;

    /// <summary>
    /// A description of the OpenGL context (once initialized).
    /// </summary>
    public string? GlInfo => _renderer?.Info;

    /// <summary>
    /// The error which prevented rendering (if any).
    /// </summary>
    public Exception? Error { get; private set; }

    /// <summary>
    /// Requests a redraw (can be called from any thread).
    /// </summary>
    public void Invalidate()
    {
        if (Interlocked.Exchange(ref _renderRequested, 1) == 0)
            Dispatcher.UIThread.Post(() =>
            {
                Interlocked.Exchange(ref _renderRequested, 0);
                RequestNextFrameRendering();
            }, DispatcherPriority.Render);
    }

    /// <summary>
    /// Moves the camera such that the complete scene is visible.
    /// </summary>
    public void FitToScene()
    {
        if (Scene == null) return;
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            // Not laid out yet - fit once the size is known
            _fitPending = true;
            return;
        }
        _fitPending = false;
        Camera.Fit(Scene.GetBounds(), (float)(Bounds.Width / Math.Max(Bounds.Height, 1)));
        Invalidate();
    }

    /// <summary>
    /// Renders the next frame and returns its content.
    /// </summary>
    public Task<Screenshot> CaptureAsync()
    {
        var tcs = new TaskCompletionSource<Screenshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_pendingCaptures)
            _pendingCaptures.Add(tcs);
        Invalidate();
        return tcs.Task;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SceneProperty)
        {
            if (_subscribedScene != null)
                _subscribedScene.Changed -= Invalidate;
            _subscribedScene = Scene;
            if (_subscribedScene != null)
                _subscribedScene.Changed += Invalidate;
            Invalidate();
        }
        else if (change.Property == BoundsProperty)
        {
            if (_fitPending)
                FitToScene();
            Invalidate();
        }
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        try
        {
            _renderer = new GlSceneRenderer(gl);
            Error = null;
            GlInitialized?.Invoke(_renderer.Info, null);
        }
        catch (Exception ex)
        {
            Error = ex;
            GlInitialized?.Invoke(null, ex);
        }
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        _renderer?.Dispose();
        _renderer = null;
    }

    protected override void OnOpenGlLost()
    {
        _renderer = null;
        base.OnOpenGlLost();
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        double scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        int width = Math.Max(1, (int)(Bounds.Width * scaling)), height = Math.Max(1, (int)(Bounds.Height * scaling));
        _pixelSize = new PixelSize(width, height);
        List<TaskCompletionSource<Screenshot>> captures;
        lock (_pendingCaptures)
        {
            captures = _pendingCaptures.ToList();
            _pendingCaptures.Clear();
        }
        try
        {
            if (_renderer != null && Scene != null)
            {
                long start = System.Diagnostics.Stopwatch.GetTimestamp();
                _renderer.Render(Scene, Camera, fb, width, height);
                FrameRendered?.Invoke(System.Diagnostics.Stopwatch.GetElapsedTime(start));
                if (captures.Count > 0)
                {
                    var screenshot = new Screenshot(width, height, _renderer.ReadPixels(width, height));
                    captures.ForEach(c => c.TrySetResult(screenshot));
                    captures.Clear();
                }
            }
            else
            {
                gl.ClearColor(0.9f, 0.9f, 0.9f, 1);
                gl.Clear(GlConsts.GL_COLOR_BUFFER_BIT);
            }
        }
        catch (Exception ex)
        {
            Error = ex;
            _renderer = null;
            GlInitialized?.Invoke(null, ex);
        }
        foreach (var capture in captures)
            capture.TrySetException(Error ?? new InvalidOperationException("Nothing to render (no scene or OpenGL is not available)."));
        if (Continuous)
            RequestNextFrameRendering();
    }

    #region Mouse interaction

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        PointerPointProperties props = e.GetCurrentPoint(this).Properties;
        if (e.ClickCount == 2 && props.IsLeftButtonPressed)
        {
            FitToScene();
            e.Handled = true;
            return;
        }
        _pointerDown = _lastPointer = e.GetPosition(this);
        _dragged = false;
        _panning = !props.IsLeftButtonPressed || e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        e.Pointer.Capture(this);
        Focus();
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_lastPointer is not Point last || _pointerDown is not Point down) return;
        Point position = e.GetPosition(this);
        double dx = position.X - last.X, dy = position.Y - last.Y;
        _lastPointer = position;
        if (!_dragged && Math.Abs(position.X - down.X) + Math.Abs(position.Y - down.Y) < 4) return;
        _dragged = true;
        if (_panning)
            Camera.Pan((float)dx, (float)dy, (float)Bounds.Height);
        else
            Camera.Orbit((float)(-dx * 0.008), (float)(dy * 0.008));
        Invalidate();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_pointerDown is Point down && !_dragged && e.InitialPressMouseButton == MouseButton.Left)
            Picked?.Invoke(Pick(down));
        _pointerDown = _lastPointer = null;
        e.Pointer.Capture(null);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        float factor = MathF.Pow(0.85f, (float)e.Delta.Y);
        // Zoom towards the object under the cursor (or the target, if there is none)
        Ray ray = GetRay(e.GetPosition(this));
        PickResult? hit = Scene?.Pick(ray);
        if (hit.HasValue)
            Camera.ZoomTowards(ray.Origin + ray.Direction * hit.Value.Distance, factor);
        else
            Camera.Zoom(factor);
        Invalidate();
        e.Handled = true;
    }

    /// <summary>
    /// Returns the ray through the given point (in control coordinates).
    /// </summary>
    public Ray GetRay(Point point) => Camera.GetRay((float)point.X, (float)point.Y, (float)Bounds.Width, (float)Bounds.Height);

    /// <summary>
    /// Picks the instance at the given point (in control coordinates).
    /// </summary>
    public PickResult? Pick(Point point) => Scene?.Pick(GetRay(point));

    #endregion
}

/// <summary>
/// The content of a rendered frame.
/// </summary>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
/// <param name="Rgba">RGBA pixels, top row first.</param>
public sealed record Screenshot(int Width, int Height, byte[] Rgba);
