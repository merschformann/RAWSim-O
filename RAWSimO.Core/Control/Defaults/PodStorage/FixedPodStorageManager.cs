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
    /// Defines a manager that does not change the assignment of pods to storage locations over time.
    /// </summary>
    public class FixedPodStorageManager : PodStorageManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public FixedPodStorageManager(Instance instance) : base(instance)
        {
            // Store initial positions
            _positions = instance.Pods.ToDictionary(k => k, v => instance.WaypointGraph.GetClosestWaypoint(v.Tier, v.X, v.Y));
            // Add late initialization hook to block fixed positions from being used for resting robots
            instance.LateInit += () =>
            {
                // Block the fixed locations from bots using them
                foreach (var fixLocation in _positions.Values)
                    instance.ResourceManager.ForbidRestLocation(fixLocation);
            };
        }

        /// <summary>
        /// The positions of the pods.
        /// </summary>
        private Dictionary<Pod, Waypoint> _positions;

        /// <summary>
        /// Returns a suitable storage location for the given pod.
        /// </summary>
        /// <param name="pod">The pod to fetch a storage location for.</param>
        /// <returns>The storage location to use.</returns>
        protected override Waypoint GetStorageLocationForPod(Pod pod) { return _positions[pod]; }

        #region IOptimize Members

        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        public override void SignalCurrentTime(double currentTime) { /* Ignore since this simple manager is always ready. */ }

        #endregion
    }
}
