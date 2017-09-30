using RAWSimO.MultiAgentPathFinding.Elements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MultiAgentPathFinding.DataStructures
{
    /// <summary>
    /// conflict tree for CBS
    /// </summary>
    public class ConflictTree
    {
        /// <summary>
        /// Gets or sets the root.
        /// </summary>
        /// <value>
        /// The root.
        /// </value>
        public Node Root { get; set; }

        /// <summary>
        /// Size of this instance.
        /// </summary>
        /// <returns></returns>
        public int Size
        {
            get
            {
                var size = 0;
                var stack = new Stack<Node>();
                stack.Push(Root);
                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    size++;
                    foreach (var child in current.Children)
                        if (child != null)
                            stack.Push(child);
                }
                return size;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictTree"/> class.
        /// </summary>
        /// <param name="rootChildrenCount">The root children count.</param>
        public ConflictTree(int rootChildrenCount = 2)
        {
            Root = new Node(-1, null, null, rootChildrenCount);
        }

        #region Node
        /// <summary>
        /// A Node
        /// </summary>
        public class Node
        {
            /// <summary>
            /// The parent
            /// </summary>
            public Node Parent;

            /// <summary>
            /// The children
            /// </summary>
            public Node[] Children;

            /// <summary>
            /// The interval constraint
            /// </summary>
            public ReservationTable.Interval IntervalConstraint;

            /// <summary>
            /// The solution of the node.
            /// AgentId => Path
            /// </summary>
            private Dictionary<int, Path> _solution;

            /// <summary>
            /// The reservations of the agent.
            /// AgentId => Reservation
            /// </summary>
            private Dictionary<int, List<ReservationTable.Interval>> _reservation;

            /// <summary>
            /// The solution cost of the node.
            /// </summary>
            public double SolutionCost;

            /// <summary>
            /// The solution is valid.
            /// </summary>
            public bool SolutionValid;

            /// <summary>
            /// The constraint is valid for this agent
            /// </summary>
            public int AgentId;

            /// <summary>
            /// The depth of the node
            /// </summary>
            public int Depth;

            /// <summary>
            /// Initializes a new instance of the <see cref="Node"/> class.
            /// </summary>
            /// <param name="childrenCount">The children count.</param>
            public Node(int agentId, ReservationTable.Interval constraint, Node parent, int childrenCount = 2)
            {
                Children = new Node[childrenCount];
                AgentId = agentId;
                IntervalConstraint = constraint;
                _solution = new Dictionary<int, Path>();
                _reservation = new Dictionary<int, List<ReservationTable.Interval>>();

                //set backpointer to parent
                if (parent != null)
                {
                    parent._addChild(this);
                    Parent = parent;
                    Depth = parent.Depth + 1;
                }
                else
                {
                    Depth = 0;
                }

                Debug.Assert(validate());
            }

            /// <summary>
            /// Adds the child and sets the backpointer to parent.
            /// </summary>
            /// <param name="child">The child.</param>
            private void _addChild(Node child)
            {
                //add child on first free position
                for (int i = 0; i < Children.Length; i++)
                {
                    if (Children[i] == null)
                    {
                        Children[i] = child;
                        break;
                    }
                }
            }

            /// <summary>
            /// Sets the solution for this node.
            /// </summary>
            /// <param name="agentId">The agent identifier.</param>
            /// <param name="path">The path.</param>
            /// <param name="intervals">The intervals.</param>
            public void setSolution(int agentId, Path path, List<ReservationTable.Interval> intervals)
            {
                _solution[agentId] = path;
                _reservation[agentId] = intervals;
                
                //root?
                if (Parent == null)
                {
                    //root node => Sum over all agents
                    SolutionCost = 0.0;
                    foreach (var key in _reservation.Keys)
                        SolutionCost += (_reservation[key].Count == 0) ? 0 : _reservation[key][_reservation[key].Count - 1].End - _reservation[key][0].Start;
                }
                else
                {
                    //child node => old solution cost - old agent cost + new agent cost = new solution cost
                    SolutionCost = Parent.SolutionCost - Parent._getCostOf(agentId) + ((intervals.Count == 0) ? 0 : (intervals[intervals.Count - 1].End - intervals[0].Start));
                }
            }

            /// <summary>
            /// Gets the cost of an agent.
            /// </summary>
            /// <param name="agentId">The agent identifier.</param>
            /// <returns>cost </returns>
            private double _getCostOf(int agentId)
            {
                var agentReservations = getReservation(agentId);
                return (agentReservations == null || agentReservations.Count == 0) ? 0 : agentReservations[agentReservations.Count - 1].End - agentReservations[0].Start;
            }

            /// <summary>
            /// Gets the solution for this node.
            /// </summary>
            /// <param name="agentId">The agent identifier.</param>
            /// <returns>path</returns>
            public Path getSolution(int agentId)
            {
                var currentNode = this;
                while (currentNode != null)
                {
                    //has this node a solution?
                    if (currentNode._solution.ContainsKey(agentId))
                        return currentNode._solution[agentId];

                    //this is an invariant
                    //The nodes only stores the solution for the agent where we added a constraint.
                    //The root node stores all solutions.
                    //if we have to go up although this node has a constraint for agentId from the parameters, something went horribly wrong.
                    Debug.Assert(currentNode.AgentId != agentId);

                    //no! => go up
                    currentNode = currentNode.Parent;
                }

                // No solution - return empty path
                return new Path();
            }

            /// <summary>
            /// Gets the solution for this node.
            /// </summary>
            /// <param name="agentId">The agent identifier.</param>
            /// <returns>
            /// reservation
            /// </returns>
            public List<ReservationTable.Interval> getReservation(int agentId)
            {
                var currentNode = this;
                while (currentNode != null)
                {
                    //has this node a solution?
                    if (currentNode._reservation.ContainsKey(agentId))
                        return currentNode._reservation[agentId];

                    //this is an invariant
                    //The nodes only stores the solution for the agent where we added a constraint.
                    //The root node stores all solutions.
                    //if we have to go up although this node has a constraint for agentId from the parameters, something went horribly wrong.
                    Debug.Assert(currentNode.AgentId != agentId);

                    //no! => go up
                    currentNode = currentNode.Parent;
                }

                return null;
            }

            /// <summary>
            /// Gets the constraints for a specific agent.
            /// </summary>
            /// <param name="agentID">The agent identifier.</param>
            /// <returns></returns>
            public IEnumerable<ConflictTree.Node> getConstraints(int agentId)
            {
                return new ConstraintCollector(this, agentId);
            }

            /// <summary>
            /// Returns a <see cref="System.String" /> that represents this instance.
            /// </summary>
            /// <returns>
            /// A <see cref="System.String" /> that represents this instance.
            /// </returns>
            public string printStack()
            {
                var b = new StringBuilder();

                var current = this;
                do
                {
                    if (current.IntervalConstraint != null)
                        b.Append("Agent ").Append(current.AgentId).Append(": (").Append(current.IntervalConstraint.Node).Append(") ").Append(current.IntervalConstraint.Start).Append(" - ").Append(current.IntervalConstraint.End).Append(Environment.NewLine);
                    current = current.Parent;
                } while (current != null);

                return b.ToString();
            }

            /// <summary>
            /// Validates this instance.
            /// </summary>
            /// <returns>true, if intervals are overlapping free</returns>
            public bool validate()
            {
                if (AgentId == -1)
                    return true;

                //get maximum node id
                var maxNode = 0;
                foreach (var reservation in getConstraints(AgentId))
                {
                    maxNode = Math.Max(maxNode, reservation.IntervalConstraint.Node);
                }

                //create Table
                var table = new ReservationTable(new Graph(maxNode + 1));

                //add all reservations
                try
                {
                    foreach (var reservation in getConstraints(AgentId))
                        table.Add(reservation.IntervalConstraint);
                }
                catch (DisjointIntervalTree.IntervalIntersectionException)
                {
                    return false;
                }

                return true;
            }

            #region Enumerable
            /// <summary>
            /// Collects all the constraint for a specific agent
            /// </summary>
            public class ConstraintCollector : IEnumerable<ConflictTree.Node>
            {

                /// <summary>
                /// The start node
                /// </summary>
                private Node _startNode;

                /// <summary>
                /// The agent identifier
                /// </summary>
                private int _agentId;

                /// <summary>
                /// Initializes a new instance of the <see cref="ConstraintCollector"/> class.
                /// </summary>
                /// <param name="conflictNode">The conflict node.</param>
                /// <param name="agentId">The agent identifier.</param>
                public ConstraintCollector(Node conflictNode, int agentId)
                {
                    this._startNode = conflictNode;
                    this._agentId = agentId;
                }


                /// <summary>
                /// Returns an enumerator that iterates through the collection.
                /// </summary>
                /// <returns>
                /// A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
                /// </returns>
                /// <exception cref="System.NotImplementedException"></exception>
                public IEnumerator<ConflictTree.Node> GetEnumerator()
                {
                    return new ConstraintEnumerator(_startNode, _agentId);
                }

                /// <summary>
                /// Returns an enumerator that iterates through a collection.
                /// </summary>
                /// <returns>
                /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
                /// </returns>
                IEnumerator IEnumerable.GetEnumerator()
                {
                    return this.GetEnumerator();
                }

                #region Enumerator

                /// <summary>
                /// Enumerator of constraint
                /// </summary>
                public class ConstraintEnumerator : IEnumerator<ConflictTree.Node>
                {

                    /// <summary>
                    /// The start node
                    /// </summary>
                    private Node _startNode;

                    /// <summary>
                    /// First Call of MoveNext
                    /// </summary>
                    private bool _firstCall;

                    /// <summary>
                    /// The start node
                    /// </summary>
                    private Node _currentNode;

                    /// <summary>
                    /// The agent identifier
                    /// </summary>
                    private int _agentId;

                    /// <summary>
                    /// Initializes a new instance of the <see cref="ConstraintCollector"/> class.
                    /// </summary>
                    /// <param name="conflictNode">The conflict node.</param>
                    /// <param name="agentId">The agent identifier.</param>
                    public ConstraintEnumerator(Node conflictNode, int agentId)
                    {
                        this._currentNode = conflictNode;
                        this._startNode = conflictNode;
                        this._agentId = agentId;
                        this._firstCall = true;
                    }

                    /// <summary>
                    /// Gets the element in the collection at the current position of the enumerator.
                    /// </summary>
                    /// <exception cref="System.NotImplementedException"></exception>
                    public ConflictTree.Node Current
                    {
                        get { return this._currentNode; }
                    }

                    /// <summary>
                    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
                    /// </summary>
                    /// <exception cref="System.NotImplementedException"></exception>
                    public void Dispose()
                    { }

                    /// <summary>
                    /// Gets the element in the collection at the current position of the enumerator.
                    /// </summary>
                    /// <exception cref="System.NotImplementedException"></exception>
                    object IEnumerator.Current
                    {
                        get { return this._currentNode; }
                    }

                    /// <summary>
                    /// Advances the enumerator to the next element of the collection.
                    /// </summary>
                    /// <returns>
                    /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
                    /// </returns>
                    /// <exception cref="System.NotImplementedException"></exception>
                    public bool MoveNext()
                    {
                        if (_currentNode == null)
                            return false;

                        //go one up
                        if (_firstCall)
                            _firstCall = false;
                        else
                            _currentNode = _currentNode.Parent;

                        //only nodes with matching agent id
                        while (_currentNode != null && _currentNode.AgentId != _agentId)
                            _currentNode = _currentNode.Parent;


                        return _currentNode != null;
                    }

                    /// <summary>
                    /// Sets the enumerator to its initial position, which is before the first element in the collection.
                    /// </summary>
                    /// <exception cref="System.NotImplementedException"></exception>
                    public void Reset()
                    {
                        _currentNode = _startNode;
                        _firstCall = true;
                    }
                }

                #endregion
            }
            #endregion
        }
        #endregion

    }
}
