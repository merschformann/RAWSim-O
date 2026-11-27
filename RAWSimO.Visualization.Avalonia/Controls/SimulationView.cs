using System;
using System.Diagnostics;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using RAWSimO.Visualization.Avalonia.Rendering;
using SkiaSharp;

namespace RAWSimO.Visualization.Avalonia.Controls
{
    /// <summary>
    /// Displays a simulation tier using a single custom Skia draw operation per frame (instead of one UI element per object).
    /// Supports zooming (mouse wheel), panning (drag) and selecting objects (click).
    /// </summary>
    public sealed class SimulationView : Control
    {
        private SceneRenderer _renderer;
        private Func<SceneSnapshot> _snapshotSource;
        private long _renderedVersion = -1;
        private bool _animationRequested;

        // View transformation: screen = offset + zoom * world (with y flipped by the renderer)
        private float _zoom = 1;
        private float _offsetX, _offsetY;
        private bool _userAdjustedView;

        // Pointer handling
        private Point? _dragStart;
        private Point _dragLast;
        private bool _dragMoved;

        // Cached rasterization of the static layer (only accessed on the render thread)
        private readonly StaticLayerCache _staticCache = new StaticLayerCache();

        // Frame statistics (frames are counted on the render thread)
        private int _framesRendered;
        private readonly Stopwatch _fpsClock = Stopwatch.StartNew();

        public SimulationView()
        {
            ClipToBounds = true;
            Focusable = true;
        }

        /// <summary>
        /// The renderer of the currently displayed scene (null, if none).
        /// </summary>
        public SceneRenderer Renderer => _renderer;

        /// <summary>
        /// The options used for rendering. Changes are picked up with the next frame.
        /// </summary>
        public SceneOptions Options { get; } = new SceneOptions();

        /// <summary>
        /// The currently selected object (bot, pod, station, waypoint) or null.
        /// </summary>
        public object SelectedObject { get; private set; }

        /// <summary>
        /// Raised when the user selects an object (or clears the selection by clicking on empty space).
        /// </summary>
        public event Action<object> SelectionChanged;

        /// <summary>
        /// Sets the scene to display.
        /// </summary>
        /// <param name="renderer">The renderer for the tier to display (null clears the view).</param>
        /// <param name="snapshotSource">Supplies the most recent snapshot of the dynamic state.</param>
        /// <param name="keepView">Whether to keep the current zoom and pan (e.g. when only the detail level changed).</param>
        public void SetScene(SceneRenderer renderer, Func<SceneSnapshot> snapshotSource, bool keepView = false)
        {
            _renderer = renderer;
            _snapshotSource = snapshotSource;
            if (!keepView)
            {
                _userAdjustedView = false;
                FitToView();
            }
            Select(null);
            Invalidate();
        }

        /// <summary>
        /// Number of frames rendered per second (measured since the last call).
        /// </summary>
        public double MeasureFps()
        {
            int frames = Interlocked.Exchange(ref _framesRendered, 0);
            double seconds = _fpsClock.Elapsed.TotalSeconds;
            _fpsClock.Restart();
            return seconds > 0 ? frames / seconds : 0;
        }

        /// <summary>
        /// Requests a redraw (e.g. after render options changed).
        /// </summary>
        public void Invalidate()
        {
            _renderedVersion = -1;
            InvalidateVisual();
        }

        /// <summary>
        /// Zooms and pans such that the complete tier is visible.
        /// </summary>
        public void FitToView()
        {
            if (_renderer == null || Bounds.Width <= 0 || Bounds.Height <= 0)
                return;
            SKRect world = _renderer.WorldBounds;
            const float margin = 10;
            float availableW = Math.Max(1, (float)Bounds.Width - 2 * margin), availableH = Math.Max(1, (float)Bounds.Height - 2 * margin);
            _zoom = Math.Min(availableW / world.Width, availableH / world.Height);
            _offsetX = ((float)Bounds.Width - world.Width * _zoom) / 2;
            _offsetY = ((float)Bounds.Height - world.Height * _zoom) / 2;
            InvalidateVisual();
        }

        #region Rendering

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            RequestNextFrame();
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            if (!_userAdjustedView)
                FitToView();
        }

        /// <summary>
        /// Keeps a render loop running in sync with the display's refresh: redraws whenever a new snapshot is available.
        /// </summary>
        private void RequestNextFrame()
        {
            if (_animationRequested)
                return;
            TopLevel topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
                return;
            _animationRequested = true;
            topLevel.RequestAnimationFrame(_ =>
            {
                _animationRequested = false;
                SceneSnapshot snapshot = _snapshotSource?.Invoke();
                if (snapshot != null && snapshot.Version != _renderedVersion)
                    InvalidateVisual();
                RequestNextFrame();
            });
        }

