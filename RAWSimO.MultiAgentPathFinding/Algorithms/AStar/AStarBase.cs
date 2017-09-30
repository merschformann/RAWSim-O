//#define FIB_HEAP_J

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
    /// A* Implementation
    /// </summary>
    public abstract class AStarBase
    {

        /// <summary>
        /// internal queue
        /// 
        /// Operations:
        /// FibonacciHeap<float, int> Q = new FibonacciHeap<float, int>(HeapDirection.Increasing);
        /// Q.Count > 0
        /// Q.Dequeue().Value;
        /// Q.Enqueue(0, node);
        /// Q.ChangeKey(1, node);
        /// </summary>
        //#if FIB_HEAP_J
        //        protected FibonacciHeapJ<int> Q;
        //#else
        protected FibonacciHeap<double, int> Q;
        //#endif

        /// <summary>
        /// open nodes
        /// </summary>
        //#if FIB_HEAP_J
        //public Dictionary<int, FibonacciHeapJ<int>.Entry> Open;
        //#else
        public Dictionary<int, FibonacciHeapHeapCell<double, int>> Open;
        //#endif

        /// <summary>
        /// closed nodes
        /// </summary>
        public HashSet<int> Closed;

        /// <summary>
        /// Stop the search, when this node will be removed from open.
        /// </summary>
        public int GoalNode;

        /// <summary>
        /// start node.
        /// </summary>
        public int StartNode;

        /// <summary>
        /// Initializes a new instance of the <see cref="AStarBase"/> class.
        /// </summary>
        /// <param name="searchDirection">The search direction.</param>
        public AStarBase(int startNode, int goalNode)
        {
            //#if FIB_HEAP_J
            //this.Open = new Dictionary<int, FibonacciHeapJ<int>.Entry>();
            //#else
            this.Open = new Dictionary<int, FibonacciHeapHeapCell<double, int>>();
            //#endif
            this.Closed = new HashSet<int>();
            Clear(startNode, goalNode);
        }

        /// <summary>
        /// Executes the search.
        /// </summary>
        /// <returns>found node</returns>
        public virtual bool Search()
        {
            //local data
            double gPrimeSuccessor;
            double hSuccessor;
            bool successorInOpen;
            bool successorInClosed;

            //open is not empty
            while (Open.Count > 0)
            {
                //get n with lowest value
                int n = Q.Dequeue().Value;

                //set on closed
                Open.Remove(n);
                Closed.Add(n);

                //get successor
                foreach (var successor in Successors(n))
                {
                    //f(successor) = g'(successor) + h(successor)
                    gPrimeSuccessor = gPrime(n, successor);
                    hSuccessor = h(successor);

                    //reject no solution nodes
                    if (hSuccessor == double.PositiveInfinity)
                        continue;

                    //precompute the contains
                    successorInOpen = Open.ContainsKey(successor);
                    successorInClosed = Closed.Contains(successor);

                    if (!successorInOpen && !successorInClosed)
                    {
                        //node is new
                        Open.Add(successor, Q.Enqueue(gPrimeSuccessor + hSuccessor, successor));
                        setBackPointer(n, successor, gPrimeSuccessor, hSuccessor);
                    }
                    else
                    {
                        if (successorInOpen && Open[successor].Priority > gPrimeSuccessor + hSuccessor) //smaller f-Value implies smaller g-Value
                        {
                            //node has better f-Value
                            //Open[successor].Priority = gPrimeSuccessor + hSuccessor;
                            Q.ChangeKey(Open[successor], gPrimeSuccessor + hSuccessor);
                            setBackPointer(n, successor, gPrimeSuccessor, hSuccessor);
                        }
                    }
                }

                //any stop condition
                if (StopCondition(n))
                    return true;

            }

            return false;
        }

        /// <summary>
        /// Condition to stop searching.
        /// </summary>
        /// <param name="n">The expanded node.</param>
        /// <returns></returns>
        protected virtual bool StopCondition(int n)
        {
            //stop at this node => this must be after the expansion, to resume later on
            return n == GoalNode;
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public virtual void Clear(int startNode, int goalNode)
        {
            this.Open.Clear();
            this.Closed.Clear();
            this.StartNode = startNode;
            this.GoalNode = goalNode;

            //#if FIB_HEAP_J
            //            this.Q = new FibonacciHeapJ<int>();
            //#else
            this.Q = new FibonacciHeap<double, int>();
            //#endif

            //initiate open node
            this.Open.Add(startNode, Q.Enqueue(h(startNode), startNode));
        }

        /// <summary>
        /// Sets the back pointer.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="node">The node.</param>
        /// <param name="g">The g value for the node.</param>
        /// <param name="h">The h value for the node.</param>
        protected abstract void setBackPointer(int parent, int node, double g, double h);

        /// <summary>
        /// heuristic value for the node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>h value</returns>
        public abstract double h(int node);

        /// <summary>
        /// g value for the node
        /// </summary>
        /// <param name="successor">The node.</param>
        /// <returns>g value</returns>
        public abstract double g(int node);

        /// <summary>
        /// g value for the node, if the backpointer would come from parent
        /// </summary>
        /// <param name="successor">The node.</param>
        /// <returns>g value</returns>
        public abstract double gPrime(int parent, int node);

        /// <summary>
        /// Successors of the specified n.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <returns>Successors</returns>
        protected abstract IEnumerable<int> Successors(int n);

    }
}
