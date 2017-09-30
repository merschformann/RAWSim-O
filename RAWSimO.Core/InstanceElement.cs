using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core
{
    /// <summary>
    /// An element that belongs to a specific simulation instance.
    /// </summary>
    public abstract class InstanceElement
    {
        #region Constructor

        /// <summary>
        /// Creates a new instance element.
        /// </summary>
        /// <param name="instance">The instance this element belongs to.</param>
        public InstanceElement(Instance instance) { Instance = instance; }

        #endregion

        #region Core

        /// <summary>
        /// The ID of this element. This is also used to build simple identifying names of the objects.
        /// </summary>
        public int ID;

        /// <summary>
        /// The instance this element is assigned to.
        /// </summary>
        public Instance Instance;

        /// <summary>
        /// Used for fast access of the item. Marks the position of the item in a corresponding array of all possible items.
        /// </summary>
        public int VolatileID;

        #endregion

        #region Information supply

        /// <summary>
        /// Returns the ID of the object.
        /// </summary>
        /// <returns>The ID.</returns>
        public int GetInfoID() { return this.ID; }

        #endregion

        #region Basic method definitions

        /// <summary>
        /// Returns a simple string identifying this object in its instance.
        /// </summary>
        /// <returns>A simple name identifying the instance element.</returns>
        public abstract string GetIdentfierString();

        #endregion
    }
}
