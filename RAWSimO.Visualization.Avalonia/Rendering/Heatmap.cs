using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RAWSimO.Core.Control;
using RAWSimO.Core.IO;
using RAWSimO.Core.Statistics;
using RAWSimO.Visualization.Avalonia.Generation;
using SkiaSharp;

namespace RAWSimO.Visualization.Avalonia.Rendering
{
    /// <summary>
    /// Data selection for a heatmap calculation.
    /// </summary>
    public sealed class HeatmapDataSelection
    {
        /// <summary>
        /// The heat data file (e.g. locationspolled.heat).
        /// </summary>
        public string DataFile { get; init; }
        /// <summary>
        /// Index of the value to use (see <see cref="Heatmap.GetSubDataChoices"/>).
        /// </summary>
        public int DataIndex { get; init; }
        /// <summary>
        /// Only location data of these bot tasks is considered (null = all).
        /// </summary>
        public HashSet<BotTaskType> BotTaskFilter { get; init; }
        /// <summary>
        /// Time window filter [low, high). Equal values disable the filter.
        /// </summary>
        public double TimeFilterLow { get; init; }
        public double TimeFilterHigh { get; init; }
        /// <summary>
        /// If given, these positions (tier ID, x, y) are used instead of the data of the file (used for the time window before the simulation start).
        /// </summary>
        public List<(int tier, double x, double y)> InitialBotPositions { get; init; }
    }

    /// <summary>
    /// A calculated heatmap: a grid of values in [0,1] covering a tier (index [0,0] is the tile at the origin).
    /// </summary>
    public sealed class Heatmap
    {
        public double[,] Values { get; private init; }
        public double TileLengthX { get; private init; }
        public double TileLengthY { get; private init; }
        public double Radius { get; private init; }
        public double TierLength { get; private init; }
        public double TierWidth { get; private init; }

        #region Data files

        /// <summary>
        /// Reads the type of data contained in a heat data file.
        /// </summary>
        public static HeatDataType ParseHeatDataType(string file)
        {
            string content = File.ReadAllText(file);
            int tagStart = content.IndexOf(IOConstants.STAT_HEAT_TAG_START, StringComparison.Ordinal);
            int tagEnd = content.IndexOf(IOConstants.STAT_HEAT_TAG_END, StringComparison.Ordinal);
            if (tagStart < 0 || tagEnd < 0)
                throw new FormatException("Could not find heat data type identifier!");
            string ident = content.Substring(tagStart, tagEnd - tagStart).Replace(IOConstants.STAT_HEAT_TAG_START, "").Replace(IOConstants.STAT_HEAT_TAG_END, "");
            if (!Enum.TryParse(ident, out HeatDataType dataType))
                throw new FormatException("Could not recognize heat data type of file: " + ident);
            return dataType;
        }

        /// <summary>
        /// Returns the selectable values of the given data type.
        /// </summary>
        public static string[] GetSubDataChoices(HeatDataType dataType) => dataType switch
        {
            HeatDataType.PolledLocation => Enum.GetNames(typeof(LocationDatapoint.LocationDataType)),
            HeatDataType.TimeIndependentTripData => Enum.GetNames(typeof(TimeIndependentTripDataPoint.TripDataType)),
            HeatDataType.StorageLocationInfo => Enum.GetNames(typeof(StorageLocationInfoDatapoint.StorageLocationInfoType)),
            _ => throw new ArgumentException("Unknown data-type: " + dataType),
        };

