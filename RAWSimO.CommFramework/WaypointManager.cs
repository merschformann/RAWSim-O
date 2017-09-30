using RAWSimO.Core.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.CommFramework
{
    public class WaypointManager
    {
        private Dictionary<int, DTOWaypoint> _waypoints;
        private Dictionary<DTOWaypoint, int> _waypointsReverse;
        private Dictionary<DTOWaypoint, HashSet<DTOWaypoint>> _connections;
        private Dictionary<string, int> _rfidTranslations;
        private Dictionary<int, string> _waypointTranslations;
        private Action<string> _infoOutput;

        public WaypointManager(string waypointFilePath, string dictionaryFilePath, Action<string> outputFunction)
        {
            _infoOutput = outputFunction;
            ReadWaypointsFile(waypointFilePath);
            ReadDictFile(dictionaryFilePath);
        }
        /// <summary>
        /// Used to output useful information if desired.
        /// </summary>
        /// <param name="msg">The message to output.</param>
        private void Output(string msg) { if (_infoOutput != null) { _infoOutput(msg); } }
        /// <summary>
        /// Indicates whether there is a connection from the first to the second waypoint.
        /// </summary>
        /// <param name="first">The first waypoint the connection is outgoing from.</param>
        /// <param name="second">The second waypoint the connection is incoming towards.</param>
        /// <returns><code>true</code> if the connection exists, <code>false</code> otherwise.</returns>
        public bool IsConnected(DTOWaypoint first, DTOWaypoint second) { return _connections[first].Contains(second); }
        /// <summary>
        /// Translates the RFID-tag string into a waypoint-ID.
        /// </summary>
        /// <param name="rfidTag">The RFID-tag</param>
        /// <returns>The waypoint ID belonging to the tag.</returns>
        public int Translate(string rfidTag) { return _rfidTranslations[rfidTag]; }
        /// <summary>
        /// Translates the waypoint-ID to a <code>DTOWaypoint</code> exposing more information about the waypoint.
        /// </summary>
        /// <param name="id">The ID of the waypoint.</param>
        /// <returns>The object belonging to the ID.</returns>
        public DTOWaypoint Translate(int id) { return _waypoints[id]; }
        /// <summary>
        /// Indicates whether the given RFID-tag is recognized by the system and there is a valid waypoint linked to it.
        /// </summary>
        /// <param name="rfidTag">The RFID-tag to check.</param>
        /// <returns><code>true</code> if the tag is known, <code>false</code> otherwise.</returns>
        public bool IsKnownTag(string rfidTag) { return _rfidTranslations.ContainsKey(rfidTag) && _waypointTranslations.ContainsKey(_rfidTranslations[rfidTag]); }

        #region Path planning

        /// <summary>
        /// Used to store the search tree of A*.
        /// </summary>
        private class WaypointSearchData
        {
            public WaypointSearchData(double distanceTraveled, double distanceToGoal, DTOWaypoint waypoint, WaypointSearchData parentMove, int depth)
            {
                DistanceTraveled = distanceTraveled; DistanceToGoal = distanceToGoal; Waypoint = waypoint; ParentMove = parentMove; Depth = depth;
            }
            public double DistanceTraveled;
            public double DistanceToGoal;
            public DTOWaypoint Waypoint;
            public int Depth;
            public WaypointSearchData ParentMove;
        }

        /// <summary>
        /// Used to store the result of a path search.
        /// </summary>
        public class WaypointSearchResult
        {
            /// <summary>
            /// The calculated travel route.
            /// </summary>
            public LinkedList<DTOWaypoint> Route = new LinkedList<DTOWaypoint>();
            /// <summary>
            /// Stores another step to this route. The steps have to be added in backwards order.
            /// </summary>
            /// <param name="waypoint">The waypoint of the route to add.</param>
            public void AddStep(DTOWaypoint waypoint) { Route.AddFirst(waypoint); }
        }

        private double GetDistance(DTOWaypoint from, DTOWaypoint to) { return Math.Sqrt((from.X - to.X) * (from.X - to.X) + (from.Y - to.Y) * (from.Y - to.Y)); }

        public WaypointSearchResult PlanPath(DTOWaypoint startNode, DTOWaypoint destinationNode)
        {
            Dictionary<DTOWaypoint, WaypointSearchData> openLocations = new Dictionary<DTOWaypoint, WaypointSearchData>();
            Dictionary<DTOWaypoint, WaypointSearchData> closedLocations = new Dictionary<DTOWaypoint, WaypointSearchData>();
            openLocations[startNode] = new WaypointSearchData(0.0, GetDistance(startNode, destinationNode), startNode, null, 0);

            // Don't move if already at destination
            if (startNode == destinationNode)
                return null;

            // Maximum number of waypoints to look at in search
            int maxNumIterations = 3000;
            int numIterations = 0;

            // Loop until end is found
            while (true)
            {
                // Find lowest cost waypoint in openLocations
                DTOWaypoint currentNode = null;
                double lowestCost = double.PositiveInfinity;
                foreach (var w in openLocations.Keys)
                {
                    if (openLocations[w].DistanceTraveled + openLocations[w].DistanceToGoal < lowestCost)
                    {
                        currentNode = w;
                        lowestCost = openLocations[w].DistanceTraveled + openLocations[w].DistanceToGoal;
                    }
                }
                // Something wrong happened -can't find the end
                if (currentNode == null)
                    return null;

                // Grab the details about the current waypoint
                WaypointSearchData currentNodeData = openLocations[currentNode];

                // If the closest is also the destination or out of iterations
                if (currentNode == destinationNode || numIterations++ == maxNumIterations)
                {
                    // Init result
                    WaypointSearchResult result = new WaypointSearchResult();
                    // Found it on the first move
                    if (currentNodeData.ParentMove == null)
                        return null;
                    // Go back to the first move made
                    while (currentNodeData != null)
                    {
                        result.AddStep(currentNodeData.Waypoint);
                        currentNodeData = currentNodeData.ParentMove;
                    }
                    return result;
                }

                // Transfer closest from open to closed list
                closedLocations[currentNode] = openLocations[currentNode];
                openLocations.Remove(currentNode);

                // Expand all the moves
                foreach (var successorNode in currentNode.Paths.Select(p => _waypoints[p]))
                {
                    // Check whether the node is already on closed
                    if (closedLocations.ContainsKey(successorNode))
                        // Don't deal with anything already on the closed list (don't want loops)
                        continue;

                    // If it's not in the open list, add it
                    if (!openLocations.ContainsKey(successorNode))
                    {
                        openLocations[successorNode] =
                            new WaypointSearchData(
                                currentNodeData.DistanceTraveled + GetDistance(currentNode, successorNode), // The distance already traveled
                                GetDistance(successorNode, destinationNode), // The approximate distance to the goal
                                successorNode,  // The node itself
                                currentNodeData, // Parent data 
                                currentNodeData.Depth + 1); // The current depth
                    }
                    else
                    {
                        // It's already in the open list, but see if this new path is better
                        WaypointSearchData oldPath = openLocations[successorNode];
                        // Replace it with the new one
                        if (oldPath.DistanceTraveled + oldPath.DistanceToGoal > currentNodeData.DistanceTraveled + GetDistance(currentNode, successorNode))
                            openLocations[successorNode] =
                                new WaypointSearchData(
                                    currentNodeData.DistanceTraveled + GetDistance(currentNode, successorNode), // The distance already traveled
                                    GetDistance(successorNode, destinationNode), // The approximate distance to the goal
                                    successorNode, // The node itself
                                    currentNodeData, // Parent data
                                    currentNodeData.Depth + 1); // The current depth
                    }
                }
            }
        }

        #endregion

        #region I/O

        private void ReadWaypointsFile(string filePath)
        {
            Output("Reading waypoint file: " + filePath);
            DTOInstance instance = InstanceIO.ReadDTOInstance(filePath);
            _waypoints = instance.Waypoints.ToDictionary(k => k.ID, v => v);
            _waypointsReverse = instance.Waypoints.ToDictionary(k => k, v => v.ID);
            _connections = _waypoints.ToDictionary(k => k.Value, v => new HashSet<DTOWaypoint>(v.Value.Paths.Select(p => _waypoints[p])));
            Output("Read " + _waypoints.Count + " waypoints with " + _connections.Sum(c => c.Value.Count) + " connections!");
        }
        private void ReadDictFile(string filePath)
        {
            // Parse all entries in the file
            Output("Reading dictionary file: " + filePath);
            List<Tuple<int, string>> entries = new List<Tuple<int, string>>();
            using (StreamReader sr = new StreamReader(filePath))
            {
                // Read all lines
                string line = "";
                while ((line = sr.ReadLine()) != null)
                {
                    // Ignore empty or comment lines
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;
                    // Parse the dictionary entry
                    string[] entry = line.Split(';').Select(e => e.Trim()).ToArray();
                    entries.Add(new Tuple<int, string>(int.Parse(entry[0]), entry[1]));
                }
            }
            // Connect values to the waypoint system
            _rfidTranslations = entries.ToDictionary(k => k.Item2, v => v.Item1);
            _waypointTranslations = entries.ToDictionary(k => k.Item1, v => v.Item2);
            // Log missing and additional waypoints
            Output("Read " + entries.Count + " entries");
            Output("Found " + _waypoints.Count(wp => _waypointTranslations.ContainsKey(wp.Key)) + " translations for our " + _waypoints.Count + " waypoints");
        }

        #endregion
    }
}
