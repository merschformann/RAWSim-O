// If defined, activates assertion of tractable requests to the path planners
//#define DEBUGINTRACTABLEREQUESTS

using RAWSimO.Core.Bots;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Helper;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.Waypoints;
using RAWSimO.MultiAgentPathFinding.DataStructures;
using RAWSimO.MultiAgentPathFinding;
using RAWSimO.MultiAgentPathFinding.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using RAWSimO.Core.Statistics;
using RAWSimO.Core.IO;
using RAWSimO.Core.Geometrics;
using RAWSimO.Toolbox;

namespace RAWSimO.Core.Control
{
    /// <summary>
    /// The path manager implementation.
    /// </summary>
    public abstract class PathManager : IUpdateable, IStatTracker
    {
        #region Attributes

        /// <summary>
        /// path finding optimizer
        /// </summary>
        public bool Log;

        /// <summary>
        /// Gets or sets the data points.
        /// </summary>
        /// <value>
        /// The data points.
        /// </value>
        public List<PathFindingDatapoint> StatDataPoints { get; set; }

        /// <summary>
        /// path finding optimizer
        /// </summary>
        public PathFinder PathFinder;

        /// <summary>
        /// current instance
        /// </summary>
        public Instance Instance;

        /// <summary>
        /// ids of the way points
        /// </summary>
        protected BiDictionary<Waypoint, int> _waypointIds;

        /// <summary>
        /// ids of the elevators
        /// </summary>
        protected Dictionary<int, Elevator> _elevatorIds;

        /// <summary>
        /// Contains meta information about the nodes that is passed to the path planning engine.
        /// </summary>
        private Dictionary<Waypoint, NodeInfo> _nodeMetaInfo;

        /// <summary>
        /// The queue managers
        /// </summary>
        private Dictionary<Waypoint, QueueManager> _queueManagers;

        /// <summary>
        /// The reservation table
        /// </summary>
        protected ReservationTable _reservationTable;

        /// <summary>
        /// The reservations
        /// </summary>
        protected Dictionary<BotNormal, List<ReservationTable.Interval>> _reservations;

        /// <summary>
        /// The stop watch
        /// </summary>
        protected Stopwatch _stopWatch;

        /// <summary>
        /// The time stamp of the last optimization call
        /// </summary>
        protected double _lastCallTimeStamp = double.MinValue / 10.0;

        #endregion

        #region core

        /// <summary>
        /// Initializes a new instance of the <see cref="PathManager"/> class.
        /// </summary>
        protected PathManager(Instance instance)
        {
            //instance
            this.Instance = instance;
            this.Log = true; //log => high memory consumption
        }

        /// <summary>
        /// convert way point graph -> graph for multi agent path finding
        /// </summary>
        /// <returns>The generated graph.</returns>
        protected Graph GenerateGraph()
        {
            var waypointGraph = Instance.WaypointGraph;

            //Give every way point an unique id
            _waypointIds = new BiDictionary<Waypoint, int>();
            int id = 0;
            foreach (var tier in waypointGraph.GetWayPoints())
                foreach (var waypoint in tier.Value)
                    _waypointIds.Add(waypoint, id++);

            //initiate queue managers
            _queueManagers = new Dictionary<Waypoint, QueueManager>();
            foreach (IQueuesOwner queueOwner in Instance.InputStations.Cast<IQueuesOwner>().Union(Instance.OutputStations.Cast<IQueuesOwner>().Union(Instance.Elevators.Cast<IQueuesOwner>())))
                foreach (var queue in queueOwner.Queues)
                    _queueManagers.Add(queue.Key, new QueueManager(queue.Key, queue.Value, this));

            //create the lightweight graph
            var graph = new Graph(_waypointIds.Count);

            // Create collection of all node meta information
            _nodeMetaInfo = Instance.Waypoints.ToDictionary(k => k, v => new NodeInfo()
            {
                ID = _waypointIds[v],
                IsQueue = v.QueueManager != null,
                QueueTerminal = v.QueueManager != null ? _waypointIds[v.QueueManager.QueueWaypoint] : -1,
            });
            // Submit node meta info to graph
            foreach (var waypoint in Instance.Waypoints)
                graph.NodeInfo[_waypointIds[waypoint]] = _nodeMetaInfo[waypoint];

            //create edges
            foreach (var tier in waypointGraph.GetWayPoints())
            {
                foreach (var waypointFrom in tier.Value)
                {

                    //create Array
                    Edge[] outgoingEdges = new Edge[waypointFrom.Paths.Count(w => w.Tier == tier.Key)];
                    ElevatorEdge[] outgoingElevatorEdges = new ElevatorEdge[waypointFrom.Paths.Count(w => w.Tier != tier.Key)];

                    //fill Array
                    int edgeId = 0;
                    int elevatorEdgeId = 0;
                    foreach (var waypointTo in waypointFrom.Paths)
                    {
                        //elevator edge
                        if (waypointTo.Tier != tier.Key)
                        {
                            var elevator = Instance.Elevators.First(e => e.ConnectedPoints.Contains(waypointFrom) && e.ConnectedPoints.Contains(waypointTo));

                            outgoingElevatorEdges[elevatorEdgeId++] = new ElevatorEdge
                            {
                                From = _waypointIds[waypointFrom],
                                To = _waypointIds[waypointTo],
                                Distance = 0,
                                TimeTravel = elevator.GetTiming(waypointFrom, waypointTo),
                                Reference = elevator
                            };
                        }
                        else
                        {
                            //normal edge
                            int angle = Graph.RadToDegree(Math.Atan2(waypointTo.Y - waypointFrom.Y, waypointTo.X - waypointFrom.X));
                            Edge edge = new Edge
                            {
                                From = _waypointIds[waypointFrom],
                                To = _waypointIds[waypointTo],
                                Distance = waypointFrom.GetDistance(waypointTo),
                                Angle = (short)angle,
                                FromNodeInfo = _nodeMetaInfo[waypointFrom],
                                ToNodeInfo = _nodeMetaInfo[waypointTo],
                            };
                            outgoingEdges[edgeId++] = edge;
                        }
                    }

                    //set Array
                    graph.Edges[_waypointIds[waypointFrom]] = outgoingEdges;
                    graph.PositionX[_waypointIds[waypointFrom]] = waypointFrom.X;
                    graph.PositionY[_waypointIds[waypointFrom]] = waypointFrom.Y;
                    if (outgoingElevatorEdges.Length > 0)
                        graph.ElevatorEdges[_waypointIds[waypointFrom]] = outgoingElevatorEdges;
                }
            }

            return graph;
        }

