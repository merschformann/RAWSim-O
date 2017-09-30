using RAWSimO.Core.Elements;
using RAWSimO.Core.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Items
{
    /// <summary>
    /// Implements a basic item bundle.
    /// </summary>
    public abstract class ItemBundle : InstanceElement, IItemBundleInfo
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance of an item bundle.
        /// </summary>
        /// <param name="instance">The instance this bundle belongs to.</param>
        internal ItemBundle(Instance instance) : base(instance) { TimeStampSubmit = double.PositiveInfinity; }

        #endregion

        /// <summary>
        /// The weight of this bundle.
        /// </summary>
        public double BundleWeight { get { return ItemCount * ItemDescription.Weight; } }

        /// <summary>
        /// The item-description of this physical item.
        /// </summary>
        public ItemDescription ItemDescription;

        /// <summary>
        /// The time this bundle is available.
        /// </summary>
        public double TimeStamp { get; set; }
        /// <summary>
        /// The time stamp this order was submitted to an input-station.
        /// </summary>
        public double TimeStampSubmit { get; set; }

        /// <summary>
        /// The number of items currently contained in this bundle.
        /// </summary>
        public int ItemCount { get; set; }

        /// <summary>
        /// The pod this bundle is allocated to.
        /// </summary>
        public Pod Pod { get; set; }

        #region Inherited methods

        /// <summary>
        /// Returns a simple string identifying this object in its instance.
        /// </summary>
        /// <returns>A simple name identifying the instance element.</returns>
        public override string GetIdentfierString() { return "ItemBundle" + ID; }
        /// <summary>
        /// Returns a simple string giving information about the object.
        /// </summary>
        /// <returns>A simple string.</returns>
        public override string ToString() { return "ItemBundle" + ID; }

        #endregion

        #region IItemBundleInfo Members

        /// <summary>
        /// The weight of the complete bundle.
        /// </summary>
        /// <returns>The weight of the bundle.</returns>
        public double GetInfoBundleWeight() { return BundleWeight; }
        /// <summary>
        /// The number of items contained in this bundle.
        /// </summary>
        /// <returns>The number of items contained.</returns>
        public int GetInfoItemCount() { return ItemCount; }
        /// <summary>
        /// The corresponding item description.
        /// </summary>
        /// <returns>The corresponding item description.</returns>
        public IItemDescriptionInfo GetInfoItemDescription() { return ItemDescription; }

        #endregion
    }
}
