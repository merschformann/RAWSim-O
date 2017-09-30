using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace RAWSimO.Visualization.Rendering
{
    /// <summary>
    /// Used to supply colors for heatmap rendering.
    /// </summary>
    public class HeatVisualizer
    {
        /// <summary>
        /// Contains the already generated brushes in order to recycle them.
        /// </summary>
        private static Dictionary<int, Brush> _rainbowBrushes;

        /// <summary>
        /// Generates or recycles a brush indicating the heat of the given value.
        /// </summary>
        /// <param name="value">The value to generate a heat brush for.</param>
        /// <returns>The brush depicting the heat of the value.</returns>
        public static Brush GenerateHeatBrush(double value)
        {
            if (_rainbowBrushes == null)
            {
                _rainbowBrushes = new Dictionary<int, Brush>();
                for (int i = 0; i <= VisualizationConstants.HEAT_BRUSHES; i++)
                    _rainbowBrushes[i] = new SolidColorBrush(GenerateHeatColor((double)i / (double)VisualizationConstants.HEAT_BRUSHES));
            }

            return _rainbowBrushes[(int)(value * VisualizationConstants.HEAT_BRUSHES)];
        }

        /// <summary>
        /// Generates a heat color from the given value.
        /// </summary>
        /// <param name="value">The value to generate a heat color for. This has to be a value in the range of [0,1].</param>
        /// <returns>A color depicting the heat.</returns>
        public static Color GenerateHeatColor(double value)
        {
            if (value < 0 || value > 1)
            {
                throw new ArgumentException();
            }
            // Invert value to fit HSV mode
            value = 1 - value;
            double h = value * 240;
            double s = 0.8;
            double v = 0.97;
            double r, g, b;
            ColorManager.ConvertHSVtoRGB(h, s, v, out r, out g, out b);
            byte R = r == 1 ? (byte)255 : (byte)(r * 256);
            byte G = b == 1 ? (byte)255 : (byte)(g * 256);
            byte B = g == 1 ? (byte)255 : (byte)(b * 256);
            Color color = Color.FromRgb(R, G, B);
            return color;
        }

        /// <summary>
        /// Generates a heat color by using the two colors defining high and low.
        /// Hence, if the given value is 1 the function will return the color for high and if the value is 0 the function will return the color for low.
        /// If the value is fractional, a color matching the fraction between high and low colors will be chosen.
        /// </summary>
        /// <param name="low">The color for a value of 0.</param>
        /// <param name="high">The color for a value of 1.</param>
        /// <param name="value">The value that determines the color.</param>
        /// <returns>The color matching the given value.</returns>
        public static Color GenerateBiChromaticHeatColor(Color low, Color high, double value)
        {
            if (value < 0 || value > 1)
                throw new ArgumentException("Value has to be of the interval [0,1]");
            // ----> If full transparency is given, ignore the color information of that color
            // --> Handle the degree of transparency of the other color
            if (low.A == 0 || high.A == 0)
            {
                if (low.A == 0)
                {
                    byte A = value == 1 ? (byte)255 : (byte)(value * 256);
                    return Color.FromArgb(A, high.R, high.G, high.B);
                }
                else
                {
                    byte A = value == 0 ? (byte)255 : (byte)((1 - value) * 256);
                    return Color.FromArgb(A, low.R, low.G, low.B);
                }
            }
            // --> Handle two solid colors
            else
            {
                // Get HSV representation of given color first
                double hLow, sLow, vLow;
                ColorManager.ConvertColorToHSV(low, out hLow, out sLow, out vLow);
                double hHigh, sHigh, vHigh;
                ColorManager.ConvertColorToHSV(high, out hHigh, out sHigh, out vHigh);
                // Determine fractions based on value
                double hOffsetFromLow = Math.Abs(hHigh - hLow) * value;
                double sOffsetFromLow = Math.Abs(sHigh - sLow) * value;
                double vOffsetFromLow = Math.Abs(vHigh - vLow) * value;
                // Determine HSV based resulting color
                double h = hLow <= hHigh ? hLow + hOffsetFromLow : hLow - hOffsetFromLow;
                double s = sLow <= sHigh ? sLow + sOffsetFromLow : sLow - sOffsetFromLow;
                double v = vLow <= vHigh ? vLow + vOffsetFromLow : vLow - vOffsetFromLow;
                // Convert it to RGB
                double r, g, b;
                ColorManager.ConvertHSVtoRGB(h, s, v, out r, out g, out b);
                byte R = r == 1 ? (byte)255 : (byte)(r * 256);
                byte G = g == 1 ? (byte)255 : (byte)(g * 256);
                byte B = b == 1 ? (byte)255 : (byte)(b * 256);
                // Create and return the color
                Color color = Color.FromRgb(R, G, B);
                return color;
            }
        }
    }
}
