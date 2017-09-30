using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.PodStorage
{
    /// <summary>
    /// Implements a manager that decides about the storage location for a pod by using the pod utility shared component.
    /// </summary>
    public class UtilityPodStorageManager : PodStorageManager
    {
        /// <summary>
        /// Creates a new instance of the manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public UtilityPodStorageManager(Instance instance) : base(instance) { _config = instance.ControllerConfig.PodStorageConfig as UtilityPodStorageConfiguration; }

        /// <summary>
        /// The config for this manager.
        /// </summary>
        private UtilityPodStorageConfiguration _config;
        /// <summary>
        /// Indicates whether the shared components have been initialized.
        /// </summary>
        private bool _initialized = false;

        /// <summary>
        /// Returns a suitable storage location for the given pod.
        /// </summary>
        /// <param name="pod">The pod to fetch a storage location for.</param>
        /// <returns>The storage location to use.</returns>
        protected override Waypoint GetStorageLocationForPod(Pod pod)
        {
            // Init shared components, if not done yet
            if (!_initialized)
            {
                // Init or ensure consistency of configurations
                Instance.SharedControlElements.PodUtilityManager.InitOrEnsureInit(_config.UtilityConfig);
                _initialized = true;
            }
            // Get storage location for pod
            Waypoint bestStorageLocation = Instance.SharedControlElements.PodUtilityManager.GetStorageLocation(pod, Instance.WaypointGraph.GetClosestWaypoint(pod.Tier, pod.X, pod.Y));
            // Return it
            return bestStorageLocation;
        }

        #region IOptimize Members

        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        public override void SignalCurrentTime(double currentTime) { /* Ignore since this simple manager is always ready. */ }

        #endregion
    }
}
