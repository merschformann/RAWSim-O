using RAWSimO.Core.Geometrics;
using RAWSimO.Core.Helper;
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
    /// Implements the input-station, i.e. the replenishment station.
    /// </summary>
    public class InputStation : Circle, IUpdateable, IInputStationInfo, IQueuesOwner, IExposeVolatileID
    {
        #region Constructors

        /// <summary>
        /// Creates a new input-station.
        /// </summary>
        /// <param name="instance">The instance this station belongs to.</param>
        internal InputStation(Instance instance) : base(instance) { Active = true; }

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
            Instance?.Controller?.BundleManager?.SignalStationActivated(this);
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
        /// The time it takes to transfer one bundle of items onto a pod.
        /// </summary>
        public double ItemBundleTransferTime;

        /// <summary>
        /// The waypoint this input-station is located at.
        /// </summary>
        public Waypoint Waypoint;

        /// <summary>
        /// The order ID of this station that defines the sequence in which the stations have to be activated.
        /// </summary>
        public int ActivationOrderID;

        /// <summary>
        /// The capacity of this station.
        /// </summary>
        internal double Capacity;

        /// <summary>
        /// The capacity currently in use at this station.
        /// </summary>
        internal double CapacityInUse;

        /// <summary>
        /// The amount of capacity reserved by a controller.
        /// </summary>
        internal double CapacityReserved;

        /// <summary>
        /// Contains information about the items currently contained in this station.
        /// </summary>
        private HashSet<ItemBundle> _itemBundles = new HashSet<ItemBundle>();

        /// <summary>
        /// The set of bundles not yet allocated but already registered with this station.
        /// </summary>
        public HashSet<ItemBundle> RegisteredBundles { get { return _registeredBundles; } }

        /// <summary>
        /// The set of bundles not yet allocated but already registered with this station.
        /// </summary>
        private HashSet<ItemBundle> _registeredBundles = new HashSet<ItemBundle>();

        /// <summary>
        /// Contains information about the items currently contained in this station.
        /// </summary>
        public IEnumerable<ItemBundle> ItemBundles { get { return _itemBundles; } }

        /// <summary>
        /// Checks whether the specified bundle of items can be added for reservation to this station.
        /// </summary>
        /// <param name="bundle">The bundle that has to be checked.</param>
        /// <returns><code>true</code> if the bundle fits, <code>false</code> otherwise.</returns>
        public bool FitsForReservation(ItemBundle bundle) { return CapacityInUse + CapacityReserved + bundle.BundleWeight <= Capacity; }

        /// <summary>
        /// Gets the remaining (unused and not reserved) capacity at this station.
        /// </summary>
        public double RemainingCapacity { get { return Capacity - CapacityInUse - CapacityReserved; } }

        /// <summary>
        /// Reserves capacity of this station for the given bundle. The reserved capacity will be maintained when the bundle is allocated.
        /// </summary>
        /// <param name="bundle">The bundle for which capacity shall be reserved.</param>
        internal void RegisterBundle(ItemBundle bundle)
        {
            _registeredBundles.Add(bundle);
            CapacityReserved = _registeredBundles.Sum(b => b.BundleWeight);
            if (CapacityInUse + CapacityReserved > Capacity)
                throw new InvalidOperationException("Cannot reserve more capacity than this station has!");
        }

        /// <summary>
        /// Adds the specified item to the station.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns><code>true</code> if the item was added successfully, <code>false</code> otherwise.</returns>
        public bool Add(ItemBundle item)
        {
            if (CapacityInUse + item.BundleWeight <= Capacity)
            {
                // Add the bundle
                if (Instance.SettingConfig.VisualizationAttached)
                    lock (_syncRoot)
                        _itemBundles.Add(item);
                else
                    _itemBundles.Add(item);
                // Keep track of capacity
                CapacityInUse = _itemBundles.Sum(b => b.BundleWeight);
                // Remove the bundle from the reservation list
                _registeredBundles.Remove(item);
                CapacityReserved = _registeredBundles.Sum(b => b.BundleWeight);
                // Notify instance about the allocation
                Instance.NotifyBundleAllocated(this, item);
                // Reset down-time
                _statDepletionTime = double.PositiveInfinity;
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
        /// Puts the next possible bundle in the queue on the pod.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        public void GiveBundleToPod(double currentTime)
        {
            while (_requestsInsert.Count > 0)
            {
                Bot bot = _requestsBot.Dequeue();
                Pod pod = bot.Pod;
                InsertRequest request = _requestsInsert.Dequeue();
                ItemBundle bundle = request.Bundle;

                // Can't service the request if the pod is full or not close enough
                if (!pod.Fits(bundle) || GetDistance(pod) > GetInfoRadius())
                    continue;

                // Find matching bundle to transfer
                if (_itemBundles.Contains(bundle))
                {
                    // Transfer bundle, wait until transfer is complete, remove request
                    pod.Add(bundle, request);
                    if (Instance.SettingConfig.VisualizationAttached)
                        lock (_syncRoot)
                        {
                            _itemBundles.Remove(bundle);
                        }
                    else
                        _itemBundles.Remove(bundle);
                    // Keep track of capacity
                    CapacityInUse = _itemBundles.Sum(b => b.BundleWeight);
                    // Check if the station has no further bundles and may rest now
                    if (!_itemBundles.Any())
                        _statDepletionTime = currentTime;
                    // Notify the item manager about this
                    Instance.ItemManager.CompleteBundle(bundle);
                    // Notify instance about this
                    Instance.NotifyBundleStored(this, bot, pod, bundle);
                    Instance.NotifyPodHandled(pod, this, null);
                    // Track the number of transferred bundles
                    StatNumBundlesStored++;
                    // Track the number of transferred injected bundles
                    if (request.StatInjected)
                        StatNumInjectedBundlesStored++;
                    // Count pods served if this is a beginning transaction
                    if (_newPodTransaction) { _newPodTransaction = false; }
                    // Keep track of the time at which the transaction will be finished
                    _statLastTimeTransactionFinished = currentTime + ItemBundleTransferTime;
                    // Block the bot
                    BlockedUntil = currentTime + ItemBundleTransferTime;
                    bot.WaitUntil(BlockedUntil);
                    return;
                }
            }
        }

        /// <summary>
        /// The queue of insertion requests to handle.
        /// </summary>
        public Queue<InsertRequest> _requestsInsert = new Queue<InsertRequest>();
        /// <summary>
        /// The queue of robots that requested a transfer.
        /// </summary>
        private Queue<Bot> _requestsBot = new Queue<Bot>();

        /// <summary>
        /// Requests the station to put the given bundle on the pod.
        /// </summary>
        /// <param name="bot">The robot that issues the request.</param>
        /// <param name="request">The request to handle.</param>
        public void RequestBundle(Bot bot, InsertRequest request)
        {
            if (bot.Pod != null)
            {
                _requestsInsert.Enqueue(request);
                _requestsBot.Enqueue(bot);
            }
        }

        /// <summary>
        /// Register a newly approached bot before putting begins for statistical purposes.
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
        /// The number of bundles stored.
        /// </summary>
        public int StatNumBundlesStored { get; private set; }
        /// <summary>
        /// The number of bundles stored that were injected to the task of the robot.
        /// </summary>
        public int StatNumInjectedBundlesStored { get; private set; }
        /// <summary>
        /// The number of requests currently open (not assigned to a bot) for this station.
        /// </summary>
        internal int StatCurrentlyOpenRequests { get; set; }
        /// <summary>
        /// The number of bundles currently open (not stored yet) for this station.
        /// </summary>
        internal int StatCurrentlyOpenBundles { get { return _itemBundles.Count; } }
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
        /// Indicates that the next item put belongs to a new transaction serving one pod.
        /// </summary>
        private bool _newPodTransaction = false;
        /// <summary>
        /// The bundle pile-on of this station, i.e. the relative number of bundles put on the same pod in one 'transaction'.
        /// </summary>
        public double StatBundlePileOn { get { return _statPodHandling == null ? 0 : StatNumBundlesStored / (double)_statPodHandling.PodsHandled; } }
        /// <summary>
        /// The injected bundle pile-on of this station, i.e. the relative number of injected bundles put on the same pod in one 'transaction'.
        /// </summary>
        public double StatInjectedBundlePileOn { get { return _statPodHandling == null ? 0 : StatNumInjectedBundlesStored / (double)_statPodHandling.PodsHandled; } }
        /// <summary>
        /// The time this station was idling.
        /// </summary>
        public double StatIdleTime { get; private set; }
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
        public double StatDownTime { get; private set; }
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
            StatNumBundlesStored = 0;
            StatNumInjectedBundlesStored = 0;
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
        public override string GetIdentfierString() { return "InputStation" + this.ID; }
        /// <summary>
        /// Returns a simple string giving information about the object.
        /// </summary>
        /// <returns>A simple string.</returns>
        public override string ToString() { return "InputStation" + this.ID; }

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
            if (currentTime >= BlockedUntil)
                return double.PositiveInfinity;
            else
                return BlockedUntil;
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

            // Give the bundles to the waiting pod
            GiveBundleToPod(currentTime);
        }

        #endregion

        #region IInputStationInfo Members

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
        /// Gets the number of assigned bundles.
        /// </summary>
        /// <returns>The number of assigned bundles.</returns>
        public int GetInfoAssignedBundles() { return _itemBundles.Count; }
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
        /// Gets the absolute capacity currently reserved.
        /// </summary>
        /// <returns>The reserved capacity.</returns>
        public double GetInfoCapacityReserved() { return CapacityReserved; }

        private bool _contentChanged = true;

        private object _syncRoot = new object();
        /// <summary>
        /// Gets all bundles currently contained in this station.
        /// </summary>
        /// <returns>The bundles of this station.</returns>
        public IEnumerable<IItemBundleInfo> GetInfoBundles()
        {
            lock (_syncRoot)
            {
                _contentChanged = false;
                return _itemBundles;
            }
        }
        /// <summary>
        /// Indicates whether the content of the station changed.
        /// </summary>
        /// <returns>Indicates whether the content of the station changed.</returns>
        public bool GetInfoContentChanged() { return _contentChanged; }
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
        /// Gets the number of currently open bundles (not yet stored) for this station.
        /// </summary>
        /// <returns>The number of open bundles.</returns>
        public int GetInfoOpenBundles() { return StatCurrentlyOpenBundles; }

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
