using RAWSimO.Core.Info;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Items
{
    /// <summary>
    /// Implements a basic item description, i.e. SKU.
    /// </summary>
    public abstract class ItemDescription : InstanceElement, IItemDescriptionInfo, IExposeVolatileID
    {
        #region Constructor

        /// <summary>
        /// Creates a new item-description.
        /// </summary>
        /// <param name="instance">The instance this item-description belongs to.</param>
        internal ItemDescription(Instance instance) : base(instance) { }

        #endregion

        #region Core

        /// <summary>
        /// If a value greater 1 is supplied, this defines a fixed size of the bundle to use when replenishing this product type.
        /// </summary>
        public int BundleSize = -1;

        /// <summary>
        /// Weight of the item. This defines how much capacity one instance of this item uses when put into a storage.
        /// </summary>
        public double Weight;

        /// <summary>
        /// The type of the item.
        /// </summary>
        public abstract ItemType Type { get; }

        /// <summary>
        /// Returns a string describing the item behind this type.
        /// </summary>
        /// <returns>A descriptive string.</returns>
        public abstract string ToDescriptiveString();

        #endregion

        #region Statistics

        /// <summary>
        /// The number of times this item was ordered.
        /// </summary>
        public int OrderCount { get { return Instance.FrequencyTracker.GetModifiableOrderCount(this); } }

        #endregion

        #region Inherited methods

        /// <summary>
        /// Returns a simple string identifying this object in its instance.
        /// </summary>
        /// <returns>A simple name identifying the instance element.</returns>
        public override string GetIdentfierString() { return "ItemDescription" + this.ID; }
        /// <summary>
        /// Returns a simple string giving information about the object.
        /// </summary>
        /// <returns>A simple string.</returns>
        public override string ToString() { return "ItemDescription" + this.ID; }

        #endregion

        #region IItemDescriptionInfo Members

        /// <summary>
        /// Gets the weight of one item of this type.
        /// </summary>
        /// <returns>The weight of the item.</returns>
        public double GetInfoWeight() { return Weight; }
        /// <summary>
        /// The type of the item.
        /// </summary>
        public ItemType GetInfoType() { return Type; }
        /// <summary>
        /// Gets a string representation of the item.
        /// </summary>
        /// <returns>A string representing the item.</returns>
        public string GetInfoDescription() { return ToDescriptiveString(); }

        #endregion

        #region IExposeVolatileID

        /// <summary>
        /// An ID that is useful as an index for listing this item.
        /// This ID is unique among all <code>ItemDescription</code>s while being as low as possible.
        /// Note: For now the volatile ID matches the actual ID.
        /// </summary>
        int IExposeVolatileID.VolatileID { get { return VolatileID; } }

        #endregion
    }
}
