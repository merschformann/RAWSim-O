using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Interfaces
{
    /// <summary>
    /// Owner of a queue.
    /// </summary>
    public interface IQueuesOwner
    {
        /// <summary>
        /// The Queue starting with the way point of the object ending with the most far away one.
        /// The first argument is the way point that has a queue and the second one is the List with way points
        /// </summary>
        /// <value>
        /// The queue.
        /// </value>
        Dictionary<Waypoint, List<Waypoint>> Queues { get; set; }
    }
}
