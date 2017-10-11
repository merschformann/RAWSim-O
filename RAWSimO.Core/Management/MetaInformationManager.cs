using RAWSimO.Core.Geometrics;
using RAWSimO.Core.IO;
using RAWSimO.Core.Metrics;
using RAWSimO.Core.Waypoints;
using RAWSimO.MultiAgentPathFinding.Physic;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Management
{
    /// <summary>
    /// A manager keeping track of meta information related to an instance.
    /// </summary>
    internal class MetaInformationManager
    {
        public MetaInformationManager(Instance instance)
        {
            ShortestPathManager = new ShortestPathManager();
            TimeEfficientPathManager = new TimeEfficientPathManager(instance);
        }
        /// <summary>
        /// Keeps track of shortest paths from one waypoint to another.
        /// </summary>
        public ShortestPathManager ShortestPathManager;
        /// <summary>
        /// Keeps track of shortest paths from one waypoint to another.
        /// </summary>
        public TimeEfficientPathManager TimeEfficientPathManager;
    }

    #region Shortest path management

    /// <summary>
    /// Keeps track of shortest paths from one waypoint to another.
    /// </summary>
    internal class ShortestPathManager
    {
        /// <summary>
        /// Contains all shortest paths calculated so far.
        /// </summary>
        MultiKeyDictionary<Waypoint, Waypoint, bool, double> _shortestPaths = new MultiKeyDictionary<Waypoint, Waypoint, bool, double>();

        /// <summary>
        /// Used to store the search tree of A*.
        /// </summary>
        private class WaypointSearchDatapoint
        {
            /// <summary>
            /// Creates a new waypoint search data object.
            /// </summary>
            /// <param name="distanceTraveled">The distance travelled so far.</param>
            /// <param name="distanceToGoal">The estimated distance towards the destination.</param>
            /// <param name="waypoint">The waypoint this data object belongs to.</param>
            /// <param name="parentMove">The data of the parent.</param>
            /// <param name="depth">The steps we did to come here.</param>
            public WaypointSearchDatapoint(double distanceTraveled, double distanceToGoal, Waypoint waypoint, WaypointSearchDatapoint parentMove, int depth) { DistanceTraveled = distanceTraveled; DistanceToGoal = distanceToGoal; Waypoint = waypoint; ParentMove = parentMove; Depth = depth; }
            /// <summary>
            /// The distance travelled so far.
            /// </summary>
            public double DistanceTraveled;
            /// <summary>
            /// The estimated distance towards the destination.
            /// </summary>
            public double DistanceToGoal;
            /// <summary>
            /// The waypoint this data object belongs to.
            /// </summary>
            public Waypoint Waypoint;
            /// <summary>
            /// The steps we did to come here.
            /// </summary>
            public int Depth;
            /// <summary>
            /// The data of the parent.
            /// </summary>
            public WaypointSearchDatapoint ParentMove;
        }

        /// <summary>
        /// Uses A* to find the distance from the startNode to the destinationNode.
        /// </summary>
        /// <param name="startNode">The starting waypoint.</param>
        /// <param name="destinationNode">The destination waypoint.</param>
        /// <param name="wrongTierPenalty">The penalty for the waypoints not being on the same tier.</param>
        /// <param name="emulatePodCarrying">Indicates whether to calculate a path that is ensured to be safe for carrying a pod on it.</param>
        /// <returns>The distance.</returns>
        private double CalculateShortestPath(Waypoint startNode, Waypoint destinationNode, double wrongTierPenalty, bool emulatePodCarrying)
        {
            if (startNode == null || destinationNode == null)
                return double.PositiveInfinity;

            Dictionary<Waypoint, WaypointSearchDatapoint> openLocations = new Dictionary<Waypoint, WaypointSearchDatapoint>();
            Dictionary<Waypoint, WaypointSearchDatapoint> closedLocations = new Dictionary<Waypoint, WaypointSearchDatapoint>();
            openLocations[startNode] = new WaypointSearchDatapoint(0.0, startNode.GetDistance(destinationNode), startNode, null, 0);

            // Already at destination?
            if (startNode == destinationNode)
                return 0;

            // Loop until end is found
            while (true)
            {
                // Find lowest cost waypoint in openLocations
                KeyValuePair<Waypoint, WaypointSearchDatapoint> currentNodeKVP = openLocations.ArgMin(w => w.Value.DistanceTraveled + w.Value.DistanceToGoal);
                // Something wrong happened -can't find the end
                if (currentNodeKVP.Equals(default(KeyValuePair<Waypoint, WaypointSearchDatapoint>)))
                    return double.PositiveInfinity;
                Waypoint currentNode = currentNodeKVP.Key;

                // Grab the details about the current waypoint
                WaypointSearchDatapoint currentNodeData = currentNodeKVP.Value;

                // If the closest is also the destination or out of iterations
                if (currentNode == destinationNode)
                {
                    // Return the travelled distance
                    return currentNodeData.DistanceTraveled;
                }

                // Transfer closest from open to closed list
                closedLocations[currentNode] = currentNodeData;
                openLocations.Remove(currentNode);

                // Expand all the moves
                foreach (var successorNode in currentNode.Paths)
                {
                    // Check whether the node is already on closed
                    if (closedLocations.ContainsKey(successorNode))
                        // Don't deal with anything already on the closed list (don't want loops)
                        continue;

                    // Can't use a pod storage location for the path
                    if (emulatePodCarrying && successorNode.PodStorageLocation && successorNode != destinationNode)
                        continue;

                    // Tag on more distance for a node on the wrong level
                    double additionalDistance = 0;
                    if (successorNode.Tier != destinationNode.Tier)
                        additionalDistance += wrongTierPenalty;

                    // If it's not in the open list, add it
                    if (!openLocations.ContainsKey(successorNode))
                    {
                        openLocations[successorNode] =
                            new WaypointSearchDatapoint(
                                currentNodeData.DistanceTraveled + currentNode[successorNode], // The distance already traveled
                                successorNode.GetDistance(destinationNode) + additionalDistance, // The approximate distance to the goal
                                successorNode,  // The node itself
                                currentNodeData, // Parent data 
                                currentNodeData.Depth + 1); // The current depth
                    }
                    else
                    {
                        // It's already in the open list, but see if this new path is better
                        WaypointSearchDatapoint oldPath = openLocations[successorNode];
                        // Update to the new path, if better
                        if (oldPath.DistanceTraveled > currentNodeData.DistanceTraveled + currentNode[successorNode])
                        {
                            oldPath.DistanceTraveled = currentNodeData.DistanceTraveled + currentNode[successorNode]; // The distance already traveled
                            oldPath.DistanceToGoal = successorNode.GetDistance(destinationNode) + additionalDistance; // The approximate distance to the goal
                            oldPath.Waypoint = successorNode; // The node itself
                            oldPath.ParentMove = currentNodeData; // Parent data
                            oldPath.Depth = currentNodeData.Depth + 1; // The current depth
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the shortest path from first to the second waypoint in instance units.
        /// </summary>
        /// <param name="from">The starting waypoint.</param>
        /// <param name="to">The destination waypoint.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="emulatePodCarrying">Indicates whether to calculate a path that is ensured to be safe for carrying a pod on it.</param>
        /// <returns>The distance of the shortest path. If both waypoints are not located on the same tier, penalty costs will be added. If there is no connection, an infinite length is returned.</returns>
        public double GetShortestPath(Waypoint from, Waypoint to, Instance instance, bool emulatePodCarrying)
        {
            // Check whether we have the value already
            if (!_shortestPaths.ContainsKey(from, to, emulatePodCarrying))
                // We do not have the value yet - calculate it
                _shortestPaths[from, to, emulatePodCarrying] = CalculateShortestPath(from, to, instance.WrongTierPenaltyDistance, emulatePodCarrying);
            // Return the value
            return _shortestPaths[from, to, emulatePodCarrying];
        }
    }

    #endregion

    #region Time efficient path management

    internal class TimeEfficientPathManager
    {
        #region Helper classes

        /// <summary>
        /// A time-waypoint graph that helps estimating travel times.
        /// </summary>
        public class TimeGraph
        {
            /// <summary>
            /// Two times PI as a constant.
            /// </summary>
            public const double PI2 = Math.PI * 2;
            /// <summary>
            /// This factor can be used to further penalize turning, i.e. further slowing down the turn speed.
            /// This is useful, because acceleration and deceleration are not respected by this manager.
            /// However, turning does cause acceleration and deceration, which takes even more time than turning itself.
            /// </summary>
            public const double TURN_SPEED_SLOW_FACTOR = 2.0;
            /// <summary>
            /// The instance this graph reflects.
            /// </summary>
            public Instance Instance;
            /// <summary>
            /// The speed that robots travel.
            /// </summary>
            public double Speed;
            /// <summary>
            /// The turn-speed of the robots.
            /// </summary>
            public double TurnSpeed;
            /// <summary>
            /// All generated time-waypoints.
            /// </summary>
            public HashSet<TimeWaypoint> TimeWaypoints = new HashSet<TimeWaypoint>();
            /// <summary>
            /// The time-waypoints of all original waypoints.
            /// </summary>
            public Dictionary<Waypoint, HashSet<TimeWaypoint>> TimeWaypointsOfOriginalWaypoints = new Dictionary<Waypoint, HashSet<TimeWaypoint>>();
            /// <summary>
            /// The time-waypoints reflecting specific connections.
            /// </summary>
            public MultiKeyDictionary<Waypoint, Waypoint, TimeWaypoint> TimeWaypointsOfConnections = new MultiKeyDictionary<Waypoint, Waypoint, TimeWaypoint>();
            /// <summary>
            /// Creates a new time-waypoint graph.
            /// </summary>
            /// <param name="instance">The instance that the graph shall reflect.</param>
            public TimeGraph(Instance instance)
            {
                // Save instance
                Instance = instance;
                // Determine robot characteristics (act as if robots have infinite acceleration)
                Speed = instance.Bots.Min(b => b.MaxVelocity);
                TurnSpeed = instance.Bots.Min(b => b.TurnSpeed) * TURN_SPEED_SLOW_FACTOR;
                // Create base graph for storing time information
                foreach (var from in instance.Waypoints)
                    foreach (var to in from.Paths)
                    {
                        TimeWaypoint timeWP = new TimeWaypoint()
                        {
                            // Emulated waypoint is the "to-part" of the connection
                            OriginalWaypoint = to,
                            // Store from waypoint just for completeness
                            InboundWaypoint = from,
                            // The orientation the bot is in when approaching the "to-part" of the connection is derived from the "from-part" of it
                            Orientation = Circle.GetOrientation(from.X, from.Y, to.X, to.Y),
                        };
                        TimeWaypointsOfConnections[from, to] = timeWP;
                        if (!TimeWaypointsOfOriginalWaypoints.ContainsKey(to))
                            TimeWaypointsOfOriginalWaypoints[to] = new HashSet<TimeWaypoint>();
                        TimeWaypointsOfOriginalWaypoints[to].Add(timeWP);
                        TimeWaypoints.Add(timeWP);
                    }
                // Add time information for all connections to the graph
                foreach (var timeWPFrom in TimeWaypoints)
                {
                    foreach (var connectedWP in timeWPFrom.OriginalWaypoint.Paths)
                    {
                        TimeWaypoint timeWPTo = TimeWaypointsOfConnections[timeWPFrom.OriginalWaypoint, connectedWP];
                        timeWPFrom.Edges.Add(new Tuple<TimeWaypoint, double>(
                            // --> Store node we approach when using the connection
                            timeWPTo,
                            // --> Calculate time needed to travel along the connection, i.e. the time for turning towards the connected waypoint + the time for driving to it
                            GetTimeNeededToTravel(timeWPFrom, timeWPTo)));
                    }
                }
                // Add turn on the spot information too
                foreach (var timeWPFrom in TimeWaypoints)
                {
                    foreach (var timeWPTo in TimeWaypointsOfOriginalWaypoints[timeWPFrom.OriginalWaypoint].Where(to => to != timeWPFrom))
                    {
                        timeWPFrom.Edges.Add(new Tuple<TimeWaypoint, double>(
                            // --> Store node of time graph
                            timeWPTo,
                            // --> Calculate time for turning on the spot
                            GetTimeNeededToTravel(timeWPFrom, timeWPTo)));
                    }
                }
            }
            /// <summary>
            /// Calculates the time to move from one time-waypoint to another.
            /// </summary>
            /// <param name="from">The start node.</param>
            /// <param name="to">The destination node.</param>
            /// <returns>The time for traveling from one node to the other.</returns>
            public double GetTimeNeededToTravel(TimeWaypoint from, TimeWaypoint to)
            {
                return
                    // --> Calculate time needed for turning towards new orientation
                    Math.Abs(Circle.GetOrientationDifference(from.Orientation, to.Orientation)) / PI2 * TurnSpeed +
                    // --> Calculate time needed for driving the given distance
                    Distances.CalculateEuclid(from.OriginalWaypoint, to.OriginalWaypoint, Instance.WrongTierPenaltyDistance) / Speed;
            }
        }
        /// <summary>
        /// Joins a waypoint of an instance with an orientation which the robot is facing.
        /// </summary>
        public class TimeWaypoint
        {
            /// <summary>
            /// The original waypoint reflected by this orientation extended node.
            /// </summary>
            public Waypoint OriginalWaypoint;
            /// <summary>
            /// The inbound waypoint that caused the generation of the time-waypoint for the original waypoint.
            /// </summary>
            public Waypoint InboundWaypoint;
            /// <summary>
            /// The orientation the robot is facing after approaching this node.
            /// </summary>
            public double Orientation;
            /// <summary>
            /// All connections of this node to other nodes or other orientations.
            /// </summary>
            public List<Tuple<TimeWaypoint, double>> Edges = new List<Tuple<TimeWaypoint, double>>();
            /// <summary>
            /// Creates a string representation of this object.
            /// </summary>
            /// <returns>The string representing this object.</returns>
            public override string ToString()
            {
                return
                    "(" + InboundWaypoint.X.ToString(IOConstants.FORMATTER) + "," + InboundWaypoint.Y.ToString(IOConstants.FORMATTER) + ") -> (" +
                    OriginalWaypoint.X.ToString(IOConstants.FORMATTER) + "," + OriginalWaypoint.Y.ToString(IOConstants.FORMATTER) + ") - O: " + Orientation.ToString(IOConstants.FORMATTER);
            }
        }
        /// <summary>
        /// Used to store the search tree of A*.
        /// </summary>
        private class TimeWaypointSearchDatapoint
        {
            /// <summary>
            /// Creates a new waypoint search data object.
            /// </summary>
            /// <param name="timeTraveled">The time traveled so far.</param>
            /// <param name="timeToGoal">The estimated time towards the destination.</param>
            /// <param name="waypoint">The waypoint this data object belongs to.</param>
            /// <param name="parentMove">The data of the parent.</param>
            /// <param name="depth">The steps we did to come here.</param>
            public TimeWaypointSearchDatapoint(double timeTraveled, double timeToGoal, TimeWaypoint waypoint, TimeWaypointSearchDatapoint parentMove, int depth) { TimeTraveled = timeTraveled; TimeToGoal = timeToGoal; Waypoint = waypoint; ParentMove = parentMove; Depth = depth; }
            /// <summary>
            /// The distance travelled so far.
            /// </summary>
            public double TimeTraveled;
            /// <summary>
            /// The estimated distance towards the destination.
            /// </summary>
            public double TimeToGoal;
            /// <summary>
            /// The waypoint this data object belongs to.
            /// </summary>
            public TimeWaypoint Waypoint;
            /// <summary>
            /// The steps we did to come here.
            /// </summary>
            public int Depth;
            /// <summary>
            /// The data of the parent.
            /// </summary>
            public TimeWaypointSearchDatapoint ParentMove;
        }

        #endregion

        /// <summary>
        /// Creates a new manager.
        /// </summary>
        /// <param name="instance">The instance.</param>
        public TimeEfficientPathManager(Instance instance) { _instance = instance; }
        /// <summary>
        /// The instance.
        /// </summary>
        private Instance _instance;
        /// <summary>
        /// The time graph.
        /// </summary>
        private TimeGraph _timeGraph;
        /// <summary>
        /// Contains all shortest paths calculated so far.
        /// </summary>
        MultiKeyDictionary<Waypoint, Waypoint, bool, double> _shortestPaths = new MultiKeyDictionary<Waypoint, Waypoint, bool, double>();
        /// <summary>
        /// Estimates the time it takes to travel from one waypoint to another.
        /// </summary>
        /// <param name="from">The start node.</param>
        /// <param name="to">The destination node.</param>
        /// <returns>The estimated time it takes to travel between the nodes.</returns>
        private double EstimateTravelTime(TimeWaypoint from, Waypoint to)
        {
            // Init, if not done yet
            if (_timeGraph == null)
                _timeGraph = new TimeGraph(_instance);
            // --> Calculate time needed for driving the given distance
            double travelTime = Distances.CalculateEuclid(from.OriginalWaypoint, to, _instance.WrongTierPenaltyDistance) / _timeGraph.Speed;
            // Check whether we reached the goal already, thus, eliminating the need for turning
            if (from.OriginalWaypoint == to)
                // No turning necessary - only consider time for driving
                return travelTime;
            else
                // In addition to the drive time also consider the effort for turning
                return
                    // --> Calculate time needed for turning towards new orientation
                    Math.Abs(Circle.GetOrientationDifference(from.Orientation,
                        Circle.GetOrientation(from.OriginalWaypoint.X, from.OriginalWaypoint.Y, to.X, to.Y))) / TimeGraph.PI2 * _timeGraph.TurnSpeed +
                    // --> Add time for driving
                    travelTime;
        }
        /// <summary>
        /// Uses A* to find the distance from the startNode to the destinationNode.
        /// </summary>
        /// <param name="start">The starting waypoint.</param>
        /// <param name="destination">The destination waypoint.</param>
        /// <param name="wrongTierPenalty">The penalty for the waypoints not being on the same tier.</param>
        /// <param name="emulatePodCarrying">Indicates whether to calculate a path that is ensured to be safe for carrying a pod on it.</param>
        /// <returns>The distance.</returns>
        private double CalculateShortestPath(Waypoint start, Waypoint destination, double wrongTierPenalty, bool emulatePodCarrying)
        {
            if (start == null || destination == null)
                return double.PositiveInfinity;
            if (start == destination)
                return 0;
            // Determine shortest time
            double shortestTime = double.PositiveInfinity;
            foreach (var startNode in _timeGraph.TimeWaypointsOfOriginalWaypoints[start])
            {
                Dictionary<TimeWaypoint, TimeWaypointSearchDatapoint> openLocations = new Dictionary<TimeWaypoint, TimeWaypointSearchDatapoint>();
                Dictionary<TimeWaypoint, TimeWaypointSearchDatapoint> closedLocations = new Dictionary<TimeWaypoint, TimeWaypointSearchDatapoint>();
                openLocations[startNode] = new TimeWaypointSearchDatapoint(0.0, EstimateTravelTime(startNode, destination), startNode, null, 0);

                // Loop until end is found
                while (true)
                {
                    // Find lowest cost waypoint in openLocations
                    KeyValuePair<TimeWaypoint, TimeWaypointSearchDatapoint> currentNodeKVP = openLocations.ArgMin(w => w.Value.TimeTraveled + w.Value.TimeToGoal);
                    // Something wrong happened -can't find the end
                    if (currentNodeKVP.Equals(default(KeyValuePair<TimeWaypoint, TimeWaypointSearchDatapoint>)))
                        break;
                    TimeWaypoint currentNode = currentNodeKVP.Key;

                    // Grab the details about the current waypoint
                    TimeWaypointSearchDatapoint currentNodeData = currentNodeKVP.Value;

                    // If the closest is also the destination or out of iterations
                    if (currentNode.OriginalWaypoint == destination)
                    {
                        // Update traveled time
                        shortestTime = Math.Min(shortestTime, currentNodeData.TimeTraveled);
                        break;
                    }

                    // Transfer closest from open to closed list
                    closedLocations[currentNode] = currentNodeKVP.Value;
                    openLocations.Remove(currentNode);

                    // Expand all the moves
                    foreach (var edge in currentNode.Edges)
                    {
                        // Check whether the node is already on closed
                        if (closedLocations.ContainsKey(edge.Item1))
                            // Don't deal with anything already on the closed list (don't want loops)
                            continue;

                        // Can't use a pod storage location for the path
                        if (emulatePodCarrying && edge.Item1.OriginalWaypoint.PodStorageLocation && edge.Item1.OriginalWaypoint != destination)
                            continue;

                        // Tag on more distance for a node on the wrong level
                        double additionalDistance = 0;
                        if (edge.Item1.OriginalWaypoint.Tier != destination.Tier)
                            additionalDistance += wrongTierPenalty;

                        // If it's not in the open list, add it
                        if (!openLocations.ContainsKey(edge.Item1))
                        {
                            openLocations[edge.Item1] =
                                new TimeWaypointSearchDatapoint(
                                    currentNodeData.TimeTraveled + edge.Item2, // The distance already traveled
                                    EstimateTravelTime(edge.Item1, destination) + additionalDistance, // The approximate distance to the goal
                                    edge.Item1,  // The node itself
                                    currentNodeData, // Parent data 
                                    currentNodeData.Depth + 1); // The current depth
                        }
                        else
                        {
                            // It's already in the open list, but see if this new path is better
                            TimeWaypointSearchDatapoint oldPath = openLocations[edge.Item1];
                            // Replace it with the new one, if better
                            if (oldPath.TimeTraveled > currentNodeData.TimeTraveled + edge.Item2)
                            {
                                oldPath.TimeTraveled = currentNodeData.TimeTraveled + edge.Item2; // The distance already traveled
                                oldPath.TimeToGoal = EstimateTravelTime(edge.Item1, destination) + additionalDistance; // The approximate distance to the goal
                                oldPath.Waypoint = edge.Item1; // The node itself
                                oldPath.ParentMove = currentNodeData; // Parent data
                                oldPath.Depth = currentNodeData.Depth + 1; // The current depth
                            }
                        }
                    }
                }
            }
            // Return time
            return shortestTime;
        }

        /// <summary>
        /// Estimates the time for traveling using the manhattan metric.
        /// </summary>
        /// <param name="from">The from part of the trip.</param>
        /// <param name="to">The to part of the trip.</param>
        /// <param name="instance">The instance.</param>
        /// <returns>The time needed to for conducting the trip according to the manhattan metric.</returns>
        public double EstimateShortestPathManhattan(Circle from, Circle to, Instance instance)
        {
            // Init, if not done yet
            if (_timeGraph == null)
                _timeGraph = new TimeGraph(_instance);
            return
                // Assume quarter rotation for manhattan metric
                (Math.PI / 2) / TimeGraph.PI2 * _timeGraph.TurnSpeed +
                // Use full manhattan distance to estimate travel time
                Distances.CalculateManhattan(from, to, instance.WrongTierPenaltyDistance) / _timeGraph.Speed;
        }
        /// <summary>
        /// Estimates the time for traveling using the manhattan metric.
        /// </summary>
        /// <param name="from">The from part of the trip.</param>
        /// <param name="to">The to part of the trip.</param>
        /// <param name="instance">The instance.</param>
        /// <returns>The time needed to for conducting the trip according to the manhattan metric.</returns>
        public double EstimateShortestPathEuclid(Circle from, Circle to, Instance instance)
        {
            // Init, if not done yet
            if (_timeGraph == null)
                _timeGraph = new TimeGraph(_instance);
            return
                // Use full manhattan distance to estimate travel time
                Distances.CalculateEuclid(from, to, instance.WrongTierPenaltyDistance) / _timeGraph.Speed;
        }
        /// <summary>
        /// Calculates the shortest time path from first to the second waypoint.
        /// </summary>
        /// <param name="from">The starting waypoint.</param>
        /// <param name="to">The destination waypoint.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="emulatePodCarrying">Indicates whether to calculate a path that is ensured to be safe for carrying a pod on it.</param>
        /// <returns>The time of the best path. If both waypoints are not located on the same tier, penalty costs will be added. If there is no connection, an infinite length is returned.</returns>
        public double GetShortestPath(Waypoint from, Waypoint to, Instance instance, bool emulatePodCarrying)
        {
            // Init, if not done yet
            if (_timeGraph == null)
                _timeGraph = new TimeGraph(_instance);
            // Check whether we have the value already
            if (!_shortestPaths.ContainsKey(from, to, emulatePodCarrying))
                // We do not have the value yet - calculate it
                _shortestPaths[from, to, emulatePodCarrying] = CalculateShortestPath(from, to, instance.WrongTierPenaltyDistance, emulatePodCarrying);
            // Return the value
            return _shortestPaths[from, to, emulatePodCarrying];
        }
    }

    #endregion
}
