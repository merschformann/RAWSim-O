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
    /// A* for finding an elevator sequence
    /// </summary>
    public class ElevatorAStar : AStarBase
    {

        /// <summary>
        /// The underling Graph
        /// </summary>
        private Graph _graph;

        /// <summary>
        /// The physic object
        /// </summary>
        private Physics _physics;

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
        public ElevatorAStar(Graph graph, int startNode, int endNode, Physics physics)
            : base(startNode, endNode)
        {
            this._physics = physics;
            this._graph = graph;
            this._successorEdges = new Edge[graph.NodeCount];
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
            var distance = Math.Sqrt((_graph.PositionX[node] - _graph.PositionX[GoalNode]) * (_graph.PositionX[node] - _graph.PositionX[GoalNode]) + (_graph.PositionY[node] - _graph.PositionY[GoalNode]) * (_graph.PositionY[node] - _graph.PositionY[GoalNode]));
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

            var eEdge = _successorEdges[node] as ElevatorEdge;

            //Our aim is to calculate the distance for a hop.
            //A hop is defined as follows: The longest distance between two nodes, where no turn is necessary.
            //The end of the hop is "node". In this method we search for the start ("lastTurnNode") and calculate the g as follows:
            //g(node) = time needed to get to "lastTurnNode" + time needed to turn + time needed to get to node
            var distanceForHop = _successorEdges[node].Distance;
            var timetravelForHop = (eEdge == null) ? 0 : eEdge.TimeTravel;

            //these two variables will hold the information
            var angle = _successorEdges[node].Angle;
            var lastTurnNode = parent;

            //skip all previous without angle change
            //We are searching backwards in a graph.
            while (_backpointerEdge[lastTurnNode] != null && (_backpointerEdge[lastTurnNode] is ElevatorEdge || angle == _backpointerEdge[lastTurnNode].Angle))
            {
                eEdge = _backpointerEdge[lastTurnNode] as ElevatorEdge;

                timetravelForHop += (eEdge == null) ? 0 : eEdge.TimeTravel;
                distanceForHop += _backpointerEdge[lastTurnNode].Distance;
                lastTurnNode = _backpointerEdge[lastTurnNode].From;
            }

            //is it the start node?
            if (_backpointerEdge[lastTurnNode] == null)
                angle = _successorEdges[node].Angle; //stop at any orientation at the agent destination
            else
                angle = _backpointerEdge[lastTurnNode].Angle;

            var currentAngleInRad = Graph.DegreeToRad(angle);
            var targetAngleInRad = Graph.DegreeToRad(_successorEdges[node].Angle);

            //measurement in time!
            //g(node) = time needed to get to "lastTurnNode" + time needed to turn + time needed to get to node
            return _gValues[lastTurnNode] + _physics.getTimeNeededToTurn(currentAngleInRad, targetAngleInRad) + _physics.getTimeNeededToMove(0, distanceForHop) + timetravelForHop;
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
                _successorEdges[_graph.Edges[n][i].To] = _graph.Edges[n][i];
                yield return _graph.Edges[n][i].To;
            }

            //elevator Edges
            if (_graph.ElevatorEdges.ContainsKey(n))
                for (int i = 0; i < _graph.ElevatorEdges[n].Length; i++)
                {
                    _successorEdges[_graph.ElevatorEdges[n][i].To] = _graph.ElevatorEdges[n][i];
                    yield return _graph.ElevatorEdges[n][i].To;
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
        /// Get the path from the node to the destination as a node list.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>
        /// Next the node in path.
        /// </returns>
        public List<Tuple<object, int, int>> getPathAsReferenceList(out double distance)
        {
            var references = new List<Tuple<object, int, int>>();
            distance = 0;

            //go from destination node backwards via back pointer
            var currentNode = GoalNode;
            while (_backpointerEdge[currentNode] != null)
            {
                distance += _backpointerEdge[currentNode].Distance;
                currentNode = _backpointerEdge[currentNode].From;
                if (_backpointerEdge[currentNode] is ElevatorEdge)
                {
                    var eEdge = _backpointerEdge[currentNode] as ElevatorEdge;
                    references.Add(Tuple.Create(eEdge.Reference, eEdge.From, eEdge.To));
                }
            }
            references.Reverse();

            return references;
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
    }
}