        /// <summary>
        /// Updates all lock and obstacle information for all connections.
        /// </summary>
        private void UpdateLocksAndObstacles()
        {
            // First reset locked / occupied by obstacle info
            foreach (var nodeInfo in _nodeMetaInfo)
            {
                nodeInfo.Value.IsLocked = false;
                nodeInfo.Value.IsObstacle = false;
            }
            // Prepare waypoints blocked within the queues
            IEnumerable<Waypoint> queueBlockedNodes = _queueManagers.SelectMany(m => m.Value.LockedWaypoints.Keys);
            // Prepare waypoints blocked by idling robots
            IEnumerable<Waypoint> idlingAgentsBlockedNodes = Instance.Bots.Cast<BotNormal>().Where(b => b.IsResting()).Select(b => b.CurrentWaypoint);
            // Prepare obstacles by placed pods
            IEnumerable<Waypoint> podPositions = Instance.WaypointGraph.GetPodPositions();
            // Now update with new locked / occupied by obstacle info
            foreach (var lockedWP in queueBlockedNodes.Concat(idlingAgentsBlockedNodes))
                _nodeMetaInfo[lockedWP].IsLocked = true;
            foreach (var obstacleWP in podPositions)
                _nodeMetaInfo[obstacleWP].IsObstacle = true;
            // Get blocked nodes
            // Prepare all locked nodes
            var blockedNodes = new HashSet<int>(
                // Add waypoints locked by the queue managers
                queueBlockedNodes.Select(w => _waypointIds[w])
                    // Add waypoints locked by not moving robots 
                    .Concat(idlingAgentsBlockedNodes.Select(w => _waypointIds[w])));
            // Get obstacles
            var obstacles = new HashSet<int>(podPositions.Select(wp => _waypointIds[wp]));
        }

        /// <summary>
        /// optimize path
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        private void _reoptimize(double currentTime)
        {

            //statistics
            _statStart(currentTime);

            // Log timing
            DateTime before = DateTime.Now;

            // Get agents
            Dictionary<BotNormal, Agent> botAgentsDict;
            getBotAgentDictionary(out botAgentsDict, currentTime);
            // Update locks and obstacles
            UpdateLocksAndObstacles();

#if DEBUGINTRACTABLEREQUESTS
            // Check for intractable requests for the path planners
            IEnumerable<KeyValuePair<BotNormal, Agent>> blockedAgents = botAgentsDict.Where(agent => agent.Key.RequestReoptimization && /*agent.Value.NextNode != agent.Value.DestinationNode &&*/ blockedNodes.Contains(agent.Value.DestinationNode));
            if (blockedAgents.Any())
                Debug.Fail("Cannot navigate to a locked node!", "Agents (agent/next/dest/currType/nextType/destType):\n" + string.Join("\n",
                    blockedAgents.Select(a =>
                    {
                        Func<Waypoint, string> getNodeType = (Waypoint w) =>
                        {
                            return (
                                w == null ? "n" :
                                w.PodStorageLocation ? "s" :
                                w.InputStation != null ? "i" :
                                w.OutputStation != null ? "o" :
                                w.Elevator != null ? "e" :
                                w.IsQueueWaypoint ? "q" :
                                "w");
                        };
                        return
                            a.Value.ID + "/" +
                            a.Value.NextNode + "/" +
                            a.Value.DestinationNode + "/" +
                            getNodeType(a.Key.CurrentWaypoint) + "/" +
                            getNodeType(a.Key.NextWaypoint) + "/" +
                            getNodeType(a.Key.DestinationWaypoint);
                    })));
#endif

            //get path => optimize!
            PathFinder.FindPaths(currentTime, botAgentsDict.Values.ToList());

            foreach (var botAgent in botAgentsDict)
                botAgent.Key.Path = botAgent.Value.Path;

            // Calculate time it took to plan the path(s)
            Instance.Observer.TimePathPlanning((DateTime.Now - before).TotalSeconds);

            _statEnd(currentTime);
        }

