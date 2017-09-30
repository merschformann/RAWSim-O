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
    /// RRA*
    /// Reference: WHCA* Cooperative Pathﬁnding [Silver 2005]
    /// </summary>
    public class ReverseResumableAStar : AStarBase
    {

        /// <summary>
        /// The agent
        /// </summary>
        protected Agent _agent;

        /// <summary>
        /// The underling Graph
        /// </summary>
        private Graph _graph;

        /// <summary>
        /// physics
        /// </summary>
        private Physics _physics;

        /// <summary>
        /// The edge to use for the given successor.
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
        /// Contains custom blocked nodes.
        /// </summary>
        private HashSet<int> _customBlockedNodes = null;
        /// <summary>
        /// Adds a custom lock for the given node that is not indicated by the graph itself.
        /// </summary>
        /// <param name="lockedNode">The node to lock.</param>
        internal void AddCustomLock(int lockedNode)
        {
            if (_customBlockedNodes == null)
                _customBlockedNodes = new HashSet<int>() { lockedNode };
            else
                _customBlockedNodes.Add(lockedNode);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReverseResumableAStar"/> class.
        /// </summary>
        /// <param name="graph">The graph.</param>
        public ReverseResumableAStar(Graph graph, Agent agent, Physics physics, int destinationNode, HashSet<int> customBlockedNodes = null)
            : base(destinationNode, destinationNode) //backwards search. Swapped start <-> destination. Destination will be set at runtime
        {
            this._physics = physics;
            this._graph = graph;
            this._agent = agent;
            this._customBlockedNodes = customBlockedNodes;
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
            var distance = _graph.getDistance(node, GoalNode); // Math.Sqrt((_graph.PositionX[node] - _graph.PositionX[GoalNode]) * (_graph.PositionX[node] - _graph.PositionX[GoalNode]) + (_graph.PositionY[node] - _graph.PositionY[GoalNode]) * (_graph.PositionY[node] - _graph.PositionY[GoalNode]));
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
            //We are searching backwards in a backwards graph.
            while (_backpointerEdge[lastTurnNode] != null && angle == _backpointerEdge[lastTurnNode].Angle)
            {
                distanceForHop += _backpointerEdge[lastTurnNode].Distance;
                lastTurnNode = _backpointerEdge[lastTurnNode].To; //You might ask: Why backpointer.To? It's OK. Answer: We are searching backwards in a backwards graph.
            }

            //is it the start node?
            if (_backpointerEdge[lastTurnNode] == null)
                angle = _successorEdges[node].Angle; //stop at any orientation at the agent destination (= start node - remember we are here at RRA*)
            else
                angle = _backpointerEdge[lastTurnNode].Angle;

            var currentAngleInRad = Graph.DegreeToRad(angle);
            var targetAngleInRad = Graph.DegreeToRad(_successorEdges[node].Angle);

            //measurement in time!
            //g(node) = time needed to get to "lastTurnNode" + time needed to turn + time needed to get to node
            return _gValues[lastTurnNode] + _physics.getTimeNeededToTurn(currentAngleInRad, targetAngleInRad) + _physics.getTimeNeededToMove(_physics.MaxSpeed, distanceForHop);
        }

        /// <summary>
        /// Successors of the specified n.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <returns>Successors</returns>
        protected override IEnumerable<int> Successors(int n)
        {
            for (int i = 0; i < _graph.BackwardEdges[n].Length; i++)
            {
                var edge = _graph.BackwardEdges[n][i];
                if (// Ensure that the node is not blocked by a custom lock
                    (_customBlockedNodes == null || !_customBlockedNodes.Contains(edge.From)) &&
                    // Ensure that the node is not locked and the agent can either go through obstacles or the node is not an obstacle
                    !edge.FromNodeInfo.IsLocked && (_agent.CanGoThroughObstacles || !edge.FromNodeInfo.IsObstacle) || edge.From == GoalNode)
                {
                    _successorEdges[edge.From] = edge;
                    yield return edge.From;
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
        /// Executes the search.
        /// </summary>
        /// <param name="goalNode">The goal node.</param>
        /// <returns>
        /// found node
        /// </returns>
        public bool Search(int goalNode)
        {
            this.GoalNode = goalNode;

            if (// If the node is blocked by a custom lock we cannot proceed
                (_customBlockedNodes != null && _customBlockedNodes.Contains(StartNode)) ||
                // If the node is locked, we cannot proceed
                _graph.NodeInfo[StartNode].IsLocked ||
                // If the agent cannot go through obstacles and the node is one, we cannot proceed
                (!_agent.CanGoThroughObstacles && _graph.NodeInfo[StartNode].IsObstacle))
                return false;

            //Change Keys in the heap
            foreach (var openNode in Open.Keys)
                Q.ChangeKey(Open[openNode], g(openNode) + h(openNode));

            return base.Search();
        }

        /// <summary>
        /// angle for the node
        /// </summary>
        /// <param name="successor">The node.</param>
        /// <returns>angle</returns>
        public short getAngle(int node)
        {
            if (_backpointerEdge[node] == null)
                throw new Exception("Is goal node!");
            return _backpointerEdge[node].Angle;
        }

        /// <summary>
        /// Next the node in path. Excluding the given node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>Next the node in path.</returns>
        public List<int> NextsNodesUntilTurn(int node)
        {
            var nodes = new List<int>();

            //destination node?
            if (!_backpointerEdge.ContainsKey(node) || _backpointerEdge[node] == null)
                return nodes;

            var currentNode = node;
            var currentAngle = _backpointerEdge[node].Angle;
            nodes.Add(currentNode);
            while (_backpointerEdge[currentNode] != null && Math.Abs(currentAngle - _backpointerEdge[currentNode].Angle) < 2)
            {
                currentNode = _backpointerEdge[currentNode].To;
                nodes.Add(currentNode);
            }

            return nodes;
        }

        /// <summary>
        /// Path contains any of the given blocked nodes.
        /// </summary>
        /// <param name="node">The start node of the path.</param>
        /// <returns>
        /// true, if there are any nodes on the path, that are a member of the blocked nodes
        /// </returns>
        public bool PathContains(int node)
        {
            if (// If the node is blocked by a custom lock, the path is blocked
                (_customBlockedNodes != null && _customBlockedNodes.Contains(node)) ||
                // If the node is locked, the path is blocked
                _graph.NodeInfo[node].IsLocked ||
                // If the agent cannot go through obstacles and the node is one, the path is blocked
                (!_agent.CanGoThroughObstacles && _graph.NodeInfo[node].IsObstacle))
                return true;

            //destination node?
            var currentNode = node;
            while (_backpointerEdge[currentNode] != null)
            {
                currentNode = _backpointerEdge[currentNode].To;
                if (// If the node is blocked by a custom lock, the path is blocked
                    (_customBlockedNodes != null && _customBlockedNodes.Contains(currentNode)) ||
                    // If the node is locked, the path is blocked
                    _graph.NodeInfo[currentNode].IsLocked ||
                    // If the agent cannot go through obstacles and the node is one, the path is blocked
                    (!_agent.CanGoThroughObstacles && _graph.NodeInfo[currentNode].IsObstacle))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Get the path from the node to the destination as a node list.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>
        /// Next the node in path.
        /// </returns>
        public List<int> getPathAsNodeList(int node)
        {
            var nodes = new List<int>();

            //destination node?
            var currentNode = node;
            while (_backpointerEdge[currentNode] != null)
            {
                currentNode = _backpointerEdge[currentNode].To;
                nodes.Add(currentNode);
            }

            return nodes;
        }

        /// <summary>
        /// Next the node in path.
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
        /// Executes the search.
        /// </summary>
        /// <returns>
        /// found node
        /// </returns>
        /// <exception cref="System.Exception">No goal node set!</exception>
        public override bool Search()
        {
            if (this.GoalNode == -1)
                throw new ArgumentException("No goal node set!");
            return Search();
        }

        /// <summary>
        /// Adds the RRA* algorithm nodes.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="StartNode">The start node.</param>
        public void addPath(Path path, int StartNode)
        {
            var currentNode = StartNode;
            var firstAddedNode = true;

            //go through all hops
            while (Closed.Contains(currentNode) && currentNode != this.StartNode)
            { //rraStar StartNode = GoalNode of the Agent

                //get all the nodes to the next hop
                var nextHop = NextsNodesUntilTurn(currentNode);
                for (int i = 0; i < nextHop.Count; i++)
                {
                    //add node
                    path.AddLast(nextHop[i], i == nextHop.Count - 1, 0);

                    //the first node (n) is the connection node. Sometimes a stop at (n - 1) is not necessary
                    if (firstAddedNode && path.Count >= 3)
                    {
                        firstAddedNode = false;
                        path.DeleteStopIfPossible(_graph, path.Count - 2);
                    }
                }

                currentNode = nextHop[nextHop.Count - 1];

            }

            return;
        }
    }
}
