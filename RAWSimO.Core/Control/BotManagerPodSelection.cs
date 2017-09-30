using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using RAWSimO.Core.IO;
using RAWSimO.Core.Items;
using RAWSimO.Core.Management;
using RAWSimO.Core.Metrics;
using RAWSimO.Core.Waypoints;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RAWSimO.Core.Control.RepositioningManager;

namespace RAWSimO.Core.Control
{
    /// <summary>
    /// The base implementation for managing the tasks of the bots.
    /// </summary>
    public abstract partial class BotManager
    {
        #region Helper constants

        /// <summary>
        /// Penalty to use when work amount is considered, but tiers differ.
        /// </summary>
        public const int WrongTierPenaltyForOrderWorkAmount = 1000;
        /// <summary>
        /// Penalty to use when work amount is considered, but tiers differ.
        /// </summary>
        public const int WrongTierPenaltyForBundleWorkAmount = 1000;
        /// <summary>
        /// Distinguishes between different distance metrics.
        /// </summary>
        public enum DistanceMetricType
        {
            /// <summary>
            /// The distance using the euclidean norm.
            /// </summary>
            Euclidean,
            /// <summary>
            /// The distance by using the manhattan metric.
            /// </summary>
            Manhattan,
            /// <summary>
            /// An A* calculated shortest path.
            /// </summary>
            ShortestPath,
            /// <summary>
            /// An A* calculated time-efficient path.
            /// </summary>
            ShortestTime,
        }

        #endregion

        #region Helper fields and methods

        /// <summary>
        /// Stores the available counts per SKU for a pod for on-the-fly assessment.
        /// </summary>
        private VolatileIDDictionary<ItemDescription, int> _availableCounts;
        /// <summary>
        /// Initializes some fields for pod selection.
        /// </summary>
        private void InitPodSelection()
        {
            if (_availableCounts == null)
                _availableCounts = new VolatileIDDictionary<ItemDescription, int>(Instance.ItemDescriptions.Select(i => new VolatileKeyValuePair<ItemDescription, int>(i, 0)).ToList());
        }

        #endregion

        #region Relevant request searchers

        /// <summary>
        /// Checks whether any request matches the combination of pod and station, i.e. whether any work can be done with the pod at the station.
        /// </summary>
        /// <param name="pod">The pod to check.</param>
        /// <param name="station">The station to check.</param>
        /// <returns>Returns <code>true</code> when there is work that can be done with this combination, <code>false</code> otherwise.</returns>
        internal bool AnyRelevantRequests(Pod pod, InputStation station)
        {
            // Search for any storage request of the station matching the pod
            return Instance.ResourceManager.GetStoreRequestsOfStation(station).Any(r => r.Pod == pod);
        }

        /// <summary>
        /// Returns a list of relevant store-requests for the given pod / input-station combination.
        /// </summary>
        /// <param name="pod">The pod in focus.</param>
        /// <param name="station">The station in focus.</param>
        /// <returns>A list of store-requests for bundles that can be put on the pod.</returns>
        internal List<InsertRequest> GetPossibleRequests(Pod pod, InputStation station)
        {
            return Instance.ResourceManager.GetStoreRequestsOfStation(station).Where(r => r.Pod == pod).ToList();
        }

        /// <summary>
        /// Checks whether any request matches the combination of pod and station, i.e. whether any work can be done with the pod at the station.
        /// </summary>
        /// <param name="pod">The pod to check.</param>
        /// <param name="station">The station to check.</param>
        /// <param name="filterForConsideration">Indicates the mode for filtering suitable pods for picking.</param>
        /// <returns>Returns <code>true</code> when there is work that can be done with this combination, <code>false</code> otherwise.</returns>
        internal bool AnyRelevantRequests(Pod pod, OutputStation station, PodSelectionExtractRequestFilteringMode filterForConsideration)
        {
            // Search for any pick request of the station matching an item of the pod
            switch (filterForConsideration)
            {
                case PodSelectionExtractRequestFilteringMode.AssignedOnly:
                    // Check only assigned requests
                    return Instance.ResourceManager.GetExtractRequestsOfStation(station).Any(t => pod.IsAvailable(t.Item));
                case PodSelectionExtractRequestFilteringMode.AssignedAndQueuedEqually:
                    // Check assigned and queued requests equally
                    return Instance.ResourceManager.GetExtractRequestsOfStation(station).Concat(Instance.ResourceManager.GetQueuedExtractRequestsOfStation(station)).Any(t => pod.IsAvailable(t.Item));
                case PodSelectionExtractRequestFilteringMode.AssignedAndCompleteQueued:
                    return
                        // Check already requests already assigned to the station
                        Instance.ResourceManager.GetExtractRequestsOfStation(station).Any(t => pod.IsAvailable(t.Item)) ||
                        // Check completely assignable order's requests
                        station.QueuedOrders.Any(o =>
                            Instance.ResourceManager.GetExtractRequestsOfOrder(o).Any(r => pod.IsAvailable(r.Item)) &&
                            Instance.ResourceManager.GetExtractRequestsOfOrder(o).GroupBy(r => r.Item).All(i => pod.CountAvailable(i.Key) >= i.Count()));
                default: throw new ArgumentException("Unknown mode: " + filterForConsideration);
            }
        }

        /// <summary>
        /// Returns a list of relevant items for the given pod / output-station combination.
        /// </summary>
        /// <param name="pod">The pod in focus.</param>
        /// <param name="station">The station in focus.</param>
        /// <param name="filterForReservation">Indicates the mode for filtering the requests when deciding the actual reservations for a pod.</param>
        /// <returns>A list of tuples of items to serve the respective extract-requests.</returns>
        internal List<ExtractRequest> GetPossibleRequests(Pod pod, OutputStation station, PodSelectionExtractRequestFilteringMode filterForReservation)
        {
            // Init, if necessary
            InitPodSelection();
            // Match fitting items with requests
            List<ExtractRequest> requestsToHandle = new List<ExtractRequest>();
            // Get current content of the pod
            foreach (var item in Instance.ResourceManager.GetExtractRequestsOfStation(station).Concat(Instance.ResourceManager.GetQueuedExtractRequestsOfStation(station)).Select(r => r.Item).Distinct())
                _availableCounts[item] = pod.CountAvailable(item);
            // First handle requests already assigned to the station
            foreach (var itemRequestGroup in Instance.ResourceManager.GetExtractRequestsOfStation(station).GroupBy(r => r.Item))
            {
                // Handle as many requests as possible with the given SKU
                IEnumerable<ExtractRequest> possibleRequests = itemRequestGroup.Take(_availableCounts[itemRequestGroup.Key]);
                requestsToHandle.AddRange(possibleRequests);
                // Update content available in pod for the given SKU
                _availableCounts[itemRequestGroup.Key] -= possibleRequests.Count();
            }
            // Now handle queued requests, if desired
            if (filterForReservation == PodSelectionExtractRequestFilteringMode.AssignedAndQueuedEqually || filterForReservation == PodSelectionExtractRequestFilteringMode.AssignedAndCompleteQueued)
            {
                // Go through all queued orders of the station (trying to assign requests belonging to one order together)
                foreach (var order in station.QueuedOrders.Where(o => Instance.ResourceManager.GetExtractRequestsOfOrder(o).Any()))
                {
                    // Ignore orders not exclusively assignable to the current pod, if desired
                    if (filterForReservation == PodSelectionExtractRequestFilteringMode.AssignedAndCompleteQueued &&
                        !Instance.ResourceManager.GetExtractRequestsOfOrder(order).GroupBy(r => r.Item).All(rg => _availableCounts[rg.Key] >= rg.Count()))
                        continue;
                    // Handle the requests of the queued order
                    foreach (var itemRequestGroup in Instance.ResourceManager.GetExtractRequestsOfOrder(order).GroupBy(r => r.Item))
                    {
                        // Handle as many requests as possible with the given SKU
                        IEnumerable<ExtractRequest> possibleRequests = itemRequestGroup.Take(_availableCounts[itemRequestGroup.Key]);
                        requestsToHandle.AddRange(possibleRequests);
                        // Update content available in pod for the given SKU
                        _availableCounts[itemRequestGroup.Key] -= possibleRequests.Count();
                    }
                }
            }
            // Return the result
            return requestsToHandle;
        }

        #endregion

        #region Scoring function instantiators

