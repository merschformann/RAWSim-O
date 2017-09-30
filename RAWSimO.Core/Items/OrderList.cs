using RAWSimO.Core.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Items
{
    /// <summary>
    /// Defines a list of orders and bundles that can be used to feed an offline generated list to the simulation instead of generating them online.
    /// </summary>
    public class OrderList
    {
        /// <summary>
        /// Creates a new order list.
        /// </summary>
        /// <param name="type">The type of the items in the list.</param>
        internal OrderList(ItemType type) { Type = type; ItemDescriptions = new List<ItemDescription>(); Orders = new List<Order>(); Bundles = new List<ItemBundle>(); }

        /// <summary>
        /// The type of the items in this order list.
        /// </summary>
        public ItemType Type { get; private set; }

        /// <summary>
        /// All item-descriptions of all orders in this order list.
        /// </summary>
        public List<ItemDescription> ItemDescriptions { get; private set; }

        /// <summary>
        /// All orders of this order list sorted by their placement timestamp.
        /// </summary>
        public List<Order> Orders { get; private set; }

        /// <summary>
        /// All item-bundles of this list sorted by their placement timestamp.
        /// </summary>
        public List<ItemBundle> Bundles { get; private set; }

        /// <summary>
        /// Sorts all orders by their placement timestamp. This can be used after generating a list of orders randomly.
        /// </summary>
        public void Sort() { Orders = Orders.OrderBy(o => o.TimeStamp).ToList(); Bundles = Bundles.OrderBy(b => b.TimeStamp).ToList(); }

        /// <summary>
        /// Gets a name for the order list based on the information in the list.
        /// </summary>
        /// <returns>A string describing the list using the type of the items, the number of item descriptions, the number of bundles, the number of orders and the absolute time-horizon.</returns>
        public string GetMetaInfoBasedOrderListName()
        {
            string delimiter = "-";
            return
                Type.ToString() + delimiter +
                ItemDescriptions.Count.ToString() + delimiter +
                Bundles.Count.ToString() + delimiter +
                Orders.Count.ToString() + delimiter +
                Math.Abs((Orders.Max(o => o.TimeStamp) - Orders.Min(o => o.TimeStamp))).ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER);
        }
    }
}
