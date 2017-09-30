using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.SolverWrappers
{
    /// <summary>
    /// Defines the type of a variable.
    /// </summary>
    public enum VariableType
    {
        /// <summary>
        /// Variable is continuous.
        /// </summary>
        Continuous,
        /// <summary>
        /// Variable is binary. Hence, it can only be set to 1 or 0.
        /// </summary>
        Binary,
        /// <summary>
        /// Variable is an integral number.
        /// </summary>
        Integer
    }
}
