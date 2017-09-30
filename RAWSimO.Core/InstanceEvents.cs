using RAWSimO.Core.Control;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Items;
using RAWSimO.Core.Management;
using RAWSimO.Core.Statistics;
using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RAWSimO.Core.Statistics.StationTripDatapoint;

namespace RAWSimO.Core
{
    /// THIS PARTIAL CLASS CONTAINS NOTIFIERS AND EVENTS USED TO INVOKE CALLBACKS IN ORDER TO UPDATE OBSERVERS
    /// <summary>
    /// The core element of each simulation instance.
    /// </summary>
    public partial class Instance
    {
        #region ItemPicked

        /// <summary>
        /// The event handler for the event that is raised when an item is picked.
        /// </summary>
        /// <param name="pod">The pod the item was stored in.</param>
        /// <param name="bot">The bot that is carrying the pod.</param>
        /// <param name="station">The output-station the item was picked at.</param>
        /// <param name="item">The item that was picked.</param>
        public delegate void ItemPickedEventHandler(Pod pod, Bot bot, OutputStation station, ItemDescription item);
        /// <summary>
        /// The event that is raised when an item is picked.
        /// </summary>
        public event ItemPickedEventHandler ItemPicked;
        /// <summary>
        /// Notifies the instance that items were handled in order to keep the statistics up-to-date.
        /// </summary>
        /// <param name="pod">The corresponding pod.</param>
        /// <param name="bot">The corresponding bot.</param>
        /// <param name="oStation">The corresponding station.</param>
        /// <param name="item">The item that was picked.</param>
        internal void NotifyItemHandled(Pod pod, Bot bot, OutputStation oStation, ItemDescription item)
        {
            // Store the number of handled items
            pod.StatItemsHandled++;
            StatOverallItemsHandled++;
            // Mark every item in the history with a timestamp
            _statItemHandlingTimestamps.Add(new ItemHandledDatapoint(Controller.CurrentTime - StatTimeStart, bot.ID, pod.ID, oStation.ID));
            // Flush data points in case there are too many already
            if (_statItemHandlingTimestamps.Count > STAT_MAX_DATA_POINTS)
                StatFlushItemsHandled();
            // Keep track of the maximal number of handled items
            if (StatMaxItemsHandledByPod < pod.StatItemsHandled)
                StatMaxItemsHandledByPod = pod.StatItemsHandled;
            // Raise the event
            ItemPicked?.Invoke(pod, bot, oStation, item);
        }

        #endregion

        #region LinePicked

        /// <summary>
        /// The event handler for the event that is raised when a line is picked.
        /// </summary>
        /// <param name="station">The output-station the item was picked at.</param>
        /// <param name="item">The item that was picked.</param>
        /// <param name="lineCount">The number of items that have been picked for the line.</param>
        public delegate void LinePickedEventHandler(OutputStation station, ItemDescription item, int lineCount);
        /// <summary>
        /// The event that is raised when an item is picked.
        /// </summary>
        public event LinePickedEventHandler LinePicked;
        /// <summary>
        /// Notifies the instance that a line was handled in order to keep the statistics up-to-date.
        /// </summary>
        /// <param name="oStation">The corresponding station.</param>
        /// <param name="item">The item that was picked.</param>
        /// <param name="lineCount">The number of items that have been picked for the line.</param>
        internal void NotifyLineHandled(OutputStation oStation, ItemDescription item, int lineCount)
        {
            // Store the number of handled lines
            StatOverallLinesHandled++;
            // Raise the event
            LinePicked?.Invoke(oStation, item, lineCount);
        }

        #endregion

        #region OrderCompleted

