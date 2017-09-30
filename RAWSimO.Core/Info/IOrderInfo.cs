using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Info
{
    /// <summary>
    /// The interface that handles communication with order-objects.
    /// </summary>
    public interface IOrderInfo
    {
        /// <summary>
        /// Gets all positions of the order.
        /// </summary>
        /// <returns>An enumeration of all item-description in this order.</returns>
        IEnumerable<IItemDescriptionInfo> GetInfoPositions();
        /// <summary>
        /// Gets the given position's quantity.
        /// </summary>
        /// <param name="item">The position.</param>
        /// <returns>The quantity of the position.</returns>
        int GetInfoDemandCount(IItemDescriptionInfo item);
        /// <summary>
        /// Gets the given position's already served quantity.
        /// </summary>
        /// <param name="item">The position.</param>
        /// <returns>The already served quantity of the position.</returns>
        int GetInfoServedCount(IItemDescriptionInfo item);
        /// <summary>
        /// Indicates whether the order is already completed.
        /// </summary>
        /// <returns><code>true</code> if the order is completed, <code>false</code> otherwise.</returns>
        bool GetInfoIsCompleted();
    }
}
