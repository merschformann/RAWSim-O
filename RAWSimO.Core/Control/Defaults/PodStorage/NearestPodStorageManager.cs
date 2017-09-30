using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Geometrics;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.Metrics;
using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.PodStorage
{
    /// <summary>
    /// Defines by which distance measure the free storage location is selected.
    /// </summary>
    public enum NearestPodStorageLocationDisposeRule
    {
        /// <summary>
        /// Uses the euclidean distance measure to find the next free storage location.
        /// </summary>
        Euclid,
        /// <summary>
        /// Uses the manhattan distance measure to find the next free storage location.
        /// </summary>
        Manhattan,
        /// <summary>
        /// Uses the shortest path (calculated by A*) to find the next free storage location.
        /// </summary>
        ShortestPath,
        /// <summary>
        /// Uses the most time-efficient path (calculated by A* with turn costs) to find the next free storage location.
        /// </summary>
        ShortestTime,
    }
    /// <summary>
    /// Implements a pod storage manager that aims to use the next free storage location.
    /// </summary>
    public class NearestPodStorageManager : PodStorageManager
    {
        /// <summary>
        /// Creates a new instance of the manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public NearestPodStorageManager(Instance instance) : base(instance) { _config = instance.ControllerConfig.PodStorageConfig as NearestPodStorageConfiguration; }

        /// <summary>
        /// The config for this manager.
        /// </summary>
        private NearestPodStorageConfiguration _config;

        /// <summary>
        /// Returns a suitable storage location for the given pod.
        /// </summary>
        /// <param name="pod">The pod to fetch a storage location for.</param>
        /// <returns>The storage location to use.</returns>
        protected override Waypoint GetStorageLocationForPod(Pod pod)
        {
            double minDistance = double.PositiveInfinity; Waypoint bestStorageLocation = null;
            Waypoint podLocation = // Get current waypoint of pod, if we want to estimate a path
                _config.PodDisposeRule == NearestPodStorageLocationDisposeRule.ShortestPath || _config.PodDisposeRule == NearestPodStorageLocationDisposeRule.ShortestTime ?
                    Instance.WaypointGraph.GetClosestWaypoint(pod.Tier, pod.X, pod.Y) :
                    null;
            foreach (var storageLocation in Instance.ResourceManager.UnusedPodStorageLocations)
            {
                // Calculate the distance
                double distance;
                switch (_config.PodDisposeRule)
                {
                    case NearestPodStorageLocationDisposeRule.Euclid: distance = Distances.CalculateEuclid(pod, storageLocation, Instance.WrongTierPenaltyDistance); break;
                    case NearestPodStorageLocationDisposeRule.Manhattan: distance = Distances.CalculateManhattan(pod, storageLocation, Instance.WrongTierPenaltyDistance); break;
                    case NearestPodStorageLocationDisposeRule.ShortestPath: distance = Distances.CalculateShortestPathPodSafe(podLocation, storageLocation, Instance); break;
                    case NearestPodStorageLocationDisposeRule.ShortestTime: distance = Distances.CalculateShortestTimePathPodSafe(podLocation, storageLocation, Instance); break;
                    default: throw new ArgumentException("Unknown pod dispose rule: " + _config.PodDisposeRule);
                }
                // Update minimum
                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestStorageLocation = storageLocation;
                }
            }
            // Check success
            if (bestStorageLocation == null)
                throw new InvalidOperationException("There was no suitable storage location for the pod: " + pod.ToString());
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
