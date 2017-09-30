using RAWSimO.MultiAgentPathFinding.DataStructures;
using RAWSimO.MultiAgentPathFinding.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MultiAgentPathFinding.Algorithms.AStar
{
    /// <summary>
    /// WHCA* - Windowed Hierarchical Cooperated A*
    /// Reference: WHCA* Cooperative Pathﬁnding [Silver 2005]
    /// </summary>
    public class SpaceTimeAStar : AStarBase
    {
        /// <summary>
        /// backpointer of the generated nodes
        /// </summary>
        public List<int> NodeBackpointerId;

        /// <summary>
        /// backpointer of the node, where we turned last time
        /// </summary>
        public List<int> NodeBackpointerLastStopId;

        /// <summary>
        /// The mapping of generated nodes to the 2D Edge
        /// null = Wait Edge
        /// </summary>
        public List<Edge> NodeBackpointerEdge;

        /// <summary>
        /// The mapping of generated nodes to time
        /// NodeTime[100] = point in time of the 100th generated node
        /// NodeTime is the time the agent reaches the node. This is excluding possible rotations.
        /// </summary>
        public List<double> NodeTime;

        /// <summary>
        /// temporary backpointer of the generated nodes
        /// </summary>
        public List<int> NodeBackpointerIdTemp;

        /// <summary>
        /// temporary backpointer of the node, where we turned last time
        /// </summary>
        public List<int> NodeBackpointerLastTurnIdTemp;

        /// <summary>
        /// The temporary mapping of generated nodes to the 2D Edge
        /// null = Wait Edge
        /// </summary>
        public List<Edge> NodeBackpointerEdgeTemp;

        /// <summary>
        /// The temporary mapping of generated nodes to time
        /// NodeTime[100] = point in time of the 100th generated node
        /// </summary>
        public List<double> NodeTimeTemp;

        /// <summary>
        /// The start angle of the agent
        /// </summary>
        public short StartAngle;

        /// <summary>
        /// The wait steps before start
        /// </summary>
        public int WaitStepsBeforeStart = 0;

        /// <summary>
        /// Number of back pointer calls since last successor call
        /// </summary>
        public Dictionary<int, double> BiasedCost;

        /// <summary>
        /// Class is initiated.
        /// </summary>
        protected bool _init = false;

        /// <summary>
        /// Number of generated nodes. Needed for id assignment.
        /// </summary>
        protected int _numNodeId = 0;

        /// <summary>
        /// Length of a time step.
        /// </summary>
        protected double _lengthOfAWaitStep;

        /// <summary>
        /// Length of a window.
        /// </summary>
        protected double _lengthOfAWindow;

        /// <summary>
        /// 2D Graph
        /// </summary>
        protected Graph _graph;

        /// <summary>
        /// Agent
        /// </summary>
        protected Agent _agent;

        /// <summary>
        /// reservation Table
        /// </summary>
        protected ReservationTable _reservationTable;

        /// <summary>
        /// The RRA* Algorithm
        /// </summary>
        protected ReverseResumableAStar _RRAStar;

        /// <summary>
        /// The tie breaking is turned on
        /// </summary>
        protected bool _tieBreaking;

        /// <summary>
        /// The tie breaking times
        /// </summary>
        protected double[] _tieBreakingTimes;

        /// <summary>
        /// A reservation from the end to infinity must be possible
        /// </summary>
        public bool FinalReservation = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpaceTimeAStar"/> class.
        /// </summary>
        public SpaceTimeAStar(Graph graph, double lengthOfAWaitStep, double lengthOfAWindow, ReservationTable reservationTable, Agent agent, ReverseResumableAStar rraStar, bool tieBreaking = true)
            : base(0, -1)
        {
            this._graph = graph;
            this._lengthOfAWaitStep = lengthOfAWaitStep;
            this._lengthOfAWindow = lengthOfAWindow;
            this._reservationTable = reservationTable;
            this._agent = agent;
            this._RRAStar = rraStar;
            this._tieBreaking = tieBreaking;

            //built mappings
            NodeTime = new List<double>();
            NodeBackpointerId = new List<int>();
            NodeBackpointerLastStopId = new List<int>();
            NodeBackpointerEdge = new List<Edge>();
            NodeTimeTemp = new List<double>();
            NodeBackpointerIdTemp = new List<int>();
            NodeBackpointerLastTurnIdTemp = new List<int>();
            NodeBackpointerEdgeTemp = new List<Edge>();

            StartAngle = Graph.RadToDegree(agent.OrientationAtNextNode);

            NodeTime.Add(agent.ArrivalTimeAtNextNode);
            NodeBackpointerId.Add(-1);
            NodeBackpointerLastStopId.Add(0);
            NodeBackpointerEdge.Add(null);
            _numNodeId++;

            _init = true;

            //the node 0 has no assigned value yet.
            Q.ChangeKey(Open[0], h(0));

            if (_tieBreaking)
            {
                _tieBreakingTimes = new double[graph.NodeCount];

                for (var i = 0; i < _tieBreakingTimes.Length; i++)
                    _tieBreakingTimes[i] = -1.0;
            }
        }

        /// <summary>
        /// Sets the back pointer.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="node">The node.</param>
        /// <param name="g">The g value for the node.</param>
        /// <param name="h">The h value for the node.</param>
        protected override void setBackPointer(int parent, int node, double g, double h)
        {
            //no parent discarding - backpointer already set
            //NodeBackpointer[node] = parent;
        }

        /// <summary>
        /// heuristic value for the node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>
        /// h value
        /// </returns>
        public override double h(int node)
        {
            if (!_init)
                return 0;

            var node2d = NodeTo2D(node);

            //already found in RRA*?
            if (_RRAStar.Closed.Contains(node2d))
                return
                    // Costs obtained by RRA*
                    _RRAStar.g(node2d) +
                    // Costs for turning
                    ((node2d == _RRAStar.StartNode) ? 0 : _agent.Physics.getTimeNeededToTurn(Graph.DegreeToRad(GetLastStopAngleAfterTurn(node)), Graph.DegreeToRad(_RRAStar.getAngle(node2d)))) +
                    // Biased costs
                    ((this.BiasedCost == null || !this.BiasedCost.ContainsKey(node2d)) ? 0 : this.BiasedCost[node2d]);
            //find RRA* solution
            if (_RRAStar.Search(node2d))
                return
                    // Costs obtained by RRA*
                    _RRAStar.g(node2d) +
                    // Costs for turning
                    ((node2d == _RRAStar.StartNode) ? 0 : _agent.Physics.getTimeNeededToTurn(Graph.DegreeToRad(GetLastStopAngleAfterTurn(node)), Graph.DegreeToRad(_RRAStar.getAngle(node2d)))) +
                    // Biased costs
                    ((this.BiasedCost == null || !this.BiasedCost.ContainsKey(node2d)) ? 0 : this.BiasedCost[node2d]);
            // No solution
            return double.PositiveInfinity;
        }

        /// <summary>
        /// g value for the node
        /// </summary>
        /// <param name="node"></param>
        /// <returns>
        /// g value
        /// </returns>
        public override double g(int node)
        {
            return NodeTime[node];
        }


        /// <summary>
        /// g value for the node, if the backpointer would come from parent
        /// </summary>
        /// <param name="parent">The temporary parent.</param>
        /// <param name="node">The node.</param>
        /// <returns>
        /// g value
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override double gPrime(int parent, int node)
        {
            //no parent discarding possible
            return NodeTime[node];
        }

        /// <summary>
        /// Condition to stop searching.
        /// </summary>
        /// <param name="n">The expanded node.</param>
        /// <returns></returns>
        protected override bool StopCondition(int n)
        {
            //goal node found or time limit exceeded
            if ((NodeTo2D(n) == _agent.DestinationNode || NodeTime[n] >= _lengthOfAWindow) && (!FinalReservation || _reservationTable.IntersectionFree(NodeTo2D(n), NodeTime[n], double.PositiveInfinity)))
                GoalNode = n;

            //base will check goal node
            return base.StopCondition(n);
        }

        /// <summary>
        /// Successors of the specified n.
        /// Example:
        ///  (i)                    (a')
        ///   |                      |
        ///   v                      v
        ///  (i+1)-->(i+2)-->(n)-->(i+3)-->(i+4)-->(i+5)
        ///                   |      |
        ///                   v      v
        ///                 (i+5)  (i+6)
        ///  Given:
        ///  - n:   the current node
        ///  - i+1: the node of the last turn
        ///  - a' : an other agent that crossed (i+3)
        ///  - i+3: if we would stop here the agent (a') would crash with the current agent. Even if we would directly start again. We have to pass (i+3) asap, to get over it before a'.
        /// 
        ///  Expected result:
        ///  - (n) is a wait successor: backpointer := n | time := time[n] + wait time
        ///  - (i+5) is a successor: backpointer := n | time := time[n] + turn time 90° + drive time one hop
        ///  - (i+4) is a successor because it is the nearest free node to (n) in this direction: backpointer := (i+1) | time := time[i+1] + turn time 90° + drive time 4 hops
        ///</summary>
        /// <param name="n">The n.</param>
        /// <returns>
        /// Successors
        /// </returns>
        protected override IEnumerable<int> Successors(int n)
        {
            // Keep track whether at least one successor was generated
            bool successorGenerated = false;

            //tie breaking => only increasing times
            if (_tieBreaking)
            {
                if (NodeTime[n] <= _tieBreakingTimes[NodeTo2D(n)])
                    yield break;
                else
                    _tieBreakingTimes[NodeTo2D(n)] = NodeTime[n];
            }

            List<int> checkPointNodes = new List<int>();
            List<double> checkPointDistances = new List<double>();
            List<double> checkPointTimes;

            //wait successor
            if (_reservationTable.IntersectionFree(NodeTo2D(n), NodeTime[n], NodeTime[n] + _lengthOfAWaitStep))
            {
                //add successor
                successorGenerated = true;
                NodeTime.Add(NodeTime[n] + _lengthOfAWaitStep);
                NodeBackpointerId.Add(n);
                NodeBackpointerLastStopId.Add(_numNodeId);
                NodeBackpointerEdge.Add(null);
                yield return _numNodeId;
                _numNodeId++;
            }

            if (FinalReservation && NodeTime[n] >= _lengthOfAWindow)
            {
                //only generate wait nodes
                yield break;
            }

            //just create wait successors if possible
            if (WaitStepsBeforeStart > 0 && successorGenerated)
            {
                WaitStepsBeforeStart--;
                yield break;
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
                if (timeToTurn > 0 && !_reservationTable.IntersectionFree(NodeTo2D(lastStopId), NodeTime[lastStopId], NodeTime[lastStopId] + timeToTurn))
                    continue;

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

                            //check if driving action is collision free
                            pathFree = _reservationTable.IntersectionFree(checkPointNodes, checkPointTimes, false);

                            //add node to temp => will be added, if a valid successor will be found
                            NodeTimeTemp.Add(NodeTime[lastStopId] + timeToTurn + timeToMove);
                            NodeBackpointerIdTemp.Add(backpointerNode);
                            NodeBackpointerLastTurnIdTemp.Add(lastStopId);
                            NodeBackpointerEdgeTemp.Add(edge);

                            if (pathFree)
                            {
                                //treat only the last one as successor
                                int succ = _numNodeId + NodeTimeTemp.Count - 1;
                                _numNodeId += NodeTimeTemp.Count;

                                //add temporary successors
                                NodeTime.AddRange(NodeTimeTemp);
                                NodeBackpointerId.AddRange(NodeBackpointerIdTemp);
                                NodeBackpointerLastStopId.AddRange(NodeBackpointerLastTurnIdTemp);
                                NodeBackpointerEdge.AddRange(NodeBackpointerEdgeTemp);

                                // Return it
                                yield return succ;
                            }

                            backpointerNode = _numNodeId + NodeTimeTemp.Count - 1;
                            currentNode = edge.To;
                            foundNext = true;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds the check point distances from 3d nodeFrom to 3d nodeTo.
        /// Warning: No Stops allowed between the nodes.
        /// </summary>
        /// <param name="nodeFrom">The node from.</param>
        /// <param name="nodeTo">The node to.</param>
        /// <param name="checkPointNodes">The check point nodes.</param>
        /// <param name="checkPointDistances">The check point distances.</param>
        public void AddCheckPointDistances(int nodeFrom, int nodeTo, List<int> checkPointNodes, List<double> checkPointDistances)
        {
            //we turn at lastStopId
            var tmpNode = nodeTo;
            var driveDistance = 0.0;

            //skip all previous without angle change
            //search backwards to the last node where the agent has turned
            while (tmpNode != nodeFrom)
            {

                checkPointDistances.Add(driveDistance);
                checkPointNodes.Add(NodeTo2D(tmpNode));

                driveDistance += NodeBackpointerEdge[tmpNode].Distance;

                tmpNode = NodeBackpointerId[tmpNode];
            }
            //add checkpoint
            checkPointDistances.Add(driveDistance);
            checkPointNodes.Add(NodeTo2D(tmpNode));

            //correct the distances
            //we have to fix the order due to backwards search.
            for (int i = 0; i < checkPointDistances.Count; i++)
                checkPointDistances[i] = driveDistance - checkPointDistances[i];
            checkPointNodes.Reverse();
            checkPointDistances.Reverse();
        }

        /// <summary>
        /// Gets the angle of the last stop node after turning.
        /// </summary>
        /// <param name="n">The node.</param>
        /// <returns>angle after turning</returns>
        public short GetLastStopAngleAfterTurn(int n)
        {
            if (NodeBackpointerEdge[n] != null)
                return NodeBackpointerEdge[n].Angle;

            //n is a wait or start node
            var stopNode = n;

            //search for the stop node
            while (NodeBackpointerEdge[stopNode] == null)
            {
                //n is a start node
                if (NodeBackpointerId[stopNode] == -1)
                    return StartAngle;
                stopNode = NodeBackpointerId[stopNode];
            }

            //n was a wait node
            return NodeBackpointerEdge[stopNode].Angle;

        }

        /// <summary>
        /// Convert a node id into the corresponding node id of the graph.
        /// </summary>
        /// <param name="node3d">generated node of WHCA*</param>
        /// <returns>graph node</returns>
        public int NodeTo2D(int node3d)
        {
            var tmpNode = node3d;

            //skip wait nodes
            while (NodeBackpointerEdge[tmpNode] == null)
            {
                if (tmpNode == 0)
                    return _agent.NextNode;
                tmpNode = NodeBackpointerId[tmpNode];
            }

            return NodeBackpointerEdge[tmpNode].To;
        }

        /// <summary>
        /// Adds the WHCA* start nodes.
        /// </summary>
        /// <param name="aStar">a star.</param>
        /// <param name="physics">The physics.</param>
        /// <param name="path">The path.</param>
        /// <param name="reservations">The reservations.</param>
        public void GetPathAndReservations(ref Path path, out List<ReservationTable.Interval> reservations)
        {
            GetPathAndReservations(ref path, out reservations, GoalNode, 0.0);
        }

        /// <summary>
        /// Adds the WHCA* start nodes.
        /// </summary>
        /// <param name="aStar">a star.</param>
        /// <param name="physics">The physics.</param>
        /// <param name="path">The path.</param>
        /// <param name="reservations">The reservations.</param>
        public void GetPathAndReservations(ref Path path, out List<ReservationTable.Interval> reservations, int specifiedNode, double startTime)
        {
            //set Path
            if (path != null)
                path.Clear();
            var node = specifiedNode;
            reservations = new List<ReservationTable.Interval>();

            //add path determined by WHCA*
            while (node >= 0)
            {
                //create action
                var waitTime = 0.0;

                //wait time = stay on one place
                while (node == NodeBackpointerLastStopId[node])
                {
                    if (node == 0)
                        break;

                    waitTime += NodeTime[node] - NodeTime[NodeBackpointerId[node]];
                    node = NodeBackpointerId[node];
                }

                //add wait time to reservation table
                if (waitTime > 0)
                    reservations.Insert(0, new ReservationTable.Interval(NodeTo2D(node), NodeTime[node], NodeTime[node] + waitTime));

                //add action
                if (path != null)
                    path.AddFirst(NodeTo2D(node), true, waitTime);

                //we got to the start node due to wait steps => leave
                if (node == 0)
                    break;

                //get checkpoints
                List<double> checkPointTimes = null;
                var checkpointDistances = new List<double>();
                var checkpointNodes = new List<int>();

                AddCheckPointDistances(NodeBackpointerLastStopId[node], node, checkpointNodes, checkpointDistances);
                var turnTime = _agent.Physics.getTimeNeededToTurn(Graph.DegreeToRad(GetLastStopAngleAfterTurn(node)), Graph.DegreeToRad(GetLastStopAngleAfterTurn(NodeBackpointerLastStopId[node])));
                _agent.Physics.getTimeNeededToMove(0, NodeTime[NodeBackpointerLastStopId[node]] + turnTime, checkpointDistances[checkpointDistances.Count - 1], checkpointDistances, out checkPointTimes);

                for (int i = checkpointNodes.Count - 1; i >= 0; i--)
                {
                    if (i == 0)
                        reservations.Insert(0, new ReservationTable.Interval(checkpointNodes[i], NodeTime[NodeBackpointerLastStopId[node]], checkPointTimes[i + 1])); //you can not take checkPointTimes[i] because the checkPointTimes include the turn
                    else if (i == checkpointNodes.Count - 1)
                        reservations.Insert(0, new ReservationTable.Interval(checkpointNodes[i], checkPointTimes[i - 1], checkPointTimes[i]));
                    else
                        reservations.Insert(0, new ReservationTable.Interval(checkpointNodes[i], checkPointTimes[i - 1], checkPointTimes[i + 1]));

                    //create action for intermediate nodes
                    if (path != null && 0 < i && i < checkpointNodes.Count - 1)
                        path.AddFirst(checkpointNodes[i], false, 0.0);

                    //stop condition
                    if (reservations[0].Start < startTime)
                        break;
                }

                //set next node
                node = NodeBackpointerLastStopId[node];

                //stop condition
                if (reservations[0].Start < startTime)
                    break;
            }

            //rounding differences
            for (int i = reservations.Count - 2; i >= 0; i--)
            {
                if (reservations[i].Node == reservations[i + 1].Node && Math.Abs(reservations[i].End - reservations[i + 1].Start) < 0.001)
                {
                    reservations[i].End = reservations[i + 1].End;
                    reservations.RemoveAt(i + 1);
                }
            }

        }
    }
}
