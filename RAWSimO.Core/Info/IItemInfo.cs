using RAWSimO.Core.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Info
{
    /// <summary>
    /// The interface for getting information about an item description object.
    /// </summary>
    public interface IItemDescriptionInfo : IIdentifiableObjectInfo
    {
        /// <summary>
        /// Gets the weight of one item of this type.
        /// </summary>
        /// <returns>The weight of the item.</returns>
        double GetInfoWeight();
        /// <summary>
        /// The type of the item.
        /// </summary>
        ItemType GetInfoType();
        /// <summary>
        /// Gets a string representation of the item.
        /// </summary>
        /// <returns>A string representing the item.</returns>
        string GetInfoDescription();
    }
    /// <summary>
    /// The interface for getting information about a colored letter description object.
    /// </summary>
    public interface IColoredLetterDescriptionInfo : IItemDescriptionInfo
    {
        /// <summary>
        /// The color of this letter.
        /// </summary>
        /// <returns>The color.</returns>
        LetterColors GetInfoColor();
        /// <summary>
        /// The letter of this colored letter.
        /// </summary>
        /// <returns>The character describing the letter.</returns>
        char GetInfoLetter();
    }
    /// <summary>
    /// The interface for getting information about simple item description object.
    /// </summary>
    public interface ISimpleItemDescriptionInfo : IItemDescriptionInfo
    {
        /// <summary>
        /// The color of this item (used to better distinguish the items - does not have any additional meaning).
        /// </summary>
        /// <returns>The color.</returns>
        double GetInfoHue();
    }
    /// <summary>
    /// The interface for getting information about an item bundle object.
    /// </summary>
    public interface IItemBundleInfo
    {
        /// <summary>
        /// The weight of the complete bundle.
        /// </summary>
        /// <returns>The weight of the bundle.</returns>
        double GetInfoBundleWeight();
        /// <summary>
        /// The number of items contained in this bundle.
        /// </summary>
        /// <returns>The number of items contained.</returns>
        int GetInfoItemCount();
        /// <summary>
        /// The corresponding item description.
        /// </summary>
        /// <returns>The corresponding item description.</returns>
        IItemDescriptionInfo GetInfoItemDescription();
    }
}
