using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Info
{
    /// <summary>
    /// The interface for supplying information about an elevator object.
    /// </summary>
    public interface IElevatorInfo : IGeneralObjectInfo
    {
        /// <summary>
        /// Returns all waypoints of this elevator.
        /// </summary>
        /// <returns>The waypoints of this elevator.</returns>
        IEnumerable<IWaypointInfo> GetInfoWaypoints();
    }
}
