using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace RAWSimO.Visualization.Rendering
{
    internal class ColorManager
    {
        /// <summary>
        /// Contains the already generated brushes in order to recycle them.
        /// </summary>
        private static Dictionary<double, Brush> _recycledBrushes = new Dictionary<double, Brush>();

        /// <summary>
        /// Generates or recycles a brush indicating the heat of the given value.
        /// </summary>
        /// <param name="hue">The hue-value to generate a brush for.</param>
        /// <returns>The brush based on the given hue.</returns>
        public static Brush GenerateHueBrush(double hue)
        {
            hue = Math.Max(hue, 0);
            hue = Math.Min(hue, 360);
            if (!_recycledBrushes.ContainsKey(hue))
            {
                double r, g, b;
                ConvertHSVtoRGB(hue, 0.8, 0.97, out r, out g, out b);
                byte R = (byte)(r * 256);
                byte G = (byte)(g * 256);
                byte B = (byte)(b * 256);
                _recycledBrushes[hue] = new SolidColorBrush(Color.FromRgb(R, G, B));
            }
            return _recycledBrushes[hue];
        }

        /// <summary>
        /// Converts HSV colors to RGB colors.
        /// </summary>
        /// <param name="h">The hue.</param>
        /// <param name="s">The saturation.</param>
        /// <param name="v">The value.</param>
        /// <param name="r">The red value.</param>
        /// <param name="g">The green value.</param>
        /// <param name="b">The blue value.</param>
        public static void ConvertHSVtoRGB(double h, double s, double v, out double r, out double g, out double b)
        {
            int i;
            double f, p, q, t;
            if (s == 0)
            {
                // achromatic (grey)
                r = g = b = v;
                return;
            }
            h /= 60; // sector 0 to 5
            i = (int)Math.Floor(h);
            f = h - i; // factorial part of h
            p = v * (1 - s);
            q = v * (1 - s * f);
            t = v * (1 - s * (1 - f));
            switch (i)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                default: r = v; g = p; b = q; break;
            }
        }

        /// <summary>
        /// Converts a color to HSV values.
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <param name="hue">The hue of the color.</param>
        /// <param name="saturation">The saturation of the color.</param>
        /// <param name="value">The value of the color.</param>
        public static void ConvertColorToHSV(Color color, out double hue, out double saturation, out double value)
        {
            // Convert RGB [0,255] to rgb [0,1]
            double r = color.R / 255.0; double g = color.G / 255.0; double b = color.B / 255.0;
            // Determine min, max and the delta between those
            double min, max, delta;
            min = Math.Min(Math.Min(r, g), b);
            max = Math.Max(Math.Max(r, g), b);
            delta = max - min;
            // Simply set the value
            value = max;
            // See whether we are dealing with black
            if (max != 0)
                saturation = delta / max; // Not black -> convert saturation
            else {
                // r = g = b = 0 -> it's black
                saturation = 0;
                hue = 0;
                return;
            }
            // See whether we are dealing with gray
            if (delta == 0)
            {
                hue = 0;
                return;
            }
            // See in which space the hue lies
            if (r == max)
                hue = (g - b) / delta; // between yellow & magenta
            else if (g == max)
                hue = 2 + (b - r) / delta; // between cyan & yellow
            else
                hue = 4 + (r - g) / delta; // between magenta & cyan
            hue *= 60; // convert to degrees
            // If we have a negative hue, just make it positive
            if (hue < 0)
                hue += 360;
        }
    }
}
