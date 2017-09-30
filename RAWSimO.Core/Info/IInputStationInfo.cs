using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Info
{
    /// <summary>
    /// The interface for getting information about an input station object.
    /// </summary>
    public interface IInputStationInfo : IImmovableObjectInfo
    {
        /// <summary>
        /// Gets the number of assigned bundles.
        /// </summary>
        /// <returns>The number of assigned bundles.</returns>
        int GetInfoAssignedBundles();
        /// <summary>
        /// Gets the capacity this station offers.
        /// </summary>
        /// <returns>The capacity of the station.</returns>
        double GetInfoCapacity();
        /// <summary>
        /// Gets the absolute capacity currently in use.
        /// </summary>
        /// <returns>The capacity in use.</returns>
        double GetInfoCapacityUsed();
        /// <summary>
        /// Gets the absolute capacity currently reserved.
        /// </summary>
        /// <returns>The reserved capacity.</returns>
        double GetInfoCapacityReserved();
        /// <summary>
        /// Gets all bundles currently contained in this station.
        /// </summary>
        /// <returns>The bundles of this station.</returns>
        IEnumerable<IItemBundleInfo> GetInfoBundles();
        /// <summary>
        /// Indicates whether the content of the station changed.
        /// </summary>
        /// <returns>Indicates whether the content of the station changed.</returns>
        bool GetInfoContentChanged();
        /// <summary>
        /// Indicates the number that determines the overall sequence in which stations get activated.
        /// </summary>
        /// <returns>The order ID of the station.</returns>
        int GetInfoActivationOrderID();
        /// <summary>
        /// Gets the information queue.
        /// </summary>
        /// <returns>Queue</returns>
        string GetInfoQueue();
        /// <summary>
        /// Indicates whether the station is currently activated (available for new assignments).
        /// </summary>
        /// <returns><code>true</code> if the station is active, <code>false</code> otherwise.</returns>
        bool GetInfoActive();
        /// <summary>
        /// Indicates whether the station is currently blocked due to activity.
        /// </summary>
        /// <returns><code>true</code> if it is blocked, <code>false</code> otherwise.</returns>
        bool GetInfoBlocked();
        /// <summary>
        /// Gets the remaining time this station is blocked.
        /// </summary>
        /// <returns>The remaining time this station is blocked.</returns>
        double GetInfoBlockedLeft();
        /// <summary>
        /// Gets the of requests currently open (not assigned to a bot) for this station.
        /// </summary>
        /// <returns>The number of active requests.</returns>
        int GetInfoOpenRequests();
        /// <summary>
        /// Gets the number of currently open bundles (not yet stored) for this station.
        /// </summary>
        /// <returns>The number of open bundles.</returns>
        int GetInfoOpenBundles();
    }
}
