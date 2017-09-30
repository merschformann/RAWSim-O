using RAWSimO.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Waypoints;
using RAWSimO.Toolbox;

namespace RAWSimO.Core.Control
{
    /// <summary>
    /// The base manager for creating all repositioning moves.
    /// </summary>
    public abstract class RepositioningManager : IUpdateable, IOptimize, IStatTracker
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public RepositioningManager(Instance instance) { Instance = instance; }

        /// <summary>
        /// The instance this manager is assigned to.
        /// </summary>
        protected Instance Instance { get; set; }
        /// <summary>
        /// Inits this controller.
        /// </summary>
        private void InitTimeouts() { _botTimeouts = new VolatileIDDictionary<Bot, double>(Instance.Bots.Select(b => new VolatileKeyValuePair<Bot, double>(b, double.MinValue)).ToList()); }
        /// <summary>
        /// A global timeout for the repositioning manager.
        /// This can be set to an arbitrary time in the (simulation time) future until which calls to the repositioning manager are blocked to safe computational ressources.
        /// </summary>
        protected double GlobalTimeout { get; set; } = double.NegativeInfinity;
        /// <summary>
        /// The minimal simulation time between two requests for a bot.
        /// </summary>
        private const double REQUEST_TIMEOUT = 15;
        /// <summary>
        /// Manages timeouts for the bots. If a bot is affected by a timeout all requests for a repositioning move for this bot will be ignored.
        /// </summary>
        private VolatileIDDictionary<Bot, double> _botTimeouts;
        /// <summary>
        /// Sets a timeout for a bot. No requests will be handled for the bot until the simulation time passed the timeout timestamp.
        /// </summary>
        /// <param name="bot">The bot to timeout.</param>
        /// <param name="timeout">The time until the bot will be timed out.</param>
        protected void SetTimeout(Bot bot, double timeout) { if (_botTimeouts == null) InitTimeouts(); _botTimeouts[bot] = timeout; }
        /// <summary>
        /// Decides the next repositioning move to do for the given robot.
        /// </summary>
        /// <param name="robot">The robot that is asking to conduct such a move.</param>
        /// <returns>A repositioning move or <code>null</code> if no such move was available.</returns>
        protected abstract RepositioningMove GetRepositioningMove(Bot robot);

        /// <summary>
        /// Gets the next repositioning move to do, if available.
        /// </summary>
        /// <param name="robot">The robot that is asking for a repositioning move to do.</param>
        /// <returns>The repositioning move to do or <code>null</code> if no such move was available.</returns>
        public RepositioningMove GetNextMove(Bot robot)
        {
            // Check timeouts
            if (_botTimeouts == null)
                InitTimeouts();
            if (Instance.Controller.CurrentTime <= _botTimeouts[robot])
                return null;
            if (robot.Pod != null)
                Instance.LogDefault("WARNING! Cannot reposition a pod when the robot already is carrying one!");
            _botTimeouts[robot] = Instance.Controller.CurrentTime + REQUEST_TIMEOUT;
            // Measure time for decision
            DateTime before = DateTime.Now;
            // Fetch next pod
            RepositioningMove move = GetRepositioningMove(robot);
            // Calculate decision time
            Instance.Observer.TimeRepositioning((DateTime.Now - before).TotalSeconds);
            // Return it
            return move;
        }

        #region IUpdateable Members

        /// <summary>
        /// The next event when this element has to be updated.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns>The next time this element has to be updated.</returns>
        public virtual double GetNextEventTime(double currentTime) { return double.PositiveInfinity; }
        /// <summary>
        /// Updates the element to the specified time.
        /// </summary>
        /// <param name="lastTime">The time before the update.</param>
        /// <param name="currentTime">The time to update to.</param>
        public virtual void Update(double lastTime, double currentTime) { /* Nothing to do here. */ }

        #endregion

        #region IOptimize Members

        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        public abstract void SignalCurrentTime(double currentTime);

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

        #region Move implementation

        /// <summary>
        /// Declares a single repositioning operation.
        /// </summary>
        public class RepositioningMove
        {
            /// <summary>
            /// The pod to move to a new storage location.
            /// </summary>
            public Pod Pod;
            /// <summary>
            /// The storage location to move the pod to.
            /// </summary>
            public Waypoint StorageLocation;
        }

        #endregion
    }
}