        /// <summary>
        /// The handler for the event that is called when an order is completed.
        /// </summary>
        /// <param name="order">The order that was completed.</param>
        /// <param name="station">The station at which the order was completed.</param>
        public delegate void OrderCompletedEventHandler(Order order, OutputStation station);
        /// <summary>
        /// The event that is raised when an order is completed.
        /// </summary>
        public event OrderCompletedEventHandler OrderCompleted;
        /// <summary>
        /// Notifies the instance that an order was completed in order to keep the statistics up-to-date.
        /// </summary>
        /// <param name="oStation">The corresponding station.</param>
        /// <param name="order">The order that was completed.</param>
        internal void NotifyOrderCompleted(Order order, OutputStation oStation)
        {
            // Store the number of handled items
            StatOverallOrdersHandled++;
            // Check whether the order was completed too late
            if (Controller.CurrentTime - order.DueTime > 0)
                StatOverallOrdersLate++;
            // Mark every item in the history with a timestamp
            _statOrderHandlingTimestamps.Add(
                new OrderHandledDatapoint(StatTime, oStation.ID, Controller.CurrentTime - order.TimeStamp, Controller.CurrentTime - order.TimeStampSubmit, Controller.CurrentTime - order.DueTime));
            // Flush data points in case there are too many already
            if (_statOrderHandlingTimestamps.Count > STAT_MAX_DATA_POINTS)
                StatFlushOrdersHandled();
            // Log turnover time
            _statOrderTurnoverTimes.Add(Controller.CurrentTime - order.TimeStamp);
            // Log throughput time
            _statOrderThroughputTimes.Add(Controller.CurrentTime - order.TimeStampSubmit);
            // Log lateness
            _statOrderLatenessTimes.Add(Controller.CurrentTime - order.DueTime);
            // Raise the event
            OrderCompleted?.Invoke(order, oStation);
        }

        #endregion

        #region Collision

        /// <summary>
        /// The event handler for the event that is raised when a collision occurs.
        /// </summary>
        /// <param name="bot">The bot that detected the collision.</param>
        /// <param name="tier">The tier the collision happened on.</param>
        public delegate void CollisionEventHandler(Bot bot, Tier tier);
        /// <summary>
        /// The event that is raised when a collision occurs.
        /// </summary>
        public event CollisionEventHandler Collision;
        /// <summary>
        /// Notifies the instance that a collision happened in order to keep the statistics up-to-date.
        /// </summary>
        /// <param name="bot">The bot that reports the collision.</param>
        /// <param name="tier">The tier on which the collision happened.</param>
        internal void NotifyCollision(Bot bot, Tier tier)
        {
            // Store the number of collisions
            StatOverallCollisions++;
            // Mark every incident in the history with a timestamp
            _statCollisionTimestamps.Add(new CollisionDatapoint(StatTime, bot.ID, tier.ID));
            // Flush data points in case there are too many already
            if (_statCollisionTimestamps.Count > STAT_MAX_DATA_POINTS)
                StatFlushCollisions();
            // Raise the event
            Collision?.Invoke(bot, tier);
        }

        #endregion

        #region TripCompleted

        /// <summary>
        /// The event handler for the event that is raised when a robot reaches the queueing area of its destination station.
        /// </summary>
        /// <param name="bot">The bot that just reached the queueing area.</param>
        /// <param name="tripType">The type of the trip.</param>
        /// <param name="tripTime">The time it took to reach the queueing area measured from the trip starting waypoint.</param>
        public delegate void TripCompletedEventHandler(Bot bot, StationTripType tripType, double tripTime);
        /// <summary>
        /// The event that is raised when a robot reached the queueing area of its destination station.
        /// </summary>
        public event TripCompletedEventHandler TripCompleted;
        /// <summary>
        /// Notifies the instance that a bot just arrived in the queueing area of its destination station.
        /// </summary>
        /// <param name="bot">The bot that just arrived.</param>
        /// <param name="tripType">The trip type.</param>
        /// <param name="tripTime">The time it took to get to the queueing area.</param>
        internal void NotifyTripCompleted(Bot bot, StationTripType tripType, double tripTime)
        {
            // Add to running statistics
            StatAddTrip(tripType, tripTime);
            // Mark every incident in the history with a timestamp
            _statStationTripTimestamps.Add(new StationTripDatapoint(StatTime, tripTime, tripType));
            // Flush data points in case there are too many already
            if (_statStationTripTimestamps.Count > STAT_MAX_DATA_POINTS)
                StatFlushTripsCompleted();
            // Raise the event
            TripCompleted?.Invoke(bot, tripType, tripTime);
        }

