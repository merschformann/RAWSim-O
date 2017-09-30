using RAWSimO.Core.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Waypoints;
using RAWSimO.Toolbox;
using RAWSimO.Core.Control.Shared;
using RAWSimO.Core.Metrics;

namespace RAWSimO.Core.Control.Defaults.Repositioning
{
    /// <summary>
    /// A repositioning manager aiming to store useful pods in a cache zone while freeing up space in front of the output-stations.
    /// </summary>
    public class CacheDropoffRepositioningManager : RepositioningManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public CacheDropoffRepositioningManager(Instance instance) : base(instance) { Instance = instance; _config = instance.ControllerConfig.RepositioningConfig as CacheDropoffRepositioningConfiguration; }
        /// <summary>
        /// The configuration.
        /// </summary>
        private CacheDropoffRepositioningConfiguration _config;
        /// <summary>
        /// The stations belonging to the respective storage location's cache.
        /// </summary>
        private VolatileIDDictionary<Waypoint, OutputStation> _storageLocationStations;
        /// <summary>
        /// The number of pods targeted for the caches.
        /// </summary>
        private VolatileIDDictionary<OutputStation, int> _cacheTargetCounts;
        /// <summary>
        /// The number of pods for the respective cache.
        /// </summary>
        private VolatileIDDictionary<OutputStation, int> _cachePodCounts;
        /// <summary>
        /// Contains information about whether the current pod is cacheable per output station.
        /// </summary>
        private VolatileIDDictionary<OutputStation, bool> _cacheableInfoPerStation;
        /// <summary>
        /// Contains information about whether the current pod is cacheable at all.
        /// </summary>
        private bool _cacheableAtAll = false;

        /// <summary>
        /// A helper used to determine the best move.
        /// </summary>
        private BestCandidateSelector _bestCandidateSelector = null;
        /// <summary>
        /// A helper variable storing the current bot that asks for the move.
        /// </summary>
        private Bot _currentBot = null;
        /// <summary>
        /// A helper variable storing the current pod to assess.
        /// </summary>
        private Pod _currentPod = null;
        /// <summary>
        /// A helper variable storing the current waypoint to assess.
        /// </summary>
        private Waypoint _currentStorageLocation = null;

