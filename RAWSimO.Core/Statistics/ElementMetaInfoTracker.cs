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

namespace RAWSimO.Core.Statistics
{
    /// <summary>
    /// Tracks which pods contain which items.
    /// </summary>
    public class ElementMetaInfoTracker
    {
        /// <summary>
        /// Creates a new instance of this tracker.
        /// </summary>
        /// <param name="instance">The instance to track.</param>
        public ElementMetaInfoTracker(Instance instance)
        {
            _instance = instance;
            // Register callbacks
            _instance.BundleStored += BundleStored;
            _instance.ItemExtracted += ItemExtracted;
            _instance.PodItemReserved += PodItemReserved;
            _instance.PodItemUnreserved += PodItemUnreserved;
            _instance.NewOrder += NewOrder;
        }

        /// <summary>
        /// The instance that is being tracked.
        /// </summary>
        private Instance _instance;

        /// <summary>
        /// All storage locations ordered by their prominence.
        /// </summary>
        public IReadOnlyList<Waypoint> StorageLocationsOrdered { get; private set; }
        /// <summary>
        /// All storage locations per rank.
        /// </summary>
        private Dictionary<int, List<Waypoint>> _storageLocationsPerRank;
        /// <summary>
        /// The index values of the storage locations for fast access.
        /// </summary>
        private VolatileIDDictionary<Waypoint, int> _storageLocationIndeces;
        /// <summary>
        /// The ranks of the different storage locations.
        /// </summary>
        private VolatileIDDictionary<Waypoint, int> _storageLocationRanks;
        /// <summary>
        /// The maximal storage location rank.
        /// </summary>
        public int StorageLocationRankMax { get; private set; }
        /// <summary>
        /// The prominence values of all storage locations for quick access.
        /// </summary>
        private VolatileIDDictionary<Waypoint, double> _storageLocationProminence;

        /// <summary>
        /// Stores all pods containing the respective item.
        /// </summary>
        private VolatileIDDictionary<ItemDescription, HashSet<Pod>> _podsContainingItems;
        /// <summary>
        /// Stores all pods having the respective item available.
        /// </summary>
        private VolatileIDDictionary<ItemDescription, HashSet<Pod>> _podsAvailableItems;

        /// <summary>
        /// Utility scores of all pods.
        /// </summary>
        private VolatileIDDictionary<Pod, double> _podUtility;
        /// <summary>
        /// The pod that currently has the maximal utility value.
        /// </summary>
        private Pod _podUtilityMaxPod;
        /// <summary>
        /// The current maximum pod utility.
        /// </summary>
        public double PodUtilityMax { get; private set; }
        /// <summary>
        /// Speed scores of all pods.
        /// </summary>
        private VolatileIDDictionary<Pod, double> _podSpeed;
        /// <summary>
        /// The pod that currently has the maximal speed value.
        /// </summary>
        private Pod _podSpeedMaxPod;
        /// <summary>
        /// The current maximum pod speed.
        /// </summary>
        public double PodSpeedMax { get; private set; }

