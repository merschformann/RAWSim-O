using RAWSimO.Core.Metrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static RAWSimO.Core.Control.BotManager;

namespace RAWSimO.Core.Configurations
{
    #region Rest location scorers

    /// <summary>
    /// Determines the ordering for the selection of a rest location for a robot.
    /// </summary>
    public enum PrefRestLocationForBot
    {
        /// <summary>
        /// Order by random.
        /// </summary>
        Random,
        /// <summary>
        /// Order by random, but prefer locations on the same tier.
        /// </summary>
        RandomSameTier,
        /// <summary>
        /// Order by vicinity to middle of the tier.
        /// </summary>
        Middle,
        /// <summary>
        /// Order by vicinity to middle of the tier, but prefer the same tier.
        /// </summary>
        MiddleSameTier,
        /// <summary>
        /// Order by vicinity to bot.
        /// </summary>
        Nearest,
    }

    #endregion

    #region Pod selection scorers (input-station for bot carrying a pod)

    /// <summary>
    /// Determines the ordering for the selection of an input station for a robot.
    /// </summary>
    public enum PrefIStationForBotWithPod
    {
        /// <summary>
        /// Order by random.
        /// </summary>
        Random,
        /// <summary>
        /// Order by the distance between bot and station.
        /// </summary>
        Nearest,
        /// <summary>
        /// Order by the estimate amount of work that can be done at the station.
        /// </summary>
        WorkAmount,
    }
    /// <summary>
    /// The base class for all pod selection scorer parameter classes.
    /// </summary>
    [XmlInclude(typeof(PCScorerIStationForBotWithPodRandom))]
    [XmlInclude(typeof(PCScorerIStationForBotWithPodNearest))]
    [XmlInclude(typeof(PCScorerIStationForBotWithPodWorkAmount))]
    public abstract class PCScorerIStationForBotWithPod
    {
        /// <summary>
        /// The type of the method.
        /// </summary>
        /// <returns>The type.</returns>
        public abstract PrefIStationForBotWithPod Type();
    }
    /// <summary>
    /// Exposes the parameters for the given pod selection scorer / strategy.
    /// </summary>
    public class PCScorerIStationForBotWithPodRandom : PCScorerIStationForBotWithPod
    {
        /// <summary>
        /// The type of the method.
        /// </summary>
        /// <returns>The type.</returns>
        public override PrefIStationForBotWithPod Type() { return PrefIStationForBotWithPod.Random; }
        /// <summary>
        /// Indicates whether to prefer the same tier.
        /// </summary>
        public bool PreferSameTier = true;
    }
    /// <summary>
    /// Exposes the parameters for the given pod selection scorer / strategy.
    /// </summary>
    public class PCScorerIStationForBotWithPodNearest : PCScorerIStationForBotWithPod
    {
        /// <summary>
        /// The type of the method.
        /// </summary>
        /// <returns>The type.</returns>
        public override PrefIStationForBotWithPod Type() { return PrefIStationForBotWithPod.Nearest; }
        /// <summary>
        /// Indicates whether to prefer the same tier.
        /// </summary>
        public DistanceMetricType DistanceMetric = DistanceMetricType.Euclidean;
    }
    /// <summary>
    /// Exposes the parameters for the given pod selection scorer / strategy.
    /// </summary>
    public class PCScorerIStationForBotWithPodWorkAmount : PCScorerIStationForBotWithPod
    {
        /// <summary>
        /// The type of the method.
        /// </summary>
        /// <returns>The type.</returns>
        public override PrefIStationForBotWithPod Type() { return PrefIStationForBotWithPod.WorkAmount; }
        /// <summary>
        /// Indicates whether to consider the age of the 
        /// </summary>
        public bool IncludeAge = false;
        /// <summary>
        /// Indicates whether to prefer the same tier.
        /// </summary>
        public bool PreferSameTier = true;
    }

    #endregion

    #region Pod selection scorers (output-station for bot carrying a pod)

