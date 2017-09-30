using RAWSimO.Core.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RAWSimO.Core.IO
{
    /// <summary>
    /// A simplified representation of the original object used for serialization.
    /// </summary>
    [XmlRootAttribute("OrderList")]
    public class DTOOrderList : IDataTransferObject<OrderList, DTOOrderList>
    {
        /// <summary>
        /// The type of the items of this list.
        /// </summary>
        [XmlAttribute]
        public string Type;
        /// <summary>
        /// All item descriptions used in this list.
        /// </summary>
        [XmlArrayItem("ItemDescription")]
        public List<DTOItemDescription> ItemDescriptions;
        /// <summary>
        /// All bundles of this list.
        /// </summary>
        [XmlArrayItem("ItemBundle")]
        public List<DTOItemBundle> ItemBundles;
        /// <summary>
        /// All orders of this list.
        /// </summary>
        [XmlArrayItem("Order")]
        public List<DTOOrder> Orders;
        /// <summary>
        /// Creates a DTO representation of the original object.
        /// </summary>
        /// <param name="value">The original object.</param>
        public static implicit operator DTOOrderList(OrderList value)
        {
            if (value == null)
                return null;
            DTOOrderList orderList = new DTOOrderList() { Type = value.Type.ToString() };
            orderList.ItemDescriptions = new List<DTOItemDescription>();
            foreach (var item in value.ItemDescriptions)
                orderList.ItemDescriptions.Add(item);
            orderList.ItemBundles = new List<DTOItemBundle>();
            foreach (var bundle in value.Bundles)
                orderList.ItemBundles.Add(bundle);
            orderList.Orders = new List<DTOOrder>();
            foreach (var order in value.Orders)
                orderList.Orders.Add(order);
            return orderList;
        }

        #region IDataTransferObject<OrderList,DTOOrderList> Members

        /// <summary>
        /// Creates a clone out of the original.
        /// </summary>
        /// <param name="original">The original object.</param>
        /// <returns>The cloned object.</returns>
        public DTOOrderList FromOrig(OrderList original) { return original; }
        /// <summary>
        /// Submits all this object to the instance.
        /// </summary>
        /// <param name="instance">The instance to submit to.</param>
        /// <returns>The original object created at the instance.</returns>
        public OrderList Submit(Instance instance)
        {
            Dictionary<int, ItemDescription> itemDescriptions = new Dictionary<int, ItemDescription>();
            foreach (var dtoItemDescription in ItemDescriptions)
            {
                ItemDescription itemDescription = dtoItemDescription.Submit(instance);
                itemDescriptions[dtoItemDescription.ID] = itemDescription;
            }
            OrderList list = instance.CreateOrderList(itemDescriptions.First().Value.Type);
            list.ItemDescriptions.AddRange(itemDescriptions.Values.OrderBy(i => i.ID));
            foreach (var dtoBundle in ItemBundles.OrderBy(b => b.TimeStamp))
                dtoBundle.Submit(instance);
            foreach (var dtoOrder in Orders.OrderBy(o => o.TimeStamp))
                dtoOrder.Submit(instance);
            return list;
        }

        #endregion
    }

    /// <summary>
    /// A DTO representation of an order.
    /// </summary>
    [XmlRootAttribute("Order")]
    public class DTOOrder : IDataTransferObject<Order, DTOOrder>
    {
        /// <summary>
        /// The time at which this order becomes active.
        /// </summary>
        [XmlAttribute]
        public double TimeStamp;
        /// <summary>
        /// The lines of this order.
        /// </summary>
        [XmlArrayItem("Position")]
        public List<DTOOrderPosition> Positions;
        /// <summary>
        /// Creates a DTO representation of the original object.
        /// </summary>
        /// <param name="value">The original object.</param>
        public static implicit operator DTOOrder(Order value)
        {
            if (value == null)
                return null;
            DTOOrder dtoOrder = new DTOOrder() { TimeStamp = value.TimeStamp };
            dtoOrder.Positions = new List<DTOOrderPosition>();
            foreach (var position in value.Positions)
                dtoOrder.Positions.Add(position);
            return dtoOrder;
        }

        #region IDataTransferObject<Order,DTOOrder> Members

        /// <summary>
        /// Creates a clone out of the original.
        /// </summary>
        /// <param name="original">The original object.</param>
        /// <returns>The cloned object.</returns>
        public DTOOrder FromOrig(Order original) { return original; }
        /// <summary>
        /// Submits all this object to the instance.
        /// </summary>
        /// <param name="instance">The instance to submit to.</param>
        /// <returns>The original object created at the instance.</returns>
        public Order Submit(Instance instance)
        {
            Order order = new Order();
            order.TimeStamp = TimeStamp;
            foreach (var position in Positions)
                position.Submit(order, instance);
            instance.OrderList.Orders.Add(order);
            return order;
        }

        #endregion
    }

    /// <summary>
    /// A DTO representation of one order line.
    /// </summary>
    [XmlRootAttribute("Position")]
    public class DTOOrderPosition
    {
        /// <summary>
        /// The ID of the item description of this order line.
        /// </summary>
        [XmlAttribute]
        public int ItemDescriptionID;
        /// <summary>
        /// The quantity of this order line.
        /// </summary>
        [XmlAttribute]
        public int Count;
        /// <summary>
        /// Creates a DTO representation of the original object.
        /// </summary>
        /// <param name="value">The original object.</param>
        public static implicit operator DTOOrderPosition(KeyValuePair<ItemDescription, int> value)
        {
            return new DTOOrderPosition() { Count = value.Value, ItemDescriptionID = value.Key.ID };
        }
        /// <summary>
        /// Submits all this object to the instance.
        /// </summary>
        /// <param name="instance">The instance to submit to.</param>
        /// <param name="order">The order to submit to.</param>
        /// <returns>The original object created at the instance.</returns>
        public void Submit(Order order, Instance instance)
        {
            order.AddPosition(instance.GetItemDescriptionByID(ItemDescriptionID), Count);
        }
    }
    /// <summary>
    /// A DTO representation of an item description.
    /// </summary>
    [XmlRootAttribute("ItemDescription")]
    public class DTOItemDescription : IDataTransferObject<ItemDescription, DTOItemDescription>
    {
        /// <summary>
        /// The ID of this element.
        /// </summary>
        [XmlAttribute]
        public int ID;
        /// <summary>
        /// The type of the item.
        /// </summary>
        [XmlAttribute]
        public string Type;
        /// <summary>
        /// The weight of one unit of this item.
        /// </summary>
        [XmlAttribute]
        public double Weight;
        /// <summary>
        /// The letter of this item.
        /// </summary>
        [XmlAttribute]
        public char Letter;
        /// <summary>
        /// The color of this item.
        /// </summary>
        [XmlAttribute]
        public string Color;
        /// <summary>
        /// Creates a DTO representation of the original object.
        /// </summary>
        /// <param name="value">The original object.</param>
        public static implicit operator DTOItemDescription(ItemDescription value)
        {
            return value == null ? null : new DTOItemDescription
            {
                ID = value.ID,
                Type = value.Type.ToString(),
                Weight = value.Weight,
                Letter = (value is ColoredLetterDescription) ? (value as ColoredLetterDescription).Letter : ' ',
                Color = (value is ColoredLetterDescription) ? (value as ColoredLetterDescription).Color.ToString() : ""
            };
        }

        #region IDataTransferObject<ItemDescription,DTOItemDescription> Members

        /// <summary>
        /// Creates a clone out of the original.
        /// </summary>
        /// <param name="original">The original object.</param>
        /// <returns>The cloned object.</returns>
        public DTOItemDescription FromOrig(ItemDescription original) { return original; }
        /// <summary>
        /// Submits all this object to the instance.
        /// </summary>
        /// <param name="instance">The instance to submit to.</param>
        /// <returns>The original object created at the instance.</returns>
        public ItemDescription Submit(Instance instance)
        {
            ItemDescription itemDescription = instance.CreateItemDescription(ID, (ItemType)Enum.Parse(typeof(ItemType), Type));
            itemDescription.Weight = Weight;
            if (itemDescription.Type == ItemType.Letter)
            {
                (itemDescription as ColoredLetterDescription).Color = (LetterColors)Enum.Parse(typeof(LetterColors), Color);
                (itemDescription as ColoredLetterDescription).Letter = Letter;
            }
            return itemDescription;
        }

        #endregion
    }
    /// <summary>
    /// A DTO representation of a bundle.
    /// </summary>
    [XmlRootAttribute("ItemBundle")]
    public class DTOItemBundle : IDataTransferObject<ItemBundle, DTOItemBundle>
    {
        /// <summary>
        /// The time at which this bundle becomes active.
        /// </summary>
        [XmlAttribute]
        public double TimeStamp;
        /// <summary>
        /// The item description of this bundle.
        /// </summary>
        [XmlAttribute]
        public int ItemDescription;
        /// <summary>
        /// The size of this bundle.
        /// </summary>
        [XmlAttribute]
        public int Size;
        /// <summary>
        /// Creates a DTO representation of the original object.
        /// </summary>
        /// <param name="value">The original object.</param>
        public static implicit operator DTOItemBundle(ItemBundle value)
        {
            return value == null ? null : new DTOItemBundle
            {
                TimeStamp = value.TimeStamp,
                ItemDescription = value.ItemDescription.ID,
                Size = value.ItemCount
            };
        }

        #region IDataTransferObject<ItemBundle,DTOItemBundle> Members

        /// <summary>
        /// Creates a clone out of the original.
        /// </summary>
        /// <param name="original">The original object.</param>
        /// <returns>The cloned object.</returns>
        public DTOItemBundle FromOrig(ItemBundle original) { return original; }
        /// <summary>
        /// Submits all this object to the instance.
        /// </summary>
        /// <param name="instance">The instance to submit to.</param>
        /// <returns>The original object created at the instance.</returns>
        public ItemBundle Submit(Instance instance)
        {
            ItemBundle bundle = instance.CreateItemBundle(instance.GetItemDescriptionByID(ItemDescription), Size);
            bundle.TimeStamp = TimeStamp;
            instance.OrderList.Bundles.Add(bundle);
            return bundle;
        }

        #endregion
    }
}
