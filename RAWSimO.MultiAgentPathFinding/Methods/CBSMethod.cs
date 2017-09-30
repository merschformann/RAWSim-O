using RAWSimO.MultiAgentPathFinding.Algorithms.AStar;
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
    public class CBSMethod : PathFinder
    {
        /// <summary>
        /// The search method for node selection
        /// </summary>
        public CBSSearchMethod SearchMethod = CBSSearchMethod.BestFirst;

        /// <summary>
        /// The reservation table for finding a way through constraints
        /// </summary>
        private ReservationTable _reservationTable;

        /// <summary>
        /// The reservation table for collision detection
        /// </summary>
        ReservationTable _agentReservationTable;

        /// <summary>
        /// Lambda Express for node selection
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        delegate double NodeSelectionExpression(ConflictTree.Node node);

        /// <summary>
        /// The deadlock handler
        /// </summary>
        private DeadlockHandler _deadlockHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="FARMethod"/> class.
        /// </summary>
        /// <param name="graph">graph</param>
        /// <param name="seed">The seed to use for the randomizer.</param>
        /// <param name="logger">The logger to use.</param>
        public CBSMethod(Graph graph, int seed, PathPlanningCommunicator logger)
            : base(graph, seed, logger)
        {
            if (graph.BackwardEdges == null)
                graph.GenerateBackwardEgdes();
            _reservationTable = new ReservationTable(graph);
            _agentReservationTable = new ReservationTable(graph, false, true, false);
            _deadlockHandler = new DeadlockHandler(graph, seed);
        }

        /// <summary>
        /// Find the path for all the agents.
        /// </summary>
        /// <param name="currentTime">The current Time.</param>
        /// <param name="agents">agents</param>
        public override void FindPaths(double currentTime, List<Agent> agents)
        {
            Stopwatch.Restart();

            //initialization data structures
            var conflictTree = new ConflictTree();
            var Open = new FibonacciHeap<double, ConflictTree.Node>();
            var solvable = true;
            var generatedNodes = 0;
            ConflictTree.Node bestNode = null;
            double bestTime = 0.0;

            //deadlock handling
            _deadlockHandler.LengthOfAWaitStep = LengthOfAWaitStep;
            _deadlockHandler.MaximumWaitTime = 30;
            _deadlockHandler.Update(agents, currentTime);

            //simply blocked
            foreach (var agent in agents.Where(a => a.FixedPosition))
                Graph.NodeInfo[agent.NextNode].IsLocked = true;

            // TODO this only works as long as a possible solution is guaranteed - maybe instead ignore paths to plan for agents with no possible solution and hope that it clears by others moving on?
            //first node initialization
            List<Agent> unsolvableAgents = null;
            foreach (var agent in agents.Where(a => !a.FixedPosition))
            {
                bool agentSolved = Solve(conflictTree.Root, currentTime, agent);
                if (!agentSolved)
                {
                    if (unsolvableAgents == null)
                        unsolvableAgents = new List<Agent>() { agent };
                    else
                        unsolvableAgents.Add(agent);
                }
                solvable = solvable && agentSolved;
            }

            //node selection strategy (Queue will pick the node with minimum value
            NodeSelectionExpression nodeObjectiveSelector = node =>
            {
                switch (SearchMethod)
                {
                    case CBSSearchMethod.BestFirst:
                        return node.SolutionCost;
                    case CBSSearchMethod.BreathFirst:
                        return node.Depth;
                    case CBSSearchMethod.DepthFirst:
                        return (-1) * node.Depth;
                    default:
                        return 0;
                }
            };

            //Enqueue first node
            if (solvable)
                Open.Enqueue(conflictTree.Root.SolutionCost, conflictTree.Root);
            else
                Communicator.LogDefault("WARNING! Aborting CBS - could not obtain an initial solution for the following agents: " +
                    string.Join(",", unsolvableAgents.Select(a => "Agent" + a.ID.ToString() + "(" + a.NextNode.ToString() + "->" + a.DestinationNode.ToString() + ")")));
            bestNode = conflictTree.Root;

            //search loop
            ConflictTree.Node p = conflictTree.Root;
            while (Open.Count > 0)
            {

                //local variables
                int agentId1;
                int agentId2;
                ReservationTable.Interval interval;

                //pop out best node
                p = Open.Dequeue().Value;

                //check the path
                var hasNoConflicts = ValidatePath(p, agents, out agentId1, out agentId2, out interval);

                //has no conflicts?
                if (hasNoConflicts)
                {
                    bestNode = p;
                    break;
                }

                // time up? => return the best solution
                if (Stopwatch.ElapsedMilliseconds / 1000.0 > RuntimeLimitPerAgent * agents.Count * 0.9 || Stopwatch.ElapsedMilliseconds / 1000.0 > RunTimeLimitOverall)
                {
                    Communicator.SignalTimeout();
                    break;
                }

                //save best node
                if (bestNode == null || interval.Start > bestTime)
                {
                    bestTime = interval.Start;
                    bestNode = p;
                }

                //append child 1
                var node1 = new ConflictTree.Node(agentId1, interval, p);
                solvable = Solve(node1, currentTime, agents.First(a => a.ID == agentId1));
                if (solvable)
                    Open.Enqueue(node1.SolutionCost, node1);

                //append child 2
                var node2 = new ConflictTree.Node(agentId2, interval, p);
                solvable = Solve(node2, currentTime, agents.First(a => a.ID == agentId2));
                if (solvable)
                    Open.Enqueue(node2.SolutionCost, node2);

                generatedNodes += 2;

            }

            //return the solution => suboptimal
            foreach (var agent in agents)
            {
                agent.Path = bestNode.getSolution(agent.ID);
                if (_deadlockHandler.IsInDeadlock(agent, currentTime))
                    _deadlockHandler.RandomHop(agent);
            }
        }

        private bool ValidatePath(ConflictTree.Node node, List<Agent> agents, out int agentId1, out int agentId2, out ReservationTable.Interval interval)
        {
            //clear
            _agentReservationTable.Clear();

            //add next hop reservations
            foreach (var agent in agents.Where(a => !a.FixedPosition))
                _agentReservationTable.Add(agent.ReservationsToNextNode, agent.ID);

            //get all reservations sorted
            var reservations = new FibonacciHeap<double, Tuple<Agent, ReservationTable.Interval>>();
            foreach (var agent in agents.Where(a => !a.FixedPosition))
                foreach (var reservation in node.getReservation(agent.ID))
                    reservations.Enqueue(reservation.Start, Tuple.Create(agent, reservation));

            //check all reservations
            while (reservations.Count > 0)
            {
                var reservation = reservations.Dequeue().Value;

                int collideWithAgentId;
                var intersectionFree = _agentReservationTable.IntersectionFree(reservation.Item2, out collideWithAgentId);
                if (!intersectionFree)
                {
                    agentId1 = collideWithAgentId;
                    agentId2 = reservation.Item1.ID;
                    interval = _agentReservationTable.GetOverlappingInterval(reservation.Item2);
                    if (interval.End - interval.Start > ReservationTable.TOLERANCE)
                        return false;
                }
                else
                {
                    _agentReservationTable.Add(reservation.Item2, reservation.Item1.ID);
                }
            }

            agentId1 = -1;
            agentId2 = -1;
            interval = null;
            return true;
        }


        /// <summary>
        /// Solves the specified node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="currentTime">The current time.</param>
        /// <param name="agent">The agent.</param>
        /// <param name="obstacleNodes">The obstacle nodes.</param>
        /// <param name="lockedNodes">The locked nodes.</param>
        /// <returns></returns>
        private bool Solve(ConflictTree.Node node, double currentTime, Agent agent)
        {
            //clear reservation table
            _reservationTable.Clear();

            //add constraints (agentId = -1 => Intervals from the tree)
            foreach (var constraint in node.getConstraints(agent.ID))
                _reservationTable.Add(constraint.IntervalConstraint);

            //drive to next node must be possible - otherwise it is not a valid node
            foreach (var reservation in agent.ReservationsToNextNode)
                if (!_reservationTable.IntersectionFree(reservation))
                    return false;

            //We can use WHCA Star here in a low level approach.
            //Window = Infinitively long
            var rraStar = new ReverseResumableAStar(Graph, agent, agent.Physics, agent.DestinationNode);
            var aStar = new SpaceTimeAStar(Graph, LengthOfAWaitStep, double.PositiveInfinity, _reservationTable, agent, rraStar);

            //execute
            var found = aStar.Search();

            //+ WHCA* Nodes
            List<ReservationTable.Interval> reservations;
            Path path = new Path();
            if (found)
            {
                //add all WHCA Nodes
                aStar.GetPathAndReservations(ref path, out reservations);

#if DEBUG
                foreach (var reservation in reservations)
                    Debug.Assert(_reservationTable.IntersectionFree(reservation));
#endif

                //add the next node again
                if (path.Count == 0 || path.NextAction.Node != agent.NextNode || path.NextAction.StopAtNode == false)
                    path.AddFirst(agent.NextNode, true, 0);

                node.setSolution(agent.ID, path, reservations);

                //found
                return true;

            }
            else
            {
                //not found
                return false;
            }


        }

        /// <summary>
        /// Strategy for Node Selection
        /// </summary>
        [Serializable]
        public enum CBSSearchMethod
        {
            BestFirst,
            DepthFirst,
            BreathFirst
        }

    }
}