        #endregion

        #region RepositioningStarted

        /// <summary>
        /// The event handler for the event that is raised when a collision occurs.
        /// </summary>
        /// <param name="bot">The bot that detected the collision.</param>
        /// <param name="from">The waypoint from which the pod is brought to another one.</param>
        /// <param name="to">The waypoint to which the pod is being brought.</param>
        /// <param name="pod">The pod that is being repositioned.</param>
        public delegate void RepositioningEventHandler(Bot bot, Waypoint from, Waypoint to, Pod pod);
        /// <summary>
        /// The event that is raised when a collision occurs.
        /// </summary>
        public event RepositioningEventHandler RepositioningStarted;
        /// <summary>
        /// Notifies the instance that a collision happened in order to keep the statistics up-to-date.
        /// </summary>
        /// <param name="bot">The bot that reports the collision.</param>
        /// <param name="from">The waypoint from which the pod is brought to another one.</param>
        /// <param name="to">The waypoint to which the pod is being brought.</param>
        /// <param name="pod">The pod that is being repositioned.</param>
        internal void NotifyRepositioningStarted(Bot bot, Waypoint from, Waypoint to, Pod pod)
        {
            // Store the number of repositionings
            StatRepositioningMoves++;
            // Raise the event
            RepositioningStarted?.Invoke(bot, from, to, pod);
        }

        #endregion

        #region PodClaimed

        /// <summary>
        /// The event handler for the event that is raised when a pod is claimed by a bot.
        /// </summary>
        /// <param name="bot">The bot that claimed the pod.</param>
        /// <param name="pod">The pod that was claimed.</param>
        /// <param name="purpose">The purpose the pod was claimed for.</param>
        public delegate void PodClaimedEventHandler(Pod pod, Bot bot, BotTaskType purpose);
        /// <summary>
        /// The event that is raised when a pod is claimed by a bot.
        /// </summary>
        public event PodClaimedEventHandler PodClaimed;
        /// <summary>
        /// Notifies the instance that a pod was picked up.
        /// </summary>
        /// <param name="pod">The pod that was pickep up.</param>
        /// <param name="bot">The bot that picked up the pod.</param>
        /// <param name="purpose">The purpose the pod was claimed for.</param>
        internal void NotifyPodClaimed(Pod pod, Bot bot, BotTaskType purpose)
        {
            // Raise the event
            PodClaimed?.Invoke(pod, bot, purpose);
        }

        #endregion

        #region PodPickup

        /// <summary>
        /// The event handler for the event that is raised when a pod is picked up.
        /// </summary>
        /// <param name="bot">The bot that picked up the pod.</param>
        /// <param name="pod">The pod that was picked up.</param>
        public delegate void PodPickupEventHandler(Pod pod, Bot bot);
        /// <summary>
        /// The event that is raised when a pod is picked up.
        /// </summary>
        public event PodPickupEventHandler PodPickup;
        /// <summary>
        /// Notifies the instance that a pod was picked up.
        /// </summary>
        /// <param name="pod">The pod that was pickep up.</param>
        /// <param name="bot">The bot that picked up the pod.</param>
        internal void NotifyPodPickup(Pod pod, Bot bot)
        {
            // Raise the event
            PodPickup?.Invoke(pod, bot);
        }

        #endregion

        #region PodSetDown