        /// <summary>
        /// Instantiates a scoring function from the given config.
        /// </summary>
        /// <param name="scorerConfig">The config.</param>
        /// <returns>The scoring function.</returns>
        private Func<double> GenerateScorerIStationForBotWithPod(PCScorerIStationForBotWithPod scorerConfig)
        {
            switch (scorerConfig.Type())
            {
                case PrefIStationForBotWithPod.Random:
                    { PCScorerIStationForBotWithPodRandom tempcfg = scorerConfig as PCScorerIStationForBotWithPodRandom; return () => { return Score(tempcfg, _currentBot, _currentIStation); }; }
                case PrefIStationForBotWithPod.Nearest:
                    { PCScorerIStationForBotWithPodNearest tempcfg = scorerConfig as PCScorerIStationForBotWithPodNearest; return () => { return Score(tempcfg, _currentBot, _currentIStation); }; }
                case PrefIStationForBotWithPod.WorkAmount:
                    { PCScorerIStationForBotWithPodWorkAmount tempcfg = scorerConfig as PCScorerIStationForBotWithPodWorkAmount; return () => { return Score(tempcfg, _currentBot, _currentIStation); }; }
                default: throw new ArgumentException("Unknown score type: " + scorerConfig.Type());
            }
        }
        /// <summary>
        /// Instantiates a scoring function from the given config.
        /// </summary>
        /// <param name="scorerConfig">The config.</param>
        /// <returns>The scoring function.</returns>
        private Func<double> GenerateScorerOStationForBotWithPod(PCScorerOStationForBotWithPod scorerConfig)
        {
            switch (scorerConfig.Type())
            {
                case PrefOStationForBotWithPod.Random:
                    { PCScorerOStationForBotWithPodRandom tempcfg = scorerConfig as PCScorerOStationForBotWithPodRandom; return () => { return Score(tempcfg, _currentBot, _currentOStation); }; }
                case PrefOStationForBotWithPod.Nearest:
                    { PCScorerOStationForBotWithPodNearest tempcfg = scorerConfig as PCScorerOStationForBotWithPodNearest; return () => { return Score(tempcfg, _currentBot, _currentOStation); }; }
                case PrefOStationForBotWithPod.WorkAmount:
                    { PCScorerOStationForBotWithPodWorkAmount tempcfg = scorerConfig as PCScorerOStationForBotWithPodWorkAmount; return () => { return Score(tempcfg, _currentBot, _currentOStation); }; }
                default: throw new ArgumentException("Unknown score type: " + scorerConfig.Type());
            }
        }
        /// <summary>
        /// Instantiates a scoring function from the given config.
        /// </summary>
        /// <param name="scorerConfig">The config.</param>
        /// <returns>The scoring function.</returns>
        private Func<double> GenerateScorerPodForIStationBot(PCScorerPodForIStationBot scorerConfig)
        {
            switch (scorerConfig.Type())
            {
                case PrefPodForIStationBot.Random:
                    { PCScorerPodForIStationBotRandom tempcfg = scorerConfig as PCScorerPodForIStationBotRandom; return () => { return Score(tempcfg, _currentBot, _currentPod, _currentIStation); }; }
                case PrefPodForIStationBot.Nearest:
                    { PCScorerPodForIStationBotNearest tempcfg = scorerConfig as PCScorerPodForIStationBotNearest; return () => { return Score(tempcfg, _currentBot, _currentPod, _currentIStation); }; }
                case PrefPodForIStationBot.Demand:
                    { PCScorerPodForIStationBotDemand tempcfg = scorerConfig as PCScorerPodForIStationBotDemand; return () => { return Score(tempcfg, _currentBot, _currentPod, _currentIStation); }; }
                case PrefPodForIStationBot.WorkAmount:
                    { PCScorerPodForIStationBotWorkAmount tempcfg = scorerConfig as PCScorerPodForIStationBotWorkAmount; return () => { return Score(tempcfg, _currentBot, _currentPod, _currentIStation); }; }
                default: throw new ArgumentException("Unknown score type: " + scorerConfig.Type());
            }
        }
        /// <summary>
        /// Instantiates a scoring function from the given config.
        /// </summary>
        /// <param name="scorerConfig">The config.</param>
        /// <returns>The scoring function.</returns>
        private Func<double> GenerateScorerPodForOStationBot(PCScorerPodForOStationBot scorerConfig)
        {
            switch (scorerConfig.Type())
            {
                case PrefPodForOStationBot.Random:
                    { PCScorerPodForOStationBotRandom tempcfg = scorerConfig as PCScorerPodForOStationBotRandom; return () => { return Score(tempcfg, _currentBot, _currentPod, _currentOStation); }; }
                case PrefPodForOStationBot.Fill:
                    { PCScorerPodForOStationBotFill tempcfg = scorerConfig as PCScorerPodForOStationBotFill; return () => { return Score(tempcfg, _currentBot, _currentPod, _currentOStation); }; }
                case PrefPodForOStationBot.Nearest:
                    { PCScorerPodForOStationBotNearest tempcfg = scorerConfig as PCScorerPodForOStationBotNearest; return () => { return Score(tempcfg, _currentBot, _currentPod, _currentOStation); }; }
                case PrefPodForOStationBot.Demand:
                    { PCScorerPodForOStationBotDemand tempcfg = scorerConfig as PCScorerPodForOStationBotDemand; return () => { return Score(tempcfg, _currentBot, _currentPod, _currentOStation); }; }
                case PrefPodForOStationBot.Completeable:
                    { PCScorerPodForOStationBotCompleteable tempcfg = scorerConfig as PCScorerPodForOStationBotCompleteable; return () => { return Score(tempcfg, _currentBot, _currentPod, _currentOStation); }; }
                case PrefPodForOStationBot.WorkAmount:
                    { PCScorerPodForOStationBotWorkAmount tempcfg = scorerConfig as PCScorerPodForOStationBotWorkAmount; return () => { return Score(tempcfg, _currentBot, _currentPod, _currentOStation); }; }
                default: throw new ArgumentException("Unknown score type: " + scorerConfig.Type());
            }
        }

        #endregion

        #region Assignment scorers (input-station for bot carrying a pod)

        /// <summary>
        /// Determines a score that can be used to decide about an assignment.
        /// </summary>
        /// <param name="config">The config specifying further parameters.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="station">The station.</param>
        /// <returns>A score that can be used to decide about the best assignment. Minimization / Smaller is better.</returns>
        public double Score(PCScorerIStationForBotWithPodRandom config, Bot bot, InputStation station)
        {
            return config.PreferSameTier ?
                station.Tier == bot.Tier ? -Instance.Randomizer.NextDouble() : Instance.Randomizer.NextDouble() :
                Instance.Randomizer.NextDouble();
        }
        /// <summary>
        /// Determines a score that can be used to decide about an assignment.
        /// </summary>
        /// <param name="config">The config specifying further parameters.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="station">The station.</param>
        /// <returns>A score that can be used to decide about the best assignment. Minimization / Smaller is better.</returns>
        public double Score(PCScorerIStationForBotWithPodNearest config, Bot bot, InputStation station)
        {
            switch (config.DistanceMetric)
            {
                case DistanceMetricType.Euclidean: return Distances.CalculateEuclid(bot, station, Instance.WrongTierPenaltyDistance);
                case DistanceMetricType.Manhattan: return Distances.CalculateManhattan(bot, station, Instance.WrongTierPenaltyDistance);
                case DistanceMetricType.ShortestPath:
                    return bot.CurrentWaypoint == null ?
                        Distances.CalculateManhattan(bot, station, Instance.WrongTierPenaltyDistance) :
                        bot.Pod == null ?
                            Distances.CalculateShortestPath(bot.CurrentWaypoint, station.Waypoint, Instance) :
                            Distances.CalculateShortestPathPodSafe(bot.CurrentWaypoint, station.Waypoint, Instance);
                case DistanceMetricType.ShortestTime:
                    return bot.CurrentWaypoint == null ?
                        Distances.EstimateManhattanTime(bot, station, Instance) :
                        bot.Pod == null ?
                            Distances.CalculateShortestTimePath(bot.CurrentWaypoint, station.Waypoint, Instance) :
                            Distances.CalculateShortestTimePathPodSafe(bot.CurrentWaypoint, station.Waypoint, Instance);
                default: throw new ArgumentException("Unknown distance metric: " + config.DistanceMetric);
            }
        }
        /// <summary>
        /// Determines a score that can be used to decide about an assignment.
        /// </summary>
        /// <param name="config">The config specifying further parameters.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="station">The station.</param>
        /// <returns>A score that can be used to decide about the best assignment. Minimization / Smaller is better.</returns>
        public double Score(PCScorerIStationForBotWithPodWorkAmount config, Bot bot, InputStation station)
        {
            // If the bot is not carrying a pod, this scorer cannot do anything
            if (bot.Pod == null)
                return double.MaxValue;
            double value;
            if (config.IncludeAge)
                // Measure work amount by also including the time the bundle was already present at the station
                value = -Instance.ResourceManager.GetStoreRequestsOfStation(station)
                    .Where(r => r.Pod == bot.Pod)
                    .Sum(r => Instance.Controller.CurrentTime - r.Bundle.TimeStampSubmit);
            else
                // Measure work by only considering the number of bundles that can be put on the pod
                value = -Instance.ResourceManager.GetStoreRequestsOfStation(station)
                    .Where(r => r.Pod == bot.Pod)
                    .Count();
            // Penalize stations that are on a different tier
            if (config.PreferSameTier && bot.Tier != station.Tier)
                value += WrongTierPenaltyForBundleWorkAmount;
            return value;
        }

