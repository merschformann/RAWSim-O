using RAWSimO.MultiAgentPathFinding.Algorithms.AStar;
using RAWSimO.MultiAgentPathFinding.DataStructures;
using RAWSimO.MultiAgentPathFinding.Elements;
using RAWSimO.MultiAgentPathFinding.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MultiAgentPathFinding.Methods
{
    /// <summary>
    /// PAS - Parallel Agent Search - Erdmann 2015
    /// </summary>
    public class PASMethod : PathFinder
    {

        /// <summary>
        /// The length of a wait step
        /// </summary>
        public double LengthOfAWindow = 30.0;

        /// <summary>
        /// Maximum number of retries
        /// </summary>
        public int MaxPriorities = 2;

        /// <summary>
        /// The RRA* Searches
        /// </summary>
        public Dictionary<int, ReverseResumableAStar> rraStars;

        /// <summary>
        /// Reservation Table
        /// </summary>
        public ReservationTable _reservationTable;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="graph">graph</param>
        /// <param name="seed">The seed to use for the randomizer.</param>
        /// <param name="logger">The logger to use.</param>
        public PASMethod(Graph graph, int seed, PathPlanningCommunicator logger)
            : base(graph, seed, logger)
        {
            if (graph.BackwardEdges == null)
                graph.GenerateBackwardEgdes();
            rraStars = new Dictionary<int, ReverseResumableAStar>();
            _reservationTable = new ReservationTable(graph, true, true, true);
        }

        /// <summary>
        /// Find the path for all the agents.
        /// </summary>
        /// <param name="currentTime"></param>
        /// <param name="agents">agents</param>
        /// <param name="obstacleNodes">The way points of the obstacles.</param>
        /// <param name="lockedNodes"></param>
        /// <param name="nextReoptimization">The next re-optimization time.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void FindPaths(double currentTime, List<Elements.Agent> agents)
        {
            Stopwatch.Restart();

            //Reservation Table
            _reservationTable.Clear();

            //get fixed Blockage
            var fixedBlockage = AgentInfoExtractor.getStartBlockage(agents, currentTime);
            foreach (var agent in agents)
                _reservationTable.Add(fixedBlockage[agent], agent.ID, MaxPriorities + 1);

            //initial agent times
            var agentTimesHeap = new FibonacciHeap<double, Agent>(HeapDirection.Increasing);
            var agentTimesDict = agents.Where(a => !a.FixedPosition).ToDictionary(a => a, a => agentTimesHeap.Enqueue(a.ArrivalTimeAtNextNode, a));

            //initial agent nodes
            var agentPrios = agents.Where(a => !a.FixedPosition).ToDictionary(a => a.ID, a => 0);
            var agentNodes = agents.Where(a => !a.FixedPosition).ToDictionary(a => a.ID, a => 0);
            var agentReservations = agents.Where(a => !a.FixedPosition).ToDictionary(a => a.ID, a => new List<ReservationTable.Interval>());

            //initiate action generator
            var actionGenerator = new Dictionary<int, PASActionGenerator>();
            foreach (var agent in agents.Where(a => !a.FixedPosition))
            {
                //Create RRA* search if necessary.
                //Necessary if the agent has none or the agents destination has changed
                ReverseResumableAStar rraStar;
                if (!rraStars.TryGetValue(agent.ID, out rraStar) || rraStar.StartNode != agent.DestinationNode)
                    rraStars[agent.ID] = new ReverseResumableAStar(Graph, agent, agent.Physics, agent.DestinationNode);

                actionGenerator.Add(agent.ID, new PASActionGenerator(Graph, LengthOfAWaitStep, agent, rraStars[agent.ID]));
            }

            //action sorter
            var actionSorter = new FibonacciHeap<double, Tuple<int, List<ReservationTable.Interval>, List<Collision>>>(HeapDirection.Increasing);

            //loop
            double cancelAt = currentTime + LengthOfAWindow;
            while (agentTimesHeap.Count > 0 && Stopwatch.ElapsedMilliseconds / 1000.0 < RuntimeLimitPerAgent * agents.Count * 0.9 && Stopwatch.ElapsedMilliseconds / 1000.0 < RunTimeLimitOverall)
            {
                //pick the agent that has the smallest time
                if (cancelAt <= agentTimesHeap.Top.Priority)
                    break;

                var currentAgent = agentTimesHeap.Top.Value;
                var currentActionGenerator = actionGenerator[currentAgent.ID];
                var currentAgentNode = agentNodes[currentAgent.ID];
                var currentAgentReservations = agentReservations[currentAgent.ID];

                //if (currentAgent.ID == 41)
                //    currentAgent.ID = currentAgent.ID;

                var reservationSuccessfull = false;

                //initiate sorter
                while (actionSorter.Count > 0)
                    actionSorter.Dequeue();

                //get and sort actions
                var actions = currentActionGenerator.GetActions(currentAgentNode, agentPrios[currentAgent.ID], _reservationTable);
                var allInfinity = actions.All(a => double.IsInfinity(currentActionGenerator.h(currentAgentNode, a.Item1))) && actions.Count > 1;
                foreach (var action in actions)
                    actionSorter.Enqueue(currentActionGenerator.g(action.Item1) + currentActionGenerator.h(currentAgentNode, action.Item1, allInfinity), action);

                //try to reserve
                while (actionSorter.Count > 0)
                {

                    var actionNode = actionSorter.Top.Value.Item1;
                    var actionReservatios = actionSorter.Top.Value.Item2;
                    var actionCollisions = actionSorter.Top.Value.Item3;
                    actionSorter.Dequeue();


                    //reservation possible?
                    if (actionCollisions == null || actionCollisions.All(c => c.priority < agentPrios[currentAgent.ID] || c.agentId == currentAgent.ID))
                    {

                        //delete other reservations
                        if (actionCollisions != null)
                        {
                            //delete own reservations till last turn node
                            while (currentAgentReservations.Count > 0 &&
                                   currentAgentReservations.Last().Start >= currentActionGenerator.NodeTime[currentActionGenerator.NodeBackpointerLastStopId[actionNode]] - ReservationTable.TOLERANCE)
                            {
                                _reservationTable.Remove(currentAgentReservations.Last());
                                currentAgentReservations.RemoveAt(currentAgentReservations.Count - 1);
                            }

                            //delete other reservations
                            foreach (var collsion in actionCollisions.Where(c => c.agentId != currentAgent.ID))
                            {
                                var nodeToSetBackToPast = agentNodes[collsion.agentId];
                                var reservationToSetBackToPast = agentReservations[collsion.agentId];
                                setAgentBackToPast(collsion.agentId, actionGenerator[collsion.agentId], ref nodeToSetBackToPast, ref reservationToSetBackToPast, collsion.time);
                                agentNodes[collsion.agentId] = nodeToSetBackToPast;
                                agentReservations[collsion.agentId] = reservationToSetBackToPast; //note: I know - it is only a reference to the list => but for clearance
                            }
                        }

                        //reserve me
                        _reservationTable.Add(actionReservatios, currentAgent.ID, agentPrios[currentAgent.ID]);
                        currentAgentReservations.AddRange(actionReservatios);

                        //set my node
                        currentAgentNode = agentNodes[currentAgent.ID] = actionNode;

                        //reached destination?
                        if (currentActionGenerator.NodeTo2D(currentAgentNode) == currentAgent.DestinationNode)
                        {
                            //Here the reason of commenting: Only 2 or 3 Nodes will be checked by reservation table, the rest will be added blind. If there are 10 Nodes in the hop, 7 reservations will possibly rejected. So the whole transfer will be rejected.
                            //cancelAt = Math.Min(cancelAt,currentActionGenerator.NodeTime[currentAgentNode]);
                            agentTimesHeap.Dequeue();
                            if (_reservationTable.IntersectionFree(currentActionGenerator.NodeTo2D(currentAgentNode), currentActionGenerator.NodeTime[currentAgentNode], double.PositiveInfinity))
                                _reservationTable.Add(currentActionGenerator.NodeTo2D(currentAgentNode), currentActionGenerator.NodeTime[currentAgentNode], double.PositiveInfinity, currentAgent.ID, MaxPriorities + 1);
                        }
                        else
                        {
                            //set the time and node
                            agentTimesHeap.ChangeKey(agentTimesDict[currentAgent], actionReservatios.Last().End);
                        }

                        //reservation successful
                        reservationSuccessfull = true;

                        break;
                    }

                }

                //could not find an action
                if (reservationSuccessfull)
                {
                    agentPrios[currentAgent.ID] = 0;
                }
                else
                {
                    agentPrios[currentAgent.ID]++;

                    //wait step
                    var waitNode = currentActionGenerator.GenerateWaitNode(currentAgentNode, _reservationTable);

                    if (agentPrios[currentAgent.ID] < MaxPriorities)
                    {

                        if (waitNode.Item3 == null || waitNode.Item3.All(c => c.priority < agentPrios[currentAgent.ID]))
                        {
                            //delete other reservations
                            if (waitNode.Item3 != null)
                            {
                                foreach (var collsion in waitNode.Item3)
                                {
                                    //reset agent moves
                                    var nodeToSetBackToPast = agentNodes[collsion.agentId];
                                    var reservationToSetBackToPast = agentReservations[collsion.agentId];
                                    setAgentBackToPast(collsion.agentId, actionGenerator[collsion.agentId], ref nodeToSetBackToPast, ref reservationToSetBackToPast, collsion.time);
                                    agentNodes[collsion.agentId] = nodeToSetBackToPast;
                                    agentReservations[collsion.agentId] = reservationToSetBackToPast; //note: I know - it is only a reference to the list => but for clearance
                                }
                            }

                            //reserve me
                            currentAgentReservations.AddRange(waitNode.Item2);
                            _reservationTable.Add(waitNode.Item2, currentAgent.ID, agentPrios[currentAgent.ID]);

                            //set next node
                            agentTimesHeap.ChangeKey(agentTimesDict[currentAgent], waitNode.Item2.Last().End);
                            currentAgentNode = agentNodes[currentAgent.ID] = waitNode.Item1;
                        }
                    }
                    else
                    {
                        //no reservation
                        agentPrios[currentAgent.ID] = 0;

                        //set next node
                        agentTimesHeap.ChangeKey(agentTimesDict[currentAgent], waitNode.Item2.Last().End);
                        currentAgentNode = agentNodes[currentAgent.ID] = waitNode.Item1;

                    }
                }


            }//agent pick loop

            // Signal potential timeout
            if (Stopwatch.ElapsedMilliseconds / 1000.0 > RuntimeLimitPerAgent * agents.Count * 0.9 || Stopwatch.ElapsedMilliseconds / 1000.0 > RunTimeLimitOverall)
                Communicator.SignalTimeout();

            foreach (var agent in agents.Where(a => !a.FixedPosition))
            {
                agent.Path = agent.Path ?? new Path();

                //+ WHCA* Nodes
                List<ReservationTable.Interval> reservations;
                actionGenerator[agent.ID].GetPathAndReservations(ref agent.Path, out reservations, agentNodes[agent.ID], 0.0);

                //+ RRA* Nodes
                rraStars[agent.ID].addPath(agent.Path, actionGenerator[agent.ID].NodeTo2D(agentNodes[agent.ID]));

                //add the next node again
                if (fixedBlockage.Count > 0 && (agent.Path.Count == 0 || agent.Path.NextAction.Node != agent.NextNode || agent.Path.NextAction.StopAtNode == false))
                    agent.Path.AddFirst(agent.NextNode, true, 0);

                //next time ready?
                if (agent.Path.Count == 0)
                    rraStars[agent.ID] = null;
            }
        }

        /// <summary>
        /// Sets the agent node back to past and deletes all the reservations done by this agent.
        /// </summary>
        /// <param name="agentId">The agent identifier.</param>
        /// <param name="actionGenerator">The action generator.</param>
        /// <param name="node">The node.</param>
        /// <param name="time">The time to set back.</param>
        private void setAgentBackToPast(int agentId, PASActionGenerator actionGenerator, ref int node, ref List<ReservationTable.Interval> reservations, double time)
        {
            var currentTime = actionGenerator.NodeTime[node];

            while (currentTime > time)
            {
                //get previous node (last stop node if it is not a wait node)
                int previousNode = node > actionGenerator.NodeBackpointerLastStopId[node] ? actionGenerator.NodeBackpointerLastStopId[node] : actionGenerator.NodeBackpointerId[node];

                //can't go back any further
                if (previousNode == -1)
                {
                    foreach (var reservation in reservations)
                        _reservationTable.Remove(reservation);

                    reservations.Clear();
                    return;
                }

                //remove reservation
                while (reservations.Count > 0 &&
                       reservations.Last().Start >= actionGenerator.NodeTime[previousNode])
                {
                    _reservationTable.Remove(reservations.Last());
                    reservations.RemoveAt(reservations.Count - 1);
                }

                //go back
                node = previousNode;
            }

        }
    }

    /// <summary>
    /// Step Generator
    /// </summary>
    public class PASActionGenerator : SpaceTimeAStar
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="PASActionGenerator"/> class.
        /// </summary>
        /// <param name="graph">The graph.</param>
        /// <param name="lengthOfAWaitStep">The length of a wait step.</param>
        /// <param name="agent">The agent.</param>
        /// <param name="obstacles">The obstacles.</param>
        /// <param name="rraStar">The rra star.</param>
        public PASActionGenerator(Graph graph, double lengthOfAWaitStep, Agent agent, ReverseResumableAStar rraStar)
            : base(graph, lengthOfAWaitStep, double.PositiveInfinity, null, agent, rraStar)
        {

        }

        /// <summary>
        /// Generates the wait node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns></returns>
        public Tuple<int, List<ReservationTable.Interval>, List<Collision>> GenerateWaitNode(int parent, ReservationTable reservationTable)
        {
            List<Collision> collisions;
            reservationTable.IntersectionFree(NodeTo2D(parent), NodeTime[parent], NodeTime[parent] + _lengthOfAWaitStep, out collisions);

            //add successor
            var waitNode = Tuple.Create(_numNodeId, new List<ReservationTable.Interval>(new[] { new ReservationTable.Interval(NodeTo2D(parent), NodeTime[parent], NodeTime[parent] + _lengthOfAWaitStep) }), collisions);
            NodeTime.Add(NodeTime[parent] + _lengthOfAWaitStep);
            NodeBackpointerId.Add(parent);
            NodeBackpointerLastStopId.Add(_numNodeId);
            NodeBackpointerEdge.Add(null);
            _numNodeId++;
            return waitNode;

        }

        /// <summary>
        /// Gets the actions.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>actions</returns>
        public List<Tuple<int, List<ReservationTable.Interval>, List<Collision>>> GetActions(int n, int prio, ReservationTable reservationTable)
        {
            var successors = new List<Tuple<int, List<ReservationTable.Interval>, List<Collision>>>();

            List<int> checkPointNodes = new List<int>();
            List<double> checkPointDistances = new List<double>();
            List<double> checkPointTimes;
            List<Collision> collisions;

            //wait successor
            reservationTable.IntersectionFree(NodeTo2D(n), NodeTime[n], NodeTime[n] + _lengthOfAWaitStep, out collisions);
            if (collisions == null || collisions.All(c => c.priority < prio || c.agentId == _agent.ID))
            {
                //add successor
                successors.Add(Tuple.Create(_numNodeId, new List<ReservationTable.Interval>(new[] { new ReservationTable.Interval(NodeTo2D(n), NodeTime[n], NodeTime[n] + _lengthOfAWaitStep) }), collisions));
                NodeTime.Add(NodeTime[n] + _lengthOfAWaitStep);
                NodeBackpointerId.Add(n);
                NodeBackpointerLastStopId.Add(_numNodeId);
                NodeBackpointerEdge.Add(null);
                _numNodeId++;
            }

            //current angle
            short lastStopAngleAfterTurn = GetLastStopAngleAfterTurn(n);

            //for each direction
            foreach (var direction in _graph.Edges[NodeTo2D(n)])
            {

                //clear temporary data structures
                NodeTimeTemp.Clear();
                NodeBackpointerIdTemp.Clear();
                NodeBackpointerLastTurnIdTemp.Clear();
                NodeBackpointerEdgeTemp.Clear();

                //initiate checkpoints
                checkPointTimes = null;
                checkPointNodes.Clear();
                checkPointDistances.Clear();

                //Our aim is to calculate the distance for a hop.
                //A hop is defined as follows: The longest distance between two nodes, where no turn is necessary.
                //The end of the hop is "node". In this method we search for the start ("lastTurnNode") and calculate the g as follows:
                //g(node) = time needed to get to "lastTurnNode" + time needed to turn + time needed to get to node

                //angle i had before my current angle: lastTurnAngle
                //my current angle: thisAngle
                //the angle i want to go to: direction.Angle
                int lastStopId = NodeBackpointerLastStopId[n];

                if (direction.Angle != lastStopAngleAfterTurn)
                {
                    //we turn at n
                    lastStopId = n;
                    checkPointDistances.Add(0.0);
                    checkPointNodes.Add(NodeTo2D(n));
                }
                else
                {

                    //get the intermediate checkpoints
                    AddCheckPointDistances(lastStopId, n, checkPointNodes, checkPointDistances);

                }

                //time needed to get to the target orientation
                //last stop angle after turn of the last stop <=> last stop angle before turn
                short lastStopAngleBeforeTurn = GetLastStopAngleAfterTurn(lastStopId);
                double timeToTurn = _agent.Physics.getTimeNeededToTurn(Graph.DegreeToRad(lastStopAngleBeforeTurn), Graph.DegreeToRad(direction.Angle));

                //check if there is enough free time to rotate in the direction
                /*
                if (timeToTurn > 0)
                {
                    reservationTable.IntersectionFree(NodeTo2D(lastStopId), NodeTime[lastStopId], NodeTime[lastStopId] + timeToTurn, out collisions);
                    if (collisions != null && collisions.All(c => c.priority >= prio && c.agentId != _agent.ID))
                        continue;
                }
                 * 
                 * => Do it later
                 * 
                 */

                //generate successors 
                var currentNode = NodeTo2D(n);
                var agentAngle = direction.Angle;
                var backpointerNode = n;

                //get drive distance
                var driveDistance = checkPointDistances[checkPointDistances.Count - 1];

                var foundNext = true;
                var pathFree = false;
                //try to find the first node in this direction, which path is free
                while (foundNext && !pathFree)
                {
                    foundNext = false;

                    //search for next edges in same direction
                    foreach (var edge in _graph.Edges[currentNode])
                    {

                        if (Math.Abs(edge.Angle - agentAngle) < 2)
                        {
                            //skip blocked edges
                            if (edge.ToNodeInfo.IsLocked || (!_agent.CanGoThroughObstacles && edge.ToNodeInfo.IsObstacle))
                            {
                                //blocked
                                foundNext = false;
                                break;
                            }

                            //cumulate 
                            driveDistance += edge.Distance;

                            //add checkpoint
                            checkPointDistances.Add(driveDistance);
                            checkPointNodes.Add(edge.To);

                            //check whether it is intersection free
                            var timeToMove = _agent.Physics.getTimeNeededToMove(0f, NodeTime[lastStopId] + timeToTurn, driveDistance, checkPointDistances, out checkPointTimes);

                            //add time needed to turn
                            if (checkPointTimes.Count > 0)
                                checkPointTimes[0] -= timeToTurn;

                            //check if driving action is collision free
                            reservationTable.IntersectionFree(checkPointNodes, checkPointTimes, false, out collisions);
                            pathFree = collisions == null || collisions.All(c => c.priority < prio || c.agentId == _agent.ID);

                            //add node to temp => will be added, if a valid successor will be found
                            NodeTimeTemp.Add(NodeTime[lastStopId] + timeToTurn + timeToMove);
                            NodeBackpointerIdTemp.Add(backpointerNode);
                            NodeBackpointerLastTurnIdTemp.Add(lastStopId);
                            NodeBackpointerEdgeTemp.Add(edge);

                            if (pathFree)
                            {
                                //treat only the last one as successor
                                successors.Add(Tuple.Create(_numNodeId + NodeTimeTemp.Count - 1, reservationTable.CreateIntervals(NodeTime[lastStopId], checkPointNodes, checkPointTimes, false), collisions));
                                _numNodeId += NodeTimeTemp.Count;

                                //add temporary successors
                                NodeTime.AddRange(NodeTimeTemp);
                                NodeBackpointerId.AddRange(NodeBackpointerIdTemp);
                                NodeBackpointerLastStopId.AddRange(NodeBackpointerLastTurnIdTemp);
                                NodeBackpointerEdge.AddRange(NodeBackpointerEdgeTemp);
                            }

                            backpointerNode = _numNodeId + NodeTimeTemp.Count - 1;
                            currentNode = edge.To;
                            foundNext = true;
                            break;
                        }
                    }
                }

            }

            return successors;
        }

        public double h(int partent, int n, bool quaredDistance = false)
        {
            if (!quaredDistance)
            {
                if (NodeBackpointerLastStopId[partent] != NodeBackpointerLastStopId[n])
                    return base.h(n);
                else
                    return base.h(n) - _agent.Physics.getTimeNeededFromFullSpeedToZero() - _agent.Physics.getTimeNeededFromZeroToFullSpeed() + _agent.Physics.getTimeNeededToMove(_agent.Physics.MaxSpeed, _agent.Physics.getDistanceToStop(_agent.Physics.MaxSpeed) + _agent.Physics.getDistanceToFullSpeed(0.0));
            }
            else
            {
                var distance = Math.Sqrt((_graph.PositionX[NodeTo2D(n)] - _graph.PositionX[_agent.DestinationNode]) * (_graph.PositionX[NodeTo2D(n)] - _graph.PositionX[_agent.DestinationNode]));
                return _agent.Physics.getTimeNeededToMove(0, distance);
            }
        }

    }
}