    /// <summary>
    /// Determines the ordering for the selection of an output station for a robot.
    /// </summary>
    public enum PrefOStationForBotWithPod
    {
        /// <summary>
        /// Order by random.
        /// </summary>
        Random,
        /// <summary>
        /// Order by the distance between bot and station.
        /// </summary>
        Nearest,
        /// <summary>
        /// Order by the estimate amount of work that can be done at the station.
        /// </summary>
        WorkAmount,
    }
    /// <summary>
    /// The base class for all pod selection scorer parameter classes.
    /// </summary>
    [XmlInclude(typeof(PCScorerOStationForBotWithPodRandom))]
    [XmlInclude(typeof(PCScorerOStationForBotWithPodNearest))]
    [XmlInclude(typeof(PCScorerOStationForBotWithPodWorkAmount))]
    public abstract class PCScorerOStationForBotWithPod
    {
        /// <summary>
        /// The type of the method.
        /// </summary>
        /// <returns>The type.</returns>
        public abstract PrefOStationForBotWithPod Type();
    }
    /// <summary>
    /// Exposes the parameters for the given pod selection scorer / strategy.
    /// </summary>
    public class PCScorerOStationForBotWithPodRandom : PCScorerOStationForBotWithPod
    {
        /// <summary>
        /// The type of the method.
        /// </summary>
        /// <returns>The type.</returns>
        public override PrefOStationForBotWithPod Type() { return PrefOStationForBotWithPod.Random; }
        /// <summary>
        /// Indicates whether to prefer the same tier.
        /// </summary>
        public bool PreferSameTier = true;
    }
    /// <summary>
    /// Exposes the parameters for the given pod selection scorer / strategy.
    /// </summary>
    public class PCScorerOStationForBotWithPodNearest : PCScorerOStationForBotWithPod
    {
        /// <summary>
        /// The type of the method.
        /// </summary>
        /// <returns>The type.</returns>
        public override PrefOStationForBotWithPod Type() { return PrefOStationForBotWithPod.Nearest; }
        /// <summary>
        /// Indicates whether to prefer the same tier.
        /// </summary>
        public DistanceMetricType DistanceMetric = DistanceMetricType.Euclidean;
    }
    /// <summary>
    /// Exposes the parameters for the given pod selection scorer / strategy.
    /// </summary>
    public class PCScorerOStationForBotWithPodWorkAmount : PCScorerOStationForBotWithPod
    {
        /// <summary>
        /// The type of the method.
        /// </summary>
        /// <returns>The type.</returns>
        public override PrefOStationForBotWithPod Type() { return PrefOStationForBotWithPod.WorkAmount; }
        /// <summary>
        /// Indicates the base metric to calculate the score value.
        /// </summary>
        public PCScorerWorkAmountValueMetric ValueMetric = PCScorerWorkAmountValueMetric.Picks;
        /// <summary>
        /// Indicates whether lateness will only be considered as a positive value, i.e. only already late orders will make a difference in the score value.
        /// </summary>
        public bool OnlyPositiveLateness = false;
        /// <summary>
        /// Indicates whether to prefer the same tier.
        /// </summary>
        public bool PreferSameTier = true;
    }

    #endregion

    #region Pod selection scorers (pod for bot working for an input-station)