        /// <summary>
        /// start the statistical output.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        private void _statStart(double currentTime)
        {
            if (!Log)
                return;

            //create data list
            if (StatDataPoints == null)
                StatDataPoints = new List<PathFindingDatapoint>();

            StatDataPoints.Add(new PathFindingDatapoint(currentTime, 0, Instance.Bots.Count(b => ((BotNormal)b).RequestReoptimization)));

            if (_stopWatch == null)
                _stopWatch = new Stopwatch();

            _stopWatch.Restart();
        }

        /// <summary>
        /// end the statistical output.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        private void _statEnd(double currentTime)
        {
            if (!Log)
                return;

            _stopWatch.Stop();

            //add elapsed seconds
            StatDataPoints[StatDataPoints.Count - 1].Runtime = _stopWatch.ElapsedMilliseconds / 1000.0;
        }

        /// <summary>
        /// create Agents from bot positions and destinations
        /// </summary>
        /// <returns>agents</returns>
        private void getBotAgentDictionary(out Dictionary<BotNormal, Agent> botAgentDictionary, double currentTime)
        {
            botAgentDictionary = new Dictionary<BotNormal, Agent>();

            foreach (var abot in Instance.Bots)
            {
                var bot = abot as BotNormal;

                // Get way points
                Waypoint nextWaypoint = (bot.NextWaypoint != null) ? bot.NextWaypoint : bot.CurrentWaypoint;
                Waypoint finalDestination = (bot.DestinationWaypoint == null) ? bot.CurrentWaypoint : bot.DestinationWaypoint;
                Waypoint destination = finalDestination;

                // Has the destination a queue?
                if (bot.DestinationWaypoint != null && _queueManagers.ContainsKey(bot.DestinationWaypoint))
                    // Use the place in queue as the destination
                    destination = _queueManagers[bot.DestinationWaypoint].getPlaceInQueue(bot);
                // If already within a queue, there is no path finding needed => Queue manager does it for us
                if (bot.IsQueueing)
                    continue;
                // Ignore bots for which origin matches destination
                if (nextWaypoint == destination)
                    continue;

                // Create reservationToNextNode
                var reservationsToNextNode = new List<ReservationTable.Interval>(_reservations[bot]);
                reservationsToNextNode.RemoveAt(reservationsToNextNode.Count - 1); //remove last blocking node

                if (bot.NextWaypoint == null)
                    reservationsToNextNode.Clear();

                // Create agent
                var agent = new Agent
                {
                    ID = bot.ID,
                    NextNode = _waypointIds[nextWaypoint],
                    ReservationsToNextNode = reservationsToNextNode,
                    ArrivalTimeAtNextNode = (reservationsToNextNode.Count == 0) ? currentTime : Math.Max(currentTime, reservationsToNextNode[reservationsToNextNode.Count - 1].End),
                    OrientationAtNextNode = bot.GetTargetOrientation(),
                    DestinationNode = _waypointIds[destination],
                    FinalDestinationNode = _waypointIds[finalDestination],
                    Path = bot.Path, //path reference => will be filled
                    FixedPosition = bot.hasFixedPosition(),
                    Resting = bot.IsResting(),
                    CanGoThroughObstacles = Instance.ControllerConfig.PathPlanningConfig.CanTunnel && bot.Pod == null,
                    Physics = bot.Physics,
                    RequestReoptimization = bot.RequestReoptimization,
                    Queueing = bot.IsQueueing,
                    NextNodeObject = nextWaypoint,
                    DestinationNodeObject = destination,
                };
                // Add agent
                botAgentDictionary.Add(bot, agent);

                Debug.Assert(nextWaypoint.Tier.ID == destination.Tier.ID);
            }
        }

        /// <summary>
        /// Gets the way point by node identifier.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        public Waypoint GetWaypointByNodeId(int node)
        {
            return _waypointIds[node];
        }

        /// <summary>
        /// Gets the way point by node identifier.
        /// </summary>
        /// <param name="waypoint">The node.</param>
        /// <returns>The id of the node.</returns>
        public int GetNodeIdByWaypoint(Waypoint waypoint)
        {
            return _waypointIds[waypoint];
        }
        #endregion

        #region IUpdateable Members

        /// <summary>
        /// The next event when this element has to be updated.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns>The next time this element has to be updated.</returns>
        public double GetNextEventTime(double currentTime)
        {
            return double.PositiveInfinity;
        }

