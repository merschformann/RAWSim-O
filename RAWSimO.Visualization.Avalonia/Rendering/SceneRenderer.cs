using System;
using System.Collections.Generic;
using System.Linq;
using RAWSimO.Core.Info;
using SkiaSharp;

namespace RAWSimO.Visualization.Avalonia.Rendering
{
    /// <summary>
    /// Renders one tier of an instance using SkiaSharp.
    /// All drawing is done in simulation (world) coordinates; the caller supplies the world-to-screen transformation.
    /// Static content (tier, waypoint graph, stations, ...) is recorded once into an <see cref="SKPicture"/> and replayed every frame,
    /// only the dynamic content (bots, pods, markers, guards) is drawn from the given <see cref="SceneSnapshot"/>.
    /// This class does not depend on Avalonia, hence, it can also be used to render images headless.
    /// Instances are immutable after construction and can be used from any thread.
    /// </summary>
    public sealed class SceneRenderer
    {
        private readonly SKPicture _staticPicture;
        private readonly float _stroke;
        private readonly DetailLevel _detailLevel;
        private readonly BotColors _botColors;
        private readonly IInputStationInfo[] _inputStations;
        private readonly IOutputStationInfo[] _outputStations;
        private readonly IWaypointInfo[] _waypoints;

        public SceneRenderer(IInstanceInfo instance, ITierInfo tier, DetailLevel detailLevel)
        {
            Tier = tier;
            _detailLevel = detailLevel;
            WorldBounds = new SKRect(0, 0, (float)tier.GetInfoLength(), (float)tier.GetInfoWidth());
            // Reference stroke thickness (same heuristic as the WPF visualization: relative to the waypoint size)
            IWaypointInfo anyWaypoint = instance.GetInfoTiers().SelectMany(t => t.GetInfoWaypoints()).FirstOrDefault();
            double waypointRadius = anyWaypoint != null ? anyWaypoint.GetInfoLength() / 2.0 : 0.1;
            _stroke = (float)(0.2 * waypointRadius);
            // Stable per-bot colors for the rainbow modes
            _botColors = new BotColors(instance.GetInfoBots().Count());
            // Keep static objects for hit testing
            _inputStations = tier.GetInfoInputStations().ToArray();
            _outputStations = tier.GetInfoOutputStations().ToArray();
            _waypoints = tier.GetInfoWaypoints().ToArray();
            // Record static content
            using var recorder = new SKPictureRecorder();
            SKCanvas canvas = recorder.BeginRecording(WorldBounds);
            DrawStatic(canvas, instance, tier);
            _staticPicture = recorder.EndRecording();
        }

        /// <summary>
        /// The tier rendered by this renderer.
        /// </summary>
        public ITierInfo Tier { get; }

        /// <summary>
        /// The bounds of the tier in world coordinates.
        /// </summary>
        public SKRect WorldBounds { get; }

        /// <summary>
        /// Returns the transformation from world coordinates (y pointing up) to canvas coordinates (y pointing down).
        /// </summary>
        /// <param name="zoom">Pixels per world unit.</param>
        /// <param name="offsetX">Canvas x-coordinate of the world origin.</param>
        /// <param name="offsetY">Canvas y-coordinate of the world's top edge (y = tier width).</param>
        public SKMatrix GetWorldToCanvas(float zoom, float offsetX, float offsetY) =>
            new SKMatrix(zoom, 0, offsetX, 0, -zoom, offsetY + zoom * WorldBounds.Height, 0, 0, 1);

        #region Static content

