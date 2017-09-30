using RAWSimO.Core.Configurations;
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

namespace RAWSimO.Core.Control.Shared
{
    /// <summary>
    /// Distinguishes between different zone types.
    /// </summary>
    public enum ZoneType
    {
        /// <summary>
        /// Does not belong to any particular zone.
        /// </summary>
        None,
        /// <summary>
        /// Belongs to an output-stations cache.
        /// </summary>
        Cache,
        /// <summary>
        /// Belongs to an output-stations drop-off zone.
        /// </summary>
        Dropoff,
    }
    /// <summary>
    /// The priority of the different zones-
    /// </summary>
    public enum ZonePriority
    {
        /// <summary>
        /// Cache is created first, hence, best storage locations belong to cache.
        /// </summary>
        CacheFirst,
        /// <summary>
        /// Drop-off zone is created first, hence, best storage locations belong to the drop-off zone.
        /// </summary>
        DropoffFirst,
        /// <summary>
        /// Cache and drop-off are created with equal priority.
        /// </summary>
        CacheDropoffEqual,
    }
    /// <summary>
    /// Defines by which distance measure the free storage location is selected. This is superimposed by the decision whether a pod should be stored in cache or not.
    /// </summary>
    public enum CacheStorageLocationSelectionRule
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
    /// Exposes functionality to partition storage locations into hot zones / caches per station.
    /// </summary>
    public class StoragePartitioner
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="instance">The instance this partitioner belongs to.</param>
        public StoragePartitioner(Instance instance) { Instance = instance; }
        /// <summary>
        /// The instance this manager belongs to.
        /// </summary>
        private Instance Instance { get; set; }
        /// <summary>
        /// The config to use.
        /// </summary>
        private CacheConfiguration _config;
        /// <summary>
        /// All storage locations not belonging to any zone.
        /// </summary>
        private HashSet<Waypoint> UnzonedStorageLocations;
        /// <summary>
        /// The cache partitions per station.
        /// </summary>
        internal VolatileIDDictionary<OutputStation, HashSet<Waypoint>> CachePartitions;
        /// <summary>
        /// The drop-off zones per station.
        /// </summary>
        internal VolatileIDDictionary<OutputStation, HashSet<Waypoint>> DropoffPartitions;

        /// <summary>
        /// Current storage location to assess.
        /// </summary>
        private Waypoint _currentStorageLocation;
        /// <summary>
        /// Current station to assess.
        /// </summary>
        private OutputStation _currentStation;
        /// <summary>
        /// Current zone type to assess.
        /// </summary>
        private ZoneType _currentZoneType;

        /// <summary>
        /// Determines the value of the pod for the cache of the station, i.e. the fraction of pods currently stored in the cache that are inferior to the given one.
        /// </summary>
        /// <param name="pod">The pod.</param>
        /// <param name="station">The station the pod is coming from.</param>
        /// <param name="weightSpeed">The weight for the pod's speed.</param>
        /// <param name="weightUtility">The weight for the pod's utility.</param>
        /// <returns>Value of the pod for the cache of the given station.</returns>
        internal double GetCacheValue(Pod pod, OutputStation station, double weightSpeed, double weightUtility)
        {
            // If there is no pod in the cache at all, we simply allow the current one
            if (!CachePartitions[station].Any(sl => sl.Pod != null))
                return 1;
            // Determine score of pod
            double score = Instance.ElementMetaInfoTracker.GetPodCombinedScore(pod, weightSpeed, weightUtility);
            // Determine number of pods in cache
            double cachedPods = CachePartitions[station]
                .Where(sl => sl.Pod != null)
                .Count();
            // Count number of pods less useful than the given one
            double inferiorCachedPods = CachePartitions[station]
                .Where(sl => sl.Pod != null)
                .Count(sl => Instance.ElementMetaInfoTracker.GetPodCombinedScore(sl.Pod, weightSpeed, weightUtility) < score);
            // Return inferior fraction
            return inferiorCachedPods / cachedPods;
        }