        /// <summary>
        /// Initializes this tracker.
        /// </summary>
        private void EnsureInit()
        {
            if (_podsContainingItems == null)
            {
                // --> Init storage location info
                _storageLocationProminence = new VolatileIDDictionary<Waypoint, double>(_instance.Waypoints
                        .Where(w => w.PodStorageLocation)
                        // Determine pod prominence score
                        .Select(w => new VolatileKeyValuePair<Waypoint, double>(w, _instance.OutputStations.Min(s => { return Distances.CalculateShortestTimePathPodSafe(w, s.Waypoint, _instance); })))
                        .ToList());
                StorageLocationsOrdered = _storageLocationProminence
                    // Order storage locations by their prominence
                    .OrderBy(kvp => kvp.Value)
                    // Break ties randomly
                    .ThenBy(kvp => _instance.Randomizer.NextDouble())
                    // Select the actual locations and build a list
                    .Select(kvp => kvp.Key).ToList();
                // Store prominence index
                _storageLocationIndeces = new VolatileIDDictionary<Waypoint, int>(StorageLocationsOrdered.Select(w => new VolatileKeyValuePair<Waypoint, int>(w, 0)).ToList());
                for (int i = 0; i < StorageLocationsOrdered.Count; i++)
                    _storageLocationIndeces[StorageLocationsOrdered[i]] = i;
                // Determine prominence ranks
                _storageLocationRanks = new VolatileIDDictionary<Waypoint, int>(StorageLocationsOrdered.Select(w => new VolatileKeyValuePair<Waypoint, int>(w, 0)).ToList());
                int currentRank = 1; double currentProminenceValue = _storageLocationProminence[StorageLocationsOrdered.First()];
                foreach (var storageLocation in StorageLocationsOrdered)
                {
                    // Update rank, if required
                    if (_storageLocationProminence[storageLocation] > currentProminenceValue)
                    {
                        currentRank++;
                        currentProminenceValue = _storageLocationProminence[storageLocation];
                    }
                    // Set rank of storage location
                    _storageLocationRanks[storageLocation] = currentRank;
                }
                _storageLocationsPerRank = _storageLocationRanks.GroupBy(kvp => kvp.Value).ToDictionary(k => k.Key, v => v.Select(kvp => kvp.Key).OrderBy(kvp => _instance.Randomizer.NextDouble()).ToList());
                StorageLocationRankMax = _storageLocationRanks.Values.Max();
                // Store prominence ranks for statistics tracking
                foreach (var w in StorageLocationsOrdered)
                    w.InfoTagProminence = 1.0 - ((_storageLocationRanks[w] - 1.0) / (StorageLocationRankMax - 1.0));
                // --> Init pod score values
                _podsContainingItems = new VolatileIDDictionary<ItemDescription, HashSet<Pod>>(
                _instance.ItemDescriptions.Select(i => new VolatileKeyValuePair<ItemDescription, HashSet<Pod>>(i, _instance.Pods.Where(p => p.IsContained(i)).ToHashSet())).ToList());
                _podsAvailableItems = new VolatileIDDictionary<ItemDescription, HashSet<Pod>>(
                    _instance.ItemDescriptions.Select(i => new VolatileKeyValuePair<ItemDescription, HashSet<Pod>>(i, _instance.Pods.Where(p => p.IsAvailable(i)).ToHashSet())).ToList());
                // Determine initial pod utility
                _podUtility = new VolatileIDDictionary<Pod, double>(
                    _instance.Pods.Select(p => new VolatileKeyValuePair<Pod, double>(p, p.ItemDescriptionsContained.Sum(i => Math.Min(p.CountContained(i), _instance.StockInfo.GetCurrentDemand(i)))))
                    .ToList());
                VolatileKeyValuePair<Pod, double> bestUtility = _podUtility.ArgMax(pu => pu.Value);
                _podUtilityMaxPod = bestUtility.Key;
                PodUtilityMax = bestUtility.Value;
                // Determine initial pod speed
                _podSpeed = new VolatileIDDictionary<Pod, double>(
                    _instance.Pods.Select(p => new VolatileKeyValuePair<Pod, double>(p, p.ItemDescriptionsContained.Sum(i => p.CountContained(i) * _instance.FrequencyTracker.GetStaticFrequency(i))))
                    .ToList());
                VolatileKeyValuePair<Pod, double> bestSpeed = _podSpeed.ArgMax(ps => ps.Value);
                _podSpeedMaxPod = bestSpeed.Key;
                PodSpeedMax = bestSpeed.Value;
            }
        }

