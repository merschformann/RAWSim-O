using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Items;
using RAWSimO.Core.Management;
using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.TaskAllocation
{
    /// <summary>
    /// Implements a manager randomly assigning tasks to the robots.
    /// </summary>
    public class ConceptBotManager : BotManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public ConceptBotManager(Instance instance)
            : base(instance)
        {
            _config = instance.ControllerConfig.TaskAllocationConfig as ConceptTaskAllocationConfiguration;
        }

        private ConceptTaskAllocationConfiguration _config;

        private double GetOrderValue(InputStation iStation, Bot bot) { return Instance.Randomizer.NextDouble(); }
        private double GetOrderValue(OutputStation oStation, Bot bot) { return Instance.Randomizer.NextDouble(); }
        private double GetOrderValue(Pod pod, Bot bot) { return Instance.Randomizer.NextDouble(); }

        private bool DoExtractTaskWithPod(Bot bot, Pod pod)
        {
            // Bring items to the randomly selected first fitting station
            foreach (var oStation in Instance.OutputStations.OrderBy(s => GetOrderValue(s, bot)))
            {
                // Search for requests matching the items of the pod
                List<ExtractRequest> fittingRequests = GetPossibleRequests(pod, oStation, PodSelectionExtractRequestFilteringMode.AssignedOnly);
                if (fittingRequests.Any())
                {
                    // Simply execute the next task with the pod
                    EnqueueExtract(
                        bot, // The bot itself
                        oStation, // The random station
                        pod, // Keep the pod
                        fittingRequests); // The requests to serve

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
            // Randomly decide whether to keep the current pod (if there is one) or switch to another
            bool keepPod = bot.Pod != null && Instance.Randomizer.NextDouble() < 0.5;
            bool extractMode = Instance.Randomizer.NextDouble() < _config.ExtractModeProb;

            // --> Stick to current pod if desired and possible
            if (keepPod)
            {
                if (extractMode)
                {
                    // Try to do another extract task
                    if (DoExtractTaskWithPod(bot, bot.Pod))
                        // Successfully allocated next task
                        return;
                    // Try other mode
                    if (DoStoreTaskWithPod(bot, bot.Pod))
                        // Successfully allocated next task
                        return;
                }
                else
                {
                    // Try to do another store task
                    if (DoStoreTaskWithPod(bot, bot.Pod))
                        // Successfully allocated next task
                        return;
                    // Try other mode
                    if (DoExtractTaskWithPod(bot, bot.Pod))
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
            if (extractMode)
            {
                // Try to do extract job with any pod
                foreach (var pod in Instance.ResourceManager.UnusedPods.OrderBy(b => GetOrderValue(b, bot)))
                    // Try to do extract task with this pod
                    if (DoExtractTaskWithPod(bot, pod))
                        // Successfully allocated next task
                        return;
                // Try to do store job with any pod
                foreach (var pod in Instance.ResourceManager.UnusedPods.OrderBy(b => GetOrderValue(b, bot)))
                    // Try to do extract task with this pod
                    if (DoStoreTaskWithPod(bot, pod))
                        // Successfully allocated next task
                        return;
                // No job in this mode available right now - chill until mode switch or task gets available
                EnqueueRest(bot, Instance.ResourceManager.UnusedRestingLocations.ElementAt(Instance.Randomizer.NextInt(Instance.ResourceManager.UnusedRestingLocations.Count())));
            }
            else
            {
                // Try to do store job with any pod
                foreach (var pod in Instance.ResourceManager.UnusedPods.OrderBy(b => GetOrderValue(b, bot)))
                    // Try to do another store task
                    if (DoStoreTaskWithPod(bot, pod))
                        // Successfully allocated next task
                        return;
                // Try to do store job with any pod
                foreach (var pod in Instance.ResourceManager.UnusedPods.OrderBy(b => GetOrderValue(b, bot)))
                    // Try to do extract task with this pod
                    if (DoExtractTaskWithPod(bot, pod))
                        // Successfully allocated next task
                        return;
                // No job in this mode available right now - chill until mode switch or task gets available
                EnqueueRest(bot, Instance.ResourceManager.UnusedRestingLocations.ElementAt(Instance.Randomizer.NextInt(Instance.ResourceManager.UnusedRestingLocations.Count())));
            }

            // Absolutely no task available - chill
            Waypoint restLocation = Instance.ResourceManager.UnusedRestingLocations.ElementAt(Instance.Randomizer.NextInt(Instance.ResourceManager.UnusedRestingLocations.Count()));
            EnqueueRest(bot, restLocation);
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
