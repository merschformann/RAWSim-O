using RAWSimO.MultiAgentPathFinding.Algorithms.AStar;
using RAWSimO.MultiAgentPathFinding.Algorithms.ID;
using RAWSimO.MultiAgentPathFinding.DataStructures;
using RAWSimO.MultiAgentPathFinding.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MultiAgentPathFinding.Algorithms.OD
{
    /// <summary>
    /// Operator Decomposition - Finding Optimal Solutions to Cooperative Pathﬁnding Problems - T. Standley 2010
    /// </summary>
    public class OperatorDecomposition : AStarBase, IndependenceDetection.LowLevelSolver
    {
        /// <summary>
        /// The number of nodes
        /// </summary>
        public int LastNodeId;

        /// <summary>
        /// backpointer of the generated nodes
        /// </summary>
        public List<int> NodeBackpointerId;

        /// <summary>
        /// backpointer of the node, where we turned last time
        /// </summary>
        public List<Agent> NodeCreatedByAgent;

        /// <summary>
        /// The mapping of generated nodes to the 2D Edge
        /// null = Wait Edge
        /// </summary>
        public List<int> NodeStepNode;

        /// <summary>
        /// Stop the search
        /// </summary>
        private bool _stopSearch;

        /// <summary>
        /// The h values
        /// </summary>
        private List<double> _hValues;

        /// <summary>
        /// The g values
        /// </summary>
        private List<double> _gValues;

        /// <summary>
        /// The _reservation table
        /// </summary>
        private ReservationTable _reservationTable;

        /// <summary>
        /// The _node generator
        /// </summary>
        private Dictionary<int, SpaceTimeAStarSteper> _nodeGenerator;

        /// <summary>
        /// Length of a time step.
        /// </summary>
        private double _lengthOfAWaitStep;

        /// <summary>
        /// Maximum number of nodes to be generated.
        /// </summary>
        private int _maxNodeCount;

        /// <summary>
        /// Maximum number of nodes per Agent to be generated.
        /// </summary>
        private int _maxNodeCountPerAgent;

        /// <summary>
        /// Use final reservations
        /// </summary>
        public bool _useFinalReservations;

        /// <summary>
        /// The length of a wait step
        /// </summary>
        public double _lengthOfAWindow;

        /// <summary>
        /// The start time
        /// </summary>
        public double _startTime;

        /// <summary>
        /// 2D Graph
        /// </summary>
        private Graph _graph;

        /// <summary>
        /// The agents
        /// </summary>
        List<Agent> _agents;

        /// <summary>
        /// The minimum h value for MaxNodeCount exit
        /// </summary>
        private double _minHValue;

        /// <summary>
        /// The minimum h node for MaxNodeCount exit
        /// </summary>
        private int _minHNode;

        /// <summary>
        /// The minimum h time for MaxNodeCount exit
        /// </summary>
        private double _minHTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperatorDecomposition" /> class.
        /// </summary>
        /// <param name="graph">The graph.</param>
        /// <param name="lengthOfAWaitStep">The length of a wait step.</param>
        /// <param name="lengthOfAWindow">The length of a window.</param>
        public OperatorDecomposition(Graph graph, double lengthOfAWaitStep, double lengthOfAWindow, int maxNodeCountPerAgent, bool useFinalReservations)
            : base(0, -1)
        {
            this._lengthOfAWaitStep = lengthOfAWaitStep;
            this._lengthOfAWindow = lengthOfAWindow;
            this._maxNodeCountPerAgent = maxNodeCountPerAgent;
            this._useFinalReservations = useFinalReservations;
            this._graph = graph;
            this._stopSearch = false;
            NodeBackpointerId = new List<int>();
            NodeCreatedByAgent = new List<Agent>();
            NodeStepNode = new List<int>();

            _hValues = new List<double>();
            _gValues = new List<double>();
        }

        public void Init(double currentTime, List<Agent> allAgents)
        {
            _startTime = currentTime;
            _nodeGenerator = new Dictionary<int, SpaceTimeAStarSteper>();

            //create one A* node generator per agent
            foreach (var agent in allAgents)
            {
                _nodeGenerator.Add(agent.ID, new SpaceTimeAStarSteper(_graph, _lengthOfAWaitStep, agent, new AStar.ReverseResumableAStar(_graph, agent, agent.Physics, agent.DestinationNode)));
            }
        }

        /// <summary>
        /// Finds the paths.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        /// <param name="agents">The agents.</param>
        /// <param name="obstacleNodes">The obstacle nodes.</param>
        /// <param name="lockedNodes">The locked nodes.</param>
        /// <returns></returns>
        public List<IndependenceDetection.PlannedPath> FindPaths(List<Agent> agents)
        {
            //set data
            this.Clear(0, -1);
            this._agents = agents;
            this._stopSearch = false;

            //clear data structures
            NodeBackpointerId.Clear();
            NodeCreatedByAgent.Clear();
            NodeStepNode.Clear();
            _hValues.Clear();
            _gValues.Clear();
            _reservationTable = new ReservationTable(_graph, true);

            //initiate data structures
            var plannedPaths = new List<IndependenceDetection.PlannedPath>();

            //create one A* node generator per agent
            foreach (var agent in agents)
                _nodeGenerator[agent.ID].Clear();

            //add first Node
            LastNodeId = 0;
            NodeBackpointerId.Add(-1);
            NodeCreatedByAgent.Add(null);
            NodeStepNode.Add(-1);
            _hValues.Add(_agents.Sum(a => _nodeGenerator[a.ID].h(0)));
            _gValues.Add(0.0);

            _minHValue = _hValues[0];
            _minHNode = 0;
            _minHTime = 0;
            _maxNodeCount = _maxNodeCountPerAgent * agents.Count;

            //execute the search
            this.Search();

            //get the result
            foreach (var agent in agents)
            {
                var plannedPath = new IndependenceDetection.PlannedPath();
                plannedPath.Agent = agent;
                plannedPath.Path = new Path();
                _nodeGenerator[agent.ID].GetPathAndReservations(ref plannedPath.Path, out plannedPath.Reservations);
                plannedPaths.Add(plannedPath);
            }

            return plannedPaths;
        }

        /// <summary>
        /// Sets the back pointer.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="node">The node.</param>
        /// <param name="g">The g.</param>
        /// <param name="h">The h.</param>
        protected override void setBackPointer(int parent, int node, double g, double h)
        {
            //no parent discarding - backpointer set at successor
        }

        /// <summary>
        /// hes the specified node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        public override double h(int node)
        {
            if (_hValues == null || _hValues.Count == 0)
                return 0;

            //calculation needed?
            if (_hValues.Count <= node)
            {
                var agent = this.NodeCreatedByAgent[node];
                //h value of parent - part of current agent in parent + part of current agent in this node
                _hValues.Add(_hValues[NodeBackpointerId[node]] - _nodeGenerator[agent.ID].h(_getStepNode(NodeBackpointerId[node], agent)) + _nodeGenerator[agent.ID].h(_getStepNode(node, agent)));

            }

            //return value
            return _hValues[node];
        }

        /// <summary>
        /// gs the specified node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        public override double g(int node)
        {
            return _gValues[node];
        }

        /// <summary>
        /// gs the prime.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        public override double gPrime(int parent, int node)
        {
            if (node == 0)
                return 0;

            //calculation needed?
            if (_gValues.Count <= node)
            {
                var agent = this.NodeCreatedByAgent[node];
                //g value of parent - part of current agent in parent + part of current agent in this node
                _gValues.Add(_gValues[parent] - _nodeGenerator[agent.ID].g(_getStepNode(parent, agent)) + _nodeGenerator[agent.ID].g(_getStepNode(node, agent)));
            }

            //return value
            return _gValues[node];
        }

        /// <summary>
        /// Successors the specified n.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <returns></returns>
        protected override IEnumerable<int> Successors(int n)
        {
            //next agent => the agent that has the lowest node time
            var minTime = double.MaxValue;
            Agent minAgent = null;
            foreach (var agentCandidate in _agents)
            {
                var nodeTime = _nodeGenerator[agentCandidate.ID].NodeTime[_getStepNode(n, agentCandidate)];
                if (nodeTime < minTime && agentCandidate.DestinationNode != _nodeGenerator[agentCandidate.ID].NodeTo2D(_getStepNode(n, agentCandidate)))
                {
                    minTime = nodeTime;
                    minAgent = agentCandidate;
                }
            }

            //no further agents found, that have to be moved to find the destination
            //currentNode2d = _nodeGenerator[minAgent.ID].NodeTo2D(_getStepNode(n, minAgent))
            if (minAgent == null)
            {
                foreach (var finishedAgent in _agents)
                    _nodeGenerator[finishedAgent.ID].GoalNode = _getStepNode(n, finishedAgent);

                //found optimal solution
                _stopSearch = true;
                yield break;
            }

            //set reservation table
            setReservations(minAgent, n, minTime);
            _nodeGenerator[minAgent.ID].ReservationTable = _reservationTable;

            //generate next nodes
            var nextStepNodes = _nodeGenerator[minAgent.ID].Step(_getStepNode(n, minAgent));

            foreach (var nextStepNode in nextStepNodes)
            {
                //add next node
                LastNodeId++;
                NodeBackpointerId.Add(n);
                NodeCreatedByAgent.Add(minAgent);
                NodeStepNode.Add(nextStepNode);
                yield return LastNodeId;
            }
        }

        /// <summary>
        /// Sets the reservations.
        /// </summary>
        /// <param name="agent">The agent that wants to move.</param>
        /// <param name="startTime">The start time.</param>
        private void setReservations(Agent agent, int node, double startTime)
        {
            _reservationTable.Clear();

            foreach (var otherAgent in _agents)
            {
                //only reservations for other agents
                if (otherAgent == agent)
                    continue;

                List<ReservationTable.Interval> reservations;
                Path path = null;
                _nodeGenerator[otherAgent.ID].GetPathAndReservations(ref path, out reservations, _getStepNode(node, otherAgent), startTime);

                if (_useFinalReservations)
                {
                    if (reservations.Count > 0)
                        reservations.Last().End = double.PositiveInfinity;
                    else
                        reservations.Add(new ReservationTable.Interval(otherAgent.NextNode, otherAgent.ArrivalTimeAtNextNode, double.PositiveInfinity));

                    if (_reservationTable.IntersectionFree(reservations))
                        _reservationTable.Add(reservations);
                }
                else
                {
                    //initiate this step node and time
                    _reservationTable.Add(reservations);
                }

                //reservation of the next hop
                var thisStepNode = otherAgent.ReservationsToNextNode.Count - 1;
                while (thisStepNode >= 0 && otherAgent.ReservationsToNextNode[thisStepNode].End > startTime)
                {
                    if (!_useFinalReservations || _reservationTable.IntersectionFree(otherAgent.ReservationsToNextNode[thisStepNode]))
                        _reservationTable.Add(otherAgent.ReservationsToNextNode[thisStepNode]);
                    thisStepNode--;
                }

            }
        }

        /// <summary>
        /// Gets the step node for the agent.
        /// </summary>
        /// <param name="node">The node of OD.</param>
        /// <param name="agent">The agent.</param>
        /// <returns>step node</returns>
        private int _getStepNode(int node, Agent agent)
        {

            var currcentNode = node;
            while (NodeBackpointerId[currcentNode] != -1 && NodeCreatedByAgent[currcentNode].ID != agent.ID)
                currcentNode = NodeBackpointerId[currcentNode];

            return NodeBackpointerId[currcentNode] == -1 ? 0 : NodeStepNode[currcentNode];

        }

        /// <summary>
        /// Condition to stop searching.
        /// </summary>
        /// <param name="n">The expanded node.</param>
        /// <returns></returns>
        protected override bool StopCondition(int n)
        {
            if (n == 0)
                return false;

            var nodeTime = _nodeGenerator[NodeCreatedByAgent[n].ID].NodeTime[_getStepNode(n, NodeCreatedByAgent[n])];

            //set the minimum h node
            if ((nodeTime - _startTime) > (_minHTime - _startTime) * 2.0 || _hValues[n] < _minHValue)
            {
                _minHValue = _hValues[n];
                _minHNode = n;
                _minHTime = nodeTime;
            }

            //stop condition 1: goal node found
            if (_stopSearch)
            {
                GoalNode = n;
                foreach (var agent in _agents)
                    _nodeGenerator[agent.ID].GoalNode = _getStepNode(GoalNode, agent);
                return true;
            }

            //stop condition 2:  time limit exceeded
            if (nodeTime > _startTime + _lengthOfAWindow)
            {
                GoalNode = n;
                foreach (var agent in _agents)
                    _nodeGenerator[agent.ID].GoalNode = _getStepNode(GoalNode, agent);
                return true;
            }

            //stop condition 3: maximum node count exceeded
            if (LastNodeId > _maxNodeCount)
            {
                GoalNode = _minHNode;
                foreach (var agent in _agents)
                    _nodeGenerator[agent.ID].GoalNode = _getStepNode(GoalNode, agent);
                return true;
            }

            //base will check goal node
            return base.StopCondition(n);
        }

    }
}