        /// <summary>
        /// The event handler for the event that is raised when a pod is set down.
        /// </summary>
        /// <param name="bot">The bot that set down the pod.</param>
        /// <param name="pod">The pod that was set down.</param>
        public delegate void PodSetdownEventHandler(Pod pod, Bot bot);
        /// <summary>
        /// The event that is raised when a pod is set down.
        /// </summary>
        public event PodSetdownEventHandler PodSetdown;
        /// <summary>
        /// Notifies the instance that a pod was set down.
        /// </summary>
        /// <param name="pod">The pod that was set down.</param>
        /// <param name="bot">The bot that set down the pod.</param>
        internal void NotifyPodSetdown(Pod pod, Bot bot)
        {
            // Raise the event
            PodSetdown?.Invoke(pod, bot);
        }

        #endregion

        #region PodHandled

        /// <summary>
        /// The event handler for the event that is raised when a pod is being used at a station.
        /// </summary>
        /// <param name="pod">The pod that was used.</param>
        /// <param name="iStation">The input station at which the pod was used or <code>null</code> if it was used at an output station.</param>
        /// <param name="oStation">The output station at which the pod was used or <code>null</code> if it was used at an input station.</param>
        public delegate void PodHandledEventHandler(Pod pod, InputStation iStation, OutputStation oStation);
        /// <summary>
        /// The event that is raised when a pod is being used at a station.
        /// </summary>
        public event PodHandledEventHandler PodHandled;
        /// <summary>
        /// Notifies the instance that a pod was used at a station.
        /// </summary>
        /// <param name="pod">The pod that was used.</param>
        /// <param name="iStation">The input station at which the pod was used or <code>null</code> if it was used at an output station.</param>
        /// <param name="oStation">The output station at which the pod was used or <code>null</code> if it was used at an input station.</param>
        internal void NotifyPodHandled(Pod pod, InputStation iStation, OutputStation oStation)
        {
            // Raise the event
            PodHandled?.Invoke(pod, iStation, oStation);
        }

        #endregion

        #region ItemStorageDecided

        /// <summary>
        /// The handler for the event that is called when a pod was chosen for a bundle.
        /// </summary>
        /// <param name="pod">The chosen pod.</param>
        /// <param name="bundle">The corresponding bundle.</param>
        public delegate void ItemStorageDecidedEventHandler(Pod pod, ItemBundle bundle);
        /// <summary>
        /// The event that is raised when a pod was chosen for a new bundle.
        /// </summary>
        public event ItemStorageDecidedEventHandler ItemStorageDecided;
        /// <summary>
        /// Notifies the instance that a pod was chosen for a bundle.
        /// </summary>
        /// <param name="pod">The chosen pod.</param>
        /// <param name="bundle">The corresponding bundle.</param>
        internal void NotifyItemStorageDecided(Pod pod, ItemBundle bundle)
        {
            // Raise the event
            ItemStorageDecided?.Invoke(pod, bundle);
        }

        #endregion

        #region ItemStorageAllocationAvailable

        /// <summary>
        /// The handler for the event that is called when a bundle was submitted for allocation to a pod.
        /// </summary>
        /// <param name="pod">The pod the bundle was assigned to.</param>
        /// <param name="bundle">The bundle that was assigned.</param>
        public delegate void ItemStorageAllocationAvailableEventHandler(Pod pod, ItemBundle bundle);
        /// <summary>
        /// The event that is raised when a bundle was submitted for allocation to a pod.
        /// </summary>
        public event ItemStorageAllocationAvailableEventHandler ItemStorageAllocationAvailable;
        /// <summary>
        /// Notifies the instance that a bundle was submitted for allocation to a pod.
        /// </summary>
        internal void NotifyItemStorageAllocationAvailable(Pod pod, ItemBundle bundle)
        {
            // Raise the event
            ItemStorageAllocationAvailable?.Invoke(pod, bundle);
        }

        #endregion

        #region ReplenishmentBatchingDecided

