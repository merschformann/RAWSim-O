using RAWSimO.Core.Elements;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.PodStorage
{
    /// <summary>
    /// A manager that can be used when the decisions ordinarily made by a pod storage manager are conducted in another manager.
    /// </summary>
    class DummyPodStorageManager : PodStorageManager
    {
        public DummyPodStorageManager(Instance instance) : base(instance) { }

        /// <summary>
        /// Returns a suitable storage location for the given pod.
        /// </summary>
        /// <param name="pod">The pod to fetch a storage location for.</param>
        /// <returns>The storage location to use.</returns>
        protected override Waypoint GetStorageLocationForPod(Pod pod) { return null; }

        #region IOptimize Members

        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        public override void SignalCurrentTime(double currentTime) { /* Nothing to do */ }

        #endregion
    }
}
