using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.DataPreparation
{
    /// <summary>
    /// Declares all defined colors that should be used for plotting.
    /// </summary>
    internal enum PlotColors
    {
        DarkGrey,
        MediumGrey,
        LightGrey,
        DarkBlue,
        MediumBlue,
        Lightblue,
        DarkGreen,
        MediumGreen,
        LightGreen,
        MediumYellow,
        MediumOrange,
        MediumRed,
        MediumViolet,
        MediumTurquoise,
    }

    /// <summary>
    /// Contains coloring information for plot generation.
    /// </summary>
    internal class PlotColoring
    {
        /// <summary>
        /// Defines all hex-codes for the different plot colors.
        /// </summary>
        private static Dictionary<PlotColors, string> _hexValues = new Dictionary<PlotColors, string>()
        {
            { PlotColors.DarkGrey, "#1c1c1c" },
            { PlotColors.MediumGrey, "#474747" },
            { PlotColors.LightGrey, "#737373" },
            { PlotColors.DarkBlue, "#4f658c" },
            { PlotColors.MediumBlue, "#7090c8" },
            { PlotColors.Lightblue, "#8cb4fa" },
            { PlotColors.DarkGreen, "#29732e" },
            { PlotColors.MediumGreen, "#41b449" },
            { PlotColors.LightGreen, "#5afa65" },
            { PlotColors.MediumYellow, "#f7cb38" },
            { PlotColors.MediumOrange, "#ff8a3c" },
            { PlotColors.MediumRed, "#db4937" },
            { PlotColors.MediumViolet, "#925ac7" },
            { PlotColors.MediumTurquoise, "#57bde3" },
        };
        /// <summary>
        /// The order for all available colors.
        /// </summary>
        private static List<PlotColors> _colorOrder = new List<PlotColors>
        {
            PlotColors.MediumBlue,
            PlotColors.MediumGreen,
            PlotColors.MediumYellow,
            PlotColors.MediumRed,
            PlotColors.MediumGrey,
            PlotColors.MediumViolet,
            PlotColors.MediumTurquoise,
            PlotColors.MediumOrange,
            PlotColors.DarkGrey,
            PlotColors.DarkBlue,
            PlotColors.DarkGreen,
            PlotColors.LightGrey,
            PlotColors.Lightblue,
            PlotColors.LightGreen,
        };

        /// <summary>
        /// All available colors in the default order.
        /// </summary>
        public static IEnumerable<string> OrderedHexCodes { get { return _colorOrder.Select(c => _hexValues[c]); } }
        /// <summary>
        /// Retrieves the hex code for the line with the given number.
        /// </summary>
        /// <param name="lineNumber">The line number (starting at 1).</param>
        /// <returns>The hex code for the given line.</returns>
        public static string GetHexCode(int lineNumber) { return _hexValues[_colorOrder[Math.Abs(lineNumber - 1) % _colorOrder.Count]]; }
        /// <summary>
        /// Returns the hex code for the given color.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <returns>The hex code.</returns>
        public static string GetHexCode(PlotColors color) { return _hexValues[color]; }
        /// <summary>
        /// Returns the line style number of the given plot color.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <returns>The default line style number.</returns>
        public static int GetLineStyle(PlotColors color) { return _colorOrder.IndexOf(color) + 1; }
    }
}
