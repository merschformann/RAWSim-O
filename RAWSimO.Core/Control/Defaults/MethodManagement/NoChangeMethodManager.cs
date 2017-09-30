using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.MethodManagement
{
    /// <summary>
    /// Defines a method manager that does not change any methods at all.
    /// </summary>
    public class NoChangeMethodManager : MethodManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public NoChangeMethodManager(Instance instance) : base(instance) { Instance = instance; }
    }
}
