using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.VisualToolbox.Arrows
{
    /// <summary>
    /// Indicates which end of the line has an arrow. (see <see href="http://www.charlespetzold.com/blog/2007/04/191200.html"/>)
    /// </summary>
    public enum ArrowEnds
    {
        /// <summary>
        /// Arrow has no arrow heads.
        /// </summary>
        None = 0,
        /// <summary>
        /// Arrow has an arrow head at line start.
        /// </summary>
        Start = 1,
        /// <summary>
        /// Arrow has an arrow head at line end.
        /// </summary>
        End = 2,
        /// <summary>
        /// Arrow has a head on both sides.
        /// </summary>
        Both = 3
    }
}
