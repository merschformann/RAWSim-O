using RAWSimO.MultiAgentPathFinding.Algorithms.AStar;
using RAWSimO.MultiAgentPathFinding.DataStructures;
using RAWSimO.MultiAgentPathFinding.Elements;
using RAWSimO.MultiAgentPathFinding.Physic;
using RAWSimO.MultiAgentPathFinding.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MultiAgentPathFinding.Methods
{
    /// <summary>
    /// Flow Annotation Re-planning by Ko-Hsin Cindy Wang and Adi Botea 2008
    /// </summary>
    public class DummyMethod : PathFinder
    {
        /// <summary>
        /// The deadlock handler
        /// </summary>
        private DeadlockHandler _deadlockHandler;

        /// <summary>
        /// The _blocked
        /// </summary>
        private HashSet<int> _blocked;

        /// <summary>
        /// Initializes a new instance of the <see cref="FARMethod"/> class.
        /// </summary>
        /// <param name="graph">graph</param>
        /// <param name="seed">The seed to use for the randomizer.</param>
        /// <param name="logger">The logger to use.</param>
        public DummyMethod(Graph graph, int seed, PathPlanningCommunicator logger)
            : base(graph, seed, logger)
        {
            if (graph.BackwardEdges == null)
                graph.GenerateBackwardEgdes();
            _deadlockHandler = new DeadlockHandler(graph, seed);
            _blocked = new HashSet<int>();
        }

        public override void FindPaths(double currentTime, List<Agent> agents)
        {
            //randomHop
            _deadlockHandler.LengthOfAWaitStep = LengthOfAWaitStep;
            foreach (var agent in agents.Where(a => !a.FixedPosition))
                _deadlockHandler.RandomHop(agent);
        }
    }
}
