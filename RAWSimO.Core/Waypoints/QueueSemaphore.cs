using RAWSimO.Core.Elements;
using RAWSimO.Core.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RAWSimO.Core.Waypoints
{
    /// <summary>
    /// Supplies methods to control a protected area in which only a previously fixed amount of robots can be at a time.
    /// </summary>
    public class QueueSemaphore : InstanceElement, ISemaphoreInfo
    {
        /// <summary>
        /// Creates a new object of this instance.
        /// </summary>
        /// <param name="instance">The instance this element belongs to.</param>
        /// <param name="maximalCount">The maximal number of bots allowed concurrently in the guarded area.</param>
        internal QueueSemaphore(Instance instance, int maximalCount) : base(instance) { Requests = 0; Capacity = maximalCount; }

        /// <summary>
        /// The number of bots currently in this area.
        /// </summary>
        public int Requests { get; private set; }

        /// <summary>
        /// The maximal number of bots allowed concurrently in the guarded area.
        /// </summary>
        public int Capacity { get; private set; }

        /// <summary>
        /// All bots currently in this area.
        /// </summary>
        private HashSet<Bot> _bots = new HashSet<Bot>();

        /// <summary>
        /// All entrances and exits of this area.
        /// </summary>
        private HashSet<QueueGuard> _guards = new HashSet<QueueGuard>();

        /// <summary>
        /// All entrances and exits of this area.
        /// </summary>
        public IEnumerable<QueueGuard> Guards { get { return _guards; } }

        /// <summary>
        /// Register a new entrance / exit of this area.
        /// </summary>
        /// <param name="from">The from part of the connecting edge.</param>
        /// <param name="to">The to part of the connecting edge.</param>
        /// <param name="entry">Defines whether the connection is an entrance or an exit.</param>
        /// <param name="barrier">Defines whether the connection is blocked as soon as the limit of requests is reached.</param>
        /// <returns>The guard protecting the corresponding connection.</returns>
        public QueueGuard RegisterGuard(Waypoint from, Waypoint to, bool entry, bool barrier)
        {
            QueueGuard guard = new QueueGuard(from, to, entry, barrier, this);
            _guards.Add(guard);
            from.RegisterGuard(guard);
            return guard;
        }
        /// <summary>
        /// Unregisters the guard from this semaphore.
        /// </summary>
        /// <param name="guard">The guard to remove.</param>
        public void UnregisterGuard(QueueGuard guard)
        {
            _guards.Remove(guard);
            guard.From.UnregisterGuard(guard);
        }

        /// <summary>
        /// Indicates whether the semaphore is accessible (not at its limit) or not.
        /// </summary>
        public bool IsAccessible { get { return Requests < Capacity; } }

        /// <summary>
        /// Called when a bot passes a guard and enters the protected area.
        /// </summary>
        /// <param name="bot">The bot that passed the guard.</param>
        public void Access(Bot bot) { if (!_bots.Contains(bot)) { Requests++; _bots.Add(bot); } }

        /// <summary>
        /// Called when a bot leaves the area.
        /// </summary>
        /// <param name="bot">The bot that just left.</param>
        public void Release(Bot bot) { if (_bots.Contains(bot)) { Requests = Math.Max(Requests - 1, 0); _bots.Remove(bot); } }

        #region Inherited members

        /// <summary>
        /// Returns a simple string identifying this object in its instance.
        /// </summary>
        /// <returns>A simple name identifying the instance element.</returns>
        public override string GetIdentfierString() { return "Semaphore" + ID.ToString(); }
        /// <summary>
        /// Returns a simple string giving information about the object.
        /// </summary>
        /// <returns>A simple string.</returns>
        public override string ToString() { return "Semaphore" + ID.ToString(); }

        #endregion

        #region ISemaphoreInfo Members

        /// <summary>
        /// The number of guards associated with this semaphore.
        /// </summary>
        /// <returns>The number of guards belonging to this semaphore.</returns>
        public int GetInfoGuards() { return _guards.Count; }

        #endregion
    }
}
