using RAWSimO.Core.Control.Defaults.OrderBatching;
using RAWSimO.Core.Control.Shared;
using RAWSimO.Core.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RAWSimO.Core.Configurations
{
    #region Order batching configurations

    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class DefaultOrderBatchingConfiguration : OrderBatchingConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override OrderBatchingMethodType GetMethodType() { return OrderBatchingMethodType.Default; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            string name = "obD";
            switch (OrderSelectionRule)
            {
                case DefaultOrderSelection.Random: name += "r"; break;
                case DefaultOrderSelection.FCFS: name += "f"; break;
                case DefaultOrderSelection.DueTime: name += "d"; break;
                case DefaultOrderSelection.FrequencyAge: name += "a"; break;
                default: throw new ArgumentException("Unexpected argument!");
            }
            switch (StationSelectionRule)
            {
                case DefaultOutputStationSelection.Random: name += "r"; break;
                case DefaultOutputStationSelection.LeastBusy: name += "l"; break;
                case DefaultOutputStationSelection.MostBusy: name += "m"; break;
                default: throw new ArgumentException("Unexpected argument!");
            }
            name += ((Recycle == true) ? "t" : "f");
            return name;
        }
        /// <summary>
        /// The rule to choose the order by.
        /// </summary>
        public DefaultOrderSelection OrderSelectionRule = DefaultOrderSelection.Random;
        /// <summary>
        /// The rule to choose the station by.
        /// </summary>
        public DefaultOutputStationSelection StationSelectionRule = DefaultOutputStationSelection.Random;
        /// <summary>
        /// Indicates whether stations are recycled, i.e. one station is filled with orders as long as there is capacity left.
        /// </summary>
        public bool Recycle = false;
        /// <summary>
        /// Indicates whether a fast lane is used overriding the last assignment slot.
        /// </summary>
        public bool FastLane = false;
        /// <summary>
        /// Indicates how to break ties when assigning fast lane orders.
        /// </summary>
        public FastLaneTieBreaker FastLaneTieBreaker = FastLaneTieBreaker.EarliestDueTime;
    }

    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class RandomOrderBatchingConfiguration : OrderBatchingConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override OrderBatchingMethodType GetMethodType() { return OrderBatchingMethodType.Random; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "obR" + ((Recycle == true) ? "t" : "f"); }
        /// <summary>
        /// Indicates whether stations are recycled, i.e. one station is filled with orders as long as there is capacity left.
        /// </summary>
        public bool Recycle = false;
    }

    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class WorkloadOrderBatchingConfiguration : OrderBatchingConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override OrderBatchingMethodType GetMethodType() { return OrderBatchingMethodType.Workload; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            string name = "obWL";
            switch (OrderingRule)
            {
                case WorkloadOrderingRule.LowestOrderCount: name += "l"; break;
                case WorkloadOrderingRule.HighestOrderCount: name += "h"; break;
                default: throw new ArgumentException("Unexpected argument!");
            }
            return name;
        }
        /// <summary>
        /// Indicates which stations will be preferred for assigning orders to them.
        /// </summary>
        public WorkloadOrderingRule OrderingRule = WorkloadOrderingRule.LowestOrderCount;
    }

    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class RelatedOrderBatchingConfiguration : OrderBatchingConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override OrderBatchingMethodType GetMethodType() { return OrderBatchingMethodType.Related; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            string name = "obRL";
            switch (TieBreaker)
            {
                case RelatedOrderBatchingTieBreaker.Random: name += "r"; break;
                case RelatedOrderBatchingTieBreaker.LeastBusy: name += "l"; break;
                case RelatedOrderBatchingTieBreaker.MostBusy: name += "m"; break;
                default: throw new ArgumentException("Unexpected argument!");
            }
            return name;
        }
        /// <summary>
        /// The tie breaker to use when there are multiple stations with same number of order lines in common.
        /// </summary>
        public RelatedOrderBatchingTieBreaker TieBreaker = RelatedOrderBatchingTieBreaker.LeastBusy;
    }

    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class NearBestPodOrderBatchingConfiguration : OrderBatchingConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override OrderBatchingMethodType GetMethodType() { return OrderBatchingMethodType.NearBestPod; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            string name = "obNBP";
            switch (DistanceRule)
            {
                case NearBestPodOrderBatchingDistanceRule.Euclid: name += "e"; break;
                case NearBestPodOrderBatchingDistanceRule.Manhattan: name += "m"; break;
                case NearBestPodOrderBatchingDistanceRule.ShortestPath: name += "s"; break;
                default: throw new ArgumentException("Unexpected argument!");
            }
            return name;
        }
        /// <summary>
        /// The rule determining which combination of best pod and output station is nearest to each other.
        /// </summary>
        public NearBestPodOrderBatchingDistanceRule DistanceRule = NearBestPodOrderBatchingDistanceRule.ShortestPath;
    }

    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class ForesightOrderBatchingConfiguration : OrderBatchingConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override OrderBatchingMethodType GetMethodType() { return OrderBatchingMethodType.Foresight; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            string name = "obFo";
            switch (ScoreFunctionStationOrder)
            {
                case ScoreFunctionStationOrder.InboundPodsAvailablePicks: name += "P"; break;
                case ScoreFunctionStationOrder.InboundPodsAvailablePicksDepletePod: name += "Pd"; break;
                case ScoreFunctionStationOrder.Deadline: name += "D"; break;
                case ScoreFunctionStationOrder.InboundPodsAvailablePicksNotDepletePod: name += "Ow"; break;
                default:
                    break;
            }
            switch (ScoreFunctionStationOrderSecondLevel)
            {
                case ScoreFunctionStationOrder.InboundPodsAvailablePicks: name += "P"; break;
                case ScoreFunctionStationOrder.InboundPodsAvailablePicksDepletePod: name += "Pd"; break;
                case ScoreFunctionStationOrder.Deadline: name += "D"; break;
                case ScoreFunctionStationOrder.InboundPodsAvailablePicksNotDepletePod: name += "Ow"; break;
                default:
                    break;
            }
            return name;
        }
        /// <summary>
        /// The first score function to determine the best combination of order and station.
        /// </summary>
        public ScoreFunctionStationOrder ScoreFunctionStationOrder = ScoreFunctionStationOrder.InboundPodsAvailablePicks;
        /// <summary>
        /// The tie breaker function to determine the best combination of order and station.
        /// </summary>
        public ScoreFunctionStationOrder ScoreFunctionStationOrderSecondLevel = ScoreFunctionStationOrder.Deadline;
    }

    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class PodMatchingOrderBatchingConfiguration : OrderBatchingConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override OrderBatchingMethodType GetMethodType() { return OrderBatchingMethodType.PodMatching; }
        /// <summary>
        /// Indicates how to break ties when assigning orders.
        /// </summary>
        public OrderSelectionTieBreaker TieBreaker = OrderSelectionTieBreaker.EarliestDueTime;
        /// <summary>
        /// Indicates whether a fast lane slot is used, i.e. one slot of each station is kept free for immediately fulfillable orders.
        /// </summary>
        public bool FastLane = true;
        /// <summary>
        /// Indicates that orders already late will be preferred over a well matching order.
        /// </summary>
        public bool LateBeforeMatch = false;
        /// <summary>
        /// Indicates how to break ties when assigning fast lane orders.
        /// </summary>
        public FastLaneTieBreaker FastLaneTieBreaker = FastLaneTieBreaker.EarliestDueTime;
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            string name = "obPM" + (FastLane ? "y" : "n");
            return name;
        }
    }

    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class LinesInCommonOrderBatchingConfiguration : OrderBatchingConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override OrderBatchingMethodType GetMethodType() { return OrderBatchingMethodType.LinesInCommon; }
        /// <summary>
        /// Indicates how to break ties when assigning orders.
        /// </summary>
        public OrderSelectionTieBreaker TieBreaker = OrderSelectionTieBreaker.EarliestDueTime;
        /// <summary>
        /// Indicates whether a fast lane slot is used, i.e. one slot of each station is kept free for immediately fulfillable orders.
        /// </summary>
        public bool FastLane = false;
        /// <summary>
        /// Indicates how to break ties when assigning fast lane orders.
        /// </summary>
        public FastLaneTieBreaker FastLaneTieBreaker = FastLaneTieBreaker.EarliestDueTime;
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            string name = "obLC" + (FastLane ? "y" : "n");
            return name;
        }
    }

    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class QueueOrderBatchingConfiguration : OrderBatchingConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override OrderBatchingMethodType GetMethodType() { return OrderBatchingMethodType.Queue; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            string name = "obQ";
            name += QueueLength.ToString(IOConstants.FORMATTER);
            return name;
        }
        /// <summary>
        /// The length of the order queue per station.
        /// </summary>
        public int QueueLength = 20;
        /// <summary>
        /// Indicates whether there always is one capacity slot of a station reserved for an immediately fulfillable order.
        /// </summary>
        public bool FastLane = true;

        /// <summary>
        /// Rule settings for selecting an order to be assigned to a station (main rule).
        /// </summary>
        public QueueOrderSelectionRuleConfig StationOrderSelectionRule1 = new QueueOrderSelectionDeadlineVacant();
        /// <summary>
        /// Rule settings for selecting an order to be assigned to a station (first tie breaker).
        /// </summary>
        public QueueOrderSelectionRuleConfig StationOrderSelectionRule2 = new QueueOrderSelectionCompleteable() { OnlyNearestPod = true };
        /// <summary>
        /// Rule settings for selecting an order to be assigned to a station (second tie breaker).
        /// </summary>
        public QueueOrderSelectionRuleConfig StationOrderSelectionRule3 = new QueueOrderSelectionCompleteable() { OnlyNearestPod = false };
        /// <summary>
        /// Rule settings for selecting an order for an open fast lane slot of a station (main rule).
        /// </summary>
        public QueueOrderSelectionRuleConfig FastLaneOrderSelectionRule1 = new QueueOrderSelectionMostLines();
        /// <summary>
        /// Rule settings for selecting an order for an open fast lane slot of a station (first tie breaker).
        /// </summary>
        public QueueOrderSelectionRuleConfig FastLaneOrderSelectionRule2 = new QueueOrderSelectionEarliestDeadline();
        /// <summary>
        /// Rule settings for selecting an order for an open fast lane slot of a station (second tie breaker).
        /// </summary>
        public QueueOrderSelectionRuleConfig FastLaneOrderSelectionRule3 = new QueueOrderSelectionFCFS();
        /// <summary>
        /// Rule settings for selecting an order for the queue of a station (main rule).
        /// </summary>
        public QueueOrderSelectionRuleConfig QueueOrderSelectionRule1 = new QueueOrderSelectionInboundMatches();
        /// <summary>
        /// Rule settings for selecting an order for the queue of a station (first tie breaker).
        /// </summary>
        public QueueOrderSelectionRuleConfig QueueOrderSelectionRule2 = new QueueOrderSelectionRelated();
        /// <summary>
        /// Rule settings for selecting an order for the queue of a station (second tie breaker).
        /// </summary>
        public QueueOrderSelectionRuleConfig QueueOrderSelectionRule3 = new QueueOrderSelectionEarliestDeadline();

        /// <summary>
        /// Distinguishes between different order selection strategies for the queue order manager.
        /// </summary>
        public enum QueueOrderSelectionRuleType
        {
            /// <summary>
            /// A random selection rule.
            /// </summary>
            Random,
            /// <summary>
            /// A first come first served selection rule.
            /// </summary>
            FCFS,
            /// <summary>
            /// Prefers the earliest deadline.
            /// </summary>
            EarliestDeadline,
            /// <summary>
            /// Prefers deadlines that are becoming vacant.
            /// </summary>
            VacantDeadline,
            /// <summary>
            /// Prefers orders with matches along the inbound pods.
            /// </summary>
            InboundMatches,
            /// <summary>
            /// Prefers orders that can be completed quickly.
            /// </summary>
            Completable,
            /// <summary>
            /// Prefers orders with most lines.
            /// </summary>
            Lines,
            /// <summary>
            /// Prefers orders related to already assigned ones.
            /// </summary>
            Related,

        }
        /// <summary>
        /// The base config for all order selection rules implemented by this manager.
        /// </summary>
        [XmlInclude(typeof(QueueOrderSelectionRandom))]
        [XmlInclude(typeof(QueueOrderSelectionFCFS))]
        [XmlInclude(typeof(QueueOrderSelectionEarliestDeadline))]
        [XmlInclude(typeof(QueueOrderSelectionDeadlineVacant))]
        [XmlInclude(typeof(QueueOrderSelectionInboundMatches))]
        [XmlInclude(typeof(QueueOrderSelectionCompleteable))]
        [XmlInclude(typeof(QueueOrderSelectionMostLines))]
        [XmlInclude(typeof(QueueOrderSelectionRelated))]
        public abstract class QueueOrderSelectionRuleConfig
        {
            /// <summary>
            /// Returns the type of this selection rule.
            /// </summary>
            /// <returns>The type of this selection rule.</returns>
            public abstract QueueOrderSelectionRuleType Type();
        }
        /// <summary>
        /// The config for the random selection rule.
        /// </summary>
        public class QueueOrderSelectionRandom : QueueOrderSelectionRuleConfig
        {
            /// <summary>
            /// Returns the type of this selection rule.
            /// </summary>
            /// <returns>The type of this selection rule.</returns>
            public override QueueOrderSelectionRuleType Type() { return QueueOrderSelectionRuleType.Random; }
        }
        /// <summary>
        /// The config for the FCFS selection rule.
        /// </summary>
        public class QueueOrderSelectionFCFS : QueueOrderSelectionRuleConfig
        {
            /// <summary>
            /// Returns the type of this selection rule.
            /// </summary>
            /// <returns>The type of this selection rule.</returns>
            public override QueueOrderSelectionRuleType Type() { return QueueOrderSelectionRuleType.FCFS; }
        }
        /// <summary>
        /// The config for the earliest deadline selection rule.
        /// </summary>
        public class QueueOrderSelectionEarliestDeadline : QueueOrderSelectionRuleConfig
        {
            /// <summary>
            /// Returns the type of this selection rule.
            /// </summary>
            /// <returns>The type of this selection rule.</returns>
            public override QueueOrderSelectionRuleType Type() { return QueueOrderSelectionRuleType.EarliestDeadline; }
        }
        /// <summary>
        /// The config for the deadline closer than X selection rule.
        /// </summary>
        public class QueueOrderSelectionDeadlineVacant : QueueOrderSelectionRuleConfig
        {
            /// <summary>
            /// Returns the type of this selection rule.
            /// </summary>
            /// <returns>The type of this selection rule.</returns>
            public override QueueOrderSelectionRuleType Type() { return QueueOrderSelectionRuleType.VacantDeadline; }
            /// <summary>
            /// All orders with a deadline earlier than the given cutoff will be considered vacant.
            /// </summary>
            public double VacantOrderCutoff = 600;
        }
        /// <summary>
        /// The config for the inbound inventory match count selection rule.
        /// </summary>
        public class QueueOrderSelectionInboundMatches : QueueOrderSelectionRuleConfig
        {
            /// <summary>
            /// Returns the type of this selection rule.
            /// </summary>
            /// <returns>The type of this selection rule.</returns>
            public override QueueOrderSelectionRuleType Type() { return QueueOrderSelectionRuleType.InboundMatches; }
            /// <summary>
            /// The distance within which available material will get a score bonus. Matches behind this distance are considered equally.
            /// </summary>
            public double DistanceForWeighting = 10;
        }
        /// <summary>
        /// The config for the immediate order completability selection rule.
        /// </summary>
        public class QueueOrderSelectionCompleteable : QueueOrderSelectionRuleConfig
        {
            /// <summary>
            /// Returns the type of this selection rule.
            /// </summary>
            /// <returns>The type of this selection rule.</returns>
            public override QueueOrderSelectionRuleType Type() { return QueueOrderSelectionRuleType.Completable; }
            /// <summary>
            /// Indicates whether only the nearest pod is considered for really immediately completable orders.
            /// </summary>
            public bool OnlyNearestPod = true;
        }
        /// <summary>
        /// The config for the most lines selection rule.
        /// </summary>
        public class QueueOrderSelectionMostLines : QueueOrderSelectionRuleConfig
        {
            /// <summary>
            /// Returns the type of this selection rule.
            /// </summary>
            /// <returns>The type of this selection rule.</returns>
            public override QueueOrderSelectionRuleType Type() { return QueueOrderSelectionRuleType.Lines; }
            /// <summary>
            /// Prefers orders with most units overall instead of most lines.
            /// </summary>
            public bool UnitsInsteadOfLines = true;
        }
        /// <summary>
        /// The config for the related to order pool selection rule.
        /// </summary>
        public class QueueOrderSelectionRelated : QueueOrderSelectionRuleConfig
        {
            /// <summary>
            /// Returns the type of this selection rule.
            /// </summary>
            /// <returns>The type of this selection rule.</returns>
            public override QueueOrderSelectionRuleType Type() { return QueueOrderSelectionRuleType.Related; }
        }
    }

    #endregion
}