        private void Init()
        {
            // --> Init shared component or ensure consistency
            Instance.SharedControlElements.StoragePartitioner.CreateOrEnsureZones(_config.ZoningConfiguration);
            // --> Init meta info
            _cacheTargetCounts = new VolatileIDDictionary<OutputStation, int>(
                Instance.OutputStations.Select(s => new VolatileKeyValuePair<OutputStation, int>(s,
                    (int)(_config.TargetFillCache * Instance.SharedControlElements.StoragePartitioner.CachePartitions[s].Count))).ToList());
            _cachePodCounts = new VolatileIDDictionary<OutputStation, int>(
                Instance.OutputStations.Select(s => new VolatileKeyValuePair<OutputStation, int>(s, 0)).ToList());
            _storageLocationStations = new VolatileIDDictionary<Waypoint, OutputStation>(
                Instance.Waypoints.Select(w => new VolatileKeyValuePair<Waypoint, OutputStation>(w, null)).ToList());
            foreach (var station in Instance.OutputStations)
                foreach (var wp in Instance.SharedControlElements.StoragePartitioner.CachePartitions[station])
                    _storageLocationStations[wp] = station;
            _cacheableInfoPerStation = new VolatileIDDictionary<OutputStation, bool>(
                Instance.OutputStations.Select(s => new VolatileKeyValuePair<OutputStation, bool>(s, false)).ToList());
            // --> Init move scoring
            List<Func<double>> scorers = new List<Func<double>>();
            // First try to keep the move on the same tier, if desired (this heavily influences the possible moves)
            if (_config.PreferSameTierMoves)
                scorers.Add(() => { return _currentPod.Tier == _currentBot.Tier && _currentStorageLocation.Tier == _currentBot.Tier ? 1 : 0; });
            // Then decide about move type
            scorers.Add(() =>
            {
                if (_currentPod.Waypoint.InfoTagCache == ZoneType.Dropoff && _currentStorageLocation.InfoTagCache == ZoneType.None)
                    // Drop-off -> Normal
                    return 2;
                else if (_currentPod.Waypoint.InfoTagCache == ZoneType.Dropoff && _currentStorageLocation.InfoTagCache == ZoneType.Cache)
                    // Drop-off -> Cache
                    return 2;
                else if (_currentPod.Waypoint.InfoTagCache == ZoneType.Cache && _currentStorageLocation.InfoTagCache == ZoneType.None)
                    // Cache -> Normal
                    return 1;
                else if (_currentPod.Waypoint.InfoTagCache == ZoneType.None && _currentStorageLocation.InfoTagCache == ZoneType.Cache)
                    // Normal -> Cache
                    return 1;
                else
                    throw new InvalidOperationException("Forbidden move: " + _currentPod.Waypoint.InfoTagCache + " -> " + _currentStorageLocation.InfoTagCache);
            });
            // Then prefer moves keeping cache at targeted level
            scorers.Add(() =>
            {
                // Move types should now be separated, i.e. if there is a drop-off clearing move there is no 'normal' move, respectively, if there is no drop-off clearing move there are only 'normal' moves
                if (_currentPod.Waypoint.InfoTagCache == ZoneType.Cache && _currentStorageLocation.InfoTagCache == ZoneType.None)
                    // Cache -> Normal
                    return
                        // Current cache level minus
                        (double)_cachePodCounts[_storageLocationStations[_currentPod.Waypoint]] / Instance.SharedControlElements.StoragePartitioner.CachePartitions[_storageLocationStations[_currentPod.Waypoint]].Count -
                        // targeted cache level
                        _config.TargetFillCache;
                else if (_currentPod.Waypoint.InfoTagCache == ZoneType.None && _currentStorageLocation.InfoTagCache == ZoneType.Cache)
                    // Normal -> Cache
                    return
                        // Targeted cache level minus
                        _config.TargetFillCache -
                        // current cache level
                        (double)_cachePodCounts[_storageLocationStations[_currentStorageLocation]] / Instance.SharedControlElements.StoragePartitioner.CachePartitions[_storageLocationStations[_currentStorageLocation]].Count;
                else if (_currentPod.Waypoint.InfoTagCache == ZoneType.Dropoff && _currentStorageLocation.InfoTagCache == ZoneType.None)
                    // Drop-off -> Normal
                    return
                        _cacheableAtAll ? 0 : 1;
                else if (_currentPod.Waypoint.InfoTagCache == ZoneType.Dropoff && _currentStorageLocation.InfoTagCache == ZoneType.Cache)
                    // Drop-off -> Cache
                    return
                        _cacheableInfoPerStation[_storageLocationStations[_currentStorageLocation]] ? 1 : 0;
                else
                    throw new InvalidOperationException("Forbidden move: " + _currentPod.Waypoint.InfoTagCache + " -> " + _currentStorageLocation.InfoTagCache);
            });
            // Then prefer moves that make the greatest contribution in keeping the cache hot (depending on settings)
            scorers.Add(() =>
            {
                if (_currentPod.Waypoint.InfoTagCache == ZoneType.Cache) // Cache clearing move
                    return 1 - Instance.ElementMetaInfoTracker.GetPodCombinedScore(_currentPod, _config.WeightSpeed, _config.WeightUtility);
                else if (_currentStorageLocation.InfoTagCache == ZoneType.Cache) // Cache filling move
                    return Instance.ElementMetaInfoTracker.GetPodCombinedScore(_currentPod, _config.WeightSpeed, _config.WeightUtility);
                else
                    return 0; // Drop-off clearing -> highest priority
            });
            // Then prefer the shortest moves
            scorers.Add(() =>
            {
                switch (_config.PodDisposeRule)
                {
                    case CacheStorageLocationSelectionRule.Euclid: return -Distances.CalculateEuclid(_currentPod.Waypoint, _currentStorageLocation, Instance.WrongTierPenaltyDistance);
                    case CacheStorageLocationSelectionRule.Manhattan: return -Distances.CalculateManhattan(_currentPod.Waypoint, _currentStorageLocation, Instance.WrongTierPenaltyDistance);
                    case CacheStorageLocationSelectionRule.ShortestPath: return -Distances.CalculateShortestPathPodSafe(_currentPod.Waypoint, _currentStorageLocation, Instance);
                    case CacheStorageLocationSelectionRule.ShortestTime: return -Distances.CalculateShortestTimePathPodSafe(_currentPod.Waypoint, _currentStorageLocation, Instance);
                    default: throw new ArgumentException("Unknown pod dispose rule: " + _config.PodDisposeRule);
                }
            });
            // Then use randomness
            scorers.Add(() =>
            {
                return Instance.Randomizer.NextDouble();
            });
            // Instantiate best candidate assessment
            _bestCandidateSelector = new BestCandidateSelector(true, scorers.ToArray());
        }

