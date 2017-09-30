using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Info
{
    /// <summary>
    /// The interface for supplying information about a waypoint object.
    /// </summary>
    public interface IWaypointInfo : IImmovableObjectInfo
    {
        /// <summary>
        /// Indicates whether the waypoint is a storage location.
        /// </summary>
        /// <returns><code>true</code> if it is a storage location, <code>false</code> otherwise.</returns>
        bool GetInfoStorageLocation();
        /// <summary>
        /// Gets all outgoing connections of the waypoint.
        /// </summary>
        /// <returns>An enumeration of waypoints this waypoint has a directed edge to.</returns>
        IEnumerable<IWaypointInfo> GetInfoConnectedWaypoints();
    }
}
