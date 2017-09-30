using RAWSimO.Core.Configurations;
using RAWSimO.Core.Control.Shared;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Geometrics;
using RAWSimO.Core.Info;
using RAWSimO.Core.IO;
using RAWSimO.Core.Metrics;
using RAWSimO.Core.Statistics;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using static RAWSimO.Core.Control.PathManager;

namespace RAWSimO.Core.Waypoints
{
    /// <summary>
    /// Implements one waypoint of the waypoint graph.
    /// </summary>
    public class Waypoint : Circle, IWaypointInfo, IExposeVolatileID
    {
        /// <summary>
        /// Creates a new waypoint.
        /// </summary>
        /// <param name="instance">The instance this waypoint belongs to.</param>
        internal Waypoint(Instance instance) : base(instance) { }

        /// <summary>
        /// Contains all possible paths to take from this waypoint. The corresponding weights are given by the distance to the other waypoint.
        /// </summary>
        private Dictionary<Waypoint, double> _paths = new Dictionary<Waypoint, double>();

        /// <summary>
        /// Returns all connected waypoints as an enumeration.
        /// </summary>
        public IEnumerable<Waypoint> Paths { get { return _paths.Keys; } }

        /// <summary>
        /// Returns the distance to the specified waypoint.
        /// </summary>
        /// <param name="other">The other waypoint to which the distance is desired.</param>
        /// <returns>The distance to the other waypoint. Distance might be positive infinity if there is no direct connection or the connection is dynamically blocked.</returns>
        public double this[Waypoint other] { get { return GetPathDistance(other); } }

        /// <summary>
        /// Contains all guards used to block certain paths dynamically.
        /// </summary>
        private Dictionary<Waypoint, List<QueueGuard>> _guards = new Dictionary<Waypoint, List<QueueGuard>>();

        /// <summary>
        /// The bots registered at this waypoint (approaching).
        /// </summary>
        private HashSet<Bot> _botsApproaching = new HashSet<Bot>();

        /// <summary>
        /// The bots registered at this waypoint (leaving).
        /// </summary>
        private HashSet<Bot> _botsLeaving = new HashSet<Bot>();

        /// <summary>
        /// Contains the output-station located at this waypoint, if there is one.
        /// </summary>
        public OutputStation OutputStation { get; internal set; }

        /// <summary>
        /// Contains the input-station located at this waypoint, if there is one.
        /// </summary>
        public InputStation InputStation { get; internal set; }

        /// <summary>
        /// Contains the elevator connected to this waypoint, if there is one.
        /// </summary>
        public Elevator Elevator { get; internal set; }

        /// <summary>
        /// Contains the pod located at this waypoint, if there is one.
        /// </summary>
        public Pod Pod { get; internal set; }

        /// <summary>
        /// Indicates whether this waypoint can be used to store pods.
        /// </summary>
        public bool PodStorageLocation { get; internal set; }

        /// <summary>
        /// Indicates whether this waypoint belongs to a queue.
        /// </summary>
        public bool IsQueueWaypoint { get; internal set; }

        /// <summary>
        /// This fields contains the queue manager this waypoint belongs to, if it belongs to a queue.
        /// </summary>
        internal QueueManager QueueManager { get; set; }

        /// <summary>
        /// Adds a connection to the specified waypoint. The weight is calculated by using the distance.
        /// </summary>
        /// <param name="other">The waypoint to connect this one with.</param>
        public void AddPath(Waypoint other) { _paths[other] = GetDistance(other); }

        /// <summary>
        /// Removes the connection to the specified waypoint.
        /// </summary>
        /// <param name="other">The waypoint to disconnect</param>
        public void RemovePath(Waypoint other) { _paths.Remove(other); }

        /// <summary>
        /// Adds a connection to the other waypoint that is bidirectional.
        /// </summary>
        /// <param name="other">The waypoint to connect to.</param>
        public void AddBidirectionalPath(Waypoint other) { _paths[other] = GetDistance(other); other.AddPath(this); }

        /// <summary>
        /// Simply indicates whether the specified waypoint is associated with this one.
        /// </summary>
        /// <param name="other">The other waypoint.</param>
        /// <returns><code>true</code> if it is connected to this one, <code>false</code> otherwise</returns>
        public bool ContainsPath(Waypoint other) { return _paths.ContainsKey(other); }

