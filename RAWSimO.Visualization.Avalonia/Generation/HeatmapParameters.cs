using System.ComponentModel;

namespace RAWSimO.Visualization.Avalonia.Generation
{
    /// <summary>
    /// Colors available for bichromatic heatmap coloring (same choices as in the WPF visualization).
    /// </summary>
    public enum HeatmapColor { Transparent, White, Black, Blue, Red, Green, Yellow, Purple, Lime, Navy }

    /// <summary>
    /// Parameters for rendering heatmaps. Edited via the generic config editor, the defaults match the WPF visualization.
    /// </summary>
    public sealed class HeatmapParameters
    {
        [Description("Specifies whether the tile size is given by the number of tiles (TilesX) or by their length (TileLengthX).")]
        public bool UseTileCount = true;
        [Description("The number of tiles in x-dimension (tiles in y-dimension get a similar length).")]
        public int TilesX = 100;
        [Description("The length of a tile in x-dimension (tiles in y-dimension get a similar length).")]
        public double TileLengthX = 0.2;
        [Description("The radius for filtering the datapoints relevant for a tile, relative to the tile length.")]
        public double RadiusRelativeToTileLength = 4.0;
        [Description("Specifies whether to use a logarithmic scale for the values.")]
        public bool Logarithmic = true;
        [Description("Specifies whether to weight the values of the datapoints by their distance to the tile center.")]
        public bool WeightByDistance = true;
        [Description("Specifies whether to use the manhattan distance metric instead of the euclidean one.")]
        public bool ManhattanDistance = false;
        [Description("Specifies whether to use bichromatic coloring (low / high color) instead of the rainbow coloring.")]
        public bool BichromaticColoring = false;
        [Description("The color for low values when using bichromatic coloring.")]
        public HeatmapColor LowColor = HeatmapColor.Black;
        [Description("The color for high values when using bichromatic coloring.")]
        public HeatmapColor HighColor = HeatmapColor.Transparent;
        [Description("Specifies whether to draw the heatmap behind the instance (instead of in front of it).")]
        public bool DrawInBackground = false;
        [Description("Specifies whether the tier background is transparent (instead of white).")]
        public bool TransparentTier = false;
        [Description("The length of the time-windows (in seconds) when rendering one heatmap per time-window.")]
        public double TimeWindowLength = 1200;

        /// <summary>
        /// Creates a copy (all fields are values).
        /// </summary>
        public HeatmapParameters Clone() => (HeatmapParameters)MemberwiseClone();
    }
}
