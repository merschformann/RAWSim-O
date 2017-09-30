using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Info
{
    /// <summary>
    /// The more basic interface for supplying information about a movable object.
    /// </summary>
    public interface IMovableObjectInfo : IGeneralObjectInfo
    {
        /// <summary>
        /// Gets the orientation the object is facing in radians. (An element facing east is defined with orientation 0 or equally 2*pi.)
        /// </summary>
        /// <returns>The orientation.</returns>
        double GetInfoOrientation();
        /// <summary>
        /// Gets the radius defining the size of the object.
        /// </summary>
        /// <returns>The radius.</returns>
        double GetInfoRadius();
        /// <summary>
        /// Indicates whether the underlying object changed since the last call of <code>GetChanged()</code>.
        /// </summary>
        /// <returns><code>true</code> if the object changed since the last call of this method, <code>false</code> otherwise.</returns>
        bool GetInfoChanged();
    }
}