        #endregion

        #region Assignment scorers (output-station for bot carrying a pod)

        /// <summary>
        /// Determines a score that can be used to decide about an assignment.
        /// </summary>
        /// <param name="config">The config specifying further parameters.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="station">The station.</param>
        /// <returns>A score that can be used to decide about the best assignment. Minimization / Smaller is better.</returns>
        public double Score(PCScorerOStationForBotWithPodRandom config, Bot bot, OutputStation station)
        {
            return config.PreferSameTier ?
                station.Tier == bot.Tier ? -Instance.Randomizer.NextDouble() : Instance.Randomizer.NextDouble() :
                Instance.Randomizer.NextDouble();
        }
        /// <summary>
        /// Determines a score that can be used to decide about an assignment.
        /// </summary>
        /// <param name="config">The config specifying further parameters.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="station">The station.</param>
        /// <returns>A score that can be used to decide about the best assignment. Minimization / Smaller is better.</returns>
        public double Score(PCScorerOStationForBotWithPodNearest config, Bot bot, OutputStation station)
        {
            switch (config.DistanceMetric)
            {
                case DistanceMetricType.Euclidean: return Distances.CalculateEuclid(bot, station, Instance.WrongTierPenaltyDistance);
                case DistanceMetricType.Manhattan: return Distances.CalculateManhattan(bot, station, Instance.WrongTierPenaltyDistance);
                case DistanceMetricType.ShortestPath:
                    return bot.CurrentWaypoint == null ?
                        Distances.CalculateManhattan(bot, station, Instance.WrongTierPenaltyDistance) :
                        bot.Pod == null ?
                            Distances.CalculateShortestPath(bot.CurrentWaypoint, station.Waypoint, Instance) :
                            Distances.CalculateShortestPathPodSafe(bot.CurrentWaypoint, station.Waypoint, Instance);
                case DistanceMetricType.ShortestTime:
                    return bot.CurrentWaypoint == null ?
                        Distances.EstimateManhattanTime(bot, station, Instance) :
                        bot.Pod == null ?
                            Distances.CalculateShortestTimePath(bot.CurrentWaypoint, station.Waypoint, Instance) :
                            Distances.CalculateShortestTimePathPodSafe(bot.CurrentWaypoint, station.Waypoint, Instance);
                default: throw new ArgumentException("Unknown distance metric: " + config.DistanceMetric);
            }
        }
        /// <summary>
        /// Determines a score that can be used to decide about an assignment.
        /// </summary>
        /// <param name="config">The config specifying further parameters.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="station">The station.</param>
        /// <returns>A score that can be used to decide about the best assignment. Minimization / Smaller is better.</returns>
        public double Score(PCScorerOStationForBotWithPodWorkAmount config, Bot bot, OutputStation station)
        {
            // If the bot is not carrying a pod, this scorer cannot do anything
            if (bot.Pod == null)
                return double.MaxValue;
            double value;
            switch (config.ValueMetric)
            {
                case PCScorerWorkAmountValueMetric.OrderAge:
                    // Measure work amount by also including the time the order was already present at the station
                    value = -Instance.ResourceManager.GetExtractRequestsOfStation(station).GroupBy(r => r.Item)
                        .Sum(g => g.OrderBy(o => o.Order.TimeStampSubmit).Take(bot.Pod.CountAvailable(g.Key))
                        .Sum(r => (Instance.Controller.CurrentTime - r.Order.TimeStampSubmit) / r.Order.Positions.Count()));
                    break;
                case PCScorerWorkAmountValueMetric.OrderDueTime:
                    // Measure work amount by also including the time the order was already present at the station
                    value = -Instance.ResourceManager.GetExtractRequestsOfStation(station).GroupBy(r => r.Item)
                        .Sum(g => g.OrderBy(o => o.Order.DueTime).Take(bot.Pod.CountAvailable(g.Key))
                        .Sum(r => (config.OnlyPositiveLateness ? Math.Max(Instance.Controller.CurrentTime - r.Order.DueTime, 0) : Instance.Controller.CurrentTime - r.Order.DueTime) / r.Order.Positions.Count()));
                    break;
                case PCScorerWorkAmountValueMetric.Picks:
                    // Measure work by only considering the number of picks that can be done for the pod
                    value = -Instance.ResourceManager.GetExtractRequestsOfStation(station)
                        .GroupBy(r => r.Item)
                        .Sum(g => Math.Min(bot.Pod.CountAvailable(g.Key), g.Count()));
                    break;
                default: throw new ArgumentException("Unknown value metric: " + config.ValueMetric);
            }
            // Penalize stations that are on a different tier
            if (config.PreferSameTier && bot.Tier != station.Tier)
                value += WrongTierPenaltyForOrderWorkAmount;
            return value;
        }

        #endregion

        #region Assignment scorers (pod for bot working for an input-station)

        /// <summary>
        /// Determines a score that can be used to decide about an assignment.
        /// </summary>
        /// <param name="config">The config specifying further parameters.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="pod">The pod.</param>
        /// <param name="station">The station.</param>
        /// <returns>A score that can be used to decide about the best assignment. Minimization / Smaller is better.</returns>
        public double Score(PCScorerPodForIStationBotRandom config, Bot bot, Pod pod, InputStation station)
        {
            return config.PreferSameTier ?
                // Penalize entities on different tiers
                (station.Tier == pod.Tier && pod.Tier == bot.Tier ?
                    -Instance.Randomizer.NextDouble() - 1 :
                pod.Tier == station.Tier ?
                    -Instance.Randomizer.NextDouble() :
                    Instance.Randomizer.NextDouble()) :
                // Use a simple random (neglect tiers)
                Instance.Randomizer.NextDouble();
        }
        /// <summary>
        /// Determines a score that can be used to decide about an assignment.
        /// </summary>
        /// <param name="config">The config specifying further parameters.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="pod">The pod.</param>
        /// <param name="station">The station.</param>
        /// <returns>A score that can be used to decide about the best assignment. Minimization / Smaller is better.</returns>
        public double Score(PCScorerPodForIStationBotNearest config, Bot bot, Pod pod, InputStation station)
        {
            switch (config.DistanceMetric)
            {
                case DistanceMetricType.Euclidean:
                    return
                        Distances.CalculateEuclid(bot, pod, Instance.WrongTierPenaltyDistance) +
                        Distances.CalculateEuclid(pod, station, Instance.WrongTierPenaltyDistance);
                case DistanceMetricType.Manhattan:
                    return
                        Distances.CalculateManhattan(bot, pod, Instance.WrongTierPenaltyDistance) +
                        Distances.CalculateManhattan(pod, station, Instance.WrongTierPenaltyDistance);
                case DistanceMetricType.ShortestPath:
                    {
                        double trip = 0;
                        if (bot.CurrentWaypoint == null || pod.Waypoint == null || config.EstimateBotPodDistance)
                            trip += Distances.CalculateManhattan(bot, pod, Instance.WrongTierPenaltyDistance);
                        else
                            trip += Distances.CalculateShortestPath(bot.CurrentWaypoint, pod.Waypoint, Instance);
                        if (pod.Waypoint == null)
                            trip += Distances.CalculateManhattan(pod, station, Instance.WrongTierPenaltyDistance);
                        else
                            trip += Distances.CalculateShortestPathPodSafe(pod.Waypoint, station.Waypoint, Instance);
                        return trip;
                    }
                case DistanceMetricType.ShortestTime:
                    {
                        double trip = 0;
                        if (bot.CurrentWaypoint == null || pod.Waypoint == null || config.EstimateBotPodDistance)
                            trip += Distances.EstimateManhattanTime(bot, pod, Instance);
                        else
                            trip += Distances.CalculateShortestTimePath(bot.CurrentWaypoint, pod.Waypoint, Instance);
                        if (pod.Waypoint == null)
                            trip += Distances.EstimateManhattanTime(pod, station, Instance);
                        else
                            trip += Distances.CalculateShortestTimePathPodSafe(pod.Waypoint, station.Waypoint, Instance);
                        return trip;
                    }
                default: throw new ArgumentException("Unknown distance metric: " + config.DistanceMetric);
            }
        }
        /// <summary>
        /// Determines a score that can be used to decide about an assignment.
        /// </summary>
        /// <param name="config">The config specifying further parameters.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="pod">The pod.</param>
        /// <param name="station">The station.</param>
        /// <returns>A score that can be used to decide about the best assignment. Minimization / Smaller is better.</returns>
        public double Score(PCScorerPodForIStationBotDemand config, Bot bot, Pod pod, InputStation station)
        {
            return -pod.RegisteredBundles.Sum(b =>
                Math.Min(
                    // Overall demand
                    Instance.ResourceManager.GetDemandAssigned(b.ItemDescription) + Instance.ResourceManager.GetDemandQueued(b.ItemDescription) + Instance.ResourceManager.GetDemandBacklog(b.ItemDescription),
                    // New stock offered by bundle
                    b.ItemCount));
        }
        /// <summary>
        /// Determines a score that can be used to decide about an assignment.
        /// </summary>
        /// <param name="config">The config specifying further parameters.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="pod">The pod.</param>
        /// <param name="station">The station.</param>
        /// <returns>A score that can be used to decide about the best assignment. Minimization / Smaller is better.</returns>
        public double Score(PCScorerPodForIStationBotWorkAmount config, Bot bot, Pod pod, InputStation station)
        {
            // Determine score
            double value;
            if (config.IncludeAge)
                // Measure work amount by also including the time the bundle was already present at the station
                value = -Instance.ResourceManager.GetStoreRequestsOfStation(station)
                    .Where(r => r.Pod == pod)
                    .Sum(r => Instance.Controller.CurrentTime - r.Bundle.TimeStampSubmit);
            else
                // Measure work by only considering the number of bundles that can be put on the pod
                value = -Instance.ResourceManager.GetStoreRequestsOfStation(station)
                    .Where(r => r.Pod == pod)
                    .Count();
            // Penalize entities that are on different tiers
            if (config.PreferSameTier)
            {
                if (bot.Tier != pod.Tier && pod.Tier != station.Tier)
                    value += 2 * WrongTierPenaltyForBundleWorkAmount;
                else if (bot.Tier != pod.Tier || pod.Tier != station.Tier)
                    value += WrongTierPenaltyForBundleWorkAmount;
            }
            return value;
        }

