using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Toolbox
{
    /// <summary>
    /// Exposes some useful extensions for working with strings.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Pads both sides of a string keeping the actual one in the middle.
        /// </summary>
        /// <param name="s">The string to pad.</param>
        /// <param name="length">The desired length of the resulting string.</param>
        /// <returns>The resulting string.</returns>
        public static string PadBoth(this string s, int length)
        {
            int spaces = length - s.Length;
            int padLeft = spaces / 2 + s.Length;
            return s.PadLeft(padLeft).PadRight(length);
        }
    }
}
