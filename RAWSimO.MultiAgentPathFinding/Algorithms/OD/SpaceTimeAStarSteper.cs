using RAWSimO.MultiAgentPathFinding.Algorithms.AStar;
using RAWSimO.MultiAgentPathFinding.DataStructures;
using RAWSimO.MultiAgentPathFinding.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MultiAgentPathFinding.Algorithms.OD
{
    /// <summary>
    /// Space Time A Star which can trigger each step individually
    /// </summary>
    public class SpaceTimeAStarSteper : SpaceTimeAStar
    {

        /// <summary>
        /// Gets or sets the reservation table.
        /// </summary>
        /// <value>
        /// The reservation table.
        /// </value>
        public ReservationTable ReservationTable
        {
            get { return _reservationTable; }
            set { _reservationTable = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpaceTimeAStar"/> class.
        /// </summary>
        public SpaceTimeAStarSteper(Graph graph, double lengthOfAWaitStep, Agent agent, ReverseResumableAStar rraStar)
            : base(graph, lengthOfAWaitStep, 0, null, agent, rraStar, false)
        {
            //we don't need the AStar Base things
            Q = null;
            Open = null;
            Closed = null;
        }


        /// <summary>
        /// Step for the specified node identifier.
        /// </summary>
        /// <param name="nodeId">The node identifier.</param>
        public IEnumerable<int> Step(int nodeId)
        {
            //get successor
            return Successors(nodeId);
        }

        /// <summary>
        /// Executes the search.
        /// </summary>
        /// <returns>
        /// found node
        /// </returns>
        /// <exception cref="System.NotSupportedException"></exception>
        public override bool Search()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        internal void Clear()
        {
            StartNode = 0;
            GoalNode = -1;

            //built mappings
            NodeTime.Clear();
            NodeBackpointerId.Clear();
            NodeBackpointerLastStopId.Clear();
            NodeBackpointerEdge.Clear();
            NodeTimeTemp.Clear();
            NodeBackpointerIdTemp.Clear();
            NodeBackpointerLastTurnIdTemp.Clear();
            NodeBackpointerEdgeTemp.Clear();
            _numNodeId = 0;

            StartAngle = Graph.RadToDegree(_agent.OrientationAtNextNode);

            NodeTime.Add(_agent.ArrivalTimeAtNextNode);
            NodeBackpointerId.Add(-1);
            NodeBackpointerLastStopId.Add(0);
            NodeBackpointerEdge.Add(null);
            _numNodeId++;

            _init = true;

            if (_tieBreaking)
            {
                _tieBreakingTimes = new double[_graph.NodeCount];

                for (var i = 0; i < _tieBreakingTimes.Length; i++)
                    _tieBreakingTimes[i] = -1.0;
            }
        }
    }
}
