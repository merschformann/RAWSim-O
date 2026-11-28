using System;
using SkiaSharp;

namespace RAWSimO.Visualization.Avalonia.Rendering
{
    /// <summary>
    /// Caches the static layer of a <see cref="SceneRenderer"/> as an image in device space.
    /// As long as the view (zoom, pan, size) does not change, every frame only needs to draw this image instead of
    /// replaying all static content (which can be tens of thousands of waypoints, connections, etc.).
    /// Not thread-safe: use one instance per render thread (e.g. per view).
    /// </summary>
    public sealed class StaticLayerCache : IDisposable
    {
        private SKSurface _surface;
        private SKImage _image;
        private SceneRenderer _renderer;
        private SKMatrix _matrix;
        private SKRectI _deviceBounds;
        private GRContext _grContext;

        /// <summary>
        /// Number of times the static layer had to be re-rasterized (for diagnostics).
        /// </summary>
        public int RasterizationCount { get; private set; }

        /// <summary>
        /// Draws the static layer of the renderer onto the canvas. The canvas must already be transformed to world coordinates.
        /// </summary>
        /// <param name="canvas">The target canvas.</param>
        /// <param name="renderer">The renderer whose static content shall be drawn.</param>
        /// <param name="grContext">The GPU context of the target canvas, if any (null for CPU rendering).</param>
        public void Draw(SKCanvas canvas, SceneRenderer renderer, GRContext grContext)
        {
            SKMatrix matrix = canvas.TotalMatrix;
            SKRectI deviceBounds = canvas.DeviceClipBounds;
            if (deviceBounds.Width <= 0 || deviceBounds.Height <= 0)
                return;

            if (_image == null || renderer != _renderer || !matrix.Equals(_matrix) || deviceBounds != _deviceBounds || grContext != _grContext)
            {
                Rasterize(renderer, matrix, deviceBounds, grContext);
                RasterizationCount++;
            }

            // Draw the cached image 1:1 in device space
            canvas.Save();
            canvas.ResetMatrix();
            canvas.DrawImage(_image, deviceBounds.Left, deviceBounds.Top);
            canvas.Restore();
        }

        private void Rasterize(SceneRenderer renderer, SKMatrix matrix, SKRectI deviceBounds, GRContext grContext)
        {
            // Reuse the surface, if the size did not change
            if (_surface == null || deviceBounds.Size != _deviceBounds.Size || grContext != _grContext)
            {
                _surface?.Dispose();
                var info = new SKImageInfo(deviceBounds.Width, deviceBounds.Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
                _surface = (grContext != null ? SKSurface.Create(grContext, false, info) : null) ?? SKSurface.Create(info);
            }
            SKCanvas c = _surface.Canvas;
            c.ResetMatrix();
            c.Clear(SKColors.Transparent);
            c.Translate(-deviceBounds.Left, -deviceBounds.Top);
            c.Concat(in matrix);
            renderer.DrawStatic(c);
            c.Flush();
            _image?.Dispose();
            _image = _surface.Snapshot();
            _renderer = renderer;
            _matrix = matrix;
            _deviceBounds = deviceBounds;
            _grContext = grContext;
        }

        public void Dispose()
        {
            _image?.Dispose();
            _surface?.Dispose();
            _image = null;
            _surface = null;
        }
    }
}
