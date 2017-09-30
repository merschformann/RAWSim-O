using RAWSimO.Core.Configurations;
using RAWSimO.Core.Control.Shared;
using RAWSimO.Core.Elements;
using RAWSimO.Core.IO;
using RAWSimO.Core.Metrics;
using RAWSimO.Core.Waypoints;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.PodStorage
{
    /// <summary>
    /// Supplies a turnover based pod storage manager.
    /// </summary>
    public class TurnoverPodStorageManager : PodStorageManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public TurnoverPodStorageManager(Instance instance) : base(instance)
        {
            _config = instance.ControllerConfig.PodStorageConfig as TurnoverPodStorageConfiguration;
            // Initialize class manager
            _classManager = instance.SharedControlElements.TurnoverClassBuilder;
            _classManager.ParseConfigAndEnsureCompatibility(
                _config.ClassBorders.Split(IOConstants.DELIMITER_LIST).Select(e => double.Parse(e, IOConstants.FORMATTER)).OrderBy(v => v).ToArray(),
                _config.ReallocationDelay,
                _config.ReallocationOrderCount);
        }

        /// <summary>
        /// The config for this manager.
        /// </summary>
        private TurnoverPodStorageConfiguration _config;
        /// <summary>
        /// The class manager in use.
        /// </summary>
        private TurnoverClassBuilder _classManager;

        /// <summary>
        /// Chooses the storage location to use for the given pod.
        /// </summary>
        /// <param name="pod">The pod to store.</param>
        /// <returns>The storage location to use for the pod.</returns>
        private Waypoint ChooseStorageLocation(Pod pod)
        {
            // Get the storage class the pod should end up in
            int desiredStorageClass = _classManager.DetermineStorageClass(pod);
            // Try to allocate the pod to its storage class - if not possible try neighboring classes
            int currentClassTriedLow = desiredStorageClass; int currentClassTriedHigh = desiredStorageClass;
            Waypoint chosenStorageLocation = null;
            while (true)
            {
                // Try the less frequent class first
                if (currentClassTriedLow < _classManager.ClassCount)
                    chosenStorageLocation = _classManager.GetClassStorageLocations(currentClassTriedLow)
                        .Where(wp => !Instance.ResourceManager.IsStorageLocationClaimed(wp)) // Only use not occupied ones
                        .OrderBy(wp =>
                        {
                            switch (_config.PodDisposeRule)
                            {
                                case TurnoverPodStorageLocationDisposeRule.NearestEuclid:
                                    return Distances.CalculateEuclid(wp, pod, Instance.WrongTierPenaltyDistance);
                                case TurnoverPodStorageLocationDisposeRule.NearestManhattan:
                                    return Distances.CalculateManhattan(wp, pod, Instance.WrongTierPenaltyDistance);
                                case TurnoverPodStorageLocationDisposeRule.NearestShortestPath:
                                    return Distances.CalculateShortestPathPodSafe(Instance.WaypointGraph.GetClosestWaypoint(pod.Tier, pod.X, pod.Y), wp, Instance);
                                case TurnoverPodStorageLocationDisposeRule.NearestShortestTime:
                                    return Distances.CalculateShortestTimePathPodSafe(Instance.WaypointGraph.GetClosestWaypoint(pod.Tier, pod.X, pod.Y), wp, Instance);
                                case TurnoverPodStorageLocationDisposeRule.OStationNearestEuclid:
                                    return Instance.OutputStations.Min(s => Distances.CalculateEuclid(wp, s, Instance.WrongTierPenaltyDistance));
                                case TurnoverPodStorageLocationDisposeRule.OStationNearestManhattan:
                                    return Instance.OutputStations.Min(s => Distances.CalculateManhattan(wp, s, Instance.WrongTierPenaltyDistance));
                                case TurnoverPodStorageLocationDisposeRule.OStationNearestShortestPath:
                                    return Instance.OutputStations.Min(s => Distances.CalculateShortestPathPodSafe(wp, s.Waypoint, Instance));
                                case TurnoverPodStorageLocationDisposeRule.OStationNearestShortestTime:
                                    return Instance.OutputStations.Min(s => Distances.CalculateShortestTimePathPodSafe(wp, s.Waypoint, Instance));
                                case TurnoverPodStorageLocationDisposeRule.Random:
                                    return wp.Instance.Randomizer.NextDouble();
                                default: throw new ArgumentException("Unknown pod dispose rule: " + _config.PodDisposeRule);
                            }
                        }) // Order the remaining ones by the given rule
                        .FirstOrDefault(); // Use the first one
                // Check whether we found a suitable pod of this class
                if (chosenStorageLocation != null)
                    break;
                // Try the higher frequent class next
                if (currentClassTriedHigh >= 0 && currentClassTriedHigh != currentClassTriedLow)
                    chosenStorageLocation = _classManager.GetClassStorageLocations(currentClassTriedHigh)
                        .Where(wp => !Instance.ResourceManager.IsStorageLocationClaimed(wp)) // Only use not occupied ones
                        .OrderBy(wp =>
                        {
                            switch (_config.PodDisposeRule)
                            {
                                case TurnoverPodStorageLocationDisposeRule.NearestEuclid:
                                    return Distances.CalculateEuclid(wp, pod, Instance.WrongTierPenaltyDistance);
                                case TurnoverPodStorageLocationDisposeRule.NearestManhattan:
                                    return Distances.CalculateManhattan(wp, pod, Instance.WrongTierPenaltyDistance);
                                case TurnoverPodStorageLocationDisposeRule.NearestShortestPath:
                                    return Distances.CalculateShortestPathPodSafe(Instance.WaypointGraph.GetClosestWaypoint(pod.Tier, pod.X, pod.Y), wp, Instance);
                                case TurnoverPodStorageLocationDisposeRule.NearestShortestTime:
                                    return Distances.CalculateShortestTimePathPodSafe(Instance.WaypointGraph.GetClosestWaypoint(pod.Tier, pod.X, pod.Y), wp, Instance);
                                case TurnoverPodStorageLocationDisposeRule.OStationNearestEuclid:
                                    return Instance.OutputStations.Min(s => Distances.CalculateEuclid(wp, s, Instance.WrongTierPenaltyDistance));
                                case TurnoverPodStorageLocationDisposeRule.OStationNearestManhattan:
                                    return Instance.OutputStations.Min(s => Distances.CalculateManhattan(wp, s, Instance.WrongTierPenaltyDistance));
                                case TurnoverPodStorageLocationDisposeRule.OStationNearestShortestPath:
                                    return Instance.OutputStations.Min(s => Distances.CalculateShortestPathPodSafe(wp, s.Waypoint, Instance));
                                case TurnoverPodStorageLocationDisposeRule.OStationNearestShortestTime:
                                    return Instance.OutputStations.Min(s => Distances.CalculateShortestTimePathPodSafe(wp, s.Waypoint, Instance));
                                case TurnoverPodStorageLocationDisposeRule.Random:
                                    return wp.Instance.Randomizer.NextDouble();
                                default: throw new ArgumentException("Unknown pod dispose rule: " + _config.PodDisposeRule);
                            }
                        }) // Order the remaining ones by the given rule
                        .FirstOrDefault(); // Use the first one
                // Check whether we found a suitable pod of this class
                if (chosenStorageLocation != null)
                    break;
                // Update the class indeces to check next
                currentClassTriedLow++; currentClassTriedHigh--;
                // Check index correctness
                if (currentClassTriedHigh < 0 && currentClassTriedLow >= _classManager.ClassCount)
                    throw new InvalidOperationException("There was no storage location available!");
            }
            // Return the chosen one
            return chosenStorageLocation;
        }

        /// <summary>
        /// Determines the storage location for the given pod.
        /// </summary>
        /// <param name="pod">The pod to store.</param>
        /// <returns>The storage location to use for storing the pod.</returns>
        protected override Waypoint GetStorageLocationForPod(Pod pod)
        {
            // Choose
            Waypoint chosenLocation = ChooseStorageLocation(pod);
            // Check success
            if (chosenLocation == null)
                throw new InvalidOperationException("There was no suitable storage location for the pod: " + pod.ToString());
            // Return it
            return chosenLocation;
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
