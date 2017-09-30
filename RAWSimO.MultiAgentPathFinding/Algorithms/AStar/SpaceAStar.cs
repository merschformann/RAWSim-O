using RAWSimO.MultiAgentPathFinding.DataStructures;
using RAWSimO.MultiAgentPathFinding.Elements;
using RAWSimO.MultiAgentPathFinding.Physic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MultiAgentPathFinding.Algorithms.AStar
{
    /// <summary>
    /// A*
    /// </summary>
    public class SpaceAStar : AStarBase
    {
        /// <summary>
        /// The agent.
        /// </summary>
        private Agent _agent;

        /// <summary>
        /// The underling Graph
        /// </summary>
        private Graph _graph;

        /// <summary>
        /// The physic object
        /// </summary>
        private Physics _physics;

        /// <summary>
        /// The physic object
        /// </summary>
        private short _startAngle;

        /// <summary>
        /// Number of back pointer calls since last successor call
        /// </summary>
        private Dictionary<int, double> _biasedCost;

        /// <summary>
        /// The edges to use for the given successors.
        /// </summary>
        private Edge[] _successorEdges;

        /// <summary>
        /// backpointer
        /// </summary>
        private Dictionary<int, Edge> _backpointerEdge;

        /// <summary>
        /// g values
        /// </summary>
        private Dictionary<int, double> _gValues;

        /// <summary>
        /// class is loaded
        /// </summary>
        private bool _init = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReverseResumableAStar"/> class.
        /// </summary>
        /// <param name="graph">The graph.</param>
        public SpaceAStar(Graph graph, int startNode, int endNode, double orientation, Physics physics, Agent agent, Dictionary<int, double> biasedCost = null)
            : base(startNode, endNode)
        {
            this._physics = physics;
            this._agent = agent;
            this._graph = graph;
            this._biasedCost = biasedCost;
            this._startAngle = Graph.RadToDegree(orientation);
            this._successorEdges = new Edge[_graph.NodeCount];
        }

        /// <summary>
        /// heuristic value for the node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>h value</returns>
        public override double h(int node)
        {
            if (!_init)
                return 0;

            //Here is a possible improvement: Add the rotation
            //Advantage: A* is more informed => will be expand less nodes => faster
            //Disadvantage: Need to Calculate the current angle and the angle to the goal node => slower
            var distance = Math.Sqrt((_graph.PositionX[node] - _graph.PositionX[GoalNode]) * (_graph.PositionX[node] - _graph.PositionX[GoalNode]) + (_graph.PositionY[node] - _graph.PositionY[GoalNode]) * (_graph.PositionY[node] - _graph.PositionY[GoalNode])) + ((this._biasedCost == null || !this._biasedCost.ContainsKey(node)) ? 0 : this._biasedCost[node]);
            return _physics.getTimeNeededToMove(0, distance);
        }

        /// <summary>
        /// g value for the node
        /// </summary>
        /// <param name="successor">The node.</param>
        /// <returns>g value</returns>
        public override double g(int node)
        {
            return _gValues[node];
        }

        /// <summary>
        /// g value for the node, if the backpointer would come from parent
        /// </summary>
        /// <param name="parent">The temporary parent.</param>
        /// <param name="node">The node.</param>
        /// <returns>
        /// g value
        /// </returns>
        public override double gPrime(int parent, int node)
        {
            //by definition: g(s) = 0
            if (node == StartNode)
                return 0;

            //Our aim is to calculate the distance for a hop.
            //A hop is defined as follows: The longest distance between two nodes, where no turn is necessary.
            //The end of the hop is "node". In this method we search for the start ("lastTurnNode") and calculate the g as follows:
            //g(node) = time needed to get to "lastTurnNode" + time needed to turn + time needed to get to node
            var distanceForHop = _successorEdges[node].Distance;

            //these two variables will hold the information
            var angle = _successorEdges[node].Angle;
            var lastTurnNode = parent;

            //skip all previous without angle change
            while (_backpointerEdge[lastTurnNode] != null && angle == _backpointerEdge[lastTurnNode].Angle)
            {
                distanceForHop += _backpointerEdge[lastTurnNode].Distance;
                lastTurnNode = _backpointerEdge[lastTurnNode].From;
            }

            //is it the start node?
            if (_backpointerEdge[lastTurnNode] == null)
                angle = _startAngle;
            else
                angle = _backpointerEdge[lastTurnNode].Angle;

            var currentAngleInRad = Graph.DegreeToRad(angle);
            var targetAngleInRad = Graph.DegreeToRad(_successorEdges[node].Angle);

            //measurement in time!
            //g(node) = time needed to get to "lastTurnNode" + time needed to turn + time needed to get to node
            return _gValues[lastTurnNode] + _physics.getTimeNeededToTurn(currentAngleInRad, targetAngleInRad) + _physics.getTimeNeededToMove(0, distanceForHop);
        }

        /// <summary>
        /// Successors of the specified n.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <returns>Successors</returns>
        protected override IEnumerable<int> Successors(int n)
        {
            //normal Edges
            for (int i = 0; i < _graph.Edges[n].Length; i++)
            {
                var edge = _graph.Edges[n][i];
                if (!edge.ToNodeInfo.IsLocked && (_agent.CanGoThroughObstacles || !edge.ToNodeInfo.IsObstacle))
                {
                    _successorEdges[edge.To] = edge;
                    yield return edge.To;
                }
            }
        }

        /// <summary>
        /// Sets the back pointer.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <param name="successor">The successor.</param>
        /// <param name="g">The g value for n.</param>
        /// <param name="h">The h value for n.</param>
        protected override void setBackPointer(int parent, int node, double g, double h)
        {
            //get edge
            _backpointerEdge[node] = _successorEdges[node];

            //save g value
            _gValues[node] = g;

        }

        /// <summary>
        /// clear the instance.
        /// </summary>
        public override void Clear(int startNode, int goalNode)
        {
            base.Clear(startNode, goalNode);
            _backpointerEdge = new Dictionary<int, Edge>();
            _gValues = new Dictionary<int, double>();

            //by definition
            _gValues.Add(startNode, 0);
            _backpointerEdge.Add(startNode, null);

            _init = true;
        }


        /// <summary>
        /// Gets the reservations and path.
        /// </summary>
        /// <param name="startTime">The start time.</param>
        /// <param name="path">The path.</param>
        /// <param name="reservations">The reservations.</param>
        public void getReservationsAndPath(double startTime, ref Path path, out List<ReservationTable.Interval> reservations)
        {
            //helper for calculation
            var intervallBuilderHelper = new ReservationTable(_graph);

            //output variables
            reservations = new List<ReservationTable.Interval>();

            //get the sequence of edges from the start to the goal node
            var edgesPath = new List<Edge>();
            var currentNode = GoalNode;
            while (_backpointerEdge[currentNode] != null)
            {
                edgesPath.Add(_backpointerEdge[currentNode]);
                currentNode = _backpointerEdge[currentNode].From;
            }
            edgesPath.Reverse(); //the path was created backwards, so we have to turn it know

            //to from the start to the end and create the intervals
            var currentTime = startTime;
            var currentAngle = _startAngle;
            for (int edgeNo = 0; edgeNo < edgesPath.Count;)
            {
                var checkPointNodes = new List<int>();
                var checkPointDistances = new List<double>();

                //start node for hop
                checkPointNodes.Add(edgesPath[edgeNo].From);
                checkPointDistances.Add(0);

                //skip to an edge that points in a different direction
                var currentDistance = 0.0;
                while (edgeNo < edgesPath.Count && edgesPath[edgeNo].Angle == currentAngle)
                {
                    currentDistance += edgesPath[edgeNo].Distance;
                    checkPointNodes.Add(edgesPath[edgeNo].To);
                    checkPointDistances.Add(currentDistance);
                    edgeNo++;
                }

                //add checkpoint node to the past. Leave the last one out, because the check point nodes overlap in
                //every iteration. Example:
                // 1. Iteration: 1 - 2 - 3 (turn)
                // 2. Iteration:         3 - 4 - 5 (turn) ...
                for (int i = 0; i < checkPointNodes.Count - 1; i++)
                    path.AddLast(checkPointNodes[i], (i == 0), 0.0);

                //add reservations for driving among the nodes
                if (checkPointNodes.Count > 1)
                {
                    //add reservations
                    List<double> checkPointTimes = null;
                    _physics.getTimeNeededToMove(0.0, currentTime, currentDistance, checkPointDistances, out checkPointTimes);
                    reservations.AddRange(intervallBuilderHelper.CreateIntervals(checkPointTimes[0], checkPointNodes, checkPointTimes, false));

                    //move time forward
                    currentTime = checkPointTimes[checkPointTimes.Count - 1];
                }

                //add reservation for turning ahead the next node
                if (edgeNo < edgesPath.Count)
                {
                    var timeNeededToTurn = _physics.getTimeNeededToTurn(Graph.DegreeToRad(currentAngle), Graph.DegreeToRad(edgesPath[edgeNo].Angle));
                    currentAngle = edgesPath[edgeNo].Angle;

                    //create reservation for turn
                    reservations.Add(new ReservationTable.Interval(edgesPath[edgeNo].From, currentTime, currentTime + timeNeededToTurn));
                    currentTime += timeNeededToTurn;
                }
            }

            //add destination
            path.AddLast(GoalNode, true, 0.0);
        }

        /// <summary>
        /// Changes the biased cost.
        /// </summary>
        /// <param name="biasedCost">The new biased cost.</param>
        public void changeBiasedCost(Dictionary<int, double> biasedCost)
        {
            _biasedCost = biasedCost;
        }
    }
}
