using RAWSimO.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Info
{
    /// <summary>
    /// The interface for supplying information about an item managere object.
    /// </summary>
    public interface IItemManagerInfo : IUpdateable
    {
        /// <summary>
        /// Gets the number of currently available orders that are not yet allocated.
        /// </summary>
        /// <returns>The number of currently available orders that are not yet allocated.</returns>
        int GetInfoPendingOrderCount();
        /// <summary>
        /// Gets the number of currently available bundles that are not yet allocated.
        /// </summary>
        /// <returns>The number of currently available bundles that are not yet allocated.</returns>
        int GetInfoPendingBundleCount();
        /// <summary>
        /// Gets an enumeration of the currently pending orders. Hence, all orders not yet assigned to any station.
        /// </summary>
        /// <returns>The orders currently pending.</returns>
        IEnumerable<IOrderInfo> GetInfoPendingOrders();
        /// <summary>
        /// Gets an enumeration of the currently open orders. This are all orders currently assigned to a station.
        /// </summary>
        /// <returns>The orders currently open.</returns>
        IEnumerable<IOrderInfo> GetInfoOpenOrders();
        /// <summary>
        /// Gets an enumeration of the already completed orders.
        /// </summary>
        /// <returns>The orders already completed.</returns>
        IEnumerable<IOrderInfo> GetInfoCompletedOrders();
    }
}
