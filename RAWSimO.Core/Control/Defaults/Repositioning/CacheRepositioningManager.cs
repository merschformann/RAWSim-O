using RAWSimO.Core.Configurations;
using RAWSimO.Core.Control.Shared;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Metrics;
using RAWSimO.Core.Waypoints;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.Repositioning
{
    /// <summary>
    /// A repositioning manager aiming to store useful pods in a cache zone while freeing up space in front of the output-stations.
    /// </summary>
    public class CacheRepositioningManager : RepositioningManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public CacheRepositioningManager(Instance instance) : base(instance) { Instance = instance; _config = instance.ControllerConfig.RepositioningConfig as CacheRepositioningConfiguration; }
        /// <summary>
        /// The configuration.
        /// </summary>
        private CacheRepositioningConfiguration _config;
        /// <summary>
        /// The caches per station.
        /// </summary>
        private VolatileIDDictionary<OutputStation, HashSet<Waypoint>> _chacheStorageLocations;
        /// <summary>
        /// The stations belonging to the respective storage location's cache.
        /// </summary>
        private Dictionary<Waypoint, OutputStation> _stationsOfStorageLocations = new Dictionary<Waypoint, OutputStation>();
        /// <summary>
        /// All storage locations that do not belong to a hot zone.
        /// </summary>
        private HashSet<Waypoint> _regularStorageLocations;
        /// <summary>
        /// A helper used to determine the best candidate (clear move).
        /// </summary>
        private BestCandidateSelector _bestCandidateSelectorClear = null;
        /// <summary>
        /// A helper used to determine the best candidate (fill move).
        /// </summary>
        private BestCandidateSelector _bestCandidateSelectorFill = null;
        /// <summary>
        /// The current output-station, which cache is being assessed.
        /// </summary>
        private OutputStation _currentStation = null;
        /// <summary>
        /// A helper variable storing the current pod to assess.
        /// </summary>
        private Pod _currentPod = null;
        /// <summary>
        /// A helper variable storing the current waypoint to assess.
        /// </summary>
        private Waypoint _currentStorageLocation = null;
        /// <summary>
        /// Checks whether a clearing repositioning move is necessary for the given station.
        /// </summary>
        /// <param name="station">The station to check.</param>
        /// <returns><code>true</code> if clearing repositioning should be done for the station, <code>false</code> otherwise. </returns>
        private bool NeedsClearingRepositioning(OutputStation station)
        {
            return
                // We need to relocate, if the number of used cache storage locations is greater ...
                _chacheStorageLocations[station].Count(w => w.Pod != null) >
                // ... than the number of cache storage locations overall minus the ones to keep free per cache
                _chacheStorageLocations[station].Count - _config.CacheClearing;
        }
        /// <summary>
        /// Checks whether a filling repositioning move is necessary for the given station.
        /// </summary>
        /// <param name="station">The station to check.</param>
        /// <returns><code>true</code> if filling repositioning should be done for the station, <code>false</code> otherwise. </returns>
        private bool NeedsFillingRepositioning(OutputStation station)
        {
            return
                // We need to relocate, if the number of used cache storage locations is smaller ...
                _chacheStorageLocations[station].Count(w => w.Pod != null) <
                // ... than the number of cache storage locations overall minus the ones to keep free per cache
                _chacheStorageLocations[station].Count - _config.CacheClearing;
        }
        /// <summary>
        /// Decides the next repositioning move to do for the given robot.
        /// </summary>
        /// <param name="robot">The robot that is asking to conduct such a move.</param>
        /// <returns>A repositioning move or <code>null</code> if no such move was available.</returns>
        protected override RepositioningMove GetRepositioningMove(Bot robot)
        {
            // Prepare hot zones
            if (_chacheStorageLocations == null)
            {
                // Ensure valid zones
                Instance.SharedControlElements.StoragePartitioner.CreateOrEnsureZones(_config.ZoningConfiguration);
                // Store zones for fast access
                _chacheStorageLocations = Instance.SharedControlElements.StoragePartitioner.CachePartitions;
                foreach (var station in _chacheStorageLocations.Keys)
                    foreach (var storageLocation in _chacheStorageLocations[station])
                        _stationsOfStorageLocations[storageLocation] = station;
                _regularStorageLocations = Instance.Waypoints.Except(_chacheStorageLocations.SelectMany(c => c.Value)).ToHashSet();
            }
            // Init
            if (_bestCandidateSelectorClear == null)
            {
                _bestCandidateSelectorClear = new BestCandidateSelector(false,
                    // First try to keep the move on the same tier as the robot
                    () => { return _currentPod.Tier == robot.Tier && _currentStorageLocation.Tier == robot.Tier ? 0 : 1; },
                    // Then try to find a pod useless for the station
                    () =>
                    {
                        // Check the number of potential picks possible with the pod (given by station orders' demand)
                        int potentialPicks = Instance.ResourceManager.GetExtractRequestsOfStation(_currentStation)
                            .Concat(Instance.ResourceManager.GetQueuedExtractRequestsOfStation(_currentStation))
                            .GroupBy(r => r.Item).Sum(g => Math.Min(_currentPod.CountContained(g.Key), g.Count()));
                        // Use negative potential picks to mark useless pod
                        return potentialPicks;
                    },
                    // Then try to find a pod useless overall
                    () =>
                    {
                        // Check the number of potential picks possible with the pod (given by all orders' demand)
                        int potentialPicks = _currentPod.ItemDescriptionsContained.Sum(i => Math.Min(_currentPod.CountContained(i),
                                Instance.ResourceManager.GetDemandAssigned(i) +
                                Instance.ResourceManager.GetDemandQueued(i) +
                                (_config.UselessConsiderBacklog ? Instance.ResourceManager.GetDemandBacklog(i) : 0)));
                        // Use negative potential picks to mark useless pod
                        return potentialPicks;
                    },
                    // Then try to use an empty pod
                    () => { return _currentPod.CapacityInUse; },
                    // Then try to get a destination location most near to the input-stations (if pod is considered empty) or the shortest move distance (if pod still has sufficient content)
                    () =>
                    {
                        return (_currentPod.CapacityInUse / _currentPod.Capacity < _config.PodEmptyThreshold) ?
                            _currentStorageLocation.ShortestPodPathDistanceToNextInputStation :
                            Distances.CalculateShortestPathPodSafe(_currentPod.Waypoint, _currentStorageLocation, Instance);
                    },
                    // Then try to make a move with the pod most near to an output-station
                    () => { return _currentPod.Waypoint.ShortestPodPathDistanceToNextOutputStation; });
            }
            if (_bestCandidateSelectorFill == null)
            {
                _bestCandidateSelectorFill = new BestCandidateSelector(false,
                    // First try to keep the move on the same tier as the robot
                    () => { return _currentPod.Tier == robot.Tier && _currentStorageLocation.Tier == robot.Tier ? 0 : 1; },
                    // Then try to find a pod useful for the station
                    () =>
                    {
                        // Check the number of potential picks possible with the pod (given by station orders' demand)
                        int potentialPicks = Instance.ResourceManager.GetExtractRequestsOfStation(_currentStation)
                            .Concat(Instance.ResourceManager.GetQueuedExtractRequestsOfStation(_currentStation))
                            .GroupBy(r => r.Item).Sum(g => Math.Min(_currentPod.CountContained(g.Key), g.Count()));
                        // Use negative potential picks to mark useless pod
                        return -potentialPicks;
                    },
                    // Then try to find a pod useful overall
                    () =>
                    {
                        // Check the number of potential picks possible with the pod (given by all orders' demand)
                        int potentialPicks = _currentPod.ItemDescriptionsContained.Sum(i => Math.Min(_currentPod.CountContained(i),
                                Instance.ResourceManager.GetDemandAssigned(i) +
                                Instance.ResourceManager.GetDemandQueued(i) +
                                (_config.UselessConsiderBacklog ? Instance.ResourceManager.GetDemandBacklog(i) : 0)));
                        // Use negative potential picks to mark useless pod
                        return -potentialPicks;
                    },
                    // Then try to use a full pod
                    () => { return -_currentPod.CapacityInUse; },
                    // Then try to do a short move
                    () => { return Distances.CalculateShortestPathPodSafe(_currentPod.Waypoint, _currentStorageLocation, Instance); });
            }

            // Init
            Pod bestPod = null;
            Waypoint bestStorageLocation = null;

            // Check whether any cache has too many pods
            if (Instance.OutputStations.Any(s => NeedsClearingRepositioning(s)))
            {
                // Clear potential old results
                _bestCandidateSelectorClear.Recycle();
                // Check all stations
                foreach (var station in Instance.OutputStations.Where(s => NeedsClearingRepositioning(s)))
                {
                    // Update current candidate to assess
                    _currentStation = station;
                    // Check occupied storage locations of cache of station
                    foreach (var from in _chacheStorageLocations[station].Where(w => w.Pod != null && !Instance.ResourceManager.IsPodClaimed(w.Pod)))
                    {
                        // Check unoccupied storage locations not in a cache
                        foreach (var to in Instance.ResourceManager.UnusedPodStorageLocations.Where(w => _regularStorageLocations.Contains(w)))
                        {
                            // Update current candidate to assess
                            _currentPod = from.Pod;
                            _currentStorageLocation = to;
                            // Check whether the current combination is better
                            if (_bestCandidateSelectorClear.Reassess())
                            {
                                // Update best candidate
                                bestPod = _currentPod;
                                bestStorageLocation = _currentStorageLocation;
                            }
                        }
                    }
                }
            }
            // Check whether any cache has too few pods
            if (Instance.OutputStations.Any(s => NeedsFillingRepositioning(s)))
            {
                // Clear potential old results
                _bestCandidateSelectorFill.Recycle();
                // Check all stations
                foreach (var station in Instance.OutputStations.Where(s => NeedsFillingRepositioning(s)))
                {
                    // Update current candidate to assess
                    _currentStation = station;
                    // Check unused pods
                    foreach (var pod in Instance.ResourceManager.UnusedPods.Where(p => _regularStorageLocations.Contains(p.Waypoint)))
                    {
                        // Check unoccupied storage locations of cache of station
                        foreach (var to in _chacheStorageLocations[station].Where(w => !Instance.ResourceManager.IsStorageLocationClaimed(w)))
                        {
                            // Update current candidate to assess
                            _currentPod = pod;
                            _currentStorageLocation = to;
                            // Check whether the current combination is better
                            if (_bestCandidateSelectorFill.Reassess())
                            {
                                // Update best candidate
                                bestPod = _currentPod;
                                bestStorageLocation = _currentStorageLocation;
                            }
                        }
                    }
                }
            }

            // Check whether a move was obtained
            if (bestPod != null)
            {
                // Return the move
                return new RepositioningMove() { Pod = bestPod, StorageLocation = bestStorageLocation };
            }
            else
            {
                // No move available - block calls for a while
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