        public override void Render(DrawingContext context)
        {
            var bounds = new Rect(Bounds.Size);
            context.FillRectangle(new SolidColorBrush(Color.FromRgb(Palette.Background.Red, Palette.Background.Green, Palette.Background.Blue)), bounds);
            if (_renderer == null)
                return;
            SceneSnapshot snapshot = _snapshotSource?.Invoke();
            _renderedVersion = snapshot?.Version ?? -1;
            context.Custom(new SceneDrawOperation(
                bounds, _renderer, snapshot, Options.Clone(), _renderer.GetWorldToCanvas(_zoom, _offsetX, _offsetY), 1 / _zoom, SelectedObject, this));
        }

        /// <summary>
        /// Draws the scene on Avalonia's render thread directly onto the Skia canvas.
        /// </summary>
        private sealed class SceneDrawOperation : ICustomDrawOperation
        {
            private readonly SceneRenderer _renderer;
            private readonly SceneSnapshot _snapshot;
            private readonly SceneOptions _options;
            private readonly SKMatrix _worldToCanvas;
            private readonly float _pixelSize;
            private readonly object _selected;
            private readonly SimulationView _owner;

            public SceneDrawOperation(Rect bounds, SceneRenderer renderer, SceneSnapshot snapshot, SceneOptions options, SKMatrix worldToCanvas, float pixelSize, object selected, SimulationView owner)
            {
                Bounds = bounds;
                _renderer = renderer;
                _snapshot = snapshot;
                _options = options;
                _worldToCanvas = worldToCanvas;
                _pixelSize = pixelSize;
                _selected = selected;
                _owner = owner;
            }

            public Rect Bounds { get; }
            public bool HitTest(Point p) => Bounds.Contains(p);
            public bool Equals(ICustomDrawOperation other) => false;
            public void Dispose() { }

            public void Render(ImmediateDrawingContext context)
            {
                var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (leaseFeature == null)
                    return; // Only Skia based rendering is supported (Avalonia's default on all desktop platforms)
                using var lease = leaseFeature.Lease();
                SKCanvas canvas = lease.SkCanvas;
                canvas.Save();
                canvas.ClipRect(new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height));
                SKMatrix m = _worldToCanvas;
                canvas.Concat(in m);
                _renderer.Draw(canvas, _snapshot, _options, _pixelSize, _selected, _owner._staticCache, lease.GrContext);
                canvas.Restore();
                Interlocked.Increment(ref _owner._framesRendered);
            }
        }

        #endregion

        #region Input handling

        /// <summary>
        /// Converts a point in control coordinates into world coordinates.
        /// </summary>
        public SKPoint ScreenToWorld(Point p)
        {
            SKMatrix m = _renderer?.GetWorldToCanvas(_zoom, _offsetX, _offsetY) ?? SKMatrix.Identity;
            return m.Invert().MapPoint((float)p.X, (float)p.Y);
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            Point p = e.GetPosition(this);
            ZoomAt(p, (float)Math.Pow(1.2, e.Delta.Y));
            e.Handled = true;
        }

        /// <summary>
        /// Zooms by the given factor while keeping the given screen point fixed.
        /// </summary>
        public void ZoomAt(Point p, float factor)
        {
            if (_renderer == null) return;
            float newZoom = Math.Clamp(_zoom * factor, 1e-3f, 1e5f);
            factor = newZoom / _zoom;
            // Keep the world point under the cursor fixed: offset' = p - factor * (p - offset).
            // The y-offset refers to the top edge of the tier, which also scales with the zoom (handled by the renderer's matrix).
            _offsetX = (float)p.X - factor * ((float)p.X - _offsetX);
            _offsetY = (float)p.Y - factor * ((float)p.Y - _offsetY);
            _zoom = newZoom;
            _userAdjustedView = true;
            InvalidateVisual();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            Focus();
            if (e.ClickCount == 2)
            {
                // Double click resets the view
                _userAdjustedView = false;
                FitToView();
                _dragStart = null;
                e.Handled = true;
                return;
            }
            _dragStart = _dragLast = e.GetPosition(this);
            _dragMoved = false;
            e.Pointer.Capture(this);
            e.Handled = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (_dragStart == null) return;
            Point p = e.GetPosition(this);
            if (!_dragMoved && Distance(p, _dragStart.Value) < 4)
                return; // Small movements still count as a click
            _dragMoved = true;
            _offsetX += (float)(p.X - _dragLast.X);
            _offsetY += (float)(p.Y - _dragLast.Y);
            _dragLast = p;
            _userAdjustedView = true;
            InvalidateVisual();
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (_dragStart == null) return;
            if (!_dragMoved && _renderer != null)
            {
                // Click: select object at the position
                SKPoint world = ScreenToWorld(e.GetPosition(this));
                Select(_renderer.HitTest(_snapshotSource?.Invoke(), world.X, world.Y));
            }
            _dragStart = null;
            e.Pointer.Capture(null);
        }

        /// <summary>
        /// Selects the given object (null to clear the selection).
        /// </summary>
        public void Select(object obj)
        {
            if (ReferenceEquals(obj, SelectedObject)) return;
            SelectedObject = obj;
            SelectionChanged?.Invoke(obj);
            InvalidateVisual();
        }

        private static double Distance(Point a, Point b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        #endregion
    }
}