    /// <summary>
    /// Determines the ordering for the selection of an input station for a pod.
    /// </summary>
    public enum PrefPodForIStationBot
    {
        /// <summary>
        /// Order by random.
        /// </summary>
        Random,
        /// <summary>
        /// Order by the distance between pod and bot.
        /// </summary>
        Nearest,
        /// <summary>
        /// Order by demand for the bundles.
        /// </summary>
        Demand,
        /// <summary>
        /// Order by the estimate amount of work that can be done at the station.
        /// </summary>
        WorkAmount,
    }
    /// <summary>
    /// The base class for all pod selection scorer parameter classes.
    /// </summary>
    [XmlInclude(typeof(PCScorerPodForIStationBotRandom))]
    [XmlInclude(typeof(PCScorerPodForIStationBotNearest))]
    [XmlInclude(typeof(PCScorerPodForIStationBotDemand))]
    [XmlInclude(typeof(PCScorerPodForIStationBotWorkAmount))]
    public abstract class PCScorerPodForIStationBot
    {
        /// <summary>
        /// The type of the method.
        /// </summary>
        /// <returns>The type.</returns>
        public abstract PrefPodForIStationBot Type();
    }
    /// <summary>
    /// Exposes the parameters for the given pod selection scorer / strategy.
    /// </summary>
    public class PCScorerPodForIStationBotRandom : PCScorerPodForIStationBot
    {
        /// <summary>
        /// The type of the method.
        /// </summary>
        /// <returns>The type.</returns>
        public override PrefPodForIStationBot Type() { return PrefPodForIStationBot.Random; }
        /// <summary>
        /// Indicates whether to prefer the same tier.
        /// </summary>
        public bool PreferSameTier = true;
    }
    /// <summary>
    /// Exposes the parameters for the given pod selection scorer / strategy.
    /// </summary>
    public class PCScorerPodForIStationBotNearest : PCScorerPodForIStationBot
    {
        /// <summary>
        /// The type of the method.
        /// </summary>
        /// <returns>The type.</returns>
        public override PrefPodForIStationBot Type() { return PrefPodForIStationBot.Nearest; }
        /// <summary>
        /// Indicates whether to prefer the same tier.
        /// </summary>
        public DistanceMetricType DistanceMetric = DistanceMetricType.Euclidean;
        /// <summary>
        /// Indicates whether to estimate the distance between the bot and the pod instead of fully calculating it.
        /// </summary>
        public bool EstimateBotPodDistance = true;
    }
    /// <summary>
    /// Exposes the parameters for the given pod selection scorer / strategy.
    /// </summary>
    public class PCScorerPodForIStationBotDemand : PCScorerPodForIStationBot
    {
        /// <summary>
        /// The type of the method.
        /// </summary>
        /// <returns>The type.</returns>
        public override PrefPodForIStationBot Type() { return PrefPodForIStationBot.Demand; }
    }
    /// <summary>
    /// Exposes the parameters for the given pod selection scorer / strategy.
    /// </summary>
    public class PCScorerPodForIStationBotWorkAmount : PCScorerPodForIStationBot
    {
        /// <summary>
        /// The type of the method.
        /// </summary>
        /// <returns>The type.</returns>
        public override PrefPodForIStationBot Type() { return PrefPodForIStationBot.WorkAmount; }
        /// <summary>
        /// Indicates whether to consider the age of the 
        /// </summary>
        public bool IncludeAge = false;
        /// <summary>
        /// Indicates whether to prefer the same tier.
        /// </summary>
        public bool PreferSameTier = true;
    }

    #endregion

    #region Pod selection scorers (pod for bot working for an output-station)