        #endregion

        #region Assignment scorers (pod for bot working for an output-station)

        /// <summary>
        /// Determines a score that can be used to decide about an assignment.
        /// </summary>
        /// <param name="config">The config specifying further parameters.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="pod">The pod.</param>
        /// <param name="station">The station.</param>
        /// <returns>A score that can be used to decide about the best assignment. Minimization / Smaller is better.</returns>
        public double Score(PCScorerPodForOStationBotRandom config, Bot bot, Pod pod, OutputStation station)
        {
            return config.PreferSameTier ?
                // Penalize entities on different tiers
                (station.Tier == pod.Tier && pod.Tier == bot.Tier ?
                    -Instance.Randomizer.NextDouble() - 1 :
                pod.Tier == station.Tier ?
                    -Instance.Randomizer.NextDouble() :
                    Instance.Randomizer.NextDouble()) :
                // Use a simple random (neglect tiers)
                Instance.Randomizer.NextDouble();
        }
        /// <summary>
        /// Determines a score that can be used to decide about an assignment.
        /// </summary>
        /// <param name="config">The config specifying further parameters.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="pod">The pod.</param>
        /// <param name="station">The station.</param>
        /// <returns>A score that can be used to decide about the best assignment. Minimization / Smaller is better.</returns>
        public double Score(PCScorerPodForOStationBotFill config, Bot bot, Pod pod, OutputStation station)
        {
            return config.Binary ?
                // Use the threshold to determine the pod score
                (config.PreferFullest ? (pod.CapacityInUse / pod.Capacity > config.Threshold ? 0 : 1) : (pod.CapacityInUse / pod.Capacity < config.Threshold ? 0 : 1)) :
                // Use the absolute capacity used to determine the pod score
                (config.PreferFullest ? -pod.CapacityInUse : pod.CapacityInUse);
        }
        /// <summary>
        /// Determines a score that can be used to decide about an assignment.
        /// </summary>
        /// <param name="config">The config specifying further parameters.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="pod">The pod.</param>
        /// <param name="station">The station.</param>
        /// <returns>A score that can be used to decide about the best assignment. Minimization / Smaller is better.</returns>
        public double Score(PCScorerPodForOStationBotNearest config, Bot bot, Pod pod, OutputStation station)
        {
            switch (config.DistanceMetric)
            {
                case DistanceMetricType.Euclidean:
                    return
                        Distances.CalculateEuclid(bot, pod, Instance.WrongTierPenaltyDistance) +
                        Distances.CalculateEuclid(pod, station, Instance.WrongTierPenaltyDistance);
                case DistanceMetricType.Manhattan:
                    return
                        Distances.CalculateManhattan(bot, pod, Instance.WrongTierPenaltyDistance) +
                        Distances.CalculateManhattan(pod, station, Instance.WrongTierPenaltyDistance);
                case DistanceMetricType.ShortestPath:
                    {
                        double trip = 0;
                        if (bot.CurrentWaypoint == null || pod.Waypoint == null || config.EstimateBotPodDistance)
                            trip += Distances.CalculateManhattan(bot, pod, Instance.WrongTierPenaltyDistance);
                        else
                            trip += Distances.CalculateShortestPath(bot.CurrentWaypoint, pod.Waypoint, Instance);
                        if (pod.Waypoint == null)
                            trip += Distances.CalculateManhattan(pod, station, Instance.WrongTierPenaltyDistance);
                        else
                            trip += Distances.CalculateShortestPathPodSafe(pod.Waypoint, station.Waypoint, Instance);
                        return trip;
                    }
                case DistanceMetricType.ShortestTime:
                    {
                        double trip = 0;
                        if (bot.CurrentWaypoint == null || pod.Waypoint == null || config.EstimateBotPodDistance)
                            trip += Distances.EstimateManhattanTime(bot, pod, Instance);
                        else
                            trip += Distances.CalculateShortestTimePath(bot.CurrentWaypoint, pod.Waypoint, Instance);
                        if (pod.Waypoint == null)
                            trip += Distances.EstimateManhattanTime(pod, station, Instance);
                        else
                            trip += Distances.CalculateShortestTimePathPodSafe(pod.Waypoint, station.Waypoint, Instance);
                        return trip;
                    }
                default: throw new ArgumentException("Unknown distance metric: " + config.DistanceMetric);
            }
        }
        /// <summary>
        /// Determines a score that can be used to decide about an assignment.
        /// </summary>
        /// <param name="config">The config specifying further parameters.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="pod">The pod.</param>
        /// <param name="station">The station.</param>
        /// <returns>A score that can be used to decide about the best assignment. Minimization / Smaller is better.</returns>
        public double Score(PCScorerPodForOStationBotDemand config, Bot bot, Pod pod, OutputStation station)
        {
            return -pod.ItemDescriptionsContained.Sum(i =>
                Math.Min(
                    // Overall demand
                    Instance.ResourceManager.GetDemandAssigned(i) + Instance.ResourceManager.GetDemandQueued(i) + Instance.ResourceManager.GetDemandBacklog(i),
                    // Stock offered by pod
                    pod.CountContained(i)));
        }
        /// <summary>
        /// Determines a score that can be used to decide about an assignment.
        /// </summary>
        /// <param name="config">The config specifying further parameters.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="pod">The pod.</param>
        /// <param name="station">The station.</param>
        /// <returns>A score that can be used to decide about the best assignment. Minimization / Smaller is better.</returns>
        public double Score(PCScorerPodForOStationBotCompleteable config, Bot bot, Pod pod, OutputStation station)
        {
            // Check picks leading to completed orders
            int completeableAssignedOrders = 0;
            int completeableQueuedOrders = 0;
            // Get current pod content
            foreach (var item in Instance.ResourceManager.GetExtractRequestsOfStation(station).Select(r => r.Item).Distinct())
                _availableCounts[item] = pod.CountAvailable(item);
            // Check all assigned orders
            foreach (var order in station.AssignedOrders)
            {
                // Get demand for items caused by order
                List<IGrouping<ItemDescription, ExtractRequest>> itemDemands = Instance.ResourceManager.GetExtractRequestsOfOrder(order).GroupBy(r => r.Item).ToList();
                // Check whether sufficient inventory is still available in the pod (also make sure it is was available in the beginning, not all values were updated at the beginning of this function / see above)
                if (itemDemands.All(g => pod.IsAvailable(g.Key) && _availableCounts[g.Key] >= g.Count()))
                {
                    // Update remaining pod content
                    foreach (var itemDemand in itemDemands)
                        _availableCounts[itemDemand.Key] -= itemDemand.Count();
                    // Update number of completeable orders
                    completeableAssignedOrders++;
                }
            }
            // Check all queued orders
            if (config.IncludeQueuedOrders)
            {
                foreach (var order in station.QueuedOrders)
                {
                    // Get demand for items caused by order
                    List<IGrouping<ItemDescription, ExtractRequest>> itemDemands = Instance.ResourceManager.GetExtractRequestsOfOrder(order).GroupBy(r => r.Item).ToList();
                    // Check whether sufficient inventory is still available in the pod (also make sure it is was available in the beginning, not all values were updated at the beginning of this function / see above)
                    if (itemDemands.All(g => pod.IsAvailable(g.Key) && _availableCounts[g.Key] >= g.Count()))
                    {
                        // Update remaining pod content
                        foreach (var itemDemand in itemDemands)
                            _availableCounts[itemDemand.Key] -= itemDemand.Count();
                        // Update number of completeable orders
                        completeableQueuedOrders++;
                    }
                }
            }
            return -(completeableAssignedOrders + completeableQueuedOrders);
        }
        /// <summary>
        /// Determines a score that can be used to decide about an assignment.
        /// </summary>
        /// <param name="config">The config specifying further parameters.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="pod">The pod.</param>
        /// <param name="station">The station.</param>
        /// <returns>A score that can be used to decide about the best assignment. Minimization / Smaller is better.</returns>
        public double Score(PCScorerPodForOStationBotWorkAmount config, Bot bot, Pod pod, OutputStation station)
        {
            // Check picks leading to completed orders (if necessary)
            if (config.CompleteableOrderBoost > 0)
            {
                // Get current pod content
                foreach (var item in Instance.ResourceManager.GetExtractRequestsOfStation(station).Select(r => r.Item).Distinct())
                    _availableCounts[item] = pod.CountAvailable(item);
                // Check all assigned orders
                foreach (var order in station.AssignedOrders)
                {
                    // Get demand for items caused by order
                    List<IGrouping<ItemDescription, ExtractRequest>> itemDemands = Instance.ResourceManager.GetExtractRequestsOfOrder(order).GroupBy(r => r.Item).ToList();
                    // Check whether sufficient inventory is still available in the pod (also make sure it is was available in the beginning, not all values were updated at the beginning of this function / see above)
                    if (itemDemands.All(g => pod.IsAvailable(g.Key) && _availableCounts[g.Key] >= g.Count()))
                    {
                        // Update remaining pod content
                        foreach (var itemDemand in itemDemands)
                            _availableCounts[itemDemand.Key] -= itemDemand.Count();
                        // Mark completeable
                        order.HelperBoolTag = true;
                    }
                    else
                    {
                        // Mark not completeable
                        order.HelperBoolTag = false;
                    }
                }
                // Check all queued orders (if necessary)
                foreach (var order in station.QueuedOrders)
                {
                    // Get demand for items caused by order
                    List<IGrouping<ItemDescription, ExtractRequest>> itemDemands = Instance.ResourceManager.GetExtractRequestsOfOrder(order).GroupBy(r => r.Item).ToList();
                    // Check whether sufficient inventory is still available in the pod (also make sure it is was available in the beginning, not all values were updated at the beginning of this function / see above)
                    if (config.CompleteableQueuedOrders && itemDemands.All(g => pod.IsAvailable(g.Key) && _availableCounts[g.Key] >= g.Count()))
                    {
                        // Update remaining pod content
                        foreach (var itemDemand in itemDemands)
                            _availableCounts[itemDemand.Key] -= itemDemand.Count();
                        // Mark completeable
                        order.HelperBoolTag = true;
                    }
                    else
                    {
                        // Mark not completeable
                        order.HelperBoolTag = false;
                    }
                }
            }
            // Determine score
            double value;
            switch (config.ValueMetric)
            {
                case PCScorerWorkAmountValueMetric.OrderAge:
                    // Measure work amount by also including the time the order was already present at the station
                    value = -Instance.ResourceManager.GetExtractRequestsOfStation(station).Concat(Instance.ResourceManager.GetQueuedExtractRequestsOfStation(station))
                        // Group by SKU
                        .GroupBy(r => r.Item)
                        // Per SKU get the already assigned requests first, but also consider the queued ones - take as many as the pod allows
                        .Sum(g => g.OrderBy(r => r.Station != null ? 0 : 1).ThenBy(r => r.Order.TimeStampSubmit).Take(pod.CountAvailable(g.Key))
                        // Sum the age we can get rid off (use a different weight for requests belonging to queued orders)
                        .Sum(r => (r.Station != null ?
                            // Order is submitted
                            ((Instance.Controller.CurrentTime - r.Order.TimeStampSubmit) / r.Order.GetOpenDemandCount()) :
                            // Order is queued
                            (config.BacklogWeight * (Instance.Controller.CurrentTime - r.Order.TimeStampQueued) / r.Order.GetOpenDemandCount())) *
                            // Boost score, if an order can be completed with the pod
                            (r.Order.HelperBoolTag ? 1 + config.CompleteableOrderBoost : 1)));
                    break;
                case PCScorerWorkAmountValueMetric.OrderDueTime:
                    // Measure lateness we can eliminate
                    value = -Instance.ResourceManager.GetExtractRequestsOfStation(station).Concat(Instance.ResourceManager.GetQueuedExtractRequestsOfStation(station))
                        // Group by SKU
                        .GroupBy(r => r.Item)
                        // Per SKU get the already assigned requests first, but also consider the queued ones - take as many as the pod allows
                        .Sum(g => g.OrderBy(r => r.Station != null ? 0 : 1).ThenBy(r => r.Order.DueTime).Take(pod.CountAvailable(g.Key))
                        // Sum the age we can get rid off (use a different weight for requests belonging to queued orders)
                        .Sum(r => (r.Station != null ?
                            // Order is submitted
                            ((config.OnlyPositiveLateness ? Math.Max(Instance.Controller.CurrentTime - r.Order.DueTime, 0) : Instance.Controller.CurrentTime - r.Order.DueTime) / r.Order.GetOpenDemandCount()) :
                            // Order is queued
                            (config.BacklogWeight * (config.OnlyPositiveLateness ? Math.Max(Instance.Controller.CurrentTime - r.Order.DueTime, 0) : Instance.Controller.CurrentTime - r.Order.DueTime) / r.Order.GetOpenDemandCount())) *
                            // Boost score, if an order can be completed with the pod
                            (r.Order.HelperBoolTag ? 1 + config.CompleteableOrderBoost : 1)));
                    break;
                case PCScorerWorkAmountValueMetric.Picks:
                    // Measure work by only considering the number of picks that can be done with the pod
                    value = -Instance.ResourceManager.GetExtractRequestsOfStation(station).Concat(Instance.ResourceManager.GetQueuedExtractRequestsOfStation(station))
                        // Group by SKU
                        .GroupBy(r => r.Item)
                        // Per SKU get the already assigned requests first, but also consider the queued ones - take as many as the pod allows
                        .Sum(g => g.OrderBy(r => r.Station != null ? 0 : 1).Take(pod.CountAvailable(g.Key))
                        // Sum the picks we can do with the pod (use a different weight for requests belonging to queued orders)
                        .Sum(r => (r.Station != null ? 1 : config.BacklogWeight) *
                        // Boost score, if an order can be completed with the pod
                        (r.Order.HelperBoolTag ? 1 + config.CompleteableOrderBoost : 1)));
                    break;
                default: throw new ArgumentException("Unknown value metric: " + config.ValueMetric);
            }
            // Penalize the distance that needs to be traveled
            if (config.TimeCosts > 0)
            {
                // Check whether there is any work to do
                if (value >= 0)
                {
                    // Hard penalize combinations with no work to do
                    value += 2 * Instance.WrongTierPenaltyDistance * config.TimeCosts;
                }
                else
                {
                    // Add some costs for the distance
                    double travelTime = 0;
                    if (config.EstimateBotPodDistancePenalty || bot.CurrentWaypoint == null || pod.Waypoint == null)
                        travelTime += Distances.EstimateManhattanTime(bot, pod, Instance);
                    else
                        travelTime += Distances.CalculateShortestTimePath(bot.CurrentWaypoint, pod.Waypoint, Instance);
                    if (config.EstimatePodStationDistancePenalty || pod.Waypoint == null)
                        travelTime += Distances.EstimateManhattanTime(pod, station, Instance);
                    else
                        travelTime += Distances.CalculateShortestTimePathPodSafe(pod.Waypoint, station.Waypoint, Instance);
                    value += travelTime * config.TimeCosts;
                }
            }
            // Penalize entities that are on different tiers
            if (config.PreferSameTier)
            {
                if (bot.Tier != pod.Tier && pod.Tier != station.Tier)
                    value += 2 * WrongTierPenaltyForBundleWorkAmount;
                else if (bot.Tier != pod.Tier || pod.Tier != station.Tier)
                    value += WrongTierPenaltyForBundleWorkAmount;
            }
            return value;
        }