        /// <summary>
        /// Returns all pods containing the given item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>All pods containing the item.</returns>
        public IEnumerable<Pod> GetPodsContaining(ItemDescription item)
        {
            // Make sure tracking is enabled
            if (!_instance.SettingConfig.MonitorWellSortedness)
                throw new InvalidOperationException("Wellsortedness tracking is disabled!");
            // Ensure init
            EnsureInit();
            return _podsContainingItems[item];
        }
        /// <summary>
        /// Returns all pods offering the given item (having at least one available).
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>All pods offering the item.</returns>
        public IEnumerable<Pod> GetPodsOffering(ItemDescription item)
        {
            // Make sure tracking is enabled
            if (!_instance.SettingConfig.MonitorWellSortedness)
                throw new InvalidOperationException("Wellsortedness tracking is disabled!");
            // Ensure init
            EnsureInit();
            return _podsAvailableItems[item];
        }
        /// <summary>
        /// Returns the prominence index of the given storage location.
        /// </summary>
        /// <param name="waypoint">The storage location to lookup.</param>
        /// <returns>The index of the storage prominence location.</returns>
        public int GetStorageLocationIndex(Waypoint waypoint)
        {
            // Make sure tracking is enabled
            if (!_instance.SettingConfig.MonitorWellSortedness)
                throw new InvalidOperationException("Wellsortedness tracking is disabled!");
            // Ensure init
            EnsureInit();
            return _storageLocationIndeces[waypoint];
        }
        /// <summary>
        /// Returns the prominence rank of the given storage location.
        /// </summary>
        /// <param name="waypoint">The storage location.</param>
        /// <returns>The prominence rank of the storage location.</returns>
        public int GetStorageLocationRank(Waypoint waypoint)
        {
            // Make sure tracking is enabled
            if (!_instance.SettingConfig.MonitorWellSortedness)
                throw new InvalidOperationException("Wellsortedness tracking is disabled!");
            // Ensure init
            EnsureInit();
            return _storageLocationRanks[waypoint];
        }
        /// <summary>
        /// Returns all storage locations of the given rank.
        /// </summary>
        /// <param name="rank">The rank to retrieve.</param>
        /// <returns>All storage locations of the given rank. Or an empty enumeration if no storage locations of the rank are existing.</returns>
        public IEnumerable<Waypoint> GetStorageLocationsOfRank(int rank)
        {
            // Make sure tracking is enabled
            if (!_instance.SettingConfig.MonitorWellSortedness)
                throw new InvalidOperationException("Wellsortedness tracking is disabled!");
            // Ensure init
            EnsureInit();
            return _storageLocationsPerRank.ContainsKey(rank) ? _storageLocationsPerRank[rank] : Enumerable.Empty<Waypoint>();
        }
        /// <summary>
        /// Returns the current utility value for the given pod.
        /// </summary>
        /// <param name="pod">The pod.</param>
        /// <returns>The utility of the pod.</returns>
        public double GetPodUtility(Pod pod)
        {
            // Make sure tracking is enabled
            if (!_instance.SettingConfig.MonitorWellSortedness)
                return 0;
            // Ensure init
            EnsureInit();
            return _podUtility[pod];
        }
        /// <summary>
        /// Returns the current speed value for the given pod.
        /// </summary>
        /// <param name="pod">The pod.</param>
        /// <returns>The speed of the pod.</returns>
        public double GetPodSpeed(Pod pod)
        {
            // Make sure tracking is enabled
            if (!_instance.SettingConfig.MonitorWellSortedness)
                return 0;
            // Ensure init
            EnsureInit();
            return _podSpeed[pod];
        }
        /// <summary>
        /// Returns the current combined score value for the given pod.
        /// </summary>
        /// <param name="pod">The pod.</param>
        /// <param name="weightSpeed">The weight of the speed value.</param>
        /// <param name="weightUtility">The weight of the utility value.</param>
        /// <returns>The combined speed and utility score according to the given weights. This always is a value between 0 and 1.</returns>
        public double GetPodCombinedScore(Pod pod, double weightSpeed, double weightUtility)
        {
            // Make sure tracking is enabled
            if (!_instance.SettingConfig.MonitorWellSortedness)
                return 0;
            // Ensure init
            EnsureInit();
            double combinedWeight = weightSpeed + weightUtility;
            double normalizedPodSpeed = PodSpeedMax > 0 ? _podSpeed[pod] / PodSpeedMax : 0;
            double normalizedPodUtility = PodUtilityMax > 0 ? _podUtility[pod] / PodUtilityMax : 0;
            return
                // First calculate normalized speed score and weigh it to then add it to ...
                (weightSpeed / combinedWeight) * (normalizedPodSpeed) +
                // the normalized and weighted utility score
                (weightUtility / combinedWeight) * (normalizedPodUtility);
        }
        /// <summary>
        /// Counts the number of inversions for the pods currently stored at the storage locations.
        /// </summary>
        /// <param name="invCombinedTotal">Number of total inversions according to combined score.</param>
        /// <param name="invCombinedRank">Total rank offsets of all inversions according to combined score.</param>
        /// <param name="invCombinedAvgRank">Average rank offset of all inversions according to combined score.</param>
        /// <param name="invSpeedTotal">Number of total inversions according to speed score.</param>
        /// <param name="invSpeedRank">Total rank offsets of all inversions according to speed score.</param>
        /// <param name="invSpeedAvgRank">Average rank offset of all inversions according to speed score.</param>
        /// <param name="invUtilityTotal">Number of total inversions according to utility score.</param>
        /// <param name="invUtilityRank">Total rank offsets of all inversions according to utility score.</param>
        /// <param name="invUtilityAvgRank">Average rank offset of all inversions according to utility score.</param>
        public void CountInversions(
            out int invCombinedTotal, out int invCombinedRank, out double invCombinedAvgRank,
            out int invSpeedTotal, out int invSpeedRank, out double invSpeedAvgRank,
            out int invUtilityTotal, out int invUtilityRank, out double invUtilityAvgRank)
        {
            // Make sure tracking is enabled
            if (!_instance.SettingConfig.MonitorWellSortedness)
            {
                invCombinedTotal = 0; invCombinedRank = 0; invCombinedAvgRank = 0;
                invSpeedTotal = 0; invSpeedRank = 0; invSpeedAvgRank = 0;
                invUtilityTotal = 0; invUtilityRank = 0; invUtilityAvgRank = 0;
                return;
            }
            // Ensure init
            EnsureInit();
            // Init
            invCombinedTotal = 0; invCombinedRank = 0;
            invSpeedTotal = 0; invSpeedRank = 0;
            invUtilityTotal = 0; invUtilityRank = 0;
            // Count number of inversions
            for (int index1 = 0; index1 < StorageLocationsOrdered.Count; index1++)
            {
                for (int index2 = index1 + 1; index2 < StorageLocationsOrdered.Count; index2++)
                {
                    if (// Check whether pods are located at both storage locations
                        StorageLocationsOrdered[index1].Pod != null && StorageLocationsOrdered[index2].Pod != null &&
                        // Ignore storage locations of the same rank
                        _storageLocationRanks[StorageLocationsOrdered[index1]] != _storageLocationRanks[StorageLocationsOrdered[index2]])
                    {
                        // Check for combined score inversions
                        if (GetPodCombinedScore(StorageLocationsOrdered[index1].Pod, 1, 1) < GetPodCombinedScore(StorageLocationsOrdered[index2].Pod, 1, 1))
                        {
                            invCombinedTotal++;
                            invCombinedRank += _storageLocationRanks[StorageLocationsOrdered[index2]] - _storageLocationRanks[StorageLocationsOrdered[index1]];
                        }
                        // Check for speed inversions
                        if (GetPodSpeed(StorageLocationsOrdered[index1].Pod) < GetPodSpeed(StorageLocationsOrdered[index2].Pod))
                        {
                            invSpeedTotal++;
                            invSpeedRank += _storageLocationRanks[StorageLocationsOrdered[index2]] - _storageLocationRanks[StorageLocationsOrdered[index1]];
                        }
                        // Check for utility inversions
                        if (GetPodUtility(StorageLocationsOrdered[index1].Pod) < GetPodUtility(StorageLocationsOrdered[index2].Pod))
                        {
                            invUtilityTotal++;
                            invUtilityRank += _storageLocationRanks[StorageLocationsOrdered[index2]] - _storageLocationRanks[StorageLocationsOrdered[index1]];
                        }
                    }
                }
            }
            // Calculate averages
            invCombinedAvgRank = (double)invCombinedRank / invCombinedTotal;
            invSpeedAvgRank = (double)invSpeedRank / invSpeedTotal;
            invUtilityAvgRank = (double)invUtilityRank / invUtilityTotal;
        }

