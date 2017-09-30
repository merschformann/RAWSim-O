using RAWSimO.Core.Bots;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.Items;
using RAWSimO.Core.Management;
using RAWSimO.Core.Metrics;
using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.TaskAllocation
{
    /// <summary>
    /// A brute-force manager deciding the next task for each bot by checking all possible tasks
    /// for the one with the minimal time for storing the current pod, picking up the next one
    /// and bringing it to the task's destination station.
    /// 
    /// <remarks>This manager overrides any storage assignment or pod storage assignment manager.</remarks>
    /// </summary>
    public class BruteForceBotManager : BotManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public BruteForceBotManager(Instance instance) : base(instance) { }

        bool _deliverMode = true;

        /// <summary>
        /// Determines the next task for the bot.
        /// </summary>
        /// <param name="bot">The bot to get a task for.</param>
        protected override void GetNextTask(Bot bot)
        {
            if (_deliverMode)
            {
                if (DoExtractTask(bot))
                    return;

                if (DoStoreTask(bot))
                {
                    _deliverMode = false;
                    return;
                }
            }
            else
            {
                if (DoStoreTask(bot))
                    return;

                if (DoExtractTask(bot))
                {
                    _deliverMode = true;
                    return;
                }
            }

            GetOutOfTheWay(bot);
        }

        #region Extract

        /// <summary>
        /// Finds the best item delivery task and then executes it.
        /// </summary>
        /// <returns><code>false</code> if no task available, <code>true</code> otherwise.</returns>
        public bool DoExtractTask(Bot bot)
        {
            if (!Instance.ResourceManager.AvailableAndAssignedExtractRequests.Any())
                return false;

            // Fetch the best extraction job
            ExtractRequest bestRequest;
            Pod bestPod;
            ItemDescription bestItemDescription;
            OutputStation bestStation;
            Waypoint bestStorageLocationOldPod;
            GetBestDeliveryTask(bot, out bestRequest, out bestPod, out bestItemDescription, out bestStation, out bestStorageLocationOldPod);

            // Ensure that bot is either carrying the right pod or no pod
            if (bot.Pod == null)
            {
                // No good pods!
                if (bestPod == null)
                {
                    return false;
                }
            }
            else
            { // Already have a pod... see if it's the best one
                // If don't have the best pod for the job, then store the old one first
                if (bot.Pod != bestPod)
                {
                    if (bestStorageLocationOldPod != null)
                        EnqueueParkPod(bot, bot.Pod, bestStorageLocationOldPod);
                    else
                        StorePodAtClosestStorageLocation(bot);
                    return true;
                }
            }
            // Enqueue the new extraction task
            EnqueueExtract(bot, bestStation, bestPod, new List<ExtractRequest> { bestRequest });
            return true;
        }

        /// <summary>
        /// Determines the best delivery task for the given bot, and allocates the required resources.
        /// </summary>
        /// <param name="bot">The bot to consider.</param>
        /// <param name="bestPod">The pod to use for the best delivery task.</param>
        /// <param name="bestItemDescription">The type of the item to use for the best delivery task.</param>
        /// <param name="bestRequest">The request to use for the best delivery task.</param>
        /// <param name="bestStation">The station to use for the best delivery task.</param>
        /// <param name="bestStorageLocationOldPod">The storage location for the current pod to use for the best delivery task.</param>
        public void GetBestDeliveryTask(Bot bot,
            out ExtractRequest bestRequest,
            out Pod bestPod,
            out ItemDescription bestItemDescription,
            out OutputStation bestStation,
            out Waypoint bestStorageLocationOldPod)
        {
            // Find an item to retrieve
            double bestTaskTime = double.PositiveInfinity;
            bestPod = null;
            bestStorageLocationOldPod = null;

            // Try with the current pod
            ExtractRequest bestExtractTask; double bestExtractTime;
            GetBestTaskForPod(bot, bot.Pod, bot.CurrentWaypoint, out bestExtractTask, out bestExtractTime);
            bestRequest = bestExtractTask;
            if (bestRequest != null)
            {
                bestTaskTime = bestExtractTime;
                bestPod = bot.Pod;
            }

            // Time to pick up the pod
            double baseTime = bot.PodTransferTime;
            // Time to set down the current pod if it has one
            if (bot.Pod != null)
                baseTime += bot.PodTransferTime;

            foreach (var pod in Instance.ResourceManager.UnusedPods)
            {
                double time = baseTime;
                Waypoint bestStorageLocationForThisPod = null;

                // If need to set down current pod
                if (bot.Pod != null)
                {
                    // Grab this once to pull it out of the loop
                    Waypoint targetPodWaypoint = pod.Waypoint;

                    double minStorageTime = double.PositiveInfinity;
                    foreach (var storageLocation in Instance.ResourceManager.UnusedPodStorageLocations)
                    {
                        // Need time to set down current pod and also pick up new pod
                        double storeTime = 2 * bot.PodTransferTime;

                        // Find time to get to storage location
                        storeTime += Estimators.EstimateTravelTimeEuclid(bot, storageLocation);
                        // Find time to get from storage location to new pod
                        storeTime += Estimators.EstimateTravelTimeEuclid(bot, storageLocation, targetPodWaypoint);

                        if (storeTime < minStorageTime)
                        {
                            minStorageTime = storeTime;
                            bestStorageLocationForThisPod = storageLocation;
                        }
                    }

                    // Use the best waypoint
                    time += minStorageTime;
                }
                else
                { // No pod, just go pick it up
                    time += Estimators.EstimateTravelTimeEuclid(bot, pod.Waypoint);
                }

                // Get place to take the pod
                GetBestTaskForPod(bot, pod, pod.Waypoint, out bestExtractTask, out bestExtractTime);
                if (bestExtractTask != null)
                {
                    // If the time is still better, then count it
                    if (time + bestExtractTime < bestTaskTime)
                    {
                        bestRequest = bestExtractTask;
                        bestTaskTime = time + bestExtractTime;
                        bestPod = pod;
                        bestStorageLocationOldPod = bestStorageLocationForThisPod;
                    }
                }
            }

            // If found no task, take the oldest
            if (bestRequest == null)
                bestRequest = Instance.ResourceManager.AvailableAndAssignedExtractRequests.First();

            bestItemDescription = bestRequest.Item;
            bestStation = bestRequest.Station;
        }

        /// <summary>
        /// Finds the best delivery task for the specified pod.
        /// Sets <code>bestDeliveryRequest</code> to null if none found, otherwise <code>bestDeliveryRequest</code> and <code>bestTimeForDeliveryRequest</code> are initialized.
        /// </summary>
        /// <param name="bot">The bot to consider.</param>
        /// <param name="pod">Pod to take.</param>
        /// <param name="podLocation">Current location of the pod.</param>
        /// <param name="bestExtractTask">The best extract task set by this method.</param>
        /// <param name="bestTimeForExtract">The time of the best extract set by this method.</param>
        void GetBestTaskForPod(Bot bot, Pod pod, Waypoint podLocation,
            out ExtractRequest bestExtractTask, out double bestTimeForExtract)
        {
            bestExtractTask = null;
            bestTimeForExtract = 0.0;

            bestExtractTask = null;
            if (pod == null || podLocation == null)
                return;

            bestTimeForExtract = double.PositiveInfinity;

            // Check all tasks
            foreach (var delivery in Instance.ResourceManager.AvailableAndAssignedExtractRequests)
            {
                // If it has the item
                if (pod.IsContained(delivery.Item))
                {
                    // See how long it would take to get to the output-station
                    // Choose the worst of delivering or waiting
                    Waypoint sw = delivery.Station.Waypoint;
                    double time = Math.Max(Estimators.EstimateTravelTimeEuclid(bot, podLocation, sw), Estimators.EstimateOutputStationWaitTime(bot, sw));

                    // If it's the best, then use it
                    if (time < bestTimeForExtract)
                    {
                        bestExtractTask = delivery;
                        bestTimeForExtract = time;
                    }
                }
            }
        }

        #endregion

        #region Store

        /// <summary>
        /// Finds the best item-bundle pickup task and then executes it.
        /// </summary>
        /// <returns><code>false</code> if no task available, <code>true</code> otherwise.</returns>
        public bool DoStoreTask(Bot bot)
        {
            // Make sure there's a bundle available to get
            if (!Instance.ResourceManager.AvailableStoreRequests.Any())
                return false;

            Pod bestPod;
            InputStation bestStation;
            InsertRequest bestRequest;
            ReserveBestItemToPickUp(bot, out bestRequest, out bestPod, out bestStation);
            // Check whether bot already carries a pod
            if (bot.Pod == null)
            {
                // Check whether there is a task at all
                if (bestPod == null)
                {
                    // Cancel task reservation and store the pod
                    return false;
                }
            }
            else
            { // Already have a pod ... see if it's the best one
                // If don't have the best pod for the job, then store the old one
                if (bot.Pod != bestPod)
                {
                    // Cancel task reservation and store the pod
                    StorePodAtClosestStorageLocation(bot);
                    return true;
                }
            }
            // Enqueue the new storage task
            EnqueueInsert(bot, bestStation, bestPod, new List<InsertRequest> { bestRequest });
            return true;
        }

        /// <summary>
        /// Based on the current situation, the bot needs to get an item, but doesn't have it.
        /// This function reserves the item the pod should pick up. Returns the pod that should be used, null if it's carying a pod and the current pod won't work (too full).
        /// </summary>
        /// <param name="bot">The bot to consider.</param>
        /// <param name="bestRequest">The best request to use for the best pickup task.</param>
        /// <param name="bestStation">The station to use for the best pickup task.</param>
        /// <param name="bestPod">The pod to use for the best pickup task.</param>
        public void ReserveBestItemToPickUp(Bot bot, out InsertRequest bestRequest, out Pod bestPod, out InputStation bestStation)
        {
            bestPod = null;
            bestStation = null;
            bestRequest = null;

            // If have a pod currently
            if (bot.Pod != null)
            {
                Pod pod = bot.Pod;

                // Current pod works, so find closest input station and best task
                double closestInputStationTime = double.PositiveInfinity;

                // Check all tasks
                foreach (var storeTask in Instance.ResourceManager.AvailableStoreRequests)
                {
                    // See how long it would take to get to this input station
                    // Choose the worst of delivering or waiting
                    Waypoint sw = storeTask.Station.Waypoint;
                    double time = Math.Max(Estimators.EstimateTravelTimeEuclid(bot, bot.CurrentWaypoint, sw), Estimators.EstimateInputStationWaitTime(bot, sw));

                    // If it's the best and it fits the current pod, then use it
                    if (time < closestInputStationTime && pod.CapacityInUse + storeTask.Bundle.BundleWeight <= pod.Capacity)
                    {
                        bestRequest = storeTask;
                        closestInputStationTime = time;
                    }
                }

                // If no pickup suits the pod, get rid of it
                if (bestRequest == null)
                    return;

                // Allocate task!
                bestStation = bestRequest.Station;
                bestPod = pod;
            }

            // Don't have a pod
            double bestPickupTime = double.PositiveInfinity;
            bestRequest = null;
            bestPod = null;

            foreach (var pod in Instance.ResourceManager.UnusedPods)
            {
                // Find time to pick up the bundle
                double pickupTime = Estimators.EstimateTravelTimeEuclid(bot, pod.Waypoint);

                // Check all tasks
                foreach (var storeTask in Instance.ResourceManager.AvailableStoreRequests)
                {
                    // If it has room
                    if (pod.CapacityInUse + storeTask.Bundle.BundleWeight <= pod.Capacity)
                    {
                        // See how long it would take to get to this input station
                        // Choose the worst of delivering or waiting
                        Waypoint sw = storeTask.Station.Waypoint;
                        double deliverTime = Math.Max(Estimators.EstimateTravelTimeEuclid(bot, pod.Waypoint, sw), Estimators.EstimateInputStationWaitTime(bot, sw));

                        //if it's the best, then use it
                        if (pickupTime + deliverTime < bestPickupTime)
                        {
                            bestRequest = storeTask;
                            bestPickupTime = pickupTime + deliverTime;
                            bestPod = pod;
                        }
                    }
                }
            }

            // No pickup request available
            if (bestRequest == null)
                return;

            // Pickup available - set it
            bestStation = bestRequest.Station;
        }

        #endregion

        #region Commands

        /// <summary>
        /// Tells the bot to store its current pod at the closest waypoint.
        /// </summary>
        /// <param name="bot">The bot to consider.</param>
        public void StorePodAtClosestStorageLocation(Bot bot)
        {
            // Find closest free storage location
            Waypoint closest = null;
            double closestDistance = double.PositiveInfinity;
            foreach (var w in Instance.ResourceManager.UnusedPodStorageLocations)
            {
                double distance = bot.GetDistance(w);
                // If it's closer than the previous closest, then use this new one instead
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = w;
                }
            }
            // Enqueue task
            EnqueueParkPod(bot, bot.Pod, closest);
        }

        /// <summary>
        /// Tells the bot to store its current pod at the closest location (if it has one), and then go resting to stay out of the way of other bots.
        /// </summary>
        /// <param name="bot">The bot to consider.</param>
        void GetOutOfTheWay(Bot bot)
        {
            // If carrying a pod dispose it first
            if (bot.Pod != null)
            {
                StorePodAtClosestStorageLocation(bot);
                return;
            }

            // Find closest free storage location
            Waypoint closest = null;
            double closestDistance = double.PositiveInfinity;
            foreach (var w in Instance.ResourceManager.UnusedRestingLocations)
            {
                double distance = bot.GetDistance(w);
                // If it's closer than the previous closest, then use this new one instead
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = w;
                }
            }

            // Rest at the closest WP
            EnqueueRest(bot, closest);
        }

        #endregion

        #region IUpdateable Members

        /// <summary>
        /// The next event when this element has to be updated.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns>The next time this element has to be updated.</returns>
        public override double GetNextEventTime(double currentTime) { return double.PositiveInfinity; }
        /// <summary>
        /// Updates the element to the specified time.
        /// </summary>
        /// <param name="lastTime">The time before the update.</param>
        /// <param name="currentTime">The time to update to.</param>
        public override void Update(double lastTime, double currentTime) { }

        #endregion

        #region IOptimize Members

        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        public override void SignalCurrentTime(double currentTime) { /* Ignore since this simple manager is always ready. */ }

        #endregion
    }
}
