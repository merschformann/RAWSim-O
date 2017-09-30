using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.IO
{
    /// <summary>
    /// Defines the basic functionality of a DTO wrapper for an object.
    /// </summary>
    /// <typeparam name="Orig">The original type.</typeparam>
    /// <typeparam name="Clone">The type of the clone.</typeparam>
    public interface IDataTransferObject<Orig,Clone>
    {
        /// <summary>
        /// Creates a clone out of the original.
        /// </summary>
        /// <param name="original">The original object.</param>
        /// <returns>The cloned object.</returns>
        Clone FromOrig(Orig original);
        /// <summary>
        /// Submits all this object to the instance.
        /// </summary>
        /// <param name="instance">The instance to submit to.</param>
        /// <returns>The original object created at the instance.</returns>
        Orig Submit(Instance instance);
    }
}