        /// <summary>
        /// Updates the specified last time.
        /// </summary>
        /// <param name="lastTime">The last time.</param>
        /// <param name="currentTime">The current time.</param>
        public virtual void Update(double lastTime, double currentTime)
        {
            //manage queues
            foreach (var queueManager in _queueManagers.Values)
                queueManager.Update();

            //reorganize table
            if (_reservationTable == null)
                _initReservationTable();
            _reservationTable.Reorganize(currentTime);

            //check if minimum time span was exceeded
            if (_lastCallTimeStamp + Instance.ControllerConfig.PathPlanningConfig.Clocking > currentTime)
                return;

            //check if any bot request a re-optimization and reset the flag
            var reoptimize = Instance.Bots.Any(b => ((BotNormal)b).RequestReoptimization);

            //NextReoptimization < currentTime with < instead of <=, because we want to update in the next call.
            //So we wait until all bots have updated from lastTime to currentTime
            if (reoptimize)
            {
                //call re optimization
                _lastCallTimeStamp = currentTime;
                _reoptimize(currentTime);

                //reset flag
                foreach (var bot in Instance.Bots)
                    ((BotNormal)bot).RequestReoptimization = false;
            }

        }

        /// <summary>
        /// Notifies the path manager when the bot has a new destination.
        /// </summary>
        /// <param name="bot">The bot.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        internal void notifyBotNewDestination(BotNormal bot)
        {
            if (bot.DestinationWaypoint == null)
                return;

            if (_queueManagers.ContainsKey(bot.DestinationWaypoint))
                _queueManagers[bot.DestinationWaypoint].onBotJoinQueue(bot);
        }

        /// <summary>
        /// Checks weather the bot can go to the next way point without collisions.
        /// </summary>
        /// <param name="botNormal">The bot.</param>
        /// <param name="currentTime">The current time.</param>
        /// <param name="waypointStart">The way point start.</param>
        /// <param name="waypointEnd">The way point end.</param>
        /// <param name="blockCurrentWaypointUntil">Block duration.</param>
        /// <param name="rotationDuration">Rotation duration.</param>
        /// <returns></returns>
        public bool RegisterNextWaypoint(BotNormal botNormal, double currentTime, double blockCurrentWaypointUntil, double rotationDuration, Waypoint waypointStart, Waypoint waypointEnd)
        {
            //get checkpoints
            var tmpReservations = _reservationTable.CreateIntervals(currentTime, blockCurrentWaypointUntil + rotationDuration, 0.0, botNormal.Physics, _waypointIds[waypointStart], _waypointIds[waypointEnd], true);

            if (tmpReservations == null)
                return false; //no valid way point

            //if the last node or way point is an elevator way point than add all connected nodes
            if (tmpReservations.Count >= 2)
            {
                var lastReservation = tmpReservations[tmpReservations.Count - 1];
                var elevatorWaypoint = _waypointIds[lastReservation.Node];
                if (elevatorWaypoint.Elevator != null)
                {
                    var prelastReservation = tmpReservations[tmpReservations.Count - 2];

                    foreach (var waypoint in elevatorWaypoint.Elevator.ConnectedPoints.Where(w => w != elevatorWaypoint))
                    {
                        tmpReservations.Add(new ReservationTable.Interval(_waypointIds[waypoint], prelastReservation.Start, prelastReservation.End));
                        tmpReservations.Add(new ReservationTable.Interval(_waypointIds[waypoint], lastReservation.Start, lastReservation.End));
                    }
                }
            }

            //remove current
            _reservationTable.Remove(_reservations[botNormal]);

            //check if free
            var free = _reservationTable.IntersectionFree(tmpReservations);

            //check if a pod collision can occur
            if (botNormal.Pod != null)
            {
                //checker if there is a static pod
                foreach (var interval in tmpReservations)
                {
                    if (_waypointIds[interval.Node].Pod != null)
                    {
                        //there exists a way point with a pod on it
                        free = false;
                        break;
                    }
                }
            }

            if (free)
            {
                _reservations[botNormal] = tmpReservations;

                //#RealWorldIntegration.start
                if (botNormal.Instance.SettingConfig.RealWorldIntegrationCommandOutput && botNormal.Instance.SettingConfig.LogAction != null)
                {
                    //Log the wait command
                    var sb = new StringBuilder();
                    sb.Append("#RealWorldIntegration => Bot ").Append(botNormal.ID).Append(" Drive: ");

                    if (blockCurrentWaypointUntil - currentTime > 0)
                        sb.Append("(wait: ").Append(blockCurrentWaypointUntil - currentTime).Append("s)");

                    for (var i = 1; i < _reservations[botNormal].Count - 1; i++)
                        sb.Append(_waypointIds[_reservations[botNormal][i].Node].ID).Append(";");
                    botNormal.Instance.SettingConfig.LogAction(sb.ToString());
                    // Issue the path command
                    Instance.RemoteController.RobotSubmitPath(
                        botNormal.ID,  // The ID of the robot the path is send to
                        blockCurrentWaypointUntil - currentTime, // The time the robot shall wait before executing the path
                        _reservations[botNormal].Take(_reservations[botNormal].Count - 1).Select(r => _waypointIds[r.Node].ID).ToList()); // The path to execute
                }
                //#RealWorldIntegration.end
            }

            //Debug
            //Log the wait command
            /*
            var tmp = new StringBuilder();
            tmp.Append("Bot ").Append(botNormal.ID).Append(" Drive: ");

            if (blockCurrentWaypointUntil - currentTime > 0)
                tmp.Append("(wait: ").Append(blockCurrentWaypointUntil - currentTime).Append("s)");

            for (var i = 1; i < _reservations[botNormal].Count - 1; i++)
                tmp.Append(_waypointIds[_reservations[botNormal][i].Node].ID).Append(";");
            botNormal.Instance.Configuration.LogAction(tmp.ToString());
            if (tmp.ToString().Equals(_lastWaitLog))
                botNormal.Instance.Configuration.LogAction("Same Log(for Debugging)");
            _lastWaitLog = tmp.ToString();
            */

            //(re)add intervals
            _reservationTable.Add(_reservations[botNormal]);

            return free;

        }