        #endregion

        #region Pod selectors

        // --->>> BEST CANDIDATE HELPER FIELDS - USED FOR SELECTING THE NEXT BEST TASK
        /// <summary>
        /// The current bot to assess.
        /// </summary>
        private Bot _currentBot = null;
        /// <summary>
        /// The current pod to assess.
        /// </summary>
        private Pod _currentPod = null;
        /// <summary>
        /// The current input station to assess
        /// </summary>
        private InputStation _currentIStation = null;
        /// <summary>
        /// The current output station to assess
        /// </summary>
        private OutputStation _currentOStation = null;
        // --->>> BEST CANDIDATE SELECTORS FOR INPUT TASKS
        /// <summary>
        /// A helper used to determine the best candidate.
        /// </summary>
        private BestCandidateSelector _bestPodIStationCandidateSelector = null;
        /// <summary>
        /// A helper used to determine the best candidate.
        /// </summary>
        private BestCandidateSelector _bestIStationCandidateSelector = null;
        // --->>> BEST CANDIDATE SELECTORS FOR OUTPUT TASKS
        /// <summary>
        /// A helper used to determine the best candidate.
        /// </summary>
        private BestCandidateSelector _bestPodOStationCandidateSelector = null;
        /// <summary>
        /// A helper used to determine the best candidate.
        /// </summary>
        private BestCandidateSelector _bestOStationCandidateSelector = null;