        #region Callbacks

        private void BundleStored(InputStation iStation, Bot bot, Pod pod, ItemBundle bundle)
        {
            // Skip callback, if inactive
            if (!_instance.SettingConfig.MonitorWellSortedness)
                return;
            // Return if not in use yet
            if (_podsContainingItems == null) return;
            // --> Add pod to the list of pods containing / offering the respective item
            _podsAvailableItems[bundle.ItemDescription].Add(pod);
            _podsContainingItems[bundle.ItemDescription].Add(pod);
            // --> Update pod utility
            int itemDemand = _instance.StockInfo.GetCurrentDemand(bundle.ItemDescription);
            int beforeCount = pod.CountContained(bundle.ItemDescription) - bundle.ItemCount;
            // Add either rest of demand that now can be supplied by the pod or simply the content of the bundle
            if (beforeCount < itemDemand)
            {
                _podUtility[pod] += Math.Min(itemDemand - beforeCount, bundle.ItemCount);
                // Update max value
                if (_podUtility[pod] > PodUtilityMax)
                {
                    PodUtilityMax = _podUtility[pod];
                    _podUtilityMaxPod = pod;
                }
            }
            // --> Update pod speed
            _podSpeed[pod] += bundle.ItemCount * _instance.FrequencyTracker.GetStaticFrequency(bundle.ItemDescription);
            if (_podSpeed[pod] > PodSpeedMax)
            {
                PodSpeedMax = _podSpeed[pod];
                _podSpeedMaxPod = pod;
            }
        }
        private void ItemExtracted(Pod pod, ItemDescription item)
        {
            // Skip callback, if inactive
            if (!_instance.SettingConfig.MonitorWellSortedness)
                return;
            // Return if not in use yet
            if (_podsContainingItems == null) return;
            // --> Update pod utility
            int itemDemand = _instance.StockInfo.GetCurrentDemand(item);
            bool updateUtilityMax = false;
            // Update demand by new content of pod (pod lost one of these items)
            if (itemDemand >= pod.CountContained(item))
            {
                // The demand for this item is still high, but we can now supply one item less (because the content is low in comparison to the demand)
                _podUtility[pod]--;
                // Check whether utility max has to be updated
                if (pod == _podUtilityMaxPod)
                    updateUtilityMax = true;
            }
            // Update to new demand (demand for the given item sunk by one)
            foreach (var itemPod in _podsContainingItems[item])
            {
                // If the demand for the item is less then the pod content, we need to update to the new demand by decreasing the utility by one
                if (itemDemand < itemPod.CountContained(item))
                {
                    _podUtility[itemPod]--;
                    // Check whether utility max has to be updated
                    if (itemPod == _podUtilityMaxPod)
                        updateUtilityMax = true;
                }
            }
            // Refresh new max
            if (updateUtilityMax)
            {
                VolatileKeyValuePair<Pod, double> bestUtility = _podUtility.ArgMax(pu => pu.Value);
                _podUtilityMaxPod = bestUtility.Key;
                PodUtilityMax = bestUtility.Value;
            }
            // --> Remove pod from list of pods containing / offering the item, if it was the last one
            if (!pod.IsAvailable(item))
                _podsAvailableItems[item].Remove(pod);
            if (!pod.IsContained(item))
                _podsContainingItems[item].Remove(pod);
            // --> Update pod speed
            _podSpeed[pod] -= _instance.FrequencyTracker.GetStaticFrequency(item);
            if (pod == _podSpeedMaxPod)
            {
                // Refresh max value
                VolatileKeyValuePair<Pod, double> bestSpeed = _podSpeed.ArgMax(ps => ps.Value);
                _podSpeedMaxPod = bestSpeed.Key;
                PodSpeedMax = bestSpeed.Value;
            }
        }
        private void PodItemUnreserved(Pod pod, ItemDescription item, Management.ExtractRequest request)
        {
            // Skip callback, if inactive
            if (!_instance.SettingConfig.MonitorWellSortedness)
                return;
            // Return if not in use yet
            if (_podsContainingItems == null) return;
            // --> Add pod to the list of pods offering the respective item
            _podsAvailableItems[item].Add(pod);
        }
        private void PodItemReserved(Pod pod, ItemDescription item, Management.ExtractRequest request)
        {
            // Skip callback, if inactive
            if (!_instance.SettingConfig.MonitorWellSortedness)
                return;
            // Return if not in use yet
            if (_podsContainingItems == null) return;
            // --> Remove pod from list of pods offering the item, if it was the last one
            if (!pod.IsAvailable(item))
                _podsAvailableItems[item].Remove(pod);
        }
        private void NewOrder(Order order)
        {
            // Skip callback, if inactive
            if (!_instance.SettingConfig.MonitorWellSortedness)
                return;
            // Return if not in use yet
            if (_podsContainingItems == null) return;
            // --> Update pod utility
            foreach (var line in order.Positions)
            {
                int itemDemand = _instance.StockInfo.GetCurrentDemand(line.Key);
                foreach (var itemPod in _podsContainingItems[line.Key])
                {
                    int beforeDemand = itemDemand - line.Value;
                    int contained = itemPod.CountContained(line.Key);
                    // Add either complete new demand (if pod has sufficient items of that kind) or add the remaining content to the utility
                    if (beforeDemand < contained)
                    {
                        _podUtility[itemPod] += Math.Min(contained - beforeDemand, line.Value);
                        // Update max value
                        if (_podUtility[itemPod] > PodUtilityMax)
                        {
                            PodUtilityMax = _podUtility[itemPod];
                            _podUtilityMaxPod = itemPod;
                        }
                    }
                }
            }
        }

        #endregion
    }
}