    /// <summary>
    /// Determines the ordering for the selection of an output station for a pod.
    /// </summary>
    public enum PrefPodForOStationBot
    {
        /// <summary>
        /// Order by random.
        /// </summary>
        Random,
        /// <summary>
        /// Order by fill level.
        /// </summary>
        Fill,
        /// <summary>
        /// Order by the distance.
        /// </summary>
        Nearest,
        /// <summary>
        /// Order by combined demand.
        /// </summary>
        Demand,
        /// <summary>
        /// Order by number of completeable orders.
        /// </summary>
        Completeable,
        /// <summary>
        /// Order by the estimate amount of work that can be done at the station.
        /// </summary>
        WorkAmount,
    }
    /// <summary>
    /// The base class for all pod selection scorer parameter classes.
    /// </summary>
    [XmlInclude(typeof(PCScorerPodForOStationBotRandom))]
    [XmlInclude(typeof(PCScorerPodForOStationBotFill))]
    [XmlInclude(typeof(PCScorerPodForOStationBotNearest))]
    [XmlInclude(typeof(PCScorerPodForOStationBotDemand))]
    [XmlInclude(typeof(PCScorerPodForOStationBotCompleteable))]
    [XmlInclude(typeof(PCScorerPodForOStationBotWorkAmount))]
    public abstract class PCScorerPodForOStationBot
    {
        /// <summary>
        /// The type of the method.
        /// </summary>
        /// <returns>The type.</returns>
        public abstract PrefPodForOStationBot Type();
    }
    /// <summary>
    /// Exposes the parameters for the given pod selection scorer / strategy.
    /// </summary>
    public class PCScorerPodForOStationBotRandom : PCScorerPodForOStationBot
    {
        /// <summary>
        /// The type of the method.
        /// </summary>
        /// <returns>The type.</returns>
        public override PrefPodForOStationBot Type() { return PrefPodForOStationBot.Random; }
        /// <summary>
        /// Indicates whether to prefer the same tier.
        /// </summary>
        public bool PreferSameTier = true;
    }
    /// <summary>
    /// Exposes the parameters for the given pod selection scorer / strategy.
    /// </summary>
    public class PCScorerPodForOStationBotFill : PCScorerPodForOStationBot
    {
        /// <summary>
        /// The type of the method.
        /// </summary>
        /// <returns>The type.</returns>
        public override PrefPodForOStationBot Type() { return PrefPodForOStationBot.Fill; }
        /// <summary>
        /// Indicates whether we are looking for a pod with a lot of inventory on it.
        /// </summary>
        public bool PreferFullest = true;
        /// <summary>
        /// Indicates whether a relative threshold is used to distinguish between full / empty pods.
        /// </summary>
        public bool Binary = true;
        /// <summary>
        /// The threshold to distinguish between full / empty pods when in binary mode.
        /// </summary>
        public double Threshold = 0.7;
    }
    /// <summary>
    /// Exposes the parameters for the given pod selection scorer / strategy.
    /// </summary>
    public class PCScorerPodForOStationBotNearest : PCScorerPodForOStationBot
    {
        /// <summary>
        /// The type of the method.
        /// </summary>
        /// <returns>The type.</returns>
        public override PrefPodForOStationBot Type() { return PrefPodForOStationBot.Nearest; }
        /// <summary>
        /// Indicates whether to prefer the same tier.
        /// </summary>
        public DistanceMetricType DistanceMetric = DistanceMetricType.ShortestTime;
        /// <summary>
        /// Indicates whether to estimate the distance between the bot and the pod instead of fully calculating it.
        /// </summary>
        public bool EstimateBotPodDistance = true;
    }
    /// <summary>
    /// Exposes the parameters for the given pod selection scorer / strategy.
    /// </summary>
    public class PCScorerPodForOStationBotDemand : PCScorerPodForOStationBot
    {
        /// <summary>
        /// The type of the method.
        /// </summary>
        /// <returns>The type.</returns>
        public override PrefPodForOStationBot Type() { return PrefPodForOStationBot.Demand; }
    }
    /// <summary>
    /// Exposes the parameters for the given pod selection scorer / strategy.
    /// </summary>
    public class PCScorerPodForOStationBotCompleteable : PCScorerPodForOStationBot
    {
        /// <summary>
        /// The type of the method.
        /// </summary>
        /// <returns>The type.</returns>
        public override PrefPodForOStationBot Type() { return PrefPodForOStationBot.Completeable; }
        /// <summary>
        /// Indicates whether to consider queued orders too.
        /// </summary>
        public bool IncludeQueuedOrders = true;
    }
    /// <summary>
    /// Exposes the parameters for the given pod selection scorer / strategy.
    /// </summary>
    public class PCScorerPodForOStationBotWorkAmount : PCScorerPodForOStationBot
    {
        /// <summary>
        /// The type of the method.
        /// </summary>
        /// <returns>The type.</returns>
        public override PrefPodForOStationBot Type() { return PrefPodForOStationBot.WorkAmount; }
        /// <summary>
        /// Indicates the base metric to calculate the score value.
        /// </summary>
        public PCScorerWorkAmountValueMetric ValueMetric = PCScorerWorkAmountValueMetric.Picks;
        /// <summary>
        /// Indicates whether lateness will only be considered as a positive value, i.e. only already late orders will make a difference in the score value.
        /// </summary>
        public bool OnlyPositiveLateness = false;
        /// <summary>
        /// Indicates whether to prefer the same tier.
        /// </summary>
        public bool PreferSameTier = true;
        /// <summary>
        /// The weight of picks that can potentially be done with the content of the pod for orders currently assigned to the station's individual backlog.
        /// The number expresses how one pick will be weighted compared to a pick that can be done right away with orders already assigned to the station.
        /// </summary>
        public double BacklogWeight = 0.2;
        /// <summary>
        /// The additional boost for picks that are part of an order that can be completed with the assignment, i.e. a value of one means that completeable orders count double.
        /// </summary>
        public double CompleteableOrderBoost = 1;
        /// <summary>
        /// Indicates whether completeable orders that are queued will also be considered.
        /// </summary>
        public bool CompleteableQueuedOrders = true;
        /// <summary>
        /// Indicates how much one time unit (for fetching the pod and bringing it to the station) costs in terms of picks (or in terms of eliminated age).
        /// I.e., this measure is a penalty cost added to each assignment option. Hence, assignments that use a pod further away get punished.
        /// </summary>
        public double TimeCosts = 1.0 / 3.0;
        // Obsolete (use when using shortest-path instead of shortest path time): 
        // 2/9 should be the exact amount of picks lost when driving one distance unit at full speed (robot velocity of 1.5 m/s, pick time of 3s), hence, it's the best case of lost picks
        /// <summary>
        /// Indicates whether to only estimate the distance between bot and pod in order to calculate a penalty (instead of computing the real paths with A*, computationally costly).
        /// </summary>
        public bool EstimateBotPodDistancePenalty = true;
        /// <summary>
        /// Indicates whether to only estimate the distance between pod and station in order to calculate a penalty (instead of computing the real paths with A*, computationally costly).
        /// </summary>
        public bool EstimatePodStationDistancePenalty = false;
    }

