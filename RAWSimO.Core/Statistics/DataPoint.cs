using RAWSimO.Core.Control;
using RAWSimO.Core.Elements;
using RAWSimO.Core.IO;
using RAWSimO.Core.Items;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Statistics
{
    #region General performance footprint datapoint

    /// <summary>
    /// Contains all performance criteria of one execution of the simulation.
    /// </summary>
    public class FootprintDatapoint
    {
        /// <summary>
        /// Enumerates all entries of a footprint.
        /// </summary>
        public enum FootPrintEntry
        {
            // --> Setup
            /// <summary>
            /// The name of the instance.
            /// </summary>
            Instance,
            /// <summary>
            /// The name of the setting.
            /// </summary>
            Setting,
            /// <summary>
            /// The name of the controller.
            /// </summary>
            Controller,
            /// <summary>
            /// The tag that was passed to the execution. This field can be used to identify a run, otherwise it contains not any useful information.
            /// </summary>
            Tag,
            /// <summary>
            /// Name of the path planner in use.
            /// </summary>
            PP,
            /// <summary>
            /// Name of the path planner in use.
            /// </summary>
            TA,
            /// <summary>
            /// Name of the station activator in use.
            /// </summary>
            SA,
            /// <summary>
            /// Name of the item storager in use.
            /// </summary>
            IS,
            /// <summary>
            /// Name of the pod storager in use.
            /// </summary>
            PS,
            /// <summary>
            /// Name of the repositioning planner in use.
            /// </summary>
            RP,
            /// <summary>
            /// Name of the order batcher in use.
            /// </summary>
            OB,
            /// <summary>
            /// Name of the replenishment batcher in use.
            /// </summary>
            RB,
            /// <summary>
            /// Name of the method manager in use.
            /// </summary>
            MM,
            /// <summary>
            /// The warmup time in seconds.
            /// </summary>
            Warmup,
            /// <summary>
            /// The simulation duration in seconds.
            /// </summary>
            Duration,
            // --> Meta-data
            /// <summary>
            /// The number of tiers.
            /// </summary>
            NTiers,
            /// <summary>
            /// The number of robots.
            /// </summary>
            NBots,
            /// <summary>
            /// The number of pods.
            /// </summary>
            NPods,
            /// <summary>
            /// The number of input stations.
            /// </summary>
            NIStations,
            /// <summary>
            /// The number of output stations.
            /// </summary>
            NOStations,
            /// <summary>
            /// The number of bots divided by the number of stations.
            /// </summary>
            BotsPerStation,
            /// <summary>
            /// The number of bots divided by the number of input stations.
            /// </summary>
            BotsPerISTation,
            /// <summary>
            /// The number of bots divided by the number of output stations.
            /// </summary>
            BotsPerOStation,
            /// <summary>
            /// The average capacity of an input station.
            /// </summary>
            IStationCapacityAvg,
            /// <summary>
            /// The average capacity of an output station.
            /// </summary>
            OStationCapacityAvg,
            /// <summary>
            /// The number of different SKUs available during simulation.
            /// </summary>
            SKUs,
            // --> Input statistics
            /// <summary>
            /// The number of bundles submitted to the system overall.
            /// </summary>
            BundlesPlaced,
            /// <summary>
            /// The number of orders submitted to the system overall.
            /// </summary>
            OrdersPlaced,
            /// <summary>
            /// The number of bundles rejected by the system.
            /// </summary>
            BundlesRejected,
            /// <summary>
            /// The number of orders rejected by the system.
            /// </summary>
            OrdersRejected,
            /// <summary>
            /// The number of bundles backlogged by the system.
            /// </summary>
            BundlesInBacklogRemaining,
            /// <summary>
            /// The number of orders backlogged by the system.
            /// </summary>
            OrdersInBacklogRemaining,
            /// <summary>
            /// The average number of bundles in backlog.
            /// </summary>
            BundlesInBacklogAvg,
            /// <summary>
            /// The average number of orders in backlog.
            /// </summary>
            OrdersInBacklogAvg,
            // --> Overall performance
            /// <summary>
            /// The number of bundles stored overall.
            /// </summary>
            BundlesHandled,
            /// <summary>
            /// The number of items picked overall.
            /// </summary>
            ItemsHandled,
            /// <summary>
            /// The number of lines picked overall.
            /// </summary>
            LinesHandled,
            /// <summary>
            /// The number of orders fulfilled overall.
            /// </summary>
            OrdersHandled,
            /// <summary>
            /// The number of handled items and bundles overall.
            /// </summary>
            UnitsHandled,
            /// <summary>
            /// The number of collisions that occurred.
            /// </summary>
            Collisions,
            /// <summary>
            /// The number of failed reservations.
            /// </summary>
            FailedReservations,
            /// <summary>
            /// The number of times the path planning search ran into a timeout.
            /// </summary>
            PathPlanningTimeouts,
            /// <summary>
            /// The fractional amount of timeouts across all path planning calls.
            /// </summary>
            PathPlanningTimeoutFractional,
            /// <summary>
            /// The aggregated distance all robots travelled.
            /// </summary>
            DistanceTraveled,
            /// <summary>
            /// The distance traveled in average per bot.
            /// </summary>
            DistanceTraveledPerBot,
            /// <summary>
            /// The aggregated distance estimated by all the robots.
            /// </summary>
            DistanceEstimated,
            /// <summary>
            /// The overall optimal distance by all requested trips.
            /// </summary>
            DistanceRequestedOptimal,
            /// <summary>
            /// The average time a bot was moving.
            /// </summary>
            TimeMoving,
            /// <summary>
            /// The average time a bot was queueing.
            /// </summary>
            TimeQueueing,
            /// <summary>
            /// The average distance traveled per trip (overall distance traveled divided by trip count observed).
            /// </summary>
            TripDistance,
            /// <summary>
            /// The average time spent per trip (sum of measured trip times divided by trip count observed).
            /// </summary>
            TripTime,
            /// <summary>
            /// The average time spent per trip without the queueing time measured (sum of measured trip times minus measured queueing time divided by trip count observed).
            /// </summary>
            TripTimeWithoutQueueing,
            /// <summary>
            /// The overall number of trips observed.
            /// </summary>
            TripCount,
            /// <summary>
            /// The number of times a robot completed a trip to the queueing area of its output station destination.
            /// </summary>
            LastMileTripOStationCount,
            /// <summary>
            /// The average time for completing the last part trip to the queueing area of its output station destination.
            /// </summary>
            LastMileTripOStationTimeAvg,
            /// <summary>
            /// The number of times a robot completed a trip to the queueing area of its input station destination.
            /// </summary>
            LastMileTripIStationCount,
            /// <summary>
            /// The average time for completing the last part trip to the queueing area of its input station destination.
            /// </summary>
            LastMileTripIStationTimeAvg,
            /// <summary>
            /// The number of assigned tasks overall.
            /// </summary>
            OverallAssignedTasks,
            /// <summary>
            /// The number of pods handled at input stations.
            /// </summary>
            OverallPodsHandledAtIStations,
            /// <summary>
            /// The number of pods handled at output stations.
            /// </summary>
            OverallPodsHandledAtOStations,
            /// <summary>
            /// The number of pods handled at input stations as a fractional of all pods handled overall.
            /// </summary>
            PodsHandledAtIStationsFractional,
            /// <summary>
            /// The number of pods handled at output stations as a fractional of all pods handled overall.
            /// </summary>
            PodsHandledAtOStationsFractional,
            /// <summary>
            /// The number of pods handled per input station per hour.
            /// </summary>
            PodsHandledPerIStationPerHour,
            /// <summary>
            /// The number of pods handled per output station per hour.
            /// </summary>
            PodsHandledPerOStationPerHour,
            /// <summary>
            /// The number of repositioning moves that were executed.
            /// </summary>
            RepositioningMoves,
            // --> Station statistics
            /// <summary>
            /// The average number of pods handled per replenishment station.
            /// </summary>
            PodsHandledPerIStationAvg,
            /// <summary>
            /// The variance of the average number of pods handled per replenishment station.
            /// </summary>
            PodsHandledPerIStationVar,
            /// <summary>
            /// The average number of pods handled per pick station.
            /// </summary>
            PodsHandledPerOStationAvg,
            /// <summary>
            /// The variance of the average number of pods handled per pick station.
            /// </summary>
            PodsHandledPerOStationVar,
            // --> Inventory level
            /// <summary>
            /// The inventory level as an average over time.
            /// </summary>
            InventoryLevelAvg,
            /// <summary>
            /// The lower quartile of the average inventory level over time.
            /// </summary>
            InventoryLevelLQ,
            /// <summary>
            /// The upper quartile of the average inventory level over time
            /// </summary>
            InventoryLevelUQ,
            // --> Inventory inversions
            /// <summary>
            /// The number of ranks of all storage locations.
            /// </summary>
            StorageLocationRanks,
            /// <summary>
            /// The total number of inversions (combined score, average across simulation time).
            /// </summary>
            InvCombinedTotal,
            /// <summary>
            /// The aggregated rank difference of all inversions (combined score, average across simulation time).
            /// </summary>
            InvCombinedRank,
            /// <summary>
            /// The average rank difference of all inversions (combined score, average across simulation time).
            /// </summary>
            InvCombinedAvgRank,
            /// <summary>
            /// The total number of inversions (speed score, average across simulation time).
            /// </summary>
            InvSpeedTotal,
            /// <summary>
            /// The aggregated rank difference of all inversions (speed score, average across simulation time).
            /// </summary>
            InvSpeedRank,
            /// <summary>
            /// The average rank difference of all inversions (speed score, average across simulation time).
            /// </summary>
            InvSpeedAvgRank,
            /// <summary>
            /// The total number of inversions (utility score, average across simulation time).
            /// </summary>
            InvUtilityTotal,
            /// <summary>
            /// The aggregated rank difference of all inversions (utility score, average across simulation time).
            /// </summary>
            InvUtilityRank,
            /// <summary>
            /// The average rank difference of all inversions (utility score, average across simulation time).
            /// </summary>
            InvUtilityAvgRank,
            // --> Order process and bundle process info
            /// <summary>
            /// The number of times the generation of bundles was paused during simulation horizon.
            /// </summary>
            BundleGenerationStops,
            /// <summary>
            /// The number of times the generation of orders was paused during simulation horizon.
            /// </summary>
            OrderGenerationStops,
            // --> Hardware consumption
            /// <summary>
            /// The maximal amount of memory used in megabyte (working set).
            /// </summary>
            MemoryUsedMax,
            /// <summary>
            /// The walltime used by the simulation.
            /// </summary>
            RealTimeUsed,
            // --> Rates
            /// <summary>
            /// The number of bundles handled per hour.
            /// </summary>
            BundleThroughputRate,
            /// <summary>
            /// The number of items handled per hour.
            /// </summary>
            ItemThroughputRate,
            /// <summary>
            /// The upper bound on the number of items handled per hour.
            /// </summary>
            ItemThroughputRateUB,
            /// <summary>
            /// The score reached during simulation regarding the item throughput rate (relative value, ITR vs. ITR upper bound).
            /// </summary>
            ItemThroughputRateScore,
            /// <summary>
            /// The number of order lines handled per hour.
            /// </summary>
            LineThroughputRate,
            /// <summary>
            /// The number of orders handled per hour.
            /// </summary>
            OrderThroughputRate,
            // --> Order turnover-time
            /// <summary>
            /// The average turnover time across all orders.
            /// </summary>
            OrderTurnoverTimeAvg,
            /// <summary>
            /// The median turnover time across all orders.
            /// </summary>
            OrderTurnoverTimeMed,
            /// <summary>
            /// The lower quartile turnover time across all orders.
            /// </summary>
            OrderTurnoverTimeLQ,
            /// <summary>
            /// The upper quartile turnover time across all orders.
            /// </summary>
            OrderTurnoverTimeUQ,
            // --> Order throughput-time
            /// <summary>
            /// The average throughput time across all orders.
            /// </summary>
            OrderThroughputTimeAvg,
            /// <summary>
            /// The median throughput time across all orders.
            /// </summary>
            OrderThroughputTimeMed,
            /// <summary>
            /// The lower quartile throughput time across all orders.
            /// </summary>
            OrderThroughputTimeLQ,
            /// <summary>
            /// The upper quartile throughput time across all orders.
            /// </summary>
            OrderThroughputTimeUQ,
            // --> Bundle turnover-time
            /// <summary>
            /// The average turnover time across all bundles.
            /// </summary>
            BundleTurnoverTimeAvg,
            /// <summary>
            /// The median turnover time across all bundles.
            /// </summary>
            BundleTurnoverTimeMed,
            /// <summary>
            /// The lower quartile turnover time across all bundles.
            /// </summary>
            BundleTurnoverTimeLQ,
            /// <summary>
            /// The upper quartile turnover time across all bundles.
            /// </summary>
            BundleTurnoverTimeUQ,
            // --> Bundle throughput-time
            /// <summary>
            /// The average throughput time across all bundles.
            /// </summary>
            BundleThroughputTimeAvg,
            /// <summary>
            /// The median throughput time across all bundles.
            /// </summary>
            BundleThroughputTimeMed,
            /// <summary>
            /// The lower quartile throughput time across all bundles.
            /// </summary>
            BundleThroughputTimeLQ,
            /// <summary>
            /// The upper quartile throughput time across all bundles.
            /// </summary>
            BundleThroughputTimeUQ,
            // --> Order lateness
            /// <summary>
            /// The average lateness across all orders that were not completed in time.
            /// </summary>
            OrderLatenessAvg,
            /// <summary>
            /// The median lateness across all orders that were not completed in time.
            /// </summary>
            OrderLatenessMed,
            /// <summary>
            /// The lower quartile lateness across all orders that were not completed in time.
            /// </summary>
            OrderLatenessLQ,
            /// <summary>
            /// The upper quartile lateness across all orders that were not completed in time.
            /// </summary>
            OrderLatenessUQ,
            // --> Order offset
            /// <summary>
            /// The average offset across all orders that were not completed in time.
            /// </summary>
            OrderOffsetAvg,
            /// <summary>
            /// The median offset across all orders that were not completed in time.
            /// </summary>
            OrderOffsetMed,
            /// <summary>
            /// The lower quartile offset across all orders that were not completed in time.
            /// </summary>
            OrderOffsetLQ,
            /// <summary>
            /// The upper quartile offset across all orders that were not completed in time.
            /// </summary>
            OrderOffsetUQ,
            // --> Lateness counts
            /// <summary>
            /// The number of orders that were completed too late.
            /// </summary>
            LateOrdersCount,
            /// <summary>
            /// The number of orders that were completed in time.
            /// </summary>
            OnTimeOrdersCount,
            /// <summary>
            /// The fractional amount of orders that were completed too late.
            /// </summary>
            LateOrdersFractional,
            /// <summary>
            /// The number of orders that were completed too late per hour.
            /// </summary>
            LateOrdersRate,
            /// <summary>
            /// The number of orders that were completed in time per hour.
            /// </summary>
            OnTimeOrdersRate,
            // --> Item pile-on
            /// <summary>
            /// The average item pile-on across all output stations.
            /// </summary>
            ItemPileOneAvg,
            /// <summary>
            /// The median item pile-on across all output stations.
            /// </summary>
            ItemPileOneMed,
            /// <summary>
            /// The lower quartile item pile-on across all output stations.
            /// </summary>
            ItemPileOneLQ,
            /// <summary>
            /// The upper quartile item pile-on across all output stations.
            /// </summary>
            ItemPileOneUQ,
            // --> Injected item pile-on
            /// <summary>
            /// The average injected item pile-on across all output stations, i.e. the number of items picked per pod that were injected into the robots task in average.
            /// </summary>
            InjectedItemPileOneAvg,
            /// <summary>
            /// The median injected item pile-on across all output stations, i.e. the number of items picked per pod that were injected into the robots task in average.
            /// </summary>
            InjectedItemPileOneMed,
            /// <summary>
            /// The lower quartile injected item pile-on across all output stations, i.e. the number of items picked per pod that were injected into the robots task in average.
            /// </summary>
            InjectedItemPileOneLQ,
            /// <summary>
            /// The upper quartile injected item pile-on across all output stations, i.e. the number of items picked per pod that were injected into the robots task in average.
            /// </summary>
            InjectedItemPileOneUQ,
            // --> Order pile-on
            /// <summary>
            /// The average order pile-on across all output stations.
            /// </summary>
            OrderPileOneAvg,
            /// <summary>
            /// The median order pile-on across all output stations.
            /// </summary>
            OrderPileOneMed,
            /// <summary>
            /// The lower quartile order pile-on across all output stations.
            /// </summary>
            OrderPileOneLQ,
            /// <summary>
            /// The upper quartile order pile-on across all output stations.
            /// </summary>
            OrderPileOneUQ,
            // --> Bundle pile-on
            /// <summary>
            /// The average bundle pile-on across all input stations.
            /// </summary>
            BundlePileOneAvg,
            /// <summary>
            /// The median bundle pile-on across all input stations.
            /// </summary>
            BundlePileOneMed,
            /// <summary>
            /// The lower quartile bundle pile-on across all input stations.
            /// </summary>
            BundlePileOneLQ,
            /// <summary>
            /// The uper quartil bundle pile-on across all input stations.
            /// </summary>
            BundlePileOneUQ,
            // --> Injected bundle pile-on
            /// <summary>
            /// The average injected bundle pile-on across all input stations, i.e. the number of bundles stored per pod that were injected into the robots task in average.
            /// </summary>
            InjectedBundlePileOneAvg,
            /// <summary>
            /// The median injected bundle pile-on across all input stations, i.e. the number of bundles stored per pod that were injected into the robots task in average.
            /// </summary>
            InjectedBundlePileOneMed,
            /// <summary>
            /// The lower quartile injected bundle pile-on across all input stations, i.e. the number of bundles stored per pod that were injected into the robots task in average.
            /// </summary>
            InjectedBundlePileOneLQ,
            /// <summary>
            /// The uper quartil injected bundle pile-on across all input stations, i.e. the number of bundles stored per pod that were injected into the robots task in average.
            /// </summary>
            InjectedBundlePileOneUQ,
            // --> Idle-time
            /// <summary>
            /// The average idle time across all output stations.
            /// </summary>
            OSIdleTimeAvg,
            /// <summary>
            /// The median idle time across all output stations.
            /// </summary>
            OSIdleTimeMed,
            /// <summary>
            /// The lower quartile idle time across all output stations.
            /// </summary>
            OSIdleTimeLQ,
            /// <summary>
            /// The upper quartile idle time across all output stations.
            /// </summary>
            OSIdleTimeUQ,
            /// <summary>
            /// The average idle time across all input stations.
            /// </summary>
            ISIdleTimeAvg,
            /// <summary>
            /// The median idle time across all input stations.
            /// </summary>
            ISIdleTimeMed,
            /// <summary>
            /// The lower quartile idle time across all input stations.
            /// </summary>
            ISIdleTimeLQ,
            /// <summary>
            /// The upper quartile idle time across all input stations.
            /// </summary>
            ISIdleTimeUQ,
            // --> Up-time
            /// <summary>
            /// The average idle time across all output stations.
            /// </summary>
            OSUpTimeAvg,
            /// <summary>
            /// The median up-time across all output stations.
            /// </summary>
            OSUpTimeMed,
            /// <summary>
            /// The lower quartile up-time across all output stations.
            /// </summary>
            OSUpTimeLQ,
            /// <summary>
            /// The upper quartile up-time across all output stations.
            /// </summary>
            OSUpTimeUQ,
            /// <summary>
            /// The average up-time across all input stations.
            /// </summary>
            ISUpTimeAvg,
            /// <summary>
            /// The median up-time across all input stations.
            /// </summary>
            ISUpTimeMed,
            /// <summary>
            /// The lower quartile up-time across all input stations.
            /// </summary>
            ISUpTimeLQ,
            /// <summary>
            /// The upper quartile up-time across all input stations.
            /// </summary>
            ISUpTimeUQ,
            // --> Down-time
            /// <summary>
            /// The average down time across all output stations.
            /// </summary>
            OSDownTimeAvg,
            /// <summary>
            /// The median down time across all output stations.
            /// </summary>
            OSDownTimeMed,
            /// <summary>
            /// The lower quartile down time across all output stations.
            /// </summary>
            OSDownTimeLQ,
            /// <summary>
            /// The upper quartile down time across all output stations.
            /// </summary>
            OSDownTimeUQ,
            /// <summary>
            /// The average down time across all input stations.
            /// </summary>
            ISDownTimeAvg,
            /// <summary>
            /// The median down time across all input stations.
            /// </summary>
            ISDownTimeMed,
            /// <summary>
            /// The lower quartile down time across all input stations.
            /// </summary>
            ISDownTimeLQ,
            /// <summary>
            /// The upper quartile down time across all input stations.
            /// </summary>
            ISDownTimeUQ,
            // --> Bot task stats
            /// <summary>
            /// The number of times the corresponding task was requested from a bot.
            /// </summary>
            TaskCountNone,
            /// <summary>
            /// The time spent in the corresponding task by all bots.
            /// </summary>
            TaskTimeNone,
            /// <summary>
            /// The number of times the corresponding task was requested from a bot.
            /// </summary>
            TaskCountParkPod,
            /// <summary>
            /// The time spent in the corresponding task by all bots.
            /// </summary>
            TaskTimeParkPod,
            /// <summary>
            /// The number of times the corresponding task was requested from a bot.
            /// </summary>
            TaskCountRepositionPod,
            /// <summary>
            /// The time spent in the corresponding task by all bots.
            /// </summary>
            TaskTimeRepositionPod,
            /// <summary>
            /// The number of times the corresponding task was requested from a bot.
            /// </summary>
            TaskCountInsert,
            /// <summary>
            /// The time spent in the corresponding task by all bots.
            /// </summary>
            TaskTimeInsert,
            /// <summary>
            /// The number of times the corresponding task was requested from a bot.
            /// </summary>
            TaskCountExtract,
            /// <summary>
            /// The time spent in the corresponding task by all bots.
            /// </summary>
            TaskTimeExtract,
            /// <summary>
            /// The number of times the corresponding task was requested from a bot.
            /// </summary>
            TaskCountRest,
            /// <summary>
            /// The time spent in the corresponding task by all bots.
            /// </summary>
            TaskTimeRest,
            // --> Bot aggregate task stats
            /// <summary>
            /// The time spent in any meaningful task averaged across all bots. Meaningful tasks are all but Rest or None.
            /// </summary>
            TaskTimeBusyAvg,
            /// <summary>
            /// The time spent in any meaningful task averaged across all bots and as a fraction of the overall simulation horizon. Meaningful tasks are all but Rest or None.
            /// </summary>
            TaskTimeBusyAvgFractional,
            // --> Bot state stats
            /// <summary>
            /// The number of times a bot entered the corresponding state.
            /// </summary>
            StateCountPickup,
            /// <summary>
            /// The time spent in the corresponding state by all bots.
            /// </summary>
            StateTimePickup,
            /// <summary>
            /// The number of times a bot entered the corresponding state.
            /// </summary>
            StateCountSetdown,
            /// <summary>
            /// The time spent in the corresponding state by all bots.
            /// </summary>
            StateTimeSetdown,
            /// <summary>
            /// The number of times a bot entered the corresponding state.
            /// </summary>
            StateCountGet,
            /// <summary>
            /// The time spent in the corresponding state by all bots.
            /// </summary>
            StateTimeGet,
            /// <summary>
            /// The number of times a bot entered the corresponding state.
            /// </summary>
            StateCountPut,
            /// <summary>
            /// The time spent in the corresponding state by all bots.
            /// </summary>
            StateTimePut,
            /// <summary>
            /// The number of times a bot entered the corresponding state.
            /// </summary>
            StateCountRest,
            /// <summary>
            /// The time spent in the corresponding state by all bots.
            /// </summary>
            StateTimeRest,
            /// <summary>
            /// The number of times a bot entered the corresponding state.
            /// </summary>
            StateCountMove,
            /// <summary>
            /// The time spent in the corresponding state by all bots.
            /// </summary>
            StateTimeMove,
            /// <summary>
            /// The number of times a bot entered the corresponding state.
            /// </summary>
            StateCountEvade,
            /// <summary>
            /// The time spent in the corresponding state by all bots.
            /// </summary>
            StateTimeEvade,
            /// <summary>
            /// The number of times a bot entered the corresponding state.
            /// </summary>
            StateCountElevator,
            /// <summary>
            /// The time spent in the corresponding state by all bots.
            /// </summary>
            StateTimeElevator,
            // --> Performance measurement
            /// <summary>
            /// The overall timing spent for deciding / planning.
            /// </summary>
            TimingDecisionsOverall,
            /// <summary>
            /// The average time per path planning call.
            /// </summary>
            TimingPathPlanningAvg,
            /// <summary>
            /// The overall time for path planning.
            /// </summary>
            TimingPathPlanningOverall,
            /// <summary>
            /// The number of path planning calls.
            /// </summary>
            TimingPathPlanningCount,
            /// <summary>
            /// The average time per task allocation call.
            /// </summary>
            TimingTaskAllocationAvg,
            /// <summary>
            /// The overall time for task allocation.
            /// </summary>
            TimingTaskAllocationOverall,
            /// <summary>
            /// The number of task allocation calls.
            /// </summary>
            TimingTaskAllocationCount,
            /// <summary>
            /// The average time per item storage call.
            /// </summary>
            TimingItemStorageAvg,
            /// <summary>
            /// The overall time for item storage.
            /// </summary>
            TimingItemStorageOverall,
            /// <summary>
            /// The number of item storage calls.
            /// </summary>
            TimingItemStorageCount,
            /// <summary>
            /// The average time per pod storage call.
            /// </summary>
            TimingPodStorageAvg,
            /// <summary>
            /// The overall time for pod storage.
            /// </summary>
            TimingPodStorageOverall,
            /// <summary>
            /// The number of pod storage calls.
            /// </summary>
            TimingPodStorageCount,
            /// <summary>
            /// The average time per pod storage call.
            /// </summary>
            TimingRepositioningAvg,
            /// <summary>
            /// The overall time for pod storage.
            /// </summary>
            TimingRepositioningOverall,
            /// <summary>
            /// The number of pod storage calls.
            /// </summary>
            TimingRepositioningCount,
            /// <summary>
            /// The average time per replsnishment batching call.
            /// </summary>
            TimingReplenishmentBatchingAvg,
            /// <summary>
            /// The overall time for replenishment batching.
            /// </summary>
            TimingReplenishmentBatchingOverall,
            /// <summary>
            /// The number of replenishment batching calls.
            /// </summary>
            TimingReplenishmentBatchingCount,
            /// <summary>
            /// The average time per order batching call.
            /// </summary>
            TimingOrderBatchingAvg,
            /// <summary>
            /// The overall time for order batching.
            /// </summary>
            TimingOrderBatchingOverall,
            /// <summary>
            /// The number of order batching calls.
            /// </summary>
            TimingOrderBatchingCount,
            // --> Custom controller performance logs
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogPPString,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogPP1,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogPP2,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogPP3,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogPP4,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogTAString,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogTA1,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogTA2,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogTA3,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogTA4,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogPCString,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogPC1,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogPC2,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogPC3,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogPC4,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogSAString,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogSA1,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogSA2,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogSA3,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogSA4,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogISString,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogIS1,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogIS2,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogIS3,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogIS4,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogPSString,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogPS1,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogPS2,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogPS3,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogPS4,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogRPString,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogRP1,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogRP2,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogRP3,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogRP4,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogOBString,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogOB1,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogOB2,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogOB3,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogOB4,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogRBString,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogRB1,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogRB2,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogRB3,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogRB4,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogMMString,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogMM1,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogMM2,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogMM3,
            /// <summary>
            /// A custom performance info by the respective controller.
            /// </summary>
            CustomLogMM4,
            // --> Comment tags
            /// <summary>
            /// A tag that is set by the setting config.
            /// </summary>
            TagSetting1,
            /// <summary>
            /// A tag that is set by the setting config.
            /// </summary>
            TagSetting2,
            /// <summary>
            /// A tag that is set by the setting config.
            /// </summary>
            TagSetting3,
            /// <summary>
            /// A tag that is set by the control config.
            /// </summary>
            TagControl1,
            /// <summary>
            /// A tag that is set by the control config.
            /// </summary>
            TagControl2,
            /// <summary>
            /// A tag that is set by the control config.
            /// </summary>
            TagControl3,
        }
        /// <summary>
        /// The order of the entries.
        /// </summary>
        private static List<FootPrintEntry> _entryOrderStorage;
        /// <summary>
        /// The order of the entries.
        /// </summary>
        private static IList<FootPrintEntry> EntryOrder
        {
            get
            {
                if (_entryOrderStorage == null) // Get entry order by reflection (use order of enum)
                    _entryOrderStorage = Enum.GetValues(typeof(FootPrintEntry)).Cast<FootPrintEntry>().ToList();
                return _entryOrderStorage;
            }
        }
        /// <summary>
        /// Indicates the order in which the footprint shall be read / written.
        /// </summary>
        private Dictionary<FootPrintEntry, Type> _entryTypes = new Dictionary<FootPrintEntry, Type>()
        {
            // --> Setup
            { FootPrintEntry.Instance, typeof(string) },
            { FootPrintEntry.Setting, typeof(string) },
            { FootPrintEntry.Controller, typeof(string) },
            { FootPrintEntry.Tag, typeof(string) },
            { FootPrintEntry.PP, typeof(string) },
            { FootPrintEntry.TA, typeof(string) },
            { FootPrintEntry.SA, typeof(string) },
            { FootPrintEntry.IS, typeof(string) },
            { FootPrintEntry.PS, typeof(string) },
            { FootPrintEntry.RP, typeof(string) },
            { FootPrintEntry.OB, typeof(string) },
            { FootPrintEntry.RB, typeof(string) },
            { FootPrintEntry.MM, typeof(string) },
            { FootPrintEntry.Warmup, typeof(double) },
            { FootPrintEntry.Duration, typeof(double) },
            // --> Meta-data
            { FootPrintEntry.NTiers, typeof(int) },
            { FootPrintEntry.NBots, typeof(int) },
            { FootPrintEntry.NPods, typeof(int) },
            { FootPrintEntry.NIStations, typeof(int) },
            { FootPrintEntry.NOStations, typeof(int) },
            { FootPrintEntry.BotsPerStation, typeof(double) },
            { FootPrintEntry.BotsPerISTation, typeof(double) },
            { FootPrintEntry.BotsPerOStation, typeof(double) },
            { FootPrintEntry.IStationCapacityAvg, typeof(double) },
            { FootPrintEntry.OStationCapacityAvg, typeof(double) },
            { FootPrintEntry.SKUs, typeof(int) },
            // --> Input statistics
            { FootPrintEntry.BundlesPlaced, typeof(int) },
            { FootPrintEntry.OrdersPlaced, typeof(int) },
            { FootPrintEntry.BundlesRejected, typeof(int) },
            { FootPrintEntry.OrdersRejected, typeof(int) },
            { FootPrintEntry.BundlesInBacklogRemaining, typeof(int) },
            { FootPrintEntry.OrdersInBacklogRemaining, typeof(int) },
            { FootPrintEntry.BundlesInBacklogAvg, typeof(double) },
            { FootPrintEntry.OrdersInBacklogAvg, typeof(double) },
            // --> Overall performance
            { FootPrintEntry.BundlesHandled, typeof(int) },
            { FootPrintEntry.ItemsHandled, typeof(int) },
            { FootPrintEntry.LinesHandled, typeof(int) },
            { FootPrintEntry.OrdersHandled, typeof(int) },
            { FootPrintEntry.UnitsHandled, typeof(int) },
            { FootPrintEntry.Collisions, typeof(int) },
            { FootPrintEntry.FailedReservations, typeof(int) },
            { FootPrintEntry.PathPlanningTimeouts, typeof(int) },
            { FootPrintEntry.PathPlanningTimeoutFractional, typeof(double) },
            { FootPrintEntry.DistanceTraveled, typeof(double) },
            { FootPrintEntry.DistanceTraveledPerBot, typeof(double) },
            { FootPrintEntry.DistanceEstimated, typeof(double) },
            { FootPrintEntry.DistanceRequestedOptimal, typeof(double) },
            { FootPrintEntry.TimeMoving, typeof(double) },
            { FootPrintEntry.TimeQueueing, typeof(double) },
            { FootPrintEntry.TripDistance, typeof(double) },
            { FootPrintEntry.TripTime, typeof(double) },
            { FootPrintEntry.TripTimeWithoutQueueing, typeof(double) },
            { FootPrintEntry.TripCount, typeof(int) },
            { FootPrintEntry.LastMileTripOStationCount, typeof(int) },
            { FootPrintEntry.LastMileTripOStationTimeAvg, typeof(double) },
            { FootPrintEntry.LastMileTripIStationCount, typeof(int) },
            { FootPrintEntry.LastMileTripIStationTimeAvg, typeof(double) },
            { FootPrintEntry.OverallAssignedTasks, typeof(int) },
            { FootPrintEntry.OverallPodsHandledAtIStations, typeof(int) },
            { FootPrintEntry.OverallPodsHandledAtOStations, typeof(int) },
            { FootPrintEntry.PodsHandledAtIStationsFractional, typeof(double) },
            { FootPrintEntry.PodsHandledAtOStationsFractional, typeof(double) },
            { FootPrintEntry.PodsHandledPerIStationPerHour, typeof(double) },
            { FootPrintEntry.PodsHandledPerOStationPerHour, typeof(double) },
            { FootPrintEntry.RepositioningMoves, typeof(int) },
            // --> Station statistics
            { FootPrintEntry.PodsHandledPerIStationAvg, typeof(double) },
            { FootPrintEntry.PodsHandledPerIStationVar, typeof(double) },
            { FootPrintEntry.PodsHandledPerOStationAvg, typeof(double) },
            { FootPrintEntry.PodsHandledPerOStationVar, typeof(double) },
            // --> Inventory level
            { FootPrintEntry.InventoryLevelAvg, typeof(double) },
            { FootPrintEntry.InventoryLevelLQ, typeof(double) },
            { FootPrintEntry.InventoryLevelUQ, typeof(double) },
            // --> Inventory inversions
            { FootPrintEntry.StorageLocationRanks, typeof(int) },
            { FootPrintEntry.InvCombinedTotal, typeof(double) },
            { FootPrintEntry.InvCombinedRank, typeof(double) },
            { FootPrintEntry.InvCombinedAvgRank, typeof(double) },
            { FootPrintEntry.InvSpeedTotal, typeof(double) },
            { FootPrintEntry.InvSpeedRank, typeof(double) },
            { FootPrintEntry.InvSpeedAvgRank, typeof(double) },
            { FootPrintEntry.InvUtilityTotal, typeof(double) },
            { FootPrintEntry.InvUtilityRank, typeof(double) },
            { FootPrintEntry.InvUtilityAvgRank, typeof(double) },
            // --> Bundle and order process info
            { FootPrintEntry.BundleGenerationStops, typeof(int) },
            { FootPrintEntry.OrderGenerationStops, typeof(int) },
            // --> Hardware consumption
            { FootPrintEntry.MemoryUsedMax, typeof(double) },
            { FootPrintEntry.RealTimeUsed, typeof(double) },
            // --> Rates
            { FootPrintEntry.BundleThroughputRate, typeof(double) },
            { FootPrintEntry.ItemThroughputRate, typeof(double) },
            { FootPrintEntry.ItemThroughputRateUB, typeof(double) },
            { FootPrintEntry.ItemThroughputRateScore, typeof(double) },
            { FootPrintEntry.LineThroughputRate, typeof(double) },
            { FootPrintEntry.OrderThroughputRate, typeof(double) },
            // --> Order turnover time
            { FootPrintEntry.OrderTurnoverTimeAvg, typeof(double) },
            { FootPrintEntry.OrderTurnoverTimeMed, typeof(double) },
            { FootPrintEntry.OrderTurnoverTimeLQ, typeof(double) },
            { FootPrintEntry.OrderTurnoverTimeUQ, typeof(double) },
            // --> Order throughput time
            { FootPrintEntry.OrderThroughputTimeAvg, typeof(double) },
            { FootPrintEntry.OrderThroughputTimeMed, typeof(double) },
            { FootPrintEntry.OrderThroughputTimeLQ, typeof(double) },
            { FootPrintEntry.OrderThroughputTimeUQ, typeof(double) },
            // --> Bundle turnover time
            { FootPrintEntry.BundleTurnoverTimeAvg, typeof(double) },
            { FootPrintEntry.BundleTurnoverTimeMed, typeof(double) },
            { FootPrintEntry.BundleTurnoverTimeLQ, typeof(double) },
            { FootPrintEntry.BundleTurnoverTimeUQ, typeof(double) },
            // --> Bundle throughput time
            { FootPrintEntry.BundleThroughputTimeAvg, typeof(double) },
            { FootPrintEntry.BundleThroughputTimeMed, typeof(double) },
            { FootPrintEntry.BundleThroughputTimeLQ, typeof(double) },
            { FootPrintEntry.BundleThroughputTimeUQ, typeof(double) },
            // --> Lateness
            { FootPrintEntry.OrderLatenessAvg, typeof(double) },
            { FootPrintEntry.OrderLatenessMed, typeof(double) },
            { FootPrintEntry.OrderLatenessLQ, typeof(double) },
            { FootPrintEntry.OrderLatenessUQ, typeof(double) },
            // --> Offset
            { FootPrintEntry.OrderOffsetAvg, typeof(double) },
            { FootPrintEntry.OrderOffsetMed, typeof(double) },
            { FootPrintEntry.OrderOffsetLQ, typeof(double) },
            { FootPrintEntry.OrderOffsetUQ, typeof(double) },
            // --> Lateness counts
            { FootPrintEntry.LateOrdersCount, typeof(int) },
            { FootPrintEntry.OnTimeOrdersCount, typeof(int) },
            { FootPrintEntry.LateOrdersFractional, typeof(double) },
            { FootPrintEntry.LateOrdersRate, typeof(double) },
            { FootPrintEntry.OnTimeOrdersRate, typeof(double) },
            // --> Item pile-on
            { FootPrintEntry.ItemPileOneAvg, typeof(double) },
            { FootPrintEntry.ItemPileOneMed, typeof(double) },
            { FootPrintEntry.ItemPileOneLQ, typeof(double) },
            { FootPrintEntry.ItemPileOneUQ, typeof(double) },
            // --> Injected item pile-on
            { FootPrintEntry.InjectedItemPileOneAvg, typeof(double) },
            { FootPrintEntry.InjectedItemPileOneMed, typeof(double) },
            { FootPrintEntry.InjectedItemPileOneLQ, typeof(double) },
            { FootPrintEntry.InjectedItemPileOneUQ, typeof(double) },
            // --> Order pile-on
            { FootPrintEntry.OrderPileOneAvg, typeof(double) },
            { FootPrintEntry.OrderPileOneMed, typeof(double) },
            { FootPrintEntry.OrderPileOneLQ, typeof(double) },
            { FootPrintEntry.OrderPileOneUQ, typeof(double) },
            // --> Bundle pile-on
            { FootPrintEntry.BundlePileOneAvg, typeof(double) },
            { FootPrintEntry.BundlePileOneMed, typeof(double) },
            { FootPrintEntry.BundlePileOneLQ, typeof(double) },
            { FootPrintEntry.BundlePileOneUQ, typeof(double) },
            // --> Injected bundle pile-on
            { FootPrintEntry.InjectedBundlePileOneAvg, typeof(double) },
            { FootPrintEntry.InjectedBundlePileOneMed, typeof(double) },
            { FootPrintEntry.InjectedBundlePileOneLQ, typeof(double) },
            { FootPrintEntry.InjectedBundlePileOneUQ, typeof(double) },
            // --> Idle-time
            { FootPrintEntry.OSIdleTimeAvg, typeof(double) },
            { FootPrintEntry.OSIdleTimeMed, typeof(double) },
            { FootPrintEntry.OSIdleTimeLQ, typeof(double) },
            { FootPrintEntry.OSIdleTimeUQ, typeof(double) },
            { FootPrintEntry.ISIdleTimeAvg, typeof(double) },
            { FootPrintEntry.ISIdleTimeMed, typeof(double) },
            { FootPrintEntry.ISIdleTimeLQ, typeof(double) },
            { FootPrintEntry.ISIdleTimeUQ, typeof(double) },
            // --> Up-time
            { FootPrintEntry.OSUpTimeAvg, typeof(double) },
            { FootPrintEntry.OSUpTimeMed, typeof(double) },
            { FootPrintEntry.OSUpTimeLQ, typeof(double) },
            { FootPrintEntry.OSUpTimeUQ, typeof(double) },
            { FootPrintEntry.ISUpTimeAvg, typeof(double) },
            { FootPrintEntry.ISUpTimeMed, typeof(double) },
            { FootPrintEntry.ISUpTimeLQ, typeof(double) },
            { FootPrintEntry.ISUpTimeUQ, typeof(double) },
            // --> Down-time
            { FootPrintEntry.OSDownTimeAvg, typeof(double) },
            { FootPrintEntry.OSDownTimeMed, typeof(double) },
            { FootPrintEntry.OSDownTimeLQ, typeof(double) },
            { FootPrintEntry.OSDownTimeUQ, typeof(double) },
            { FootPrintEntry.ISDownTimeAvg, typeof(double) },
            { FootPrintEntry.ISDownTimeMed, typeof(double) },
            { FootPrintEntry.ISDownTimeLQ, typeof(double) },
            { FootPrintEntry.ISDownTimeUQ, typeof(double) },
            // --> Task stats
            { FootPrintEntry.TaskCountNone, typeof(int) },
            { FootPrintEntry.TaskTimeNone, typeof(double) },
            { FootPrintEntry.TaskCountParkPod, typeof(int) },
            { FootPrintEntry.TaskTimeParkPod, typeof(double) },
            { FootPrintEntry.TaskCountRepositionPod, typeof(int) },
            { FootPrintEntry.TaskTimeRepositionPod, typeof(double) },
            { FootPrintEntry.TaskCountInsert, typeof(int) },
            { FootPrintEntry.TaskTimeInsert, typeof(double) },
            { FootPrintEntry.TaskCountExtract, typeof(int) },
            { FootPrintEntry.TaskTimeExtract, typeof(double) },
            { FootPrintEntry.TaskCountRest, typeof(int) },
            { FootPrintEntry.TaskTimeRest, typeof(double) },
            // --> Aggregate task stats
            { FootPrintEntry.TaskTimeBusyAvg, typeof(double) },
            { FootPrintEntry.TaskTimeBusyAvgFractional, typeof(double) },
            // --> State stats
            { FootPrintEntry.StateCountPickup, typeof(int) },
            { FootPrintEntry.StateTimePickup, typeof(double) },
            { FootPrintEntry.StateCountSetdown, typeof(int) },
            { FootPrintEntry.StateTimeSetdown, typeof(double) },
            { FootPrintEntry.StateCountGet, typeof(int) },
            { FootPrintEntry.StateTimeGet, typeof(double) },
            { FootPrintEntry.StateCountPut, typeof(int) },
            { FootPrintEntry.StateTimePut, typeof(double) },
            { FootPrintEntry.StateCountRest, typeof(int) },
            { FootPrintEntry.StateTimeRest, typeof(double) },
            { FootPrintEntry.StateCountMove, typeof(int) },
            { FootPrintEntry.StateTimeMove, typeof(double) },
            { FootPrintEntry.StateCountEvade, typeof(int) },
            { FootPrintEntry.StateTimeEvade, typeof(double) },
            { FootPrintEntry.StateCountElevator, typeof(int) },
            { FootPrintEntry.StateTimeElevator, typeof(double) },
            // --> Performance measurement
            { FootPrintEntry.TimingDecisionsOverall, typeof(double) },
            { FootPrintEntry.TimingPathPlanningAvg, typeof(double) },
            { FootPrintEntry.TimingPathPlanningOverall, typeof(double) },
            { FootPrintEntry.TimingPathPlanningCount, typeof(int) },
            { FootPrintEntry.TimingTaskAllocationAvg, typeof(double) },
            { FootPrintEntry.TimingTaskAllocationOverall, typeof(double) },
            { FootPrintEntry.TimingTaskAllocationCount, typeof(int) },
            { FootPrintEntry.TimingItemStorageAvg, typeof(double) },
            { FootPrintEntry.TimingItemStorageOverall, typeof(double) },
            { FootPrintEntry.TimingItemStorageCount, typeof(int) },
            { FootPrintEntry.TimingPodStorageAvg, typeof(double) },
            { FootPrintEntry.TimingPodStorageOverall, typeof(double) },
            { FootPrintEntry.TimingPodStorageCount, typeof(int) },
            { FootPrintEntry.TimingRepositioningAvg, typeof(double) },
            { FootPrintEntry.TimingRepositioningOverall, typeof(double) },
            { FootPrintEntry.TimingRepositioningCount, typeof(int) },
            { FootPrintEntry.TimingReplenishmentBatchingAvg, typeof(double) },
            { FootPrintEntry.TimingReplenishmentBatchingOverall, typeof(double) },
            { FootPrintEntry.TimingReplenishmentBatchingCount, typeof(int) },
            { FootPrintEntry.TimingOrderBatchingAvg, typeof(double) },
            { FootPrintEntry.TimingOrderBatchingOverall, typeof(double) },
            { FootPrintEntry.TimingOrderBatchingCount, typeof(int) },
            // --> Custom performance info
            { FootPrintEntry.CustomLogPPString, typeof(string) },
            { FootPrintEntry.CustomLogPP1, typeof(double) },
            { FootPrintEntry.CustomLogPP2, typeof(double) },
            { FootPrintEntry.CustomLogPP3, typeof(double) },
            { FootPrintEntry.CustomLogPP4, typeof(double) },
            { FootPrintEntry.CustomLogTAString, typeof(string) },
            { FootPrintEntry.CustomLogTA1, typeof(double) },
            { FootPrintEntry.CustomLogTA2, typeof(double) },
            { FootPrintEntry.CustomLogTA3, typeof(double) },
            { FootPrintEntry.CustomLogTA4, typeof(double) },
            { FootPrintEntry.CustomLogPCString, typeof(string) },
            { FootPrintEntry.CustomLogPC1, typeof(double) },
            { FootPrintEntry.CustomLogPC2, typeof(double) },
            { FootPrintEntry.CustomLogPC3, typeof(double) },
            { FootPrintEntry.CustomLogPC4, typeof(double) },
            { FootPrintEntry.CustomLogSAString, typeof(string) },
            { FootPrintEntry.CustomLogSA1, typeof(double) },
            { FootPrintEntry.CustomLogSA2, typeof(double) },
            { FootPrintEntry.CustomLogSA3, typeof(double) },
            { FootPrintEntry.CustomLogSA4, typeof(double) },
            { FootPrintEntry.CustomLogISString, typeof(string) },
            { FootPrintEntry.CustomLogIS1, typeof(double) },
            { FootPrintEntry.CustomLogIS2, typeof(double) },
            { FootPrintEntry.CustomLogIS3, typeof(double) },
            { FootPrintEntry.CustomLogIS4, typeof(double) },
            { FootPrintEntry.CustomLogPSString, typeof(string) },
            { FootPrintEntry.CustomLogPS1, typeof(double) },
            { FootPrintEntry.CustomLogPS2, typeof(double) },
            { FootPrintEntry.CustomLogPS3, typeof(double) },
            { FootPrintEntry.CustomLogPS4, typeof(double) },
            { FootPrintEntry.CustomLogRPString, typeof(string) },
            { FootPrintEntry.CustomLogRP1, typeof(double) },
            { FootPrintEntry.CustomLogRP2, typeof(double) },
            { FootPrintEntry.CustomLogRP3, typeof(double) },
            { FootPrintEntry.CustomLogRP4, typeof(double) },
            { FootPrintEntry.CustomLogOBString, typeof(string) },
            { FootPrintEntry.CustomLogOB1, typeof(double) },
            { FootPrintEntry.CustomLogOB2, typeof(double) },
            { FootPrintEntry.CustomLogOB3, typeof(double) },
            { FootPrintEntry.CustomLogOB4, typeof(double) },
            { FootPrintEntry.CustomLogRBString, typeof(string) },
            { FootPrintEntry.CustomLogRB1, typeof(double) },
            { FootPrintEntry.CustomLogRB2, typeof(double) },
            { FootPrintEntry.CustomLogRB3, typeof(double) },
            { FootPrintEntry.CustomLogRB4, typeof(double) },
            { FootPrintEntry.CustomLogMMString, typeof(string) },
            { FootPrintEntry.CustomLogMM1, typeof(double) },
            { FootPrintEntry.CustomLogMM2, typeof(double) },
            { FootPrintEntry.CustomLogMM3, typeof(double) },
            { FootPrintEntry.CustomLogMM4, typeof(double) },
            // --> Comment tags
            { FootPrintEntry.TagSetting1, typeof(string) },
            { FootPrintEntry.TagSetting2, typeof(string) },
            { FootPrintEntry.TagSetting3, typeof(string) },
            { FootPrintEntry.TagControl1, typeof(string) },
            { FootPrintEntry.TagControl2, typeof(string) },
            { FootPrintEntry.TagControl3, typeof(string) },
        };
        /// <summary>
        /// Contains the actual values of the footprint entries.
        /// </summary>
        Dictionary<FootPrintEntry, object> _entryValues = new Dictionary<FootPrintEntry, object>();
        /// <summary>
        /// Retrieves the value of the given entry within this footprint.
        /// </summary>
        /// <param name="entry">The entry to retrieve.</param>
        /// <returns>The value of the entry.</returns>
        public object this[FootPrintEntry entry] { get { return _entryValues[entry]; } }
        /// <summary>
        /// Creates a new datapoint by extracting all necessary information from the instance object that just completed a simulation run.
        /// </summary>
        /// <param name="instance">The instance object that just completed the simulation run.</param>
        public FootprintDatapoint(Instance instance)
        {
            // Setup
            _entryValues[FootPrintEntry.Instance] = instance.Name;
            _entryValues[FootPrintEntry.Setting] = instance.SettingConfig.Name;
            _entryValues[FootPrintEntry.Controller] = instance.ControllerConfig.Name;
            _entryValues[FootPrintEntry.Tag] = instance.Tag;
            _entryValues[FootPrintEntry.PP] = instance.ControllerConfig.PathPlanningConfig.GetMethodName();
            _entryValues[FootPrintEntry.TA] = instance.ControllerConfig.TaskAllocationConfig.GetMethodName();
            _entryValues[FootPrintEntry.SA] = instance.ControllerConfig.StationActivationConfig.GetMethodName();
            _entryValues[FootPrintEntry.IS] = instance.ControllerConfig.ItemStorageConfig.GetMethodName();
            _entryValues[FootPrintEntry.PS] = instance.ControllerConfig.PodStorageConfig.GetMethodName();
            _entryValues[FootPrintEntry.RP] = instance.ControllerConfig.RepositioningConfig.GetMethodName();
            _entryValues[FootPrintEntry.OB] = instance.ControllerConfig.OrderBatchingConfig.GetMethodName();
            _entryValues[FootPrintEntry.RB] = instance.ControllerConfig.ReplenishmentBatchingConfig.GetMethodName();
            _entryValues[FootPrintEntry.MM] = instance.ControllerConfig.MethodManagementConfig.GetMethodName();
            // Meta-data
            _entryValues[FootPrintEntry.Warmup] = instance.SettingConfig.SimulationWarmupTime;
            _entryValues[FootPrintEntry.Duration] = instance.SettingConfig.SimulationDuration;
            _entryValues[FootPrintEntry.NTiers] = instance.Compound.Tiers.Count;
            _entryValues[FootPrintEntry.NBots] = instance.Bots.Count;
            _entryValues[FootPrintEntry.NPods] = instance.Pods.Count;
            _entryValues[FootPrintEntry.NIStations] = instance.InputStations.Count;
            _entryValues[FootPrintEntry.NOStations] = instance.OutputStations.Count;
            _entryValues[FootPrintEntry.BotsPerStation] = (instance.InputStations.Any() || instance.OutputStations.Any()) ? (double)instance.Bots.Count / (instance.InputStations.Count + instance.OutputStations.Count) : -1.0;
            _entryValues[FootPrintEntry.BotsPerISTation] = instance.InputStations.Any() ? (double)instance.Bots.Count / instance.InputStations.Count : -1.0;
            _entryValues[FootPrintEntry.BotsPerOStation] = instance.OutputStations.Any() ? (double)instance.Bots.Count / instance.OutputStations.Count : -1.0;
            _entryValues[FootPrintEntry.IStationCapacityAvg] = instance.InputStations.Any() ? instance.InputStations.Sum(s => s.Capacity) / instance.InputStations.Count : 0.0;
            _entryValues[FootPrintEntry.OStationCapacityAvg] = instance.OutputStations.Any() ? (double)instance.OutputStations.Sum(s => s.Capacity) / instance.OutputStations.Count : 0.0;
            _entryValues[FootPrintEntry.SKUs] = instance.ItemDescriptions.Count;
            // Input statistics
            _entryValues[FootPrintEntry.BundlesPlaced] = instance.StatOverallBundlesPlaced;
            _entryValues[FootPrintEntry.OrdersPlaced] = instance.StatOverallOrdersPlaced;
            _entryValues[FootPrintEntry.BundlesRejected] = instance.StatOverallBundlesRejected;
            _entryValues[FootPrintEntry.OrdersRejected] = instance.StatOverallOrdersRejected;
            _entryValues[FootPrintEntry.BundlesInBacklogRemaining] = instance.ItemManager.BacklogBundleCount;
            _entryValues[FootPrintEntry.OrdersInBacklogRemaining] = instance.ItemManager.BacklogOrderCount;
            _entryValues[FootPrintEntry.BundlesInBacklogAvg] = instance.Observer.BundleOrderSituationLog.Average(d => d.BacklogBundleCount);
            _entryValues[FootPrintEntry.OrdersInBacklogAvg] = instance.Observer.BundleOrderSituationLog.Average(d => d.BacklogOrderCount);
            // Overall performance
            _entryValues[FootPrintEntry.BundlesHandled] = instance.StatOverallBundlesHandled;
            _entryValues[FootPrintEntry.ItemsHandled] = instance.StatOverallItemsHandled;
            _entryValues[FootPrintEntry.LinesHandled] = instance.StatOverallLinesHandled;
            _entryValues[FootPrintEntry.OrdersHandled] = instance.StatOverallOrdersHandled;
            _entryValues[FootPrintEntry.UnitsHandled] = instance.StatOverallItemsHandled + instance.StatOverallBundlesHandled;
            _entryValues[FootPrintEntry.Collisions] = instance.StatOverallCollisions;
            _entryValues[FootPrintEntry.FailedReservations] = instance.StatOverallFailedReservations;
            _entryValues[FootPrintEntry.PathPlanningTimeouts] = instance.StatOverallPathPlanningTimeouts;
            _entryValues[FootPrintEntry.PathPlanningTimeoutFractional] = (double)instance.StatOverallPathPlanningTimeouts / instance.Observer.TimingPathPlanningDecisionCount;
            _entryValues[FootPrintEntry.DistanceTraveled] = instance.StatOverallDistanceTraveled;
            _entryValues[FootPrintEntry.DistanceTraveledPerBot] = instance.StatOverallDistanceTraveled / instance.Bots.Count;
            _entryValues[FootPrintEntry.DistanceEstimated] = instance.StatOverallDistanceEstimated;
            _entryValues[FootPrintEntry.DistanceRequestedOptimal] = instance.Bots.Sum(b => b.StatDistanceRequestedOptimal);
            _entryValues[FootPrintEntry.TimeMoving] = instance.Bots.Average(b => b.StatTotalTimeMoving);
            _entryValues[FootPrintEntry.TimeQueueing] = instance.Bots.Average(b => b.StatTotalTimeQueueing);
            _entryValues[FootPrintEntry.TripDistance] = instance.StatOverallDistanceTraveled / instance.Waypoints.Sum(w => w.StatOutgoingTrips);
            _entryValues[FootPrintEntry.TripTime] = instance.Waypoints.Sum(w => w.StatOutgoingTripTime) / instance.Waypoints.Sum(w => w.StatOutgoingTrips);
            _entryValues[FootPrintEntry.TripTimeWithoutQueueing] = (instance.Waypoints.Sum(w => w.StatOutgoingTripTime) - instance.Bots.Sum(b => b.StatTotalTimeQueueing)) / instance.Waypoints.Sum(w => w.StatOutgoingTrips);
            _entryValues[FootPrintEntry.TripCount] = instance.Waypoints.Sum(w => w.StatOutgoingTrips);
            _entryValues[FootPrintEntry.LastMileTripOStationCount] = instance.OStationTripCount;
            _entryValues[FootPrintEntry.LastMileTripOStationTimeAvg] = instance.OStationTripTimeAvg;
            _entryValues[FootPrintEntry.LastMileTripIStationCount] = instance.IStationTripCount;
            _entryValues[FootPrintEntry.LastMileTripIStationTimeAvg] = instance.IStationTripTimeAvg;
            _entryValues[FootPrintEntry.OverallAssignedTasks] = instance.StatOverallAssignedTasks;
            _entryValues[FootPrintEntry.OverallPodsHandledAtIStations] = instance.InputStations.Count == 0 ? 0 : instance.InputStations.Sum(s => s.StatPodsHandled);
            _entryValues[FootPrintEntry.OverallPodsHandledAtOStations] = instance.OutputStations.Count == 0 ? 0 : instance.OutputStations.Sum(s => s.StatPodsHandled);
            int overallPodsHandled = ((int)_entryValues[FootPrintEntry.OverallPodsHandledAtIStations] + (int)_entryValues[FootPrintEntry.OverallPodsHandledAtOStations]);
            _entryValues[FootPrintEntry.PodsHandledAtIStationsFractional] = overallPodsHandled == 0 ? 0.0 : (int)_entryValues[FootPrintEntry.OverallPodsHandledAtIStations] / (double)overallPodsHandled;
            _entryValues[FootPrintEntry.PodsHandledAtOStationsFractional] = overallPodsHandled == 0 ? 0.0 : (int)_entryValues[FootPrintEntry.OverallPodsHandledAtOStations] / (double)overallPodsHandled;
            _entryValues[FootPrintEntry.PodsHandledPerIStationPerHour] = instance.InputStations.Count == 0 ? 0 : instance.InputStations.Average(s => s.StatPodsHandled) / TimeSpan.FromSeconds(instance.SettingConfig.SimulationDuration).TotalHours;
            _entryValues[FootPrintEntry.PodsHandledPerOStationPerHour] = instance.OutputStations.Count == 0 ? 0 : instance.OutputStations.Average(s => s.StatPodsHandled) / TimeSpan.FromSeconds(instance.SettingConfig.SimulationDuration).TotalHours;
            _entryValues[FootPrintEntry.RepositioningMoves] = instance.StatRepositioningMoves;
            // Station statistics
            _entryValues[FootPrintEntry.PodsHandledPerIStationAvg] = instance.InputStations.Count == 0 ? 0 : instance.InputStations.Average(s => s.StatPodsHandled);
            _entryValues[FootPrintEntry.PodsHandledPerIStationVar] = instance.InputStations.Variance(s => s.StatPodsHandled);
            _entryValues[FootPrintEntry.PodsHandledPerOStationAvg] = instance.OutputStations.Count == 0 ? 0 : instance.InputStations.Average(s => s.StatPodsHandled);
            _entryValues[FootPrintEntry.PodsHandledPerOStationVar] = instance.OutputStations.Variance(s => s.StatPodsHandled); ;
            // Inventory level
            _entryValues[FootPrintEntry.InventoryLevelAvg] = instance.Observer.InventoryLevelLog.Count() == 0 ? 0 : instance.Observer.InventoryLevelLog.Average(d => d.InventoryLevel);
            _entryValues[FootPrintEntry.InventoryLevelLQ] = instance.Observer.InventoryLevelLog.Count() == 0 ? 0 : StatisticsHelper.GetLowerQuartile(instance.Observer.InventoryLevelLog.Select(d => d.InventoryLevel));
            _entryValues[FootPrintEntry.InventoryLevelUQ] = instance.Observer.InventoryLevelLog.Count() == 0 ? 0 : StatisticsHelper.GetUpperQuartile(instance.Observer.InventoryLevelLog.Select(d => d.InventoryLevel));
            // Inventory inversions
            _entryValues[FootPrintEntry.StorageLocationRanks] = instance.ElementMetaInfoTracker.StorageLocationRankMax;
            _entryValues[FootPrintEntry.InvCombinedTotal] = instance.Observer.InventoryLevelLog.Count() == 0 ? 0 : instance.Observer.InventoryLevelLog.Average(d => d.InvCombinedTotal);
            _entryValues[FootPrintEntry.InvCombinedRank] = instance.Observer.InventoryLevelLog.Count() == 0 ? 0 : instance.Observer.InventoryLevelLog.Average(d => d.InvCombinedRank);
            _entryValues[FootPrintEntry.InvCombinedAvgRank] = instance.Observer.InventoryLevelLog.Count() == 0 ? 0 : instance.Observer.InventoryLevelLog.Average(d => d.InvCombinedAvgRank);
            _entryValues[FootPrintEntry.InvSpeedTotal] = instance.Observer.InventoryLevelLog.Count() == 0 ? 0 : instance.Observer.InventoryLevelLog.Average(d => d.InvSpeedTotal);
            _entryValues[FootPrintEntry.InvSpeedRank] = instance.Observer.InventoryLevelLog.Count() == 0 ? 0 : instance.Observer.InventoryLevelLog.Average(d => d.InvSpeedRank);
            _entryValues[FootPrintEntry.InvSpeedAvgRank] = instance.Observer.InventoryLevelLog.Count() == 0 ? 0 : instance.Observer.InventoryLevelLog.Average(d => d.InvSpeedAvgRank);
            _entryValues[FootPrintEntry.InvUtilityTotal] = instance.Observer.InventoryLevelLog.Count() == 0 ? 0 : instance.Observer.InventoryLevelLog.Average(d => d.InvUtilityTotal);
            _entryValues[FootPrintEntry.InvUtilityRank] = instance.Observer.InventoryLevelLog.Count() == 0 ? 0 : instance.Observer.InventoryLevelLog.Average(d => d.InvUtilityRank);
            _entryValues[FootPrintEntry.InvUtilityAvgRank] = instance.Observer.InventoryLevelLog.Count() == 0 ? 0 : instance.Observer.InventoryLevelLog.Average(d => d.InvUtilityAvgRank);
            // Bundle / order generation info
            _entryValues[FootPrintEntry.BundleGenerationStops] = instance.StatBundleGenerationStops;
            _entryValues[FootPrintEntry.OrderGenerationStops] = instance.StatOrderGenerationStops;
            // Hardware consumption
            _entryValues[FootPrintEntry.MemoryUsedMax] = instance.StatMaxMemoryUsed;
            _entryValues[FootPrintEntry.RealTimeUsed] = (instance.SettingConfig.StartTime != default(DateTime) && instance.SettingConfig.StopTime != default(DateTime)) ? (instance.SettingConfig.StopTime - instance.SettingConfig.StartTime).TotalSeconds : 0;
            // Rates
            _entryValues[FootPrintEntry.BundleThroughputRate] = instance.StatOverallBundlesHandled / TimeSpan.FromSeconds(instance.SettingConfig.SimulationDuration).TotalHours;
            _entryValues[FootPrintEntry.ItemThroughputRate] = instance.StatOverallItemsHandled / TimeSpan.FromSeconds(instance.SettingConfig.SimulationDuration).TotalHours;
            _entryValues[FootPrintEntry.ItemThroughputRateUB] = instance.OutputStations.Any() ? UpperBoundHelper.CalcUBItemThroughputRate(instance, instance.OutputStations.Where(s => s.StatNumItemsPicked > 0).Average(s => s.StatItemPileOn)) : 0;
            _entryValues[FootPrintEntry.ItemThroughputRateScore] = (double)_entryValues[FootPrintEntry.ItemThroughputRate] / (double)_entryValues[FootPrintEntry.ItemThroughputRateUB];
            _entryValues[FootPrintEntry.LineThroughputRate] = instance.StatOverallLinesHandled / TimeSpan.FromSeconds(instance.SettingConfig.SimulationDuration).TotalHours;
            _entryValues[FootPrintEntry.OrderThroughputRate] = instance.StatOverallOrdersHandled / TimeSpan.FromSeconds(instance.SettingConfig.SimulationDuration).TotalHours;
            // Order turnover time
            _entryValues[FootPrintEntry.OrderTurnoverTimeAvg] = (instance._statOrderTurnoverTimes.Count == 0) ? 0 : instance._statOrderTurnoverTimes.Average();
            _entryValues[FootPrintEntry.OrderTurnoverTimeMed] = (instance._statOrderTurnoverTimes.Count == 0) ? 0 : StatisticsHelper.GetMedian(instance._statOrderTurnoverTimes);
            _entryValues[FootPrintEntry.OrderTurnoverTimeLQ] = (instance._statOrderTurnoverTimes.Count == 0) ? 0 : StatisticsHelper.GetLowerQuartile(instance._statOrderTurnoverTimes);
            _entryValues[FootPrintEntry.OrderTurnoverTimeUQ] = (instance._statOrderTurnoverTimes.Count == 0) ? 0 : StatisticsHelper.GetUpperQuartile(instance._statOrderTurnoverTimes);
            // Order throughput time
            _entryValues[FootPrintEntry.OrderThroughputTimeAvg] = (instance._statOrderThroughputTimes.Count == 0) ? 0 : instance._statOrderThroughputTimes.Average();
            _entryValues[FootPrintEntry.OrderThroughputTimeMed] = (instance._statOrderThroughputTimes.Count == 0) ? 0 : StatisticsHelper.GetMedian(instance._statOrderThroughputTimes);
            _entryValues[FootPrintEntry.OrderThroughputTimeLQ] = (instance._statOrderThroughputTimes.Count == 0) ? 0 : StatisticsHelper.GetLowerQuartile(instance._statOrderThroughputTimes);
            _entryValues[FootPrintEntry.OrderThroughputTimeUQ] = (instance._statOrderThroughputTimes.Count == 0) ? 0 : StatisticsHelper.GetUpperQuartile(instance._statOrderThroughputTimes);
            // Bundle turnover time
            _entryValues[FootPrintEntry.BundleTurnoverTimeAvg] = (instance._statBundleTurnoverTimes.Count == 0) ? 0 : instance._statBundleTurnoverTimes.Average();
            _entryValues[FootPrintEntry.BundleTurnoverTimeMed] = (instance._statBundleTurnoverTimes.Count == 0) ? 0 : StatisticsHelper.GetMedian(instance._statBundleTurnoverTimes);
            _entryValues[FootPrintEntry.BundleTurnoverTimeLQ] = (instance._statBundleTurnoverTimes.Count == 0) ? 0 : StatisticsHelper.GetLowerQuartile(instance._statBundleTurnoverTimes);
            _entryValues[FootPrintEntry.BundleTurnoverTimeUQ] = (instance._statBundleTurnoverTimes.Count == 0) ? 0 : StatisticsHelper.GetUpperQuartile(instance._statBundleTurnoverTimes);
            // Bundle throughput time
            _entryValues[FootPrintEntry.BundleThroughputTimeAvg] = (instance._statBundleThroughputTimes.Count == 0) ? 0 : instance._statBundleThroughputTimes.Average();
            _entryValues[FootPrintEntry.BundleThroughputTimeMed] = (instance._statBundleThroughputTimes.Count == 0) ? 0 : StatisticsHelper.GetMedian(instance._statBundleThroughputTimes);
            _entryValues[FootPrintEntry.BundleThroughputTimeLQ] = (instance._statBundleThroughputTimes.Count == 0) ? 0 : StatisticsHelper.GetLowerQuartile(instance._statBundleThroughputTimes);
            _entryValues[FootPrintEntry.BundleThroughputTimeUQ] = (instance._statBundleThroughputTimes.Count == 0) ? 0 : StatisticsHelper.GetUpperQuartile(instance._statBundleThroughputTimes);
            // Lateness
            _entryValues[FootPrintEntry.OrderLatenessAvg] = (!instance._statOrderLatenessTimes.Any(l => l > 0)) ? 0 : instance._statOrderLatenessTimes.Where(l => l > 0).Average();
            _entryValues[FootPrintEntry.OrderLatenessMed] = (!instance._statOrderLatenessTimes.Any(l => l > 0)) ? 0 : StatisticsHelper.GetMedian(instance._statOrderLatenessTimes.Where(l => l > 0));
            _entryValues[FootPrintEntry.OrderLatenessLQ] = (!instance._statOrderLatenessTimes.Any(l => l > 0)) ? 0 : StatisticsHelper.GetLowerQuartile(instance._statOrderLatenessTimes.Where(l => l > 0));
            _entryValues[FootPrintEntry.OrderLatenessUQ] = (!instance._statOrderLatenessTimes.Any(l => l > 0)) ? 0 : StatisticsHelper.GetUpperQuartile(instance._statOrderLatenessTimes.Where(l => l > 0));
            // Offset
            _entryValues[FootPrintEntry.OrderOffsetAvg] = (instance._statOrderLatenessTimes.Count == 0) ? 0 : instance._statOrderLatenessTimes.Average();
            _entryValues[FootPrintEntry.OrderOffsetMed] = (instance._statOrderLatenessTimes.Count == 0) ? 0 : StatisticsHelper.GetMedian(instance._statOrderLatenessTimes);
            _entryValues[FootPrintEntry.OrderOffsetLQ] = (instance._statOrderLatenessTimes.Count == 0) ? 0 : StatisticsHelper.GetLowerQuartile(instance._statOrderLatenessTimes);
            _entryValues[FootPrintEntry.OrderOffsetUQ] = (instance._statOrderLatenessTimes.Count == 0) ? 0 : StatisticsHelper.GetUpperQuartile(instance._statOrderLatenessTimes);
            // Lateness counts
            _entryValues[FootPrintEntry.LateOrdersCount] = instance._statOrderLatenessTimes.Count(l => l > 0);
            _entryValues[FootPrintEntry.OnTimeOrdersCount] = instance._statOrderLatenessTimes.Count(l => l <= 0);
            _entryValues[FootPrintEntry.LateOrdersFractional] = instance._statOrderLatenessTimes.Count == 0 ? 0 : (double)instance._statOrderLatenessTimes.Count(l => l > 0) / instance._statOrderLatenessTimes.Count;
            _entryValues[FootPrintEntry.LateOrdersRate] = instance._statOrderLatenessTimes.Count(l => l > 0) / TimeSpan.FromSeconds(instance.SettingConfig.SimulationDuration).TotalHours;
            _entryValues[FootPrintEntry.OnTimeOrdersRate] = instance._statOrderLatenessTimes.Count(l => l <= 0) / TimeSpan.FromSeconds(instance.SettingConfig.SimulationDuration).TotalHours;
            // Item pile-on
            _entryValues[FootPrintEntry.ItemPileOneAvg] = instance.OutputStations.Count == 0 ? 0 : instance.OutputStations.Where(s => s.StatNumItemsPicked > 0).Average(s => s.StatItemPileOn);
            _entryValues[FootPrintEntry.ItemPileOneMed] = instance.OutputStations.Count == 0 ? 0 : StatisticsHelper.GetMedian(instance.OutputStations.Where(s => s.StatNumItemsPicked > 0).Select(s => s.StatItemPileOn));
            _entryValues[FootPrintEntry.ItemPileOneLQ] = instance.OutputStations.Count == 0 ? 0 : StatisticsHelper.GetLowerQuartile(instance.OutputStations.Where(s => s.StatNumItemsPicked > 0).Select(s => s.StatItemPileOn));
            _entryValues[FootPrintEntry.ItemPileOneUQ] = instance.OutputStations.Count == 0 ? 0 : StatisticsHelper.GetUpperQuartile(instance.OutputStations.Where(s => s.StatNumItemsPicked > 0).Select(s => s.StatItemPileOn));
            // Injected item pile-on
            _entryValues[FootPrintEntry.InjectedItemPileOneAvg] = instance.OutputStations.Count == 0 ? 0 : instance.OutputStations.Average(s => s.StatInjectedItemPileOn);
            _entryValues[FootPrintEntry.InjectedItemPileOneMed] = instance.OutputStations.Count == 0 ? 0 : StatisticsHelper.GetMedian(instance.OutputStations.Select(s => s.StatInjectedItemPileOn));
            _entryValues[FootPrintEntry.InjectedItemPileOneLQ] = instance.OutputStations.Count == 0 ? 0 : StatisticsHelper.GetLowerQuartile(instance.OutputStations.Select(s => s.StatInjectedItemPileOn));
            _entryValues[FootPrintEntry.InjectedItemPileOneUQ] = instance.OutputStations.Count == 0 ? 0 : StatisticsHelper.GetUpperQuartile(instance.OutputStations.Select(s => s.StatInjectedItemPileOn));
            // Order pile-on
            _entryValues[FootPrintEntry.OrderPileOneAvg] = instance.OutputStations.Count == 0 ? 0 : instance.OutputStations.Average(s => s.StatOrderPileOn);
            _entryValues[FootPrintEntry.OrderPileOneMed] = instance.OutputStations.Count == 0 ? 0 : StatisticsHelper.GetMedian(instance.OutputStations.Select(s => s.StatOrderPileOn));
            _entryValues[FootPrintEntry.OrderPileOneLQ] = instance.OutputStations.Count == 0 ? 0 : StatisticsHelper.GetLowerQuartile(instance.OutputStations.Select(s => s.StatOrderPileOn));
            _entryValues[FootPrintEntry.OrderPileOneUQ] = instance.OutputStations.Count == 0 ? 0 : StatisticsHelper.GetUpperQuartile(instance.OutputStations.Select(s => s.StatOrderPileOn));
            // Bundle pile-on
            _entryValues[FootPrintEntry.BundlePileOneAvg] = instance.InputStations.Count == 0 ? 0 : instance.InputStations.Average(s => s.StatBundlePileOn);
            _entryValues[FootPrintEntry.BundlePileOneMed] = instance.InputStations.Count == 0 ? 0 : StatisticsHelper.GetMedian(instance.InputStations.Select(s => s.StatBundlePileOn));
            _entryValues[FootPrintEntry.BundlePileOneLQ] = instance.InputStations.Count == 0 ? 0 : StatisticsHelper.GetLowerQuartile(instance.InputStations.Select(s => s.StatBundlePileOn));
            _entryValues[FootPrintEntry.BundlePileOneUQ] = instance.InputStations.Count == 0 ? 0 : StatisticsHelper.GetUpperQuartile(instance.InputStations.Select(s => s.StatBundlePileOn));
            // Injected bundle pile-on
            _entryValues[FootPrintEntry.InjectedBundlePileOneAvg] = instance.InputStations.Count == 0 ? 0 : instance.InputStations.Average(s => s.StatInjectedBundlePileOn);
            _entryValues[FootPrintEntry.InjectedBundlePileOneMed] = instance.InputStations.Count == 0 ? 0 : StatisticsHelper.GetMedian(instance.InputStations.Select(s => s.StatInjectedBundlePileOn));
            _entryValues[FootPrintEntry.InjectedBundlePileOneLQ] = instance.InputStations.Count == 0 ? 0 : StatisticsHelper.GetLowerQuartile(instance.InputStations.Select(s => s.StatInjectedBundlePileOn));
            _entryValues[FootPrintEntry.InjectedBundlePileOneUQ] = instance.InputStations.Count == 0 ? 0 : StatisticsHelper.GetUpperQuartile(instance.InputStations.Select(s => s.StatInjectedBundlePileOn));
            // Idle-time
            _entryValues[FootPrintEntry.ISIdleTimeAvg] = instance.InputStations.Count == 0 ? 0 : instance.InputStations.Average(s => s.StatIdleTime / (instance.Controller.CurrentTime - instance.StatTimeStart));
            _entryValues[FootPrintEntry.ISIdleTimeMed] = instance.InputStations.Count == 0 ? 0 : StatisticsHelper.GetMedian(instance.InputStations.Select(s => s.StatIdleTime / (instance.Controller.CurrentTime - instance.StatTimeStart)));
            _entryValues[FootPrintEntry.ISIdleTimeLQ] = instance.InputStations.Count == 0 ? 0 : StatisticsHelper.GetLowerQuartile(instance.InputStations.Select(s => s.StatIdleTime / (instance.Controller.CurrentTime - instance.StatTimeStart)));
            _entryValues[FootPrintEntry.ISIdleTimeUQ] = instance.InputStations.Count == 0 ? 0 : StatisticsHelper.GetUpperQuartile(instance.InputStations.Select(s => s.StatIdleTime / (instance.Controller.CurrentTime - instance.StatTimeStart)));
            _entryValues[FootPrintEntry.OSIdleTimeAvg] = instance.OutputStations.Count == 0 ? 0 : instance.OutputStations.Average(s => s.StatIdleTime / (instance.Controller.CurrentTime - instance.StatTimeStart));
            _entryValues[FootPrintEntry.OSIdleTimeMed] = instance.OutputStations.Count == 0 ? 0 : StatisticsHelper.GetMedian(instance.OutputStations.Select(s => s.StatIdleTime / (instance.Controller.CurrentTime - instance.StatTimeStart)));
            _entryValues[FootPrintEntry.OSIdleTimeLQ] = instance.OutputStations.Count == 0 ? 0 : StatisticsHelper.GetLowerQuartile(instance.OutputStations.Select(s => s.StatIdleTime / (instance.Controller.CurrentTime - instance.StatTimeStart)));
            _entryValues[FootPrintEntry.OSIdleTimeUQ] = instance.OutputStations.Count == 0 ? 0 : StatisticsHelper.GetUpperQuartile(instance.OutputStations.Select(s => s.StatIdleTime / (instance.Controller.CurrentTime - instance.StatTimeStart)));
            // Up-time
            _entryValues[FootPrintEntry.ISUpTimeAvg] = instance.InputStations.Count == 0 ? 0 : instance.InputStations.Average(s => s.StatActiveTime / (instance.Controller.CurrentTime - instance.StatTimeStart));
            _entryValues[FootPrintEntry.ISUpTimeMed] = instance.InputStations.Count == 0 ? 0 : StatisticsHelper.GetMedian(instance.InputStations.Select(s => s.StatActiveTime / (instance.Controller.CurrentTime - instance.StatTimeStart)));
            _entryValues[FootPrintEntry.ISUpTimeLQ] = instance.InputStations.Count == 0 ? 0 : StatisticsHelper.GetLowerQuartile(instance.InputStations.Select(s => s.StatActiveTime / (instance.Controller.CurrentTime - instance.StatTimeStart)));
            _entryValues[FootPrintEntry.ISUpTimeUQ] = instance.InputStations.Count == 0 ? 0 : StatisticsHelper.GetUpperQuartile(instance.InputStations.Select(s => s.StatActiveTime / (instance.Controller.CurrentTime - instance.StatTimeStart)));
            _entryValues[FootPrintEntry.OSUpTimeAvg] = instance.OutputStations.Count == 0 ? 0 : instance.OutputStations.Average(s => s.StatActiveTime / (instance.Controller.CurrentTime - instance.StatTimeStart));
            _entryValues[FootPrintEntry.OSUpTimeMed] = instance.OutputStations.Count == 0 ? 0 : StatisticsHelper.GetMedian(instance.OutputStations.Select(s => s.StatActiveTime / (instance.Controller.CurrentTime - instance.StatTimeStart)));
            _entryValues[FootPrintEntry.OSUpTimeLQ] = instance.OutputStations.Count == 0 ? 0 : StatisticsHelper.GetLowerQuartile(instance.OutputStations.Select(s => s.StatActiveTime / (instance.Controller.CurrentTime - instance.StatTimeStart)));
            _entryValues[FootPrintEntry.OSUpTimeUQ] = instance.OutputStations.Count == 0 ? 0 : StatisticsHelper.GetUpperQuartile(instance.OutputStations.Select(s => s.StatActiveTime / (instance.Controller.CurrentTime - instance.StatTimeStart)));
            // Down-time
            _entryValues[FootPrintEntry.ISDownTimeAvg] = instance.InputStations.Count == 0 ? 0 : instance.InputStations.Average(s => s.StatDownTime / (instance.Controller.CurrentTime - instance.StatTimeStart));
            _entryValues[FootPrintEntry.ISDownTimeMed] = instance.InputStations.Count == 0 ? 0 : StatisticsHelper.GetMedian(instance.InputStations.Select(s => s.StatDownTime / (instance.Controller.CurrentTime - instance.StatTimeStart)));
            _entryValues[FootPrintEntry.ISDownTimeLQ] = instance.InputStations.Count == 0 ? 0 : StatisticsHelper.GetLowerQuartile(instance.InputStations.Select(s => s.StatDownTime / (instance.Controller.CurrentTime - instance.StatTimeStart)));
            _entryValues[FootPrintEntry.ISDownTimeUQ] = instance.InputStations.Count == 0 ? 0 : StatisticsHelper.GetUpperQuartile(instance.InputStations.Select(s => s.StatDownTime / (instance.Controller.CurrentTime - instance.StatTimeStart)));
            _entryValues[FootPrintEntry.OSDownTimeAvg] = instance.OutputStations.Count == 0 ? 0 : instance.OutputStations.Average(s => s.StatDownTime / (instance.Controller.CurrentTime - instance.StatTimeStart));
            _entryValues[FootPrintEntry.OSDownTimeMed] = instance.OutputStations.Count == 0 ? 0 : StatisticsHelper.GetMedian(instance.OutputStations.Select(s => s.StatDownTime / (instance.Controller.CurrentTime - instance.StatTimeStart)));
            _entryValues[FootPrintEntry.OSDownTimeLQ] = instance.OutputStations.Count == 0 ? 0 : StatisticsHelper.GetLowerQuartile(instance.OutputStations.Select(s => s.StatDownTime / (instance.Controller.CurrentTime - instance.StatTimeStart)));
            _entryValues[FootPrintEntry.OSDownTimeUQ] = instance.OutputStations.Count == 0 ? 0 : StatisticsHelper.GetUpperQuartile(instance.OutputStations.Select(s => s.StatDownTime / (instance.Controller.CurrentTime - instance.StatTimeStart)));
            // Task stats
            _entryValues[FootPrintEntry.TaskCountNone] = instance.Bots.Sum(b => b.StatTotalTaskCounts[BotTaskType.None]);
            _entryValues[FootPrintEntry.TaskTimeNone] = instance.Bots.Sum(b => b.StatTotalTaskTimes[BotTaskType.None]);
            _entryValues[FootPrintEntry.TaskCountParkPod] = instance.Bots.Sum(b => b.StatTotalTaskCounts[BotTaskType.ParkPod]);
            _entryValues[FootPrintEntry.TaskTimeParkPod] = instance.Bots.Sum(b => b.StatTotalTaskTimes[BotTaskType.ParkPod]);
            _entryValues[FootPrintEntry.TaskCountRepositionPod] = instance.Bots.Sum(b => b.StatTotalTaskCounts[BotTaskType.RepositionPod]);
            _entryValues[FootPrintEntry.TaskTimeRepositionPod] = instance.Bots.Sum(b => b.StatTotalTaskTimes[BotTaskType.RepositionPod]);
            _entryValues[FootPrintEntry.TaskCountInsert] = instance.Bots.Sum(b => b.StatTotalTaskCounts[BotTaskType.Insert]);
            _entryValues[FootPrintEntry.TaskTimeInsert] = instance.Bots.Sum(b => b.StatTotalTaskTimes[BotTaskType.Insert]);
            _entryValues[FootPrintEntry.TaskCountExtract] = instance.Bots.Sum(b => b.StatTotalTaskCounts[BotTaskType.Extract]);
            _entryValues[FootPrintEntry.TaskTimeExtract] = instance.Bots.Sum(b => b.StatTotalTaskTimes[BotTaskType.Extract]);
            _entryValues[FootPrintEntry.TaskCountRest] = instance.Bots.Sum(b => b.StatTotalTaskCounts[BotTaskType.Rest]);
            _entryValues[FootPrintEntry.TaskTimeRest] = instance.Bots.Sum(b => b.StatTotalTaskTimes[BotTaskType.Rest]);
            // Aggregate task stats
            _entryValues[FootPrintEntry.TaskTimeBusyAvg] = instance.Bots.Average(b =>
                b.StatTotalTaskTimes[BotTaskType.ParkPod] +
                b.StatTotalTaskTimes[BotTaskType.RepositionPod] +
                b.StatTotalTaskTimes[BotTaskType.Insert] +
                b.StatTotalTaskTimes[BotTaskType.Extract]);
            _entryValues[FootPrintEntry.TaskTimeBusyAvgFractional] = ((double)_entryValues[FootPrintEntry.TaskTimeBusyAvg]) / instance.SettingConfig.SimulationDuration;
            // State stats
            _entryValues[FootPrintEntry.StateCountPickup] = instance.Bots.Sum(b => b.StatTotalStateCounts[Bots.BotStateType.PickupPod]);
            _entryValues[FootPrintEntry.StateTimePickup] = instance.Bots.Sum(b => b.StatTotalStateTimes[Bots.BotStateType.PickupPod]);
            _entryValues[FootPrintEntry.StateCountSetdown] = instance.Bots.Sum(b => b.StatTotalStateCounts[Bots.BotStateType.SetdownPod]);
            _entryValues[FootPrintEntry.StateTimeSetdown] = instance.Bots.Sum(b => b.StatTotalStateTimes[Bots.BotStateType.SetdownPod]);
            _entryValues[FootPrintEntry.StateCountGet] = instance.Bots.Sum(b => b.StatTotalStateCounts[Bots.BotStateType.GetItems]);
            _entryValues[FootPrintEntry.StateTimeGet] = instance.Bots.Sum(b => b.StatTotalStateTimes[Bots.BotStateType.GetItems]);
            _entryValues[FootPrintEntry.StateCountPut] = instance.Bots.Sum(b => b.StatTotalStateCounts[Bots.BotStateType.PutItems]);
            _entryValues[FootPrintEntry.StateTimePut] = instance.Bots.Sum(b => b.StatTotalStateTimes[Bots.BotStateType.PutItems]);
            _entryValues[FootPrintEntry.StateCountRest] = instance.Bots.Sum(b => b.StatTotalStateCounts[Bots.BotStateType.Rest]);
            _entryValues[FootPrintEntry.StateTimeRest] = instance.Bots.Sum(b => b.StatTotalStateTimes[Bots.BotStateType.Rest]);
            _entryValues[FootPrintEntry.StateCountMove] = instance.Bots.Sum(b => b.StatTotalStateCounts[Bots.BotStateType.Move]);
            _entryValues[FootPrintEntry.StateTimeMove] = instance.Bots.Sum(b => b.StatTotalStateTimes[Bots.BotStateType.Move]);
            _entryValues[FootPrintEntry.StateCountEvade] = instance.Bots.Sum(b => b.StatTotalStateCounts[Bots.BotStateType.Evade]);
            _entryValues[FootPrintEntry.StateTimeEvade] = instance.Bots.Sum(b => b.StatTotalStateTimes[Bots.BotStateType.Evade]);
            _entryValues[FootPrintEntry.StateCountElevator] = instance.Bots.Sum(b => b.StatTotalStateCounts[Bots.BotStateType.UseElevator]);
            _entryValues[FootPrintEntry.StateTimeElevator] = instance.Bots.Sum(b => b.StatTotalStateTimes[Bots.BotStateType.UseElevator]);
            // Performance measurment
            _entryValues[FootPrintEntry.TimingDecisionsOverall] = instance.Observer.TimingDecisionsOverall;
            _entryValues[FootPrintEntry.TimingPathPlanningAvg] = instance.Observer.TimingPathPlanningAverage;
            _entryValues[FootPrintEntry.TimingPathPlanningOverall] = instance.Observer.TimingPathPlanningOverall;
            _entryValues[FootPrintEntry.TimingPathPlanningCount] = instance.Observer.TimingPathPlanningDecisionCount;
            _entryValues[FootPrintEntry.TimingTaskAllocationAvg] = instance.Observer.TimingTaskAllocationAverage;
            _entryValues[FootPrintEntry.TimingTaskAllocationOverall] = instance.Observer.TimingTaskAllocationOverall;
            _entryValues[FootPrintEntry.TimingTaskAllocationCount] = instance.Observer.TimingTaskAllocationDecisionCount;
            _entryValues[FootPrintEntry.TimingItemStorageAvg] = instance.Observer.TimingItemStorageAverage;
            _entryValues[FootPrintEntry.TimingItemStorageOverall] = instance.Observer.TimingItemStorageOverall;
            _entryValues[FootPrintEntry.TimingItemStorageCount] = instance.Observer.TimingItemStorageDecisionCount;
            _entryValues[FootPrintEntry.TimingPodStorageAvg] = instance.Observer.TimingPodStorageAverage;
            _entryValues[FootPrintEntry.TimingPodStorageOverall] = instance.Observer.TimingPodStorageOverall;
            _entryValues[FootPrintEntry.TimingPodStorageCount] = instance.Observer.TimingPodStorageDecisionCount;
            _entryValues[FootPrintEntry.TimingRepositioningAvg] = instance.Observer.TimingPodStorageAverage;
            _entryValues[FootPrintEntry.TimingRepositioningOverall] = instance.Observer.TimingPodStorageOverall;
            _entryValues[FootPrintEntry.TimingRepositioningCount] = instance.Observer.TimingPodStorageDecisionCount;
            _entryValues[FootPrintEntry.TimingReplenishmentBatchingAvg] = instance.Observer.TimingReplenishmentBatchingAverage;
            _entryValues[FootPrintEntry.TimingReplenishmentBatchingOverall] = instance.Observer.TimingReplenishmentBatchingOverall;
            _entryValues[FootPrintEntry.TimingReplenishmentBatchingCount] = instance.Observer.TimingReplenishmentBatchingDecisionCount;
            _entryValues[FootPrintEntry.TimingOrderBatchingAvg] = instance.Observer.TimingOrderBatchingAverage;
            _entryValues[FootPrintEntry.TimingOrderBatchingOverall] = instance.Observer.TimingOrderBatchingOverall;
            _entryValues[FootPrintEntry.TimingOrderBatchingCount] = instance.Observer.TimingOrderBatchingDecisionCount;
            // Custom performance info
            _entryValues[FootPrintEntry.CustomLogPPString] = instance.StatCustomControllerInfo.CustomLogPPString;
            _entryValues[FootPrintEntry.CustomLogPP1] = instance.StatCustomControllerInfo.CustomLogPP1;
            _entryValues[FootPrintEntry.CustomLogPP2] = instance.StatCustomControllerInfo.CustomLogPP2;
            _entryValues[FootPrintEntry.CustomLogPP3] = instance.StatCustomControllerInfo.CustomLogPP3;
            _entryValues[FootPrintEntry.CustomLogPP4] = instance.StatCustomControllerInfo.CustomLogPP4;
            _entryValues[FootPrintEntry.CustomLogTAString] = instance.StatCustomControllerInfo.CustomLogTAString;
            _entryValues[FootPrintEntry.CustomLogTA1] = instance.StatCustomControllerInfo.CustomLogTA1;
            _entryValues[FootPrintEntry.CustomLogTA2] = instance.StatCustomControllerInfo.CustomLogTA2;
            _entryValues[FootPrintEntry.CustomLogTA3] = instance.StatCustomControllerInfo.CustomLogTA3;
            _entryValues[FootPrintEntry.CustomLogTA4] = instance.StatCustomControllerInfo.CustomLogTA4;
            _entryValues[FootPrintEntry.CustomLogPCString] = instance.StatCustomControllerInfo.CustomLogPCString;
            _entryValues[FootPrintEntry.CustomLogPC1] = instance.StatCustomControllerInfo.CustomLogPC1;
            _entryValues[FootPrintEntry.CustomLogPC2] = instance.StatCustomControllerInfo.CustomLogPC2;
            _entryValues[FootPrintEntry.CustomLogPC3] = instance.StatCustomControllerInfo.CustomLogPC3;
            _entryValues[FootPrintEntry.CustomLogPC4] = instance.StatCustomControllerInfo.CustomLogPC4;
            _entryValues[FootPrintEntry.CustomLogSAString] = instance.StatCustomControllerInfo.CustomLogSAString;
            _entryValues[FootPrintEntry.CustomLogSA1] = instance.StatCustomControllerInfo.CustomLogSA1;
            _entryValues[FootPrintEntry.CustomLogSA2] = instance.StatCustomControllerInfo.CustomLogSA2;
            _entryValues[FootPrintEntry.CustomLogSA3] = instance.StatCustomControllerInfo.CustomLogSA3;
            _entryValues[FootPrintEntry.CustomLogSA4] = instance.StatCustomControllerInfo.CustomLogSA4;
            _entryValues[FootPrintEntry.CustomLogISString] = instance.StatCustomControllerInfo.CustomLogISString;
            _entryValues[FootPrintEntry.CustomLogIS1] = instance.StatCustomControllerInfo.CustomLogIS1;
            _entryValues[FootPrintEntry.CustomLogIS2] = instance.StatCustomControllerInfo.CustomLogIS2;
            _entryValues[FootPrintEntry.CustomLogIS3] = instance.StatCustomControllerInfo.CustomLogIS3;
            _entryValues[FootPrintEntry.CustomLogIS4] = instance.StatCustomControllerInfo.CustomLogIS4;
            _entryValues[FootPrintEntry.CustomLogPSString] = instance.StatCustomControllerInfo.CustomLogPSString;
            _entryValues[FootPrintEntry.CustomLogPS1] = instance.StatCustomControllerInfo.CustomLogPS1;
            _entryValues[FootPrintEntry.CustomLogPS2] = instance.StatCustomControllerInfo.CustomLogPS2;
            _entryValues[FootPrintEntry.CustomLogPS3] = instance.StatCustomControllerInfo.CustomLogPS3;
            _entryValues[FootPrintEntry.CustomLogPS4] = instance.StatCustomControllerInfo.CustomLogPS4;
            _entryValues[FootPrintEntry.CustomLogRPString] = instance.StatCustomControllerInfo.CustomLogRPString;
            _entryValues[FootPrintEntry.CustomLogRP1] = instance.StatCustomControllerInfo.CustomLogRP1;
            _entryValues[FootPrintEntry.CustomLogRP2] = instance.StatCustomControllerInfo.CustomLogRP2;
            _entryValues[FootPrintEntry.CustomLogRP3] = instance.StatCustomControllerInfo.CustomLogRP3;
            _entryValues[FootPrintEntry.CustomLogRP4] = instance.StatCustomControllerInfo.CustomLogRP4;
            _entryValues[FootPrintEntry.CustomLogOBString] = instance.StatCustomControllerInfo.CustomLogOBString;
            _entryValues[FootPrintEntry.CustomLogOB1] = instance.StatCustomControllerInfo.CustomLogOB1;
            _entryValues[FootPrintEntry.CustomLogOB2] = instance.StatCustomControllerInfo.CustomLogOB2;
            _entryValues[FootPrintEntry.CustomLogOB3] = instance.StatCustomControllerInfo.CustomLogOB3;
            _entryValues[FootPrintEntry.CustomLogOB4] = instance.StatCustomControllerInfo.CustomLogOB4;
            _entryValues[FootPrintEntry.CustomLogRBString] = instance.StatCustomControllerInfo.CustomLogRBString;
            _entryValues[FootPrintEntry.CustomLogRB1] = instance.StatCustomControllerInfo.CustomLogRB1;
            _entryValues[FootPrintEntry.CustomLogRB2] = instance.StatCustomControllerInfo.CustomLogRB2;
            _entryValues[FootPrintEntry.CustomLogRB3] = instance.StatCustomControllerInfo.CustomLogRB3;
            _entryValues[FootPrintEntry.CustomLogRB4] = instance.StatCustomControllerInfo.CustomLogRB4;
            _entryValues[FootPrintEntry.CustomLogMMString] = instance.StatCustomControllerInfo.CustomLogMMString;
            _entryValues[FootPrintEntry.CustomLogMM1] = instance.StatCustomControllerInfo.CustomLogMM1;
            _entryValues[FootPrintEntry.CustomLogMM2] = instance.StatCustomControllerInfo.CustomLogMM2;
            _entryValues[FootPrintEntry.CustomLogMM3] = instance.StatCustomControllerInfo.CustomLogMM3;
            _entryValues[FootPrintEntry.CustomLogMM4] = instance.StatCustomControllerInfo.CustomLogMM4;


            // Comment tags
            _entryValues[FootPrintEntry.TagSetting1] = instance.SettingConfig.CommentTag1;
            _entryValues[FootPrintEntry.TagSetting2] = instance.SettingConfig.CommentTag2;
            _entryValues[FootPrintEntry.TagSetting3] = instance.SettingConfig.CommentTag3;
            _entryValues[FootPrintEntry.TagControl1] = instance.ControllerConfig.CommentTag1;
            _entryValues[FootPrintEntry.TagControl2] = instance.ControllerConfig.CommentTag2;
            _entryValues[FootPrintEntry.TagControl3] = instance.ControllerConfig.CommentTag3;
        }
        /// <summary>
        /// Creates a datapoint from a string serialization of this datapoint.
        /// </summary>
        /// <param name="footprintline">The string representation of the datapoint.</param>
        public FootprintDatapoint(string footprintline)
        {
            // Get elements
            string[] elements = footprintline.Split(IOConstants.DELIMITER_VALUE);
            // Check count
            if (elements.Length != EntryOrder.Count)
                throw new ArgumentException("Unexpected entry count - trying to parse an older version?");
            // Parse them
            int index = 0;
            foreach (var entryDescription in EntryOrder)
            {
                if (_entryTypes[entryDescription] == typeof(string))
                    _entryValues[entryDescription] = elements[index];
                if (_entryTypes[entryDescription] == typeof(long))
                    _entryValues[entryDescription] = long.Parse(elements[index]);
                if (_entryTypes[entryDescription] == typeof(int))
                    _entryValues[entryDescription] = int.Parse(elements[index]);
                if (_entryTypes[entryDescription] == typeof(double))
                    _entryValues[entryDescription] = double.Parse(elements[index], IOConstants.FORMATTER);
                if (!_entryValues.ContainsKey(entryDescription))
                    throw new ArgumentException("Unexpected type of the element to parse: " + _entryTypes[entryDescription].ToString());
                // Increase index to align access to the string list values
                index++;
            }
        }
        /// <summary>
        /// Returns a line that can be used as a header for datapoints of this kind.
        /// </summary>
        /// <returns>The header line.</returns>
        public static string GetFootprintHeader()
        {
            // Fill header
            string delim = IOConstants.DELIMITER_VALUE.ToString();
            return string.Join(delim, EntryOrder.Select(e => e.ToString()));
        }
        /// <summary>
        /// Returns the footprint as a string line.
        /// </summary>
        /// <returns>The line representing the datapoint.</returns>
        public string GetFootprint()
        {
            // Fill footprint
            string delim = IOConstants.DELIMITER_VALUE.ToString();
            return string.Join(delim, EntryOrder.Select(e =>
            {
                object value = _entryValues[e];
                if (value is string) { return value as string; }
                else
                {
                    if (value is double || value is float) { return ((double)value).ToString(IOConstants.FORMATTER); }
                    else { return value.ToString(); }
                }
            }));
        }
    }

    #endregion

    #region Well sortedness datapoint

    /// <summary>
    /// Contains one value per distance
    /// </summary>
    public class WellSortednessPathTimeTuple
    {
        /// <summary>
        /// The distance to an output station for all values contained in this struct.
        /// </summary>
        public double PathTime;
        /// <summary>
        /// The number of pods stored at this path time length.
        /// </summary>
        public double PodCount;
        /// <summary>
        /// The number of storage locations at this path time length.
        /// </summary>
        public double StorageLocationCount;
        /// <summary>
        /// The average frequency of the item descriptions on the pods summed across the pods.
        /// </summary>
        public double SKUFrequencySum;
        /// <summary>
        /// The summed frequency of the physical items on the pods summed across the pods.
        /// </summary>
        public double ContentFrequencySum;
        /// <summary>
        /// The summed trips to output-stations from the given distance.
        /// </summary>
        public double OutputStationTripsSum;
        /// <summary>
        /// The summed speed score of all pods in the row.
        /// </summary>
        public double PodSpeedSum;
        /// <summary>
        /// The summed utility score of all pods in the row.
        /// </summary>
        public double PodUtilitySum;
        /// <summary>
        /// The summed combined score of all pods in the row.
        /// </summary>
        public double PodCombinedScoreSum;
    }
    /// <summary>
    /// Contains information about the current situation of well sortedness.
    /// </summary>
    public class WellSortednessDatapoint
    {
        /// <summary>
        /// The timestamp of this snapshot.
        /// </summary>
        public double TimeStamp;
        /// <summary>
        /// All values grouped per path time.
        /// </summary>
        public List<WellSortednessPathTimeTuple> PathTimeFrequencies;
        /// <summary>
        /// Creates a new datapoint.
        /// </summary>
        /// <param name="timeStamp">The timestamp of the snapshot.</param>
        /// <param name="distanceFrequencies">All frequency information.</param>
        public WellSortednessDatapoint(double timeStamp, List<WellSortednessPathTimeTuple> distanceFrequencies) { TimeStamp = timeStamp; PathTimeFrequencies = distanceFrequencies; }
        /// <summary>
        /// Creates a datapoint from a serialized line version of it.
        /// </summary>
        /// <param name="datapointLine">The datapoint as a serialized line.</param>
        public WellSortednessDatapoint(string datapointLine)
        {
            string[] firstLevel = datapointLine.Split(IOConstants.DELIMITER_VALUE);
            TimeStamp = double.Parse(firstLevel[0], IOConstants.FORMATTER);
            string[] secondLevel = firstLevel[1].Split(IOConstants.DELIMITER_LIST);
            PathTimeFrequencies = secondLevel.Select(e =>
            {
                string[] thirdLevel = e.Split(IOConstants.DELIMITER_TUPLE);
                WellSortednessPathTimeTuple tuple = new WellSortednessPathTimeTuple();
                ReflectionTools.ParseStringToFields(tuple, typeof(WellSortednessPathTimeTuple), thirdLevel, IOConstants.FORMATTER);
                return tuple;
            }).ToList();
        }
        /// <summary>
        /// Returns a headline for the string representation.
        /// </summary>
        /// <returns>A headline.</returns>
        public static string GetHeader()
        {
            return
                IOConstants.COMMENT_LINE + nameof(TimeStamp) + IOConstants.DELIMITER_VALUE +
                string.Join(IOConstants.DELIMITER_TUPLE.ToString(), ReflectionTools.ConvertFieldsToDescriptions(typeof(WellSortednessPathTimeTuple))) + "...";
        }
        /// <summary>
        /// Returns a string representation of this datapoint.
        /// </summary>
        /// <returns>The datapoint represented as a string.</returns>
        public string GetLine()
        {
            return
                TimeStamp.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                string.Join(
                    IOConstants.DELIMITER_LIST.ToString(),
                    PathTimeFrequencies.Select(d =>
                        string.Join(IOConstants.DELIMITER_TUPLE.ToString(), ReflectionTools.ConvertFields(d, typeof(WellSortednessPathTimeTuple), IOConstants.FORMATTER, IOConstants.EXPORT_FORMAT_SHORTER))));
        }
    }

    #endregion

    #region Inventory related datapoint

    /// <summary>
    /// Captures frequency information for a given item description.
    /// </summary>
    public class ItemDescriptionFrequencyDatapoint
    {
        /// <summary>
        /// ID of the corresponding item description.
        /// </summary>
        public int ItemDescriptionID;
        /// <summary>
        /// The weight for generating the item.
        /// </summary>
        public double Weight;
        /// <summary>
        /// The number of times the item was ordered.
        /// </summary>
        public int OrderCount;
        /// <summary>
        /// The measured frequency.
        /// </summary>
        public double MeasuredFrequency;
        /// <summary>
        /// The static frequency.
        /// </summary>
        public double StaticFrequency;

        /// <summary>
        /// Creates a new datapoint.
        /// </summary>
        /// <param name="itemDescription">The actual item description captured by this datapoint.</param>
        /// <param name="frequencyTracker">The frequency tracker containing more information.</param>
        public ItemDescriptionFrequencyDatapoint(ItemDescription itemDescription, FrequencyTracker frequencyTracker)
        {
            ItemDescriptionID = itemDescription.ID;
            Weight = itemDescription.Weight;
            OrderCount = itemDescription.OrderCount;
            MeasuredFrequency = frequencyTracker.GetMeasuredFrequency(itemDescription);
            StaticFrequency = frequencyTracker.GetStaticFrequency(itemDescription);
        }
        /// <summary>
        /// Creates a datapoint from a string representation.
        /// </summary>
        /// <param name="line">The line representing this datapoint.</param>
        public ItemDescriptionFrequencyDatapoint(string line)
        {
            string[] values = line.Split(IOConstants.DELIMITER_VALUE);
            ItemDescriptionID = int.Parse(values[0], IOConstants.FORMATTER);
            Weight = double.Parse(values[1], IOConstants.FORMATTER);
            OrderCount = int.Parse(values[2], IOConstants.FORMATTER);
            MeasuredFrequency = double.Parse(values[3], IOConstants.FORMATTER);
            StaticFrequency = double.Parse(values[4], IOConstants.FORMATTER);
        }
        /// <summary>
        /// Returns a headline for the string representation.
        /// </summary>
        /// <returns>A headline.</returns>
        public static string GetHeader()
        {
            return
                nameof(ItemDescriptionID) + IOConstants.DELIMITER_VALUE +
                nameof(Weight) + IOConstants.DELIMITER_VALUE +
                nameof(OrderCount) + IOConstants.DELIMITER_VALUE +
                nameof(MeasuredFrequency) + IOConstants.DELIMITER_VALUE +
                nameof(StaticFrequency);
        }
        /// <summary>
        /// Returns a string representation of this datapoint.
        /// </summary>
        /// <returns>The datapoint represented as a string.</returns>
        public string GetLine()
        {
            return
                ItemDescriptionID.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                Weight.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                OrderCount.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                MeasuredFrequency.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                StaticFrequency.ToString(IOConstants.FORMATTER);
        }
    }

    #endregion

    #region Station related datapoint

    /// <summary>
    /// Contains statistics about a station.
    /// </summary>
    public class StationStatisticsDataPoint
    {
        /// <summary>
        /// The number of pods handled at that station.
        /// </summary>
        public int PodsHandled;
        /// <summary>
        /// The average time it took to handle a pod at a station.
        /// </summary>
        public double PodHandlingTimeAvg;
        /// <summary>
        /// The variance in the pod handling time.
        /// </summary>
        public double PodHandlingTimeVariance;
        /// <summary>
        /// The overall sum of pod handling time.
        /// </summary>
        public double PodHandlingTimeSum;
        /// <summary>
        /// The minimal time of handling a pod.
        /// </summary>
        public double PodHandlingTimeMin;
        /// <summary>
        /// The maximal time of handling a pod.
        /// </summary>
        public double PodHandlingTimeMax;
    }

    #endregion

    #region Connection related data points

    /// <summary>
    /// Contains information about a connection between two waypoint of the waypoint-graph.
    /// </summary>
    public class ConnectionStatisticsDataPoint
    {
        /// <summary>
        /// The from part of this connection.
        /// </summary>
        public int FromID;
        /// <summary>
        /// The x-value of the position of the 'from' waypoint.
        /// </summary>
        public double FromX;
        /// <summary>
        /// The y-value of the position of the 'from' waypoint.
        /// </summary>
        public double FromY;
        /// <summary>
        /// Indicates whether the 'from' waypoint is a storage location.
        /// </summary>
        public bool FromIsStorage;
        /// <summary>
        /// Indicates whether the 'from' waypoint is associated with an input-station.
        /// </summary>
        public bool FromIsIStation;
        /// <summary>
        /// Indicates whether the 'from' waypoint is associated with an output-station.
        /// </summary>
        public bool FromIsOStation;
        /// <summary>
        /// Indicates whether the 'from' waypoint is associated with an elevator.
        /// </summary>
        public bool FromIsElevator;
        /// <summary>
        /// The to part of this connection.
        /// </summary>
        public int ToID;
        /// <summary>
        /// The x-value of the position of the 'to' waypoint.
        /// </summary>
        public double ToX;
        /// <summary>
        /// The y-value of the position of the 'to' waypoint.
        /// </summary>
        public double ToY;
        /// <summary>
        /// Indicates whether the 'to' waypoint is a storage location.
        /// </summary>
        public bool ToIsStorage;
        /// <summary>
        /// Indicates whether the 'to' waypoint is associated with an input-station.
        /// </summary>
        public bool ToIsIStation;
        /// <summary>
        /// Indicates whether the 'to' waypoint is associated with an output-station.
        /// </summary>
        public bool ToIsOStation;
        /// <summary>
        /// Indicates whether the 'to' waypoint is associated with an elevator.
        /// </summary>
        public bool ToIsElevator;
        /// <summary>
        /// The number of times the connection was used for a trip.
        /// </summary>
        public int Count;
        /// <summary>
        /// The average travel time per trip along this connection.
        /// </summary>
        public double TravelTimeAvg;
        /// <summary>
        /// The standard deviation of the travel time along this connection.
        /// </summary>
        public double TravelTimeVar;
        /// <summary>
        /// The overall travel time spent for this trip.
        /// </summary>
        public double TravelTimeSum;
        /// <summary>
        /// The minimal travel time spent on a trip.
        /// </summary>
        public double TravelTimeMin;
        /// <summary>
        /// The maximal travel time spent on a trip.
        /// </summary>
        public double TravelTimeMax;
        /// <summary>
        /// Returns a string describing the string tuple representation of a datapoint.
        /// </summary>
        /// <returns>The descriptive string.</returns>
        public static string GetStringTupleRepresentationDescription()
        {
            return
                "FromID" + IOConstants.DELIMITER_VALUE +
                "FromX" + IOConstants.DELIMITER_VALUE +
                "FromY" + IOConstants.DELIMITER_VALUE +
                "FromIsStorage" + IOConstants.DELIMITER_VALUE +
                "FromIsIStation" + IOConstants.DELIMITER_VALUE +
                "FromIsOStation" + IOConstants.DELIMITER_VALUE +
                "FromIsElevator" + IOConstants.DELIMITER_VALUE +
                "ToID" + IOConstants.DELIMITER_VALUE +
                "ToX" + IOConstants.DELIMITER_VALUE +
                "ToY" + IOConstants.DELIMITER_VALUE +
                "ToIsStorage" + IOConstants.DELIMITER_VALUE +
                "ToIsIStation" + IOConstants.DELIMITER_VALUE +
                "ToIsOStation" + IOConstants.DELIMITER_VALUE +
                "ToIsElevator" + IOConstants.DELIMITER_VALUE +
                "Count" + IOConstants.DELIMITER_VALUE +
                "TravelTimeAvg" + IOConstants.DELIMITER_VALUE +
                "TravelTimeVar" + IOConstants.DELIMITER_VALUE +
                "TravelTimeMin" + IOConstants.DELIMITER_VALUE +
                "TravelTimeMax";
        }
        /// <summary>
        /// Returns a string representation of this datapoint that can be used for serialization.
        /// </summary>
        /// <returns>The string representation.</returns>
        public string GetStringTupleRepresentation()
        {
            return
                FromID.ToString() + IOConstants.DELIMITER_VALUE +
                FromX.ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                FromY.ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                FromIsStorage.ToString() + IOConstants.DELIMITER_VALUE +
                FromIsIStation.ToString() + IOConstants.DELIMITER_VALUE +
                FromIsOStation.ToString() + IOConstants.DELIMITER_VALUE +
                FromIsElevator.ToString() + IOConstants.DELIMITER_VALUE +
                ToID.ToString() + IOConstants.DELIMITER_VALUE +
                ToX.ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                ToY.ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                ToIsStorage.ToString() + IOConstants.DELIMITER_VALUE +
                ToIsIStation.ToString() + IOConstants.DELIMITER_VALUE +
                ToIsOStation.ToString() + IOConstants.DELIMITER_VALUE +
                ToIsElevator.ToString() + IOConstants.DELIMITER_VALUE +
                Count.ToString() + IOConstants.DELIMITER_VALUE +
                TravelTimeAvg.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                TravelTimeVar.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                TravelTimeMin.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                TravelTimeMax.ToString(IOConstants.FORMATTER);
        }
        /// <summary>
        /// Parses a string representation into a datapoint and returns it.
        /// </summary>
        /// <param name="s">The string representation of the datapoint.</param>
        /// <returns>The datapoint.</returns>
        public static ConnectionStatisticsDataPoint FromStringTupleRepresentation(string s)
        {
            string[] values = s.Split(IOConstants.DELIMITER_VALUE);
            return new ConnectionStatisticsDataPoint()
            {
                FromID = int.Parse(values[0]),
                FromX = double.Parse(values[1], IOConstants.FORMATTER),
                FromY = double.Parse(values[2], IOConstants.FORMATTER),
                FromIsStorage = bool.Parse(values[3]),
                FromIsIStation = bool.Parse(values[4]),
                FromIsOStation = bool.Parse(values[5]),
                FromIsElevator = bool.Parse(values[6]),
                ToID = int.Parse(values[7]),
                ToX = double.Parse(values[8], IOConstants.FORMATTER),
                ToY = double.Parse(values[9], IOConstants.FORMATTER),
                ToIsStorage = bool.Parse(values[10]),
                ToIsIStation = bool.Parse(values[11]),
                ToIsOStation = bool.Parse(values[12]),
                ToIsElevator = bool.Parse(values[13]),
                Count = int.Parse(values[14]),
                TravelTimeAvg = double.Parse(values[15], IOConstants.FORMATTER),
                TravelTimeVar = double.Parse(values[16], IOConstants.FORMATTER),
                TravelTimeMin = double.Parse(values[17], IOConstants.FORMATTER),
                TravelTimeMax = double.Parse(values[18], IOConstants.FORMATTER),
            };
        }
    }

    #endregion

    #region Custom controller info datapoint

    /// <summary>
    /// Contains controller specific info that is written out to the footprint.
    /// </summary>
    public class CustomControllerDatapoint
    {
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public string CustomLogPPString = "";
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogPP1 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogPP2 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogPP3 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogPP4 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public string CustomLogTAString = "";
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogTA1 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogTA2 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogTA3 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogTA4 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public string CustomLogPCString = "";
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogPC1 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogPC2 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogPC3 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogPC4 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public string CustomLogSAString = "";
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogSA1 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogSA2 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogSA3 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogSA4 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public string CustomLogISString = "";
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogIS1 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogIS2 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogIS3 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogIS4 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public string CustomLogPSString = "";
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogPS1 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogPS2 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogPS3 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogPS4 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public string CustomLogRPString = "";
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogRP1 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogRP2 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogRP3 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogRP4 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public string CustomLogOBString = "";
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogOB1 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogOB2 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogOB3 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogOB4 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public string CustomLogRBString = "";
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogRB1 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogRB2 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogRB3 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogRB4 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public string CustomLogMMString = "";
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogMM1 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogMM2 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogMM3 = double.NaN;
        /// <summary>
        /// A custom performance info by the respective controller.
        /// </summary>
        public double CustomLogMM4 = double.NaN;
    }

    #endregion

    #region Progression related data points

    /// <summary>
    /// Constitutes one data-point for the logging of the bundle handling.
    /// </summary>
    public class BundleHandledDatapoint
    {
        /// <summary>
        /// The corresponding bot.
        /// </summary>
        public int Bot;
        /// <summary>
        /// The corresponding pod.
        /// </summary>
        public int Pod;
        /// <summary>
        /// The corresponding input-station.
        /// </summary>
        public int InputStation;
        /// <summary>
        /// The time-stamp of this datapoint.
        /// </summary>
        public double TimeStamp;
        /// <summary>
        /// The time between placement and handling of the bundle.
        /// </summary>
        public double TurnoverTime;
        /// <summary>
        /// The time between allocation and handling of the bundle.
        /// </summary>
        public double ThroughputTime;
        /// <summary>
        /// Creates a new datapoint.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="pod">The pod.</param>
        /// <param name="iStation">The station.</param>
        /// <param name="turnoverTime">The turnover time.</param>
        /// <param name="throughputTime">The throughput time.</param>
        public BundleHandledDatapoint(double timestamp, int bot, int pod, int iStation, double turnoverTime, double throughputTime)
        {
            TimeStamp = timestamp;
            Bot = bot;
            Pod = pod;
            InputStation = iStation;
            TurnoverTime = turnoverTime;
            ThroughputTime = throughputTime;
        }
        /// <summary>
        /// Creates a new datapoint from a line serialization.
        /// </summary>
        /// <param name="line">The serialized datapoint line.</param>
        public BundleHandledDatapoint(string line)
        {
            string[] values = line.Split(IOConstants.DELIMITER_VALUE);
            TimeStamp = double.Parse(values[0], IOConstants.FORMATTER);
            Bot = int.Parse(values[1]);
            Pod = int.Parse(values[2]);
            InputStation = int.Parse(values[3]);
            TurnoverTime = double.Parse(values[4], IOConstants.FORMATTER);
            ThroughputTime = double.Parse(values[5], IOConstants.FORMATTER);
        }
        /// <summary>
        /// Creates a line serialization of this datapoint.
        /// </summary>
        /// <returns>The datapoint as a line.</returns>
        public string GetLine()
        {
            return
                TimeStamp.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                Bot.ToString() + IOConstants.DELIMITER_VALUE +
                Pod.ToString() + IOConstants.DELIMITER_VALUE +
                InputStation.ToString() + IOConstants.DELIMITER_VALUE +
                TurnoverTime.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                ThroughputTime.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER);
        }
        /// <summary>
        /// Gets a header that describes the values of the line serialization.
        /// </summary>
        /// <returns>A header.</returns>
        public static string GetHeader()
        {
            return
                nameof(TimeStamp) + IOConstants.DELIMITER_VALUE +
                nameof(Bot) + IOConstants.DELIMITER_VALUE +
                nameof(Pod) + IOConstants.DELIMITER_VALUE +
                nameof(InputStation) + IOConstants.DELIMITER_VALUE +
                nameof(TurnoverTime) + IOConstants.DELIMITER_VALUE +
                nameof(ThroughputTime);
        }
    }

    /// <summary>
    /// Constitutes one data-point for the logging of the placement of incoming bundles.
    /// </summary>
    public class BundlePlacedDatapoint
    {
        /// <summary>
        /// The time-stamp of this datapoint.
        /// </summary>
        public double TimeStamp;
        /// <summary>
        /// Creates a new datapoint.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        public BundlePlacedDatapoint(double timestamp) { TimeStamp = timestamp; }
        /// <summary>
        /// Creates a new datapoint from a line serialization.
        /// </summary>
        /// <param name="line">The serialized datapoint.</param>
        public BundlePlacedDatapoint(string line) { TimeStamp = double.Parse(line.Trim(), IOConstants.FORMATTER); }
        /// <summary>
        /// Creates a line serialization of this datapoint.
        /// </summary>
        /// <returns>The line.</returns>
        public string GetLine() { return TimeStamp.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER); }
        /// <summary>
        /// Creates a header for the line serializations.
        /// </summary>
        /// <returns>The header.</returns>
        public static string GetHeader() { return nameof(TimeStamp); }
    }

    /// <summary>
    /// Constitutes one data-point for the logging of the item handling.
    /// </summary>
    public class ItemHandledDatapoint
    {
        /// <summary>
        /// The time-stamp of this datapoint.
        /// </summary>
        public double TimeStamp;
        /// <summary>
        /// The corresponding bot.
        /// </summary>
        public int Bot;
        /// <summary>
        /// The corresponding pod.
        /// </summary>
        public int Pod;
        /// <summary>
        /// The corresponding output-station.
        /// </summary>
        public int OutputStation;
        /// <summary>
        /// Creates a new datapoint.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="pod">The pod.</param>
        /// <param name="oStation">The output station.</param>
        public ItemHandledDatapoint(double timestamp, int bot, int pod, int oStation) { TimeStamp = timestamp; Bot = bot; Pod = pod; OutputStation = oStation; }
        /// <summary>
        /// Creates a new datapoint from a line serialization.
        /// </summary>
        /// <param name="line">The serialized datapoint.</param>
        public ItemHandledDatapoint(string line)
        {
            string[] values = line.Split(IOConstants.DELIMITER_VALUE);
            TimeStamp = double.Parse(values[0], IOConstants.FORMATTER);
            Bot = int.Parse(values[1]);
            Pod = int.Parse(values[2]);
            OutputStation = int.Parse(values[3]);
        }
        /// <summary>
        /// Creates a line serialization of this datapoint.
        /// </summary>
        /// <returns>The line.</returns>
        public string GetLine()
        {
            return
                TimeStamp.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                Bot.ToString() + IOConstants.DELIMITER_VALUE +
                Pod.ToString() + IOConstants.DELIMITER_VALUE +
                OutputStation.ToString();
        }
        /// <summary>
        /// Creates a header for the line serializations.
        /// </summary>
        /// <returns>The header.</returns>
        public static string GetHeader()
        {
            return
                nameof(TimeStamp) + IOConstants.DELIMITER_VALUE +
                nameof(Bot) + IOConstants.DELIMITER_VALUE +
                nameof(Pod) + IOConstants.DELIMITER_VALUE +
                nameof(OutputStation);
        }
    }

    /// <summary>
    /// Constitutes one data-point for the logging of the order handling.
    /// </summary>
    public class OrderHandledDatapoint
    {
        /// <summary>
        /// The time-stamp of this datapoint.
        /// </summary>
        public double TimeStamp;
        /// <summary>
        /// The corresponding output-station.
        /// </summary>
        public int OutputStation;
        /// <summary>
        /// The time between placement and handling of the order.
        /// </summary>
        public double TurnoverTime;
        /// <summary>
        /// The time between allocation and handling of the order.
        /// </summary>
        public double ThroughputTime;
        /// <summary>
        /// The lateness of the order. A negative number indicates the time the order was complete before its due date while a positive number accounts for its lateness.
        /// </summary>
        public double Lateness;
        /// <summary>
        /// Creates a new datapoint.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="oStation">The output station.</param>
        /// <param name="turnoverTime">The turnover time.</param>
        /// <param name="throughputTime">The throughput time.</param>
        /// <param name="lateness">The lateness.</param>
        public OrderHandledDatapoint(double timestamp, int oStation, double turnoverTime, double throughputTime, double lateness)
        { TimeStamp = timestamp; OutputStation = oStation; TurnoverTime = turnoverTime; ThroughputTime = throughputTime; Lateness = lateness; }
        /// <summary>
        /// Creates a new datapoint from a line serialization.
        /// </summary>
        /// <param name="line">The serialized datapoint.</param>
        public OrderHandledDatapoint(string line)
        {
            string[] values = line.Split(IOConstants.DELIMITER_VALUE);
            TimeStamp = double.Parse(values[0], IOConstants.FORMATTER);
            OutputStation = int.Parse(values[1]);
            TurnoverTime = double.Parse(values[2], IOConstants.FORMATTER);
            ThroughputTime = double.Parse(values[3], IOConstants.FORMATTER);
            Lateness = double.Parse(values[4], IOConstants.FORMATTER);
        }
        /// <summary>
        /// Creates a line serialization of this datapoint.
        /// </summary>
        /// <returns>The line.</returns>
        public string GetLine()
        {
            return
                TimeStamp.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                OutputStation.ToString() + IOConstants.DELIMITER_VALUE +
                TurnoverTime.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                ThroughputTime.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                Lateness.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER);
        }
        /// <summary>
        /// Creates a header for the line serializations.
        /// </summary>
        /// <returns>The header.</returns>
        public static string GetHeader()
        {
            return
                nameof(TimeStamp) + IOConstants.DELIMITER_VALUE +
                nameof(OutputStation) + IOConstants.DELIMITER_VALUE +
                nameof(TurnoverTime) + IOConstants.DELIMITER_VALUE +
                nameof(ThroughputTime) + IOConstants.DELIMITER_VALUE +
                nameof(Lateness);
        }
    }

    /// <summary>
    /// Constitutes one data-point for the logging of the placement of incoming orders.
    /// </summary>
    public class OrderPlacedDatapoint
    {
        /// <summary>
        /// The time-stamp of this datapoint.
        /// </summary>
        public double TimeStamp;
        /// <summary>
        /// Creates a new datapoint.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        public OrderPlacedDatapoint(double timestamp) { TimeStamp = timestamp; }
        /// <summary>
        /// Creates a new datapoint from a line serialization.
        /// </summary>
        /// <param name="line">The serialized datapoint.</param>
        public OrderPlacedDatapoint(string line) { TimeStamp = double.Parse(line.Trim(), IOConstants.FORMATTER); }
        /// <summary>
        /// Creates a line serialization of this datapoint.
        /// </summary>
        /// <returns>The line.</returns>
        public string GetLine() { return TimeStamp.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER); }
        /// <summary>
        /// Creates a header for the line serializations.
        /// </summary>
        /// <returns>The header.</returns>
        public static string GetHeader() { return nameof(TimeStamp); }
    }

    /// <summary>
    /// Constitutes one data-point for the logging of one collision.
    /// </summary>
    public class CollisionDatapoint
    {
        /// <summary>
        /// The time-stamp of this datapoint.
        /// </summary>
        public double TimeStamp;
        /// <summary>
        /// The ID of the bot that reported the collision.
        /// </summary>
        public int Bot;
        /// <summary>
        /// The tier on which the collision happened.
        /// </summary>
        public int Tier;
        /// <summary>
        /// Creates a new datapoint.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="tier">The tier.</param>
        public CollisionDatapoint(double timestamp, int bot, int tier) { TimeStamp = timestamp; Bot = bot; Tier = tier; }
        /// <summary>
        /// Creates a new datapoint from a line serialization.
        /// </summary>
        /// <param name="line">The serialized datapoint.</param>
        public CollisionDatapoint(string line)
        {
            string[] values = line.Split(IOConstants.DELIMITER_VALUE);
            TimeStamp = double.Parse(values[0], IOConstants.FORMATTER);
            Bot = int.Parse(values[1]);
            Tier = int.Parse(values[2]);
        }
        /// <summary>
        /// Creates a line serialization of this datapoint.
        /// </summary>
        /// <returns>The line.</returns>
        public string GetLine()
        {
            return
                TimeStamp.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                Bot.ToString() + IOConstants.DELIMITER_VALUE +
                Tier.ToString();
        }
        /// <summary>
        /// Creates a header for the line serializations.
        /// </summary>
        /// <returns>The header.</returns>
        public static string GetHeader()
        {
            return
                nameof(TimeStamp) + IOConstants.DELIMITER_VALUE +
                nameof(Bot) + IOConstants.DELIMITER_VALUE +
                nameof(Tier);
        }
    }

    /// <summary>
    /// Constitutes one data-point for the distance logging.
    /// </summary>
    public class DistanceDatapoint
    {
        /// <summary>
        /// The timestamp of this snapshot.
        /// </summary>
        public double TimeStamp;
        /// <summary>
        /// The distance traveled between this timestamp and the last one.
        /// </summary>
        public double DistanceTraveled;
        /// <summary>
        /// Creates a new datapoint.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="distanceTraveled">The distance.</param>
        public DistanceDatapoint(double timestamp, double distanceTraveled) { TimeStamp = timestamp; DistanceTraveled = distanceTraveled; }
        /// <summary>
        /// Creates a new datapoint from a line serialization.
        /// </summary>
        /// <param name="line">The serialized datapoint.</param>
        public DistanceDatapoint(string line)
        {
            string[] values = line.Split(IOConstants.DELIMITER_VALUE);
            TimeStamp = double.Parse(values[0], IOConstants.FORMATTER);
            DistanceTraveled = double.Parse(values[1], IOConstants.FORMATTER);
        }
        /// <summary>
        /// Creates a line serialization of this datapoint.
        /// </summary>
        /// <returns>The line.</returns>
        public string GetLine()
        {
            return
                TimeStamp.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                DistanceTraveled.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER);
        }
        /// <summary>
        /// Creates a header for the line serializations.
        /// </summary>
        /// <returns>The header.</returns>
        public static string GetHeader()
        {
            return
                nameof(TimeStamp) + IOConstants.DELIMITER_VALUE +
                nameof(DistanceTraveled);
        }
    }

    /// <summary>
    /// Constitutes one data-point storing bot information.
    /// </summary>
    public class BotDatapoint
    {
        /// <summary>
        /// The timestamp of this snapshot.
        /// </summary>
        public double TimeStamp;
        /// <summary>
        /// The number of bots currently queueing.
        /// </summary>
        public double BotsQueueing;
        /// <summary>
        /// The number of bots currently engaging in the given tasks.
        /// </summary>
        public Tuple<string, int>[] TaskBotCounts;
        /// <summary>
        /// Creates a new datapoint.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="botsQueueing">The number of bots currently queueing.</param>
        /// <param name="taskBotCounts">The number of bots per their current tasks.</param>
        public BotDatapoint(double timestamp, double botsQueueing, Tuple<string, int>[] taskBotCounts) { TimeStamp = timestamp; BotsQueueing = botsQueueing; TaskBotCounts = taskBotCounts; }
        /// <summary>
        /// Creates a new datapoint from a line serialization.
        /// </summary>
        /// <param name="line">The serialized datapoint.</param>
        public BotDatapoint(string line)
        {
            string[] values = line.Split(IOConstants.DELIMITER_VALUE);
            TimeStamp = double.Parse(values[0], IOConstants.FORMATTER);
            BotsQueueing = double.Parse(values[1], IOConstants.FORMATTER);
            TaskBotCounts = values[2].Split(IOConstants.DELIMITER_LIST).Select(e => e.Split(IOConstants.DELIMITER_TUPLE)).Select(e => new Tuple<string, int>(e[0], int.Parse(e[1], IOConstants.FORMATTER))).ToArray();
        }
        /// <summary>
        /// Creates a line serialization of this datapoint.
        /// </summary>
        /// <returns>The line.</returns>
        public string GetLine()
        {
            return
                TimeStamp.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                BotsQueueing.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                string.Join(IOConstants.DELIMITER_LIST.ToString(), TaskBotCounts.Select(t =>
                    t.Item1 + IOConstants.DELIMITER_TUPLE.ToString() +
                    t.Item2.ToString(IOConstants.FORMATTER)));
        }
        /// <summary>
        /// Creates a header for the line serializations.
        /// </summary>
        /// <returns>The header.</returns>
        public static string GetHeader()
        {
            return
                nameof(TimeStamp) + IOConstants.DELIMITER_VALUE +
                nameof(BotsQueueing) + IOConstants.DELIMITER_VALUE +
                nameof(TaskBotCounts);
        }
    }

    /// <summary>
    /// Constitutes one data-point storing path planning information immediately derived from the path planner.
    /// </summary>
    public class PathFindingDatapoint
    {
        /// <summary>
        /// The time stamp of this snapshot.
        /// </summary>
        public double TimeStamp;
        /// <summary>
        /// The runtime of this snapshot.
        /// </summary>
        public double Runtime;
        /// <summary>
        /// The x-value of this location snapshot.
        /// </summary>
        public int Requests;
        /// <summary>
        /// Creates a new datapoint.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="runTime">The runtime.</param>
        /// <param name="requests">The requests.</param>
        public PathFindingDatapoint(double timestamp, double runTime, int requests) { TimeStamp = timestamp; Runtime = runTime; Requests = requests; }
        /// <summary>
        /// Creates a new datapoint from a line serialization.
        /// </summary>
        /// <param name="line">The serialized datapoint.</param>
        public PathFindingDatapoint(string line)
        {
            string[] values = line.Split(IOConstants.DELIMITER_VALUE);
            TimeStamp = double.Parse(values[0], IOConstants.FORMATTER);
            Runtime = double.Parse(values[1], IOConstants.FORMATTER);
            Requests = int.Parse(values[2]);
        }
        /// <summary>
        /// Creates a line serialization of this datapoint.
        /// </summary>
        /// <returns>The line.</returns>
        public string GetLine()
        {
            return
                TimeStamp.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                Runtime.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                Requests.ToString();
        }
        /// <summary>
        /// Creates a header for the line serializations.
        /// </summary>
        /// <returns>The header.</returns>
        public static string GetHeader()
        {
            return
                nameof(TimeStamp) + IOConstants.DELIMITER_VALUE +
                nameof(Runtime) + IOConstants.DELIMITER_VALUE +
                nameof(Requests);
        }
    }

    /// <summary>
    /// Stores lightweight information about a completed trip to a station's queue.
    /// </summary>
    public class StationTripDatapoint
    {
        /// <summary>
        /// Distinguishes the different types of logged trips.
        /// </summary>
        public enum StationTripType
        {
            /// <summary>
            /// A trip to an output-station.
            /// </summary>
            O,
            /// <summary>
            /// A trip to an input-station.
            /// </summary>
            I,
        }
        /// <summary>
        /// The time the trip was completed.
        /// </summary>
        public double TimeStamp;
        /// <summary>
        /// The time the trip took.
        /// </summary>
        public double TripTime;
        /// <summary>
        /// The type of the trip.
        /// </summary>
        public StationTripType Type;
        /// <summary>
        /// Creates a new datapoint.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="tripTime">The triptime.</param>
        /// <param name="type">The type of the trip.</param>
        public StationTripDatapoint(double timestamp, double tripTime, StationTripType type) { TimeStamp = timestamp; TripTime = tripTime; Type = type; }
        /// <summary>
        /// Creates a new datapoint from a line serialization.
        /// </summary>
        /// <param name="line">The serialized datapoint.</param>
        public StationTripDatapoint(string line)
        {
            string[] values = line.Split(IOConstants.DELIMITER_VALUE);
            ReflectionTools.ParseStringToFields(this, typeof(StationTripDatapoint), values, IOConstants.FORMATTER);
        }
        /// <summary>
        /// Creates a line serialization of this datapoint.
        /// </summary>
        /// <returns>The line.</returns>
        public string GetLine()
        {
            return string.Join(IOConstants.DELIMITER_VALUE.ToString(), ReflectionTools.ConvertFields(this, typeof(StationTripDatapoint), IOConstants.FORMATTER, IOConstants.EXPORT_FORMAT_SHORTEST_BY_ROUNDING).ToArray());
        }
        /// <summary>
        /// Creates a header for the line serializations.
        /// </summary>
        /// <returns>The header.</returns>
        public static string GetHeader()
        {
            return string.Join(IOConstants.DELIMITER_VALUE.ToString(), ReflectionTools.ConvertFieldsToDescriptions(typeof(StationTripDatapoint)));
        }
    }

    /// <summary>
    /// Constitutes one data-point storing an inventory level snapshot.
    /// </summary>
    public class InventoryLevelDatapoint
    {
        /// <summary>
        /// The time stamp of this snapshot.
        /// </summary>
        public double TimeStamp;
        /// <summary>
        /// The relative level of the inventory.
        /// </summary>
        public double InventoryLevel;
        /// <summary>
        /// The number of SKUs stored in the system.
        /// </summary>
        public int ContainedSKUCount;
        /// <summary>
        /// The total number of inversions (combined score).
        /// </summary>
        public int InvCombinedTotal;
        /// <summary>
        /// The aggregated rank difference of all inversions (combined score).
        /// </summary>
        public int InvCombinedRank;
        /// <summary>
        /// The average rank difference of all inversions (combined score).
        /// </summary>
        public double InvCombinedAvgRank;
        /// <summary>
        /// The total number of inversions (speed score).
        /// </summary>
        public int InvSpeedTotal;
        /// <summary>
        /// The aggregated rank difference of all inversions (speed score).
        /// </summary>
        public int InvSpeedRank;
        /// <summary>
        /// The average rank difference of all inversions (speed score).
        /// </summary>
        public double InvSpeedAvgRank;
        /// <summary>
        /// The total number of inversions (utility score).
        /// </summary>
        public int InvUtilityTotal;
        /// <summary>
        /// The aggregated rank difference of all inversions (utility score).
        /// </summary>
        public int InvUtilityRank;
        /// <summary>
        /// The average rank difference of all inversions (utility score).
        /// </summary>
        public double InvUtilityAvgRank;
        /// <summary>
        /// Creates a new datapoint.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="inventoryLevel">The inventory level.</param>
        /// <param name="containedSKUCount">The number of SKUs stored in the system.</param>
        /// <param name="invCombinedTotal">The total number of inversions (combined score).</param>
        /// <param name="invCombinedRank">The aggregated rank difference of all inversions (combined score).</param>
        /// <param name="invCombinedAvgRank">The average rank difference of all inversions (combined score).</param>
        /// <param name="invSpeedTotal">The total number of inversions (speed score).</param>
        /// <param name="invSpeedRank">The aggregated rank difference of all inversions (speed score).</param>
        /// <param name="invSpeedAvgRank">The average rank difference of all inversions (speed score).</param>
        /// <param name="invUtilityTotal">The total number of inversions (utility score).</param>
        /// <param name="invUtilityRank">The aggregated rank difference of all inversions (utility score).</param>
        /// <param name="invUtilityAvgRank">The average rank difference of all inversions (utility score).</param>
        public InventoryLevelDatapoint(
            double timestamp, double inventoryLevel, int containedSKUCount,
            int invCombinedTotal, int invCombinedRank, double invCombinedAvgRank,
            int invSpeedTotal, int invSpeedRank, double invSpeedAvgRank,
            int invUtilityTotal, int invUtilityRank, double invUtilityAvgRank)
        {
            TimeStamp = timestamp; InventoryLevel = inventoryLevel; ContainedSKUCount = containedSKUCount;
            InvCombinedTotal = invCombinedTotal; InvCombinedRank = invCombinedRank; InvCombinedAvgRank = invCombinedAvgRank;
            InvSpeedTotal = invSpeedTotal; InvSpeedRank = invSpeedRank; InvSpeedAvgRank = invSpeedAvgRank;
            InvUtilityTotal = invUtilityTotal; InvUtilityRank = invUtilityRank; InvUtilityAvgRank = invUtilityAvgRank;
        }
        /// <summary>
        /// Creates a new datapoint from a line serialization.
        /// </summary>
        /// <param name="line">The serialized datapoint.</param>
        public InventoryLevelDatapoint(string line)
        {
            string[] values = line.Split(IOConstants.DELIMITER_VALUE);
            ReflectionTools.ParseStringToFields(this, typeof(InventoryLevelDatapoint), values, IOConstants.FORMATTER);
        }
        /// <summary>
        /// Creates a line serialization of this datapoint.
        /// </summary>
        /// <returns>The line.</returns>
        public string GetLine()
        {
            return string.Join(IOConstants.DELIMITER_VALUE.ToString(), ReflectionTools.ConvertFields(this, typeof(InventoryLevelDatapoint), IOConstants.FORMATTER, IOConstants.EXPORT_FORMAT_SHORTER).ToArray());
        }
        /// <summary>
        /// Creates a header for the line serializations.
        /// </summary>
        /// <returns>The header.</returns>
        public static string GetHeader()
        {
            return string.Join(IOConstants.DELIMITER_VALUE.ToString(), ReflectionTools.ConvertFieldsToDescriptions(typeof(InventoryLevelDatapoint)));
        }
    }

    /// <summary>
    /// Constitutes one data-point storing a current bundle / order meta info snapshot.
    /// </summary>
    public class BundleOrderSituationDatapoint
    {
        /// <summary>
        /// The time stamp of this snapshot.
        /// </summary>
        public double TimeStamp;
        /// <summary>
        /// The number of bundles in backlog.
        /// </summary>
        public int BacklogBundleCount;
        /// <summary>
        /// The number of order in backlog.
        /// </summary>
        public int BacklogOrderCount;
        /// <summary>
        /// The average age of the bundles currently assigned to the stations regarding their throughput time.
        /// </summary>
        public double BundleThroughputAgeAvg;
        /// <summary>
        /// The average age of the orders currently assigned to the stations regarding their throughput time.
        /// </summary>
        public double OrderThroughputAgeAvg;
        /// <summary>
        /// The average age of the bundles currently assigned to the stations regarding their turnover time.
        /// </summary>
        public double BundleTurnoverAgeAvg;
        /// <summary>
        /// The average age of the orders currently assigned to the stations regarding their turnover time.
        /// </summary>
        public double OrderTurnoverAgeAvg;
        /// <summary>
        /// The average frequency of the bundles currently assigned to the stations.
        /// </summary>
        public double BundleFrequencyAvg;
        /// <summary>
        /// The average frequency of all lines of all orders currently assigned to the stations.
        /// </summary>
        public double OrderFrequencyAvg;
        /// <summary>
        /// Creates a new datapoint.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="backlogBundleCount">The number of backlog bundles.</param>
        /// <param name="backlogOrderCount">The number of backlog orders.</param>
        /// <param name="bundleThroughputAge">The average age of the bundles currently assigned to the stations regarding their throughput time.</param>
        /// <param name="orderThroughputAge">The average age of the orders currently assigned to the stations regarding their throughput time.</param>
        /// <param name="bundleTurnoverAge">The average age of the bundles currently assigned to the stations regarding their turnover time</param>
        /// <param name="orderTurnoverAge">The average age of the orders currently assigned to the stations regarding their turnover time</param>
        /// <param name="bundleFrequency">The average frequency of the bundles currently assigned to the stations.</param>
        /// <param name="orderFrequency">The average frequency of all lines of all orders currently assigned to the stations.</param>
        public BundleOrderSituationDatapoint(double timestamp, int backlogBundleCount, int backlogOrderCount, double bundleThroughputAge, double orderThroughputAge, double bundleTurnoverAge, double orderTurnoverAge, double bundleFrequency, double orderFrequency)
        {
            TimeStamp = timestamp;
            BacklogBundleCount = backlogBundleCount; BacklogOrderCount = backlogOrderCount;
            BundleThroughputAgeAvg = bundleThroughputAge; OrderThroughputAgeAvg = orderThroughputAge;
            BundleTurnoverAgeAvg = bundleTurnoverAge; OrderTurnoverAgeAvg = orderTurnoverAge;
            BundleFrequencyAvg = bundleFrequency; OrderFrequencyAvg = orderFrequency;
        }
        /// <summary>
        /// Creates a new datapoint from a line serialization.
        /// </summary>
        /// <param name="line">The serialized datapoint.</param>
        public BundleOrderSituationDatapoint(string line)
        {
            string[] values = line.Split(IOConstants.DELIMITER_VALUE);
            TimeStamp = double.Parse(values[0], IOConstants.FORMATTER);
            BacklogBundleCount = int.Parse(values[1]);
            BacklogOrderCount = int.Parse(values[2]);
            BundleThroughputAgeAvg = double.Parse(values[3], IOConstants.FORMATTER);
            OrderThroughputAgeAvg = double.Parse(values[4], IOConstants.FORMATTER);
            BundleTurnoverAgeAvg = double.Parse(values[5], IOConstants.FORMATTER);
            OrderTurnoverAgeAvg = double.Parse(values[6], IOConstants.FORMATTER);
            BundleFrequencyAvg = double.Parse(values[7], IOConstants.FORMATTER);
            OrderFrequencyAvg = double.Parse(values[8], IOConstants.FORMATTER);
        }
        /// <summary>
        /// Creates a line serialization of this datapoint.
        /// </summary>
        /// <returns>The line.</returns>
        public string GetLine()
        {
            return
                TimeStamp.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                BacklogBundleCount.ToString() + IOConstants.DELIMITER_VALUE +
                BacklogOrderCount.ToString() + IOConstants.DELIMITER_VALUE +
                BundleThroughputAgeAvg.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                OrderThroughputAgeAvg.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                BundleTurnoverAgeAvg.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                OrderTurnoverAgeAvg.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                BundleFrequencyAvg.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                OrderFrequencyAvg.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER);
        }
        /// <summary>
        /// Creates a header for the line serializations.
        /// </summary>
        /// <returns>The header.</returns>
        public static string GetHeader()
        {
            return
                nameof(TimeStamp) + IOConstants.DELIMITER_VALUE +
                nameof(BacklogBundleCount) + IOConstants.DELIMITER_VALUE +
                nameof(BacklogOrderCount) + IOConstants.DELIMITER_VALUE +
                nameof(BundleThroughputAgeAvg) + IOConstants.DELIMITER_VALUE +
                nameof(OrderThroughputAgeAvg) + IOConstants.DELIMITER_VALUE +
                nameof(BundleTurnoverAgeAvg) + IOConstants.DELIMITER_VALUE +
                nameof(OrderTurnoverAgeAvg) + IOConstants.DELIMITER_VALUE +
                nameof(BundleFrequencyAvg) + IOConstants.DELIMITER_VALUE +
                nameof(OrderFrequencyAvg);
        }
    }

    /// <summary>
    /// Constitutes one data-point storing a backlog 
    /// </summary>
    public class StationDataPoint
    {
        /// <summary>
        /// The time stamp of this snapshot.
        /// </summary>
        public double TimeStamp;
        /// <summary>
        /// The number of pods handled at input-stations.
        /// </summary>
        public int PodsHandledAtIStations;
        /// <summary>
        /// The number of pods handled at output-stations.
        /// </summary>
        public int PodsHandledAtOStations;
        /// <summary>
        /// The number of input stations active.
        /// </summary>
        public int ActiveIStations;
        /// <summary>
        /// The number of output stations active.
        /// </summary>
        public int ActiveOStations;
        /// <summary>
        /// The idle-time per input station at this snapshot.
        /// </summary>
        public Tuple<int, double>[] IStationIdleTimes;
        /// <summary>
        /// The idle-time per output station at this snapshot.
        /// </summary>
        public Tuple<int, double>[] OStationIdleTimes;
        /// <summary>
        /// The up-time per input station at this snapshot.
        /// </summary>
        public Tuple<int, double>[] IStationActiveTimes;
        /// <summary>
        /// The up-time per output station at this snapshot.
        /// </summary>
        public Tuple<int, double>[] OStationActiveTimes;
        /// <summary>
        /// The number of requests per input station at this snapshot.
        /// </summary>
        public Tuple<int, int>[] IStationRequests;
        /// <summary>
        /// The number of requests per output station at this snapshot.
        /// </summary>
        public Tuple<int, int>[] OStationRequests;
        /// <summary>
        /// The number of bundles that have not been stored yet per input station at this snapshot.
        /// </summary>
        public Tuple<int, int>[] IStationBundleBacklog;
        /// <summary>
        /// The number of items that have not been picked yet per output station at this snapshot.
        /// </summary>
        public Tuple<int, int>[] OStationItemBacklog;
        /// <summary>
        /// The number of assigned bots per input station at this snapshot.
        /// </summary>
        public Tuple<int, int>[] IStationAssignedBots;
        /// <summary>
        /// The number of assigned bots per output station at this snapshot.
        /// </summary>
        public Tuple<int, int>[] OStationAssignedBots;
        /// <summary>
        /// The number of pods handled per input station at this snapshot.
        /// </summary>
        public Tuple<int, int>[] IStationPodsHandled;
        /// <summary>
        /// The number of pods handled per output station at this snapshot.
        /// </summary>
        public Tuple<int, int>[] OStationPodsHandled;
        /// <summary>
        /// The number of bundles stored per input station at this snapshot.
        /// </summary>
        public Tuple<int, int>[] IStationBundlesStored;
        /// <summary>
        /// The number of items picked per output station at this snapshot.
        /// </summary>
        public Tuple<int, int>[] OStationItemsPicked;
        /// <summary>
        /// Creates a new datapoint.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="podsHandledAtIStations">Pods handled at input stations.</param>
        /// <param name="podsHandledAtOStations">Pods handled at output stations.</param>
        /// <param name="activeIStations">The number of active input-stations.</param>
        /// <param name="activeOStations">The number of active output-stations.</param>
        /// <param name="iStationIdleTimes">The idle-time per input station at this snapshot.</param>
        /// <param name="oStationIdleTimes">The idle-time per output station at this snapshot.</param>
        /// <param name="iStationActiveTimes">The up-time per input station at this snapshot.</param>
        /// <param name="oStationActiveTimes">The up-time per output station at this snapshot.</param>
        /// <param name="iStationRequests">The number of requests per input station at this snapshot.</param>
        /// <param name="oStationRequests">The number of requests per output station at this snapshot.</param>
        /// <param name="iStationBundles">The number of bundles that have not been stored yet per input station at this snapshot.</param>
        /// <param name="oStationItems">The number of items that have not been picked yet per output station at this snapshot.</param>
        /// <param name="iStationBots">The number of assigned bots per input station at this snapshot.</param>
        /// <param name="oStationBots">The number of assigned bots per output station at this snapshot.</param>
        /// <param name="iStationPodsHandled">The number of pods handled per input station at this snapshot.</param>
        /// <param name="oStationPodsHandled">The number of bundles stored per input station at this snapshot.</param>
        /// <param name="iStationBundlesStored">The number of bundles stored per input station at this snapshot.</param>
        /// <param name="oStationItemsPicked">The number of items picked per output station at this snapshot.</param>
        public StationDataPoint(
            double timestamp,
            int podsHandledAtIStations, int podsHandledAtOStations,
            int activeIStations, int activeOStations,
            Tuple<int, double>[] iStationIdleTimes, Tuple<int, double>[] oStationIdleTimes,
            Tuple<int, double>[] iStationActiveTimes, Tuple<int, double>[] oStationActiveTimes,
            Tuple<int, int>[] iStationRequests, Tuple<int, int>[] oStationRequests,
            Tuple<int, int>[] iStationBundles, Tuple<int, int>[] oStationItems,
            Tuple<int, int>[] iStationBots, Tuple<int, int>[] oStationBots,
            Tuple<int, int>[] iStationPodsHandled, Tuple<int, int>[] oStationPodsHandled,
            Tuple<int, int>[] iStationBundlesStored, Tuple<int, int>[] oStationItemsPicked)
        {
            TimeStamp = timestamp;
            PodsHandledAtIStations = podsHandledAtIStations; PodsHandledAtOStations = podsHandledAtOStations;
            ActiveIStations = activeIStations; ActiveOStations = activeOStations;
            IStationIdleTimes = iStationIdleTimes; OStationIdleTimes = oStationIdleTimes;
            IStationActiveTimes = iStationActiveTimes; OStationActiveTimes = oStationActiveTimes;
            IStationRequests = iStationRequests; OStationRequests = oStationRequests;
            IStationBundleBacklog = iStationBundles; OStationItemBacklog = oStationItems;
            IStationAssignedBots = iStationBots; OStationAssignedBots = oStationBots;
            IStationPodsHandled = iStationPodsHandled; OStationPodsHandled = oStationPodsHandled;
            IStationBundlesStored = iStationBundlesStored; OStationItemsPicked = oStationItemsPicked;
        }
        /// <summary>
        /// Creates a new datapoint from a line serialization.
        /// </summary>
        /// <param name="line">The serialized datapoint.</param>
        public StationDataPoint(string line)
        {
            string[] values = line.Split(IOConstants.DELIMITER_VALUE);
            TimeStamp = double.Parse(values[0], IOConstants.FORMATTER);
            PodsHandledAtIStations = int.Parse(values[1]);
            PodsHandledAtOStations = int.Parse(values[2]);
            ActiveIStations = int.Parse(values[3]);
            ActiveOStations = int.Parse(values[4]);
            IStationIdleTimes = values[5].Split(IOConstants.DELIMITER_LIST).Select(e => e.Split(IOConstants.DELIMITER_TUPLE)).Select(e => new Tuple<int, double>(int.Parse(e[0]), double.Parse(e[1], IOConstants.FORMATTER))).ToArray();
            OStationIdleTimes = values[6].Split(IOConstants.DELIMITER_LIST).Select(e => e.Split(IOConstants.DELIMITER_TUPLE)).Select(e => new Tuple<int, double>(int.Parse(e[0]), double.Parse(e[1], IOConstants.FORMATTER))).ToArray();
            IStationActiveTimes = values[7].Split(IOConstants.DELIMITER_LIST).Select(e => e.Split(IOConstants.DELIMITER_TUPLE)).Select(e => new Tuple<int, double>(int.Parse(e[0]), double.Parse(e[1], IOConstants.FORMATTER))).ToArray();
            OStationActiveTimes = values[8].Split(IOConstants.DELIMITER_LIST).Select(e => e.Split(IOConstants.DELIMITER_TUPLE)).Select(e => new Tuple<int, double>(int.Parse(e[0]), double.Parse(e[1], IOConstants.FORMATTER))).ToArray();
            IStationRequests = values[9].Split(IOConstants.DELIMITER_LIST).Select(e => e.Split(IOConstants.DELIMITER_TUPLE)).Select(e => new Tuple<int, int>(int.Parse(e[0]), int.Parse(e[1]))).ToArray();
            OStationRequests = values[10].Split(IOConstants.DELIMITER_LIST).Select(e => e.Split(IOConstants.DELIMITER_TUPLE)).Select(e => new Tuple<int, int>(int.Parse(e[0]), int.Parse(e[1]))).ToArray();
            IStationBundleBacklog = values[11].Split(IOConstants.DELIMITER_LIST).Select(e => e.Split(IOConstants.DELIMITER_TUPLE)).Select(e => new Tuple<int, int>(int.Parse(e[0]), int.Parse(e[1]))).ToArray();
            OStationItemBacklog = values[12].Split(IOConstants.DELIMITER_LIST).Select(e => e.Split(IOConstants.DELIMITER_TUPLE)).Select(e => new Tuple<int, int>(int.Parse(e[0]), int.Parse(e[1]))).ToArray();
            IStationAssignedBots = values[13].Split(IOConstants.DELIMITER_LIST).Select(e => e.Split(IOConstants.DELIMITER_TUPLE)).Select(e => new Tuple<int, int>(int.Parse(e[0]), int.Parse(e[1]))).ToArray();
            OStationAssignedBots = values[14].Split(IOConstants.DELIMITER_LIST).Select(e => e.Split(IOConstants.DELIMITER_TUPLE)).Select(e => new Tuple<int, int>(int.Parse(e[0]), int.Parse(e[1]))).ToArray();
            IStationPodsHandled = values[15].Split(IOConstants.DELIMITER_LIST).Select(e => e.Split(IOConstants.DELIMITER_TUPLE)).Select(e => new Tuple<int, int>(int.Parse(e[0]), int.Parse(e[1]))).ToArray();
            OStationPodsHandled = values[16].Split(IOConstants.DELIMITER_LIST).Select(e => e.Split(IOConstants.DELIMITER_TUPLE)).Select(e => new Tuple<int, int>(int.Parse(e[0]), int.Parse(e[1]))).ToArray();
            IStationBundlesStored = values[17].Split(IOConstants.DELIMITER_LIST).Select(e => e.Split(IOConstants.DELIMITER_TUPLE)).Select(e => new Tuple<int, int>(int.Parse(e[0]), int.Parse(e[1]))).ToArray();
            OStationItemsPicked = values[18].Split(IOConstants.DELIMITER_LIST).Select(e => e.Split(IOConstants.DELIMITER_TUPLE)).Select(e => new Tuple<int, int>(int.Parse(e[0]), int.Parse(e[1]))).ToArray();
        }
        /// <summary>
        /// Creates a line serialization of this datapoint.
        /// </summary>
        /// <returns>The line.</returns>
        public string GetLine()
        {
            return
                TimeStamp.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                PodsHandledAtIStations.ToString() + IOConstants.DELIMITER_VALUE +
                PodsHandledAtOStations.ToString() + IOConstants.DELIMITER_VALUE +
                ActiveIStations.ToString() + IOConstants.DELIMITER_VALUE +
                ActiveOStations.ToString() + IOConstants.DELIMITER_VALUE +
                string.Join(IOConstants.DELIMITER_LIST.ToString(), IStationIdleTimes.Select(t =>
                    t.Item1.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_TUPLE.ToString() +
                    t.Item2.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER))) + IOConstants.DELIMITER_VALUE +
                string.Join(IOConstants.DELIMITER_LIST.ToString(), OStationIdleTimes.Select(t =>
                    t.Item1.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_TUPLE.ToString() +
                    t.Item2.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER))) + IOConstants.DELIMITER_VALUE +
                string.Join(IOConstants.DELIMITER_LIST.ToString(), IStationActiveTimes.Select(t =>
                    t.Item1.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_TUPLE.ToString() +
                    t.Item2.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER))) + IOConstants.DELIMITER_VALUE +
                string.Join(IOConstants.DELIMITER_LIST.ToString(), OStationActiveTimes.Select(t =>
                    t.Item1.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_TUPLE.ToString() +
                    t.Item2.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER))) + IOConstants.DELIMITER_VALUE +
                string.Join(IOConstants.DELIMITER_LIST.ToString(), IStationRequests.Select(t =>
                    t.Item1.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_TUPLE.ToString() +
                    t.Item2.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER))) + IOConstants.DELIMITER_VALUE +
                string.Join(IOConstants.DELIMITER_LIST.ToString(), OStationRequests.Select(t =>
                    t.Item1.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_TUPLE.ToString() +
                    t.Item2.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER))) + IOConstants.DELIMITER_VALUE +
                string.Join(IOConstants.DELIMITER_LIST.ToString(), IStationBundleBacklog.Select(t =>
                    t.Item1.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_TUPLE.ToString() +
                    t.Item2.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER))) + IOConstants.DELIMITER_VALUE +
                string.Join(IOConstants.DELIMITER_LIST.ToString(), OStationItemBacklog.Select(t =>
                    t.Item1.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_TUPLE.ToString() +
                    t.Item2.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER))) + IOConstants.DELIMITER_VALUE +
                string.Join(IOConstants.DELIMITER_LIST.ToString(), IStationAssignedBots.Select(t =>
                    t.Item1.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_TUPLE.ToString() +
                    t.Item2.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER))) + IOConstants.DELIMITER_VALUE +
                string.Join(IOConstants.DELIMITER_LIST.ToString(), OStationAssignedBots.Select(t =>
                    t.Item1.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_TUPLE.ToString() +
                    t.Item2.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER))) + IOConstants.DELIMITER_VALUE +
                string.Join(IOConstants.DELIMITER_LIST.ToString(), IStationPodsHandled.Select(t =>
                    t.Item1.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_TUPLE.ToString() +
                    t.Item2.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER))) + IOConstants.DELIMITER_VALUE +
                string.Join(IOConstants.DELIMITER_LIST.ToString(), OStationPodsHandled.Select(t =>
                    t.Item1.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_TUPLE.ToString() +
                    t.Item2.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER))) + IOConstants.DELIMITER_VALUE +
                string.Join(IOConstants.DELIMITER_LIST.ToString(), IStationBundlesStored.Select(t =>
                    t.Item1.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_TUPLE.ToString() +
                    t.Item2.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER))) + IOConstants.DELIMITER_VALUE +
                string.Join(IOConstants.DELIMITER_LIST.ToString(), OStationItemsPicked.Select(t =>
                    t.Item1.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_TUPLE.ToString() +
                    t.Item2.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER)));
        }
        /// <summary>
        /// Creates a header for the line serializations.
        /// </summary>
        /// <returns>The header.</returns>
        public static string GetHeader()
        {
            return
                nameof(TimeStamp) + IOConstants.DELIMITER_VALUE +
                nameof(PodsHandledAtIStations) + IOConstants.DELIMITER_VALUE +
                nameof(PodsHandledAtOStations) + IOConstants.DELIMITER_VALUE +
                nameof(ActiveIStations) + IOConstants.DELIMITER_VALUE +
                nameof(ActiveOStations) + IOConstants.DELIMITER_VALUE +
                nameof(IStationIdleTimes) + IOConstants.DELIMITER_VALUE +
                nameof(OStationIdleTimes) + IOConstants.DELIMITER_VALUE +
                nameof(IStationActiveTimes) + IOConstants.DELIMITER_VALUE +
                nameof(OStationActiveTimes) + IOConstants.DELIMITER_VALUE +
                nameof(IStationRequests) + IOConstants.DELIMITER_VALUE +
                nameof(OStationRequests) + IOConstants.DELIMITER_VALUE +
                nameof(IStationBundleBacklog) + IOConstants.DELIMITER_VALUE +
                nameof(OStationItemBacklog) + IOConstants.DELIMITER_VALUE +
                nameof(IStationAssignedBots) + IOConstants.DELIMITER_VALUE +
                nameof(OStationAssignedBots) + IOConstants.DELIMITER_VALUE +
                nameof(IStationPodsHandled) + IOConstants.DELIMITER_VALUE +
                nameof(OStationPodsHandled) + IOConstants.DELIMITER_VALUE +
                nameof(IStationBundlesStored) + IOConstants.DELIMITER_VALUE +
                nameof(OStationItemsPicked);
        }
    }

    /// <summary>
    /// Constitutes one data-point storing a performance snapshot.
    /// </summary>
    public class PerformanceDatapoint
    {
        /// <summary>
        /// The time stamp of this snapshot.
        /// </summary>
        public double TimeStamp;
        /// <summary>
        /// The memory used in megabyte.
        /// </summary>
        public double MemoryUsage;
        /// <summary>
        /// The consumed real-time so far in seconds.
        /// </summary>
        public double RealTime;
        /// <summary>
        /// The time spent for the different controllers at this snapshot.
        /// </summary>
        public Tuple<string, double>[] OverallControllerTimes;
        /// <summary>
        /// Creates a new datapoint.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="memoryUsage">The memory used in megabyte</param>
        /// <param name="realTime">The real-time consumed so far in seconds.</param>
        /// <param name="overallControllerTimes">The time spent for the different controllers at this snapshot.</param>
        public PerformanceDatapoint(double timestamp, double memoryUsage, double realTime, Tuple<string, double>[] overallControllerTimes)
        { TimeStamp = timestamp; MemoryUsage = memoryUsage; RealTime = realTime; OverallControllerTimes = overallControllerTimes; }
        /// <summary>
        /// Creates a new datapoint from a line serialization.
        /// </summary>
        /// <param name="line">The serialized datapoint.</param>
        public PerformanceDatapoint(string line)
        {
            string[] values = line.Split(IOConstants.DELIMITER_VALUE);
            TimeStamp = double.Parse(values[0], IOConstants.FORMATTER);
            MemoryUsage = double.Parse(values[1], IOConstants.FORMATTER);
            RealTime = double.Parse(values[2], IOConstants.FORMATTER);
            OverallControllerTimes = values[3].Split(IOConstants.DELIMITER_LIST).Select(e => e.Split(IOConstants.DELIMITER_TUPLE)).Select(e => new Tuple<string, double>(e[0], double.Parse(e[1], IOConstants.FORMATTER))).ToArray();
        }
        /// <summary>
        /// Creates a line serialization of this datapoint.
        /// </summary>
        /// <returns>The line.</returns>
        public string GetLine()
        {
            return
                TimeStamp.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                MemoryUsage.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                RealTime.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                string.Join(IOConstants.DELIMITER_LIST.ToString(), OverallControllerTimes.Select(t =>
                    t.Item1 + IOConstants.DELIMITER_TUPLE.ToString() +
                    t.Item2.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER)));
        }
        /// <summary>
        /// Creates a header for the line serializations.
        /// </summary>
        /// <returns>The header.</returns>
        public static string GetHeader()
        {
            return
                nameof(TimeStamp) + IOConstants.DELIMITER_VALUE +
                nameof(MemoryUsage) + IOConstants.DELIMITER_VALUE +
                nameof(RealTime) + IOConstants.DELIMITER_VALUE +
                nameof(OverallControllerTimes);
        }
    }

    #endregion

    #region Heat related data points

    /// <summary>
    /// Enumerates all available 'core' heat data types.
    /// </summary>
    public enum HeatDataType
    {
        /// <summary>
        /// One data point stores the position of one bot at a certain timepoint.
        /// </summary>
        PolledLocation,
        /// <summary>
        /// One data point stores information about trips of the robots in a time-independent way.
        /// </summary>
        TimeIndependentTripData,
        /// <summary>
        /// One data point stores information about the storage location at the given time.
        /// </summary>
        StorageLocationInfo,
    }
    /// <summary>
    /// The core class for heatmap related datapoints.
    /// </summary>
    public abstract class HeatDatapoint
    {
        /// <summary>
        /// The x-value of this location.
        /// </summary>
        public double X;
        /// <summary>
        /// The y-value of this location.
        /// </summary>
        public double Y;
        /// <summary>
        /// The tier on which the datapoint was measured.
        /// </summary>
        public int Tier;
        /// <summary>
        /// Returns the value associated with this data point.
        /// </summary>
        /// <param name="dataIndex">The index of the data to get from the datapoint.</param>
        /// <returns>The value associated with this data point.</returns>
        public abstract double GetValue(int dataIndex);
    }

    /// <summary>
    /// Constitutes one data-point storing a location snapshot.
    /// </summary>
    public class LocationDatapoint : HeatDatapoint
    {
        /// <summary>
        /// Distinguishes all location polling data-types.
        /// </summary>
        public enum LocationDataType
        {
            /// <summary>
            /// Simply expresses that a robot was seen at the given coordinate to the given time.
            /// </summary>
            Seen,
        }
        /// <summary>
        /// The timestamp of this snapshot.
        /// </summary>
        public double TimeStamp;
        /// <summary>
        /// The state the bot was in at the time of the snapshot.
        /// </summary>
        public BotTaskType BotTask;
        /// <summary>
        /// Returns a header that is used for the corresponding CSV file storing the data.
        /// </summary>
        /// <returns>A CSV header.</returns>
        public static string GetCSVHeader()
        {
            return
                IOConstants.COMMENT_LINE + IOConstants.STAT_HEAT_TAG_START + HeatDataType.PolledLocation.ToString() + IOConstants.STAT_HEAT_TAG_END +
                "TimeStamp" + IOConstants.DELIMITER_VALUE +
                "Tier" + IOConstants.DELIMITER_VALUE +
                "X" + IOConstants.DELIMITER_VALUE +
                "Y" + IOConstants.DELIMITER_VALUE +
                "BotTask";
        }
        /// <summary>
        /// Returns a CSV string identifying this data-point.
        /// </summary>
        /// <returns>A CSV string.</returns>
        public string ToCSV()
        {
            char task = ' ';
            switch (BotTask)
            {
                case BotTaskType.None: task = 'N'; break;
                case BotTaskType.ParkPod: task = 'P'; break;
                case BotTaskType.RepositionPod: task = 'R'; break;
                case BotTaskType.Insert: task = 'I'; break;
                case BotTaskType.Extract: task = 'E'; break;
                case BotTaskType.Rest: task = 'S'; break;
                default: throw new ArgumentException("Unknown bot task type: " + BotTask.ToString());
            }
            return
                TimeStamp.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                Tier.ToString() + IOConstants.DELIMITER_VALUE +
                X.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                Y.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                task.ToString();
        }
        /// <summary>
        /// Parses the data-point from a CSV-file line.
        /// </summary>
        /// <param name="csvLine">The line from the CSV file.</param>
        /// <returns>The data-point read from the line.</returns>
        public static LocationDatapoint FromCSV(string csvLine)
        {
            string[] values = csvLine.Split(IOConstants.DELIMITER_VALUE);
            BotTaskType task = BotTaskType.None;
            switch (values[4][0])
            {
                case 'N': task = BotTaskType.None; break;
                case 'P': task = BotTaskType.ParkPod; break;
                case 'R': task = BotTaskType.RepositionPod; break;
                case 'I': task = BotTaskType.Insert; break;
                case 'E': task = BotTaskType.Extract; break;
                case 'S': task = BotTaskType.Rest; break;
                default: throw new ArgumentException("Unknown bot task type: " + values[4]);
            }
            return new LocationDatapoint()
            {
                TimeStamp = double.Parse(values[0], IOConstants.FORMATTER),
                Tier = int.Parse(values[1]),
                X = double.Parse(values[2], IOConstants.FORMATTER),
                Y = double.Parse(values[3], IOConstants.FORMATTER),
                BotTask = task,
            };
        }
        /// <summary>
        /// Returns the value associated with this data point.
        /// </summary>
        /// <param name="dataIndex">The index of the data to get from the datapoint.</param>
        /// <returns>The value associated with this data point.</returns>
        public override double GetValue(int dataIndex) { /* Value is always one, because exactly one robot was seen at this position at this timepoint */ return 1; }
    }

    /// <summary>
    /// Constitutes one data-point storing information about the visits of a certain location in the system (e.g. a waypoint).
    /// </summary>
    public class TimeIndependentTripDataPoint : HeatDatapoint
    {
        /// <summary>
        /// Distinguishes the different data-types exposed by this datapoint.
        /// </summary>
        public enum TripDataType
        {
            /// <summary>
            /// The number of times this location was visited, i.e. the number of times this location was the start of a trip.
            /// </summary>
            Overall,
            /// <summary>
            /// The number of trips inbound from an output-station.
            /// </summary>
            FromOStation,
            /// <summary>
            /// The number of trips outbound to an output-station.
            /// </summary>
            ToOStation,
            /// <summary>
            /// The number of trips inbound from an input-station.
            /// </summary>
            FromIStation,
            /// <summary>
            /// The number of trips outbound to an input-station.
            /// </summary>
            ToIStation,
        }
        /// <summary>
        /// The number of times this location was visited.
        /// </summary>
        public int Overall;
        /// <summary>
        /// The number of trips inbound from an output-station.
        /// </summary>
        public int FromOStation;
        /// <summary>
        /// The number of trips outbound to an output-station.
        /// </summary>
        public int ToOStation;
        /// <summary>
        /// The number of trips inbound from an input-station.
        /// </summary>
        public int FromIStation;
        /// <summary>
        /// The number of trips outbound to an input-station.
        /// </summary>
        public int ToIStation;
        /// <summary>
        /// Returns a header that is used for the corresponding CSV file storing the data.
        /// </summary>
        /// <returns>A CSV header.</returns>
        public static string GetCSVHeader()
        {
            return
                IOConstants.COMMENT_LINE + IOConstants.STAT_HEAT_TAG_START + HeatDataType.TimeIndependentTripData.ToString() + IOConstants.STAT_HEAT_TAG_END +
                "Tier" + IOConstants.DELIMITER_VALUE +
                "X" + IOConstants.DELIMITER_VALUE +
                "Y" + IOConstants.DELIMITER_VALUE +
                "Overall" + IOConstants.DELIMITER_VALUE +
                "FromOStation" + IOConstants.DELIMITER_VALUE +
                "ToOStation" + IOConstants.DELIMITER_VALUE +
                "FromIStation" + IOConstants.DELIMITER_VALUE +
                "ToIStation";
        }
        /// <summary>
        /// Returns a CSV string identifying this data-point.
        /// </summary>
        /// <returns>A CSV string.</returns>
        public string ToCSV()
        {
            return
                Tier.ToString() + IOConstants.DELIMITER_VALUE +
                X.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                Y.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                Overall.ToString() + IOConstants.DELIMITER_VALUE +
                FromOStation.ToString() + IOConstants.DELIMITER_VALUE +
                ToOStation.ToString() + IOConstants.DELIMITER_VALUE +
                FromIStation.ToString() + IOConstants.DELIMITER_VALUE +
                ToIStation.ToString();
        }
        /// <summary>
        /// Parses the data-point from a CSV-file line.
        /// </summary>
        /// <param name="csvLine">The line from the CSV file.</param>
        /// <returns>The data-point read from the line.</returns>
        public static TimeIndependentTripDataPoint FromCSV(string csvLine)
        {
            string[] values = csvLine.Split(IOConstants.DELIMITER_VALUE);
            return new TimeIndependentTripDataPoint()
            {
                Tier = int.Parse(values[0]),
                X = double.Parse(values[1], IOConstants.FORMATTER),
                Y = double.Parse(values[2], IOConstants.FORMATTER),
                Overall = int.Parse(values[3]),
                FromOStation = int.Parse(values[4]),
                ToOStation = int.Parse(values[5]),
                FromIStation = int.Parse(values[6]),
                ToIStation = int.Parse(values[7]),
            };
        }
        /// <summary>
        /// Returns the value associated with this data point.
        /// </summary>
        /// <param name="dataIndex">The index of the data to get from the datapoint.</param>
        /// <returns>The value associated with this data point.</returns>
        public override double GetValue(int dataIndex)
        {
            TripDataType dataType = Enum.GetValues(typeof(TripDataType)).Cast<TripDataType>().ElementAt(dataIndex);
            switch (dataType)
            {
                case TripDataType.Overall: return Overall;
                case TripDataType.FromOStation: return FromOStation;
                case TripDataType.ToOStation: return ToOStation;
                case TripDataType.FromIStation: return FromIStation;
                case TripDataType.ToIStation: return ToIStation;
                default: throw new ArgumentException("Unknown data-type: " + dataType);
            }
        }
    }

    /// <summary>
    /// Constitutes one data-point storing information about the visits of a certain location in the system (e.g. a waypoint).
    /// </summary>
    public class StorageLocationInfoDatapoint : HeatDatapoint
    {
        /// <summary>
        /// Distinguishes the different data-types exposed by this datapoint.
        /// </summary>
        public enum StorageLocationInfoType
        {
            /// <summary>
            /// The aggregated frequency of the product units contained in the pod stored at the storage location.
            /// </summary>
            Speed,
            /// <summary>
            /// The number of potential picks offered by the pod stored at the storage location.
            /// </summary>
            Utility,
            /// <summary>
            /// The combined speed and utility of a pod stored at the storage location.
            /// </summary>
            Combined,
        }
        /// <summary>
        /// The timestamp of the datapoint.
        /// </summary>
        public double TimeStamp;
        /// <summary>
        /// The aggregated frequency of the product units contained in the pod stored at the storage location.
        /// </summary>
        public double Speed;
        /// <summary>
        /// The number of potential picks offered by the pod stored at the storage location.
        /// </summary>
        public double Utility;
        /// <summary>
        /// The combined speed and utility of a pod stored at the storage location.
        /// </summary>
        public double Combined;
        /// <summary>
        /// Returns a header that is used for the corresponding CSV file storing the data.
        /// </summary>
        /// <returns>A CSV header.</returns>
        public static string GetCSVHeader()
        {
            return
                IOConstants.COMMENT_LINE + IOConstants.STAT_HEAT_TAG_START + HeatDataType.StorageLocationInfo.ToString() + IOConstants.STAT_HEAT_TAG_END +
                "TimeStamp" + IOConstants.DELIMITER_VALUE +
                "Tier" + IOConstants.DELIMITER_VALUE +
                "X" + IOConstants.DELIMITER_VALUE +
                "Y" + IOConstants.DELIMITER_VALUE +
                "Speed" + IOConstants.DELIMITER_VALUE +
                "Utility" + IOConstants.DELIMITER_VALUE +
                "Combined";
        }
        /// <summary>
        /// Returns a CSV string identifying this data-point.
        /// </summary>
        /// <returns>A CSV string.</returns>
        public string ToCSV()
        {
            return
                TimeStamp.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                Tier.ToString() + IOConstants.DELIMITER_VALUE +
                X.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                Y.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                Speed.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                Utility.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                Combined.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER);
        }
        /// <summary>
        /// Parses the data-point from a CSV-file line.
        /// </summary>
        /// <param name="csvLine">The line from the CSV file.</param>
        /// <returns>The data-point read from the line.</returns>
        public static StorageLocationInfoDatapoint FromCSV(string csvLine)
        {
            string[] values = csvLine.Split(IOConstants.DELIMITER_VALUE);
            return new StorageLocationInfoDatapoint()
            {
                TimeStamp = double.Parse(values[0], IOConstants.FORMATTER),
                Tier = int.Parse(values[1]),
                X = double.Parse(values[2], IOConstants.FORMATTER),
                Y = double.Parse(values[3], IOConstants.FORMATTER),
                Speed = double.Parse(values[4], IOConstants.FORMATTER),
                Utility = double.Parse(values[5], IOConstants.FORMATTER),
                Combined = double.Parse(values[6], IOConstants.FORMATTER),
            };
        }
        /// <summary>
        /// Returns the value associated with this data point.
        /// </summary>
        /// <param name="dataIndex">The index of the data to get from the datapoint.</param>
        /// <returns>The value associated with this data point.</returns>
        public override double GetValue(int dataIndex)
        {
            StorageLocationInfoType dataType = Enum.GetValues(typeof(StorageLocationInfoType)).Cast<StorageLocationInfoType>().ElementAt(dataIndex);
            switch (dataType)
            {
                case StorageLocationInfoType.Speed: return Speed;
                case StorageLocationInfoType.Utility: return Utility;
                case StorageLocationInfoType.Combined: return Combined;
                default: throw new ArgumentException("Unknown data-type: " + dataType);
            }
        }
    }

    #endregion
}
