using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Management
{
    /// <summary>
    /// Defines different modes for order and bundle generation.
    /// </summary>
    public enum OrderMode
    {
        /// <summary>
        /// All orders and bundles are generated randomly as required.
        /// </summary>
        Fill,

        /// <summary>
        /// All orders are generated in a poisson process.
        /// </summary>
        Poisson,

        /// <summary>
        /// All orders and bundles are submitted like specified by a given file.
        /// </summary>
        Fixed
    }
}
