using RAWSimO.Core.Control;
using RAWSimO.Core.Geometrics;
using RAWSimO.Core.Info;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.Items;
using RAWSimO.Core.Management;
using RAWSimO.Core.Statistics;
using RAWSimO.Core.Waypoints;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Elements
{
    /// <summary>
    /// Implements the output-station, i.e. the picking station.
    /// </summary>
    public class OutputStation : Circle, IUpdateable, IOutputStationInfo, IQueuesOwner, IExposeVolatileID
    {
        #region Constructors

        /// <summary>
        /// Creates a new output-station.
        /// </summary>
        /// <param name="instance">The instance this station belongs to.</param>
        internal OutputStation(Instance instance) : base(instance) { }

        #endregion

        #region Core

        /// <summary>
        /// Indicates whether this station is active
        /// </summary>
        public bool Active { get; private set; } = true;
        /// <summary>
        /// Activates this station.
        /// </summary>
        public void Activate()
        {
            // Make available
            Active = true;
            // Mark new situation to manager
            Instance?.Controller?.OrderManager?.SignalStationActivated(this);
            // Track up-time from the current time on
            _lastActiveMeasurement = Instance.Controller != null ? Instance.Controller.CurrentTime : 0;
        }
        /// <summary>
        /// Deactivates this station.
        /// </summary>
        public void Deactivate()
        {
            // Make unavailable
            Active = false;
            // Finish tracking of up-time
            if (_lastActiveMeasurement < (Instance.Controller != null ? Instance.Controller.CurrentTime : 0))
                StatActiveTime += (Instance.Controller != null ? Instance.Controller.CurrentTime : 0) - _lastActiveMeasurement;
        }

        /// <summary>
        /// The time it takes to pick one item from a pod and put it into the order tote.
        /// </summary>
        public double ItemTransferTime;

        /// <summary>
        /// The time it takes to pick one item from a pod (excluding the time it takes to put it into a tote - after this the robot is free to leave, if it does not have other items to be picked).
        /// </summary>
        public double ItemPickTime;

        /// <summary>
        /// The time it takes to complete an order.
        /// </summary>
        public double OrderCompletionTime;

        /// <summary>
        /// The waypoint this output-station is located at.
        /// </summary>
        public Waypoint Waypoint;

        /// <summary>
        /// The order ID of this station that defines the sequence in which the stations have to be activated.
        /// </summary>
        public int ActivationOrderID;

        /// <summary>
        /// The capacity of this station.
        /// </summary>
        public int Capacity;

        /// <summary>
        /// The capacity currently in use at this station.
        /// </summary>
        public int CapacityInUse { get { return _assignedOrders.Count; } }

        /// <summary>
        /// The amount of capacity reserved by a controller.
        /// </summary>
        internal int CapacityReserved { get { return _registeredOrders.Count; } }

        /// <summary>
        /// The orders currently assigned to this station.
        /// </summary>
        private HashSet<Order> _assignedOrders = new HashSet<Order>();
        /// <summary>
        /// The set of orders not yet allocated but already registered with this station.
        /// </summary>
        private HashSet<Order> _registeredOrders = new HashSet<Order>();
        /// <summary>
        /// The set of orders queued for this station.
        /// </summary>
        private HashSet<Order> _queuedOrders = new HashSet<Order>();

        /// <summary>
        /// The orders currently assigned to this station.
        /// </summary>
        public IEnumerable<Order> AssignedOrders { get { return _assignedOrders; } }
        /// <summary>
        /// The orders currently queued to this station.
        /// </summary>
        public IEnumerable<Order> QueuedOrders { get { return _queuedOrders; } }

        /// <summary>
        /// Checks whether the specified order can be added for reservation to this station.
        /// </summary>
        /// <param name="order">The order that has to be checked.</param>
        /// <returns><code>true</code> if the bundle fits, <code>false</code> otherwise.</returns>
        public bool FitsForReservation(Order order) { return CapacityInUse + CapacityReserved + 1 <= Capacity; }

        /// <summary>
        /// Reserves capacity of this station for the given order. The reserved capacity will be maintained when the order is allocated.
        /// </summary>
        /// <param name="order">The order for which capacity shall be reserved.</param>
        internal void RegisterOrder(Order order)
        {
            _registeredOrders.Add(order);
            if (CapacityInUse + CapacityReserved > Capacity)
                throw new InvalidOperationException("Cannot reserve more capacity than this station has!");

        }

        /// <summary>
        /// The order to queue in for this station.
        /// </summary>
        /// <param name="order">The order to queue in.</param>
        internal void QueueOrder(Order order)
        {
            StatCurrentlyOpenQueuedItems += order.Requests.Count();
            _queuedOrders.Add(order);
        }

        /// <summary>
        /// Assigns a new order to this station.
        /// </summary>
        /// <param name="order">The order to assign to this station.</param>
        /// <returns><code>true</code> if the order was successfully assigned, <code>false</code> otherwise.</returns>
        public bool AssignOrder(Order order)
        {
            if (_assignedOrders.Count < Capacity)
            {
                // Assign the order
                _assignedOrders.Add(order);
                // Remove the bundle from the reservation list
                _registeredOrders.Remove(order);
                // Notify the instance about the order
                Instance.NotifyOrderAllocated(this, order);
                // Keep track of current number of items to pick
                StatCurrentlyOpenItems += order.Positions.Sum(p => p.Value);
                // Remove order from queue, if it came from the queue
                if (_queuedOrders.Contains(order))
                {
                    StatCurrentlyOpenQueuedItems -= order.Requests.Count();
                    _queuedOrders.Remove(order);
                }
                // Reset rest-time
                _statDepletionTime = double.PositiveInfinity;
                // Update the order list for the visualization, if present
                if (Instance.SettingConfig.VisualizationAttached)
                    lock (_syncRoot)
                        _openOrders.Add(order);
                // Return success
                return true;
            }
            else
            {
                // Return fail
                return false;
            }
        }

        /// <summary>
        /// The queue of items to extract from the pods.
        /// </summary>
        private Queue<ExtractRequest> _requestsExtract = new Queue<ExtractRequest>();
        /// <summary>
        /// The queue of bots per item extraction request.
        /// </summary>
        private Queue<Bot> _requestsBot = new Queue<Bot>();

        /// <summary>
        /// Picks the next enqueued item.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        /// <returns><code>true</code> if there was an item to pick and the operation was successful, <code>false</code> otherwise.</returns>
        protected bool TakeItemFromPod(double currentTime)
        {
            // Keep going through queue until have something to take or done with queue
            while (_requestsExtract.Count > 0)
            {
                // Fetch necessary stuff
                Bot bot = _requestsBot.Dequeue();
                Pod pod = bot.Pod;
                ExtractRequest request = _requestsExtract.Dequeue();
                ItemDescription item = request.Item;

                if (pod.IsContained(item) && GetDistance(pod) < GetInfoRadius())
                {
                    // If order is null, then just choose the first one that fits 
                    if (request.Order == null)
                    {
                        foreach (var order in _assignedOrders)
                            if (order.Serve(item))
                            {
                                // Physically remove the item
                                pod.Remove(item, request);
                                // Block the station for the transfer
                                BlockedUntil = currentTime + ItemTransferTime;
                                // Make the bot wait until completion
                                bot.WaitUntil(currentTime + ItemPickTime);
                                // Count the number of picked items
                                StatNumItemsPicked++;
                                // Keep track of injected item picks
                                if (request.StatInjected)
                                    StatNumInjectedItemsPicked++;
                                // Keep track of current number of items to pick
                                StatCurrentlyOpenItems--;
                                // Count pods served if this is a beginning transaction
                                if (_newPodTransaction) { _newPodTransaction = false; }
                                // Notify instance about the pick
                                Instance.NotifyItemHandled(pod, bot, this, request.Item);
                                Instance.NotifyPodHandled(pod, null, this);
                                // Notify the instance, if the line was completed by the pick
                                if (order.PositionServedCount(item) >= order.PositionOverallCount(item))
                                    Instance.NotifyLineHandled(this, item, order.PositionOverallCount(item));
                                // Keep track of the time at which the transaction will be finished
                                _statLastTimeTransactionFinished = currentTime + ItemTransferTime;
                                // Return success
                                return true;
                            }
                        // Mark request aborted
                        request.Abort();
                    }
                    else
                    {
                        // Order is specified
                        // If it's at this station and the item can be added, then add it
                        if (_assignedOrders.Contains(request.Order) && request.Order.Serve(item))
                        {
                            // Physically remove the item
                            pod.Remove(item, request);
                            // Block the station for the transfer
                            BlockedUntil = currentTime + ItemTransferTime;
                            // Make the bot wait until completion
                            bot.WaitUntil(currentTime + ItemPickTime);
                            // Count the number of picked items
                            StatNumItemsPicked++;
                            // Keep track of injected item picks
                            if (request.StatInjected)
                                StatNumInjectedItemsPicked++;
                            // Keep track of current number of items to pick
                            StatCurrentlyOpenItems--;
                            // Count pods served if this is a beginning transaction
                            if (_newPodTransaction) { _newPodTransaction = false; }
                            // Notify instance about the pick
                            Instance.NotifyItemHandled(pod, bot, this, request.Item);
                            Instance.NotifyPodHandled(pod, null, this);
                            // Notify the instance, if the line was completed by the pick
                            if (request.Order.PositionServedCount(item) >= request.Order.PositionOverallCount(item))
                                Instance.NotifyLineHandled(this, item, request.Order.PositionOverallCount(item));
                            // Keep track of the time at which the transaction will be finished
                            _statLastTimeTransactionFinished = currentTime + ItemTransferTime;
                            // Return success
                            return true;
                        }
                        else
                        {
                            // Mark the request aborted
                            request.Abort();
                        }
                    }
                }
            }
            // Nothing to pick - return unsuccessfully
            return false;
        }

        /// <summary>
        /// Completes an order that is ready, if there is one.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        /// <returns>The completed order if there was one, <code>null</code> otherwise.</returns>
        protected Order RemoveAnyCompletedOrder(double currentTime)
        {
            // Remove any orders that are finished
            Order finishedOrder = null;
            foreach (var order in _assignedOrders)
                if (order.IsCompleted())
                {
                    finishedOrder = order;
                    StatNumOrdersFinished++;
                    // Notify the item manager about this
                    Instance.ItemManager.CompleteOrder(finishedOrder);
                    // Notify completed order
                    Instance.NotifyOrderCompleted(finishedOrder, this);
                    // Break early and block action
                    BlockedUntil = currentTime + OrderCompletionTime;
                    break;
                }
            // Check if the station has no further assigned orders and may rest now
            if (!_assignedOrders.Any())
                _statDepletionTime = currentTime;
            // Remove the finished order from the todo-list
            _assignedOrders.Remove(finishedOrder);
            if (Instance.SettingConfig.VisualizationAttached && finishedOrder != null)
            {
                lock (_syncRoot)
                {
                    // Add order to the completed order list
                    _completedOrders.Add(finishedOrder);
                    // Remove it from the open list
                    _openOrders.Remove(finishedOrder);
                }
            }
            // Return either the completed order or null to signal no order could be completed
            return finishedOrder;
        }

        /// <summary>
        /// Requests the station to pick the given item for the given order.
        /// </summary>
        /// <param name="bot">The bot that requests the pick.</param>
        /// <param name="request">The request to handle.</param>
        public void RequestItemTake(Bot bot, ExtractRequest request)
        {
            if (bot.Pod != null && _assignedOrders.Contains(request.Order))
            {
                // Add the request to the list of requests to handle
                _requestsBot.Enqueue(bot);
                _requestsExtract.Enqueue(request);
            }
            else
            {
                // Something went wrong, refuse to handle the request
                request.Abort();
                bot.WaitUntil(Instance.Controller.CurrentTime + Instance.RefusedRequestPenaltyTime);
            }
        }

        /// <summary>
        /// Contains all pods currently inbound for this station.
        /// </summary>
        private HashSet<Pod> _inboundPods = new HashSet<Pod>();
        /// <summary>
        /// Marks a pod as inbound for a station.
        /// </summary>
        /// <param name="pod">The pod that being brought to the station.</param>
        internal void RegisterInboundPod(Pod pod) { _inboundPods.Add(pod); }
        /// <summary>
        /// Removes a pod from the list of inbound pods.
        /// </summary>
        /// <param name="pod">The pod that is not inbound anymore.</param>
        internal void UnregisterInboundPod(Pod pod) { _inboundPods.Remove(pod); }
        /// <summary>
        /// All pods currently approaching the station.
        /// </summary>
        internal IEnumerable<Pod> InboundPods { get { return _inboundPods; } }

        /// <summary>
        /// All extract tasks that are currently carried out by robots for this station.
        /// </summary>
        private HashSet<ExtractTask> _activeExtractTasks = new HashSet<ExtractTask>();
        /// <summary>
        /// Register an extract task with this station.
        /// </summary>
        /// <param name="task">The task that shall be done at this station.</param>
        internal void RegisterExtractTask(ExtractTask task) { _activeExtractTasks.Add(task); }
        /// <summary>
        /// Unregister an extract task with this station.
        /// </summary>
        /// <param name="task">The task that was done or cancelled for this station.</param>
        internal void UnregisterExtractTask(ExtractTask task) { _activeExtractTasks.Remove(task); }
        /// <summary>
        /// All extract tasks that are registered for being done at this station.
        /// </summary>
        IEnumerable<ExtractTask> ActiveTasks { get { return _activeExtractTasks; } }

        /// <summary>
        /// Register a newly approached bot before picking begins for statistical purposes.
        /// </summary>
        /// <param name="bot">The bot that just approached the station.</param>
        public void RegisterBot(Bot bot)
        {
            _newPodTransaction = true;
            // Track the time it took to serve the last pod
            if (!double.IsNaN(_statLastTimeNewPod))
            {
                if (_statPodHandling == null)
                    _statPodHandling = StatInitPodHandlingDataPoint(_statLastTimeTransactionFinished - _statLastTimeNewPod);
                else
                    StatisticsHelper.UpdateAvgVarData(
                        ref _statPodHandling.PodsHandled,
                        ref _statPodHandling.PodHandlingTimeAvg,
                        ref _statPodHandling.PodHandlingTimeVariance,
                        ref _statPodHandling.PodHandlingTimeMin,
                        ref _statPodHandling.PodHandlingTimeMax,
                        ref _statPodHandling.PodHandlingTimeSum,
                        _statLastTimeTransactionFinished - _statLastTimeNewPod);
            }
            // Log the arrival time of the pod
            _statLastTimeNewPod = Instance.Controller.CurrentTime;
        }

        #endregion

        #region Statistics

        /// <summary>
        /// The number of items handled by this station.
        /// </summary>
        public int StatNumItemsPicked;
        /// <summary>
        /// The number of items picked by this station that were injected to the task of the robot.
        /// </summary>
        public int StatNumInjectedItemsPicked;
        /// <summary>
        /// The number of orders completed at this station.
        /// </summary>
        public int StatNumOrdersFinished;
        /// <summary>
        /// The number of requests currently open (not assigned to a bot) for this station.
        /// </summary>
        internal int StatCurrentlyOpenRequests { get; set; }
        /// <summary>
        /// The number of requests currently open (not assigned to a bot) for this station.
        /// </summary>
        internal int StatCurrentlyOpenQueuedRequests { get; set; }
        /// <summary>
        /// The number of items currently open (not picked yet) for this station.
        /// </summary>
        internal int StatCurrentlyOpenItems { get; private set; }
        /// <summary>
        /// The number of items currently open (not picked yet) and queued for this station.
        /// </summary>
        internal int StatCurrentlyOpenQueuedItems { get; private set; }
        /// <summary>
        /// Contains statistics about the pods handled at this station.
        /// </summary>
        private StationStatisticsDataPoint _statPodHandling;
        /// <summary>
        /// The (sequential) number of pods handled at this station.
        /// </summary>
        public int StatPodsHandled { get { return _statPodHandling == null ? 0 : _statPodHandling.PodsHandled; } }
        /// <summary>
        /// The time it took to handle one pod in average.
        /// </summary>
        public double StatPodHandlingTimeAvg { get { return _statPodHandling == null ? 0 : _statPodHandling.PodHandlingTimeAvg; } }
        /// <summary>
        /// The variance in the handling times of the pods.
        /// </summary>
        public double StatPodHandlingTimeVar { get { return _statPodHandling == null ? 0 : _statPodHandling.PodHandlingTimeVariance; } }
        /// <summary>
        /// The minimal handling time of a pod.
        /// </summary>
        public double StatPodHandlingTimeMin { get { return _statPodHandling == null ? 0 : _statPodHandling.PodHandlingTimeMin; } }
        /// <summary>
        /// The maximal handling time of a pod.
        /// </summary>
        public double StatPodHandlingTimeMax { get { return _statPodHandling == null ? 0 : _statPodHandling.PodHandlingTimeMax; } }
        /// <summary>
        /// Indicates that the next item picked belongs to a new transaction serving one pod.
        /// </summary>
        private bool _newPodTransaction;
        /// <summary>
        /// The item pile-on of this station, i.e. the relative number of items picked from the same pod in one 'transaction'.
        /// </summary>
        public double StatItemPileOn { get { return _statPodHandling == null ? 0 : StatNumItemsPicked / (double)_statPodHandling.PodsHandled; } }
        /// <summary>
        /// The injected item pile-on of this station, i.e. the relative number of injected items picked from the same pod in one 'transaction'.
        /// </summary>
        public double StatInjectedItemPileOn { get { return _statPodHandling == null ? 0 : StatNumInjectedItemsPicked / (double)_statPodHandling.PodsHandled; } }
        /// <summary>
        /// The order pile-on of this station, i.e. the relative number of orders finished from the same pod in one 'transaction'.
        /// </summary>
        public double StatOrderPileOn { get { return _statPodHandling == null ? 0 : StatNumOrdersFinished / (double)_statPodHandling.PodsHandled; } }
        /// <summary>
        /// The time this station was idling.
        /// </summary>
        public double StatIdleTime;
        /// <summary>
        /// The time this station was active.
        /// </summary>
        public double StatActiveTime { get; private set; }
        /// <summary>
        /// The last time the activity of the station was logged.
        /// </summary>
        private double _lastActiveMeasurement = 0;
        /// <summary>
        /// The time this station was shutdown.
        /// </summary>
        public double StatDownTime;
        /// <summary>
        /// The timepoint at which the station completed its last order and may have moved to a rest state.
        /// </summary>
        private double _statDepletionTime = double.PositiveInfinity;
        /// <summary>
        /// Stores the last time when the handling of a pod started.
        /// </summary>
        private double _statLastTimeNewPod = double.NaN;
        /// <summary>
        /// Stores the last time when a transaction was finished.
        /// </summary>
        private double _statLastTimeTransactionFinished = double.NaN;

        /// <summary>
        /// Inits the first datapoint for pod handling.
        /// </summary>
        /// <param name="handlingTime">The time it took to serve the pod.</param>
        /// <returns>The new datapoint.</returns>
        private StationStatisticsDataPoint StatInitPodHandlingDataPoint(double handlingTime)
        {
            return new StationStatisticsDataPoint()
            {
                PodsHandled = 1,
                PodHandlingTimeAvg = handlingTime,
                PodHandlingTimeMax = handlingTime,
                PodHandlingTimeMin = handlingTime,
                PodHandlingTimeSum = handlingTime,
                PodHandlingTimeVariance = 0
            };
        }

        /// <summary>
        /// Resets the statistics.
        /// </summary>
        public void ResetStatistics()
        {
            StatNumOrdersFinished = 0;
            StatNumItemsPicked = 0;
            StatNumInjectedItemsPicked = 0;
            _statPodHandling = null;
            _newPodTransaction = true; // Immediately begin counting of served pods (do not forget the one currently being served)
            _statLastTimeNewPod = double.NaN;
            _statLastTimeTransactionFinished = double.NaN;
            StatIdleTime = 0.0;
            StatActiveTime = 0.0;
            _lastActiveMeasurement = Instance.Controller.CurrentTime;
            StatDownTime = 0.0;
        }

        #endregion

        #region Inherited methods

        /// <summary>
        /// Returns a simple string identifying this object in its instance.
        /// </summary>
        /// <returns>A simple name identifying the instance element.</returns>
        public override string GetIdentfierString() { return "OutputStation" + this.ID; }
        /// <summary>
        /// Returns a simple string giving information about the object.
        /// </summary>
        /// <returns>A simple string.</returns>
        public override string ToString() { return "OutputStation" + this.ID; }

        #endregion

        #region IUpdateable Members

        private double BlockedUntil = -1.0;

        /// <summary>
        /// The next event when this element has to be updated.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns>The next time this element has to be updated.</returns>
        public double GetNextEventTime(double currentTime)
        {
            if (currentTime >= BlockedUntil) return Double.PositiveInfinity;
            else return BlockedUntil;
        }
        /// <summary>
        /// Updates the element to the specified time.
        /// </summary>
        /// <param name="lastTime">The time before the update.</param>
        /// <param name="currentTime">The time to update to.</param>
        public void Update(double lastTime, double currentTime)
        {
            // Track time the station is available for assignments (active)
            if (_lastActiveMeasurement < currentTime)
            {
                // If active, measure time the station was active
                if (Active)
                    StatActiveTime += currentTime - _lastActiveMeasurement;
                // Update the poll
                _lastActiveMeasurement = currentTime;
            }

            // See whether we have to do anything
            if (currentTime < BlockedUntil)
                return;

            // Indicate change at instance
            Instance.Changed = true;

            // Log idle time
            StatIdleTime += currentTime - lastTime;

            // Log down time
            if (currentTime - _statDepletionTime > Instance.SettingConfig.StationShutdownThresholdTime)
                StatDownTime += Math.Min(currentTime - _statDepletionTime, currentTime - lastTime);

            if (RemoveAnyCompletedOrder(currentTime) != null)
                return;

            if (TakeItemFromPod(currentTime))
                return;
        }

        #endregion

        #region IOutputStationInfo Members

        /// <summary>
        /// Returns the active instance belonging to this element.
        /// </summary>
        /// <returns>The active instance.</returns>
        public IInstanceInfo GetInfoInstance() { return Instance; }
        /// <summary>
        /// Gets the current tier this object is placed on. Can't change in case of an immovable object.
        /// </summary>
        /// <returns>The current tier.</returns>
        public ITierInfo GetInfoCurrentTier() { return Tier; }
        /// <summary>
        /// Gets the number of assigned orders.
        /// </summary>
        /// <returns>The number of assigned orders.</returns>
        public int GetInfoAssignedOrders() { return _assignedOrders.Count; }

        private object _syncRoot = new object();
        private List<IOrderInfo> _completedOrders = new List<IOrderInfo>();
        private List<IOrderInfo> _openOrders = new List<IOrderInfo>();
        /// <summary>
        /// Gets all order currently open.
        /// </summary>
        /// <returns>The enumeration of open orders.</returns>
        public IEnumerable<IOrderInfo> GetInfoOpenOrders() { lock (_syncRoot) { return _openOrders.ToList(); } }
        /// <summary>
        /// Gets all orders already completed.
        /// </summary>
        /// <returns>The enumeration of completed orders.</returns>
        public IEnumerable<IOrderInfo> GetInfoCompletedOrders() { lock (_syncRoot) { return _completedOrders.ToList(); } }
        /// <summary>
        /// Gets the capacity this station offers.
        /// </summary>
        /// <returns>The capacity of the station.</returns>
        public double GetInfoCapacity() { return Capacity; }
        /// <summary>
        /// Gets the absolute capacity currently in use.
        /// </summary>
        /// <returns>The capacity in use.</returns>
        public double GetInfoCapacityUsed() { return CapacityInUse; }
        /// <summary>
        /// Indicates the number that determines the overall sequence in which stations get activated.
        /// </summary>
        /// <returns>The order ID of the station.</returns>
        public int GetInfoActivationOrderID() { return ActivationOrderID; }
        /// <summary>
        /// Gets the information queue.
        /// </summary>
        /// <returns>Queue</returns>
        public string GetInfoQueue() { return (Queues == null || Queues.Count == 0) ? "" : Queues.First().Value.Select(w => w.ID.ToString()).Aggregate((current, next) => current + ", " + next); }
        /// <summary>
        /// Indicates whether the station is currently activated (available for new assignments).
        /// </summary>
        /// <returns><code>true</code> if the station is active, <code>false</code> otherwise.</returns>
        public bool GetInfoActive() { return Active; }
        /// <summary>
        /// Indicates whether the station is currently blocked due to activity.
        /// </summary>
        /// <returns><code>true</code> if it is blocked, <code>false</code> otherwise.</returns>
        public bool GetInfoBlocked() { return Instance.Controller.CurrentTime < BlockedUntil; }
        /// <summary>
        /// Gets the remaining time this station is blocked.
        /// </summary>
        /// <returns>The remaining time this station is blocked.</returns>
        public double GetInfoBlockedLeft() { double currentTime = Instance.Controller.CurrentTime; double blockedUntil = BlockedUntil; return currentTime < blockedUntil ? blockedUntil - currentTime : double.NaN; }
        /// <summary>
        /// Gets the of requests currently open (not assigned to a bot) for this station.
        /// </summary>
        /// <returns>The number of active requests.</returns>
        public int GetInfoOpenRequests() { return StatCurrentlyOpenRequests; }
        /// <summary>
        /// Gets the number of queued requests currently open (not assigned to a bot) for this station.
        /// </summary>
        /// <returns>The number of active queued requests.</returns>
        public int GetInfoOpenQueuedRequests() { return StatCurrentlyOpenQueuedRequests; }
        /// <summary>
        /// Gets the number of currently open items (not yet picked) for this station.
        /// </summary>
        /// <returns>The number of open items.</returns>
        public int GetInfoOpenItems() { return StatCurrentlyOpenItems; }
        /// <summary>
        /// Gets the number of currently queued and open items (not yet picked) for this station.
        /// </summary>
        /// <returns>The number of queued open items.</returns>
        public int GetInfoOpenQueuedItems() { return StatCurrentlyOpenQueuedItems; }
        /// <summary>
        /// Gets the number of pods currently incoming to this station.
        /// </summary>
        /// <returns>The number of pods currently incoming to this station.</returns>
        public int GetInfoInboundPods() { return _inboundPods.Count; }

        #endregion

        #region IQueueOwner Members

        /// <summary>
        /// The Queue starting with the nearest way point ending with the most far away one.
        /// </summary>
        /// <value>
        /// The queue.
        /// </value>
        public Dictionary<Waypoint, List<Waypoint>> Queues { get; set; }

        #endregion

        #region IExposeVolatileID

        /// <summary>
        /// An ID that is useful as an index for listing this item.
        /// This ID is unique among all <code>ItemDescription</code>s while being as low as possible.
        /// Note: For now the volatile ID matches the actual ID.
        /// </summary>
        int IExposeVolatileID.VolatileID { get { return VolatileID; } }

        #endregion
    }
}
