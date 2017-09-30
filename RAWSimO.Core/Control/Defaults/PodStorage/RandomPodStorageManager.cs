using RAWSimO.Core.Configurations;
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
    /// Implements a pod storage manager that randomly assigns pods to free storage locations.
    /// </summary>
    public class RandomPodStorageManager : PodStorageManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public RandomPodStorageManager(Instance instance) : base(instance) { _config = Instance.ControllerConfig.PodStorageConfig as RandomPodStorageConfiguration; }

        private RandomPodStorageConfiguration _config;

        /// <summary>
        /// Returns a suitable storage location for the given pod.
        /// </summary>
        /// <param name="pod">The pod to fetch a storage location for.</param>
        /// <returns>The storage location to use.</returns>
        protected override Waypoint GetStorageLocationForPod(Pod pod)
        {
            // Prefer same tier, if desired and there are more than one
            if (_config.PreferSameTier && Instance.Compound.Tiers.Count > 1)
            {
                // Try to get a location on the same tier
                Waypoint[] locationsOfThisTier = Instance.ResourceManager.UnusedPodStorageLocations.Where(l => l.Tier == pod.Tier).ToArray();
                // Return a location on this tier, if there is one
                if (locationsOfThisTier.Any())
                    return locationsOfThisTier[Instance.Randomizer.NextInt(locationsOfThisTier.Length)];
            }
            // Check success
            if (!Instance.ResourceManager.UnusedPodStorageLocations.Any())
                throw new InvalidOperationException("There was no suitable storage location for the pod: " + pod.ToString());
            // Default behavior: return a random location of all available ones
            return Instance.ResourceManager.UnusedPodStorageLocations.ElementAt(Instance.Randomizer.NextInt(Instance.ResourceManager.UnusedPodStorageLocations.Count()));
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
