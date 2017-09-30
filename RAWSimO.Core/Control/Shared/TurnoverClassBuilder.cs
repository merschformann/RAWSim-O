using RAWSimO.Core.Configurations;
using RAWSimO.Core.Control.Defaults.ItemStorage;
using RAWSimO.Core.Control.Defaults.PodStorage;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.IO;
using RAWSimO.Core.Items;
using RAWSimO.Core.Metrics;
using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Shared
{
    /// <summary>
    /// Indicates the rule to use to divide storage locations into classes for turnover based mechanisms.
    /// </summary>
    public enum TurnoverPodStorageLocationClassRule
    {
        /// <summary>
        /// Storage locations are assigned to classes by their euclidean distance to the next output station.
        /// </summary>
        OutputStationDistanceEuclidean,
        /// <summary>
        /// Storage locations are assigned to classes by their manhattan distance to the next output station.
        /// </summary>
        OutputStationDistanceManhattan,
        /// <summary>
        /// Storage locations are assigned to classes by the distance of the shortest path to the next output station.
        /// </summary>
        OutputStationDistanceShortestPath,
        /// <summary>
        /// Storage locations are assigned to classes by the time of the most time-efficient path to the next output station.
        /// </summary>
        OutputStationDistanceShortestTime,
    }
    /// <summary>
    /// Decides which storage location of the given class will be used when there are more than one options.
    /// </summary>
    public enum TurnoverPodStorageLocationDisposeRule
    {
        /// <summary>
        /// Uses the nearest (euclidean distance) free storage location of the class.
        /// </summary>
        NearestEuclid,
        /// <summary>
        /// Uses the nearest (manhattan distance) free storage location of the class.
        /// </summary>
        NearestManhattan,
        /// <summary>
        /// Uses the nearest (shortest path by A*) free storage location of the class.
        /// </summary>
        NearestShortestPath,
        /// <summary>
        /// Uses the nearest (most time-efficient path by A*) free storage location of the class.
        /// </summary>
        NearestShortestTime,
        /// <summary>
        /// Uses the free storage location of the class nearest (euclidean distance) to an output station.
        /// </summary>
        OStationNearestEuclid,
        /// <summary>
        /// Uses the free storage location of the class nearest (manhattan distance) to an output station.
        /// </summary>
        OStationNearestManhattan,
        /// <summary>
        /// Uses the free storage location of the class nearest (shortest path) to an output station.
        /// </summary>
        OStationNearestShortestPath,
        /// <summary>
        /// Uses the free storage location of the class nearest (most time-efficient path) to an output station.
        /// </summary>
        OStationNearestShortestTime,
        /// <summary>
        /// Uses a random free storage location of the class.
        /// </summary>
        Random,
    }
    /// <summary>
    /// This class enables management of pods and storage locations in a class based way.
    /// </summary>
    internal class TurnoverClassBuilder : IUpdateable
    {
        /// <summary>
        /// Initializes this class manager.
        /// </summary>
        /// <param name="instance">The active instance.</param>
        public TurnoverClassBuilder(Instance instance) { Instance = instance; instance.NewOrder += NewOrderCallback; }

        /// <summary>
        /// Sets the given class borders or checks their compatibility if there are already class borders present.
        /// </summary>
        /// <param name="classBorders">The class borders to use.</param>
        /// <param name="reallocationDelay">The delay in simulation time after which a reallocation is done according to the currently measured situation.</param>
        /// <param name="reallocationOrderCount">The number of orders after which a reallocation is done according to the currently measured situation.</param>
        public void ParseConfigAndEnsureCompatibility(double[] classBorders, double reallocationDelay, int reallocationOrderCount)
        {
            // Check whether this class builder was already parameterized
            if (_classCount < 0)
            {
                // Parse configuration
                _classBorders = classBorders;
                _classCount = _classBorders.Length;
                _reallocationDelay = reallocationDelay;
                _reallocationOrderCount = reallocationOrderCount;
            }
            else
            {
                // Check compatibility
                if (_classBorders.Length != classBorders.Length ||
                    _reallocationDelay != reallocationDelay ||
                    _reallocationOrderCount != reallocationOrderCount)
                    throw new ArgumentException("Configurations for the two attached managers do not match!");
                for (int i = 0; i < _classBorders.Length; i++)
                    if (_classBorders[i] != classBorders[i])
                        throw new ArgumentException("Configurations for the two attached managers do not match!");
            }
        }

        /// <summary>
        /// The instance this manager belongs to.
        /// </summary>
        private Instance Instance { get; set; }
        /// <summary>
        /// The bundle storage manager associated with this class manager.
        /// </summary>
        private ItemStorageManager BundleStorageManager { get; set; }
        /// <summary>
        /// The pod storage manager associated with this class manager.
        /// </summary>
        private PodStorageManager PodStorageManager { get; set; }
        /// <summary>
        /// Indicates whether this manager was initialized.
        /// </summary>
        private bool _initialized = false;

        /// <summary>
        /// The number of classes this class-based storage manager works with.
        /// </summary>
        private int _classCount = -1;
        /// <summary>
        /// The bounds of the classes in terms of relative item frequencies.
        /// </summary>
        private double[] _classBorders;
        /// <summary>
        /// The rule used to assign the storage locations to the different classes.
        /// </summary>
        private TurnoverPodStorageLocationClassRule _storageLocationClassRule = TurnoverPodStorageLocationClassRule.OutputStationDistanceShortestTime;
        /// <summary>
        /// The delay in simulation time after which a reallocation takes place.
        /// </summary>
        private double _reallocationDelay;
        /// <summary>
        /// The number of orders after which a reallocation takes place.
        /// </summary>
        private int _reallocationOrderCount;

        /// <summary>
        /// The actual pods serving for the different classes.
        /// </summary>
        private List<Pod>[] _classStoragePods;
        /// <summary>
        /// The actual storage locations serving for the different classes.
        /// </summary>
        private List<Waypoint>[] _classStorageLocations;

        /// <summary>
        /// The classes of all pods.
        /// </summary>
        private Dictionary<Pod, int> _podClasses = new Dictionary<Pod, int>();
        /// <summary>
        /// The classes of all item-descriptions.
        /// </summary>
        private Dictionary<ItemDescription, int> _itemClasses;

        /// <summary>
        /// The pods in a frequency sorted manner.
        /// </summary>
        private List<Pod> _podsOrdered;
        /// <summary>
        /// The storage locations in a distance sorted manner.
        /// </summary>
        private List<Waypoint> _storageLocationsOrdered;
        /// <summary>
        /// The item-descriptions in a frequency sorted manner.
        /// </summary>
        private List<ItemDescription> _itemDescriptionsOrdered;

        /// <summary>
        /// The last time the storage classes were restructured to fit the current demand.
        /// </summary>
        private double _lastReallocation;
        /// <summary>
        /// The number of orders since the last reallocation.
        /// </summary>
        private int _orderCountOfPeriod;

        /// <summary>
        /// Returns the number of classes.
        /// </summary>
        public int ClassCount { get { EnsureInit(); return _classCount; } }
        /// <summary>
        /// Returns the class to use for the given bundle.
        /// </summary>
        /// <param name="bundle">The bundle to find the fitting class for.</param>
        /// <returns>The class of the given bundle.</returns>
        public int DetermineStorageClass(ItemBundle bundle) { EnsureInit(); return _itemClasses[bundle.ItemDescription]; }
        /// <summary>
        /// Returns the class to use for the given pod.
        /// </summary>
        /// <param name="pod">The pod.</param>
        /// <returns>The class to use for storing the pod.</returns>
        public int DetermineStorageClass(Pod pod) { EnsureInit(); return _podClasses[pod]; }

        /// <summary>
        /// Returns all storage pods belonging to the specified class.
        /// </summary>
        /// <param name="storageClass">The storage class.</param>
        /// <returns>All pods belonging to the specified class.</returns>
        public List<Pod> GetClassPods(int storageClass) { EnsureInit(); return _classStoragePods[storageClass]; }
        /// <summary>
        /// Returns all storage locations belonging to the specified class.
        /// </summary>
        /// <param name="storageClass">The storage class.</param>
        /// <returns>All storage locations belonging to the specififed class.</returns>
        public List<Waypoint> GetClassStorageLocations(int storageClass) { EnsureInit(); return _classStorageLocations[storageClass]; }

        /// <summary>
        /// This is called every time an order is placed.
        /// </summary>
        /// <param name="order">The new order that was placed.</param>
        private void NewOrderCallback(Order order) { _orderCountOfPeriod++; }

        /// <summary>
        /// The callback for checking whether a reallocation is necessary and conducting it if so.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        public void ReallocateCallback(double currentTime)
        {
            if (// See whether we have to reallocate due to time
                _reallocationDelay > 0 && currentTime - _lastReallocation >= _reallocationDelay ||
                // See whether we have to reallocate due to the number of orders
                _reallocationOrderCount > 0 && _orderCountOfPeriod >= _reallocationOrderCount)
            {
                // Reallocate items and pods for the first time
                ReallocatePods();

                // Reallocate items and pods for the first time
                ReallocateItems();

                // Set back trackers
                _lastReallocation = currentTime;
                _orderCountOfPeriod = 0;
                // Reset the OrderCount of every item
                Instance.FrequencyTracker.ResetModifiableOrderCount();
            }
        }

        /// <summary>
        /// Enables late initialization.
        /// </summary>
        private void EnsureInit()
        {
            if (!_initialized)
                Init();
        }
        /// <summary>
        /// Initializes this manager.
        /// </summary>
        private void Init()
        {
            // Remember initialization
            _initialized = true;
            // Fetch additional settings
            if (Instance.Controller.PodStorageManager is TurnoverPodStorageManager)
                _storageLocationClassRule = (Instance.ControllerConfig.PodStorageConfig as TurnoverPodStorageConfiguration).StorageLocationClassRule;
            // Initialize ordered storage location list
            _storageLocationsOrdered = Instance.Waypoints
                .Where(wp => wp.PodStorageLocation)
                .OrderBy(wp => Instance.OutputStations.Min(o =>
                {
                    double value = 0;
                    switch (_storageLocationClassRule)
                    {
                        case TurnoverPodStorageLocationClassRule.OutputStationDistanceEuclidean: value = Distances.CalculateEuclid(wp, o, Instance.WrongTierPenaltyDistance); break;
                        case TurnoverPodStorageLocationClassRule.OutputStationDistanceManhattan: value = Distances.CalculateManhattan(wp, o, Instance.WrongTierPenaltyDistance); break;
                        case TurnoverPodStorageLocationClassRule.OutputStationDistanceShortestPath: value = Distances.CalculateShortestPathPodSafe(wp, o.Waypoint, Instance); break;
                        case TurnoverPodStorageLocationClassRule.OutputStationDistanceShortestTime: value = Distances.CalculateShortestTimePathPodSafe(wp, o.Waypoint, Instance); break;
                        default: throw new ArgumentException("Unknown storage location rule: " + _storageLocationClassRule);
                    }
                    return value;
                }))
                .ToList();

            // Allocate storage locations to classes
            _classStorageLocations = new List<Waypoint>[_classCount];
            List<Waypoint> tempPodsOrdered = _storageLocationsOrdered.ToList();
            for (int i = 0; i < _classCount; i++)
            {
                if (i < _classCount - 1)
                {
                    _classStorageLocations[i] = tempPodsOrdered.Take((int)(_classBorders[i] * _storageLocationsOrdered.Count)).ToList();
                    tempPodsOrdered.RemoveRange(0, _classStorageLocations[i].Count);
                }
                else
                {
                    _classStorageLocations[i] = tempPodsOrdered;
                }
            }

            // Log this reallocation
            Instance.LogInfo("Allocated storage locations - classes: " + string.Join(";", Enumerable.Range(0, _classCount).Select(c => c + ":" + _classStorageLocations[c].Count)));

            // Reallocate items and pods for the first time
            ReallocatePods();

            // Reallocate items and pods for the first time
            ReallocateItems();
        }

        /// <summary>
        /// Reallocates the pods to different classes.
        /// </summary>
        private void ReallocatePods()
        {
            // Sort pods by their current average frequency
            _podsOrdered = Instance.Pods
                .OrderBy(p => p.ItemDescriptionsContained.Any() ? p.ItemDescriptionsContained.Average(item => item.OrderCount) : 0)
                .ThenBy(p => Instance.OutputStations.Min(o =>
                 {
                     double value = 0;
                     switch (_storageLocationClassRule)
                     {
                         case TurnoverPodStorageLocationClassRule.OutputStationDistanceEuclidean:
                             value = Distances.CalculateEuclid(p, o, Instance.WrongTierPenaltyDistance); break;
                         case TurnoverPodStorageLocationClassRule.OutputStationDistanceManhattan:
                             value = Distances.CalculateManhattan(p, o, Instance.WrongTierPenaltyDistance); break;
                         case TurnoverPodStorageLocationClassRule.OutputStationDistanceShortestPath:
                             value = Distances.CalculateShortestPathPodSafe(Instance.WaypointGraph.GetClosestWaypoint(p.Tier, p.X, p.Y), o.Waypoint, Instance); break;
                         case TurnoverPodStorageLocationClassRule.OutputStationDistanceShortestTime:
                             value = Distances.CalculateShortestTimePathPodSafe(Instance.WaypointGraph.GetClosestWaypoint(p.Tier, p.X, p.Y), o.Waypoint, Instance); break;
                         default: throw new ArgumentException("Unknown storage location rule: " + _storageLocationClassRule);
                     }
                     return value;
                 }))
                .ToList();

            // Determine the shares of the capacity of the storage location classes
            double overallCapacity = _classStorageLocations.Sum(l => l.Count);
            Dictionary<int, double> classStorageCapacityShares = new Dictionary<int, double>();
            for (int i = 0; i < _classCount; i++)
                classStorageCapacityShares[i] = _classStorageLocations[i].Count / overallCapacity;

            // Group pods to classes
            _podClasses = new Dictionary<Pod, int>();
            _classStoragePods = Enumerable.Range(0, _classCount).Select(i => new List<Pod>()).ToArray();
            int currentClass = 0; int podCount = 0; double aggregatedRelativeCapacity = classStorageCapacityShares[0];
            foreach (var pod in _podsOrdered)
            {
                podCount++;
                // See whether the pod still fits in the current storage location area
                if ((double)podCount / _podsOrdered.Count > aggregatedRelativeCapacity)
                {
                    // Update virtual capacity
                    currentClass++;
                    if (currentClass < _classCount)
                        aggregatedRelativeCapacity += classStorageCapacityShares[currentClass];
                }
                // Assign the pod to the class
                _podClasses[pod] = currentClass;
                _classStoragePods[currentClass].Add(pod);
                // Mark the pods
                pod.InfoTagPodStorageType = (_classCount - currentClass - 1.0) / (_classCount - 1.0);
                pod.InfoTagPodStorageInfo = "Class" + currentClass;
            }

            // Log this reallocation
            Instance.LogInfo("Reallocated pods - classes: " + string.Join(";", Enumerable.Range(0, _classCount).Select(c => c + ":" + _classStoragePods[c].Count)));

            // Mark this re-allocation
            _lastReallocation = Instance.Controller.CurrentTime;
        }
        /// <summary>
        /// Reallocates the items to different classes
        /// </summary>
        private void ReallocateItems()
        {
            // Sort item descriptions by their current frequency
            _itemDescriptionsOrdered = Instance.ItemDescriptions.OrderByDescending(i => i.OrderCount).ToList();

            // Determine the shares of the capacity of the storage pod classes
            double overallCapacity = _classStoragePods.Sum(l => l.Sum(p => p.Capacity));
            Dictionary<int, double> classStorageCapacityShares = new Dictionary<int, double>();
            for (int i = 0; i < _classCount; i++)
                classStorageCapacityShares[i] = _classStoragePods[i].Sum(p => p.Capacity) / overallCapacity;

            // Get weighted item demand
            double overallDemand = _itemDescriptionsOrdered.Sum(i => i.OrderCount);
            double overallWeight = _itemDescriptionsOrdered.Sum(i => i.Weight);
            Dictionary<ItemDescription, double> weightedItemDemand = _itemDescriptionsOrdered.ToDictionary(
                k => k,
                v => (v.OrderCount / overallDemand + v.Weight / overallWeight) / 2.0);

            // Group items to classes
            _itemClasses = new Dictionary<ItemDescription, int>();
            int currentClass = 0; double aggregatedWeightedDemand = 0; double aggregatedWeightedCapacity = classStorageCapacityShares[0];
            foreach (var itemDescription in _itemDescriptionsOrdered)
            {
                // Keep track of estimated demand for capacity within the class
                aggregatedWeightedDemand += weightedItemDemand[itemDescription];
                // Check whether this item description still virtually fits into the class
                if (aggregatedWeightedDemand > aggregatedWeightedCapacity)
                {
                    // Update current virtual capacity
                    currentClass++;
                    if (currentClass < _classCount)
                        aggregatedWeightedCapacity += classStorageCapacityShares[currentClass];
                }
                // Assign the item to the current class
                _itemClasses[itemDescription] = currentClass;
            }

            // Log this reallocation
            Instance.LogInfo("Reallocated item descriptions - classes: " +
                string.Join(";", Enumerable.Range(0, _classCount).Select(c => c + ":" + _itemClasses.Count(i => i.Value == c))));

            // Mark this re-allocation
            _lastReallocation = Instance.Controller.CurrentTime;
        }

        /// <summary>
        /// The next event when this element has to be updated.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns>The next time this element has to be updated.</returns>
        public double GetNextEventTime(double currentTime) { return double.PositiveInfinity; }

        /// <summary>
        /// Updates the element to the specified time.
        /// </summary>
        /// <param name="lastTime">The time before the update.</param>
        /// <param name="currentTime">The time to update to.</param>
        public void Update(double lastTime, double currentTime) { EnsureInit(); ReallocateCallback(currentTime); }
    }
}
