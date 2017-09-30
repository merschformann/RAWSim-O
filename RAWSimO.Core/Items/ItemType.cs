using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Items
{
    /// <summary>
    /// The different item types that are supported by this framework.
    /// </summary>
    public enum ItemType
    {
        /// <summary>
        /// A simple item only identified by its ID. (Coloring is only used to better visually distinguish multiple items)
        /// </summary>
        SimpleItem,
        /// <summary>
        /// An item that is described by a combination of a letter and a color.
        /// </summary>
        Letter,
    }
}
