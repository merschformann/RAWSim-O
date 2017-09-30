using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Toolbox
{
    /// <summary>
    /// This class contains functionality to support basic math stuff.
    /// </summary>
    public class MathHelpers
    {
        /// <summary>
        /// This function simply returns the minimum value of all arguments supplied.
        /// </summary>
        /// <param name="args">The arguments to get the minimum of.</param>
        /// <returns>The minimal value of all given ones.</returns>
        public static double Min(params double[] args) { return args.Min(a => a); }
        /// <summary>
        /// This function simply returns the maximum value of all arguments supplied.
        /// </summary>
        /// <param name="args">The arguments to get the maximum of.</param>
        /// <returns>The maximal value of all given ones.</returns>
        public static double Max(params double[] args) { return args.Max(a => a); }
    }
}
