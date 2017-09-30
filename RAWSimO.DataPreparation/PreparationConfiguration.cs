using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.DataPreparation
{
    public class PreparationConfiguration
    {
        public PreparationConfiguration()
        {
            PrintDiagramTitle = true;
            ItemProgressionStepLength = 10;
            ItemProgressionConsolidationTimespan = 60;
            BundleProgressionStepLength = 10;
            BundleProgressionConsolidationTimespan = 60;
            OrderProgressionStepLength = 10;
            OrderProgressionConsolidationTimespan = 60;
            CollisionProgressionStepLength = 10;
            CollisionProgressionConsolidationTimespan = 60;
            CustomDiagrams = new List<CustomDiagramConfiguration>()
            {
                new CustomDiagramConfiguration() {
                    Name = "overview",
                    PlotTime = PlotTime.Hour,
                    AverageWindow = 1200,
                    Plots = new List<List<SinglePlotConfiguration>>() {
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.BundlesPlaced, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.BundlesHandled, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.OrdersPlaced, Axis = PlotAxis.Y2 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.OrdersHandled, Axis = PlotAxis.Y2 },
                        },
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.BundleThroughputTimeAvg, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.BundleTurnoverTimeAvg, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.OrderThroughputTimeAvg, Axis = PlotAxis.Y2 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.OrderTurnoverTimeAvg, Axis = PlotAxis.Y2 }
                        },
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.InventoryLevel, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.DistanceTraveled, Axis = PlotAxis.Y2 }
                        },
                         new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.BundlesHandled, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.OrdersHandled, Axis = PlotAxis.Y1 }
                        },
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.BundlePileon, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.ItemPileon, Axis = PlotAxis.Y1 }
                        },
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.PodsHandledAtIS, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.PodsHandledAtOS, Axis = PlotAxis.Y1 }
                        },
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.BundlesBacklogLevel, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.OrdersBacklogLevel, Axis = PlotAxis.Y2 },
                        },
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.OrderLatenessAvg, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.LateOrderFractional, Axis = PlotAxis.Y2 },
                        },
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.BundleThroughputAgeAverage, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.BundleTurnoverAgeAverage, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.OrderThroughputAgeAverage, Axis = PlotAxis.Y2 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.OrderTurnoverAgeAverage, Axis = PlotAxis.Y2 },
                        },
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.SKUCountContained, Axis = PlotAxis.Y2 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.BundleFrequencyAverage, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.OrderFrequencyAverage, Axis = PlotAxis.Y1 },
                        },
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.InvCombinedTotal, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.InvSpeedTotal, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.InvUtilityTotal, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.InvCombinedAvgRank, Axis = PlotAxis.Y2 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.InvSpeedAvgRank, Axis = PlotAxis.Y2 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.InvUtilityAvgRank, Axis = PlotAxis.Y2 },
                        },
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.IStationIdleTime, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.OStationIdleTime, Axis = PlotAxis.Y1 },
                        },
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.IStationActiveTime, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.OStationActiveTime, Axis = PlotAxis.Y1 },
                        },
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.IStationOpenWork, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.IStationRequests, Axis = PlotAxis.Y1 },
                        },
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.OStationOpenWork, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.OStationRequests, Axis = PlotAxis.Y1 },
                        },
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.IStationBots, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.OStationBots, Axis = PlotAxis.Y1 },
                        },
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.BotsQueueing, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.TaskBotCounts, Axis = PlotAxis.Y2 },
                        },
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.LastMileTripTimeOStation, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.LastMileTripTimeIStation, Axis = PlotAxis.Y1 },
                        },
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.IStationBundlesStored, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.OStationItemsPicked, Axis = PlotAxis.Y2 }
                        },
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.IStationBundlePileon, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.OStationItemPileon, Axis = PlotAxis.Y2 }
                        },
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.IStationsActive, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.OStationsActive, Axis = PlotAxis.Y1 },
                        },
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.ControllerRuntimes, Axis = PlotAxis.Y1 },
                        },
                        new List<SinglePlotConfiguration>() {
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.RealTime, Axis = PlotAxis.Y1 },
                            new SinglePlotConfiguration() { DataType = PlotDataContentType.MemoryUsage, Axis = PlotAxis.Y2 }
                        },
                    }
                },
            };
            Logger = (string message) => { Console.WriteLine(message); };
            DegreeOfParallelism = 8;
        }

        /// <summary>
        /// The log action to use.
        /// </summary>
        public Action<string> Logger { get; set; }
        /// <summary>
        /// The degree of parallelism that is allowed while generating.
        /// </summary>
        public int DegreeOfParallelism { get; set; }
        /// <summary>
        /// Indicates whether to print a title on each diagram.
        /// </summary>
        public bool PrintDiagramTitle { get; set; }
        /// <summary>
        /// The step length when preparing the item progression (in simulation time units).
        /// </summary>
        public double ItemProgressionStepLength { get; set; }
        /// <summary>
        /// The timespan when consolidating the item progression entries (in simulation time units).
        /// </summary>
        public double ItemProgressionConsolidationTimespan { get; set; }
        /// <summary>
        /// The step length when preparing the bundle progression (in simulation time units).
        /// </summary>
        public double BundleProgressionStepLength { get; set; }
        /// <summary>
        /// The timespan when consolidating the bundle progression entries (in simulation time units).
        /// </summary>
        public double BundleProgressionConsolidationTimespan { get; set; }
        /// <summary>
        /// The step length when preparing the order progression (in simulation time units).
        /// </summary>
        public double OrderProgressionStepLength { get; set; }
        /// <summary>
        /// The timespan when consolidating the order progression entries (in simulation time units).
        /// </summary>
        public double OrderProgressionConsolidationTimespan { get; set; }
        /// <summary>
        /// The step length when preparing the order progression (in simulation time units).
        /// </summary>
        public double CollisionProgressionStepLength { get; set; }
        /// <summary>
        /// The timespan when consolidating the order progression entries (in simulation time units).
        /// </summary>
        public double CollisionProgressionConsolidationTimespan { get; set; }
        /// <summary>
        /// List of custom diagrams that have to be prepared.
        /// </summary>
        public List<CustomDiagramConfiguration> CustomDiagrams { get; set; }
    }

    /// <summary>
    /// Defines data content types. This is used to build customized diagrams.
    /// </summary>
    public enum PlotDataContentType
    {
        /// <summary>
        /// Depicts data corresponding to the number of orders that were handled in a given time horizon.
        /// </summary>
        OrdersHandled,
        /// <summary>
        /// Depicts data corresponding to the number of bundles that were handled in a given time horizon.
        /// </summary>
        BundlesHandled,
        /// <summary>
        /// Depicts data corresponding to the number of items that were handled in a given time horizon.
        /// </summary>
        ItemsHandled,
        /// <summary>
        /// Depicts data corresponding to the number of orders that were placed in a given time horizon.
        /// </summary>
        OrdersPlaced,
        /// <summary>
        /// Depicts data corresponding to the number of bundles that were placed in a given time horizon.
        /// </summary>
        BundlesPlaced,
        /// <summary>
        /// Depicts data corresponding to the average throughput time of bundles in a given time horizon.
        /// </summary>
        BundleThroughputTimeAvg,
        /// <summary>
        /// Depicts data corresponding to the average turnover time of bundles in a given time horizon.
        /// </summary>
        BundleTurnoverTimeAvg,
        /// <summary>
        /// Depicts data corresponding to the average throughput time of orders in a given time horizon.
        /// </summary>
        OrderThroughputTimeAvg,
        /// <summary>
        /// Depicts data corresponding to the average turnover time of orders in a given time horizon.
        /// </summary>
        OrderTurnoverTimeAvg,
        /// <summary>
        /// Depicts data corresponding to the average lateness of orders not completed in time in a given horizon.
        /// </summary>
        OrderLatenessAvg,
        /// <summary>
        /// Depicts data corresponding to the fractional amount of orders not completed vs. the ones completed in time in a given horizon.
        /// </summary>
        LateOrderFractional,
        /// <summary>
        /// Depicts data corresponding to the number of orders that were in backlock in a given time horizon.
        /// </summary>
        OrdersBacklogLevel,
        /// <summary>
        /// Depicts data corresponding to the number of bundles that were in backlock in a given time horizon.
        /// </summary>
        BundlesBacklogLevel,
        /// <summary>
        /// Depicts data corresponding to the average throughput age of orders in a given time horizon.
        /// </summary>
        OrderThroughputAgeAverage,
        /// <summary>
        /// Depicts data corresponding to the average throughput age of bundles in a given time horizon.
        /// </summary>
        BundleThroughputAgeAverage,
        /// <summary>
        /// Depicts data corresponding to the average turnover age of orders in a given time horizon.
        /// </summary>
        OrderTurnoverAgeAverage,
        /// <summary>
        /// Depicts data corresponding to the average turnover age of bundles in a given time horizon.
        /// </summary>
        BundleTurnoverAgeAverage,
        /// <summary>
        /// The average frequency of the orders currently assigned to the stations in a given time horizon.
        /// </summary>
        OrderFrequencyAverage,
        /// <summary>
        /// The average frequency of the bundles currently assigned to the stations in a given time horizon.
        /// </summary>
        BundleFrequencyAverage,
        /// <summary>
        /// Depicts data corresponding to the number of pods handled at input stations in a given time horizon.
        /// </summary>
        PodsHandledAtIS,
        /// <summary>
        /// Depicts data corresponding to the number of pods handled at output stations in a given time horizon.
        /// </summary>
        PodsHandledAtOS,
        /// <summary>
        /// Depicts data corresponding to the bundle pile-on at input stations in a given time horizon.
        /// </summary>
        BundlePileon,
        /// <summary>
        /// Depicts data corresponding to the item pile-on at output stations in a given time horizon.
        /// </summary>
        ItemPileon,
        /// <summary>
        /// Depicts data corresponding to the idle time of the input stations in a given time horizon.
        /// </summary>
        IStationIdleTime,
        /// <summary>
        /// Depicts data corresponding to the idle time of the output stations in a given time horizon.
        /// </summary>
        OStationIdleTime,
        /// <summary>
        /// Depicts data corresponding to the active time of the input stations in a given time horizon.
        /// </summary>
        IStationActiveTime,
        /// <summary>
        /// Depicts data corresponding to the active time of the output stations in a given time horizon.
        /// </summary>
        OStationActiveTime,
        /// <summary>
        /// Depicts data corresponding to the average requests of the input stations in a given time horizon.
        /// </summary>
        IStationRequests,
        /// <summary>
        /// Depicts data corresponding to the average requests of the output stations in a given time horizon.
        /// </summary>
        OStationRequests,
        /// <summary>
        /// Depicts data corresponding to the average bundles not yet stored at the respective input stations in a given time horizon.
        /// </summary>
        IStationOpenWork,
        /// <summary>
        /// Depicts data corresponding to the average items not yet picked at the respective input stations in a given time horizon.
        /// </summary>
        OStationOpenWork,
        /// <summary>
        /// Depicts data corresponding to the number of bots working for the respective input stations in a given time horizon.
        /// </summary>
        IStationBots,
        /// <summary>
        /// Depicts data corresponding to the number of bots working for the respective output stations in a given time horizon.
        /// </summary>
        OStationBots,
        /// <summary>
        /// Depicts data corresponding to the number of bundles stored for the respective input stations in a given time horizon.
        /// </summary>
        IStationBundlesStored,
        /// <summary>
        /// Depicts data corresponding to the number of items picked for the respective input stations in a given time horizon.
        /// </summary>
        OStationItemsPicked,
        /// <summary>
        /// Depicts data corresponding to the bundle pile-on for the respective input stations in a given time horizon.
        /// </summary>
        IStationBundlePileon,
        /// <summary>
        /// Depicts data corresponding to the item pile-on for the respective input stations in a given time horizon.
        /// </summary>
        OStationItemPileon,
        /// <summary>
        /// Depicts data corresponding to the number of active input stations in a given time horizon.
        /// </summary>
        IStationsActive,
        /// <summary>
        /// Depicts data corresponding to the number of active output stations in a given time horizon.
        /// </summary>
        OStationsActive,
        /// <summary>
        /// Depicts data corresponding to the distance that the bots traveled in a given time horizon.
        /// </summary>
        DistanceTraveled,
        /// <summary>
        /// Depicts data corresponding to the average time for completing the last part of a trip until reaching the destination output station.
        /// </summary>
        LastMileTripTimeOStation,
        /// <summary>
        /// Depicts data corresponding to the average time for completing the last part of a trip until reaching the destination input station.
        /// </summary>
        LastMileTripTimeIStation,
        /// <summary>
        /// The number of bots queueing.
        /// </summary>
        BotsQueueing,
        /// <summary>
        /// The number of bots engaging in the respective tasks.
        /// </summary>
        TaskBotCounts,
        /// <summary>
        /// Depicts data corresponding to the inventory level.
        /// </summary>
        InventoryLevel,
        /// <summary>
        /// Depicts data corresponding to the number of SKUs contained.
        /// </summary>
        SKUCountContained,
        /// <summary>
        /// The total number of inversions (combined score).
        /// </summary>
        InvCombinedTotal,
        /// <summary>
        /// The aggregated rank difference of all inversions (combined score).
        /// </summary>
        InvCombinedRank,
        /// <summary>
        /// The average rank difference of all inversions (combined score).
        /// </summary>
        InvCombinedAvgRank,
        /// <summary>
        /// The total number of inversions (speed score).
        /// </summary>
        InvSpeedTotal,
        /// <summary>
        /// The aggregated rank difference of all inversions (speed score).
        /// </summary>
        InvSpeedRank,
        /// <summary>
        /// The average rank difference of all inversions (speed score).
        /// </summary>
        InvSpeedAvgRank,
        /// <summary>
        /// The total number of inversions (utility score).
        /// </summary>
        InvUtilityTotal,
        /// <summary>
        /// The aggregated rank difference of all inversions (utility score).
        /// </summary>
        InvUtilityRank,
        /// <summary>
        /// The average rank difference of all inversions (utility score).
        /// </summary>
        InvUtilityAvgRank,
        /// <summary>
        /// Depicts data corresponding to the memory consumed.
        /// </summary>
        MemoryUsage,
        /// <summary>
        /// Depicts data corresponding to the real-time consumed.
        /// </summary>
        RealTime,
        /// <summary>
        /// The runtimes spent by the different methods.
        /// </summary>
        ControllerRuntimes,
    }

    /// <summary>
    /// Defines the time-unit to use for the custom plot.
    /// </summary>
    public enum PlotTime
    {
        /// <summary>
        /// Use minutes as the time unit.
        /// </summary>
        Minute,
        /// <summary>
        /// Use hours as the time unit.
        /// </summary>
        Hour,
        /// <summary>
        /// Use days as the time unit.
        /// </summary>
        Day
    }

    /// <summary>
    /// Indicates which axis the data shall be plot to.
    /// </summary>
    public enum PlotAxis
    {
        /// <summary>
        /// The left y-axis (x1y1 in gnuplot).
        /// </summary>
        Y1,
        /// <summary>
        /// The right y-axis (x1y2 in gnuplot).
        /// </summary>
        Y2
    }
    /// <summary>
    /// Defines a custom diagram.
    /// </summary>
    public class CustomDiagramConfiguration
    {
        public CustomDiagramConfiguration()
        {
            Name = "customplot";
            PlotTime = PlotTime.Hour;
            PrintDiagramTitle = true;
            Plots = new List<List<SinglePlotConfiguration>>();
        }
        /// <summary>
        /// The name of this custom diagram.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The time-unit to use.
        /// </summary>
        public PlotTime PlotTime { get; set; }
        /// <summary>
        /// The time-window to average values by.
        /// </summary>
        public double AverageWindow { get; set; }
        /// <summary>
        /// Indicates whether to print the diagram title or not.
        /// </summary>
        public bool PrintDiagramTitle { get; set; }
        /// <summary>
        /// All plots that this diagram is composed of.
        /// </summary>
        public List<List<SinglePlotConfiguration>> Plots { get; set; }
    }
    public class GenericProgressionDatapoint
    {
        public double Timestamp;
        public double[] Values;
        public double[] SupportValues;
    }
    /// <summary>
    /// Defines a single plot of a custom diagram.
    /// </summary>
    public class SinglePlotConfiguration
    {
        /// <summary>
        /// Descriptions for all known data-types.
        /// </summary>
        private static readonly Dictionary<PlotDataContentType, string> _descriptions = new Dictionary<PlotDataContentType, string>()
        {
            { PlotDataContentType.BundlesHandled, "Bundles handled" },
            { PlotDataContentType.OrdersHandled, "Orders handled" },
            { PlotDataContentType.ItemsHandled, "Items handled" },
            { PlotDataContentType.BundlesPlaced, "Bundles placed" },
            { PlotDataContentType.OrdersPlaced, "Orders placed" },
            { PlotDataContentType.BundleThroughputTimeAvg, "Bundle throughput time" },
            { PlotDataContentType.BundleTurnoverTimeAvg, "Bundle turnover time" },
            { PlotDataContentType.OrderThroughputTimeAvg, "Order throughput time" },
            { PlotDataContentType.OrderTurnoverTimeAvg, "Order turnover time" },
            { PlotDataContentType.OrderLatenessAvg, "Order lateness" },
            { PlotDataContentType.LateOrderFractional, "Late orders" },
            { PlotDataContentType.DistanceTraveled, "Distance traveled" },
            { PlotDataContentType.LastMileTripTimeOStation, "Output-station trip time" },
            { PlotDataContentType.LastMileTripTimeIStation, "Input-station trip time" },
            { PlotDataContentType.BotsQueueing, "Bots queueing" },
            { PlotDataContentType.TaskBotCounts, "Bots per " },
            { PlotDataContentType.InventoryLevel, "Inventory level" },
            { PlotDataContentType.SKUCountContained, "SKU count" },
            { PlotDataContentType.InvCombinedTotal, "Inversion count (combined)" },
            { PlotDataContentType.InvCombinedRank, "Inversion rank diff. (combined)" },
            { PlotDataContentType.InvCombinedAvgRank, "Avg. inversion rank diff. (combined)" },
            { PlotDataContentType.InvSpeedTotal, "Inversion count (speed)" },
            { PlotDataContentType.InvSpeedRank, "Inversion rank diff. (speed)" },
            { PlotDataContentType.InvSpeedAvgRank, "Avg. inversion rank diff. (speed)" },
            { PlotDataContentType.InvUtilityTotal, "Inversion count (utility)" },
            { PlotDataContentType.InvUtilityRank, "Inversion rank diff. (utility)" },
            { PlotDataContentType.InvUtilityAvgRank, "Avg. inversion rank diff. (utility)" },
            { PlotDataContentType.MemoryUsage, "Memory used" },
            { PlotDataContentType.RealTime, "Real time" },
            { PlotDataContentType.ControllerRuntimes, "Time consumed by " },
            { PlotDataContentType.BundlesBacklogLevel, "Bundles backlog level" },
            { PlotDataContentType.OrdersBacklogLevel, "Orders backlog level" },
            { PlotDataContentType.BundleThroughputAgeAverage, "Bundle age (throughput time)" },
            { PlotDataContentType.OrderThroughputAgeAverage, "Order age (throughput time)" },
            { PlotDataContentType.BundleTurnoverAgeAverage, "Bundle age (turnover time)" },
            { PlotDataContentType.OrderTurnoverAgeAverage, "Order age (turnover time)" },
            { PlotDataContentType.BundleFrequencyAverage, "Bundle frequency (avg.)" },
            { PlotDataContentType.OrderFrequencyAverage, "Order frequency (avg.)" },
            { PlotDataContentType.PodsHandledAtIS, "Pods handled at input-stations" },
            { PlotDataContentType.PodsHandledAtOS, "Pods handled at output-stations" },
            { PlotDataContentType.BundlePileon, "Bundle pile-on" },
            { PlotDataContentType.ItemPileon, "Item pile-on" },
            { PlotDataContentType.IStationIdleTime, "Idle-time of input" },
            { PlotDataContentType.OStationIdleTime, "Idle-time of output" },
            { PlotDataContentType.IStationActiveTime, "Up-time of input" },
            { PlotDataContentType.OStationActiveTime, "Up-time of output" },
            { PlotDataContentType.IStationRequests, "Open requests of input" },
            { PlotDataContentType.OStationRequests, "Open requests of output" },
            { PlotDataContentType.IStationOpenWork, "Open bundles of input" },
            { PlotDataContentType.OStationOpenWork, "Open items of output" },
            { PlotDataContentType.IStationBots, "Bots of input" },
            { PlotDataContentType.OStationBots, "Bots of output" },
            { PlotDataContentType.IStationBundlesStored, "Bundles stored at input" },
            { PlotDataContentType.OStationItemsPicked, "Items picked at output" },
            { PlotDataContentType.IStationBundlePileon, "Bundle pile-on of input" },
            { PlotDataContentType.OStationItemPileon, "Item pile-on of output" },
            { PlotDataContentType.IStationsActive, "Active input stations" },
            { PlotDataContentType.OStationsActive, "Active output stations" },
        };
        /// <summary>
        /// Unit descriptions for all known data-types.
        /// </summary>
        private static readonly Dictionary<PlotDataContentType, string> _unitDescriptions = new Dictionary<PlotDataContentType, string>()
        {
            { PlotDataContentType.BundlesHandled, "Count (per hour)" },
            { PlotDataContentType.OrdersHandled, "Count (per hour)" },
            { PlotDataContentType.ItemsHandled, "Count (per hour)" },
            { PlotDataContentType.BundlesPlaced, "Count (per hour)" },
            { PlotDataContentType.OrdersPlaced, "Count (per hour)" },
            { PlotDataContentType.BundleThroughputTimeAvg, "Time (avg, in seconds)" },
            { PlotDataContentType.BundleTurnoverTimeAvg, "Time (avg, in seconds)" },
            { PlotDataContentType.OrderThroughputTimeAvg, "Time (avg, in seconds)" },
            { PlotDataContentType.OrderTurnoverTimeAvg, "Time (avg, in seconds)" },
            { PlotDataContentType.OrderLatenessAvg, "Time (avg, in seconds)" },
            { PlotDataContentType.LateOrderFractional, "Fraction (%)" },
            { PlotDataContentType.DistanceTraveled, "Distance (meter)" },
            { PlotDataContentType.LastMileTripTimeOStation, "Time (in seconds)" },
            { PlotDataContentType.LastMileTripTimeIStation, "Time (in seconds)" },
            { PlotDataContentType.BotsQueueing, "Count" },
            { PlotDataContentType.TaskBotCounts, "Count" },
            { PlotDataContentType.InventoryLevel, "Inventory fill (%)" },
            { PlotDataContentType.SKUCountContained, "Count" },
            { PlotDataContentType.InvCombinedTotal, "Count" },
            { PlotDataContentType.InvCombinedRank, "Rank difference" },
            { PlotDataContentType.InvCombinedAvgRank, "Avg. rank difference" },
            { PlotDataContentType.InvSpeedTotal, "Count" },
            { PlotDataContentType.InvSpeedRank, "Rank difference" },
            { PlotDataContentType.InvSpeedAvgRank, "Avg. rank difference" },
            { PlotDataContentType.InvUtilityTotal, "Count" },
            { PlotDataContentType.InvUtilityRank, "Rank difference" },
            { PlotDataContentType.InvUtilityAvgRank, "Avg. rank difference" },
            { PlotDataContentType.MemoryUsage, "Memory (megabyte)" },
            { PlotDataContentType.RealTime, "Time consumed (minutes)" },
            { PlotDataContentType.ControllerRuntimes, "Time consumed (seconds)" },
            { PlotDataContentType.BundlesBacklogLevel, "Average count" },
            { PlotDataContentType.OrdersBacklogLevel, "Average count" },
            { PlotDataContentType.BundleThroughputAgeAverage, "Average age (in seconds)" },
            { PlotDataContentType.OrderThroughputAgeAverage, "Average age (in seconds)" },
            { PlotDataContentType.BundleTurnoverAgeAverage, "Average age (in seconds)" },
            { PlotDataContentType.OrderTurnoverAgeAverage, "Average age (in seconds)" },
            { PlotDataContentType.BundleFrequencyAverage, "Average SKU frequency" },
            { PlotDataContentType.OrderFrequencyAverage, "Average SKU frequency" },
            { PlotDataContentType.PodsHandledAtIS, "Pod count (per hour)" },
            { PlotDataContentType.PodsHandledAtOS, "Pod count (per hour)" },
            { PlotDataContentType.BundlePileon, "pile-on value" },
            { PlotDataContentType.ItemPileon, "pile-on value" },
            { PlotDataContentType.IStationIdleTime, "Idle time (in %)" },
            { PlotDataContentType.OStationIdleTime, "Idle time (in %)" },
            { PlotDataContentType.IStationActiveTime, "Up-time (in %)" },
            { PlotDataContentType.OStationActiveTime, "Up-time (in %)" },
            { PlotDataContentType.IStationRequests, "Count" },
            { PlotDataContentType.OStationRequests, "Count" },
            { PlotDataContentType.IStationOpenWork, "Count" },
            { PlotDataContentType.OStationOpenWork, "Count" },
            { PlotDataContentType.IStationBots, "Count" },
            { PlotDataContentType.OStationBots, "Count" },
            { PlotDataContentType.IStationBundlesStored, "Bundles (per time-unit)" },
            { PlotDataContentType.OStationItemsPicked, "Items (per time-unit)" },
            { PlotDataContentType.IStationBundlePileon, "Bundle pile-on" },
            { PlotDataContentType.OStationItemPileon, "Item pile-on" },
            { PlotDataContentType.IStationsActive, "Count" },
            { PlotDataContentType.OStationsActive, "Count" },
        };
        /// <summary>
        /// The data to plot.
        /// </summary>
        public PlotDataContentType DataType { get; set; }
        /// <summary>
        /// The axis to plot the data to.
        /// </summary>
        public PlotAxis Axis { get; set; }
        /// <summary>
        /// Returns the description corresponding to the given data type.
        /// </summary>
        /// <returns>A description that can be used.</returns>
        public string GetDescription() { return _descriptions[DataType]; }
        /// <summary>
        /// Returns the description corresponding to the given data type.
        /// </summary>
        /// <returns>A description that can be used, e.g. for an y-axis.</returns>
        public string GetUnitDescription() { return _unitDescriptions[DataType]; }
    }
}