        private void PrepareAssessment()
        {
            // Update current pod count of all caches
            foreach (var station in Instance.OutputStations)
                _cachePodCounts[station] = Instance.SharedControlElements.StoragePartitioner.CachePartitions[station].Count(wp => wp.Pod != null);
        }

        private bool IsValidMove(Pod podToMove, Waypoint newStorageLocation)
        {
            if (// Forbid moves within the same zone type
                podToMove.Waypoint.InfoTagCache == newStorageLocation.InfoTagCache ||
                // Forbid moves to a drop-off storage location
                newStorageLocation.InfoTagCache == ZoneType.Dropoff)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Decides the next repositioning move to do for the given robot.
        /// </summary>
        /// <param name="robot">The robot that is asking to conduct such a move.</param>
        /// <returns>A repositioning move or <code>null</code> if no such move was available.</returns>
        protected override RepositioningMove GetRepositioningMove(Bot robot)
        {
            // Ensure init
            if (_bestCandidateSelector == null)
                Init();
            // Prepare some meta info
            PrepareAssessment();
            // Init
            Pod bestPod = null;
            Waypoint bestStorageLocation = null;
            // Clear potential old results
            _bestCandidateSelector.Recycle();
            // Search for best move
            foreach (var pod in Instance.ResourceManager.UnusedPods)
            {
                // Update current candidate to assess
                _currentPod = pod;

                // If the pod is stored at a drop-off location, we need information about its cache potential
                if (pod.Waypoint.InfoTagCache == ZoneType.Dropoff)
                {
                    // Update cacheable info
                    foreach (var station in Instance.OutputStations)
                    {
                        _cacheableInfoPerStation[station] =
                            // Determine cacheable
                            Instance.SharedControlElements.StoragePartitioner.GetCacheValue(pod, station, _config.WeightSpeed, _config.WeightUtility) > _config.PodCacheableThreshold;
                    }
                    _cacheableAtAll = _cacheableInfoPerStation.Any();
                }

                // Assess all valid pod destination combinations
                foreach (var storageLocation in Instance.ResourceManager.UnusedPodStorageLocations.Where(sl => IsValidMove(pod, sl)))
                {
                    // Update current candidate to assess
                    _currentStorageLocation = storageLocation;
                    // Check whether the current combination is better
                    if (_bestCandidateSelector.Reassess())
                    {
                        // Update best candidate
                        bestPod = _currentPod;
                        bestStorageLocation = _currentStorageLocation;
                    }
                }
            }

            // Check for unavailable or useless move
            if (bestPod != null)
            {
                // Return the move
                return new RepositioningMove() { Pod = bestPod, StorageLocation = bestStorageLocation };
            }
            else
            {
                // No suitable move ... penalize with a timeout
                GlobalTimeout = Instance.Controller.CurrentTime + _config.GlobalTimeout;
                return null;
            }
        }

        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        public override void SignalCurrentTime(double currentTime) { /* Nothing to do, because this manager is always ready */ }
    }
}
