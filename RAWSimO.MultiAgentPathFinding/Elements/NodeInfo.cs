using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MultiAgentPathFinding.Elements
{
    /// <summary>
    /// Contains information about a node resembling a waypoint.
    /// </summary>
    public class NodeInfo
    {
        /// <summary>
        /// The ID of the node the info belongs to.
        /// </summary>
        public int ID;
        /// <summary>
        /// Indicates whether the node is currently locked.
        /// </summary>
        public bool IsLocked = false;
        /// <summary>
        /// Indicates whether the node is occupied by an obstacle.
        /// </summary>
        public bool IsObstacle = false;
        /// <summary>
        /// Indicates whether the node is part of a queue.
        /// </summary>
        public bool IsQueue = false;
        /// <summary>
        /// If the node is part of a queue, this field contains the id of the terminal / destination node of the queue.
        /// </summary>
        public int QueueTerminal = -1;
    }
}
