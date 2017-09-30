using RAWSimO.Core.Info;
using RAWSimO.Core.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Items
{
    /// <summary>
    /// Implements the item description (SKU) for the item type simple.
    /// </summary>
    public class SimpleItemDescription : ItemDescription, ISimpleItemDescriptionInfo
    {
        #region Constructor

        /// <summary>
        /// Instantiates this class.
        /// </summary>
        /// <param name="instance">The instance this element belongs to.</param>
        internal SimpleItemDescription(Instance instance) : base(instance) { }

        #endregion

        #region Core

        /// <summary>
        /// The ID used to identify the item. (Same as ID set by the instance)
        /// </summary>
        public int ItemID { get { return ID; } }

        /// <summary>
        /// The color of this item (used to better distinguish multiple item types).
        /// </summary>
        public double Hue;

        #endregion

        #region Inherited methods

        /// <summary>
        /// The type of the item.
        /// </summary>
        public override ItemType Type { get { return ItemType.SimpleItem; } }
        /// <summary>
        /// Returns a simple string identifying this object in its instance.
        /// </summary>
        /// <returns>A simple name identifying the instance element.</returns>
        public override string GetIdentfierString() { return "SimpleItemDescription" + ID; }
        /// <summary>
        /// Returns a simple string giving information about the object.
        /// </summary>
        /// <returns>A simple string.</returns>
        public override string ToString() { return "SimpleItemDescription" + ID; }
        /// <summary>
        /// Returns a string describing the item behind this type.
        /// </summary>
        /// <returns>A descriptive string.</returns>
        public override string ToDescriptiveString() { return "(" + ID + ")"; }

        #endregion

        #region ISimpleItemDescriptionInfo Members

        /// <summary>
        /// The color of this item (used to better distinguish the items - does not have any additional meaning).
        /// </summary>
        /// <returns>The color.</returns>
        public double GetInfoHue() { return Hue; }

        #endregion
    }
}
