using RAWSimO.Core.Info;
using RAWSimO.Core.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Items
{
    /// <summary>
    /// Implements a colored letter based item description.
    /// </summary>
    public class ColoredLetterDescription : ItemDescription, IColoredLetterDescriptionInfo
    {
        #region Constructor

        /// <summary>
        /// Instantiates this class.
        /// </summary>
        /// <param name="instance">The instance this element belongs to.</param>
        internal ColoredLetterDescription(Instance instance) : base(instance) { }

        #endregion

        #region Core

        /// <summary>
        /// The letter of this colored letter.
        /// </summary>
        public char Letter;

        /// <summary>
        /// The color of this colored letter.
        /// </summary>
        public LetterColors Color;

        #endregion

        #region Inherited methods

        /// <summary>
        /// The type of the item.
        /// </summary>
        public override ItemType Type { get { return ItemType.Letter; } }
        /// <summary>
        /// Returns a simple string identifying this object in its instance.
        /// </summary>
        /// <returns>A simple name identifying the instance element.</returns>
        public override string GetIdentfierString() { return "LetterDescription" + ID; }
        /// <summary>
        /// Returns a simple string giving information about the object.
        /// </summary>
        /// <returns>A simple string.</returns>
        public override string ToString() { return "LetterDescription-(" + ID + "/" + Letter + "/" + Color + ")"; }
        /// <summary>
        /// Returns a string describing the item behind this type.
        /// </summary>
        /// <returns>A descriptive string.</returns>
        public override string ToDescriptiveString() { return "(" + Letter + "," + Color + ")"; }

        #endregion

        #region IColoredLetterDescriptionInfo Members

        /// <summary>
        /// The color of this letter.
        /// </summary>
        /// <returns>The color.</returns>
        public LetterColors GetInfoColor() { return Color; }
        /// <summary>
        /// The letter of this colored letter.
        /// </summary>
        /// <returns>The character describing the letter.</returns>
        public char GetInfoLetter() { return Letter; }

        #endregion
    }
}