        /// <summary>
        /// The handler for the event that is called when a station was chosen for a bundle.
        /// </summary>
        /// <param name="station">The chosen station.</param>
        /// <param name="bundle">The corresponding bundle.</param>
        public delegate void ReplenishmentBatchingDecidedEventHandler(InputStation station, ItemBundle bundle);
        /// <summary>
        /// The event that is raised when a station was chosen for a new bundle.
        /// </summary>
        public event ReplenishmentBatchingDecidedEventHandler ReplenishmentBatchingDecided;
        /// <summary>
        /// Notifies the instance that a station was chosen for a bundle.
        /// </summary>
        /// <param name="station">The chosen station.</param>
        /// <param name="bundle">The corresponding bundle.</param>
        internal void NotifyReplenishmentBatchingDecided(InputStation station, ItemBundle bundle)
        {
            // Raise the event
            ReplenishmentBatchingDecided?.Invoke(station, bundle);
        }

        #endregion

        #region BundleRegistered

        /// <summary>
        /// The handler for the event that is called when a bundle was registered with a pod.
        /// </summary>
        /// <param name="pod">The corresponding pod.</param>
        /// <param name="bundle">The handled bundle.</param>
        public delegate void BundleRegisteredEventHandler(Pod pod, ItemBundle bundle);
        /// <summary>
        /// The event that is raised when a bundle is registered with a pod.
        /// </summary>
        public event BundleRegisteredEventHandler BundleRegistered;
        /// <summary>
        /// Notifies the instance that a bundle was registered with a pod.
        /// </summary>
        /// <param name="pod">The pod the bundle was registered with.</param>
        /// <param name="bundle">The bundle that was moved from the input-station to the pod.</param>
        internal void NotifyBundleRegistered(Pod pod, ItemBundle bundle)
        {
            // Update reserved fill level
            StorageReserved += bundle.BundleWeight;
            StorageBacklog -= bundle.BundleWeight;
            // Raise the event
            BundleRegistered?.Invoke(pod, bundle);
        }

        #endregion

        #region BundleStored

        /// <summary>
        /// The handler for the event that is called when a bundle was stored.
        /// </summary>
        /// <param name="pod">The corresponding pod.</param>
        /// <param name="bot">The corresponding bot.</param>
        /// <param name="iStation">The corresponding station.</param>
        /// <param name="bundle">The handled bundle.</param>
        public delegate void BundleStoredEventHandler(InputStation iStation, Bot bot, Pod pod, ItemBundle bundle);
        /// <summary>
        /// The event that is raised when a bundle is transferred to a pod.
        /// </summary>
        public event BundleStoredEventHandler BundleStored;
        /// <summary>
        /// Notifies the instance that a bundle was placed in a pod.
        /// </summary>
        /// <param name="pod">The pod the bundle was placed in.</param>
        /// <param name="bot">The corresponding bot.</param>
        /// <param name="bundle">The bundle that was moved from the input-station to the pod.</param>
        /// <param name="station">The station the bundle was distributed from.</param>
        internal void NotifyBundleStored(InputStation station, Bot bot, Pod pod, ItemBundle bundle)
        {
            // Store the number of handled bundles
            pod.StatBundlesHandled++;
            StatOverallBundlesHandled++;
            // Mark every bundle in the history with a timestamp
            _statBundleHandlingTimestamps.Add(new BundleHandledDatapoint(StatTime, bot.ID, pod.ID, station.ID, Controller.CurrentTime - bundle.TimeStamp, Controller.CurrentTime - bundle.TimeStampSubmit));
            // Flush data points in case there are too many already
            if (_statBundleHandlingTimestamps.Count > STAT_MAX_DATA_POINTS)
                StatFlushBundlesHandled();
            // Keep track of the maximal number of handled bundles
            if (StatMaxBundlesHandledByPod < pod.StatBundlesHandled)
                StatMaxBundlesHandledByPod = pod.StatBundlesHandled;
            // Log turnover time
            _statBundleTurnoverTimes.Add(Controller.CurrentTime - bundle.TimeStamp);
            // Log throughput time
            _statBundleThroughputTimes.Add(Controller.CurrentTime - bundle.TimeStampSubmit);
            // Update inventory fill level
            StorageUsage += bundle.BundleWeight;
            StorageReserved -= bundle.BundleWeight;
            // Raise the event
            BundleStored?.Invoke(station, bot, pod, bundle);
        }

