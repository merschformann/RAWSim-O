using RAWSimO.MultiAgentPathFinding.DataStructures;
using RAWSimO.MultiAgentPathFinding.Physic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MultiAgentPathFinding.Elements
{
    /// <summary>
    /// The Agent has a start node and a destination node
    /// </summary>
    public class Agent
    {

        /// <summary>
        /// The identifier
        /// </summary>
        public int ID;

        /// <summary>
        /// next node
        /// </summary>
        public int NextNode;

        /// <summary>
        /// passed nodes to next node
        /// </summary>
        public List<ReservationTable.Interval> ReservationsToNextNode;

        /// <summary>
        /// Time of arrival at next node
        /// </summary>
        public double ArrivalTimeAtNextNode;

        /// <summary>
        /// Start orientation.
        /// </summary>
        public double OrientationAtNextNode;

        /// <summary>
        /// Destination node.
        /// </summary>
        public int DestinationNode;

        /// <summary>
        /// The final destination of the agent. This might differ from the destination node in case the bot is send to an area managed by a queue.
        /// </summary>
        public int FinalDestinationNode;

        /// <summary>
        /// Agent has a fixed position.
        /// </summary>
        public bool FixedPosition;

        /// <summary>
        /// Agent is resting.
        /// </summary>
        public bool Resting;

        /// <summary>
        /// Agent can go throw obstacles.
        /// </summary>
        public bool CanGoThroughObstacles;

        /// <summary>
        /// Agent has a physics class.
        /// </summary>
        public Physics Physics;

        /// <summary>
        /// The calculated path of the agent
        /// </summary>
        public Path Path;

        /// <summary>
        /// The agent requests a re optimization
        /// </summary>
        public bool RequestReoptimization;

        /// <summary>
        /// Indicates whether the bot is currently queueing.
        /// </summary>
        public bool Queueing;

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "Agent" + this.ID;
        }

        #region Debug fields

        /// <summary>
        /// The destination object of the bot.
        /// </summary>
        public object DestinationNodeObject;
        /// <summary>
        /// The next node object of the bot.
        /// </summary>
        public object NextNodeObject;

        #endregion
    }
}