        private void DrawStatic(SKCanvas canvas, IInstanceInfo instance, ITierInfo tier)
        {
            using var fill = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };
            using var outline = new SKPaint { Style = SKPaintStyle.Stroke, IsAntialias = true, Color = Palette.Outline, StrokeWidth = _stroke };
            // Note: the tier background is not part of the static layer (see Draw), so that heatmaps can be drawn in between
            // Waypoint graph
            if (_detailLevel == DetailLevel.Debug)
                DrawWaypointGraph(canvas, tier);
            else if (_detailLevel == DetailLevel.Full)
                DrawWaypoints(canvas, tier, fill, outline);
            // Stations
            foreach (var station in tier.GetInfoInputStations())
                DrawRect(canvas, station, Palette.InputStation, fill, outline);
            foreach (var station in tier.GetInfoOutputStations())
                DrawRect(canvas, station, Palette.OutputStation, fill, outline);
            // Elevator entrances
            const float elevatorSizeFactor = 3;
            fill.Color = Palette.ElevatorEntrance;
            foreach (var wp in instance.GetInfoElevators().SelectMany(e => e.GetInfoWaypoints()).Where(wp => wp.GetInfoCurrentTier() == tier))
            {
                float r = (float)(wp.GetInfoLength() / 2.0) * elevatorSizeFactor;
                canvas.DrawCircle((float)wp.GetInfoCenterX(), (float)wp.GetInfoCenterY(), r, fill);
                canvas.DrawCircle((float)wp.GetInfoCenterX(), (float)wp.GetInfoCenterY(), r, outline);
            }
        }

        private static void DrawRect(SKCanvas canvas, IGeneralObjectInfo obj, SKColor color, SKPaint fill, SKPaint outline)
        {
            // TL coordinates are given in world coordinates (y pointing up), hence, the rectangle extends downwards from TLY
            var immovable = (IImmovableObjectInfo)obj;
            float l = (float)immovable.GetInfoTLX(), t = (float)immovable.GetInfoTLY();
            var rect = new SKRect(l, t - (float)immovable.GetInfoWidth(), l + (float)immovable.GetInfoLength(), t);
            fill.Color = color;
            canvas.DrawRect(rect, fill);
            canvas.DrawRect(rect, outline);
        }

        private void DrawWaypointGraph(SKCanvas canvas, ITierInfo tier)
        {
            // Combine bidirectional connections into one line
            var seen = new HashSet<(IWaypointInfo, IWaypointInfo)>();
            using var path = new SKPath();
            foreach (var wp in tier.GetInfoWaypoints())
                foreach (var other in wp.GetInfoConnectedWaypoints())
                {
                    if (other.GetInfoCurrentTier() != tier || seen.Contains((other, wp)))
                        continue;
                    seen.Add((wp, other));
                    path.MoveTo((float)wp.GetInfoCenterX(), (float)wp.GetInfoCenterY());
                    path.LineTo((float)other.GetInfoCenterX(), (float)other.GetInfoCenterY());
                }
            using var paint = new SKPaint { Style = SKPaintStyle.Stroke, IsAntialias = true, Color = Palette.Waypoint, StrokeWidth = _stroke * 1.5f, StrokeCap = SKStrokeCap.Round };
            canvas.DrawPath(path, paint);
        }

        private void DrawWaypoints(SKCanvas canvas, ITierInfo tier, SKPaint fill, SKPaint outline)
        {
            // Connections as arrows (one path for all of them)
            using var arrows = new SKPath();
            foreach (var wp in tier.GetInfoWaypoints())
                foreach (var other in wp.GetInfoConnectedWaypoints())
                {
                    if (other.GetInfoCurrentTier() != tier)
                        continue;
                    var start = new SKPoint((float)wp.GetInfoCenterX(), (float)wp.GetInfoCenterY());
                    var end = new SKPoint((float)other.GetInfoCenterX(), (float)other.GetInfoCenterY());
                    AddArrow(arrows, start, end);
                }
            using var arrowPaint = new SKPaint { Style = SKPaintStyle.Stroke, IsAntialias = true, Color = Palette.Outline, StrokeWidth = _stroke };
            canvas.DrawPath(arrows, arrowPaint);
            // Waypoint dots
            fill.Color = Palette.Waypoint;
            foreach (var wp in tier.GetInfoWaypoints())
                canvas.DrawCircle((float)wp.GetInfoCenterX(), (float)wp.GetInfoCenterY(), (float)(wp.GetInfoLength() / 2.0), fill);
        }

