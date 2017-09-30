using RAWSimO.Core.Control;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.IO;
using RAWSimO.Core.Items;
using RAWSimO.Core.Waypoints;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Management
{
    /// <summary>
    /// Implements a resource manager that keeps track of all the resources in the system like available storage locations and requests.
    /// </summary>
    public class ResourceManager : IUpdateable
    {
        /// <summary>
        /// Creates a new instance of the resource manager.
        /// </summary>
        /// <param name="instance">The instance the resource manager belongs to.</param>
        public ResourceManager(Instance instance)
        {
            _instance = instance;
            _usedPods = new Dictionary<Pod, Bot>();
            _unusedPods = new HashSet<Pod>(instance.Pods);
            List<Waypoint> usedStorageLocations = instance.Waypoints.Where(w => w.Pod != null).ToList();
            List<Waypoint> unusedStorageLocations = instance.Waypoints.Where(w => w.PodStorageLocation).Except(usedStorageLocations).ToList();
            foreach (var wp in usedStorageLocations)
                AddNewUsedPodStorageLocation(wp.Pod, wp);
            foreach (var wp in unusedStorageLocations)
                AddNewValidPodStorageLocation(wp);
            foreach (var oStation in instance.OutputStations)
            {
                _availableExtractRequestsPerStation[oStation] = new HashSet<ExtractRequest>();
                _availableExtractRequestsPerStationQueue[oStation] = new HashSet<ExtractRequest>();
            }
            foreach (var iStation in instance.InputStations)
                _availableStoreRequestsPerStation[iStation] = new HashSet<InsertRequest>();
            foreach (var pod in instance.Pods)
                _availableStoreRequestsPerPod[pod] = new HashSet<InsertRequest>();
            // Register for new order event to keep track of requests for all placed orders
            instance.OrderCompleted += OrderCompleted;
        }

        /// <summary>
        /// The instance this manager belongs to.
        /// </summary>
        private Instance _instance;

        #region Pod handling

        /// <summary>
        /// All pods currently reserved by bots.
        /// </summary>
        private Dictionary<Pod, Bot> _usedPods;

        /// <summary>
        /// All pods currently not used.
        /// </summary>
        private HashSet<Pod> _unusedPods;

        /// <summary>
        /// All pods currently reserved by bots.
        /// </summary>
        public IEnumerable<Pod> UsedPods { get { return _usedPods.Keys; } }

        /// <summary>
        /// All pods currently not used.
        /// </summary>
        public IEnumerable<Pod> UnusedPods { get { return _unusedPods; } }

        /// <summary>
        /// Indicates whether the given pod is currently claimed by a bot.
        /// </summary>
        /// <param name="pod">The pod to check.</param>
        /// <returns><code>true</code> if the pod is currently unavailable for any use, <code>false</code> otherwise.</returns>
        public bool IsPodClaimed(Pod pod) { return _usedPods.ContainsKey(pod); }

        /// <summary>
        /// Reserves the pod for the given bot.
        /// </summary>
        /// <param name="pod">The pod to reserve.</param>
        /// <param name="bot">The bot that reserves the pod.</param>
        /// <param name="purpose">The purpose for claiming the pod.</param>
        public void ClaimPod(Pod pod, Bot bot, BotTaskType purpose)
        {
            // Sanity check
            if (_usedPods.ContainsKey(pod) && _usedPods[pod] != bot)
                throw new InvalidOperationException("Pod is already claimed by another bot!");
            // Claim it
            _usedPods[pod] = bot;
            _unusedPods.Remove(pod);
            // Notify the instance about the operation
            _instance.NotifyPodClaimed(pod, bot, purpose);
        }

        /// <summary>
        /// Releases the reservation of the pod.
        /// </summary>
        /// <param name="pod">The pod to release.</param>
        public void ReleasePod(Pod pod)
        {
            // Release claim
            _unusedPods.Add(pod);
            _usedPods.Remove(pod);
        }

        #endregion

        #region Pod storage location handling

        /// <summary>
        /// All pod storage locations currently not in use.
        /// </summary>
        private HashSet<Waypoint> _unusedPodStorageLocations = new HashSet<Waypoint>();

        /// <summary>
        /// All pod storage locations currently in use.
        /// </summary>
        private HashSet<Waypoint> _usedPodStorageLocations = new HashSet<Waypoint>();

        /// <summary>
        /// All pod storage locations currently not in use.
        /// </summary>
        public IEnumerable<Waypoint> UnusedPodStorageLocations { get { return _unusedPodStorageLocations; } }

        /// <summary>
        /// All pod storage locations currently in use.
        /// </summary>
        public IEnumerable<Waypoint> UsedPodStorageLocations { get { return _usedPodStorageLocations; } }

        /// <summary>
        /// Checks whether the given storage location is already occupied by another pod or reserved.
        /// </summary>
        /// <param name="wp">The storage location to check.</param>
        /// <returns><code>true</code> if the storage location is free, <code>false</code> otherwise.</returns>
        public bool IsStorageLocationClaimed(Waypoint wp) { return _usedPodStorageLocations.Contains(wp); }

        /// <summary>
        /// Reserves the pod storage location.
        /// </summary>
        /// <param name="storageLocation">The pod storage location to reserve.</param>
        public void ClaimStorageLocation(Waypoint storageLocation)
        {
            bool successfulOperation = _usedPodStorageLocations.Add(storageLocation);
            successfulOperation = successfulOperation && _unusedPodStorageLocations.Remove(storageLocation);
            if (!successfulOperation)
                throw new InvalidOperationException("Cannot claim a storage location that is not available!");
        }

        /// <summary>
        /// Releases the reservation of the pod storage location.
        /// </summary>
        /// <param name="storageLocation">The storage location to release.</param>
        public void ReleaseStorageLocation(Waypoint storageLocation)
        {
            bool successfulOperation = _unusedPodStorageLocations.Add(storageLocation);
            successfulOperation = successfulOperation && _usedPodStorageLocations.Remove(storageLocation);
            if (!successfulOperation)
                throw new InvalidOperationException("Cannot release an unclaimed storage location!");
        }

        /// <summary>
        /// Adds a new valid currently used location to store pods on the map.
        /// </summary>
        /// <param name="pod">The pod currently stored at the position.</param>
        /// <param name="waypoint">The waypoint which serves as a pod storage location.</param>
        public void AddNewUsedPodStorageLocation(Pod pod, Waypoint waypoint)
        {
            _usedPodStorageLocations.Add(waypoint);
        }

        /// <summary>
        /// Adds a new valid unused location to store pods on the map.
        /// </summary>
        /// <param name="waypoint">The waypoint which serves as a pod storage location.</param>
        public void AddNewValidPodStorageLocation(Waypoint waypoint)
        {
            _unusedPodStorageLocations.Add(waypoint);
        }

        /// <summary>
        /// Removes a pod storage location again.
        /// </summary>
        /// <param name="waypoint">The storage location to remove.</param>
        public void RemovePodStorageLocation(Waypoint waypoint)
        {
            _usedPodStorageLocations.Remove(waypoint);
            _unusedPodStorageLocations.Remove(waypoint);
        }

        #endregion

        #region Resting location handling

        /// <summary>
        /// All resting locations currently not in use.
        /// </summary>
        public IEnumerable<Waypoint> UnusedRestingLocations
        {
            get
            {
                // TODO for now this matches the pod storage locations
                return _unusedPodStorageLocations.Except(_forbiddenRestingLocations);
            }
        }
        /// <summary>
        /// Locations that cannot be used for resting.
        /// </summary>
        private HashSet<Waypoint> _forbiddenRestingLocations = new HashSet<Waypoint>();
        /// <summary>
        /// Mark a location as not available for resting.
        /// </summary>
        /// <param name="waypoint">The waypoint to mark as not usable for resting.</param>
        public void ForbidRestLocation(Waypoint waypoint) { _forbiddenRestingLocations.Add(waypoint); }
        /// <summary>
        /// Indicates whether the given resting location is available.
        /// </summary>
        /// <param name="waypoint">The resting location to check for availability.</param>
        /// <returns><code>true</code> if the location is unoccupied, <code>false</code> otherwise.</returns>
        public bool IsRestingLocationAvailable(Waypoint waypoint) { return _unusedPodStorageLocations.Contains(waypoint); }
        /// <summary>
        /// Reserves the resting location.
        /// </summary>
        /// <param name="waypoint">The resting location to reserve.</param>
        public void ClaimRestingLocation(Waypoint waypoint)
        {
            bool successfulOperation = _usedPodStorageLocations.Add(waypoint);
            successfulOperation = successfulOperation && _unusedPodStorageLocations.Remove(waypoint);
            if (!successfulOperation)
                throw new InvalidOperationException("Cannot claim a resting location that is not available!");
        }
        /// <summary>
        /// Releases the reservation of the resting location.
        /// </summary>
        /// <param name="waypoint">The resting location to release.</param>
        public void ReleaseRestingLocation(Waypoint waypoint)
        {
            bool successfulOperation = _unusedPodStorageLocations.Add(waypoint);
            successfulOperation = successfulOperation && _usedPodStorageLocations.Remove(waypoint);
            if (!successfulOperation)
                throw new InvalidOperationException("Cannot release an unclaimed resting location!");
        }

        #endregion

        #region Store request handling

        /// <summary>
        /// All available requests for storing an item bundle.
        /// </summary>
        private HashSet<InsertRequest> _availableStoreRequests = new HashSet<InsertRequest>();

        /// <summary>
        /// All requests to store a bundle per input station they are allocated to.
        /// </summary>
        private Dictionary<InputStation, HashSet<InsertRequest>> _availableStoreRequestsPerStation = new Dictionary<InputStation, HashSet<InsertRequest>>();

        /// <summary>
        /// All requests to store a bundle per pod they are allocated to.
        /// </summary>
        private Dictionary<Pod, HashSet<InsertRequest>> _availableStoreRequestsPerPod = new Dictionary<Pod, HashSet<InsertRequest>>();

        /// <summary>
        /// All available requests for storing an item bundle.
        /// </summary>
        public IEnumerable<InsertRequest> AvailableStoreRequests { get { return _availableStoreRequests; } }

        /// <summary>
        /// Removes the corresponding request to store a bundle.
        /// </summary>
        /// <param name="request">The request to remove.</param>
        public void RemoveStoreRequest(InsertRequest request)
        {
            _availableStoreRequests.Remove(request);
            if (request.Station != null)
            {
                _availableStoreRequestsPerStation[request.Station].Remove(request);
                request.Station.StatCurrentlyOpenRequests = _availableStoreRequestsPerStation[request.Station].Count;
            }
            if (request.Pod != null)
                _availableStoreRequestsPerPod[request.Pod].Remove(request);
        }

        /// <summary>
        /// Adds the previously removed request to store a bundle again.
        /// </summary>
        /// <param name="request">The request to add again.</param>
        public void ReInsertStoreRequest(InsertRequest request)
        {
            request.ReInsert();
            _availableStoreRequests.Add(request);
            if (request.Station != null)
            {
                _availableStoreRequestsPerStation[request.Station].Add(request);
                request.Station.StatCurrentlyOpenRequests = _availableStoreRequestsPerStation[request.Station].Count;
            }
            if (request.Pod != null)
                _availableStoreRequestsPerPod[request.Pod].Add(request);
        }

        /// <summary>
        /// Called whenever a new item has been assigned to an InputStation
        /// </summary>
        /// <param name="item">The assigned item.</param>
        /// <param name="inputStation">The InputStation the item is assigned to.</param>
        /// <param name="pod">The pod to store this item in.</param>
        public void NewItemBundleAssignedToStation(ItemBundle item, InputStation inputStation, Pod pod)
        {
            InsertRequest request = new InsertRequest(item, inputStation, pod);
            _availableStoreRequests.Add(request);
            if (request.Station != null)
            {
                _availableStoreRequestsPerStation[request.Station].Add(request);
                request.Station.StatCurrentlyOpenRequests = _availableStoreRequestsPerStation[request.Station].Count;
            }
            if (request.Pod != null)
                _availableStoreRequestsPerPod[request.Pod].Add(request);
        }

        /// <summary>
        /// Returns all requests belonging to the specified station.
        /// </summary>
        /// <param name="station">The station which requests are desired.</param>
        /// <returns>The requests of the given station.</returns>
        public IEnumerable<InsertRequest> GetStoreRequestsOfStation(InputStation station) { return _availableStoreRequestsPerStation[station]; }

        /// <summary>
        /// Returns all requests belonging to the specified pod.
        /// </summary>
        /// <param name="pod">The pod which requests are desired.</param>
        /// <returns>The requests of the given pod.</returns>
        public IEnumerable<InsertRequest> GetStoreRequestsOfPod(Pod pod) { return _availableStoreRequestsPerPod[pod]; }

        #endregion

        #region Extract request handling

        /// <summary>
        /// All requests to extract an item.
        /// </summary>
        private HashSet<ExtractRequest> _availableExtractRequests = new HashSet<ExtractRequest>();
        /// <summary>
        /// All available requests to extract an item per order they belong to.
        /// </summary>
        private Dictionary<Order, HashSet<ExtractRequest>> _availableExtractRequestsPerOrder = new Dictionary<Order, HashSet<ExtractRequest>>();
        /// <summary>
        /// All requests to extract an item per output station they are allocated to.
        /// </summary>
        private Dictionary<OutputStation, HashSet<ExtractRequest>> _availableExtractRequestsPerStation = new Dictionary<OutputStation, HashSet<ExtractRequest>>();
        /// <summary>
        /// All requests to extract an item that have been queued for the respective stations.
        /// </summary>
        private Dictionary<OutputStation, HashSet<ExtractRequest>> _availableExtractRequestsPerStationQueue = new Dictionary<OutputStation, HashSet<ExtractRequest>>();
        /// <summary>
        /// The respective stations an extract request was queued for, if it was queued.
        /// </summary>
        private Dictionary<ExtractRequest, OutputStation> _stationQueuedPerExtractRequest = new Dictionary<ExtractRequest, OutputStation>();

        /// <summary>
        /// Creates requests for all placed orders.
        /// </summary>
        /// <param name="order">The order that was just placed.</param>
        public void CreateExtractRequests(Order order)
        {
            // Create requests for all lines and units of the order
            foreach (var l in order.Positions)
                for (int i = 0; i < l.Value; i++)
                {
                    ExtractRequest request = new ExtractRequest(l.Key, order, null);
                    order.AddRequest(l.Key, request);
                    _availableExtractRequests.Add(request);
                }
            _availableExtractRequestsPerOrder[order] = new HashSet<ExtractRequest>(order.Requests);
            // Update demand tracking
            foreach (var pos in order.Positions)
                _backlogDemand[pos.Key] += pos.Value;
        }

        /// <summary>
        /// Called whenever a new order shall be assigned to the order pool of a station.
        /// </summary>
        /// <param name="order">The order to assign to the order pool of the station.</param>
        /// <param name="station">The station to assign the order to.</param>
        public void NewOrderQueuedToStation(Order order, OutputStation station)
        {
            // Remember queue time
            order.TimeStampQueued = _instance.Controller.CurrentTime;
            // Remember station for all requests of the order
            foreach (var request in order.Requests)
                _stationQueuedPerExtractRequest[request] = station;
            // Update active requests
            foreach (var request in _availableExtractRequestsPerOrder[order])
            {
                // Move not yet allocated requests to available requests of station
                _availableExtractRequestsPerStationQueue[station].Add(request);
                // Update demand
                _queuedDemand[request.Item]++;
                _backlogDemand[request.Item]--;
            }
            // Update statistics
            station.StatCurrentlyOpenQueuedRequests = _availableExtractRequestsPerStationQueue[station].Count;
        }

        /// <summary>
        /// Called whenever a new Order has been assigned to an OutputStation.
        /// </summary>
        /// <param name="order">The Order which is assigned to the station.</param>
        /// <param name="station">The station the order is assigned to.</param>
        public void NewOrderAssignedToStation(Order order, OutputStation station)
        {
            // Connect request info with assigned station
            foreach (var request in order.Requests)
            {
                request.Assign(station);
                // If request is not yet assigned, update the info
                if (_availableExtractRequests.Contains(request))
                {
                    // Move request to available request list of station
                    _availableExtractRequestsPerStation[station].Add(request);
                    _availableExtractRequestsPerStationQueue[station].Remove(request);
                    // Update demand
                    _queuedDemand[request.Item]--;
                    _assignedDemand[request.Item]++;
                }
                // Manage queue info
                _stationQueuedPerExtractRequest.Remove(request);
            }
            station.StatCurrentlyOpenRequests = _availableExtractRequestsPerStation[station].Count;
            station.StatCurrentlyOpenQueuedRequests = _availableExtractRequestsPerStationQueue[station].Count;
        }

        /// <summary>
        /// Removes the request to extract the item.
        /// </summary>
        /// <param name="request">The request to remove.</param>
        public void RemoveExtractRequest(ExtractRequest request)
        {
            // Manage overall list of available requests
            _availableExtractRequests.Remove(request);
            _availableExtractRequestsPerOrder[request.Order].Remove(request);

            // Manage requests available per station
            if (request.Station != null)
            {
                _availableExtractRequestsPerStation[request.Station].Remove(request);
                request.Station.StatCurrentlyOpenRequests = _availableExtractRequestsPerStation[request.Station].Count;
                // Update demand
                _assignedDemand[request.Item]--;
            }
            // Remove from queue of the station
            else if (_stationQueuedPerExtractRequest.ContainsKey(request))
            {
                _availableExtractRequestsPerStationQueue[_stationQueuedPerExtractRequest[request]].Remove(request);
                _stationQueuedPerExtractRequest[request].StatCurrentlyOpenQueuedRequests = _availableExtractRequestsPerStationQueue[_stationQueuedPerExtractRequest[request]].Count;
                // Update demand
                _queuedDemand[request.Item]--;
            }
            // Remove from overall backlog
            else
            {
                // Update demand
                _backlogDemand[request.Item]--;
            }
        }

        /// <summary>
        /// Adds the request to extract the corresponding item again.
        /// </summary>
        /// <param name="request">The request to insert again.</param>
        public void ReInsertExtractRequest(ExtractRequest request)
        {
            // Mark request as available again
            request.ReInsert();
            // Manage overall list of available requests
            _availableExtractRequests.Add(request);
            _availableExtractRequestsPerOrder[request.Order].Add(request);
            // Manage requests available per station
            if (request.Station != null)
            {
                _availableExtractRequestsPerStation[request.Station].Add(request);
                request.Station.StatCurrentlyOpenRequests = _availableExtractRequestsPerStation[request.Station].Count;
                // Update demand
                _assignedDemand[request.Item]++;
            }
            else if (_stationQueuedPerExtractRequest.ContainsKey(request))
            // Re-add to queue of the station
            {
                _availableExtractRequestsPerStationQueue[_stationQueuedPerExtractRequest[request]].Add(request);
                _stationQueuedPerExtractRequest[request].StatCurrentlyOpenQueuedRequests = _availableExtractRequestsPerStationQueue[_stationQueuedPerExtractRequest[request]].Count;
                // Update demand
                _queuedDemand[request.Item]++;
            }
            // Re-add to overall backlog
            else
            {
                // Update demand
                _backlogDemand[request.Item]++;
            }
        }

        /// <summary>
        /// Removes the request information belonging to the order.
        /// </summary>
        /// <param name="order">The order that was just completed.</param>
        /// <param name="station">The station at which the order was completed.</param>
        private void OrderCompleted(Order order, OutputStation station)
        {
            foreach (var r in order.Requests)
                _stationQueuedPerExtractRequest.Remove(r);
            _availableExtractRequestsPerOrder.Remove(order);
        }

        /// <summary>
        /// Returns all requests belonging to the specified station.
        /// </summary>
        /// <param name="station">The station which requests are desired.</param>
        /// <returns>The requests of the given station.</returns>
        public IEnumerable<ExtractRequest> GetExtractRequestsOfStation(OutputStation station) { return _availableExtractRequestsPerStation[station]; }
        /// <summary>
        /// Returns all requests currently queued for the given station.
        /// </summary>
        /// <param name="station">The station the requests are queued for.</param>
        /// <returns>All requests currently queued for the given station.</returns>
        public IEnumerable<ExtractRequest> GetQueuedExtractRequestsOfStation(OutputStation station) { return _availableExtractRequestsPerStationQueue[station]; }
        /// <summary>
        /// Returns all requests belonging to the specified order that haven't been reserved yet.
        /// </summary>
        /// <param name="order">The order to get the extract requests of.</param>
        /// <returns>All extract requests available for reservation of the given order.</returns>
        public IEnumerable<ExtractRequest> GetExtractRequestsOfOrder(Order order) { return _availableExtractRequestsPerOrder[order]; }

        /// <summary>
        /// All requests to extract an item.
        /// </summary>
        public IEnumerable<ExtractRequest> AvailableAndAssignedExtractRequests { get { return _availableExtractRequests.Where(r => r.Station != null); } }

        #endregion

        #region Demand tracking

        /// <summary>
        /// The demand for specific SKUs given by the current backlog orders.
        /// </summary>
        private VolatileIDDictionary<ItemDescription, int> _backlogDemandStore;
        /// <summary>
        /// The demand for specific SKUs given by the current backlog orders.
        /// </summary>
        private VolatileIDDictionary<ItemDescription, int> _backlogDemand { get { if (_backlogDemandStore == null) InitDemandTracking(); return _backlogDemandStore; } }
        /// <summary>
        /// The demand for specific SKUs given by the currently queued orders.
        /// </summary>
        private VolatileIDDictionary<ItemDescription, int> _queuedDemandStore;
        /// <summary>
        /// The demand for specific SKUs given by the currently queued orders.
        /// </summary>
        private VolatileIDDictionary<ItemDescription, int> _queuedDemand { get { if (_queuedDemandStore == null) InitDemandTracking(); return _queuedDemandStore; } }
        /// <summary>
        /// The demand for specific SKUs given by the currently assigned orders.
        /// </summary>
        private VolatileIDDictionary<ItemDescription, int> _assignedDemandStore;
        /// <summary>
        /// The demand for specific SKUs given by the currently assigned orders.
        /// </summary>
        private VolatileIDDictionary<ItemDescription, int> _assignedDemand { get { if (_assignedDemandStore == null) InitDemandTracking(); return _assignedDemandStore; } }
        /// <summary>
        /// Inits demand tracking, if not already done.
        /// </summary>
        private void InitDemandTracking()
        {
            _backlogDemandStore = new VolatileIDDictionary<ItemDescription, int>(_instance.ItemDescriptions.Select(i => new VolatileKeyValuePair<ItemDescription, int>(i, 0)).ToList());
            _queuedDemandStore = new VolatileIDDictionary<ItemDescription, int>(_instance.ItemDescriptions.Select(i => new VolatileKeyValuePair<ItemDescription, int>(i, 0)).ToList());
            _assignedDemandStore = new VolatileIDDictionary<ItemDescription, int>(_instance.ItemDescriptions.Select(i => new VolatileKeyValuePair<ItemDescription, int>(i, 0)).ToList());
        }
        /// <summary>
        /// Gets the demand for the given item given by the backlog orders.
        /// </summary>
        /// <param name="item">The item to look-up the demand for.</param>
        /// <returns>The demand for the given item.</returns>
        internal int GetDemandBacklog(ItemDescription item) { return _backlogDemand[item]; }
        /// <summary>
        /// Gets the demand for the given item given by the queued orders.
        /// </summary>
        /// <param name="item">The item to look-up the demand for.</param>
        /// <returns>The demand for the given item.</returns>
        internal int GetDemandQueued(ItemDescription item) { return _queuedDemand[item]; }
        /// <summary>
        /// Gets the demand for the given item given by the assigned orders.
        /// </summary>
        /// <param name="item">The item to look-up the demand for.</param>
        /// <returns>The demand for the given item.</returns>
        internal int GetDemandAssigned(ItemDescription item) { return _assignedDemand[item]; }

        #endregion

        #region IUpdateable Members

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
        public void Update(double lastTime, double currentTime) { }

        #endregion
    }

    /// <summary>
    /// Distinguishes between different states a request can be in.
    /// </summary>
    public enum RequestState
    {
        /// <summary>
        /// Indicates that the request was not fulfilled yet.
        /// </summary>
        Unfinished,
        /// <summary>
        /// Indicates that the request was aborted due to some reason.
        /// </summary>
        Aborted,
        /// <summary>
        /// Indicates that the request was finished due to some reason.
        /// </summary>
        Finished,
    }

    /// <summary>
    /// Depicts one specific request to extract an item of the specified type at the specified station in order to serve the specified order.
    /// </summary>
    public class ExtractRequest
    {
        /// <summary>
        /// Creates a new extraction request.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="order">The order.</param>
        /// <param name="station">The station.</param>
        public ExtractRequest(ItemDescription item, Order order, OutputStation station) { Item = item; Order = order; Station = station; State = RequestState.Unfinished; }
        /// <summary>
        /// The state of the request.
        /// </summary>
        public RequestState State { get; private set; }
        /// <summary>
        /// Marks a previously aborted request as open again.
        /// </summary>
        public void ReInsert() { State = RequestState.Unfinished; }
        /// <summary>
        /// Aborts the request due to some reason. The request should be re-enqueued.
        /// </summary>
        public void Abort() { State = RequestState.Aborted; }
        /// <summary>
        /// Marks the request as completed.
        /// </summary>
        public void Finish() { State = RequestState.Finished; }
        /// <summary>
        /// Assigns the request to the given station.
        /// </summary>
        /// <param name="station">The station to assign the request to.</param>
        public void Assign(OutputStation station) { Station = station; }
        /// <summary>
        /// Assigns the request to the given pod.
        /// </summary>
        /// <param name="pod">The pod to assign the request to.</param>
        public void Assign(Pod pod) { Pod = pod; }
        /// <summary>
        /// Unassigns the request from the given pod.
        /// </summary>
        /// <param name="pod">The pod that previously was assigned to this request.</param>
        public void Unassign(Pod pod) { Pod = null; }
        /// <summary>
        /// The type of the item needed to serve a position of the order.
        /// </summary>
        public ItemDescription Item { get; private set; }
        /// <summary>
        /// The order for which the item is needed.
        /// </summary>
        public Order Order { get; private set; }
        /// <summary>
        /// The station to which the corresponding order is assigned to.
        /// </summary>
        public OutputStation Station { get; private set; }
        /// <summary>
        /// The pod with which this request shall be handled.
        /// </summary>
        public Pod Pod { get; private set; }
        /// <summary>
        /// Used to indicate whether the request was used to build a new task or injected to an existing task.
        /// </summary>
        public bool StatInjected { get; set; }
        /// <summary>
        /// Returns a simple string giving information about the object.
        /// </summary>
        /// <returns>A simple string.</returns>
        public override string ToString() { return Station.ToString() + IOConstants.DELIMITER_TUPLE + Order.ToString() + IOConstants.DELIMITER_TUPLE + Item.ToString(); }
    }

    /// <summary>
    /// Depicts one specific request to fetch the specified bundle from the specified station and store in the inventory.
    /// </summary>
    public class InsertRequest
    {
        /// <summary>
        /// Creates a new insertion request.
        /// </summary>
        /// <param name="bundle">The bundle.</param>
        /// <param name="station">The station.</param>
        /// <param name="pod">The pod.</param>
        public InsertRequest(ItemBundle bundle, InputStation station, Pod pod) { Bundle = bundle; Station = station; Pod = pod; State = RequestState.Unfinished; }
        /// <summary>
        /// The state of the request.
        /// </summary>
        public RequestState State { get; private set; }
        /// <summary>
        /// Marks a previously aborted request as open again.
        /// </summary>
        public void ReInsert() { State = RequestState.Unfinished; }
        /// <summary>
        /// Aborts the request due to some reason. The request should be re-enqueued.
        /// </summary>
        public void Abort() { State = RequestState.Aborted; }
        /// <summary>
        /// Marks the request as completed.
        /// </summary>
        public void Finish() { State = RequestState.Finished; }
        /// <summary>
        /// The bundle to store in the inventory.
        /// </summary>
        public ItemBundle Bundle { get; private set; }
        /// <summary>
        /// The station the bundle is located.
        /// </summary>
        public InputStation Station { get; private set; }
        /// <summary>
        /// The pod to store the bundle in.
        /// </summary>
        public Pod Pod { get; private set; }
        /// <summary>
        /// Used to indicate whether the request was used to build a new task or injected to an existing task.
        /// </summary>
        public bool StatInjected { get; set; }
        /// <summary>
        /// Returns a simple string giving information about the object.
        /// </summary>
        /// <returns>A simple string.</returns>
        public override string ToString() { return Station.ToString() + IOConstants.DELIMITER_TUPLE + Bundle.ToString(); }
    }
}
