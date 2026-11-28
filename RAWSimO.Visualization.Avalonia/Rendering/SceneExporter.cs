using System;
using System.IO;
using SkiaSharp;

namespace RAWSimO.Visualization.Avalonia.Rendering
{
    /// <summary>
    /// Exports the complete tier (not just the visible part) as PNG or SVG.
    /// </summary>
    public static class SceneExporter
    {
        /// <summary>
        /// Margin around the tier in output units.
        /// </summary>
        private const float Margin = 10;

        /// <summary>
        /// Exports the scene. The format is determined by the file extension (.svg for vector graphics, PNG otherwise).
        /// </summary>
        /// <param name="file">The file to write.</param>
        /// <param name="renderer">The renderer of the tier to export.</param>
        /// <param name="snapshot">The dynamic state to export.</param>
        /// <param name="options">The render options.</param>
        /// <param name="maxPixelSize">The length of the longer side of the PNG in pixels.</param>
        public static void Export(string file, SceneRenderer renderer, SceneSnapshot snapshot, SceneOptions options, int maxPixelSize = 4000)
        {
            bool svg = string.Equals(Path.GetExtension(file), ".svg", StringComparison.OrdinalIgnoreCase);
            SKRect world = renderer.WorldBounds;
            // SVG: 20 units per world unit (resolution independent anyway), PNG: fit the longer side into the given size
            float zoom = svg ? 20 : (maxPixelSize - 2 * Margin) / Math.Max(world.Width, world.Height);
            int width = (int)Math.Ceiling(world.Width * zoom + 2 * Margin), height = (int)Math.Ceiling(world.Height * zoom + 2 * Margin);

            void Draw(SKCanvas canvas)
            {
                canvas.Clear(Palette.Background);
                SKMatrix m = renderer.GetWorldToCanvas(zoom, Margin, Margin);
                canvas.Concat(in m);
                renderer.Draw(canvas, snapshot, options, 1 / zoom, null);
            }

            using FileStream stream = File.Create(file);
            if (svg)
            {
                using var managed = new SKManagedWStream(stream);
                using (SKCanvas canvas = SKSvgCanvas.Create(new SKRect(0, 0, width, height), managed))
                    Draw(canvas); // The SVG is completed when the canvas is disposed
            }
            else
            {
                using var surface = SKSurface.Create(new SKImageInfo(width, height));
                Draw(surface.Canvas);
                using SKImage image = surface.Snapshot();
                using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
                data.SaveTo(stream);
            }
        }
    }
}
