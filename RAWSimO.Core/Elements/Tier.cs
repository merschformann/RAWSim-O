using RAWSimO.Core.Geometrics;
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
    /// Describes one floor or level of an environment
    /// </summary>
    public class Tier : InstanceElement, IUpdateable, ITierInfo
    {
        #region Constructors

        /// <summary>
        /// Creates a new tier.
        /// </summary>
        /// <param name="instance">The instance this tier belongs to.</param>
        /// <param name="length">The length of the tier (corresponds to the x-axis).</param>
        /// <param name="width">The width of the tier (corresponds to the y-axis).</param>
        internal Tier(Instance instance, double length, double width) : base(instance)
        { Length = length; Width = width; BotQuadTree = new QuadTree<Bot>(Length, Width); PodQuadTree = new QuadTree<Pod>(Length, Width); }

        #endregion

        #region Core

        /// <summary>
        /// The length of the tier. (Corresponds to the x-axis)
        /// </summary>
        public double Length;

        /// <summary>
        /// The width of the tier. (Corresponds to the y-axis)
        /// </summary>
        public double Width;

        /// <summary>
        /// The relative position of the tier (just as information used for drawing).
        /// </summary>
        public double RelativePositionX;

        /// <summary>
        /// The relative position of the tier (just as information used for drawing).
        /// </summary>
        public double RelativePositionY;

        /// <summary>
        /// The relative position of the tier (just as information used for drawing).
        /// </summary>
        public double RelativePositionZ;

        /// <summary>
        /// The quad-tree observing positions and collisions of the bots on this tier.
        /// </summary>
        internal QuadTree<Bot> BotQuadTree;

        /// <summary>
        /// The quad-tree observing positions and collisions of the pods on this tier.
        /// </summary>
        internal QuadTree<Pod> PodQuadTree;

        /// <summary>
        /// All input-stations on this tier.
        /// </summary>
        internal List<InputStation> InputStations = new List<InputStation>();

        /// <summary>
        /// All output-stations on this tier.
        /// </summary>
        internal List<OutputStation> OutputStations = new List<OutputStation>();

        /// <summary>
        /// Set of all bots currently located on this tier.
        /// </summary>
        internal HashSet<Bot> Bots = new HashSet<Bot>();

        /// <summary>
        /// Set of all pods currently located on this tier.
        /// </summary>
        internal HashSet<Pod> Pods = new HashSet<Pod>();

        /// <summary>
        /// Set of waypoints on this tier.
        /// </summary>
        internal IEnumerable<Waypoint> Waypoints { get { return Instance.WaypointGraph.GetWayPoints()[this]; } }

        /// <summary>
        /// Exposes all bots currently located on this tier.
        /// </summary>
        public IEnumerable<Bot> CurrentBots { get { return Bots; } }

        /// <summary>
        /// Exposes all pods currently located on this tier.
        /// </summary>
        public IEnumerable<Pod> CurrentPods { get { return Pods; } }

        internal void AddBot(Bot bot)
        {
            Instance.Compound.BotCurrentTier[bot] = this;
            lock (this)
                Bots.Add(bot);
            BotQuadTree.Add(bot); BotQuadTree.UpdateTree();
            bot.Tier = this;
        }
        internal void RemoveBot(Bot bot)
        {
            Instance.Compound.BotCurrentTier.Remove(bot);
            lock (this)
                Bots.Remove(bot);
            BotQuadTree.Remove(bot); BotQuadTree.UpdateTree();
            bot.Tier = null;
        }

        internal void AddPod(Pod pod)
        {
            Instance.Compound.PodCurrentTier[pod] = this;
            lock (this)
                Pods.Add(pod);
            PodQuadTree.Add(pod); PodQuadTree.UpdateTree();
            pod.Tier = this;
        }
        internal void RemovePod(Pod pod)
        {
            Instance.Compound.PodCurrentTier.Remove(pod);
            lock (this)
                Pods.Remove(pod);
            PodQuadTree.Remove(pod); PodQuadTree.UpdateTree();
            pod.Tier = null;
        }

        internal void AddInputStation(InputStation iStation)
        {
            InputStations.Add(iStation);
            iStation.Tier = this;
        }
        internal void RemoveInputStation(InputStation iStation)
        {
            InputStations.Remove(iStation);
        }

        internal void AddOutputStation(OutputStation oStation)
        {
            OutputStations.Add(oStation);
            oStation.Tier = this;
        }
        internal void RemoveOutputStation(OutputStation oStation)
        {
            OutputStations.Remove(oStation);
        }

        internal void AddWaypoint(Waypoint waypoint)
        {
            waypoint.Tier = this;
        }
        internal void RemoveWaypoint(Waypoint waypoint)
        {
            Instance.WaypointGraph.Remove(waypoint);
        }
        /// <summary>
        /// Returns all bots within the given distance around the given spot.
        /// </summary>
        /// <param name="x">The x-value of the spot.</param>
        /// <param name="y">The y-value of the spot.</param>
        /// <param name="distance">The distance.</param>
        /// <returns>All bots within distance.</returns>
        public IEnumerable<Bot> GetBotsWithinDistance(double x, double y, double distance) { return BotQuadTree.GetObjectsWithinDistance(x, y, distance); }
        /// <summary>
        /// Returns all pods within the given distance around the given spot.
        /// </summary>
        /// <param name="x">The x-value of the spot.</param>
        /// <param name="y">The y-value of the spot.</param>
        /// <param name="distance">The distance.</param>
        /// <returns>All pods within distance.</returns>
        public IEnumerable<Pod> GetPodsWithinDistance(double x, double y, double distance) { return PodQuadTree.GetObjectsWithinDistance(x, y, distance); }
        /// <summary>
        /// Checks whether the bot can go to the given coordinates.
        /// </summary>
        /// <param name="bot">The bot.</param>
        /// <param name="x">The x-coordinate.</param>
        /// <param name="y">The y-coordinate.</param>
        /// <returns><code>true</code> if the bot can move to the given coordinates, <code>false</code> otherwise.</returns>
        public bool IsBotMoveValid(Bot bot, double x, double y)
        {
            // Check whether the move will carry the bot out of bounds
            if (x - bot.Radius < 0 || x + bot.Radius > Length || y - bot.Radius < 0 || y + bot.Radius > Width)
                return false;
            // Check whether the move does not collide with any other bot nor pod
            if (BotQuadTree.IsValidMove(bot, x, y))
            {
                if (bot.Pod == null)
                    return true;
                if (PodQuadTree.IsValidMove(bot.Pod, x, y))
                    return true;
            }
            // At this point the move is invalid due to previous conditions
            return false;
        }
        /// <summary>
        /// Moves the bot to the given coordinates.
        /// </summary>
        /// <param name="bot">The bot to move.</param>
        /// <param name="x">The x-coordinate.</param>
        /// <param name="y">The y-coordinate.</param>
        /// <returns><code>true</code> if the move was possible and was conducted, <code>false</code> otherwise.</returns>
        public bool MoveBot(Bot bot, double x, double y)
        {
            // Check whether the move will carry the bot out of bounds
            if (x - bot.Radius < 0 || x + bot.Radius > Length || y - bot.Radius < 0 || y + bot.Radius > Width)
                return false;
            // Check whether the move is possible according to the other bots
            if (BotQuadTree.IsValidMove(bot, x, y))
            {
                // If the bot is not carrying a pod, just move it
                if (bot.Pod == null)
                {
                    BotQuadTree.MoveTo(bot, x, y);
                    return true;
                }
                // In case it is carrying a pod, check the pod first
                if (PodQuadTree.IsValidMove(bot.Pod, x, y))
                {
                    PodQuadTree.MoveTo(bot.Pod, x, y);
                    BotQuadTree.MoveTo(bot, x, y);
                    return true;
                }
            }
            // Move not possible
            return false;
        }
        /// <summary>
        /// Moves the bot to the given coordinates. Checks for collisions, but will move the bot anyway.
        /// </summary>
        /// <param name="bot">The bot to move.</param>
        /// <param name="x">The x-coordinate.</param>
        /// <param name="y">The y-coordinate.</param>
        /// <returns><code>true</code> if there was no collision detected while moving, <code>false</code> otherwise.</returns>
        public bool MoveBotOverride(Bot bot, double x, double y)
        {
            // Check the move
            bool moveValid =
                // Check tier boundaries
                (x - bot.Radius >= 0 || x + bot.Radius <= Length || y - bot.Radius >= 0 || y + bot.Radius <= Width) &&
                // Check the move of the bot
                BotQuadTree.IsValidMove(bot, x, y) &&
                // Check the move of the pod
                (bot.Pod == null || PodQuadTree.IsValidMove(bot.Pod, x, y));
            // Move the bot
            BotQuadTree.MoveTo(bot, x, y);
            if (bot.Pod != null)
            {
                PodQuadTree.MoveTo(bot.Pod, x, y);
            }
            // Return check result
            return moveValid;
        }
        /// <summary>
        /// Returns the shortest time without a collision considering the current orientations of the bots and their maximal speed.
        /// </summary>
        /// <returns>The shortest time without a collision.</returns>
        public double GetShortestTimeWithoutCollision()
        {
            // TODO revisit this minimal timing code
            //float min_dist = podbotQuadtree.getShortestDistanceWithoutCollision();
            //min_dist = Math.min(min_dist, podQuadtree.getShortestDistanceWithoutCollision());

            double maxVel = 0.001;	//start non-zero
            foreach (var bot in Bots)
                maxVel = Math.Max(maxVel, bot.MaxVelocity);
            //return min_dist / max_vel;

            //temporary method - simply find the min time a bot can cover its diameter
            //under the diameters, such that bots don't "tunnel" through each other
            double diam = 1.8 * Instance.Bots.First().Radius;
            return diam / maxVel;
        }

        #endregion

        #region Inherited methods

        /// <summary>
        /// Returns a simple string identifying this object in its instance.
        /// </summary>
        /// <returns>A simple name identifying the instance element.</returns>
        public override string GetIdentfierString() { return "Tier" + this.ID; }
        /// <summary>
        /// Returns a simple string giving information about the object.
        /// </summary>
        /// <returns>A simple string.</returns>
        public override string ToString() { return "Tier" + this.ID; }

        #endregion

        #region IUpdateable Members

        /// <summary>
        /// The next event when this element has to be updated.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns>The next time this element has to be updated.</returns>
        public double GetNextEventTime(double currentTime) { return double.PositiveInfinity; }
        /// <summary>
        /// Updates the element to the specified time.
        /// </summary>
        /// <param name="lastTime">The time before the update.</param>
        /// <param name="currentTime">The time to update to.</param>
        public void Update(double lastTime, double currentTime)
        {
            PodQuadTree.UpdateTree();
            BotQuadTree.UpdateTree();
        }

        #endregion

        #region ITierInfo Members

        /// <summary>
        /// Returns the bots currently moving on this tier.
        /// </summary>
        /// <returns>All bots of the tier.</returns>
        public IEnumerable<IBotInfo> GetInfoBots()
        {
            IEnumerable<IBotInfo> bots;
            lock (this)
                bots = Bots.ToArray();
            return bots;
        }
        /// <summary>
        /// Returns the pods currently placed on this tier.
        /// </summary>
        /// <returns>All pods of this tier.</returns>
        public IEnumerable<IPodInfo> GetInfoPods()
        {
            IEnumerable<IPodInfo> pods;
            lock (this)
                pods = Pods.ToArray();
            return pods;
        }
        /// <summary>
        /// Returns the input stations placed on this tier.
        /// </summary>
        /// <returns>All input stations located on this tier.</returns>
        public IEnumerable<IInputStationInfo> GetInfoInputStations() { return InputStations; }
        /// <summary>
        /// Returns the output stations placed on this tier.
        /// </summary>
        /// <returns>All output stations located on this tier.</returns>
        public IEnumerable<IOutputStationInfo> GetInfoOutputStations() { return OutputStations; }
        /// <summary>
        /// Returns all waypoints on this tier.
        /// </summary>
        /// <returns>The waypoints on this tier.</returns>
        public IEnumerable<IWaypointInfo> GetInfoWaypoints() { return Waypoints; }
        /// <summary>
        /// Returns all guards on this tier.
        /// </summary>
        /// <returns>The guards working on this tier.</returns>
        public IEnumerable<IGuardInfo> GetInfoGuards() { return Instance.Semaphores.SelectMany(s => s.Guards).Where(g => g.From.Tier == this); }
        /// <summary>
        /// Returns the vertical position of the tier.
        /// </summary>
        /// <returns>The z-position.</returns>
        public double GetInfoZ() { return RelativePositionZ; }
        /// <summary>
        /// Gets the x-position of the bottom left corner of the objects' area.
        /// </summary>
        /// <returns>The x-position.</returns>
        public double GetInfoTLX() { return RelativePositionX; }
        /// <summary>
        /// Gets the y-position of the bottom left corner of the objects' area.
        /// </summary>
        /// <returns>The y-position.</returns>
        public double GetInfoTLY() { return RelativePositionY; }
        /// <summary>
        /// Gets the x-position of the center of the object.
        /// </summary>
        /// <returns>The x-position.</returns>
        public double GetInfoCenterX() { return RelativePositionX + (0.5 * Length); }
        /// <summary>
        /// Gets the y-position of the center of the object.
        /// </summary>
        /// <returns>The y-position.</returns>
        public double GetInfoCenterY() { return RelativePositionY + (0.5 * Width); }
        /// <summary>
        /// The length of the objects' area. (Corresponds to the x-axis)
        /// </summary>
        /// <returns>The length of the objects' area.</returns>
        public double GetInfoLength() { return Length; }
        /// <summary>
        /// The width of the objects' area. (Corresponds to the y-axis)
        /// </summary>
        /// <returns>The width of the objects' area.</returns>
        public double GetInfoWidth() { return Width; }
        /// <summary>
        /// Gets the current tier this object is placed on. Can't change in case of an immovable object.
        /// </summary>
        /// <returns>The current tier.</returns>
        public ITierInfo GetInfoCurrentTier() { return this; }
        /// <summary>
        /// Returns the active instance belonging to this element.
        /// </summary>
        /// <returns>The active instance.</returns>
        public IInstanceInfo GetInfoInstance() { return Instance; }

        #endregion
    }
}
