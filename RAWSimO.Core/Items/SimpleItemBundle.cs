using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Items
{
    /// <summary>
    /// Implements an item bundle of the type simple.
    /// </summary>
    public class SimpleItemBundle : ItemBundle
    {
        #region Constructor

        /// <summary>
        /// Instantiates this class.
        /// </summary>
        /// <param name="instance">The instance this element belongs to.</param>
        internal SimpleItemBundle(Instance instance) : base(instance) { }

        #endregion

        #region Inherited methods

        /// <summary>
        /// Returns a simple string identifying this object in its instance.
        /// </summary>
        /// <returns>A simple name identifying the instance element.</returns>
        public override string GetIdentfierString() { return "LetterBundle" + ID; }
        /// <summary>
        /// Returns a simple string giving information about the object.
        /// </summary>
        /// <returns>A simple string.</returns>
        public override string ToString() { return "LetterBundle-(" + ID + "/" + (ItemDescription as ColoredLetterDescription).Letter + "/" + (ItemDescription as ColoredLetterDescription).Color + ")"; }

        #endregion
    }
}
