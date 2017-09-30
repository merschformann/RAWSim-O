using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Geometrics
{
    /// <summary>
    /// A quadtree implementation used for more efficient collision detection.
    /// </summary>
    /// <typeparam name="T">The type of the elements to check collisions for.</typeparam>
    public class QuadTree<T> where T : Circle
    {
        #region Constants

        /// <summary>
        /// One QuadNode is subdivided, if it contains at least this count of objects.
        /// </summary>
        private const int DivisionThreshold = 12;

        /// <summary>
        /// If fewer objects are contained in the four children of a QuadNode, they are recombined.
        /// </summary>
        private const int CombineThreshold = 8;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new <code>QuadTree</code> of the given size.
        /// </summary>
        /// <param name="length">The width of the area to manage.</param>
        /// <param name="width">The height of the area to manage.</param>
        public QuadTree(double length, double width) { _rootNode = new QuadNode<T>(DivisionThreshold, CombineThreshold, 0, length, 0, width); }
        /// <summary>
        /// Creates a new <code>QuadTree</code> with the given parameters.
        /// </summary>
        /// <param name="divisionThreshold">One QuadNode is subdivided, if it contains at least this count of objects.</param>
        /// <param name="combineThreshold">If fewer objects are contained in the four children of a QuadNode, they are recombined.</param>
        /// <param name="x1">The lower bound of the x-values belonging to this sector.</param>
        /// <param name="x2">The upper bound of the x-values belonging to this sector.</param>
        /// <param name="y1">The lower bound of the y-values belonging to this sector.</param>
        /// <param name="y2">The upper bound of the y-values belonging to this sector.</param>
        public QuadTree(int divisionThreshold, int combineThreshold, double x1, double x2, double y1, double y2) { _rootNode = new QuadNode<T>(divisionThreshold, combineThreshold, x1, x2, y1, y2); }

        #endregion

        #region Core

        /// <summary>
        /// The root node of this QuadTree.
        /// </summary>
        private QuadNode<T> _rootNode;

        /// <summary>
        /// Adds the specified object to the tree.
        /// </summary>
        /// <param name="c">The object to add to this tree.</param>
        public void Add(T c) { _rootNode.Add(c); _rootNode.Reoptimize(); }

        /// <summary>
        /// Removes the specified object from the tree.
        /// </summary>
        /// <param name="c">The object to remove.</param>
        public void Remove(T c) { _rootNode.Remove(c); _rootNode.Reoptimize(); }

        /// <summary>
        /// Returns the number of objects inside this QuadTree.
        /// </summary>
        /// <returns>The number of objects in the tree.</returns>
        public int Count() { return _rootNode.Count(); }

        /// <summary>
        /// Finds the shortest distance any object can move before a collision could happen.
        /// </summary>
        /// <returns>The shortest distance to a collision.</returns>
        public double GetShortestDistanceWithoutCollision() { return _rootNode.GetShortestDistanceWithoutCollision(); }

        /// <summary>
        /// Returns true if Circle c moving to location x, y will not collide with another Circle, false if it will collide.
        /// </summary>
        /// <param name="c">Circle to check for collision.</param>
        /// <param name="x">The new x-coordinate.</param>
        /// <param name="y">The new y-coordinate.</param>
        /// <returns><code>true</code> if the move is valid, <code>false</code> otherwise.</returns>
        public bool IsValidMove(T c, double x, double y) { return _rootNode.IsValidMove(c, x, y); }

        /// <summary>
        /// Checks an object for any collisions with other objects.
        /// </summary>
        /// <param name="c">The object to check for collisions.</param>
        /// <returns><code>true</code> if there are any collisions, <code>false</code> otherwise.</returns>
        public bool IsCollision(T c) { return _rootNode.IsCollision(c); }

        /// <summary>
        /// Moves Circle c from its current location to the position specified by x, y.
        /// </summary>
        /// <param name="c">The Circle to move.</param>
        /// <param name="x">The new x-coordinate.</param>
        /// <param name="y">The new y-coordinate.</param>
        public void MoveTo(T c, double x, double y) { _rootNode.Remove(c); c.X = x; c.Y = y; _rootNode.Add(c); }

        /// <summary>
        /// Updates the tree such that it stays an efficient size.
        /// </summary>
        public void UpdateTree() { _rootNode.Reoptimize(); }

        /// <summary>
        /// Validates the complete QuadTree by checking whether all objects are within the boundaries of their nodes.
        /// </summary>
        /// <returns><code>true</code> if the tree is valid, <code>false</code> otherwise.</returns>
        public bool ValidateTree() { return _rootNode.Validate(); }

        /// <summary>
        /// Returns an enumeration of objects within the given distance around the given coordinates.
        /// </summary>
        /// <param name="x">The x-value of the coordinate.</param>
        /// <param name="y">The y-value of the coordinate.</param>
        /// <param name="distance">The distance for the search.</param>
        /// <returns>All objects within distance.</returns>
        public IEnumerable<T> GetObjectsWithinDistance(double x, double y, double distance) { return _rootNode.GetObjectsWithinDistance(x, y, distance); }

        /// <summary>
        /// Returns the object nearest to the given coordinates.
        /// </summary>
        /// <param name="x">The x-value of the coordinates.</param>
        /// <param name="y">The y-value of the coordinates.</param>
        /// <param name="distance">The distance to the nearest object.</param>
        /// <returns>The object nearest to the given coordinates.</returns>
        public T GetNearestObject(double x, double y, out double distance)
        {
            double obtainedDistance = double.PositiveInfinity;
            T obtainedObject = null;
            _rootNode.GetNearestObject(x, y, ref obtainedObject, ref obtainedDistance);
            distance = obtainedDistance;
            return obtainedObject;
        }

        #endregion
    }
}
