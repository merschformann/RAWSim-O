using RAWSimO.MultiAgentPathFinding.Algorithms.AStar;
using RAWSimO.MultiAgentPathFinding.Algorithms.ID;
using RAWSimO.MultiAgentPathFinding.Algorithms.OD;
using RAWSimO.MultiAgentPathFinding.DataStructures;
using RAWSimO.MultiAgentPathFinding.Elements;
using RAWSimO.MultiAgentPathFinding.Physic;
using RAWSimO.MultiAgentPathFinding.Toolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MultiAgentPathFinding.Methods
{

    /// <summary>
    /// Conflict-based for optimal multi-agent pathﬁnding, Sharon 2015
    /// </summary>
    public class ODIDMethod : PathFinder
    {
        /// <summary>
        /// The length of a wait step
        /// </summary>
        public double LengthOfAWindow = 15.0;

        /// <summary>
        /// Use Final Reservations
        /// </summary>
        public bool UseFinalReservations = false;

        /// <summary>
        /// Maximum number of nodes to be generated
        /// </summary>
        public int MaxNodeCountPerAgent = 100;

        /// <summary>
        /// The _independence detection
        /// </summary>
        private IndependenceDetection _independenceDetection;

        /// <summary>
        /// Initializes a new instance of the <see cref="FARMethod"/> class.
        /// </summary>
        /// <param name="graph">graph</param>
        /// <param name="seed">The seed to use for the randomizer.</param>
        /// <param name="logger">The logger to use.</param>
        public ODIDMethod(Graph graph, int seed, PathPlanningCommunicator logger)
            : base(graph, seed, logger)
        {
            if (graph.BackwardEdges == null)
                graph.GenerateBackwardEgdes();

            //create Algorithms
            _independenceDetection = new IndependenceDetection(graph, seed, null);
        }

        /// <summary>
        /// Find the path for all the agents.
        /// </summary>
        /// <param name="currentTime">The current Time.</param>
        /// <param name="agents">agents</param>
        /// <param name="obstacleNodes">The way points of the obstacles.</param>
        /// <param name="lockedNodes"></param>
        /// <param name="nextReoptimization">The next re-optimization time.</param>
        public override void FindPaths(double currentTime, List<Agent> agents)
        {
            _independenceDetection.LowLevSolver = _independenceDetection.LowLevSolver ?? new OperatorDecomposition(Graph, LengthOfAWaitStep, LengthOfAWindow, MaxNodeCountPerAgent, UseFinalReservations);

            //find path
            _independenceDetection.FindPaths(currentTime, agents, Math.Min(RuntimeLimitPerAgent * agents.Count, RunTimeLimitOverall), Communicator);
        }

    }
}