        /// <summary>
        /// Creates all zones or ensures their consistency between different managers using those.
        /// </summary>
        /// <param name="config">The configuration to use for creating the zones.</param>
        internal void CreateOrEnsureZones(CacheConfiguration config)
        {
            // Check whether this was already done
            if (_config != null)
            {
                // --> Ensure compatibility
                if (!_config.Match(config))
                    throw new ArgumentException("Incompatible cache configurations: " + _config.ToString() + " vs. " + config.ToString());
            }
            else
            {
                // Init
                _config = config;
                List<ZoneType> zoneTypes = new List<ZoneType>() { ZoneType.Cache, ZoneType.Dropoff };
                UnzonedStorageLocations = Instance.Waypoints.Where(w => w.PodStorageLocation).ToHashSet();
                CachePartitions = new VolatileIDDictionary<OutputStation, HashSet<Waypoint>>(Instance.OutputStations.Select(s =>
                    new VolatileKeyValuePair<OutputStation, HashSet<Waypoint>>(s, new HashSet<Waypoint>())).ToList());
                int cacheWPCountPerStation = (int)Math.Ceiling(UnzonedStorageLocations.Count * _config.CacheFraction / Instance.OutputStations.Count);
                DropoffPartitions = new VolatileIDDictionary<OutputStation, HashSet<Waypoint>>(Instance.OutputStations.Select(s =>
                     new VolatileKeyValuePair<OutputStation, HashSet<Waypoint>>(s, new HashSet<Waypoint>())).ToList());
                int dropoffWPCountPerStation = _config.DropoffCount;
                // Init selector
                BestCandidateSelector scorer = new BestCandidateSelector(false,
                    // First adhere to preference between zone types
                    () =>
                    {
                        switch (_config.ZonePriority)
                        {
                            case ZonePriority.CacheFirst: return _currentZoneType == ZoneType.Cache ? 0 : 1;
                            case ZonePriority.DropoffFirst: return _currentZoneType == ZoneType.Dropoff ? 0 : 1;
                            case ZonePriority.CacheDropoffEqual: return 0;
                            default: throw new ArgumentException("Unknown priority: " + _config.ZonePriority);
                        }
                    },
                    // Then assess the main distance metric
                    () =>
                    {
                        switch (_currentZoneType)
                        {
                            case ZoneType.Cache: return Distances.CalculateShortestTimePathPodSafe(_currentStorageLocation, _currentStation.Waypoint, Instance);
                            case ZoneType.Dropoff: return Distances.CalculateShortestTimePathPodSafe(_currentStation.Waypoint, _currentStorageLocation, Instance);
                            default: throw new ArgumentException("Unknown zone type for partitioning: " + _currentZoneType.ToString());
                        }
                    },
                    // Break ties based on the value for the other zone
                    () =>
                    {
                        switch (_currentZoneType)
                        {
                            case ZoneType.Cache: return -Distances.CalculateShortestTimePathPodSafe(_currentStation.Waypoint, _currentStorageLocation, Instance);
                            case ZoneType.Dropoff: return -Distances.CalculateShortestTimePathPodSafe(_currentStorageLocation, _currentStation.Waypoint, Instance);
                            default: throw new ArgumentException("Unknown zone type for partitioning: " + _currentZoneType.ToString());
                        }
                    });

                // --> Create partitions
                // Assign storage locations to different zones
                while ( // Check for any remaining assignments
                    CachePartitions.Values.Any(p => p.Count < cacheWPCountPerStation) ||
                    DropoffPartitions.Values.Any(p => p.Count < dropoffWPCountPerStation))
                {
                    // Search for next assignment
                    scorer.Recycle();
                    OutputStation bestStation = null;
                    Waypoint bestStorageLocation = null;
                    ZoneType bestZoneType = ZoneType.None;
                    // Check all unzoned storage locations
                    foreach (var storageLocation in UnzonedStorageLocations)
                    {
                        _currentStorageLocation = storageLocation;
                        // Check all stations
                        foreach (var station in Instance.OutputStations)
                        {
                            _currentStation = station;
                            // Check all types
                            foreach (var zoneType in zoneTypes)
                            {
                                _currentZoneType = zoneType;
                                // Skip invalid assignments
                                if (zoneType == ZoneType.Cache && CachePartitions[station].Count >= cacheWPCountPerStation)
                                    continue;
                                if (zoneType == ZoneType.Dropoff && DropoffPartitions[station].Count >= dropoffWPCountPerStation)
                                    continue;
                                // Determine score and update assignment
                                if (scorer.Reassess())
                                {
                                    bestStation = _currentStation;
                                    bestStorageLocation = _currentStorageLocation;
                                    bestZoneType = _currentZoneType;
                                }
                            }
                        }
                    }
                    // Sanity check
                    if (bestStation == null)
                        throw new InvalidOperationException("Ran out of available assignments while partitioning the caches - partitions so far: " +
                            "Cache: " + string.Join(",", CachePartitions.Select(p => p.Key.ToString() + "(" + p.Value.Count + ")")) +
                            "Dropoff: " + string.Join(",", DropoffPartitions.Select(p => p.Key.ToString() + "(" + p.Value.Count + ")")));
                    // Set assignment
                    switch (bestZoneType)
                    {
                        case ZoneType.Cache:
                            CachePartitions[bestStation].Add(bestStorageLocation);
                            bestStorageLocation.InfoTagCache = ZoneType.Cache;
                            break;
                        case ZoneType.Dropoff:
                            DropoffPartitions[bestStation].Add(bestStorageLocation);
                            bestStorageLocation.InfoTagCache = ZoneType.Dropoff;
                            break;
                        default: throw new InvalidOperationException("Invalid zone determined: " + bestZoneType);
                    }
                    UnzonedStorageLocations.Remove(bestStorageLocation);
                }
            }
        }
    }
}