    #endregion

    #region Pod selection (sub) configurations

    /// <summary>
    /// Distinguishes different modes for the selection of pods for picking.
    /// </summary>
    public enum PodSelectionExtractRequestFilteringMode
    {
        /// <summary>
        /// Only requests already assigned to a station will be considered.
        /// </summary>
        AssignedOnly,
        /// <summary>
        /// Assigned and queued requests will be considered equally.
        /// </summary>
        AssignedAndQueuedEqually,
        /// <summary>
        /// Assigned requests and requests belonging to immediately completeable orders will be considered.
        /// </summary>
        AssignedAndCompleteQueued,
    }
    /// <summary>
    /// Exposes parameters for the default selection of pods.
    /// </summary>
    public class DefaultPodSelectionConfiguration : ControllerConfigurationBase
    {
        /// <summary>
        /// Indicates whether more suitable extract requests are included in an ongoing extract task on-the-fly.
        /// </summary>
        public bool OnTheFlyExtract = true;
        /// <summary>
        /// Indicates whether more suitable insert requests are included in an ongoing store task on-the-fly.
        /// </summary>
        public bool OnTheFlyStore = true;
        /// <summary>
        /// Indicates the mode for filtering suitable pods for picking.
        /// </summary>
        public PodSelectionExtractRequestFilteringMode FilterForConsideration = PodSelectionExtractRequestFilteringMode.AssignedAndCompleteQueued;
        /// <summary>
        /// Indicates the mode for filtering the requests when deciding the actual reservations for a pod.
        /// </summary>
        public PodSelectionExtractRequestFilteringMode FilterForReservation = PodSelectionExtractRequestFilteringMode.AssignedAndCompleteQueued;

        /// <summary>
        /// Rule settings for selecting an input station for a bot carrying a pod (main rule).
        /// </summary>
        public PCScorerIStationForBotWithPod InputExtendedSearchScorer = new PCScorerIStationForBotWithPodWorkAmount();
        /// <summary>
        /// Rule settings for selecting an input station for a bot carrying a pod (first tie breaker).
        /// </summary>
        public PCScorerIStationForBotWithPod InputExtendedSearchScorerTieBreaker1 = new PCScorerIStationForBotWithPodNearest();
        /// <summary>
        /// Rule settings for selecting an input station for a bot carrying a pod (second tie breaker).
        /// </summary>
        public PCScorerIStationForBotWithPod InputExtendedSearchScorerTieBreaker2 = new PCScorerIStationForBotWithPodRandom();
        /// <summary>
        /// Rule settings for selecting an output station for a bot carrying a pod (main rule).
        /// </summary>
        public PCScorerOStationForBotWithPod OutputExtendedSearchScorer = new PCScorerOStationForBotWithPodWorkAmount();
        /// <summary>
        /// Rule settings for selecting an output station for a bot carrying a pod (first tie breaker).
        /// </summary>
        public PCScorerOStationForBotWithPod OutputExtendedSearchScorerTieBreaker1 = new PCScorerOStationForBotWithPodNearest();
        /// <summary>
        /// Rule settings for selecting an output station for a bot carrying a pod (second tie breaker1).
        /// </summary>
        public PCScorerOStationForBotWithPod OutputExtendedSearchScorerTieBreaker2 = new PCScorerOStationForBotWithPodRandom();