        /// <summary>
        /// Allocates an available store task to the bot for the predefined input-station. If no task is available the search might be extended to neighbour-stations or a rest task is done.
        /// </summary>
        /// <param name="bot">The bot to allocate a task to.</param>
        /// <param name="iStation">The station to do work for.</param>
        /// <param name="extendSearch">Indicates whether the search can be extended to neighbor stations.</param>
        /// <param name="extendSearchRadius">The radius by which the search can be expanded.</param>
        /// <param name="config">Pod selection config to use.</param>
        /// <returns>true if bot has a new store task or is just parking the pod, false if it is doing a rest task.</returns>
        protected bool DoStoreTaskForStation(Bot bot, InputStation iStation, bool extendSearch, double extendSearchRadius, DefaultPodSelectionConfiguration config)
        {
            // Init
            InitPodSelection();
            // --> Prepare best candidate selectors
            if (_bestIStationCandidateSelector == null)
            {
                _bestIStationCandidateSelector = new BestCandidateSelector(false,
                    GenerateScorerIStationForBotWithPod(config.InputExtendedSearchScorer),
                    GenerateScorerIStationForBotWithPod(config.InputExtendedSearchScorerTieBreaker1),
                    GenerateScorerIStationForBotWithPod(config.InputExtendedSearchScorerTieBreaker2));
            }
            if (_bestPodIStationCandidateSelector == null)
            {
                _bestPodIStationCandidateSelector = new BestCandidateSelector(false,
                    GenerateScorerPodForIStationBot(config.InputPodScorer),
                    GenerateScorerPodForIStationBot(config.InputPodScorerTieBreaker1),
                    GenerateScorerPodForIStationBot(config.InputPodScorerTieBreaker2));
            }

            // Try another task with the current pod if there is one
            if (bot.Pod != null)
            {
                // Search for requests fitting the pod
                if (AnyRelevantRequests(bot.Pod, iStation))
                {
                    // Get all fitting requests
                    List<InsertRequest> fittingRequests = GetPossibleRequests(bot.Pod, iStation);
                    // Log
                    Instance.LogVerbose("PC (insert): Recycling combination (" + fittingRequests.Count + " requests)");
                    // Simply execute the next task with the pod
                    EnqueueInsert(
                        bot, // The bot looking for a task
                        iStation, // The current station of the bot
                        bot.Pod, // The pod the bot is already carrying
                        fittingRequests.ToList()); // The requests to serve

                    // Finished search for next task
                    return true;
                }
                else
                {
                    // Look for work to do for nearby stations (if desired)
                    if (extendSearch)
                    {
                        // Find best station to do the next task for
                        InputStation bestStation = null;
                        _bestIStationCandidateSelector.Recycle();
                        foreach (var station in bot.Tier.InputStations
                            // Limit search by distance to currently assigned station
                            .Where(s => s != iStation && s.Tier == iStation.Tier && Distances.CalculateEuclid(iStation, s, Instance.WrongTierPenaltyDistance) <= extendSearchRadius)
                            // Get best station while filtering by any work to do for it
                            .Where(s => AnyRelevantRequests(bot.Pod, s)))
                        {
                            // Update current candidate to assess
                            _currentBot = bot;
                            _currentIStation = station;
                            // Check whether the current combination is better
                            if (_bestIStationCandidateSelector.Reassess())
                            {
                                // Update best candidate
                                bestStation = _currentIStation;
                            }
                        }
                        // If a station is suitable, add the fitting requests
                        if (bestStation != null)
                        {
                            // Get all fitting requests
                            List<InsertRequest> fittingRequests = GetPossibleRequests(bot.Pod, bestStation);
                            // Log
                            Instance.LogVerbose("PC (insert): Recycling pod only (" + fittingRequests.Count + " requests)");
                            // Simply execute the next task with the pod
                            EnqueueInsert(
                                bot, // The bot looking for a task
                                bestStation, // The current station of the bot
                                bot.Pod, // The pod the bot is already carrying
                                fittingRequests.ToList()); // The requests to serve
                            // Log score statistics
                            if (_statIStationForPodScorerValues == null)
                                _statIStationForPodScorerValues = _bestIStationCandidateSelector.BestScores.ToArray();
                            else
                                for (int i = 0; i < _bestIStationCandidateSelector.BestScores.Length; i++)
                                    _statIStationForPodScorerValues[i] += _bestIStationCandidateSelector.BestScores[i];
                            _statIStationForPodAssignments++;
                            // Finished search for next task
                            return true;
                        }
                    }

                    // Pod is not useful anymore - put it away
                    EnqueueParkPod(bot, bot.Pod, Instance.Controller.PodStorageManager.GetStorageLocation(bot.Pod));
                    return true;
                }
            }
            else
            {
                // Determine best pod
                Pod bestPod = null;
                _bestPodIStationCandidateSelector.Recycle();
                foreach (var pod in Instance.ResourceManager.UnusedPods
                    // Get best pod while ensuring that any work can be done with it
                    .Where(p => AnyRelevantRequests(p, iStation)))
                {
                    // Update current candidate to assess
                    _currentBot = bot;
                    _currentIStation = iStation;
                    _currentPod = pod;
                    // Check whether the current combination is better
                    if (_bestPodIStationCandidateSelector.Reassess())
                    {
                        // Update best candidate
                        bestPod = _currentPod;
                    }
                }
                // See whether there was a suitable pod
                if (bestPod != null)
                {
                    // Get all fitting requests
                    List<InsertRequest> fittingRequests = GetPossibleRequests(bestPod, iStation);
                    // Log
                    Instance.LogVerbose("PC (insert): New pod (" + fittingRequests.Count + " requests)");
                    // Simply execute the next task with the pod
                    EnqueueInsert(
                        bot, // The bot looking for a task
                        iStation, // The current station of the bot
                        bestPod, // The new pod
                        fittingRequests.ToList()); // The requests to serve
                    // Log score statistics
                    if (_statPodForIStationScorerValues == null)
                        _statPodForIStationScorerValues = _bestPodIStationCandidateSelector.BestScores.ToArray();
                    else
                        for (int i = 0; i < _bestPodIStationCandidateSelector.BestScores.Length; i++)
                            _statPodForIStationScorerValues[i] += _bestPodIStationCandidateSelector.BestScores[i];
                    _statPodForIStationAssignments++;
                    // Finished search for next task
                    return true;
                }
            }

            // Signal no task found
            return false;
        }

