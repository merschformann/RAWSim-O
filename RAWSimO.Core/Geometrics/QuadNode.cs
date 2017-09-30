using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Geometrics
{
    /// <summary>
    /// Represents on node of a quadtree. The node can either contain all elements itself or have four subnodes separating the elements by their position.
    /// </summary>
    /// <typeparam name="T">The type of elements contained by the node.</typeparam>
    public class QuadNode<T> where T : Circle
    {
        /// <summary>
        /// Creates a new quadnode.
        /// </summary>
        /// <param name="divisionThreshold"></param>
        /// <param name="combineThreshold"></param>
        /// <param name="x1">The lower bound of the x-values belonging to this sector.</param>
        /// <param name="x2">The upper bound of the x-values belonging to this sector.</param>
        /// <param name="y1">The lower bound of the y-values belonging to this sector.</param>
        /// <param name="y2">The upper bound of the y-values belonging to this sector.</param>
        public QuadNode(int divisionThreshold, int combineThreshold, double x1, double x2, double y1, double y2)
        {
            DivisionThreshold = divisionThreshold;
            CombineThreshold = combineThreshold;
            X1 = x1;
            X2 = x2;
            Y1 = y1;
            Y2 = y2;
            _midX = X1 + (X2 - X1) / 2;
            _midY = Y1 + (Y2 - Y1) / 2;
            _midXs = new double[] { X1 + (X2 - X1) * (1.0 / 4.0), X1 + (X2 - X1) * (3.0 / 4.0), X1 + (X2 - X1) * (1.0 / 4.0), X1 + (X2 - X1) * (3.0 / 4.0) };
            _midYs = new double[] { Y1 + (Y2 - Y1) * (1.0 / 4.0), Y1 + (Y2 - Y1) * (1.0 / 4.0), Y1 + (Y2 - Y1) * (3.0 / 4.0), Y1 + (Y2 - Y1) * (3.0 / 4.0), };
        }

        /// <summary>
        /// The sub-nodes having this node as a parent. These nodes subdivide the space of this node into 4 equally sized parts.
        /// </summary>
        internal QuadNode<T>[] Children = new QuadNode<T>[4];

        /// <summary>
        /// The IDs of all children as an array.
        /// </summary>
        private int[] _childrenIDs = new int[] { 0, 1, 2, 3 };

        /// <summary>
        /// The lower bound of the x-values belonging to this sector.
        /// </summary>
        private double X1;
        /// <summary>
        /// The upper bound of the x-values belonging to this sector.
        /// </summary>
        private double X2;
        /// <summary>
        /// The lower bound of the y-values belonging to this sector.
        /// </summary>
        private double Y1;
        /// <summary>
        /// The upper bound of the y-values belonging to this sector.
        /// </summary>
        private double Y2;

        /// <summary>
        /// The middle x-value of this node.
        /// </summary>
        private double _midX;
        /// <summary>
        /// The middle y-value of this node.
        /// </summary>
        private double _midY;

        /// <summary>
        /// The middle x-values of the children.
        /// </summary>
        private double[] _midXs;
        /// <summary>
        /// The middle y-values of the children.
        /// </summary>
        private double[] _midYs;

        /// <summary>
        /// One QuadNode is subdivided, if it contains at least this count of objects.
        /// </summary>
        private int DivisionThreshold;

        /// <summary>
        /// If fewer objects are contained in the four children of a QuadNode, they are recombined.
        /// </summary>
        private int CombineThreshold;

        /// <summary>
        /// Contains all objects attached to this node.
        /// </summary>
        internal HashSet<T> Objects = new HashSet<T>();

        /// <summary>
        /// The largest object of this node.
        /// </summary>
        private T _largestObject = null;

        /// <summary>
        /// Returns the <code>QuadNode</code> belonging to the specified direction.
        /// </summary>
        /// <param name="direction">The direction of the child.</param>
        /// <returns>The desired <code>QuadNode</code> object.</returns>
        public QuadNode<T> this[QuadDirections direction]
        {
            get
            {
                switch (direction)
                {
                    case QuadDirections.SW: return Children[0];
                    case QuadDirections.SE: return Children[1];
                    case QuadDirections.NW: return Children[2];
                    case QuadDirections.NE: return Children[3];
                    default: return null;
                }
            }
        }

        /// <summary>
        /// Returns true if Circle c moving to location x, y will not collide with another Circle, false if it will collide.
        /// </summary>
        /// <param name="c">Circle to check for collision.</param>
        /// <param name="x">The new x-coordinate.</param>
        /// <param name="y">The new y-coordinate.</param>
        /// <returns><code>true</code> if the move is valid, <code>false</code> otherwise.</returns>
        public bool IsValidMove(T c, double x, double y)
        {
            // If this is a leaf node, check all objects
            if (this.Children[0] == null)
            {
                foreach (var other in Objects)
                {
                    if (other == c) { continue; }
                    if (other.IsCollision(x, y, c.Radius)) { return false; }
                }
                return true;
            }

            // Use 2*diameter to account for another circle beyond the QuadtreeNode
            double diameter = 4 * c.Radius;

            // Check to see if it's in any of the four quadrants; be leniant by a diameter incase it's at the edge
            // See if in left half
            if (c.X - diameter <= _midX)
            {
                // See if in bottom half
                if (c.Y - diameter <= _midY)
                    if (!this.Children[0].IsValidMove(c, x, y))
                        return false;

                // See if in top half
                if (c.Y + diameter >= _midY)
                    if (!this.Children[2].IsValidMove(c, x, y))
                        return false;
            }

            // See if in right half
            if (c.X + diameter >= _midX)
            {
                // See if in bottom half
                if (c.Y - diameter <= _midY)
                    if (!this.Children[1].IsValidMove(c, x, y))
                        return false;

                // See if in top half
                if (c.Y + diameter >= _midY)
                    if (!this.Children[3].IsValidMove(c, x, y))
                        return false;
            }

            // Survived all the collision tests
            return true;
        }

        /// <summary>
        /// Checks an object for any collisions with other objects.
        /// </summary>
        /// <param name="c">The object to check for collisions.</param>
        /// <returns><code>true</code> if there are any collisions, <code>false</code> otherwise.</returns>
        public bool IsCollision(T c)
        {
            // If this is a leaf node, check all objects
            if (this.Children[0] == null)
            {
                foreach (var other in Objects)
                {
                    if (other == c) { continue; }
                    if (other.IsCollision(c)) { return true; }
                }
                return false;
            }

            // Use 2*diameter to account for another circle beyond the QuadtreeNode
            double diameter = 4 * c.Radius;

            // Check to see if it's in any of the four quadrants; be leniant by a diameter incase it's at the edge
            // See if in left half
            if (c.X - diameter <= _midX)
            {
                // See if in bottom half
                if (c.Y - diameter <= _midY)
                    if (!this.Children[0].IsCollision(c))
                        return false;

                // See if in top half
                if (c.Y + diameter >= _midY)
                    if (!this.Children[2].IsCollision(c))
                        return false;
            }

            // See if in right half
            if (c.X + diameter >= _midX)
            {
                // See if in bottom half
                if (c.Y - diameter <= _midY)
                    if (!this.Children[1].IsCollision(c))
                        return false;

                // See if in top half
                if (c.Y + diameter >= _midY)
                    if (!this.Children[3].IsCollision(c))
                        return false;
            }

            // Survived all the collision tests
            return false;
        }

        /// <summary>
        /// Adds the specified Circle to the tree.
        /// </summary>
        /// <param name="c">The Circle to add.</param>
        public void Add(T c)
        {
            // If it's bigger, then it's the largest circle
            if (_largestObject == null || c.Radius > _largestObject.Radius)
                _largestObject = c;

            if (this.Children[0] == null)
            {
                this.Objects.Add(c);
            }
            else
            {
                // See if in left half
                if (c.X < _midX)
                {
                    // See if in bottom half
                    if (c.Y < _midY)
                        this.Children[0].Add(c);
                    // In top half
                    else
                        this.Children[2].Add(c);
                }
                // In right half
                else
                {
                    // See if in bottom half
                    if (c.Y < _midY)
                        this.Children[1].Add(c);
                    // In top half
                    else
                        this.Children[3].Add(c);
                }
            }
        }

        /// <summary>
        /// Removes the specified Circle from the tree.
        /// </summary>
        /// <param name="c">The Circle to remove.</param>
        public void Remove(T c)
        {
            // If leaf node, remove circle, otherwise traverse subnodes
            if (this.Children[0] == null)
            {
                bool removeSuccess = this.Objects.Remove(c);
                if (!removeSuccess)
                    throw new InvalidOperationException("Cannot remove the object from the tree - it is not in the position where it was expected");

                // Get new largest circle
                double largest_size = 0.0;
                _largestObject = null;
                foreach (var i in this.Objects)
                    if (i.Radius > largest_size)
                    {
                        _largestObject = i;
                        largest_size = _largestObject.Radius;
                    }
            }
            else
            {
                // See if in left half
                if (c.X < _midX)
                {
                    // See if in bottom half
                    if (c.Y < _midY)
                        this.Children[0].Remove(c);
                    // In top half
                    else
                        this.Children[2].Remove(c);
                }
                // In right half
                else
                {
                    // See if in bottom half
                    if (c.Y < _midY)
                        this.Children[1].Remove(c);
                    // In top half
                    else
                        this.Children[3].Remove(c);
                }

                // Get new largest circle
                double largest_size = 0.0;
                _largestObject = null;
                for (int i = 0; i < 4; i++)
                    if (this.Children[i]._largestObject != null && this.Children[i]._largestObject.Radius > largest_size)
                    {
                        _largestObject = this.Children[i]._largestObject;
                        largest_size = _largestObject.Radius;
                    }
            }
        }

        /// <summary>
        /// Returns an enumeration of objects within the given distance around the given coordinates.
        /// </summary>
        /// <param name="x">The x-value of the coordinate.</param>
        /// <param name="y">The y-value of the coordinate.</param>
        /// <param name="distance">The distance for the search.</param>
        /// <returns>All objects within distance.</returns>
        public IEnumerable<T> GetObjectsWithinDistance(double x, double y, double distance)
        {
            // If this is a leaf node only check the attached objects and return
            if (this.Children[0] == null)
            {
                foreach (var o in this.Objects)
                    if (o.IsCollision(x, y, distance))
                        yield return o;
                yield break;
            }

            // Find distance to search for another object (but make sure to include a padding to check for the nodes where an object may be overlapping two nodes (but don'task pass this distance on to the collision detection itself)
            double dist = distance;
            if (_largestObject != null)
                dist += _largestObject.Radius;

            // Check to see if it's in any of the four quadrants see if in left half
            if (x - dist <= _midX)
            {
                // See if in bottom half
                if (y - dist <= _midY)
                    foreach (var child in this.Children[0].GetObjectsWithinDistance(x, y, distance))
                        yield return child;

                // See if in top half
                if (y + dist >= _midY)
                    foreach (var child in this.Children[2].GetObjectsWithinDistance(x, y, distance))
                        yield return child;
            }

            // See if in right half
            if (x + dist >= _midX)
            {
                // See if in bottom half
                if (y - dist <= _midY)
                    foreach (var child in this.Children[1].GetObjectsWithinDistance(x, y, distance))
                        yield return child;

                // See if in top half
                if (y + dist >= _midY)
                    foreach (var child in this.Children[3].GetObjectsWithinDistance(x, y, distance))
                        yield return child;
            }
        }

        /// <summary>
        /// Gets the object nearest to the given coordinates.
        /// </summary>
        /// <param name="x">The x-value.</param>
        /// <param name="y">The y-value.</param>
        /// <param name="nearestObject">Is updated with the nearest object of the node, if there is a nearer one.</param>
        /// <param name="nearestDistance">This field is passed the current best distance which is updated, if a better one is found.</param>
        /// <returns>The object nearest to the given coordinates.</returns>
        public void GetNearestObject(double x, double y, ref T nearestObject, ref double nearestDistance)
        {
            // If this is a leaf node, only check the attached objects
            if (Children[0] == null)
            {
                // Obtain nearest object
                foreach (var child in Objects)
                {
                    double distance = child.GetDistance(x, y);
                    if (distance < nearestDistance)
                    {
                        nearestObject = child;
                        nearestDistance = distance;
                    }
                }
                return;
            }

            // Determine search order
            int first = -1; double firstValue = double.MaxValue;
            int second = -1; double secondValue = double.MaxValue;
            int third = -1; double thirdValue = double.MaxValue;
            int fourth = -1; double fourthValue = double.MaxValue;
            for (int i = 0; i < _childrenIDs.Length; i++)
            {
                double distance = Metrics.Distances.CalculateEuclid(_midXs[i], _midYs[i], x, y);
                if (distance < firstValue)
                {
                    fourth = third; fourthValue = thirdValue;
                    third = second; thirdValue = secondValue;
                    second = first; secondValue = firstValue;
                    first = i; firstValue = distance;
                }
                else if (distance < secondValue)
                {
                    fourth = third; fourthValue = thirdValue;
                    third = second; thirdValue = secondValue;
                    second = i; secondValue = distance;
                }
                else if (distance < thirdValue)
                {
                    fourth = third; fourthValue = thirdValue;
                    third = i; thirdValue = distance;
                }
                else if (distance < fourthValue)
                {
                    fourth = i; fourthValue = distance;
                }
            }
            // Check all children for the nearest object
            // Check first section
            Children[first].GetNearestObject(x, y, ref nearestObject, ref nearestDistance);
            // Check second section if necessary
            if (nearestObject == null || !CheckChildLookupUnnecessary(second, x, y, nearestDistance))
                Children[second].GetNearestObject(x, y, ref nearestObject, ref nearestDistance);
            // Check third section if necessary
            if (nearestObject == null || !CheckChildLookupUnnecessary(third, x, y, nearestDistance))
                Children[third].GetNearestObject(x, y, ref nearestObject, ref nearestDistance);
            // Check fourth section if necessary
            if (nearestObject == null || !CheckChildLookupUnnecessary(fourth, x, y, nearestDistance))
                Children[fourth].GetNearestObject(x, y, ref nearestObject, ref nearestDistance);
        }
        /// <summary>
        /// Checks whether the given section has to be checked considering the so far best distance.
        /// </summary>
        /// <param name="childID">The section to be checked.</param>
        /// <param name="x">The x-value to search for the nearest neigbor for.</param>
        /// <param name="y">The y-value to search for the nearest neigbor for.</param>
        /// <param name="bestDistance">The best distance so far.</param>
        /// <returns><code>true</code> if the lookup of the given section is unnecessary, <code>false</code> otherwise.</returns>
        private bool CheckChildLookupUnnecessary(int childID, double x, double y, double bestDistance)
        {
            switch (childID)
            {
                // Check bottom left section
                case 0: { return x - bestDistance >= _midXs[childID] || y - bestDistance >= _midYs[childID]; }
                // Check bottom right section
                case 1: { return x + bestDistance <= _midXs[childID] || y - bestDistance >= _midYs[childID]; }
                // Check top left section
                case 2: { return x - bestDistance >= _midXs[childID] || y + bestDistance <= _midYs[childID]; }
                // Check top right section
                case 3: { return x + bestDistance <= _midXs[childID] || y + bestDistance <= _midYs[childID]; }
                default: throw new ArgumentException("Unknown child ID: " + childID);
            }
        }

        /// <summary>
        /// Returns the number of objects beneath this node of the QuadTree.
        /// </summary>
        /// <returns>The number of objects at this node or the summed number of objects at the child-nodes of this node.</returns>
        public int Count()
        {
            if (this.Children[0] == null)
                return this.Objects.Count;
            else
                return this.Children[0].Count() + this.Children[1].Count() + this.Children[2].Count() + this.Children[3].Count();
        }

        /// <summary>
        /// Validates this node in terms of checking whether all objects are within the nodes boundaries.
        /// </summary>
        /// <returns><code>true</code> if the node is valid, <code>false</code> otherwise.</returns>
        public bool Validate()
        {
            if (this.Children[0] == null)
                return this.Objects.All(o => o.X >= X1 && o.X <= X2 && o.Y >= Y1 && o.Y <= Y2);
            else
                return this.Children.All(c => c.Validate());
        }

        /// <summary>
        /// Rearranges the tree by expanding and collapsing this node and all of its children, if necessary.
        /// </summary>
        public void Reoptimize()
        {
            // Growing the tree
            if (this.Objects.Count >= DivisionThreshold)
            {
                // Create child trees
                this.Children[0] = new QuadNode<T>(DivisionThreshold, CombineThreshold, X1, _midX, Y1, _midY);
                this.Children[1] = new QuadNode<T>(DivisionThreshold, CombineThreshold, _midX, X2, Y1, _midY);
                this.Children[2] = new QuadNode<T>(DivisionThreshold, CombineThreshold, X1, _midX, _midY, Y2);
                this.Children[3] = new QuadNode<T>(DivisionThreshold, CombineThreshold, _midX, X2, _midY, Y2);

                // Move all objects into the corresponding one
                foreach (var c in this.Objects)
                    Add(c);

                // Clean up this node
                this.Objects.Clear();
            }

            // Update all children
            if (this.Children[0] != null)
                foreach (var q in this.Children)
                    q.Reoptimize();

            // If has children, and each child is a leaf node, check to see if the sum of all of the objects within those 4 children is less than the QuadTree.CombineThreshold. If so, take all of the children's objects into this node and detach the children. 
            if (this.Children[0] != null &&
                this.Children[0].Children[0] == null &&
                this.Children[1].Children[0] == null &&
                this.Children[2].Children[0] == null &&
                this.Children[3].Children[0] == null &&
                (this.Children[0].Objects.Count + this.Children[1].Objects.Count + this.Children[2].Objects.Count + this.Children[3].Objects.Count) < CombineThreshold)
            {
                // Iterate the children
                for (int i = 0; i < 4; i++)
                {
                    // Need to clear Children[0] first so that Add will know it has no children.
                    HashSet<T> circles = this.Children[i].Objects;
                    this.Children[i] = null;
                    // Add all objects from child nodes
                    foreach (var c in circles) Add(c);
                }
            }
        }

        /// <summary>
        /// Finds the shortest distance any object can move before a collision could happen.
        /// </summary>
        /// <returns>The shortest distance to a collision.</returns>
        public double GetShortestDistanceWithoutCollision()
        {
            // If not a leaf node, then get values from child nodes, and find the minimum
            if (this.Children[0] != null)
            {
                return Math.Min(
                    Math.Min(this.Children[0].GetShortestDistanceWithoutCollision(), this.Children[1].GetShortestDistanceWithoutCollision()),
                    Math.Min(this.Children[2].GetShortestDistanceWithoutCollision(), this.Children[3].GetShortestDistanceWithoutCollision()));
            }

            // Leaf node, so find shortest distances for each robot
            T[] objectArray = this.Objects.ToArray();
            double minDistance = Double.PositiveInfinity;
            for (int i = 0; i < this.Objects.Count - 1; i++)
            {
                T c1 = objectArray[i];
                // Only check moving objects
                if (!c1.Moving)
                    continue;

                double minDistanceSquared = double.PositiveInfinity;

                // Find distance to closest other object
                for (int j = i + 1; j < this.Objects.Count; j++)
                {
                    T c2 = objectArray[j];
                    // Only check moving objects
                    if (!c2.Moving)
                        continue;

                    // Calculate distance
                    double dist = (c1.X - c2.X) * (c1.X - c2.X) + (c1.Y - c2.Y) * (c1.Y - c2.Y);
                    if (dist < minDistanceSquared)
                        minDistanceSquared = dist;
                }

                // Set new minimum, if necessary
                minDistance = Math.Min(minDistance, Math.Sqrt(minDistanceSquared));

                // Check distance to sides of collision pod
                // Use 2*diameter to account for another circle beyond the QuadNode
                double diameter = 2 * c1.Radius;

                // Left side
                minDistance = Math.Min(minDistance, c1.X - (X1 - diameter));
                // Right side
                minDistance = Math.Min(minDistance, (X2 + diameter) - c1.X);
                // Top side
                minDistance = Math.Min(minDistance, (Y1 + diameter) - c1.Y);
                // Bottom side
                minDistance = Math.Min(minDistance, c1.Y - (Y2 - diameter));
            }

            // Return the minimal distance between two objects of this node and all of its children
            return minDistance;
        }
    }
}
