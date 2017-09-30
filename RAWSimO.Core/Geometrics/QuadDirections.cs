using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Geometrics
{
    /// <summary>
    /// Defines the different sectors of a <code>QuadNode</code>.
    /// </summary>
    public enum QuadDirections : int
    {
        /// <summary>
        /// South-West sector.
        /// </summary>
        SW = 0,

        /// <summary>
        /// South-East sector.
        /// </summary>
        SE = 1,

        /// <summary>
        /// North-West sector.
        /// </summary>
        NW = 2,

        /// <summary>
        /// North-East sector.
        /// </summary>
        NE = 3,
    }
}