        private static void AddArrow(SKPath path, SKPoint start, SKPoint end)
        {
            // Line with an arrow head at the end (head length: 20% of the line, 45° opening angle)
            SKPoint d = end - start;
            float length = d.Length;
            if (length <= 0) return;
            path.MoveTo(start);
            path.LineTo(end);
            float head = 0.2f * length;
            double angle = Math.Atan2(d.Y, d.X);
            const double spread = Math.PI / 8; // 22.5° to each side = 45° opening
            path.MoveTo(end.X - head * (float)Math.Cos(angle - spread), end.Y - head * (float)Math.Sin(angle - spread));
            path.LineTo(end);
            path.LineTo(end.X - head * (float)Math.Cos(angle + spread), end.Y - head * (float)Math.Sin(angle + spread));
        }

        #endregion

        #region Dynamic content

        /// <summary>
        /// Renders a complete frame. The canvas must already be transformed to world coordinates (see <see cref="GetWorldToCanvas"/>).
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="snapshot">The dynamic state to draw (may be null to only draw static content).</param>
        /// <param name="options">The render options.</param>
        /// <param name="pixelSize">The size of a screen pixel in world units (used for zoom independent line widths).</param>
        /// <param name="selected">The currently selected object, if any.</param>
        /// <param name="staticCache">An optional cache for the static layer (strongly recommended for continuous rendering).</param>
        /// <param name="grContext">The GPU context of the canvas, if any (used by the static layer cache).</param>
        public void Draw(SKCanvas canvas, SceneSnapshot snapshot, SceneOptions options, float pixelSize, object selected,
            StaticLayerCache staticCache = null, GRContext grContext = null)
        {
            // Tier background
            if (!options.TransparentTier)
            {
                using var background = new SKPaint { Style = SKPaintStyle.Fill, Color = Palette.Tier };
                canvas.DrawRect(WorldBounds, background);
            }
            HeatmapOverlay heatmap = options.Heatmap;
            if (heatmap != null && heatmap.InBackground)
                DrawHeatmap(canvas, heatmap);
            // Static content
            if (staticCache != null)
                staticCache.Draw(canvas, this, grContext);
            else
                DrawStatic(canvas);
            if (snapshot != null)
                DrawDynamic(canvas, snapshot, options, pixelSize, selected);
            if (heatmap != null && !heatmap.InBackground)
                DrawHeatmap(canvas, heatmap);
        }

        private static void DrawHeatmap(SKCanvas canvas, HeatmapOverlay heatmap)
        {
            // One pixel per tile, scaled without interpolation (sharp tiles as in the WPF version)
            using var paint = new SKPaint { Color = SKColors.White.WithAlpha((byte)(Math.Clamp(heatmap.Opacity, 0, 1) * 255)) };
            canvas.DrawImage(heatmap.Image, heatmap.WorldRect, new SKSamplingOptions(SKFilterMode.Nearest), paint);
        }

        private void DrawDynamic(SKCanvas canvas, SceneSnapshot snapshot, SceneOptions options, float pixelSize, object selected)
        {

            using var fill = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };
            using var outline = new SKPaint { Style = SKPaintStyle.Stroke, IsAntialias = true, Color = Palette.Outline, StrokeWidth = _stroke };
            using var line = new SKPaint { Style = SKPaintStyle.Stroke, IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };

