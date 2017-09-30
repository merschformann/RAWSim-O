using RAWSimO.Core.Helper;
using RAWSimO.Core.Info;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Elements
{
    /// <summary>
    /// An elevator implementation that is able to transport robots from one tier to another.
    /// </summary>
    public class Elevator : InstanceElement, IQueuesOwner, IElevatorInfo
    {
        #region Constructors

        /// <summary>
        /// Creates a new elevator.
        /// </summary>
        /// <param name="instance">The instance this elevator belongs to.</param>
        internal Elevator(Instance instance) : base(instance) { }

        #endregion

        #region Core

        /// <summary>
        /// All waypoints connected by the elevator.
        /// </summary>
        private HashSet<Waypoint> _connectedPoints = new HashSet<Waypoint>();

        /// <summary>
        /// Exposes all waypoints connected by the elevator.
        /// </summary>
        public ICollection<Waypoint> ConnectedPoints { get { return _connectedPoints; } }

        /// <summary>
        /// Contains the time necessary to move the elevator from the source waypoint to the destination waypoint.
        /// </summary>
        private Dictionary<Waypoint, Dictionary<Waypoint, double>> _timings = new Dictionary<Waypoint, Dictionary<Waypoint, double>>();

        /// <summary>
        /// Indicates whether the elevator is currently in use.
        /// </summary>
        public bool InUse { get; set; }

        /// <summary>
        /// Indicates which bot is currently using the elevator.
        /// </summary>
        public Bot usedBy { get; set; }
        /// <summary>
        /// Registers the given waypoints with the elevator.
        /// </summary>
        /// <param name="time">The default time to use for transportation.</param>
        /// <param name="connectedPoints">The points connected by the elevator.</param>
        public void RegisterPoints(double time, IEnumerable<Waypoint> connectedPoints)
        {
            // Store connected waypoints
            _connectedPoints = new HashSet<Waypoint>(connectedPoints);
            // Set default time to move a bot between two of the given points
            foreach (var from in connectedPoints)
                foreach (var to in connectedPoints)
                    // Do not connect a WP to itself
                    if (from != to)
                    {
                        // If it's the first time the source node is used init the dictionary
                        if (!_timings.ContainsKey(from))
                            _timings[from] = new Dictionary<Waypoint, double>();
                        // Set the timing value
                        _timings[from][to] = time;
                    }
        }
        /// <summary>
        /// Unregisters the given waypoint with this elevator.
        /// </summary>
        /// <param name="waypoint">The waypoint to remove from the elevator.</param>
        public void UnregisterPoint(Waypoint waypoint)
        {
            // Remove the connection
            _connectedPoints.Remove(waypoint);
            _timings.Remove(waypoint);
            foreach (var otherWP in _connectedPoints)
                _timings[otherWP].Remove(waypoint);
            Queues.Remove(waypoint);
        }
        /// <summary>
        /// Sets the time for transporation between the two given waypoints.
        /// </summary>
        /// <param name="from">The from waypoint.</param>
        /// <param name="to">The to waypoint.</param>
        /// <param name="time">The transportation time.</param>
        public void SetTiming(Waypoint from, Waypoint to, double time)
        {
            // Check waypoints validness
            if (from == to) throw new ArgumentException("Cannot connect a WP to itself!");
            if (!_connectedPoints.Contains(from) || !_connectedPoints.Contains(to)) throw new ArgumentException("Unknown waypoints!");
            // Set custom timing for the connection
            _timings[from][to] = time;
        }
        /// <summary>
        /// Gets the transporation time for the connection.
        /// </summary>
        /// <param name="from">The from part of the connection.</param>
        /// <param name="to">The to part of the connection.</param>
        /// <returns>The transportation time.</returns>
        public double GetTiming(Waypoint from, Waypoint to) { return _timings[from][to]; }
        /// <summary>
        /// Transports the bot along the given connection.
        /// </summary>
        /// <param name="bot">The bot to transport.</param>
        /// <param name="from">The from part of the connection.</param>
        /// <param name="to">The to part of the connection.</param>
        /// <returns>The transportation time.</returns>
        public double Transport(Bot bot, Waypoint from, Waypoint to)
        {
            // Update the meta-info
            bot.Tier.RemoveBot(bot);
            to.Tier.AddBot(bot);
            if (bot.Pod != null)
            {
                bot.Pod.Tier.RemovePod(bot.Pod);
                to.Tier.AddPod(bot.Pod);
            }
            // Return the time it takes to transport the bot
            return _timings[from][to];
        }

        #endregion

        #region Inherited methods

        /// <summary>
        /// Returns a simple string identifying this object in its instance.
        /// </summary>
        /// <returns>A simple name identifying the instance element.</returns>
        public override string GetIdentfierString() { return "Elevator" + this.ID; }
        /// <summary>
        /// Returns a simple string giving information about the object.
        /// </summary>
        /// <returns>A simple string.</returns>
        public override string ToString() { return "Elevator" + this.ID; }

        #endregion

        #region IQueueOwner Members

        /// <summary>
        /// The Queue starting with the nearest way point ending with the most far away one.
        /// </summary>
        /// <value>
        /// The queue.
        /// </value>
        public Dictionary<Waypoint, List<Waypoint>> Queues { get; set; }

        #endregion

        #region IElevatorInfo Members

        /// <summary>
        /// Returns all waypoints of this elevator.
        /// </summary>
        /// <returns>The waypoints of this elevator.</returns>
        public IEnumerable<IWaypointInfo> GetInfoWaypoints() { return _connectedPoints; }
        /// <summary>
        /// Gets the x-position of the center of the object.
        /// </summary>
        /// <returns>The x-position.</returns>
        public double GetInfoCenterX() { return 0; }
        /// <summary>
        /// Gets the y-position of the center of the object.
        /// </summary>
        /// <returns>The y-position.</returns>
        public double GetInfoCenterY() { return 0; }
        /// <summary>
        /// Gets the x-position of the bottom left corner of the objects' area.
        /// </summary>
        /// <returns>The x-position.</returns>
        public double GetInfoTLX() { return 0; }
        /// <summary>
        /// Gets the y-position of the bottom left corner of the objects' area.
        /// </summary>
        /// <returns>The y-position.</returns>
        public double GetInfoTLY() { return 0; }
        /// <summary>
        /// Gets the current tier this object is placed on. Can't change in case of an immovable object.
        /// </summary>
        /// <returns>The current tier.</returns>
        public ITierInfo GetInfoCurrentTier() { return null; }
        /// <summary>
        /// Returns the active instance belonging to this element.
        /// </summary>
        /// <returns>The active instance.</returns>
        public IInstanceInfo GetInfoInstance() { return Instance; }

        #endregion
    }
}