        #endregion

        #region BundleAllocated

        /// <summary>
        /// The handler for the event that is called when a bundle was allocated to a station.
        /// </summary>
        /// <param name="iStation">The input-station the bundle was assigned to.</param>
        /// <param name="bundle">The bundle that was assigned.</param>
        public delegate void BundleAllocatedEventHandler(InputStation iStation, ItemBundle bundle);
        /// <summary>
        /// The event that is raised when a bundle is allocated to a station.
        /// </summary>
        public event BundleAllocatedEventHandler BundleAllocated;
        /// <summary>
        /// Notifies the instance that a bundle was allocated.
        /// </summary>
        internal void NotifyBundleAllocated(InputStation iStation, ItemBundle bundle)
        {
            // Store the time the bundle was submitted to the system
            bundle.TimeStampSubmit = Controller.CurrentTime;
            // Raise the event
            BundleAllocated?.Invoke(iStation, bundle);
        }

        #endregion

        #region OrderAllocated

        /// <summary>
        /// The handler for the event that is called when an order was allocated to a station.
        /// </summary>
        /// <param name="oStation">The output-station the order was assigned to.</param>
        /// <param name="order">The order that was assigned.</param>
        public delegate void OrderAllocatedEventHandler(OutputStation oStation, Order order);
        /// <summary>
        /// The event that is raised when an order is allocated to a station.
        /// </summary>
        public event OrderAllocatedEventHandler OrderAllocated;
        /// <summary>
        /// Notifies the instance that an order was allocated.
        /// </summary>
        internal void NotifyOrderAllocated(OutputStation oStation, Order order)
        {
            // Store the time the order was submitted to the system
            order.TimeStampSubmit = Controller.CurrentTime;
            // Raise the event
            OrderAllocated?.Invoke(oStation, order);
        }

        #endregion

        #region InitialBundleStored

        /// <summary>
        /// The handler for the event that is called when a bundle is stored during the initialization phase of the inventory.
        /// </summary>
        /// <param name="bundle">The bundle that was stored.</param>
        /// <param name="pod">The pod the bundle was stored in.</param>
        public delegate void InitialBundleStoredEventHandler(ItemBundle bundle, Pod pod);
        /// <summary>
        /// The event that is raised when a bundle is stored during the initialization phase.
        /// </summary>
        public event InitialBundleStoredEventHandler InitialBundleStored;
        /// <summary>
        /// Notifies the instance that a bundle was placed in a pod.
        /// </summary>
        /// <param name="bundle">The bundle that was stored.</param>
        /// <param name="pod">The pod the bundle was placed in.</param>
        internal void NotifyInitialBundleStored(ItemBundle bundle, Pod pod)
        {
            // Raise the event
            InitialBundleStored?.Invoke(bundle, pod);
            // Update inventory fill level
            StorageUsage += bundle.BundleWeight;
        }

        #endregion

        #region ItemExtracted

        /// <summary>
        /// The handler for the event that is called when an item is extracted.
        /// </summary>
        /// <param name="pod">The pod the item was stored in.</param>
        /// <param name="item">The item that was extracted.</param>
        public delegate void ItemExtractedEventHandler(Pod pod, ItemDescription item);
        /// <summary>
        /// The event that is raised when an item was extracted.
        /// </summary>
        public event ItemExtractedEventHandler ItemExtracted;
        /// <summary>
        /// Notifies the instance that an item was extracted.
        /// </summary>
        /// <param name="pod">The pod the item was stored in.</param>
        /// <param name="item">The item that was extracted.</param>
        internal void NotifyItemExtracted(Pod pod, ItemDescription item)
        {
            // Update inventory fill level
            StorageUsage -= item.Weight;
            // Raise the event
            ItemExtracted(pod, item);
        }

        #endregion

