using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Items;
using RAWSimO.Core.Metrics;
using RAWSimO.Core.Waypoints;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Shared
{
    /// <summary>
    /// Exposes methods for managing pods and the storage locations they should be placed on by determining their utility.
    /// </summary>
    public class PodUtilityManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance to manage.</param>
        public PodUtilityManager(Instance instance)
        {
            _instance = instance;
            _instance.NewOrder += NewOrder;
            _instance.ItemExtracted += ItemExtracted;
            _instance.BundleStored += BundleStored;
        }

        #region Callbacks

        private void BundleStored(InputStation iStation, Bot bot, Pod pod, ItemBundle bundle) { _temporaryValuesOutdated = true; }

        private void ItemExtracted(Pod pod, ItemDescription item) { _temporaryValuesOutdated = true; }

        private void NewOrder(Order order) { _temporaryValuesOutdated = true; }

        #endregion

        /// <summary>
        /// The instance this manager manages.
        /// </summary>
        private Instance _instance;
        /// <summary>
        /// The config to use.
        /// </summary>
        private PodUtilityConfiguration _config;

        /// <summary>
        /// The number of storage locations that actually shall be used.
        /// </summary>
        private int _storageLocationsToUse;
        /// <summary>
        /// Indicates that the temporary values that have been calculated are outdated.
        /// </summary>
        private bool _temporaryValuesOutdated = true;
        /// <summary>
        /// Storage for pod score values.
        /// </summary>
        private VolatileIDDictionary<Pod, double> _temporaryPodScores;
        /// <summary>
        /// Storage for pod ranks.
        /// </summary>
        private VolatileIDDictionary<Pod, int> _temporaryPodScoreIndeces;

        /// <summary>
        /// Initializes this manager.
        /// </summary>
        public void InitOrEnsureInit(PodUtilityConfiguration config)
        {
            // Check whether this has already been done
            if (_config != null)
            {
                // --> Ensure compatibility
                if (!_config.Match(config))
                    throw new ArgumentException("Incompatible pod utility configurations: " + _config.ToString() + " vs. " + config.ToString());
            }
            else
            {
                // Store config
                _config = config;
                // Calculate some additional info
                _storageLocationsToUse = (int)(_instance.ElementMetaInfoTracker.StorageLocationsOrdered.Count * _config.BufferStorageLocations) + _instance.Pods.Count;
                // Init temp score storage
                _temporaryPodScores = new VolatileIDDictionary<Pod, double>(_instance.Pods.Select(p => new VolatileKeyValuePair<Pod, double>(p, 0)).ToList());
                _temporaryPodScoreIndeces = new VolatileIDDictionary<Pod, int>(_instance.Pods.Select(p => new VolatileKeyValuePair<Pod, int>(p, 0)).ToList());
            }
        }

        /// <summary>
        /// Determines the prominence rank suitable for the given pod.
        /// </summary>
        /// <param name="pod">The pod to get the storage rank for.</param>
        /// <returns>The storage rank for the given pod.</returns>
        public int DetermineRank(Pod pod)
        {
            // Check whether we can use precalculated values
            int podScoreIndex;
            if (!_temporaryValuesOutdated)
                // Use precalculated pod score index
                podScoreIndex = _temporaryPodScoreIndeces[pod];
            else
                // Obtain index of the pod among all pods
                podScoreIndex = _instance.Pods.RankOfDescending(pod, (Pod p) => _instance.ElementMetaInfoTracker.GetPodCombinedScore(p, _config.WeightSpeed, _config.WeightUtility));
            // Obtain fractional index from absolute index
            double fractionalScoreIndex = (double)podScoreIndex / _instance.Pods.Count;
            // Convert the fractional pod index to a suitable index among the storage locations and return it
            return _instance.ElementMetaInfoTracker.GetStorageLocationRank(_instance.ElementMetaInfoTracker.StorageLocationsOrdered[(int)Math.Floor(fractionalScoreIndex * _storageLocationsToUse)]);
        }

        /// <summary>
        /// Precalculates the rank information for all pods for faster access.
        /// </summary>
        public void PrepareAllPodRanks()
        {
            // Calculate scores
            foreach (var pod in _instance.Pods)
                _temporaryPodScores[pod] = _instance.ElementMetaInfoTracker.GetPodCombinedScore(pod, _config.WeightSpeed, _config.WeightUtility);
            int currentRank = -1; double currentScore = double.PositiveInfinity;
            // Iterate all pods (sorted) to determine the ranks
            foreach (var pod in _instance.Pods.OrderByDescending(p => _temporaryPodScores[p]))
            {
                // Update rank info
                if (_temporaryPodScores[pod] < currentScore)
                {
                    currentScore = _temporaryPodScores[pod];
                    currentRank++;
                }
                // Set info for pod
                _temporaryPodScoreIndeces[pod] = currentRank;
            }
            // Signal pod ranks precalculated
            _temporaryValuesOutdated = false;
        }

        /// <summary>
        /// Gets the storage location most suitable for the given pod.
        /// </summary>
        /// <param name="pod">The pod to get a storage location for.</param>
        /// <param name="tripStart">The current location of the pod / where the trip starts (to improve the distance traveled).</param>
        /// <returns>The storage location to use for the given pod.</returns>
        public Waypoint GetStorageLocation(Pod pod, Waypoint tripStart)
        {
            // Search for a storage location that is available around the best rank
            int storageLocationRank = DetermineRank(pod);
            int ranksAssessed = 0; Waypoint bestStorageLocation = null; double bestTripTime = double.PositiveInfinity; bool goBetter = false;
            int betterRank = storageLocationRank;
            int worseRank = storageLocationRank;
            while (
                // keep searching as long as not enough ranks have been assessed and ...
                ranksAssessed < _config.RankCorridor ||
                // no storage location was found at all
                bestStorageLocation == null)
            {
                // Change search direction
                goBetter = !goBetter;
                // Get current rank
                int currentRank = goBetter ? betterRank : worseRank;
                // Update index for next iteration
                if (goBetter) betterRank--;
                else worseRank++;
                // Check all unused storage locations of the rank
                foreach (var storageLocation in _instance.ElementMetaInfoTracker.GetStorageLocationsOfRank(currentRank).Where(sl => !_instance.ResourceManager.IsStorageLocationClaimed(sl)))
                {
                    // Check whether storage location is a new best
                    double tripTime = Distances.CalculateShortestTimePathPodSafe(tripStart, storageLocation, _instance);
                    if (tripTime < bestTripTime)
                    {
                        bestTripTime = tripTime;
                        bestStorageLocation = storageLocation;
                    }
                }
                // Keep track of assessed locations
                ranksAssessed++;
            }
            // Return it
            return bestStorageLocation;
        }
    }
}
