using RAWSimO.Core.Elements;
using RAWSimO.Core.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Waypoints
{
    /// <summary>
    /// Supplies methods used to manage and guard the entrance, respectively exit, of a semaphore protected area.
    /// </summary>
    public class QueueGuard : IGuardInfo
    {
        /// <summary>
        /// Creates a new object of this instance.
        /// </summary>
        /// <param name="from">The from part of the connection to guard.</param>
        /// <param name="to">The to part of the connection to guard.</param>
        /// <param name="entry">Indicates whether this guard is an entry (<code>true</code>) or an exit (<code>exit</code>).</param>
        /// <param name="barrier">Indicates whether this guard can block the entry dynamically.</param>
        /// <param name="semaphore">The semaphore associated with this guard.</param>
        public QueueGuard(Waypoint from, Waypoint to, bool entry, bool barrier, QueueSemaphore semaphore) { From = from; To = to; Entry = entry; Barrier = barrier; Semaphore = semaphore; }

        /// <summary>
        /// The from part of this guarded connection.
        /// </summary>
        public Waypoint From { get; private set; }

        /// <summary>
        /// The to part of this guarded connection.
        /// </summary>
        public Waypoint To { get; private set; }

        /// <summary>
        /// Indicates whether this guard is an entry (<code>true</code>) or an exit (<code>false</code>).
        /// </summary>
        public bool Entry { get; private set; }

        /// <summary>
        /// Indicates whether this guard can block the entry dynamically.
        /// </summary>
        public bool Barrier { get; private set; }

        /// <summary>
        /// The semaphore associated with this guard.
        /// </summary>
        public QueueSemaphore Semaphore { get; private set; }

        /// <summary>
        /// Indicates whether the protected area is accessible (not at its limit) or not.
        /// </summary>
        public bool IsAccessible { get { return !Barrier || Semaphore.IsAccessible; } }

        /// <summary>
        /// Called when a robot passes this guard.
        /// </summary>
        /// <param name="bot">The bot that passed the guard.</param>
        public void Pass(Bot bot)
        {
            if (Entry)
            {
                Semaphore.Access(bot);
                bot.IsQueueing = true;
            }
            else
            {
                Semaphore.Release(bot);
                bot.IsQueueing = false;
            }
        }

        #region Inherited members

        /// <summary>
        /// Returns a simple string giving information about the object.
        /// </summary>
        /// <returns>A simple string.</returns>
        public override string ToString() { return "Guard-" + From.ToString() + "->" + To.ToString(); }

        #endregion

        #region IGuardInfo Members

        /// <summary>
        /// Indicates whether this guard is monitoring the entrance or exit of a queue.
        /// </summary>
        /// <returns><code>true</code> if the guard monitors an entrance, <code>false</code> otherwise.</returns>
        public bool GetInfoIsBarrier() { return Barrier; }
        /// <summary>
        /// Indicates whether this guard serves as an entry to the protected area or an exit.
        /// </summary>
        /// <returns><code>true</code> if it is an entry, <code>false</code> otherwise.</returns>
        public bool GetInfoIsEntry() { return Entry; }
        /// <summary>
        /// Indicates whether this guard is currently blocked.
        /// </summary>
        /// <returns><code>false</code> if the guard is in block mode, <code>true</code> otherwise.</returns>
        public bool GetInfoIsAccessible() { return IsAccessible; }
        /// <summary>
        /// Returns the current number of requests.
        /// </summary>
        /// <returns>The number of requests.</returns>
        public int GetInfoRequests() { return Semaphore.Requests; }
        /// <summary>
        /// Returns the maximal capacity.
        /// </summary>
        /// <returns>The maximal capacity.</returns>
        public int GetInfoCapacity() { return Semaphore.Capacity; }
        /// <summary>
        /// Returns the start waypoint of the guarded path.
        /// </summary>
        /// <returns>The start waypoint.</returns>
        public IWaypointInfo GetInfoFrom() { return From; }
        /// <summary>
        /// Returns the end waypoint of the guarded path.
        /// </summary>
        /// <returns>The end waypoint.</returns>
        public IWaypointInfo GetInfoTo() { return To; }
        /// <summary>
        /// Returns the corresponding semaphore.
        /// </summary>
        /// <returns>The semaphore this guard belongs to.</returns>
        public ISemaphoreInfo GetInfoSemaphore() { return Semaphore; }

        #endregion

        #region IImmovableObjectInfo Members

        /// <summary>
        /// The diameter of a guard (only relevant for visualization).
        /// </summary>
        public const double GUARD_DIAMETER = 0.1;
        /// <summary>
        /// The length of the objects' area. (Corresponds to the x-axis)
        /// </summary>
        /// <returns>The length of the objects' area.</returns>
        public double GetInfoLength() { return GUARD_DIAMETER; }
        /// <summary>
        /// The width of the objects' area. (Corresponds to the y-axis)
        /// </summary>
        /// <returns>The width of the objects' area.</returns>
        public double GetInfoWidth() { return GUARD_DIAMETER; }

        #endregion

        #region IGeneralObjectInfo Members

        /// <summary>
        /// Gets the x-position of the center of the object.
        /// </summary>
        /// <returns>The x-position.</returns>
        public double GetInfoCenterX()
        {
            // Calculate intermediate values
            double third = Math.Abs(From.X - To.X) / 3.0;
            double xEntry = From.X < To.X ? To.X - third : To.X + third;
            double xExit = From.X < To.X ? From.X + third : From.X - third;
            // If entry gate mark the center near to the to side
            if (Entry)
                return xEntry;
            // If exit gate mark the center near to the from side
            else
                return xExit;
        }
        /// <summary>
        /// Gets the y-position of the center of the object.
        /// </summary>
        /// <returns>The y-position.</returns>
        public double GetInfoCenterY()
        {
            // Calculate intermediate values
            double third = Math.Abs(From.Y - To.Y) / 3.0;
            double yEntry = From.Y < To.Y ? To.Y - third : To.Y + third;
            double yExit = From.Y < To.Y ? From.Y + third : From.Y - third;
            // If entry gate mark the center near to the to side
            if (Entry)
                return yEntry;
            // If exit gate mark the center near to the from side
            else
                return yExit;
        }
        /// <summary>
        /// Gets the x-position of the bottom left corner of the objects' area.
        /// </summary>
        /// <returns>The x-position.</returns>
        public double GetInfoTLX() { return Math.Abs(From.X - To.X) / 2.0 - GUARD_DIAMETER / 2.0; }
        /// <summary>
        /// Gets the y-position of the bottom left corner of the objects' area.
        /// </summary>
        /// <returns>The y-position.</returns>
        public double GetInfoTLY() { return Math.Abs(From.Y - To.Y) / 2.0 + GUARD_DIAMETER / 2.0; }
        /// <summary>
        /// Gets the ID of the object.
        /// </summary>
        /// <returns>The ID.</returns>
        public int GetInfoID() { return 0; }
        /// <summary>
        /// Gets the current tier this object is placed on. Can't change in case of an immovable object.
        /// </summary>
        /// <returns>The current tier.</returns>
        public ITierInfo GetInfoCurrentTier() { return From.Tier; }
        /// <summary>
        /// Returns the active instance belonging to this element.
        /// </summary>
        /// <returns>The active instance.</returns>
        public IInstanceInfo GetInfoInstance() { return From.Tier.Instance; }

        #endregion
    }
}
