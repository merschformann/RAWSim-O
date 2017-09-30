using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Info
{
    /// <summary>
    /// The interface for getting information about an instance object.
    /// </summary>
    public interface IInstanceInfo
    {
        /// <summary>
        /// Returns an enumeration of all pods of this instance.
        /// </summary>
        /// <returns>All pods of this instance.</returns>
        IEnumerable<IPodInfo> GetInfoPods();
        /// <summary>
        /// Returns an enumeration of all bots of this instance.
        /// </summary>
        /// <returns>All bots of this instance.</returns>
        IEnumerable<IBotInfo> GetInfoBots();
        /// <summary>
        /// Returns an enumeration of all tiers of this instance.
        /// </summary>
        /// <returns>All tiers of this instance.</returns>
        IEnumerable<ITierInfo> GetInfoTiers();
        /// <summary>
        /// Returns the elevators connected to this tier.
        /// </summary>
        /// <returns>All elevators connected to this tier.</returns>
        IEnumerable<IElevatorInfo> GetInfoElevators();
        /// <summary>
        /// Indicates whether anything has changed in the instance.
        /// </summary>
        /// <returns><code>false</code> if nothing changed since the last query, <code>true</code> otherwise.</returns>
        bool GetInfoChanged();
        /// <summary>
        /// Returns all item descriptions used in the instance.
        /// </summary>
        /// <returns>All item descriptions used by the instance.</returns>
        IEnumerable<IItemDescriptionInfo> GetInfoItemDescriptions();
        /// <summary>
        /// Returns the item manager of this instance.
        /// </summary>
        /// <returns>The item manager of this instance.</returns>
        IItemManagerInfo GetInfoItemManager();
        /// <summary>
        /// Returns the count of items handled by the system.
        /// </summary>
        /// <returns>The number of items handled.</returns>
        int GetInfoStatItemsHandled();
        /// <summary>
        /// Returns the count of bundles handled by the system.
        /// </summary>
        /// <returns>The number of bundles handled.</returns>
        int GetInfoStatBundlesHandled();
        /// <summary>
        /// Returns the count of orders handled by the system.
        /// </summary>
        /// <returns>The number of orders handled.</returns>
        int GetInfoStatOrdersHandled();
        /// <summary>
        /// Returns the count of orders handled that were not completed in time.
        /// </summary>
        /// <returns>The number of orders not completed in time.</returns>
        int GetInfoStatOrdersLate();
        /// <summary>
        /// Returns the count of repositioning moves started so far.
        /// </summary>
        /// <returns>The number of repositioning moves started.</returns>
        int GetInfoStatRepositioningMoves();
        /// <summary>
        /// Returns the count of occurred collisions.
        /// </summary>
        /// <returns>The number of occurred collisions.</returns>
        int GetInfoStatCollisions();
        /// <summary>
        /// Returns the storage fill level.
        /// </summary>
        /// <returns>The storage fill level.</returns>
        double GetInfoStatStorageFillLevel();
        /// <summary>
        /// Returns the storage fill level including the already present reservations.
        /// </summary>
        /// <returns>The storage fill level.</returns>
        double GetInfoStatStorageFillAndReservedLevel();
        /// <summary>
        /// Returns the storage fill level including the already present reservations and the capacity consumed by backlog bundles.
        /// </summary>
        /// <returns>The storage fill level.</returns>
        double GetInfoStatStorageFillAndReservedAndBacklogLevel();
        /// <summary>
        /// Returns the current name of the instance.
        /// </summary>
        /// <returns>The name of the instance.</returns>
        string GetInfoName();
    }
}
