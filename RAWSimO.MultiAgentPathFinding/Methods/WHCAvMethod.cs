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

namespace RAWSimO.MultiAgentPathFinding.Methods
{
    /// <summary>
    /// A* Approach for the Multi Agent Path Finding Problem
    /// </summary>
    public class WHCAvStarMethod : PathFinder
    {

        /// <summary>
        /// The length of a wait step
        /// </summary>
        public double LengthOfAWindow = 20.0;

        /// <summary>
        /// Abort the search at first conflict
        /// </summary>
        public bool AbortAtFirstConflict = true;

        /// <summary>
        /// Indicates whether the method uses a deadlock handler.
        /// </summary>
        public bool UseDeadlockHandler = true;

        /// <summary>
        /// The RRA* Searches
        /// </summary>
        public Dictionary<int, ReverseResumableAStar> rraStars;

        /// <summary>
        /// Reservation Table
        /// </summary>
        public ReservationTable _reservationTable;

        /// <summary>
        /// The deadlock handler
        /// </summary>
        private DeadlockHandler _deadlockHandler;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="graph">graph</param>
        /// <param name="seed">The seed to use for the randomizer.</param>
        /// <param name="logger">The logger to use.</param>
        public WHCAvStarMethod(Graph graph, int seed, PathPlanningCommunicator logger)
            : base(graph, seed, logger)
        {
            if (graph.BackwardEdges == null)
                graph.GenerateBackwardEgdes();
            rraStars = new Dictionary<int, ReverseResumableAStar>();
            _reservationTable = new ReservationTable(graph);
            if (UseDeadlockHandler)
                _deadlockHandler = new DeadlockHandler(graph, seed);
        }

        /// <summary>
        /// Find the path for all the agents.
        /// </summary>
        /// <param name="agents">agents</param>
        /// <param name="queues">The queues for the destination, starting with the destination point.</param>
        /// <param name="nextReoptimization">The next re-optimization time.</param>
        /// <returns>
        /// paths
        /// </returns>
        /// <exception cref="System.Exception">Here I have to do something</exception>
        public override void FindPaths(double currentTime, List<Agent> agents)
        {
            Stopwatch.Restart();

            // Priorities of the agents
            var agentPrios = agents.ToDictionary(agent => agent.ID, agent => 0);

            for (int retry = 1; true; retry++)
            {
                var found = _findPaths(currentTime, agents, agentPrios, Stopwatch.ElapsedMilliseconds / 1000.0 > RuntimeLimitPerAgent * agents.Count * 0.7, Math.Min(RuntimeLimitPerAgent * agents.Count, RunTimeLimitOverall));
                if (found)
                    break;

                if (Stopwatch.ElapsedMilliseconds / 1000.0 > RuntimeLimitPerAgent * agents.Count * 0.9 || Stopwatch.ElapsedMilliseconds / 1000.0 > RunTimeLimitOverall)
                {
                    Communicator.SignalTimeout();
                    return;
                }
            }

        }

        /// <summary>
        /// Find the path for all the agents.
        /// </summary>
        /// <param name="agents">agents</param>
        /// <param name="obstacleWaypoints">The way points of the obstacles.</param>
        /// <param name="queues">The queues for the destination, starting with the destination point.</param>
        /// <param name="nextReoptimization">The next re-optimization time.</param>
        /// <returns>
        /// paths
        /// </returns>
        /// <exception cref="System.Exception">Here I have to do something</exception>
        private bool _findPaths(double currentTime, List<Agent> agents, Dictionary<int, int> agentPrios, bool lastRun, double runtimeLimit)
        {
            var conflictFree = true;

            //Reservation Table
            _reservationTable.Clear();
            var fixedBlockage = AgentInfoExtractor.getStartBlockage(agents, currentTime);

            SortAgents(ref agents, agentPrios);

            //set fixed blockage
            foreach (var interval in fixedBlockage.Values.SelectMany(d => d))
                _reservationTable.Add(interval);

            //deadlock handling
            if (UseDeadlockHandler)
            {
                _deadlockHandler.LengthOfAWaitStep = LengthOfAWaitStep;
                _deadlockHandler.MaximumWaitTime = 30;
                _deadlockHandler.Update(agents, currentTime);
            }

            //optimize Path
            foreach (var agent in agents.Where(a => !a.FixedPosition))
            {

                if (Stopwatch.ElapsedMilliseconds / 1000.0 > runtimeLimit * 0.9)
                {
                    Communicator.SignalTimeout();
                    return true;
                }

                //Create RRA* search if necessary.
                //Necessary if the agent has none or the agents destination has changed
                ReverseResumableAStar rraStar;
                if (!rraStars.TryGetValue(agent.ID, out rraStar) || rraStar.StartNode != agent.DestinationNode ||
                    UseDeadlockHandler && _deadlockHandler.IsInDeadlock(agent, currentTime)) // TODO this last expression is used to set back the state of the RRA* in case of a deadlock - this is only a hotfix
                {
                    rraStars[agent.ID] = new ReverseResumableAStar(Graph, agent, agent.Physics, agent.DestinationNode);
                }

                //search my path to the goal
                var aStar = new SpaceTimeAStar(Graph, LengthOfAWaitStep, currentTime + LengthOfAWindow, _reservationTable, agent, rraStars[agent.ID]);

                //the agent with a higher priority has to wait so that the others can go out of the way
                aStar.WaitStepsBeforeStart = (int)(Math.Pow(2, agentPrios[agent.ID]) / 2.0);

                //execute
                var found = aStar.Search();

                if (!found)
                {
                    conflictFree = false;

                    //fall back => ignore the collisions
                    agentPrios[agent.ID]++;
                    if (!lastRun)
                    {
                        if (!AbortAtFirstConflict)
                            continue;
                        else
                            return false;
                    }
                }

                //+ WHCA* Nodes
                List<ReservationTable.Interval> reservations;
                if (found)
                {
                    aStar.GetPathAndReservations(ref agent.Path, out reservations);

                    foreach (var reservation in reservations)
                        _reservationTable.Add(reservation);
                }

                //+ RRA* Nodes
                if (aStar.GoalNode >= 0)
                    rraStars[agent.ID].addPath(agent.Path, aStar.NodeTo2D(aStar.GoalNode));

                //add the next node again
                if (fixedBlockage.Count > 0 && (agent.Path.Count == 0 || agent.Path.NextAction.Node != agent.NextNode || agent.Path.NextAction.StopAtNode == false))
                    agent.Path.AddFirst(agent.NextNode, true, 0);

                //next time ready?
                if (agent.Path.Count == 0)
                    rraStars[agent.ID] = null;

                //deadlock?
                if (UseDeadlockHandler)
                    if (_deadlockHandler.IsInDeadlock(agent, currentTime))
                        _deadlockHandler.RandomHop(agent);
            }

            return conflictFree;
        }

        /// <summary>
        /// Sorts the agents.
        /// </summary>
        /// <param name="agents">The agents.</param>
        /// <param name="queues">The queues.</param>
        private void SortAgents(ref List<Agent> agents, Dictionary<int, int> agentPrios)
        {
            agents = agents.OrderByDescending(a => agentPrios[a.ID]).ThenBy(a => a.CanGoThroughObstacles ? 1 : 0).ThenBy(a => Graph.getDistance(a.NextNode, a.DestinationNode)).ToList();
        }
    }
}