        /// <summary>
        /// Returns the distance to the specified waypoint.
        /// </summary>
        /// <param name="other">The other waypoint to calculate the distance to.</param>
        /// <returns>The distance between this and the other waypoint.</returns>
        public double GetPathDistance(Waypoint other)
        {
            return
                _paths.ContainsKey(other) &&
                    (!Instance.SettingConfig.QueueHandlingEnabled || !_guards.ContainsKey(other) || _guards[other].All(g => g.IsAccessible)) ?
                _paths[other] :
                double.PositiveInfinity;
        }

        /// <summary>
        /// Registers the given bot at this waypoint.
        /// </summary>
        /// <param name="bot">The bot to register.</param>
        public void AddBotApproaching(Bot bot) { _botsApproaching.Add(bot); }

        /// <summary>
        /// De-registers the given bot from this waypoint.
        /// </summary>
        /// <param name="bot">The bot to remove.</param>
        public void RemoveBotApproaching(Bot bot) { _botsApproaching.Remove(bot); }

        /// <summary>
        /// Registers the given bot at this waypoint. (approaching bot)
        /// </summary>
        /// <param name="bot">The bot to register.</param>
        public void AddBotLeaving(Bot bot) { _botsLeaving.Add(bot); }

        /// <summary>
        /// De-registers the given bot from this waypoint.
        /// </summary>
        /// <param name="bot">The bot to remove.</param>
        public void RemoveBotLeaving(Bot bot) { _botsLeaving.Remove(bot); }

        /// <summary>
        /// De-registers the given bot as an approaching bot and registers it as a leaving bot.
        /// </summary>
        /// <param name="bot">The bot to register / de-register.</param>
        public void TraverseBot(Bot bot) { _botsApproaching.Remove(bot); _botsLeaving.Add(bot); }

        /// <summary>
        /// Supplies the number of bots currently registered at this waypoint.
        /// </summary>
        public int BotCountOverall { get { return _botsApproaching.Count + _botsLeaving.Count; } }

        /// <summary>
        /// Indicates whether any bots are registered at this waypoint.
        /// </summary>
        public bool AnyBotsOverall { get { return _botsApproaching.Any() || _botsLeaving.Any(); } }

        /// <summary>
        /// Supplies the number of bots currently approaching the waypoint.
        /// </summary>
        public int BotCountApproaching { get { return _botsApproaching.Count; } }

        /// <summary>
        /// Indicates whether any bots are approaching the waypoint.
        /// </summary>
        public bool AnyBotsApproaching { get { return _botsApproaching.Any(); } }

        /// <summary>
        /// Supplies the number of bots currently leaving the waypoint.
        /// </summary>
        public int BotCountLeaving { get { return _botsLeaving.Count; } }

        /// <summary>
        /// Indicates whether any bots are leaving the waypoint.
        /// </summary>
        public bool AnyBotsLeaving { get { return _botsLeaving.Any(); } }

        /// <summary>
        /// Registers a guard for a connection to another waypoint.
        /// </summary>
        /// <param name="guard">The guard monitoring the connection.</param>
        internal void RegisterGuard(QueueGuard guard)
        {
            if (!_guards.ContainsKey(guard.To))
                _guards[guard.To] = new List<QueueGuard>();
            _guards[guard.To].Add(guard);
        }
        /// <summary>
        /// Unregisters the guard from this waypoint.
        /// </summary>
        /// <param name="guard">The guard to unregister.</param>
        internal void UnregisterGuard(QueueGuard guard)
        {
            if (_guards.ContainsKey(guard.To))
            {
                _guards[guard.To].Remove(guard);
                if (_guards[guard.To].Count == 0)
                    _guards.Remove(guard.To);
            }
        }

        /// <summary>
        /// Indicates whether the given waypoint is directly accessible from this one.
        /// </summary>
        /// <param name="other">The other waypoint.</param>
        /// <returns><code>true</code> if they are connected and not dynamically blocked, <code>false</code> otherwise.</returns>
        public bool IsAccessible(Waypoint other)
        {
            return
                _paths.ContainsKey(other) &&
                    (!Instance.SettingConfig.QueueHandlingEnabled || !_guards.ContainsKey(other) || _guards[other].All(g => g.IsAccessible));
        }

