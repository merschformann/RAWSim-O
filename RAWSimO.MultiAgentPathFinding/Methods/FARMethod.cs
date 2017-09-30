using RAWSimO.MultiAgentPathFinding.Algorithms.AStar;
using RAWSimO.MultiAgentPathFinding.DataStructures;
using RAWSimO.MultiAgentPathFinding.Elements;
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
    /// Flow Annotation Re-planning by Ko-Hsin Cindy Wang and Adi Botea 2008
    /// </summary>
    public class FARMethod : PathFinder
    {
        /// <summary>
        /// Agent i is waiting for Agent j
        /// </summary>
        public Dictionary<int, int> _waitFor;

        /// <summary>
        /// Last passed node of Agent i
        /// </summary>
        public Dictionary<int, ReverseResumableAStar> _rraStar;

        /// <summary>
        /// point in time where the last move action ended
        /// if t is greater than move time + MaximumWaitTime, then the agent will try to evade.
        /// </summary>
        public Dictionary<int, double> _moveTime;

        /// <summary>
        /// point in time where the last wait action ended
        /// </summary>
        public Dictionary<int, double> _waitTime;

        /// <summary>
        /// Reservation Table
        /// </summary>
        public ReservationTable _reservationTable;

        /// <summary>
        /// Length of the panic interval. If an agent did not move within this time period, the
        /// search will look for a way around the blocked nodes even if no deadlock was found.
        /// </summary>
        public double MaximumWaitTime = 30.0;

        /// <summary>
        /// Indicates whether the method uses a deadlock handler.
        /// </summary>
        public bool UseDeadlockHandler = true;

        /// <summary>
        /// The evading strategy
        /// </summary>
        public EvadingStrategy _evadingStragety;

        /// <summary>
        /// Evading Strategy 1: The maximum number of breaking maneuver tries
        /// </summary>
        public int Es1MaximumNumberOfBreakingManeuverTries = 2;

        /// <summary>
        /// Evading Strategy 2: No evading to a node the bot already evaded from
        /// </summary>
        public bool Es2BackEvadingAvoidance = true;

        /// <summary>
        /// Evading Strategy 2: Nodes an Agent evaded from
        /// </summary>
        public Dictionary<int, HashSet<int>> _es2evadedFrom;

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
        public FARMethod(Graph graph, int seed, EvadingStrategy evadingStragety, PathPlanningCommunicator logger)
            : base(graph, seed, logger)
        {
            _evadingStragety = evadingStragety;

            if (graph.BackwardEdges == null)
                graph.GenerateBackwardEgdes();

            _waitFor = new Dictionary<int, int>();
            _reservationTable = new ReservationTable(graph, false, true, false);
            _rraStar = new Dictionary<int, ReverseResumableAStar>();
            _moveTime = new Dictionary<int, double>();
            _waitTime = new Dictionary<int, double>();
            _es2evadedFrom = new Dictionary<int, HashSet<int>>();
            if (UseDeadlockHandler)
                _deadlockHandler = new DeadlockHandler(graph, seed);
        }

        /// <summary>
        /// Find the path for all the agents.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        /// <param name="agents">agents</param>
        /// <param name="obstacleNodes">The way points of the obstacles.</param>
        /// <param name="lockedNodes">The locked nodes.</param>
        /// <param name="nextReoptimization">The next re-optimization time.</param>
        /// <param name="runtimeLimit">The runtime limit.</param>
        public override void FindPaths(double currentTime, List<Agent> agents)
        {
            Stopwatch.Restart();

            //Reservation Table
            _reservationTable.Clear();
            var fixedBlockage = AgentInfoExtractor.getStartBlockage(agents, currentTime);

            //panic times initialization
            foreach (var agent in agents)
            {
                if (!_waitTime.ContainsKey(agent.ID))
                    _waitTime.Add(agent.ID, double.NegativeInfinity);
                if (!_moveTime.ContainsKey(agent.ID))
                    _moveTime.Add(agent.ID, currentTime);
                if (!_es2evadedFrom.ContainsKey(agent.ID))
                    _es2evadedFrom.Add(agent.ID, new HashSet<int>());
            }

            //set fixed blockage
            try
            {
                foreach (var agent in agents)
                {
                    //all intervals from now to the moment of stop
                    foreach (var interval in fixedBlockage[agent])
                        _reservationTable.Add(interval, agent.ID);

                    //reservation of next node
                    int collisionAgent;
                    if (_reservationTable.IntersectionFree(agent.NextNode, agent.ArrivalTimeAtNextNode, double.PositiveInfinity, out collisionAgent))
                        _reservationTable.Add(agent.NextNode, agent.ArrivalTimeAtNextNode, double.PositiveInfinity, agent.ID);
                    else
                        Debug.Assert(collisionAgent == agent.ID);
                }
            }
            catch (RAWSimO.MultiAgentPathFinding.DataStructures.DisjointIntervalTree.IntervalIntersectionException) { }

            //deadlock handling
            if (UseDeadlockHandler)
            {
                _deadlockHandler.LengthOfAWaitStep = LengthOfAWaitStep;
                _deadlockHandler.MaximumWaitTime = 30;
                _deadlockHandler.Update(agents, currentTime);
            }

            //optimize Path
            bool deadlockBreakingManeuver;
            var reservationOwnerAgentId = -1;
            int reservationOwnerNodeId = -1;
            List<int> nextHopNodes;

            foreach (Agent a in agents)
                if (a.Path != null && !a.Path.IsConsistent)
                    throw new Exception("fs ex");

            foreach (var agent in agents.Where(a => !a.FixedPosition && a.NextNode != a.DestinationNode && a.ArrivalTimeAtNextNode <= currentTime && a.RequestReoptimization))
            {

                //runtime exceeded
                if (Stopwatch.ElapsedMilliseconds / 1000.0 > RuntimeLimitPerAgent * agents.Count * 0.9 || Stopwatch.ElapsedMilliseconds / 1000.0 > RunTimeLimitOverall)
                {
                    Communicator.SignalTimeout();
                    return;
                }

                deadlockBreakingManeuver = false;

                //agent is allowed to move?
                if (currentTime < _waitTime[agent.ID])
                    continue;

                //remove blocking next node
                _reservationTable.RemoveIntersectionWithTime(agent.NextNode, agent.ArrivalTimeAtNextNode);

                //Create RRA* search if necessary.
                //Necessary if the agent has none or the agents destination has changed
                ReverseResumableAStar rraStar;
                if (!_rraStar.TryGetValue(agent.ID, out rraStar) || rraStar == null || rraStar.StartNode != agent.DestinationNode)
                {
                    //new search
                    rraStar = new ReverseResumableAStar(Graph, agent, agent.Physics, agent.DestinationNode, new HashSet<int>());
                    _rraStar[agent.ID] = rraStar;
                    _moveTime[agent.ID] = currentTime;
                }

                //already found in RRA*?
                var found = rraStar.Closed.Contains(agent.NextNode);

                //If the search is old, the path may me blocked now
                if (found && rraStar.PathContains(agent.NextNode))
                {
                    //new search
                    rraStar = new ReverseResumableAStar(Graph, agent, agent.Physics, agent.DestinationNode, new HashSet<int>());
                    _rraStar[agent.ID] = rraStar;
                    found = false;
                }

                //Is a search processing necessary
                if (!found)
                    found = rraStar.Search(agent.NextNode);

                //the search ended with no result => just wait a moment
                if (!found)
                {

                    //new search
                    _rraStar[agent.ID] = null;

                    //still not found? Then wait!
                    if (_waitTime[agent.ID] < currentTime - LengthOfAWaitStep * 2f)
                    {
                        waitStep(agent, agent.ID, currentTime);
                        continue;
                    }
                    else
                    {
                        deadlockBreakingManeuver = true;
                    }
                }

                if (!deadlockBreakingManeuver)
                {
                    //set the next step of the path
                    nextHopNodes = _getNextHopNodes(currentTime, agent, out reservationOwnerAgentId, out reservationOwnerNodeId);

                    //avoid going back
                    if (Es2BackEvadingAvoidance && nextHopNodes.Count > 1 && _es2evadedFrom[agent.ID].Contains(nextHopNodes[1]))
                        deadlockBreakingManeuver = true;
                    else if (Es2BackEvadingAvoidance)
                        _es2evadedFrom[agent.ID].Clear();

                    //found a path => set it
                    if (!deadlockBreakingManeuver && nextHopNodes.Count > 1)
                    {
                        _setPath(nextHopNodes, agent);
                        _moveTime[agent.ID] = currentTime;
                        continue;
                    }
                }

                //deadlock breaking maneuver due to wait time
                if (!deadlockBreakingManeuver)
                {
                    deadlockBreakingManeuver = currentTime - _moveTime[agent.ID] > MaximumWaitTime;
                }

                //deadlock breaking maneuver due to wait for relation circle
                if (!deadlockBreakingManeuver)
                {
                    //transitive closure of wait for relation
                    HashSet<int> waitForSet = new HashSet<int>();
                    var waitForID = agent.ID;
                    while (!deadlockBreakingManeuver && _waitFor.ContainsKey(waitForID))
                    {
                        if (waitForSet.Contains(waitForID))
                            deadlockBreakingManeuver = true;
                        waitForSet.Add(waitForID);
                        waitForID = _waitFor[waitForID];
                    }
                }


                if (!deadlockBreakingManeuver)
                {
                    //wait a little while
                    waitStep(agent, reservationOwnerAgentId, currentTime);
                    continue;
                }
                else
                {
                    //deadlock breaking maneuver must be done!
                    switch (_evadingStragety)
                    {
                        case EvadingStrategy.EvadeByRerouting:

                            //obstacle free
                            if (!found)
                                rraStar = _rraStar[agent.ID] = new ReverseResumableAStar(Graph, agent, agent.Physics, agent.DestinationNode, new HashSet<int>());


                            for (int tries = 1; tries <= Es1MaximumNumberOfBreakingManeuverTries; tries++)
                            {

                                //deadlock breaking maneuver must be done!
                                if (tries >= Es1MaximumNumberOfBreakingManeuverTries)
                                {
                                    //wait a little while => for myself
                                    waitStep(agent, agent.ID, currentTime);
                                    break;
                                }

                                //The agent can not move
                                _waitFor[agent.ID] = reservationOwnerAgentId;

                                //add an obstacle and restart the search
                                rraStar.AddCustomLock(reservationOwnerNodeId);
                                rraStar.Clear(agent.DestinationNode, agent.NextNode);
                                found = rraStar.Search(agent.NextNode);

                                //found => get hop nodes
                                if (!found)
                                {
                                    //wait a little while => for myself
                                    waitStep(agent, agent.ID, currentTime);
                                    break;
                                }
                                else
                                {
                                    nextHopNodes = _getNextHopNodes(currentTime, agent, out reservationOwnerAgentId, out reservationOwnerNodeId);

                                    if (nextHopNodes.Count > 1)
                                    {
                                        _setPath(nextHopNodes, agent);
                                        break;
                                    }

                                }


                            }
                            break;
                        case EvadingStrategy.EvadeToNextNode:

                            //try to find a free hop
                            var foundBreakingManeuverEdge = false;
                            var possibleEdges = new List<Edge>(Graph.Edges[agent.NextNode]);
                            shuffle<Edge>(possibleEdges, Randomizer);
                            foreach (var edge in possibleEdges.Where(e => !e.ToNodeInfo.IsLocked && (agent.CanGoThroughObstacles || !e.ToNodeInfo.IsObstacle) && !_es2evadedFrom[agent.ID].Contains(e.To)))
                            {
                                //create intervals
                                var intervals = _reservationTable.CreateIntervals(currentTime, currentTime, 0, agent.Physics, agent.NextNode, edge.To, true);
                                reservationOwnerNodeId = -1;
                                reservationOwnerAgentId = -1;

                                //check if a reservation is possible
                                if (_reservationTable.IntersectionFree(intervals, out reservationOwnerNodeId, out reservationOwnerAgentId))
                                {
                                    foundBreakingManeuverEdge = true;

                                    //avoid going back
                                    if (this.Es2BackEvadingAvoidance)
                                        _es2evadedFrom[agent.ID].Add(edge.To);

                                    //create a path
                                    agent.Path.Clear();
                                    agent.Path.AddLast(edge.To, true, LengthOfAWaitStep);

                                    break;
                                }
                            }

                            if (!foundBreakingManeuverEdge)
                            {
                                //Clear the nodes
                                _es2evadedFrom[agent.ID].Clear();

                                //just wait
                                waitStep(agent, agent.ID, currentTime);
                            }

                            break;
                    }
                }

                foreach (Agent a in agents)
                    if (a.Path != null && !a.Path.IsConsistent)
                        throw new Exception("fs ex");

                //deadlock?
                if (UseDeadlockHandler)
                    if (_deadlockHandler.IsInDeadlock(agent, currentTime))
                        _deadlockHandler.RandomHop(agent);
            }
        }

        private void _setPath(List<int> nextHopNodes, Agent agent)
        {
            //create path until next turn
            agent.Path.Clear();
            for (int i = 0; i < nextHopNodes.Count; i++)
            {
                if (agent.Path.Count == 0 || agent.Path.LastAction.Node != nextHopNodes[i])
                    agent.Path.AddLast(nextHopNodes[i], (i == nextHopNodes.Count - 1), 0);
            }
        }

        private List<int> _getNextHopNodes(double currentTime, Agent agent, out int reservationOwnerAgentId, out int reservationOwnerNodeId)
        {

            var nextHopNodes = _rraStar[agent.ID].NextsNodesUntilTurn(agent.NextNode);

            var startReservation = currentTime;
            reservationOwnerNodeId = -1;
            reservationOwnerAgentId = -1;

            //while the agent could possibly move
            while (nextHopNodes.Count > 1)
            {
                //create intervals
                var intervals = _reservationTable.CreateIntervals(currentTime, startReservation, 0, agent.Physics, nextHopNodes[0], nextHopNodes[nextHopNodes.Count - 1], true);

                //check if a reservation is possible
                if (_reservationTable.IntersectionFree(intervals, out reservationOwnerNodeId, out reservationOwnerAgentId))
                {
                    break;
                }
                else
                {
                    //delete all nodes from the reserved node to the end
                    var deleteFrom = nextHopNodes.IndexOf(reservationOwnerNodeId);
                    for (int i = nextHopNodes.Count - 1; i >= deleteFrom; i--)
                        nextHopNodes.RemoveAt(i);
                }
            }

            //blame yourself if you can not find anybody
            if (reservationOwnerAgentId == -1)
                reservationOwnerAgentId = agent.ID;

            return nextHopNodes;
        }

        /// <summary>
        /// Waits the step.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="waitForAgentId">The "wait for" agent identifier.</param>
        /// <returns>A path with a waiting step</returns>
        private void waitStep(Agent agent, int waitForAgentId, double currentTime)
        {
            agent.Path.Clear();
            _waitFor[agent.ID] = waitForAgentId;

            //wait a little while
            agent.Path.AddFirst(agent.NextNode, true, LengthOfAWaitStep);

            //add blocking next node
            _reservationTable.Add(agent.NextNode, agent.ArrivalTimeAtNextNode, double.PositiveInfinity, agent.ID);

            _waitTime[agent.ID] = currentTime + LengthOfAWaitStep;

        }
        /// <summary>
        /// Shuffles the specified list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        public static void shuffle<T>(IList<T> list, Random rnd)
        {
            int n = list.Count;
            while (n > 1)
            {
                int k = (rnd.Next(0, n) % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// the evading strategies
        /// </summary>
        public enum EvadingStrategy
        {
            /// <summary>
            /// The evade by rerouting to the destination node
            /// </summary>
            EvadeByRerouting,

            /// <summary>
            /// The evade by go to a random next node
            /// </summary>
            EvadeToNextNode


        }
    }

}