        private static List<HeatDatapoint> ReadDataPoints(HeatmapDataSelection data, int tierId)
        {
            var points = new List<HeatDatapoint>();
            if (data.InitialBotPositions != null)
            {
                // Only use the initial bot positions
                points.AddRange(data.InitialBotPositions.Where(p => p.tier == tierId)
                    .Select(p => new LocationDatapoint { Tier = tierId, TimeStamp = 0, X = p.x, Y = p.y }));
                return points;
            }
            HeatDataType dataType = ParseHeatDataType(data.DataFile);
            bool InTimeWindow(double timeStamp) =>
                data.TimeFilterLow == data.TimeFilterHigh || data.TimeFilterLow <= timeStamp && timeStamp < data.TimeFilterHigh;
            foreach (string rawLine in File.ReadLines(data.DataFile))
            {
                string line = rawLine.Trim();
                if (line.StartsWith(IOConstants.COMMENT_LINE) || string.IsNullOrWhiteSpace(line))
                    continue;
                switch (dataType)
                {
                    case HeatDataType.PolledLocation:
                        {
                            LocationDatapoint point = LocationDatapoint.FromCSV(line);
                            if ((data.BotTaskFilter == null || data.BotTaskFilter.Contains(point.BotTask)) && InTimeWindow(point.TimeStamp))
                                points.Add(point);
                        }
                        break;
                    case HeatDataType.TimeIndependentTripData:
                        points.Add(TimeIndependentTripDataPoint.FromCSV(line));
                        break;
                    case HeatDataType.StorageLocationInfo:
                        {
                            StorageLocationInfoDatapoint point = StorageLocationInfoDatapoint.FromCSV(line);
                            if (InTimeWindow(point.TimeStamp))
                                points.Add(point);
                        }
                        break;
                    default: throw new ArgumentException("Unknown data type: " + dataType);
                }
            }
            return points;
        }

        #endregion

        #region Calculation

        /// <summary>
        /// Calculates a heatmap for a tier. Returns null, if there is no data (e.g. an empty time window).
        /// </summary>
        public static Heatmap Calculate(double tierLength, double tierWidth, int tierId, HeatmapDataSelection data, HeatmapParameters parameters, Action<string> log = null)
        {
            log ??= _ => { };
            // Tile dimensions
            double tileLengthX = parameters.UseTileCount
                ? tierLength / parameters.TilesX
                : tierLength / Math.Floor(tierLength / parameters.TileLengthX);
            int tilesY = (int)Math.Ceiling(tierWidth / tileLengthX);
            double tileLengthY = tierWidth / tilesY;
            int resX = (int)Math.Ceiling(tierLength / tileLengthX);
            int resY = (int)Math.Ceiling(tierWidth / tileLengthY);
            double radius = tileLengthX * parameters.RadiusRelativeToTileLength;

            List<HeatDatapoint> points = ReadDataPoints(data, tierId);
            if (points.Count == 0)
                return null;

            // Sort datapoints into the grid for a more efficient lookup
            log("Preparing dataset ...");
            var map = new List<HeatDatapoint>[resX, resY];
            foreach (var point in points)
            {
                int xi = Math.Clamp((int)(point.X / tierLength * resX), 0, resX - 1);
                int yi = Math.Clamp((int)(point.Y / tierWidth * resY), 0, resY - 1);
                (map[xi, yi] ??= new List<HeatDatapoint>()).Add(point);
            }

            double Distance(HeatDatapoint d, double x, double y) => parameters.ManhattanDistance
                ? Math.Abs(d.X - x) + Math.Abs(d.Y - y)
                : Math.Sqrt((d.X - x) * (d.X - x) + (d.Y - y) * (d.Y - y));

            // Calculate the heat of all tiles (in parallel across the columns)
            log("Calculating heatmap ...");
            var values = new double[resX, resY];
            double radiusX = radius / tierLength * resX, radiusY = radius / tierWidth * resY;
            int done = 0, overall = resX * resY;
            long lastLog = Environment.TickCount64;
            Parallel.For(0, resX, xIndex =>
            {
                for (int yIndex = 0; yIndex < resY; yIndex++)
                {
                    // Center of the tile
                    double cx = (xIndex + 0.5) / resX * tierLength;
                    double cy = (yIndex + 0.5) / resY * tierWidth;
                    double sum = 0;
                    for (int x = (int)(xIndex - radiusX); x < xIndex + radiusX; x++)
                        for (int y = (int)(yIndex - radiusY); y < yIndex + radiusY; y++)
                        {
                            if (x < 0 || y < 0 || x >= resX || y >= resY || map[x, y] == null)
                                continue;
                            foreach (var d in map[x, y])
                            {
                                double distance = Distance(d, cx, cy);
                                if (distance >= radius)
                                    continue;
                                double value = d.GetValue(data.DataIndex);
                                sum += parameters.WeightByDistance ? (1.0 - distance / radius) * value : value;
                            }
                        }
                    values[xIndex, yIndex] = sum;
                }
                int count = Interlocked.Add(ref done, resY);
                long now = Environment.TickCount64;
                long last = Interlocked.Read(ref lastLog);
                if (now - last > 3000 && Interlocked.CompareExchange(ref lastLog, now, last) == last)
                    log($"{count} / {overall}");
            });

            // Logarithmic transformation (shift values to >= 1 first)
            if (parameters.Logarithmic)
            {
                double min = values.Cast<double>().Min();
                double offset = min < 1 ? 1 - min : 0;
                for (int x = 0; x < resX; x++)
                    for (int y = 0; y < resY; y++)
                    {
                        double v = values[x, y] + offset;
                        values[x, y] = v <= 0 ? 0 : Math.Log10(v);
                    }
            }

            // Normalize to [0,1]
            double max = values.Cast<double>().Max();
            if (max > 0)
                for (int x = 0; x < resX; x++)
                    for (int y = 0; y < resY; y++)
                        values[x, y] /= max;

            log("Heatmap done!");
            return new Heatmap { Values = values, TileLengthX = tileLengthX, TileLengthY = tileLengthY, Radius = radius, TierLength = tierLength, TierWidth = tierWidth };
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Creates an overlay image of the heatmap (one pixel per tile).
        /// </summary>
        public HeatmapOverlay CreateOverlay(HeatmapParameters parameters, float opacity)
        {
            int w = Values.GetLength(0), h = Values.GetLength(1);
            var bitmap = new SKBitmap(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Unpremul));
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    double v = double.IsNaN(Values[x, y]) ? 0 : Math.Clamp(Values[x, y], 0, 1);
                    // Row 0 is drawn at world y = 0 (the renderer's canvas has the y-axis pointing up)
                    bitmap.SetPixel(x, y, parameters.BichromaticColoring
                        ? Palette.GetBichromaticColor(ToColor(parameters.LowColor), ToColor(parameters.HighColor), v)
                        : Palette.GetHeatColor(v));
                }
            bitmap.SetImmutable();
            return new HeatmapOverlay(SKImage.FromBitmap(bitmap), new SKRect(0, 0, (float)TierLength, (float)TierWidth), opacity, parameters.DrawInBackground);
        }

