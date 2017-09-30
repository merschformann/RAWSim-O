using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Info
{
    /// <summary>
    /// The interface for supplying information about a tier object.
    /// </summary>
    public interface ITierInfo : IImmovableObjectInfo
    {
        /// <summary>
        /// Returns the bots currently moving on this tier.
        /// </summary>
        /// <returns>All bots of the tier.</returns>
        IEnumerable<IBotInfo> GetInfoBots();
        /// <summary>
        /// Returns the pods currently placed on this tier.
        /// </summary>
        /// <returns>All pods of this tier.</returns>
        IEnumerable<IPodInfo> GetInfoPods();
        /// <summary>
        /// Returns the input stations placed on this tier.
        /// </summary>
        /// <returns>All input stations located on this tier.</returns>
        IEnumerable<IInputStationInfo> GetInfoInputStations();
        /// <summary>
        /// Returns the output stations placed on this tier.
        /// </summary>
        /// <returns>All output stations located on this tier.</returns>
        IEnumerable<IOutputStationInfo> GetInfoOutputStations();
        /// <summary>
        /// Returns all waypoints on this tier.
        /// </summary>
        /// <returns>The waypoints on this tier.</returns>
        IEnumerable<IWaypointInfo> GetInfoWaypoints();
        /// <summary>
        /// Returns all guards on this tier.
        /// </summary>
        /// <returns>The guards working on this tier.</returns>
        IEnumerable<IGuardInfo> GetInfoGuards();
        /// <summary>
        /// Returns the vertical position of the tier.
        /// </summary>
        /// <returns>The z-position.</returns>
        double GetInfoZ();
    }
}