        /// <summary>
        /// Signals the transition of the given bot from this waypoint to the specified one.
        /// </summary>
        /// <param name="waypoint">The waypoint the bot is moving to.</param>
        /// <param name="bot">The bot that is moving from this waypoint to the given one.</param>
        /// <param name="elevator">Elevator that was entered.</param>
        /// <param name="semaphores">An enumeration of all semaphores passed.</param>
        public void Pass(Waypoint waypoint, Bot bot, out IEnumerable<QueueSemaphore> semaphores, out Elevator elevator)
        {
            // Check whether there are guard registered for this connection
            if (waypoint != null && _guards.ContainsKey(waypoint))
            {
                // Pass the guards and return the semaphores
                _guards[waypoint].ForEach(g => g.Pass(bot));
                semaphores = _guards[waypoint].Select(g => g.Semaphore);
            }
            // No semaphore passed
            else { semaphores = null; }
            // Check whether there is an elevator we are entering
            if (waypoint != null && Elevator != null)
            {
                // Register with the elevator
                Elevator.InUse = true;
                Elevator.usedBy = bot;
                elevator = Elevator;
            }
            // No elevator entered
            else { elevator = null; }
        }

        #region Waypoint meta-info

        /// <summary>
        /// The distance of the shortest path that is always safe for carrying a pod along it to the next output-station.
        /// </summary>
        private double _shortestPodPathDistanceToNextOutputStation = double.NaN;
        /// <summary>
        /// The distance of the shortest path that is always safe for carrying a pod along it to the next output-station.
        /// </summary>
        public double ShortestPodPathDistanceToNextOutputStation
        {
            get
            {
                if (double.IsNaN(_shortestPodPathDistanceToNextOutputStation))
                    _shortestPodPathDistanceToNextOutputStation = Instance.OutputStations.Min(s => Distances.CalculateShortestPathPodSafe(this, s.Waypoint, Instance));
                return _shortestPodPathDistanceToNextOutputStation;
            }
        }
        /// <summary>
        /// The distance of the shortest path that is always safe for carrying a pod along it to the next input-station.
        /// </summary>
        private double _shortestPodPathDistanceToNextInputStation = double.NaN;
        /// <summary>
        /// The distance of the shortest path that is always safe for carrying a pod along it to the next input-station.
        /// </summary>
        public double ShortestPodPathDistanceToNextInputStation
        {
            get
            {
                if (double.IsNaN(_shortestPodPathDistanceToNextInputStation))
                    _shortestPodPathDistanceToNextInputStation = Instance.InputStations.Min(s => Distances.CalculateShortestPathPodSafe(this, s.Waypoint, Instance));
                return _shortestPodPathDistanceToNextInputStation;
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// The statistics about all connections ending at this waypoint.
        /// </summary>
        private Dictionary<Waypoint, ConnectionStatisticsDataPoint> _statBackwardStar;
        /// <summary>
        /// The statistics about all connections starting at this waypoint.
        /// </summary>
        private Dictionary<Waypoint, ConnectionStatisticsDataPoint> _statForwardStar;
        /// <summary>
        /// Inititalizes the datapoint for a trip.
        /// </summary>
        /// <param name="from">The from part of the connection.</param>
        /// <param name="to">The to part of the connection.</param>
        /// <param name="travelTime">The first travel time.</param>
        /// <returns>The datapoint.</returns>
        private ConnectionStatisticsDataPoint StatInitTripDataPoint(Waypoint from, Waypoint to, double travelTime)
        {
            return new ConnectionStatisticsDataPoint()
            {
                Count = 1,
                FromID = from.ID,
                FromX = from.X,
                FromY = from.Y,
                FromIsStorage = from.PodStorageLocation,
                FromIsIStation = from.InputStation != null,
                FromIsOStation = from.OutputStation != null,
                FromIsElevator = from.Elevator != null,
                ToID = to.ID,
                ToX = to.X,
                ToY = to.Y,
                ToIsStorage = to.PodStorageLocation,
                ToIsIStation = to.InputStation != null,
                ToIsOStation = to.OutputStation != null,
                ToIsElevator = to.Elevator != null,
                TravelTimeAvg = travelTime,
                TravelTimeMax = travelTime,
                TravelTimeMin = travelTime,
                TravelTimeVar = 0,
                TravelTimeSum = travelTime,
            };
        }
        /// <summary>
        /// Has to be called everytime a robot reaches the destination waypoint of a trip.
        /// </summary>
        /// <param name="bot">The robot that just completed the trip.</param>
        internal void StatReachedDestination(Bot bot)
        {
            // If we have two waypoints connected by a trip, log it
            if (bot.StatLastWaypoint != this && bot.StatLastWaypoint != null)
                bot.StatLastWaypoint.StatPropagateDestination(this, bot);
            // Remember new origin waypoint
            bot.StatLastWaypoint = this;
        }
        /// <summary>
        /// Has to be called by the robot, if the simulation is requesting a writeout of the trip data.
        /// </summary>
        /// <param name="bot">The bot that is logging its intermediate trip.</param>
        internal void StatLogUnfinishedTrip(Bot bot)
        {
            // Act as if the trip was finished // TODO this may lead to inaccuracy, but is still more accurate than ignoring potential deadlock trips
            if (bot.StatLastWaypoint != this && bot.StatLastWaypoint != null)
                bot.StatLastWaypoint.StatPropagateDestination(this, bot);
            // We have no new origin, but we need to reset the time for the bot
            bot.StatLastTripStartTime = Instance.Controller.CurrentTime;
        }
        /// <summary>
        /// Logs the trip between this waypoint (the 'origin') and the other waypoint (the 'destination').
        /// </summary>
        /// <param name="destinationWP">The destination waypoint of the trip to log.</param>
        /// <param name="bot">The bot that did the trip.</param>
        protected void StatPropagateDestination(Waypoint destinationWP, Bot bot)
        {
            // Init logging if not already done
            if (_statForwardStar == null)
                _statForwardStar = new Dictionary<Waypoint, ConnectionStatisticsDataPoint>();
            if (destinationWP._statBackwardStar == null)
                destinationWP._statBackwardStar = new Dictionary<Waypoint, ConnectionStatisticsDataPoint>();
            // --> Log forward stars of this origin waypoint
            if (!_statForwardStar.ContainsKey(destinationWP))
                _statForwardStar[destinationWP] = StatInitTripDataPoint(this, destinationWP, Instance.Controller.CurrentTime - bot.StatLastTripStartTime);
            else
                StatisticsHelper.UpdateAvgVarData(
                    ref _statForwardStar[destinationWP].Count,
                    ref _statForwardStar[destinationWP].TravelTimeAvg,
                    ref _statForwardStar[destinationWP].TravelTimeVar,
                    ref _statForwardStar[destinationWP].TravelTimeMin,
                    ref _statForwardStar[destinationWP].TravelTimeMax,
                    ref _statForwardStar[destinationWP].TravelTimeSum,
                    Instance.Controller.CurrentTime - bot.StatLastTripStartTime);
            // --> Log backward stars of destination waypoint
            if (!destinationWP._statBackwardStar.ContainsKey(this))
                destinationWP._statBackwardStar[this] = StatInitTripDataPoint(destinationWP, this, Instance.Controller.CurrentTime - bot.StatLastTripStartTime);
            else
                StatisticsHelper.UpdateAvgVarData(
                    ref destinationWP._statBackwardStar[this].Count,
                    ref destinationWP._statBackwardStar[this].TravelTimeAvg,
                    ref destinationWP._statBackwardStar[this].TravelTimeVar,
                    ref destinationWP._statBackwardStar[this].TravelTimeMin,
                    ref destinationWP._statBackwardStar[this].TravelTimeMax,
                    ref destinationWP._statBackwardStar[this].TravelTimeSum,
                    Instance.Controller.CurrentTime - bot.StatLastTripStartTime);
        }
        /// <summary>
        /// Indicates whether there is data for trips incoming from the specified waypoint.
        /// </summary>
        /// <param name="wp">The other waypoint.</param>
        /// <returns><code>true</code> if data is present, <code>false</code> otherwise.</returns>
        public bool StatContainsTripDataIn(Waypoint wp) { return _statBackwardStar != null && _statBackwardStar.ContainsKey(wp); }
        /// <summary>
        /// Indicates whether there is data for trips outgoing to the specified waypoint.
        /// </summary>
        /// <param name="wp">The other waypoint.</param>
        /// <returns><code>true</code> if data is present, <code>false</code> otherwise.</returns>
        public bool StatContainsTripDataOut(Waypoint wp) { return _statForwardStar != null && _statForwardStar.ContainsKey(wp); }
        /// <summary>
        /// Returns the trip data for the incoming connection from the given waypoint.
        /// </summary>
        /// <param name="wp">The other waypoint.</param>
        /// <returns>The trip data.</returns>
        public ConnectionStatisticsDataPoint StatGetTripDataIn(Waypoint wp) { return _statBackwardStar[wp]; }
        /// <summary>
        /// Returns the trip data for the outgoing connection to the given waypoint.
        /// </summary>
        /// <param name="wp">The other waypoint.</param>
        /// <returns>The trip data.</returns>
        public ConnectionStatisticsDataPoint StatGetTripDataOut(Waypoint wp) { return _statForwardStar[wp]; }
        /// <summary>
        /// The overall count of observed trips outgoing from this waypoint.
        /// </summary>
        public int StatOutgoingTrips { get { return _statForwardStar != null ? _statForwardStar.Sum(c => c.Value.Count) : 0; } }
        /// <summary>
        /// The overall time spent for trips outgoing from this waypoint.
        /// </summary>
        public double StatOutgoingTripTime { get { return _statForwardStar != null ? _statForwardStar.Sum(c => c.Value.TravelTimeSum) : 0; } }
        /// <summary>
        /// Resets all statistics for this element.
        /// </summary>
        public void ResetStatistics()
        {
            if (_statBackwardStar != null)
                _statBackwardStar.Clear();
            if (_statForwardStar != null)
                _statForwardStar.Clear();
        }

        #endregion

        #region Inherited members

        /// <summary>
        /// Returns a simple string identifying this object in its instance.
        /// </summary>
        /// <returns>A simple name identifying the instance element.</returns>
        public override string GetIdentfierString() { return "Waypoint" + ID; }
        /// <summary>
        /// Returns a simple string giving information about the object.
        /// </summary>
        /// <returns>A simple string.</returns>
        public override string ToString() { return "Waypoint" + ID + "-(" + X.ToString(IOConstants.FORMATTER) + "," + Y.ToString(IOConstants.FORMATTER) + ")"; }

        #endregion

        #region IWaypointInfo Members

        /// <summary>
        /// Indicates the zone type of this waypoint.
        /// </summary>
        internal ZoneType InfoTagCache { get; set; } = ZoneType.None;
        /// <summary>
        /// If this waypoint is a storage location and the pod utility component is active, then this field contains a value reflecting the rank-ID of the storage location.
        /// </summary>
        internal double InfoTagProminence { get; set; } = 0;

        /// <summary>
        /// Indicates whether the waypoint is a storage location.
        /// </summary>
        /// <returns><code>true</code> if it is a storage location, <code>false</code> otherwise.</returns>
        public bool GetInfoStorageLocation() { return PodStorageLocation; }
        /// <summary>
        /// Gets all outgoing connections of the waypoint.
        /// </summary>
        /// <returns>An enumeration of waypoints this waypoint has a directed edge to.</returns>
        public IEnumerable<IWaypointInfo> GetInfoConnectedWaypoints() { return _paths.Keys; }

        #endregion

        #region IImmovableObjectInfo Members

        /// <summary>
        /// The diameter of a waypoint (only relevant for visualization).
        /// </summary>
        public const double WAYPOINT_DIAMETER = 0.1;
        /// <summary>
        /// The length of the objects' area. (Corresponds to the x-axis)
        /// </summary>
        /// <returns>The length of the objects' area.</returns>
        public new double GetInfoLength() { return WAYPOINT_DIAMETER; }
        /// <summary>
        /// The width of the objects' area. (Corresponds to the y-axis)
        /// </summary>
        /// <returns>The width of the objects' area.</returns>
        public new double GetInfoWidth() { return WAYPOINT_DIAMETER; }
        /// <summary>
        /// Gets the current tier this object is placed on. Can't change in case of an immovable object.
        /// </summary>
        /// <returns>The current tier.</returns>
        public ITierInfo GetInfoCurrentTier() { return Tier; }
        /// <summary>
        /// Returns the active instance belonging to this element.
        /// </summary>
        /// <returns>The active instance.</returns>
        public IInstanceInfo GetInfoInstance() { return Instance; }

        #endregion

        #region IExposeVolatileID Members

        /// <summary>
        /// An ID that is useful as an index for listing this item.
        /// This ID is unique among all <code>ItemDescription</code>s while being as low as possible.
        /// Note: For now the volatile ID matches the actual ID.
        /// </summary>
        int IExposeVolatileID.VolatileID { get { return VolatileID; } }

        #endregion
    }
}
