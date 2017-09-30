using RAWSimO.Core.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Geometrics
{
    /// <summary>
    /// A simple class comprising a rectangle for bound checking.
    /// </summary>
    public class SimpleRectangle
    {
        /// <summary>
        /// Creates a new simple rectangle.
        /// </summary>
        /// <param name="tier">The tier this rectangle is located on.</param>
        /// <param name="x">The x-value of the lower left corner.</param>
        /// <param name="y">The y-value of the lower left corner.</param>
        /// <param name="length">The length of this rectangle (x-direction).</param>
        /// <param name="width">The width of this rectangle (y-direction).</param>
        public SimpleRectangle(Tier tier, double x, double y, double length, double width)
        {
            Tier = tier;
            XLower = x;
            XUpper = x + length;
            Length = length;
            YLower = y;
            YUpper = y + width;
            Width = width;
        }
        /// <summary>
        /// The tier this rectangle is located on.
        /// </summary>
        public Tier Tier { get; private set; }
        /// <summary>
        /// The lower border of this rectangle in x-direction.
        /// </summary>
        public double XLower { get; private set; }
        /// <summary>
        /// The upper border of this rectangle in x-direction.
        /// </summary>
        public double XUpper { get; private set; }
        /// <summary>
        /// The lower border of this rectangle in y-direction.
        /// </summary>
        public double YLower { get; private set; }
        /// <summary>
        /// The upper border of this rectangle in y-direction.
        /// </summary>
        public double YUpper { get; private set; }
        /// <summary>
        /// The length of this rectangle in x-dimension.
        /// </summary>
        public double Length { get; private set; }
        /// <summary>
        /// The width of this rectangle in y-dimension.
        /// </summary>
        public double Width { get; private set; }
        /// <summary>
        /// Checks whether the given coordinates are within this rectangle.
        /// </summary>
        /// <param name="tier">The tier coordinate.</param>
        /// <param name="x">The x-coordinate.</param>
        /// <param name="y">The y-coordinate.</param>
        /// <returns><code>true</code> if the coordinates are within the rectangle, <code>false</code> otherwise.</returns>
        public bool IsContained(Tier tier, double x, double y)
        {
            return
                // Same tier and...
                Tier == tier &&
                // must be within x-range and...
                XLower <= x && x <= XUpper &&
                // y-range
                YLower <= y && y <= YUpper;
        }
    }
}
