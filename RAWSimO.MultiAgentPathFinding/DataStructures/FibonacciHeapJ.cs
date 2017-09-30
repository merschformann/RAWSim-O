using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MultiAgentPathFinding.DataStructures
{
    /**
    * This class implements a Fibonacci heap. Much of the code in this class is
    * based on the algorithms in the "Introduction to Algorithms" by Cormen,
    * Leiserson, and Rivest in Chapter 21.
    * 
    * The amortized cost of most of these methods is O(1), making it a very fast
    * data structure. Several have an actual running time of O(1). extractMin()
    * and delete() have O(log n) amortized running times because they do the heap
    * consolidation.
    * 
    * @author Yifan Peng
    * @version 09/06/2011
    */
    public class FibonacciHeapJ<T>
    {

        private double oneOverLogPhi = 1.0 / Math.Log((1.0 + Math.Sqrt(5.0)) / 2.0);

        /**
         * Points to the root of a tree containing a minimum key. If the heap is
         * empty, then min = NIL.
         */
        private Entry min;

        /**
         * The number of nodes currently in the heap.
         */
        private int n;

        /**
         *  The number of nodes currently in the heap.
         */
        public int Count { get { return n; } }

        /**
         * Creates and returns a new heap containing no elements.
         */
        public FibonacciHeapJ()
        {
            min = null;
        }

        /**
         * Assigns to node x within heap the new key value k, which is assumed to be
         * no greater than its current key value.
         * <p>
         * Amortized cost: O(1)
         * 
         * @param x node to decrease the key of
         * @param key new key value for node x
         * 
         * @throws IllegalArgumentException if k is larger than x.key value.
         */
        public void ChangeKey(Entry x, double key)
        {

            // Ensure that the new key is no greater than the current key of x and
            // then assign the new key to x
            if (key > x.Priority)
            {
                throw new Exception("new key is greater than current key: " + key + ">" + x.Priority);
            }
            x.Priority = key;

            // Tests if x is a root or if key[x]<=key[y], where y is x's parent. If
            // so, no structural changes need occur, since min-heap order has not
            // been violated.
            Entry y = x.parent;
            if ((y != null) && (x.Priority < y.Priority))
            {
                // Cuts the link between x and its parent y, making x a root.
                cut(x, y);
                cascadingCut(y);
            }

            if (x.Priority < min.Priority)
            {
                min = x;
            }
        }

        /**
         * Cut the link between x and its parent y, making x a root.
         * 
         * Running time: O(1)
         * 
         * @param x child of y to be removed from y's child list
         * @param y parent of x about to lose a child
         */
        protected void cut(Entry x, Entry y)
        {
            // Remove x from child list of y, decrementing degree[y].
            y.child = removeNode(y.child, x);
            y.degree--;

            // Add x to the root list of H.
            min = concatenateNode(min, x);
            x.parent = null;

            // Clears mark, since it performs step 1.
            x.mark = false;
        }

        /**
         * Cut y from its parent and recurses its way up the tree until either a root
         * or an unmarked node is found.
         * 
         * Running time: O(1) exclusive of recursive calls.
         * 
         * @param y node to perform cascading cut on
         */
        private void cascadingCut(Entry y)
        {
            Entry z = y.parent;
            if (z != null)
            {
                // If y is unmarked, mark it, since its first child has just been
                // cut.
                if (!y.mark)
                {
                    y.mark = true;
                }
                else
                {
                    // y has just lost its second child.
                    cut(y, z);
                    cascadingCut(z);
                }
            }
        }

        /**
         * Deletes node x from heap. Assume there is no key value of -NUB_VALUE
         * currently in the heap.
         * 
         * Amortized cost: O(log n)
         * 
         * @param x node to remove from heap
         */
        public void delete(Entry x)
        {
            ChangeKey(x, Int32.MinValue);
            Dequeue();
        }


        public Entry Enqueue(double priority, T value)
        {
            var entry = new Entry(priority, value);
            insert(entry);
            return entry;
        }

        /**
         * Inserts node x, whose key field has already been filled in, into heap.
         * 
         * Actual cost: O(1)
         * 
         * Amortized cost: O(1)
         * 
         * @param x new node to insert into heap
         */
        protected void insert(Entry x)
        {

            // Initialize the structural fields of node x, making it its own
            // circular, double linked list.
            x.degree = 0;
            x.parent = null;
            x.child = null;
            x.left = x;
            x.right = x;
            x.mark = false;

            // Concatenate the root list containing x with root list.
            min = concatenateNode(min, x);

            // Update the pointer to the minimum node of the heap if necessary.
            if (min == null || x.Priority < min.Priority)
            {
                min = x;
            }

            // Increments n to reflect the addition of the new node.
            n++;
        }

        private Entry concatenateNode(Entry list, Entry node)
        {
            if (list == null)
            {
                node.left = node;
                node.right = node;
                return node;
            }
            else
            {
                node.left = list;
                node.right = list.right;
                list.right = node;
                node.right.left = node;
                return list;
            }
        }

        /**
         * Returns a pointer to the node in heap whose key is minimum.
         * 
         * <p>
         * Running time: O(1) actual
         * </p>
         * 
         * @return a pointer to the node in heap whose key is minimum
         */
        public Entry Top
        {
            get { return min; }
        }

        /**
         * Deletes the node from heap whose key is minimum, returning a pointer to
         * the node.
         * 
         * Amortized cost: O(log n)
         * 
         * @return a pointer to the node in heap whose key is minimum
         */
        public Entry Dequeue()
        {

            // Save a pointer z to the minimum node.
            Entry z = min;

            if (z != null)
            {
                if (z.child != null)
                {
                    // Make all of z's children roots of the heap.
                    foreach (Entry x in z.child.nodelist())
                    {
                        min = concatenateNode(min, x);
                        x.parent = null;
                    }
                }

                // Remove z from the root list.
                min = removeNode(min, z);

                if (z == z.right)
                {
                    // z was the only node on the root list and it had no children,
                    // so all that remains is to make the heap empty.
                    min = null;
                }
                else
                {
                    // Set the pointer min into the root list to point to a node
                    // other than z, which is not necessarily going to be the new
                    // minimum node when this method is done.
                    min = z.right;

                    // Reduce the number of trees in the heap.
                    consolidate();
                }

                // Decrease n.
                n--;
            }
            return z;

        }

        /**
         * Reduce the number of trees in the heap.
         */
        private void consolidate()
        {

            // Initialize array by making each entry NIL.
            int arraySize = ((int)Math.Floor(Math.Log(n) * oneOverLogPhi)) + 1;
            Entry[] array = new Entry[arraySize];

            if (min != null)
            {
                foreach (Entry w in min.nodelist())
                {

                    // Find two roots x and y in the root list with the same degree,
                    // where key[x]<=key[y].
                    Entry x = w;
                    int d = w.degree;
                    while (array[d] != null)
                    {
                        Entry y = array[d];

                        // Whichever of x and y has he smaller key becomes the parent of
                        // the other.
                        if (x.Priority > y.Priority)
                        {
                            Entry tmp = x;
                            x = y;
                            y = tmp;
                        }
                        // Remove y from the root list, and make y a child of x.
                        link(y, x);

                        // Because node y is no longer a root, the pointer to it in the
                        // array is removed.
                        array[d] = null;

                        // Restore the invariant.
                        d++;
                    }
                    array[d] = x;
                }
            }

            // Empty the root list.
            min = null;

            // Reconstruct the root list from the array.
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] != null)
                {
                    min = concatenateNode(min, array[i]);
                    if (min == null || array[i].Priority < min.Priority)
                    {
                        min = array[i];
                    }
                }
            }
        }

        /**
         * Make node y a child of node x.
         * 
         * Actual cost: O(1)
         * 
         * @param y node to become child
         * @param x node to become parent
         */
        protected void link(Entry y, Entry x)
        {

            // Remove y from root list of heap.
            min = removeNode(min, y);

            // Make y a child of x.
            y.parent = x;
            y.right = y;
            y.left = y;
            x.child = concatenateNode(x.child, y);

            // Increase degree[x].
            x.degree++;

            // Set mark[y] false.
            y.mark = false;
        }

        /**
         * Remove node from list.
         * 
         * @param list
         * 
         * @param node
         * 
         * @return if the list is empty, return null
         */
        private Entry removeNode(Entry list, Entry node)
        {
            if (node.left == node)
            {
                return null;
            }
            node.left.right = node.right;
            node.right.left = node.left;
            return list;
        }

        /**
         * Add heap h into the heap.
         * 
         * Actual cost: O(1)
         * 
         * @param h heap
         */
        public void union(FibonacciHeapJ<T> h)
        {

            // concatenate the root list of h
            min = concatenateList(min, h.min);

            // set the minimum node of heap.
            if (min == null || (h.min != null && h.min.Priority < min.Priority))
            {
                min = h.min;
            }

            // set the total number of nodes.
            n += h.n;
        }

        private Entry concatenateList(Entry list1, Entry list2)
        {
            if (list1 != null && list2 != null)
            {
                list1.right.left = list2.left;
                list2.left.right = list1.right;
                list1.right = list2;
                list2.left = list1;
                return list1;
            }
            else if (list1 == null)
            {
                return list2;
            }
            else
            {
                return list1;
            }
        }

        /**
         * Implements a node of the Fibonacci heap. It holds the information
         * necessary for maintaining the structure of the heap. It also holds the
         * reference to the key value (which is used to determine the heap
         * structure).
         * 
         * @author Yifan Peng
         * @version 09/06/2011
         */
        public class Entry
        {

            /**
             * Any one of its children
             */
            public Entry child;

            /**
             * Left sibling
             */
            public Entry left;

            /**
             * Its parent
             */
            public Entry parent;

            /**
             * Right sibling
             */
            public Entry right;

            /**
             * Whether this node has lost a child since the last time this node was
             * made the child of another node.
             */
            public bool mark;

            /**
             * Key for this node
             */
            public double Priority;

            /**
             * Value for this node
             */
            public T Value;

            /**
             * The number of children in the child list
             */
            internal int degree;

            internal Entry(double key, T value)
            {
                this.Priority = key;
                this.Value = value;
            }

            /**
             * @return node list of the same level
             */
            internal List<Entry> nodelist()
            {
                List<Entry> list = new List<Entry>();
                list.Add(this);
                Entry next = right;
                while (next != this)
                {
                    list.Add(next);
                    next = next.right;
                }
                return list;
            }
        }
    }
}