        #region PodItemReserved

        /// <summary>
        /// The handler for the event that is called when an item on a pod is reserved for picking.
        /// </summary>
        /// <param name="pod">The pod the item was stored in.</param>
        /// <param name="item">The item that was extracted.</param>
        /// <param name="request">The request the item was reserved for.</param>
        public delegate void PodItemReservedEventHandler(Pod pod, ItemDescription item, ExtractRequest request);
        /// <summary>
        /// The event that is raised when an item on a pod is reserved for picking.
        /// </summary>
        public event PodItemReservedEventHandler PodItemReserved;
        /// <summary>
        /// Notifies the instance that an item on a pod is reserved for picking.
        /// </summary>
        /// <param name="pod">The pod the item was stored in.</param>
        /// <param name="item">The item that was extracted.</param>
        /// <param name="request">The request the item was reserved for.</param>
        internal void NotifyPodItemReserved(Pod pod, ItemDescription item, ExtractRequest request)
        {
            // Raise the event
            PodItemReserved?.Invoke(pod, item, request);
        }

        #endregion

        #region PodItemUnreserved

        /// <summary>
        /// The handler for the event that is called when a reservation for an item on a pod is canceled.
        /// </summary>
        /// <param name="pod">The pod the item was stored in.</param>
        /// <param name="item">The item that was extracted.</param>
        /// <param name="request">The request the item was reserved for.</param>
        public delegate void PodItemUnreservedEventHandler(Pod pod, ItemDescription item, ExtractRequest request);
        /// <summary>
        /// The event that is raised when a reservation for an item on a pod is canceled.
        /// </summary>
        public event PodItemUnreservedEventHandler PodItemUnreserved;
        /// <summary>
        /// Notifies the instance that the reservation for an item on a pod is canceled.
        /// </summary>
        /// <param name="pod">The pod the item was stored in.</param>
        /// <param name="item">The item that was extracted.</param>
        /// <param name="request">The request the item was reserved for.</param>
        internal void NotifyPodItemUnreserved(Pod pod, ItemDescription item, ExtractRequest request)
        {
            // Raise the event
            PodItemUnreserved?.Invoke(pod, item, request);
        }

        #endregion

        #region NewBundle

        /// <summary>
        /// Handles the event that is raised when a new bundle becomes available.
        /// </summary>
        /// <param name="bundle">The new bundle.</param>
        public delegate void NewBundleEventHandler(ItemBundle bundle);
        /// <summary>
        /// The event that is raised when a new bundle becomes available.
        /// </summary>
        public event NewBundleEventHandler NewBundle;
        /// <summary>
        /// Notifies the instance about a new available bundle.
        /// </summary>
        /// <param name="bundle">The bundle that become available.</param>
        internal void NotifyBundlePlaced(ItemBundle bundle)
        {
            // Only track if there really was a bundle generated
            if (bundle != null)
            {
                // Keep track of overall bundles placed
                StatOverallBundlesPlaced++;
                // Raise the event
                NewBundle?.Invoke(bundle);
                // Mark every new bundle in the history with a timestamp
                _statBundlePlacementTimestamps.Add(new BundlePlacedDatapoint(this.Controller == null ? 0 : StatTime));
                // Flush data points in case there are too many already
                if (_statBundlePlacementTimestamps.Count > STAT_MAX_DATA_POINTS)
                    StatFlushBundlesPlaced();
                // Keep track of backlog storage capacity usage
                StorageBacklog += bundle.BundleWeight;
            }
        }

        #endregion

        #region NewOrder

