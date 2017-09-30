using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Info
{
    /// <summary>
    /// The interface for getting information about an identifiable object.
    /// </summary>
    public interface IIdentifiableObjectInfo
    {
        /// <summary>
        /// Gets the ID of the object.
        /// </summary>
        /// <returns>The ID.</returns>
        int GetInfoID();
    }
}