        /// <summary>
        /// Initializes the reservation table.
        /// </summary>
        private void _initReservationTable()
        {
            _reservationTable = new ReservationTable(PathFinder.Graph, true);

            //lasy initialization reservations
            _reservations = new Dictionary<BotNormal, List<ReservationTable.Interval>>();
            foreach (BotNormal bot in Instance.Bots)
            {
                if (bot.CurrentWaypoint == null)
                    bot.CurrentWaypoint = bot.Instance.WaypointGraph.GetClosestWaypoint(bot.Instance.Compound.BotCurrentTier[bot], bot.X, bot.Y);

                _reservations.Add(bot, new List<ReservationTable.Interval>());
                _reservations[bot].Add(new ReservationTable.Interval(_waypointIds[bot.CurrentWaypoint], 0, double.PositiveInfinity));
                _reservationTable.Add(_reservations[bot][0]);
            }
        }

        #endregion

        #region IStatTracker Members

        /// <summary>
        /// The callback that indicates that the simulation is finished and statistics have to submitted to the instance.
        /// </summary>
        public virtual void StatFinish() { /* Default case: do not flush any statistics */ }

        /// <summary>
        /// The callback indicates a reset of the statistics.
        /// </summary>
        public virtual void StatReset() { /* Default case: nothing to reset */ }

        #endregion

        #region Elevator Sequence
        /// <summary>
        /// Finds a sequence of elevators with must be visit to reach the end node.
        /// </summary>
        /// <param name="start">The start node.</param>
        /// <param name="end">The end node.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="distance">The distance.</param>
        /// <returns>
        /// elevator sequence
        /// </returns>
        public List<Tuple<Elevator, Waypoint, Waypoint>> FindElevatorSequence(BotNormal bot, Waypoint start, Waypoint end, out double distance)
        {
            //find sequence in one code line
            return Instance.Controller.PathManager.PathFinder.FindElevatorSequence(_waypointIds[start], _waypointIds[end], bot.Physics, out distance).Select(e => Tuple.Create((Elevator)e.Item1, _waypointIds[e.Item2], _waypointIds[e.Item3])).ToList();
        }
        #endregion

        #region Queue Manager

        /// <summary>
        /// Manages the path finding for bots in the queue
        /// </summary>
        internal class QueueManager
        {
            /// <summary>
            /// The queue way point
            /// </summary>
            public Waypoint QueueWaypoint;

            /// <summary>
            /// The queue
            /// </summary>
            public List<Waypoint> Queue;

            /// <summary>
            /// The locked way points
            /// </summary>
            public Dictionary<Waypoint, Bot> LockedWaypoints;

            /// <summary>
            /// The indices regarding this queue of the managed waypoints.
            /// </summary>
            private Dictionary<Waypoint, int> _queueWaypointIndices;

            /// <summary>
            /// The bots that are want to reach the way point at the beginning of the queue.
            /// </summary>
            private List<BotNormal> _managedBots;

            /// <summary>
            /// The bots that are want to reach the queue way point and already reached the queue.
            /// </summary>
            private HashSet<BotNormal> _botsInQueue;

            /// <summary>
            /// The assigned place in queue
            /// </summary>
            private Dictionary<BotNormal, Waypoint> _placeInQueue;

            /// <summary>
            /// Contains the destination of robots that are cruising along the queue (no new paths will be planned for them until they arrive).
            /// </summary>
            private Dictionary<BotNormal, List<Waypoint>> _queueCruisePaths;

            /// <summary>
            /// Contains all queue nodes acessible in a fast way.
            /// </summary>
            private QuadTree<Waypoint> _queueNodeTree;

            /// <summary>
            /// A loosely defined left border of the managed area.
            /// </summary>
            private double _queueAreaXMin;
            /// <summary>
            /// A loosely defined right border of the managed area.
            /// </summary>
            private double _queueAreaXMax;
            /// <summary>
            /// A loosely defined lower border of the managed area.
            /// </summary>
            private double _queueAreaYMin;
            /// <summary>
            /// A loosely defined upper border of the managed area.
            /// </summary>
            private double _queueAreaYMax;

            /// <summary>
            /// The maximum length of alle edges that have to be respected by the queue manager for identifying locking bots.
            /// </summary>
            private Dictionary<Waypoint, double> _maxEdgeLength;

            /// <summary>
            /// The path manager
            /// </summary>
            private PathManager _pathManager;

