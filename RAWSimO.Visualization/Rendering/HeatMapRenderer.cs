using RAWSimO.Core.Control;
using RAWSimO.Core.Info;
using RAWSimO.Core.IO;
using RAWSimO.Core.Statistics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RAWSimO.Visualization.Rendering
{
    /// <summary>
    /// Supplies a number of settings for generating heatmaps.
    /// </summary>
    public class HeatMapRendererConfiguration
    {
        /// <summary>
        /// The canvas to draw on.
        /// </summary>
        public Canvas ContentControl { get; set; }
        /// <summary>
        /// The control holding the canvas.
        /// </summary>
        public Grid ContentHost { get; set; }
        /// <summary>
        /// An action to log output to.
        /// </summary>
        public Action<string> Logger { get; set; }
        /// <summary>
        /// An action that is invoked as soon as rendering is completed.
        /// </summary>
        public Action FinishedCallback { get; set; }
        /// <summary>
        /// The tier for which the heatmap shall be rendered.
        /// </summary>
        public ITierInfo Tier { get; set; }
        /// <summary>
        /// Only snapshots belonging to the tasks contained in this hashset will be considered.
        /// </summary>
        public HashSet<BotTaskType> BotTaskFilter { get; set; } = new HashSet<BotTaskType>(Enum.GetValues(typeof(BotTaskType)).Cast<BotTaskType>());
        /// <summary>
        /// Indicates whether to use the tile count or tile length information.
        /// </summary>
        public bool UseTileCount { get; set; }
        /// <summary>
        /// The number of tiles to generate in x-direction.
        /// </summary>
        public int TilesX { get; set; }
        /// <summary>
        /// The length of a tile in x-dimension.
        /// </summary>
        public double TileLengthX { get; set; }
        /// <summary>
        /// Indicates whether the heatmap will be rendered beneath all other visual objects.
        /// </summary>
        public bool DrawInBackground { get; set; }
        /// <summary>
        /// Indicates whether heat-values will be scaled logarithmically.
        /// </summary>
        public bool Logarithmic { get; set; }
        /// <summary>
        /// Indicates whether heat-values will be weighted by the distance between datapoint and tile.
        /// </summary>
        public bool WeightByDistance { get; set; }
        /// <summary>
        /// Indicates whether the manhattan distance metric will be used instead of an euclidean one.
        /// </summary>
        public bool ManhattanDistance { get; set; }
        /// <summary>
        /// The radius within which datapoints are collected for a tile. The radius is relative to the tile length, i.e. a radius of <code>1.0</code> is exactly as long as the tile length.
        /// </summary>
        public double RadiusRelativeToTileLength { get; set; }
        /// <summary>
        /// Indicates whether bichromatic coloring will be used instead of the default one.
        /// </summary>
        public bool BichromaticColoring { get; set; }
        /// <summary>
        /// The first color used for bichromatic coloring.
        /// </summary>
        public Color BichromaticColorOne { get; set; }
        /// <summary>
        /// The second color used for bichromatic coloring.
        /// </summary>
        public Color BichromaticColorTwo { get; set; }
        /// <summary>
        /// The file storing the heat data.
        /// </summary>
        public string DataFile { get; set; }
        /// <summary>
        /// The index of the heat sub data to visualize.
        /// </summary>
        public int DataIndex { get; set; }
        /// <summary>
        /// This is used to override heat data usage. If not null initial bot positions indicated by the instance will be used instead.
        /// </summary>
        public List<Tuple<int, double, double>> InitialBotPositions { get; set; }
        /// <summary>
        /// Defines the lower bound for filtering datapoints regarding their time stamp (is only used when the data hast time-correspondence).
        /// </summary>
        public double DataTimeFilterLow { get; set; }
        /// <summary>
        /// Defines the upper bound for filtering datapoints regarding their time stamp (is only used when the data hast time-correspondence).
        /// </summary>
        public double DataTimeFilterHigh { get; set; }
    }

    /// <summary>
    /// Exposes functionality to render heatmaps.
    /// </summary>
    public class HeatMapRenderer
    {
        /// <summary>
        /// Creates a new renderer.
        /// </summary>
        /// <param name="config">Contains all necessary information.</param>
        public HeatMapRenderer(HeatMapRendererConfiguration config)
        {
            if (config.UseTileCount) TileLengthX = config.Tier.GetInfoLength() / config.TilesX;
            else TileLengthX = config.Tier.GetInfoLength() / Math.Floor(config.Tier.GetInfoLength() / config.TileLengthX);
            int tilesY = (int)Math.Ceiling(config.Tier.GetInfoWidth() / TileLengthX);
            TileLengthY = config.Tier.GetInfoWidth() / tilesY;
            int xResolution = (int)(Math.Ceiling(config.Tier.GetInfoLength() / TileLengthX));
            int yResolution = (int)(Math.Ceiling(config.Tier.GetInfoWidth() / TileLengthY));
            _heatmap = new double[xResolution, yResolution];
            _config = config;
            _dataFile = config.DataFile;
            Radius = TileLengthX * config.RadiusRelativeToTileLength;
            _tier = config.Tier;
            _contentControl = config.ContentControl;
            _transformer = new Transformation2D(xResolution, yResolution, config.ContentHost.Width, config.ContentHost.Height);
            _finishCallback = config.FinishedCallback;
            _logger = config.Logger;
        }

        #region Core members

        /// <summary>
        /// The config specifying certain settings.
        /// </summary>
        private HeatMapRendererConfiguration _config;
        /// <summary>
        /// The canvas to draw on.
        /// </summary>
        private Canvas _contentControl;
        /// <summary>
        /// The file containing the heat data.
        /// </summary>
        private string _dataFile;
        /// <summary>
        /// The type of the heat data.
        /// </summary>
        private HeatDataType _dataType;
        /// <summary>
        /// The datapoints.
        /// </summary>
        private List<HeatDatapoint> _dataPoints;
        /// <summary>
        /// The actual heatmap.
        /// </summary>
        private double[,] _heatmap;
        /// <summary>
        /// The tier we are looking at.
        /// </summary>
        private ITierInfo _tier;
        /// <summary>
        /// The transformer object used to transform the coordinates between instance and canvas lengths.
        /// </summary>
        private Transformation2D _transformer;
        /// <summary>
        /// The method to call after the operation finishes.
        /// </summary>
        private Action _finishCallback;
        /// <summary>
        /// A logger used to output progress information.
        /// </summary>
        private Action<string> _logger;
        /// <summary>
        /// Indicates whether any datapoints were read.
        /// </summary>
        private bool _anyData = false;

        #endregion

        #region Meta information

        /// <summary>
        /// The length of a tile in x-dimension.
        /// </summary>
        public double TileLengthX { get; private set; }
        /// <summary>
        /// The length of a tile in x-dimension.
        /// </summary>
        public double TileLengthY { get; private set; }
        /// <summary>
        /// The radius that determines which datapoints belong to a tile.
        /// </summary>
        public double Radius { get; private set; }
        /// <summary>
        /// The resulting image. (This is set after renering is done)
        /// </summary>
        public Image ResultImage { get; private set; }

        #endregion

        /// <summary>
        /// Reads the data and renders the heatmap (synchronously).
        /// </summary>
        public bool RenderSync()
        {
            ReadDataPoints(_dataFile);
            if (_anyData)
                BuildHeatmap();
            _finishCallback?.Invoke();
            return _anyData;
        }
        /// <summary>
        /// Reads the data and renders the heatmap (asynchronously).
        /// </summary>
        public void RenderAsync()
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(Render));
        }
        /// <summary>
        /// Reads the data and renders the heatmap - also a wrapper to fit <code>ThreadPool</code>
        /// </summary>
        /// <param name="dummy">Unused data.</param>
        private void Render(object dummy)
        {
            ReadDataPoints(_dataFile);
            if (_anyData)
                BuildHeatmap();
            _finishCallback?.Invoke();
        }

        /// <summary>
        /// Extracts the datatype information from a heat statistics file.
        /// </summary>
        /// <param name="file">The file to determine the datatype for.</param>
        /// <returns>The data contained in the heat file.</returns>
        public static HeatDataType ParseHeatDataType(string file)
        {
            HeatDataType dataType = HeatDataType.PolledLocation;
            using (StreamReader sr = new StreamReader(file))
            {
                string content = sr.ReadToEnd();
                int tagStart = content.IndexOf(IOConstants.STAT_HEAT_TAG_START);
                int tagEnd = content.IndexOf(IOConstants.STAT_HEAT_TAG_END);
                if (tagStart < 0 || tagEnd < 0)
                    throw new FormatException("Could not find heat data type identifier!");
                string ident = content.Substring(tagStart, tagEnd - tagStart).Replace(IOConstants.STAT_HEAT_TAG_START, "").Replace(IOConstants.STAT_HEAT_TAG_END, "");
                bool parseSuccess = Enum.TryParse(ident, out dataType);
                if (!parseSuccess)
                    throw new FormatException("Could not recognize heat data type of file: " + ident);
            }
            return dataType;
        }

        /// <summary>
        /// Gets the available sub data choices for the given data type.
        /// </summary>
        /// <param name="dataType">The data type to get the sub data choices for.</param>
        /// <returns>All available sub information.</returns>
        public static string[] GetSubDataChoices(HeatDataType dataType)
        {
            switch (dataType)
            {
                case HeatDataType.PolledLocation: return Enum.GetNames(typeof(LocationDatapoint.LocationDataType)).ToArray();
                case HeatDataType.TimeIndependentTripData: return Enum.GetNames(typeof(TimeIndependentTripDataPoint.TripDataType)).ToArray();
                case HeatDataType.StorageLocationInfo: return Enum.GetNames(typeof(StorageLocationInfoDatapoint.StorageLocationInfoType)).ToArray();
                default: throw new ArgumentException("Unknown data-type: " + dataType);
            }
        }

        /// <summary>
        /// Read the datapoints from the location file.
        /// </summary>
        /// <param name="file">The path to the data-file.</param>
        private void ReadDataPoints(string file)
        {
            // Identify data type
            _dataType = ParseHeatDataType(file);
            // Init data
            _dataPoints = new List<HeatDatapoint>();
            if (_config.InitialBotPositions != null)
            {
                // Only use the initial bot positions
                _dataPoints.AddRange(_config.InitialBotPositions.Where(t => t.Item1 == _tier.GetInfoID())
                    .Select(t => new LocationDatapoint() { Tier = _tier.GetInfoID(), TimeStamp = 0, X = t.Item2, Y = t.Item3 }));
            }
            else
            {
                // Read data
                using (StreamReader sr = new StreamReader(file))
                {
                    string line = "";
                    while ((line = sr.ReadLine()) != null)
                    {
                        line = line.Trim();
                        // Skip empty and comment lines
                        if (line.StartsWith(IOConstants.COMMENT_LINE) || string.IsNullOrWhiteSpace(line))
                            continue;

                        // Parse data
                        switch (_dataType)
                        {
                            case HeatDataType.PolledLocation:
                                {
                                    LocationDatapoint datapoint = LocationDatapoint.FromCSV(line);
                                    // Only add datapoint, if it belongs to the set of task types that shall be considered
                                    if (_config.BotTaskFilter.Contains(datapoint.BotTask))
                                        // Only add datapoint, if it belongs to the set time window (if a window is specified)
                                        if (_config.DataTimeFilterLow == _config.DataTimeFilterHigh || _config.DataTimeFilterLow <= datapoint.TimeStamp && datapoint.TimeStamp < _config.DataTimeFilterHigh)
                                            // The datapoint made it through all filters - add it
                                            _dataPoints.Add(datapoint);
                                }
                                break;
                            case HeatDataType.TimeIndependentTripData:
                                {
                                    _dataPoints.Add(TimeIndependentTripDataPoint.FromCSV(line));
                                }
                                break;
                            case HeatDataType.StorageLocationInfo:
                                {
                                    StorageLocationInfoDatapoint datapoint = StorageLocationInfoDatapoint.FromCSV(line);
                                    // Only add datapoint, if it belongs to the set time window (if a window is specified)
                                    if (_config.DataTimeFilterLow == _config.DataTimeFilterHigh || _config.DataTimeFilterLow <= datapoint.TimeStamp && datapoint.TimeStamp < _config.DataTimeFilterHigh)
                                        // The datapoint made it through all filters - add it
                                        _dataPoints.Add(datapoint);
                                }
                                break;
                            default: throw new ArgumentException("Unknown data type: " + _dataType.ToString());
                        }
                    }
                }
            }
            // Remark success
            if (_dataPoints.Any())
                _anyData = true;
            else
                _anyData = false;
        }

        /// <summary>
        /// Calculate the distance between x,y and the datapoint.
        /// </summary>
        /// <param name="datapoint">The datapoint constituting the one end of the line.</param>
        /// <param name="x">The x-value of the other end of the line.</param>
        /// <param name="y">The y-value of the other end of the line.</param>
        /// <returns>The euclidean distance between the two points.</returns>
        private double GetDistance(HeatDatapoint datapoint, double x, double y)
        {
            if (_config.ManhattanDistance)
                return Math.Abs(datapoint.X - x) + Math.Abs(datapoint.Y - y);
            else
                return Math.Sqrt(Math.Pow(datapoint.X - x, 2) + Math.Pow(datapoint.Y - y, 2));
        }

        /// <summary>
        /// Gets the value of the datapoint according to the given tile center.
        /// </summary>
        /// <param name="x">The x-value of the center of the heat tile.</param>
        /// <param name="y">The y-value of the center of the heat tile.</param>
        /// <param name="datapoint">The datapoint to weight.</param>
        /// <returns>Either the actual value of the datapoint or the same weighted by the distance to the tile center.</returns>
        private double GetWeightedValue(double x, double y, HeatDatapoint datapoint)
        {
            if (_config.WeightByDistance)
                // Weight datapoint value by the distance to the tiles center
                return (1.0 - GetDistance(datapoint, x, y) / Radius) * datapoint.GetValue(_config.DataIndex);
            else
                return datapoint.GetValue(_config.DataIndex);
        }

        /// <summary>
        /// Returns the heat at the given index.
        /// </summary>
        /// <param name="datapointMap">The datapoint sets per grid element.</param>
        /// <param name="xIndex">The x-index to calculate the heat for.</param>
        /// <param name="yIndex">The y-index to calculate the heat for.</param>
        /// <returns>The heat at the given cell.</returns>
        private double GetHeatValue(HashSet<HeatDatapoint>[,] datapointMap, int xIndex, int yIndex)
        {
            // Transform the heatmap indexes into map coordinates
            double xValue = (xIndex + 0.5) / (double)_heatmap.GetLength(0) * _tier.GetInfoLength(); // Shift the value of the index by 0.5 to get the center of it
            double yValue = (yIndex + 0.5) / (double)_heatmap.GetLength(1) * _tier.GetInfoWidth(); // Shift the value of the index by 0.5 to get the center of it
            // Transform the radius into grid length
            double radiusX = Radius / _tier.GetInfoLength() * _heatmap.GetLength(0);
            double radiusY = Radius / _tier.GetInfoWidth() * _heatmap.GetLength(1);
            // Count the datapoints within radius
            double aggregatedValue = 0;
            for (int x = (int)(xIndex - radiusX); x < xIndex + radiusX; x++)
                for (int y = (int)(yIndex - radiusY); y < yIndex + radiusY; y++)
                    if (x >= 0 && y >= 0 && x < datapointMap.GetLength(0) && y < datapointMap.GetLength(1) && datapointMap[x, y] != null)
                        aggregatedValue += datapointMap[x, y]
                            .Where(d => GetDistance(d, xValue, yValue) < Radius) // Only look at datapoints within the radius
                            .Sum(d => GetWeightedValue(xValue, yValue, d)); // Get the value of this datapoint (distance to the heat tile center is reflected if desired)
            // Return the result
            return aggregatedValue;
        }

        /// <summary>
        /// Builds and renders the heatmap.
        /// </summary>
        private void BuildHeatmap()
        {
            // Prepare datapoints for a bit more efficient calculation
            _logger("Preparing dataset ...");
            HashSet<HeatDatapoint>[,] datapointMap = new HashSet<HeatDatapoint>[_heatmap.GetLength(0), _heatmap.GetLength(1)];
            foreach (var datapoint in _dataPoints)
            {
                int xIndex = (int)(datapoint.X / _tier.GetInfoLength() * _heatmap.GetLength(0));
                int yIndex = (int)(datapoint.Y / _tier.GetInfoWidth() * _heatmap.GetLength(1));
                if (datapointMap[xIndex, yIndex] == null)
                    datapointMap[xIndex, yIndex] = new HashSet<HeatDatapoint>();
                datapointMap[xIndex, yIndex].Add(datapoint);
            }
            // Actually calculate all the heat information
            _logger("Calculating heatmap ...");
            DateTime lastLog = DateTime.MinValue; TimeSpan minLogInterval = TimeSpan.FromSeconds(3);
            int counter = 0; int overallCount = _heatmap.GetLength(0) * _heatmap.GetLength(1);
            Parallel.For(0, _heatmap.GetLength(0), (int x) => // Calculate heat values in parallel across the rows
            {
                for (int y = 0; y < _heatmap.GetLength(1); y++)
                {
                    _heatmap[x, y] = GetHeatValue(datapointMap, x, y);
                    counter++;
                    if (DateTime.Now - lastLog > minLogInterval)
                    {
                        _logger(counter + " / " + overallCount);
                        lastLog = DateTime.Now;
                    }
                }
            });
            _logger(overallCount + " / " + overallCount);
            // Handle logarithmic transformation, if desired
            if (_config.Logarithmic)
            {
                // If logarithmic values are desired, shift all values to numbers greater or equal to 1 first
                double minValue = _heatmap.Cast<double>().Min(v => v);
                double offsetForLog = minValue < 1 ? 1 - minValue : 0;
                if (_config.Logarithmic && offsetForLog > 0)
                    for (int x = 0; x < _heatmap.GetLength(0); x++)
                        for (int y = 0; y < _heatmap.GetLength(1); y++)
                            _heatmap[x, y] += offsetForLog;
                // Transform to logarithmic values if desired
                for (int x = 0; x < _heatmap.GetLength(0); x++)
                    for (int y = 0; y < _heatmap.GetLength(1); y++)
                        _heatmap[x, y] = _heatmap[x, y] <= 0 ? 0 : Math.Log10(_heatmap[x, y]);
            }
            // Normalize the heat to [0,1]
            _logger("Normalizing heatmap ...");
            double maxHeat = double.MinValue;
            for (int x = 0; x < _heatmap.GetLength(0); x++)
                for (int y = 0; y < _heatmap.GetLength(1); y++)
                    maxHeat = Math.Max(maxHeat, _heatmap[x, y]);
            for (int x = 0; x < _heatmap.GetLength(0); x++)
                for (int y = 0; y < _heatmap.GetLength(1); y++)
                    _heatmap[x, y] /= maxHeat;
            // Render the heat overlay
            _logger("Rendering heatmap ...");
            _contentControl.Dispatcher.Invoke(() =>
            {
                // Init image
                Image image = new Image();
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
                image.Opacity = 0.7;
                int bitmapWidth = (int)_transformer.ProjectXLength(_heatmap.GetLength(0));
                int bitmapHeight = (int)_transformer.ProjectYLength(_heatmap.GetLength(1));
                WriteableBitmap writeableBitmap = BitmapFactory.New(bitmapWidth + 1, bitmapHeight + 1); // TODO hotfixing the missing 1-pixel column and row by increasing the size of the bitmap by 1 in each direction
                // Draw all tiles
                for (int x = 0; x < _heatmap.GetLength(0); x++)
                {
                    for (int y = 0; y < _heatmap.GetLength(1); y++)
                    {
                        int x1 = (int)_transformer.ProjectX(x);
                        int y1 = (int)_transformer.ProjectY(y + 1.0);
                        int x2 = (int)_transformer.ProjectX(x + 1.0);
                        int y2 = (int)_transformer.ProjectY(y);
                        Color color = _config.BichromaticColoring ?
                            HeatVisualizer.GenerateBiChromaticHeatColor(_config.BichromaticColorOne, _config.BichromaticColorTwo, _heatmap[x, y]) :
                            HeatVisualizer.GenerateHeatColor(_heatmap[x, y]);
                        writeableBitmap.FillRectangle(x1, y1, x2, y2, color);
                    }
                }
                image.Source = writeableBitmap;
                ResultImage = image;
                // Add the image to the canvas (in background, if desired)
                if (_config.DrawInBackground)
                    _contentControl.Children.Insert(0, image);
                else
                    _contentControl.Children.Add(image);
            });
            // Finished
            _logger("Heatmap done!");
        }
    }
}