        /// <summary>
        /// Allocates an available extract task to the bot for the predefined output-station. If no task is available the search might be extended to neighbour-stations or a rest task is done.
        /// </summary>
        /// <param name="bot">The bot to allocate a task to.</param>
        /// <param name="oStation">The station to do work for.</param>
        /// <param name="extendSearch">Indicates whether the search can be extended to neighbor stations.</param>
        /// <param name="extendedSearchRadius">The radius by which the search can be expanded.</param>
        /// <param name="config">Pod selection config to use.</param>
        /// /// <returns>true if bot has a new extract task or is just parking the pod, false if it is doing a rest task.</returns>
        protected bool DoExtractTaskForStation(Bot bot, OutputStation oStation, bool extendSearch, double extendedSearchRadius, DefaultPodSelectionConfiguration config)
        {
            // Init
            InitPodSelection();
            // --> Prepare best candidate selectors
            if (_bestOStationCandidateSelector == null)
            {
                _bestOStationCandidateSelector = new BestCandidateSelector(false,
                    GenerateScorerOStationForBotWithPod(config.OutputExtendedSearchScorer),
                    GenerateScorerOStationForBotWithPod(config.OutputExtendedSearchScorerTieBreaker1),
                    GenerateScorerOStationForBotWithPod(config.OutputExtendedSearchScorerTieBreaker2));
            }
            if (_bestPodOStationCandidateSelector == null)
            {
                _bestPodOStationCandidateSelector = new BestCandidateSelector(false,
                    GenerateScorerPodForOStationBot(config.OutputPodScorer),
                    GenerateScorerPodForOStationBot(config.OutputPodScorerTieBreaker1),
                    GenerateScorerPodForOStationBot(config.OutputPodScorerTieBreaker2));
            }

            // Try another task with the current pod if there is one
            if (bot.Pod != null)
            {
                // Check for more work to do with the current combination
                if (AnyRelevantRequests(bot.Pod, oStation, config.FilterForConsideration))
                {
                    // Get all fitting requests
                    List<ExtractRequest> fittingRequests = GetPossibleRequests(bot.Pod, oStation, config.FilterForReservation);
                    // Log
                    Instance.LogVerbose("PC (extract): Recycling combination (" + fittingRequests.Count + " requests)");
                    // Simply execute the next task with the pod
                    EnqueueExtract(
                        bot, // The bot itself
                        oStation, // The current station
                        bot.Pod, // Keep the pod
                        fittingRequests); // The requests to serve
                    return true;
                }
                else
                {
                    // Look for work to do for nearby stations (if desired)
                    if (extendSearch)
                    {
                        // Find best station to do the next task for
                        OutputStation bestStation = null;
                        _bestOStationCandidateSelector.Recycle();
                        foreach (var station in bot.Tier.OutputStations
                            // Limit search by distance to currently assigned station
                            .Where(s => s != oStation && s.Tier == oStation.Tier && Distances.CalculateEuclid(oStation, s, Instance.WrongTierPenaltyDistance) <= extendedSearchRadius)
                            // Get best station while filtering by any work to do for it
                            .Where(s => AnyRelevantRequests(bot.Pod, s, config.FilterForConsideration)))
                        {
                            // Update current candidate to assess
                            _currentBot = bot;
                            _currentOStation = station;
                            // Check whether the current combination is better
                            if (_bestOStationCandidateSelector.Reassess())
                            {
                                // Update best candidate
                                bestStation = _currentOStation;
                            }
                        }
                        // See whether there was a suitable station
                        if (bestStation != null)
                        {
                            // Get all fitting requests
                            List<ExtractRequest> fittingRequests = GetPossibleRequests(bot.Pod, bestStation, config.FilterForReservation);
                            // Log
                            Instance.LogVerbose("PC (extract): Recycling pod only (" + fittingRequests.Count + " requests)");
                            // Simply execute the next task with the pod
                            EnqueueExtract(
                                bot, // The bot itself
                                bestStation, // The current station
                                bot.Pod, // Keep the pod
                                fittingRequests); // The requests to serve
                            // Log score statistics
                            if (_statOStationForPodScorerValues == null)
                                _statOStationForPodScorerValues = _bestOStationCandidateSelector.BestScores.ToArray();
                            else
                                for (int i = 0; i < _bestOStationCandidateSelector.BestScores.Length; i++)
                                    _statOStationForPodScorerValues[i] += _bestOStationCandidateSelector.BestScores[i];
                            _statOStationForPodAssignments++;
                            return true;
                        }
                    }

                    // Pod is not useful anymore - put it away
                    EnqueueParkPod(bot, bot.Pod, Instance.Controller.PodStorageManager.GetStorageLocation(bot.Pod));
                    return true;
                }
            }
            else
            {
                // Determine best pod
                Pod bestPod = null;
                _bestPodOStationCandidateSelector.Recycle();
                foreach (var pod in Instance.ResourceManager.UnusedPods
                    // Get best pod while ensuring that any work can be done with it
                    .Where(p => AnyRelevantRequests(p, oStation, config.FilterForConsideration)))
                {
                    // Update current candidate to assess
                    _currentBot = bot;
                    _currentOStation = oStation;
                    _currentPod = pod;
                    // Check whether the current combination is better
                    if (_bestPodOStationCandidateSelector.Reassess())
                    {
                        // Update best candidate
                        bestPod = _currentPod;
                    }
                }
                // See whether there was any suitable pod
                if (bestPod != null)
                {
                    // Get all fitting requests
                    List<ExtractRequest> fittingRequests = GetPossibleRequests(bestPod, oStation, config.FilterForReservation);
                    // Log
                    Instance.LogVerbose("PC (extract): New pod (" + fittingRequests.Count + " requests)");
                    // Simply execute the next task with the pod
                    EnqueueExtract(
                        bot, // The bot itself
                        oStation, // The current station
                        bestPod, // The new pod
                        fittingRequests); // The requests to serve
                    // Log score statistics
                    if (_statPodForOStationScorerValues == null)
                        _statPodForOStationScorerValues = _bestPodOStationCandidateSelector.BestScores.ToArray();
                    else
                        for (int i = 0; i < _bestPodOStationCandidateSelector.BestScores.Length; i++)
                            _statPodForOStationScorerValues[i] += _bestPodOStationCandidateSelector.BestScores[i];
                    _statPodForOStationAssignments++;
                    Instance.StatCustomControllerInfo.CustomLogPC1 = _statPodForOStationScorerValues[0] / _statPodForOStationAssignments;
                    Instance.StatCustomControllerInfo.CustomLogPC2 = _statPodForOStationScorerValues[1] / _statPodForOStationAssignments;
                    Instance.StatCustomControllerInfo.CustomLogPC3 = _statPodForOStationScorerValues[2] / _statPodForOStationAssignments;
                    return true;
                }
            }

            // Signal no task found
            return false;
        }

        #endregion

        #region On-the-fly work helpers

        /// <summary>
        /// Indicates whether we already have looked at the current order situation of the corresponding station (no need to look for additional work to do with the current pods).
        /// </summary>
        private MultiKeyDictionary<OutputStation, Bot, bool> _outputStationHasPotentialOnTheFlyWork;
        /// <summary>
        /// Indicates whether we already have looked at the current bundle situation of the corresponding station (no need to look for additional work to do with the current pods).
        /// </summary>
        private MultiKeyDictionary<InputStation, Bot, bool> _inputStationHasPotentialOnTheFlyWork;
        /// <summary>
        /// Indicates whether the current extract request situation was already investigated.
        /// </summary>
        private bool _onTheFlyExtractSituationInvestigated = false;
        /// <summary>
        /// Indicates whether the current store request situation was already investigated.
        /// </summary>
        private bool _onTheFlyStoreSituationInvestigated = false;
        /// <summary>
        /// This is called whenever a new order is assigned to a station, hence, introducing potential new work to do with pods inbound at that station.
        /// </summary>
        /// <param name="oStation">The output station that got a new order assigned to it.</param>
        /// <param name="order">The newly assigned order.</param>
        private void OrderAllocated(OutputStation oStation, Order order)
        {
            if (_outputStationHasPotentialOnTheFlyWork == null)
                InitHasPotentialOnTheFlyWork();
            foreach (var bot in Instance.Bots)
                _outputStationHasPotentialOnTheFlyWork[oStation, bot] = true;
            _onTheFlyExtractSituationInvestigated = false;
        }
        /// <summary>
        /// This is called whenever a new bundle is assigned to a station, hence, introducing potential new work to do with pods inbound at the station.
        /// </summary>
        /// <param name="iStation">The input station that got a new bundle assigned to it.</param>
        /// <param name="bundle">The newly assigned bundle.</param>
        private void BundleAllocated(InputStation iStation, ItemBundle bundle)
        {
            if (_inputStationHasPotentialOnTheFlyWork == null)
                InitHasPotentialOnTheFlyWork();
            foreach (var bot in Instance.Bots)
                _inputStationHasPotentialOnTheFlyWork[iStation, bot] = true;
            _onTheFlyStoreSituationInvestigated = false;
        }
        /// <summary>
        /// This is called whenever a pod was picked up, hence, introducing potential new work to do with the new pod inbound to a station.
        /// </summary>
        /// <param name="pod">The pod that was picked up.</param>
        /// <param name="bot">The bot that picked up the pod.</param>
        private void PodPickup(Pod pod, Bot bot)
        {
            if (bot.CurrentTask is ExtractTask)
                _onTheFlyExtractSituationInvestigated = false;
            if (bot.CurrentTask is InsertTask)
                _onTheFlyStoreSituationInvestigated = false;
        }
        /// <summary>
        /// Initializes the potential on-the-fly work information.
        /// </summary>
        private void InitHasPotentialOnTheFlyWork()
        {
            _outputStationHasPotentialOnTheFlyWork = new MultiKeyDictionary<OutputStation, Bot, bool>();
            foreach (var bot in Instance.Bots)
                foreach (var station in Instance.OutputStations)
                    _outputStationHasPotentialOnTheFlyWork[station, bot] = true;
            _inputStationHasPotentialOnTheFlyWork = new MultiKeyDictionary<InputStation, Bot, bool>();
            foreach (var bot in Instance.Bots)
                foreach (var station in Instance.InputStations)
                    _inputStationHasPotentialOnTheFlyWork[station, bot] = true;
        }