        /// <summary>
        /// Rule settings for selecting a pod for a bot working for an input station (main rule).
        /// </summary>
        public PCScorerPodForIStationBot InputPodScorer = new PCScorerPodForIStationBotWorkAmount();
        /// <summary>
        /// Rule settings for selecting a pod for a bot working for an input station (first tie breaker).
        /// </summary>
        public PCScorerPodForIStationBot InputPodScorerTieBreaker1 = new PCScorerPodForIStationBotNearest();
        /// <summary>
        /// Rule settings for selecting a pod for a bot working for an input station (second tie breaker).
        /// </summary>
        public PCScorerPodForIStationBot InputPodScorerTieBreaker2 = new PCScorerPodForIStationBotRandom();
        /// <summary>
        /// Rule settings for selecting a pod for a bot working for an output station (main rule).
        /// </summary>
        public PCScorerPodForOStationBot OutputPodScorer = new PCScorerPodForOStationBotCompleteable();
        /// <summary>
        /// Rule settings for selecting a pod for a bot working for an output station (first tie breaker).
        /// </summary>
        public PCScorerPodForOStationBot OutputPodScorerTieBreaker1 = new PCScorerPodForOStationBotWorkAmount();
        /// <summary>
        /// Rule settings for selecting a pod for a bot working for an output station (second tie breaker).
        /// </summary>
        public PCScorerPodForOStationBot OutputPodScorerTieBreaker2 = new PCScorerPodForOStationBotNearest();


        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            string name = "";
            switch (InputPodScorer.Type())
            {
                case PrefPodForIStationBot.Random: name += "r"; break;
                case PrefPodForIStationBot.Nearest: name += "n"; break;
                case PrefPodForIStationBot.WorkAmount: name += "w"; break;
                default: throw new ArgumentException("Unexpected argument!");
            }
            switch (OutputPodScorer.Type())
            {
                case PrefPodForOStationBot.Random: name += "r"; break;
                case PrefPodForOStationBot.Fill: name += "f"; break;
                case PrefPodForOStationBot.Nearest: name += "n"; break;
                case PrefPodForOStationBot.Completeable: name += "c"; break;
                case PrefPodForOStationBot.WorkAmount: name += "w"; break;
                default: throw new ArgumentException("Unexpected argument!");
            }
            switch (InputExtendedSearchScorer.Type())
            {
                case PrefIStationForBotWithPod.Random: name += "r"; break;
                case PrefIStationForBotWithPod.Nearest: name += "n"; break;
                case PrefIStationForBotWithPod.WorkAmount: name += "w"; break;
                default: throw new ArgumentException("Unexpected argument!");
            }
            switch (OutputExtendedSearchScorer.Type())
            {
                case PrefOStationForBotWithPod.Random: name += "r"; break;
                case PrefOStationForBotWithPod.Nearest: name += "n"; break;
                case PrefOStationForBotWithPod.WorkAmount: name += "w"; break;
                default: throw new ArgumentException("Unexpected argument!");
            }
            return name;
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Distinguishes between different metrics for calculating the base score value wor the work amount pod selection rule.
    /// </summary>
    public enum PCScorerWorkAmountValueMetric
    {
        /// <summary>
        /// Assesses the age (time an order spent in its pool) that we can get rid off by picking the corresponding line.
        /// </summary>
        OrderAge,
        /// <summary>
        /// Assesses the lateness we can get rid off by picking the corresponding line.
        /// </summary>
        OrderDueTime,
        /// <summary>
        /// Assesses the number of picks that be done with the pod.
        /// </summary>
        Picks,
    }

    #endregion
}