            /// <summary>
            /// Initializes a new instance of the <see cref="QueueManager"/> class.
            /// </summary>
            /// <param name="queueWaypoint">The queue way point.</param>
            /// <param name="queue">The queue.</param>
            /// <param name="pathManager">The path manager.</param>
            public QueueManager(Waypoint queueWaypoint, List<Waypoint> queue, PathManager pathManager)
            {
                QueueWaypoint = queueWaypoint;
                Queue = queue;
                _queueWaypointIndices = new Dictionary<Waypoint, int>();
                for (int i = 0; i < Queue.Count; i++)
                {
                    _queueWaypointIndices[Queue[i]] = i;
                    Queue[i].QueueManager = this;
                }
                Queue.RemoveAll(w => w == null);
                LockedWaypoints = new Dictionary<Waypoint, Bot>();

                _pathManager = pathManager;
                // Init storage
                _managedBots = new List<BotNormal>();
                _botsInQueue = new HashSet<BotNormal>();
                _placeInQueue = new Dictionary<BotNormal, Waypoint>();
                _queueCruisePaths = new Dictionary<BotNormal, List<Waypoint>>();
                // Define a loos queue area for some asserting
                double minArcLength = Queue.Min(firstWP => Queue.Where(wp => wp != firstWP).Min(secondWP => firstWP.GetDistance(secondWP)));
                _queueAreaXMin = Queue.Min(w => w.X) - minArcLength;
                _queueAreaXMax = Queue.Max(w => w.X) + minArcLength;
                _queueAreaYMin = Queue.Min(w => w.Y) - minArcLength;
                _queueAreaYMax = Queue.Max(w => w.Y) + minArcLength;
                // Get a quad tree to lookup real waypoints faster
                _queueNodeTree = new QuadTree<Waypoint>(2, 1, _queueAreaXMin, _queueAreaXMax, _queueAreaYMin, _queueAreaYMax);
                foreach (var wp in Queue)
                    _queueNodeTree.Add(wp);
                // Obtain maximal edge length
                _maxEdgeLength = Queue.ToDictionary(wp => wp, wp => wp.Paths.Max(otherWP => wp.GetDistance(otherWP)));
            }

            /// <summary>
            /// Returns the waypoint nearest to the given coordinate.
            /// </summary>
            /// <param name="x">The x-value of the coordinate.</param>
            /// <param name="y">The y-value of the coordinate.</param>
            /// <returns>The nearest waypoint of the queue.</returns>
            private Waypoint GetNearestQueueWaypoint(double x, double y)
            {
                double nearestDistance;
                return _queueNodeTree.GetNearestObject(x, y, out nearestDistance);
            }

