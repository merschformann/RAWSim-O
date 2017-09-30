using RAWSimO.Core.Elements;
using RAWSimO.Core.Geometrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Waypoints
{
    /// <summary>
    /// Implements the waypointgraph containing all waypoints of one instance.
    /// </summary>
    public class WaypointGraph
    {
        private Dictionary<Tier, HashSet<Waypoint>> _waypoints = new Dictionary<Tier, HashSet<Waypoint>>();
        private Dictionary<InputStation, Waypoint> _inputStations = new Dictionary<InputStation, Waypoint>();
        private Dictionary<OutputStation, Waypoint> _outputStations = new Dictionary<OutputStation, Waypoint>();
        private Dictionary<Pod, Waypoint> _pods = new Dictionary<Pod, Waypoint>();
        private Dictionary<Tier, QuadTree<Waypoint>> _waypointQuadtree = new Dictionary<Tier, QuadTree<Waypoint>>();
        private Dictionary<Tier, double> _largestEdgeBetweenTwoWaypoints = new Dictionary<Tier, double>();

        /// <summary>
        /// All input-station waypoints.
        /// </summary>
        public IEnumerable<Waypoint> InputStationWaypoints { get { return _inputStations.Values; } }
        /// <summary>
        /// All output-station waypoints.
        /// </summary>
        public IEnumerable<Waypoint> OutputStationWaypoints { get { return _outputStations.Values; } }
        /// <summary>
        /// Adds the new waypoint to the graph.
        /// </summary>
        /// <param name="waypoint">The waypoint.</param>
        public void Add(Waypoint waypoint)
        {
            if (!_waypointQuadtree.ContainsKey(waypoint.Tier))
            {
                _waypointQuadtree[waypoint.Tier] = new QuadTree<Waypoint>(waypoint.Tier.Length, waypoint.Tier.Width);
                _waypoints[waypoint.Tier] = new HashSet<Waypoint>();
            }
            _waypoints[waypoint.Tier].Add(waypoint);
            _waypointQuadtree[waypoint.Tier].Add(waypoint);
            if (waypoint.InputStation != null)
                _inputStations[waypoint.InputStation] = waypoint;
            if (waypoint.OutputStation != null)
                _outputStations[waypoint.OutputStation] = waypoint;
            if (waypoint.Pod != null)
                _pods[waypoint.Pod] = waypoint;
        }
        /// <summary>
        /// Removes the waypoint from the graph.
        /// </summary>
        /// <param name="waypoint">The waypoint.</param>
        public void Remove(Waypoint waypoint)
        {
            _waypoints[waypoint.Tier].Remove(waypoint);
            _waypointQuadtree[waypoint.Tier].Remove(waypoint);
            foreach (var other in _waypoints.SelectMany(wp => wp.Value).Where(wp => wp != waypoint && wp.ContainsPath(waypoint)))
                other.RemovePath(waypoint);
            foreach (var station in _inputStations.Where(kvp => kvp.Value == waypoint).Select(kvp => kvp.Key).ToArray())
                _inputStations.Remove(station);
            foreach (var station in _outputStations.Where(kvp => kvp.Value == waypoint).Select(kvp => kvp.Key).ToArray())
                _outputStations.Remove(station);
            foreach (var pod in _pods.Where(kvp => kvp.Value == waypoint).Select(kvp => kvp.Key).ToArray())
                _pods.Remove(pod);
        }
        /// <summary>
        /// Gets all waypoints of the graph.
        /// </summary>
        /// <returns>All waypoints of the graph per tier.</returns>
        public Dictionary<Tier, HashSet<Waypoint>> GetWayPoints()
        {
            return _waypoints;
        }
        /// <summary>
        /// Gets all waypoints within the given distance around the given position.
        /// </summary>
        /// <param name="tier">The tier to look at.</param>
        /// <param name="x">The x-coordinate.</param>
        /// <param name="y">The y-coordinate.</param>
        /// <param name="distance">The distance.</param>
        /// <returns>All waypoints within the distance around the position.</returns>
        public IEnumerable<Waypoint> GetWaypointsWithinDistance(Tier tier, double x, double y, double distance)
        {
            return _waypointQuadtree[tier].GetObjectsWithinDistance(x, y, distance);
        }
        /// <summary>
        /// Gets the waypoint closest to the given position.
        /// </summary>
        /// <param name="tier">The tier to look at.</param>
        /// <param name="x">The x-coordinate.</param>
        /// <param name="y">The y-coordinate.</param>
        /// <returns>The closest waypoint.</returns>
        public Waypoint GetClosestWaypoint(Tier tier, double x, double y)
        {
            if (!_largestEdgeBetweenTwoWaypoints.ContainsKey(tier))
                _largestEdgeBetweenTwoWaypoints[tier] = _waypoints[tier].Max(wp => wp.Paths.Max(otherWP => wp.GetDistance(otherWP)));
            Waypoint closest = GetWaypointsWithinDistance(tier, x, y, _largestEdgeBetweenTwoWaypoints[tier]).OrderBy(wp => wp.GetDistance(x, y)).FirstOrDefault();
            if (closest != null)
                return closest;
            else
                return _waypoints[tier].OrderBy(wp => wp.GetDistance(x, y)).First();
        }
        /// <summary>
        /// Callback that has to be called when picking up a pod.
        /// </summary>
        /// <param name="pod">The pod that was picked up.</param>
        public void PodPickup(Pod pod)
        {
            _pods[pod].Pod = null;
            _pods.Remove(pod);
        }
        /// <summary>
        /// Callback that has to be called when setting down a pod.
        /// </summary>
        /// <param name="pod">The pod that was set down.</param>
        /// <param name="waypoint">The new storage location of the pod.</param>
        public void PodSetdown(Pod pod, Waypoint waypoint)
        {
            // Manage core elements
            waypoint.Pod = pod;
            _pods[pod] = waypoint;
        }
        /// <summary>
        /// Gets the pod positions.
        /// </summary>
        /// <returns>pod positions</returns>
        internal List<Waypoint> GetPodPositions()
        {
            return _pods.Values.ToList();
        }
    }
}