        /// <summary>
        /// Handles the event that is raised when a new order becomes available.
        /// </summary>
        /// <param name="order">The new order.</param>
        public delegate void NewOrderEventHandler(Order order);
        /// <summary>
        /// The event that is raised when a new order becomes available.
        /// </summary>
        public event NewOrderEventHandler NewOrder;
        /// <summary>
        /// Notifies the instance that an order was placed.
        /// </summary>
        /// <param name="order">The order that was placed.</param>
        internal void NotifyOrderPlaced(Order order)
        {
            // Only track if there really was an order generated
            if (order != null)
            {
                // Keep track of overall orders placed
                StatOverallOrdersPlaced++;
                // Keep track of overall item count
                StatOverallItemsOrdered += order.Positions.Sum(p => p.Value);
                // Generate requests for the order
                ResourceManager.CreateExtractRequests(order);
                // --> Raise event
                NewOrder?.Invoke(order);
                // Mark every new order in the history with a timestamp
                _statOrderPlacementTimestamps.Add(new OrderPlacedDatapoint(this.Controller == null ? 0 : StatTime));
                // Flush data points in case there are too many already
                if (_statOrderPlacementTimestamps.Count > STAT_MAX_DATA_POINTS)
                    StatFlushOrdersPlaced();
            }
        }

        #endregion

        #region RejectedBundle

        /// <summary>
        /// Handles the event that is raised when a bundle was rejected.
        /// </summary>
        public delegate void RejectedBundleEventHandler();
        /// <summary>
        /// The event that is raised when a bundle was rejected.
        /// </summary>
        public event RejectedBundleEventHandler RejectedBundle;
        /// <summary>
        /// Notifies the instance about a rejected bundle.
        /// </summary>
        internal void NotifyBundleRejected()
        {
            StatOverallBundlesRejected++;
            RejectedBundle?.Invoke();
        }

        #endregion

        #region RejectedOrder

        /// <summary>
        /// Handles the event that is raised when an order was rejected.
        /// </summary>
        public delegate void RejectedOrderEventHandler();
        /// <summary>
        /// The event that is raised when an order was rejected.
        /// </summary>
        public event RejectedOrderEventHandler RejectedOrder;
        /// <summary>
        /// Notifies the instance that an order was rejected.
        /// </summary>
        internal void NotifyOrderRejected()
        {
            StatOverallOrdersRejected++;
            RejectedOrder?.Invoke();
        }

        #endregion

        #region NewPod

        /// <summary>
        /// Handles the event that is raised when a new pod is added to the system.
        /// </summary>
        /// <param name="pod">The new pod.</param>
        public delegate void NewpodEventHandler(Pod pod);
        /// <summary>
        /// The event that is raised when a new pod becomes available.
        /// </summary>
        public event NewpodEventHandler NewPod;
        /// <summary>
        /// Notifies the instance that a new pod was added.
        /// </summary>
        /// <param name="pod">The pod that was added.</param>
        internal void NotifyPodAdded(Pod pod)
        {
            // --> Raise event
            NewPod?.Invoke(pod);
        }

        #endregion

        #region BundleGenerationPause

        /// <summary>
        /// Handles the event that is raised when bundle generation was paused.
        /// </summary>
        public delegate void BundleGenerationPausedEventHandler();
        /// <summary>
        /// The event that is raised when bundle generation was paused.
        /// </summary>
        public event BundleGenerationPausedEventHandler BundleGenerationPaused;
        /// <summary>
        /// Notifies the instance that bundle generation is paused.
        /// </summary>
        internal void NotifyBundleGenerationPaused()
        {
            // --> Raise event
            BundleGenerationPaused?.Invoke();
            // Count
            StatBundleGenerationStops++;
        }

        #endregion

        #region OrderGenerationPause

        /// <summary>
        /// Handles the event that is raised when order generation was paused.
        /// </summary>
        public delegate void OrderGenerationPausedEventHandler();
        /// <summary>
        /// The event that is raised when order generation was paused.
        /// </summary>
        public event OrderGenerationPausedEventHandler OrderGenerationPaused;
        /// <summary>
        /// Notifies the instance that order generation is paused.
        /// </summary>
        internal void NotifyOrderGenerationPaused()
        {
            // --> Raise event
            OrderGenerationPaused?.Invoke();
            // Count
            StatOrderGenerationStops++;
        }

        #endregion
    }
}