            /// <summary>
            /// Updates this instance.
            /// </summary>
            public void Update()
            {
                //no need for managing
                if (_managedBots.Count == 0)
                    return;

                //get locked way points
                LockedWaypoints.Clear();
                _botsInQueue.Clear();

                //is elevator and elevator is in use => lock the destination waypoint
                if (QueueWaypoint.Elevator != null && QueueWaypoint.Elevator.InUse)
                    LockedWaypoints.Add(QueueWaypoint, null);

                //check bot states
                for (int i = 0; i < _managedBots.Count; i++)
                {
                    BotNormal bot = _managedBots[i];

                    var nextWaypoint = bot.NextWaypoint != null ? bot.NextWaypoint : bot.CurrentWaypoint;

                    var currentWaypointInQueue = Queue.Contains(bot.CurrentWaypoint);
                    var nextWaypointInQueue = Queue.Contains(nextWaypoint);

                    var locksCurrentWaypoint = currentWaypointInQueue && bot.CurrentWaypoint.GetDistance(bot) <= _maxEdgeLength[bot.CurrentWaypoint];

                    // Check whether bot is leaving the queue (only possible at the queue waypoint!)
                    if (// Check whether the bot has a new destination and its current waypoint is the end of the queue
                        (bot.DestinationWaypoint != QueueWaypoint && bot.CurrentWaypoint == QueueWaypoint && !locksCurrentWaypoint) ||
                        // Check whether the bot is already outside the queue area and has a different destination than the queue waypoint
                        (!currentWaypointInQueue && !nextWaypointInQueue && bot.DestinationWaypoint != QueueWaypoint))
                    {
                        //bot leaves elevator?
                        if (QueueWaypoint.Elevator != null && bot == QueueWaypoint.Elevator.usedBy)
                        {
                            QueueWaypoint.Elevator.InUse = false;
                            QueueWaypoint.Elevator.usedBy = null;
                        }
                        // Not in queue anymore - remove it
                        _managedBots.RemoveAt(i);
                        // Update index (we removed one)
                        i--;
                        // Mark queueing inactive (this is redundant - see below)
                        bot.IsQueueing = false;
                        // Proceed to next bot
                        continue;
                    }

                    //bot in Queue
                    if (currentWaypointInQueue)
                    {
                        _botsInQueue.Add(bot);

                        // Indicate queueing
                        bot.IsQueueing = true;

                        if (bot.CurrentWaypoint != QueueWaypoint)
                            bot.RequestReoptimization = false; //this bot will be managed by this manager

                        //add locks
                        if (locksCurrentWaypoint && !LockedWaypoints.ContainsKey(bot.CurrentWaypoint))
                            LockedWaypoints.Add(bot.CurrentWaypoint, bot);
                        if (nextWaypointInQueue && !LockedWaypoints.ContainsKey(nextWaypoint))
                            LockedWaypoints.Add(nextWaypoint, bot);
                    }

                    //bot reached end of the queue - no active queueing anymore
                    if (_queueWaypointIndices.ContainsKey(bot.CurrentWaypoint) && _queueWaypointIndices[bot.CurrentWaypoint] == 0)
                    {
                        // Mark queueing inactive
                        bot.IsQueueing = false;
                        _botsInQueue.Remove(bot);
                    }
                }

                //if this is an elevator queue, the first way point is locked, when the elevator is in use
                if (QueueWaypoint.Elevator != null && QueueWaypoint.Elevator.InUse && !LockedWaypoints.ContainsKey(QueueWaypoint))
                    LockedWaypoints[QueueWaypoint] = null;

                // Remove bots that finished their cruise
                foreach (var bot in _queueCruisePaths.Where(kvp => kvp.Key.CurrentWaypoint == kvp.Value.Last()).Select(kvp => kvp.Key).ToArray())
                    _queueCruisePaths.Remove(bot);

                // Reset places
                _placeInQueue.Clear();

                // Manage locks for cruising bots
                HashSet<BotNormal> failedCruiseBots = null;
                foreach (var bot in _queueCruisePaths.Keys)
                {
                    // Assert that the bot is in the queue
                    Debug.Assert(_queueAreaXMin <= bot.X && bot.X <= _queueAreaXMax && _queueAreaYMin <= bot.Y && bot.Y <= _queueAreaYMax);
                    // Fetch the waypoint the bot is currently at
                    var realWP = GetNearestQueueWaypoint(bot.X, bot.Y);
                    int currentQueueIndex = _queueWaypointIndices[realWP];
                    // Lock waypoints that are left for the cruise
                    foreach (var cruiseWP in _queueCruisePaths[bot].Where(wp => _queueWaypointIndices[wp] <= currentQueueIndex))
                    {
                        // If bot is not moving and next waypoint is already blocked by another bot, something went wrong - discard the cruise and lineup regularly
                        if (LockedWaypoints.ContainsKey(cruiseWP) && LockedWaypoints[cruiseWP] != bot)
                        {
                            // Cruise failed - mark for removal
                            if (failedCruiseBots == null)
                                failedCruiseBots = new HashSet<BotNormal>() { bot };
                            else
                                failedCruiseBots.Add(bot);
                        }
                        else
                        {
                            // Lock waypoint for cruise
                            LockedWaypoints[cruiseWP] = bot;
                        }
                    }
                }
                // Cancel failed cruises
                if (failedCruiseBots != null)
                    foreach (var failedBot in failedCruiseBots)
                        _queueCruisePaths.Remove(failedBot);

                // Assign already moving bots
                foreach (var bot in _botsInQueue.Where(bot => bot.CurrentWaypoint != bot.NextWaypoint && bot.NextWaypoint != null))
                    _placeInQueue.Add(bot, bot.NextWaypoint);

                //assign standing bots
                foreach (var bot in _botsInQueue.Where(bot => !_placeInQueue.ContainsKey(bot)))
                {
                    var queueIndex = _queueWaypointIndices[bot.CurrentWaypoint];
                    var newQueueIndex = -1;
                    if (queueIndex == 0 || LockedWaypoints.ContainsKey(Queue[queueIndex - 1]))
                    {
                        //locked => stay where you are
                        _placeInQueue.Add(bot, bot.CurrentWaypoint);
                        newQueueIndex = queueIndex;
                    }
                    else
                    {
                        //if almost there, just move one up
                        Path path = new Path();
                        if (queueIndex == 1)
                        {
                            LockedWaypoints.Add(Queue[queueIndex - 1], bot);
                            path.AddFirst(_pathManager.GetNodeIdByWaypoint(Queue[queueIndex - 1]), true, 0.0);
                            newQueueIndex = queueIndex - 1;
                        }
                        else
                        {
                            //go as far as you can
                            IEnumerable<Waypoint> lockedPredecessorWaypoints = LockedWaypoints.Keys.Where(q => _queueWaypointIndices[q] < queueIndex);
                            int targetQueueIndex = lockedPredecessorWaypoints.Any() ? lockedPredecessorWaypoints.Max(w => _queueWaypointIndices[w]) + 1 : QueueWaypoint.Elevator != null ? 1 : 0;
                            List<Waypoint> cruisePath = new List<Waypoint>();
                            int nextIndex = -1;
                            // Build cruise path through queue
                            for (int currentIndex = queueIndex; currentIndex >= targetQueueIndex;)
                            {
                                // Determine next waypoint in queue to go to after this one (consider shortcuts)
                                IEnumerable<Waypoint> shortCuts = Queue[currentIndex].Paths.Where(p => _queueWaypointIndices.ContainsKey(p) && _queueWaypointIndices[p] < currentIndex && targetQueueIndex <= _queueWaypointIndices[p]);
                                nextIndex = currentIndex > targetQueueIndex && shortCuts.Any() ? shortCuts.Min(p => _queueWaypointIndices[p]) : currentIndex - 1;
                                // Check whether a stop is required at the 
                                double inboundOrientation = cruisePath.Count >= 1 && currentIndex > targetQueueIndex ? Circle.GetOrientation(cruisePath[cruisePath.Count - 1].X, cruisePath[cruisePath.Count - 1].Y, Queue[currentIndex].X, Queue[currentIndex].Y) : double.NaN;
                                double outboundOrientation = cruisePath.Count >= 1 && currentIndex > targetQueueIndex ? Circle.GetOrientation(Queue[currentIndex].X, Queue[currentIndex].Y, Queue[nextIndex].X, Queue[nextIndex].Y) : double.NaN;
                                bool stopRequired = !double.IsNaN(inboundOrientation) && Math.Abs(Circle.GetOrientationDifference(inboundOrientation, outboundOrientation)) >= bot.Instance.StraightOrientationTolerance;
                                // Add connection to the overall path
                                LockedWaypoints[Queue[currentIndex]] = bot;
                                bool stopAtNode = currentIndex == queueIndex || currentIndex == targetQueueIndex || stopRequired;
                                path.AddLast(
                                    // The next waypoint to go to
                                    _pathManager.GetNodeIdByWaypoint(Queue[currentIndex]),
                                    // See whether we need to stop at the waypoint (either because it is the last one or the angles do not match - using 10 degrees in radians here, which should be in line with the Graph class of path planning)
                                    stopAtNode,
                                    // Don't wait at all
                                    0.0);
                                // Add the step to the cruise path
                                cruisePath.Add(Queue[currentIndex]);
                                // Update to next index
                                currentIndex = nextIndex;
                            }
                            // Prepare for next node in queue
                            path.NextNodeToPrepareFor = queueIndex != nextIndex && nextIndex >= 0 ? _pathManager.GetNodeIdByWaypoint(Queue[nextIndex]) : -1;
                            // The new index in queue is the targeted one
                            newQueueIndex = targetQueueIndex;
                            // Save path for overwatch
                            _queueCruisePaths[bot] = cruisePath;
                            // Check path
                            for (int i = 0; i < cruisePath.Count - 1; i++)
                            {
                                if (!cruisePath[i].ContainsPath(cruisePath[i + 1]))
                                    throw new InvalidOperationException();
                            }
                        }
                        // Set path
                        bot.Path = path;

                        _placeInQueue.Add(bot, Queue[newQueueIndex]);

                        //if the next place is an elevator set it
                        if (_placeInQueue[bot].Elevator != null)
                        {
                            QueueWaypoint.Elevator.InUse = true;
                            QueueWaypoint.Elevator.usedBy = bot;
                        }
                    }

                }

                //assign bots that are not in the queue

                //search for the first free node in the queue
                int firstFreeNode = Queue.Count - 1;
                while (firstFreeNode > 0 && !LockedWaypoints.ContainsKey(Queue[firstFreeNode - 1]))
                    firstFreeNode--;

                //if this is a queue for an elevator, then do not assign the elevator directly, because others might wait in a queue on a different tier
                if (firstFreeNode == 0 && QueueWaypoint.Elevator != null)
                    firstFreeNode = 1;

                //botsLeftToQueue
                var botsLeftToQueue = _managedBots.Where(bot => !_placeInQueue.ContainsKey(bot)).ToList();

                //while a bot exists with no place in queue
                while (botsLeftToQueue.Count > 0)
                {
                    var nearestBot = botsLeftToQueue[0];
                    var distance = Queue[firstFreeNode].GetDistance(nearestBot);
                    for (int i = 1; i < botsLeftToQueue.Count; i++)
                    {
                        if (Queue[firstFreeNode].GetDistance(botsLeftToQueue[i]) < distance)
                        {
                            nearestBot = botsLeftToQueue[i];
                            distance = Queue[firstFreeNode].GetDistance(nearestBot);
                        }
                    }

                    botsLeftToQueue.Remove(nearestBot);
                    _placeInQueue.Add(nearestBot, Queue[firstFreeNode]);

                    firstFreeNode = Math.Min(firstFreeNode + 1, Queue.Count - 1);

                }

                //Last Node should never be blocked
                if (LockedWaypoints.ContainsKey(Queue.Last()))
                    LockedWaypoints.Remove(Queue.Last());
            }

            /// <summary>
            /// Gets the place in queue.
            /// </summary>
            /// <param name="bot">The bot.</param>
            /// <returns></returns>
            public Waypoint getPlaceInQueue(BotNormal bot)
            {
                if (_botsInQueue.Contains(bot))
                    return null;
                else
                    return _placeInQueue[bot];
            }

            /// <summary>
            /// Ons the bot join queue.
            /// </summary>
            /// <param name="bot">The bot.</param>
            internal void onBotJoinQueue(BotNormal bot)
            {
                if (!_managedBots.Contains(bot))
                    _managedBots.Add(bot);
            }
        }

        #endregion
    }
}