        private static SKColor ToColor(HeatmapColor color) => color switch
        {
            HeatmapColor.Transparent => new SKColor(255, 255, 255, 0),
            HeatmapColor.White => SKColors.White,
            HeatmapColor.Black => SKColors.Black,
            HeatmapColor.Blue => new SKColor(0, 0, 255),
            HeatmapColor.Red => new SKColor(255, 0, 0),
            HeatmapColor.Green => new SKColor(0, 128, 0),
            HeatmapColor.Yellow => new SKColor(255, 255, 0),
            HeatmapColor.Purple => new SKColor(128, 0, 128),
            HeatmapColor.Lime => new SKColor(0, 255, 0),
            HeatmapColor.Navy => new SKColor(0, 0, 128),
            _ => SKColors.Black,
        };

        #endregion
    }

    /// <summary>
    /// A rendered heatmap that is drawn by the <see cref="SceneRenderer"/> (immutable).
    /// </summary>
    public sealed class HeatmapOverlay
    {
        public HeatmapOverlay(SKImage image, SKRect worldRect, float opacity, bool inBackground)
        {
            Image = image;
            WorldRect = worldRect;
            Opacity = opacity;
            InBackground = inBackground;
        }

        public SKImage Image { get; }
        public SKRect WorldRect { get; }
        public float Opacity { get; }
        public bool InBackground { get; }

        public HeatmapOverlay WithOpacity(float opacity) => new HeatmapOverlay(Image, WorldRect, opacity, InBackground);
    }
}
