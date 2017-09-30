using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.SolverWrappers
{
    /// <summary>
    /// Indicates the optimization direction.
    /// </summary>
    public enum OptimizationSense
    {
        /// <summary>
        /// Indicates that we have a minimization problem.
        /// </summary>
        Minimize,
        /// <summary>
        /// Indicates that we have a maximization problem.
        /// </summary>
        Maximize
    }
}
