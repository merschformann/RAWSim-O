using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Geometrics;
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
    public enum StationBasedPodStorageLocationDisposeRule
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
    /// Implements a pod storage manager that aims to keep the pods near to the stations.
    /// </summary>
    public class StationBasedPodStorageManager : PodStorageManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public StationBasedPodStorageManager(Instance instance) : base(instance) { _config = instance.ControllerConfig.PodStorageConfig as StationBasedPodStorageConfiguration; }

        /// <summary>
        /// The config for this manager.
        /// </summary>
        private StationBasedPodStorageConfiguration _config;

        /// <summary>
        /// Decides the waypoint to use when storing a pod. This call is measured by the timing done.
        /// </summary>
        /// <param name="pod">The pod to store.</param>
        /// <returns>The waypoint to use.</returns>
        protected override Waypoint GetStorageLocationForPod(Pod pod)
        {
            double minDistance = double.PositiveInfinity; Waypoint bestStorageLocation = null;
            foreach (var storageLocation in Instance.ResourceManager.UnusedPodStorageLocations)
            {
                // Calculate the distance
                double distance;
                switch (_config.PodDisposeRule)
                {
                    case StationBasedPodStorageLocationDisposeRule.Euclid:
                        distance = (_config.OutputStationMode ? Instance.OutputStations.Cast<Circle>() : Instance.InputStations.Cast<Circle>())
                            .Min(station => Distances.CalculateEuclid(station, storageLocation, Instance.WrongTierPenaltyDistance));
                        break;
                    case StationBasedPodStorageLocationDisposeRule.Manhattan:
                        distance = (_config.OutputStationMode ? Instance.OutputStations.Cast<Circle>() : Instance.InputStations.Cast<Circle>())
                            .Min(station => Distances.CalculateManhattan(station, storageLocation, Instance.WrongTierPenaltyDistance));
                        break;
                    case StationBasedPodStorageLocationDisposeRule.ShortestPath:
                        distance = (_config.OutputStationMode ? Instance.OutputStations.Select(s => s.Waypoint) : Instance.InputStations.Select(s => s.Waypoint))
                            .Min(station => Distances.CalculateShortestPathPodSafe(storageLocation, station, Instance));
                        break;
                    case StationBasedPodStorageLocationDisposeRule.ShortestTime:
                        distance = (_config.OutputStationMode ? Instance.OutputStations.Select(s => s.Waypoint) : Instance.InputStations.Select(s => s.Waypoint))
                            .Min(station => Distances.CalculateShortestTimePathPodSafe(storageLocation, station, Instance));
                        break;
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