            // Path markers
            if (options.DrawPath)
            {
                line.StrokeWidth = _stroke * 10;
                using var path = new SKPath();
                foreach (ref readonly var bot in snapshot.Bots.AsSpan())
                {
                    if (bot.Tier != Tier || bot.Path == null) continue;
                    path.Rewind();
                    path.AddPoly(bot.Path, close: false);
                    line.Color = GetBotColor(bot, options.BotColorMode).WithAlpha(128);
                    canvas.DrawPath(path, line);
                }
            }
            // Destination markers
            if (options.DrawDestination)
            {
                line.StrokeWidth = _stroke * 10;
                foreach (ref readonly var bot in snapshot.Bots.AsSpan())
                {
                    if (bot.Tier != Tier || !bot.HasDestination) continue;
                    line.Color = GetBotColor(bot, options.BotColorMode).WithAlpha(128);
                    canvas.DrawLine(bot.X, bot.Y, bot.DestinationX, bot.DestinationY, line);
                }
            }
            // Pods and bots are batched into a few paths (one per color + one for all outlines) instead of one draw call per object
            DrawPods(canvas, snapshot, options, fill, outline);
            DrawBots(canvas, snapshot, options, fill, outline);
            // Goal markers
            if (options.DrawGoal)
            {
                line.StrokeWidth = _stroke * 5;
                foreach (ref readonly var bot in snapshot.Bots.AsSpan())
                {
                    if (bot.Tier != Tier || !bot.HasGoal) continue;
                    line.Color = GetBotColor(bot, options.BotColorMode);
                    canvas.DrawLine(bot.X, bot.Y, bot.GoalX, bot.GoalY, line);
                }
            }
            // Guards
            if (_detailLevel >= DetailLevel.Debug)
            {
                foreach (ref readonly var guard in snapshot.Guards.AsSpan())
                {
                    if (guard.Tier != Tier) continue;
                    fill.Color = !guard.IsAccessible ? Palette.SemaphoreBlocked : guard.IsBarrier ? Palette.SemaphoreEntry : Palette.SemaphoreGuard;
                    canvas.DrawCircle(guard.X, guard.Y, guard.Radius, fill);
                    canvas.DrawCircle(guard.X, guard.Y, guard.Radius, outline);
                }
            }
            // Selection highlight (a ring with constant screen thickness)
            if (selected != null)
                DrawSelection(canvas, snapshot, selected, pixelSize);
        }

        /// <summary>
        /// Draws the static content (tier, waypoints, stations, ...). The canvas must already be transformed to world coordinates.
        /// </summary>
        public void DrawStatic(SKCanvas canvas) => canvas.DrawPicture(_staticPicture);

        private const int HeatBuckets = 20;

        private void DrawPods(SKCanvas canvas, SceneSnapshot snapshot, SceneOptions options, SKPaint fill, SKPaint outline)
        {
            using var all = new SKPath();
            SKPath[] heatPaths = options.HeatMode ? new SKPath[HeatBuckets + 1] : null;
            var points = new SKPoint[4]; // Reused, AddPoly copies the points
            foreach (ref readonly var pod in snapshot.Pods.AsSpan())
            {
                if (pod.Tier != Tier) continue;
                // Corners of the (rotated) square
                float r = pod.Radius, cos = MathF.Cos(pod.Orientation), sin = MathF.Sin(pod.Orientation);
                float ax = r * (cos - sin), ay = r * (sin + cos); // (+r, +r) rotated
                float bx = r * (cos + sin), by = r * (sin - cos); // (+r, -r) rotated
                points[0] = new SKPoint(pod.X + ax, pod.Y + ay);
                points[1] = new SKPoint(pod.X + bx, pod.Y + by);
                points[2] = new SKPoint(pod.X - ax, pod.Y - ay);
                points[3] = new SKPoint(pod.X - bx, pod.Y - by);
                all.AddPoly(points, close: true);
                if (heatPaths != null)
                {
                    int bucket = (int)(Math.Clamp(float.IsNaN(pod.Heat) ? 0 : pod.Heat, 0, 1) * HeatBuckets);
                    (heatPaths[bucket] ??= new SKPath()).AddPoly(points, close: true);
                }
            }
            if (heatPaths == null)
            {
                fill.Color = Palette.Pod;
                canvas.DrawPath(all, fill);
            }
            else
            {
                for (int i = 0; i < heatPaths.Length; i++)
                {
                    if (heatPaths[i] == null) continue;
                    fill.Color = Palette.GetHeatColor((double)i / HeatBuckets);
                    canvas.DrawPath(heatPaths[i], fill);
                    heatPaths[i].Dispose();
                }
            }
            canvas.DrawPath(all, outline);
        }

        private void DrawBots(SKCanvas canvas, SceneSnapshot snapshot, SceneOptions options, SKPaint fill, SKPaint outline)
        {
            using var outlines = new SKPath();
            var byColor = new Dictionary<SKColor, SKPath>();
            foreach (ref readonly var bot in snapshot.Bots.AsSpan())
            {
                if (bot.Tier != Tier) continue;
                SKColor color = GetBotColor(bot, options.BotColorMode);
                if (!byColor.TryGetValue(color, out SKPath path))
                    byColor[color] = path = new SKPath();
                path.AddCircle(bot.X, bot.Y, bot.Radius);
                outlines.AddCircle(bot.X, bot.Y, bot.Radius);
                // Orientation marker
                outlines.MoveTo(bot.X, bot.Y);
                outlines.LineTo(bot.X + bot.Radius * MathF.Cos(bot.Orientation), bot.Y + bot.Radius * MathF.Sin(bot.Orientation));
            }
            foreach (var (color, path) in byColor)
            {
                fill.Color = color;
                canvas.DrawPath(path, fill);
                path.Dispose();
            }
            canvas.DrawPath(outlines, outline);
        }

        private void DrawSelection(SKCanvas canvas, SceneSnapshot snapshot, object selected, float pixelSize)
        {
            SKRect? bounds = null;
            foreach (ref readonly var bot in snapshot.Bots.AsSpan())
                if (bot.Bot == selected && bot.Tier == Tier) bounds = new SKRect(bot.X - bot.Radius, bot.Y - bot.Radius, bot.X + bot.Radius, bot.Y + bot.Radius);
            if (bounds == null)
                foreach (ref readonly var pod in snapshot.Pods.AsSpan())
                    if (pod.Pod == selected && pod.Tier == Tier) bounds = new SKRect(pod.X - pod.Radius, pod.Y - pod.Radius, pod.X + pod.Radius, pod.Y + pod.Radius);
            if (bounds == null && selected is IImmovableObjectInfo immovable && immovable.GetInfoCurrentTier() == Tier)
            {
                float l = (float)immovable.GetInfoTLX(), t = (float)immovable.GetInfoTLY();
                bounds = new SKRect(l, t - (float)immovable.GetInfoWidth(), l + (float)immovable.GetInfoLength(), t);
            }
            if (bounds == null)
                return;
            SKRect b = bounds.Value;
            b.Inflate(4 * pixelSize, 4 * pixelSize);
            using var paint = new SKPaint { Style = SKPaintStyle.Stroke, IsAntialias = true, Color = Palette.Selection, StrokeWidth = 3 * pixelSize };
            canvas.DrawRoundRect(b, 3 * pixelSize, 3 * pixelSize, paint);
        }

        private SKColor GetBotColor(in BotSnapshot bot, BotColorMode mode) => _botColors.Get(bot, mode);

        #endregion

        #region Hit testing

        /// <summary>
        /// Returns the object at the given world position (bots first, then pods, stations and waypoints), or null.
        /// </summary>
        public object HitTest(SceneSnapshot snapshot, float x, float y)
        {
            if (snapshot != null)
            {
                foreach (ref readonly var bot in snapshot.Bots.AsSpan())
                    if (bot.Tier == Tier && Sq(bot.X - x) + Sq(bot.Y - y) <= Sq(bot.Radius))
                        return bot.Bot;
                foreach (ref readonly var pod in snapshot.Pods.AsSpan())
                    if (pod.Tier == Tier && Math.Abs(pod.X - x) <= pod.Radius && Math.Abs(pod.Y - y) <= pod.Radius)
                        return pod.Pod;
            }
            foreach (var station in _inputStations.Cast<IImmovableObjectInfo>().Concat(_outputStations))
            {
                double l = station.GetInfoTLX(), t = station.GetInfoTLY();
                if (x >= l && x <= l + station.GetInfoLength() && y <= t && y >= t - station.GetInfoWidth())
                    return station;
            }
            if (_detailLevel == DetailLevel.Full)
                foreach (var wp in _waypoints)
                    if (Sq((float)wp.GetInfoCenterX() - x) + Sq((float)wp.GetInfoCenterY() - y) <= Sq((float)wp.GetInfoLength() / 2))
                        return wp;
            return null;
        }

        private static float Sq(float v) => v * v;

        #endregion
    }
}
