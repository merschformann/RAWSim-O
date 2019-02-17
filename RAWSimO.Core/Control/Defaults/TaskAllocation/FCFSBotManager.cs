using RAWSimO.Core.Configurations;
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
    /// Implements a manager assigning tasks based on first arrival rate to the robots.
    /// </summary>
    public class FCFSBotManager : BotManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public FCFSBotManager(Instance instance)
            : base(instance)
        {
            _decideExtractMode = () => { return Instance.Randomizer.NextDouble() < ((double)instance.OutputStations.Count / (double)(instance.InputStations.Count + instance.OutputStations.Count)); };
            _extractMode = instance.Bots.ToDictionary(k => k, v => _decideExtractMode());
            _config = instance.ControllerConfig.TaskAllocationConfig as FCFSTaskAllocationConfiguration;
        }

        private FCFSTaskAllocationConfiguration _config;
        private Func<bool> _decideExtractMode;
        private Dictionary<Bot, bool> _extractMode;

        private double GetOrderValue(InputStation iStation, Bot bot) { return _config.PreferSameTier && iStation.Tier == bot.Tier ? -Instance.Randomizer.NextDouble() : Instance.Randomizer.NextDouble(); }
        private double GetOrderValue(OutputStation oStation, Bot bot) { return _config.PreferSameTier && oStation.Tier == bot.Tier ? -Instance.Randomizer.NextDouble() : Instance.Randomizer.NextDouble(); }
        private double GetOrderValue(Pod pod, Bot bot) { return _config.PreferSameTier && pod.Tier == bot.Tier ? -Instance.Randomizer.NextDouble() : Instance.Randomizer.NextDouble(); }

        private bool DoExtractTaskWithPod(Bot bot, Pod pod)
        {
            // Bring items to the randomly selected first fitting station
            foreach (var oStation in Instance.OutputStations.OrderBy(s => GetOrderValue(s, bot)))
            {
                // Search for requests matching the items of the pod
                List<ExtractRequest> fittingRequests = GetPossibleRequests(pod, oStation, PodSelectionExtractRequestFilteringMode.AssignedOnly);
                if (fittingRequests.Any())
                {
                    ExtractRequest oldestRequest = fittingRequests.OrderBy(o => o.Order.TimeStamp).First();
                    // Simply execute the next task with the pod
                    EnqueueExtract(
                        bot, // The bot itself
                        oStation, // The random station
                        pod, // Keep the pod
                        new List<ExtractRequest> { oldestRequest }); // The first requests to serve

                    // Finished search for next task
                    return true;
                }
            }
            // No fitting request
            return false;
        }

        private bool DoStoreTaskWithPod(Bot bot, Pod pod)
        {
            // Fetch bundles from the randomly selected first station
            foreach (var iStation in Instance.InputStations.OrderBy(s => GetOrderValue(s, bot)))
            {
                // Search for requests fitting the pod
                List<InsertRequest> fittingRequests = GetPossibleRequests(pod, iStation);
                if (fittingRequests.Any())
                {
                    // Simply execute the next task with the pod
                    EnqueueInsert(bot, iStation, pod, fittingRequests.ToList());
                    // Finished search for next task
                    return true;
                }
            }
            // No request fits the pod
            return false;
        }
        /// <summary>
        /// Main decision routine that determines the next task the bot will do.
        /// </summary>
        /// <param name="bot">The bot to assign a task to.</param>
        protected override void GetNextTask(Bot bot)
        {
            // Switch mode (I/O) randomly
            //if (Instance.Randomizer.NextDouble() > _config.StickToModeProbability)
            //    _extractMode[bot] = !_extractMode[bot];
            _extractMode[bot] = true;

            // Randomly decide whether to keep the current pod (if there is one) or switch to another
            bool keepPod = bot.Pod != null && Instance.Randomizer.NextDouble() < _config.StickToPodProbability;
            // Get the last task that was assigned to the bot
            BotTask lastTask = GetLastEnqueuedTask(bot);


            if (keepPod)
            {
                if (_extractMode[bot])
                {
                    // Try to do another extract task
                    if (DoExtractTaskWithPod(bot, bot.Pod))
                        // Successfully allocated next task
                        return;
                    // Try other mode if allowed
                    //if (_config.SwitchModeIfNoWork && DoStoreTaskWithPod(bot, bot.Pod))
                    //    // Successfully allocated next task
                    //    return;
                }
                else
                {
                    // Try to do another store task
                    if (DoStoreTaskWithPod(bot, bot.Pod))
                        // Successfully allocated next task
                        return;
                    // Try other mode if allowed
                    if (_config.SwitchModeIfNoWork && DoExtractTaskWithPod(bot, bot.Pod))
                        // Successfully allocated next task
                        return;
                }

                // No job available for the pod - just put the pod back to inventory
                EnqueueParkPod(bot, bot.Pod, Instance.Controller.PodStorageManager.GetStorageLocation(bot.Pod));
                return;
            }
            else
            {
                // Check whether there is a pod
                if (bot.Pod != null)
                {
                    // There is a pod but we don't want it anymore
                    EnqueueParkPod(bot, bot.Pod, Instance.Controller.PodStorageManager.GetStorageLocation(bot.Pod));
                    return;
                }
            }

            // --> Get a pod which offers a job
            if (_extractMode[bot])
            {
                // Try to do extract job with any pod
                foreach (var pod in Instance.ResourceManager.UnusedPods.OrderBy(b => GetOrderValue(b, bot)))
                    // Try to do extract task with this pod
                    if (DoExtractTaskWithPod(bot, pod))
                        // Successfully allocated next task
                        return;
                // Try other mode if allowed
                //if (_config.SwitchModeIfNoWork)
                //    // Try to do store job with any pod
                //    foreach (var pod in Instance.ResourceManager.UnusedPods.OrderBy(b => GetOrderValue(b, bot)))
                //        // Try to do another store task
                //        if (DoStoreTaskWithPod(bot, pod))
                //            // Successfully allocated next task
                //            return;
                GetOutOfTheWay(bot);
                // Choose resting location
                //Waypoint restingLocation =
                //// Check whether the last task was resting too
                //    lastTask != null && lastTask.Type == BotTaskType.Rest && Instance.ResourceManager.IsRestingLocationAvailable(bot.CurrentWaypoint) ?
                //// We already rested before and did not move since then - simply stay at the current resting location
                //    bot.CurrentWaypoint :
                //// We need to choose a new resting location
                ////Instance.ResourceManager.UnusedRestingLocations.ElementAt(Instance.Randomizer.NextInt(Instance.ResourceManager.UnusedRestingLocations.Count()));
                //    getCloestRestPoint(bot);
                //if (lastTask != null) Console.Write(lastTask.Type.ToString(), Instance.ResourceManager.IsRestingLocationAvailable(bot.CurrentWaypoint));
                //Console.Write(bot.CurrentWaypoint == restingLocation);
                //// No job in this mode available right now - chill until mode switch or task gets available
                //EnqueueRest(bot, restingLocation);
                if (!_extractMode[bot])
                {
                    Console.Write(_extractMode[bot]);
                }
            }
            else
            {
                if (!_extractMode[bot])
                {
                    Console.Write(_extractMode[bot]);
                }
                // Try to do store job with any pod
                foreach (var pod in Instance.ResourceManager.UnusedPods.OrderBy(b => GetOrderValue(b, bot)))
                    // Try to do another store task
                    if (DoStoreTaskWithPod(bot, pod))
                        // Successfully allocated next task
                        return;
                // Try other mode if allowed
                //if (_config.SwitchModeIfNoWork)
                //    // Try to do store job with any pod
                //    foreach (var pod in Instance.ResourceManager.UnusedPods.OrderBy(b => GetOrderValue(b, bot)))
                //        // Try to do extract task with this pod
                //        if (DoExtractTaskWithPod(bot, pod))
                //            // Successfully allocated next task
                //            return;
                GetOutOfTheWay(bot);
                // Choose resting location
                //Waypoint restingLocation =
                //    // Check whether the last task was resting too
                //    lastTask != null && lastTask.Type == BotTaskType.Rest && Instance.ResourceManager.IsRestingLocationAvailable(bot.CurrentWaypoint) ?
                //    // We already rested before and did not move since then - simply stay at the current resting location
                //    bot.CurrentWaypoint :
                //    // We need to choose a new resting location
                //    //Instance.ResourceManager.UnusedRestingLocations.ElementAt(Instance.Randomizer.NextInt(Instance.ResourceManager.UnusedRestingLocations.Count()));
                //    getCloestRestPoint(bot);

                //// No job in this mode available right now - chill until mode switch or task gets available
                //EnqueueRest(bot, restingLocation);
            }
            //GetOutOfTheWay(bot);
            // Choose resting location
            //Waypoint restLocation =
            //    // Check whether the last task was resting too
            //    lastTask != null && lastTask.Type == BotTaskType.Rest && Instance.ResourceManager.IsRestingLocationAvailable(bot.CurrentWaypoint) ?
            //    // We already rested before and did not move since then - simply stay at the current resting location
            //    bot.CurrentWaypoint :
            //    // We need to choose a new resting location
            //    //Instance.ResourceManager.UnusedRestingLocations.ElementAt(Instance.Randomizer.NextInt(Instance.ResourceManager.UnusedRestingLocations.Count()));
            //    getCloestRestPoint(bot);
            //if (lastTask != null) Console.Write(lastTask.Type.ToString(), Instance.ResourceManager.IsRestingLocationAvailable(bot.CurrentWaypoint));
            //Console.Write(bot.CurrentWaypoint == restLocation);
            //// Absolutely no task available - chill
            //EnqueueRest(bot, restLocation);
        }
        /// <summary>
        /// Finds the best delivery task for the specified pod.
        /// Sets <code>chooseRestLocation</code> to null if none found, otherwise <code>bestDeliveryRequest</code> and <code>bestTimeForDeliveryRequest</code> are initialized.
        /// </summary>
        /// <param name="bot">The bot to consider.</param>
        public Waypoint chooseRestLocation(Bot bot)
        {
            BotTask lastTask = GetLastEnqueuedTask(bot);
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
            return closest;
        }

        /// <summary>
        /// Tells the bot to store its current pod at the closest location (if it has one), and then go resting to stay out of the way of other bots.
        /// </summary>
        /// <param name="bot">The bot to consider.</param>
        void GetOutOfTheWay(Bot bot)
        {
            // If carrying a pod dispose it first
            // if (bot.Pod != null)
            // {
            //     StorePodAtClosestStorageLocation(bot);
            //     return;
            // }

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







        /// <summary>
        /// Finds the best delivery task for the specified pod.
        /// Sets <code>getCloestRestPoint</code> to null if none found, otherwise <code>bestDeliveryRequest</code> and <code>bestTimeForDeliveryRequest</code> are initialized.
        /// </summary>
        /// <param name="bot">The bot to consider.</param>
        public Waypoint getCloestRestPoint(Bot bot)
        {
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
            return closest;
        }

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
        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        public override void SignalCurrentTime(double currentTime) { /* Ignore since this simple manager is always ready. */ }
    }
}
