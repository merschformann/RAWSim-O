using RAWSimO.Core.Elements;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control
{
    /// <summary>
    /// The base class of all mechanisms deciding the position when storing a pod.
    /// </summary>
    public abstract class PodStorageManager : IUpdateable, IOptimize, IStatTracker
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public PodStorageManager(Instance instance) { Instance = instance; }

        /// <summary>
        /// The instance this manager is assigned to.
        /// </summary>
        protected Instance Instance { get; set; }

        /// <summary>
        /// Decides the waypoint to use when storing a pod. This call is measured by the timing done.
        /// </summary>
        /// <param name="pod">The pod to store.</param>
        /// <returns>The waypoint to use.</returns>
        protected abstract Waypoint GetStorageLocationForPod(Pod pod);

        #region IPodStorageManager Members

        /// <summary>
        /// Determines the storage location for the given pod.
        /// </summary>
        /// <param name="pod">The pod.</param>
        /// <returns>The storage location to use.</returns>
        public Waypoint GetStorageLocation(Pod pod)
        {
            // Measure time for decision
            DateTime before = DateTime.Now;
            // Fetch storage location
            Waypoint wp = GetStorageLocationForPod(pod);
            // Calculate decision time
            Instance.Observer.TimePodStorage((DateTime.Now - before).TotalSeconds);
            // Return it
            return wp;
        }

        #endregion

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
    }
}
