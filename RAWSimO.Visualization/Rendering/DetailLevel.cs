using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Visualization.Rendering
{
    public enum DetailLevel
    {
        /// <summary>
        /// No details are added. The design is as simple as possible.
        /// </summary>
        None,

        /// <summary>
        /// Rendering is done using additional aesthetic components.
        /// </summary>
        Aesthetics,

        /// <summary>
        /// Rendering is done for debug purposes.
        /// </summary>
        Debug,

        /// <summary>
        /// Rendering includes not only basic but full debug information.
        /// </summary>
        Full
    }
}