        /// <summary>
        /// Tries to assign more additional requests to the tasks currently executed by the robots.
        /// </summary>
        /// <param name="config">The config to use.</param>
        protected void AssignOnTheFlyWork(DefaultPodSelectionConfiguration config)
        {
            // Check whether there is any new work to assign
            if (!_onTheFlyExtractSituationInvestigated || !_onTheFlyStoreSituationInvestigated)
            {
                // Search all bots for more work
                foreach (var bot in Instance.Bots
                    // Filter out bots for which no additional work can be added
                    .Where(b =>
                    {
                        // Ensure that there is any work to do
                        if (b.CurrentTask is ExtractTask)
                            return
                                // Only check, if there is any on-the-fly extract work
                                !_onTheFlyExtractSituationInvestigated &&
                                // Only check bots that are already carrying a pod
                                b.Pod != null &&
                                // Only check constellations not previously checked
                                _outputStationHasPotentialOnTheFlyWork[(b.CurrentTask as ExtractTask).OutputStation, b] &&
                                // Only check bots that still have requests left in their extract task
                                (b.CurrentTask as ExtractTask).Requests.Any();
                        // Ensure that there is any work to do
                        else if (b.CurrentTask is InsertTask)
                            return
                                // Only check, if there is any on-the-fly store work
                                !_onTheFlyStoreSituationInvestigated &&
                                // Only check bots that are already carrying a pod
                                b.Pod != null &&
                                // Only check constellations not previously checked
                                _inputStationHasPotentialOnTheFlyWork[(b.CurrentTask as InsertTask).InputStation, b] &&
                                // Only check bots that still have requests left in their store task
                                (b.CurrentTask as InsertTask).Requests.Any();
                        // This bot is neither doing an extract nor a store task - ignore it
                        else return false;
                    })
                    // Order bots by the distance to their respective target station
                    .OrderBy(b =>
                    {
                        // Get the station the bot is going to
                        Waypoint stationWP =
                                b.CurrentTask is ExtractTask ? (b.CurrentTask as ExtractTask).OutputStation.Waypoint :
                                b.CurrentTask is InsertTask ? (b.CurrentTask as InsertTask).InputStation.Waypoint :
                                null;
                        // Ensure that we already know the station - If not, penalize it
                        if (stationWP == null)
                            return double.MaxValue;
                        // Use the shortest path to the station when bot is already queued
                        if (b.CurrentWaypoint != null && b.CurrentWaypoint.IsQueueWaypoint)
                            // Calculate the shortest path
                            return Distances.CalculateShortestPathPodSafe(b.CurrentWaypoint, stationWP, Instance);
                        else
                            // Simply use the manhattan distance and penalize it to always prefer queued robots first
                            return Distances.CalculateManhattan(b, stationWP, Instance.WrongTierPenaltyDistance) + Instance.WrongTierPenaltyDistance;
                    }))
                {
                    // Add additional extract requests to the current task
                    if (config.OnTheFlyExtract && bot.CurrentTask is ExtractTask)
                    {
                        // Fetch the task
                        ExtractTask extractTask = bot.CurrentTask as ExtractTask;
                        // Match more items to orders of the station, if possible
                        List<ExtractRequest> itemsToHandle = GetPossibleRequests(bot.Pod, extractTask.OutputStation, config.FilterForReservation);
                        // Add new matches to current task
                        foreach (var item in itemsToHandle)
                            extractTask.AddRequest(item);
                        // Mark situation investigated
                        _outputStationHasPotentialOnTheFlyWork[extractTask.OutputStation, bot] = false;
                    }
                    // Add additional insert requests to the current task
                    else if (config.OnTheFlyStore && bot.CurrentTask is InsertTask)
                    {
                        // Fetch the task
                        InsertTask storeTask = bot.CurrentTask as InsertTask;
                        // Match more bundles to orders of the station, if possible
                        List<InsertRequest> bundlesToHandle = GetPossibleRequests(bot.Pod, storeTask.InputStation);
                        // Add new matches to current task
                        foreach (var bundle in bundlesToHandle)
                            storeTask.AddRequest(bundle);
                        // Mark situation investigated
                        _inputStationHasPotentialOnTheFlyWork[storeTask.InputStation, bot] = false;
                    }
                }
                // Mark situation investigated
                _onTheFlyExtractSituationInvestigated = true;
                _onTheFlyStoreSituationInvestigated = true;
            }
        }

        #endregion

        #region Custom stat tracking

        /// <summary>
        /// Contains the aggregated scorer values.
        /// </summary>
        private double[] _statPodForOStationScorerValues = null;
        /// <summary>
        /// Contains the number of assignments done.
        /// </summary>
        private double _statPodForOStationAssignments = 0;
        /// <summary>
        /// Contains the aggregated scorer values.
        /// </summary>
        private double[] _statPodForIStationScorerValues = null;
        /// <summary>
        /// Contains the number of assignments done.
        /// </summary>
        private double _statPodForIStationAssignments = 0;
        /// <summary>
        /// Contains the aggregated scorer values.
        /// </summary>
        private double[] _statOStationForPodScorerValues = null;
        /// <summary>
        /// Contains the number of assignments done.
        /// </summary>
        private double _statOStationForPodAssignments = 0;
        /// <summary>
        /// Contains the aggregated scorer values.
        /// </summary>
        private double[] _statIStationForPodScorerValues = null;
        /// <summary>
        /// Contains the number of assignments done.
        /// </summary>
        private double _statIStationForPodAssignments = 0;
        /// <summary>
        /// The callback indicates a reset of the statistics.
        /// </summary>
        private void StatResetPC()
        {
            _statPodForOStationScorerValues = null;
            _statPodForOStationAssignments = 0;
            _statPodForIStationScorerValues = null;
            _statPodForIStationAssignments = 0;
            _statOStationForPodScorerValues = null;
            _statOStationForPodAssignments = 0;
            _statIStationForPodScorerValues = null;
            _statIStationForPodAssignments = 0;
        }
        /// <summary>
        /// The callback that indicates that the simulation is finished and statistics have to submitted to the instance.
        /// </summary>
        private void StatFinishPC()
        {
            List<string> scoreInfos = new List<string>();
            if (_statPodForOStationScorerValues != null)
                scoreInfos.Add("PForO" + IOConstants.DELIMITER_CUSTOM_CONTROLLER_FOOTPRINT + string.Join(IOConstants.DELIMITER_CUSTOM_CONTROLLER_FOOTPRINT.ToString(),
                    _statPodForOStationScorerValues.Select(e => e / _statPodForOStationAssignments).Select(e => e.ToString(IOConstants.FORMATTER))));
            if (_statPodForIStationScorerValues != null)
                scoreInfos.Add("PForI" + IOConstants.DELIMITER_CUSTOM_CONTROLLER_FOOTPRINT + string.Join(IOConstants.DELIMITER_CUSTOM_CONTROLLER_FOOTPRINT.ToString(),
                    _statPodForIStationScorerValues.Select(e => e / _statPodForIStationAssignments).Select(e => e.ToString(IOConstants.FORMATTER))));
            if (_statOStationForPodScorerValues != null)
                scoreInfos.Add("OForP" + IOConstants.DELIMITER_CUSTOM_CONTROLLER_FOOTPRINT + string.Join(IOConstants.DELIMITER_CUSTOM_CONTROLLER_FOOTPRINT.ToString(),
                    _statOStationForPodScorerValues.Select(e => e / _statOStationForPodAssignments).Select(e => e.ToString(IOConstants.FORMATTER))));
            if (_statIStationForPodScorerValues != null)
                scoreInfos.Add("IForP" + IOConstants.DELIMITER_CUSTOM_CONTROLLER_FOOTPRINT + string.Join(IOConstants.DELIMITER_CUSTOM_CONTROLLER_FOOTPRINT.ToString(),
                    _statIStationForPodScorerValues.Select(e => e / _statIStationForPodAssignments).Select(e => e.ToString(IOConstants.FORMATTER))));
            Instance.StatCustomControllerInfo.CustomLogPCString = string.Join(IOConstants.DELIMITER_CUSTOM_CONTROLLER_FOOTPRINT.ToString(), scoreInfos);
        }

        #endregion
    }
}
