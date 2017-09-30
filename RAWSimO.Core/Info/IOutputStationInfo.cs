using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Info
{
    /// <summary>
    /// The interface for supplying information about an output-station object.
    /// </summary>
    public interface IOutputStationInfo : IImmovableObjectInfo
    {
        /// <summary>
        /// Gets the number of assigned orders.
        /// </summary>
        /// <returns>The number of assigned orders.</returns>
        int GetInfoAssignedOrders();
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
        /// Gets all order currently open.
        /// </summary>
        /// <returns>The enumeration of open orders.</returns>
        IEnumerable<IOrderInfo> GetInfoOpenOrders();
        /// <summary>
        /// Gets all orders already completed.
        /// </summary>
        /// <returns>The enumeration of completed orders.</returns>
        IEnumerable<IOrderInfo> GetInfoCompletedOrders();
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
        /// Gets the number of requests currently open (not assigned to a bot) for this station.
        /// </summary>
        /// <returns>The number of active requests.</returns>
        int GetInfoOpenRequests();
        /// <summary>
        /// Gets the number of queued requests currently open (not assigned to a bot) for this station.
        /// </summary>
        /// <returns>The number of active queued requests.</returns>
        int GetInfoOpenQueuedRequests();
        /// <summary>
        /// Gets the number of currently open items (not yet picked) for this station.
        /// </summary>
        /// <returns>The number of open items.</returns>
        int GetInfoOpenItems();
        /// <summary>
        /// Gets the number of currently queued and open items (not yet picked) for this station.
        /// </summary>
        /// <returns>The number of open queued items.</returns>
        int GetInfoOpenQueuedItems();
        /// <summary>
        /// Gets the number of pods currently incoming to this station.
        /// </summary>
        /// <returns>The number of pods currently incoming to this station.</returns>
        int GetInfoInboundPods();
    }
}
