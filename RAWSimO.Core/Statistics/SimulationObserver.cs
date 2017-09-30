using RAWSimO.Core.Control;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.IO;
using RAWSimO.Core.Items;
using RAWSimO.Core.Metrics;
using RAWSimO.Core.Waypoints;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Statistics
{
    /// <summary>
    /// Offers the observation of different statistics.
    /// </summary>
    public class SimulationObserver : IUpdateable
    {
        /// <summary>
        /// States the difference in simulation time between two datapoints stored by this observer.
        /// </summary>
        public const double STEP_LENGTH_DISTANCE_TRAVELED = 60;
        /// <summary>
        /// States the difference in simulation time between two position polls.
        /// </summary>
        public const double STEP_LENGTH_POSITION_POLL = 20;
        /// <summary>
        /// States the difference in simulation time between two position polls when in intense measuring mode.
        /// </summary>
        public const double STEP_LENGTH_POSITION_POLL_INTENSE = 2;
        /// <summary>
        /// States the difference in simulation time between two pod info polls.
        /// </summary>
        public const double STEP_LENGTH_STORAGE_LOCATION_INFO_POLL = 60 * 10;
        /// <summary>
        /// States the difference in simulation time between two memory polls.
        /// </summary>
        public const double STEP_LENGTH_MEMORY_POLL = 60;
        /// <summary>
        /// States the difference in simulation time between two bot info polls.
        /// </summary>
        public const double STEP_LENGTH_BOT_POLL = 60;
        /// <summary>
        /// States the difference in simulation time between two inventory level polls.
        /// </summary>
        public const double STEP_LENGTH_INVENTORY_POLL = 60 * 10;
        /// <summary>
        /// States the difference in simulation time between two bundle / order situation polls.
        /// </summary>
        public const double STEP_LENGTH_BUNDLE_ORDER_SITUATION_POLL = 60;
        /// <summary>
        /// States the difference in simulation time between two pod handling polls.
        /// </summary>
        public const double STEP_LENGTH_POD_HANDLING_POLL = 60;
        /// <summary>
        /// States the difference in simulation time between two well sortedness polls.
        /// </summary>
        public const double STEP_LENGTH_WELL_SORTEDNESS_POLL = 60 * 20;

        /// <summary>
        /// Creates a new object of this instance.
        /// </summary>
        /// <param name="instance">The instance that is observed.</param>
        public SimulationObserver(Instance instance)
        {
            _instance = instance;
            _nextSnapshotDistanceTraveled = _instance.StatTimeStart + STEP_LENGTH_DISTANCE_TRAVELED;
            _lastDistanceTraveled = _instance.StatOverallDistanceTraveled;
            _nextSnapshotLocationPolling = _instance.StatTimeStart;
            _nextSnapshotPerformancePolling = _instance.StatTimeStart;
            _nextSnapshotBotInfoPolling = _instance.StatTimeStart;
            _nextSnapshotInventoryLevelPolling = _instance.StatTimeStart;
            _nextSnapshotBundleOrderSituationPolling = _instance.StatTimeStart;
            _nextSnapshotPodHandlingPolling = _instance.StatTimeStart;
            _nextSnapshotWellSortednessPolling = _instance.StatTimeStart;
        }

        /// <summary>
        /// The instance that is observed.
        /// </summary>
        private Instance _instance;

        /// <summary>
        /// Resets this observer.
        /// </summary>
        public void Reset()
        {
            // Clear snapshots
            _nextSnapshotDistanceTraveled = _instance.Controller.CurrentTime + STEP_LENGTH_DISTANCE_TRAVELED;
            _lastDistanceTraveled = 0;
            _nextSnapshotLocationPolling = _instance.Controller.CurrentTime;
            _logDistanceTraveled.Clear();
            _nextSnapshotBotInfoPolling = _instance.Controller.CurrentTime;
            _logBotInfoPolling.Clear();
            _nextSnapshotInventoryLevelPolling = _instance.Controller.CurrentTime;
            _logInventoryLevelPolling.Clear();
            _logPerformancePolling.Clear();
            _nextSnapshotBundleOrderSituationPolling = _instance.Controller.CurrentTime;
            _logBundleOrderSituationPolling.Clear();
            _nextSnapshotPodHandlingPolling = _instance.Controller.CurrentTime;
            _logPodHandlingPolling.Clear();
            _nextSnapshotWellSortednessPolling = _instance.Controller.CurrentTime;
            _logWellSortednessPolling.Clear();
            _wellSortednessLastOStationTripCount.Clear(); // Should be fine, because the trip statistics are also reset
            // Clear timings
            _timingDecisionsOverall = 0.0;
            _timingPathPlanningOverall = 0.0; _timingPathPlanningCount = 0;
            _timingTaskAllocationOverall = 0.0; _timingTaskAllocationCount = 0;
            _timingItemStorageOverall = 0.0; _timingItemStorageCount = 0;
            _timingPodStorageOverall = 0.0; _timingPodStorageCount = 0;
            _timingRepositioningOverall = 0.0; _timingRepositioningCount = 0;
            _timingReplenishmentBatchingOverall = 0.0; _timingReplenishmentBatchingCount = 0;
            _timingOrderBatchingOverall = 0.0; _timingOrderBatchingCount = 0;
        }

        /// <summary>
        /// Flushes all the data collected until now.
        /// </summary>
        public void FlushData()
        {
            FlushTraveledDistance();
            FlushLocationsPolled();
            FlushStorageLocationInfoPolled();
            FlushBotInfoPolled();
            FlushInventoryLevelPolled();
            FlushPerformancePolls();
            FlushBundleOrderSituationPolled();
            FlushPodHandlingPolled();
            FlushWellSortednessPolled();
        }

        #region Distance traveled

        /// <summary>
        /// The simulation time of the next snapshot.
        /// </summary>
        private double _nextSnapshotDistanceTraveled;

        /// <summary>
        /// The overall traveled distance of the last snapshot.
        /// </summary>
        private double _lastDistanceTraveled;

        /// <summary>
        /// Log of the traveled distances over time.
        /// </summary>
        private List<DistanceDatapoint> _logDistanceTraveled = new List<DistanceDatapoint>();

        /// <summary>
        /// Flushes the monitored traveled distance data to the data-file.
        /// </summary>
        private void FlushTraveledDistance()
        {
            // Init statistics directory
            _instance.StatInitDirectory();
            // Write collision progression
            switch (_instance.SettingConfig.LogFileLevel)
            {
                case Configurations.LogFileLevel.All:
                    bool alreadyExists = File.Exists(Path.Combine(_instance.SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.TraveledDistanceProgressionRaw]));
                    using (StreamWriter sw = new StreamWriter(Path.Combine(_instance.SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.TraveledDistanceProgressionRaw]), true))
                    {
                        if (!alreadyExists)
                            sw.WriteLine(IOConstants.COMMENT_LINE + DistanceDatapoint.GetHeader());
                        foreach (var d in _logDistanceTraveled)
                            sw.WriteLine(d.GetLine());
                    }
                    break;
                case Configurations.LogFileLevel.FootprintOnly:
                    break;
                default: throw new ArgumentException("Unknown log level: " + _instance.SettingConfig.LogFileLevel);
            }
            // Clear the data points
            _logDistanceTraveled.Clear();
        }

        #endregion

        #region Memory polling

        /// <summary>
        /// The simulation time of the next performance snapshot.
        /// </summary>
        private double _nextSnapshotPerformancePolling;

        /// <summary>
        /// Log of the performance polls.
        /// </summary>
        private List<PerformanceDatapoint> _logPerformancePolling = new List<PerformanceDatapoint>();

        /// <summary>
        /// Flushes the monitored performance polls to the data-file.
        /// </summary>
        private void FlushPerformancePolls()
        {
            // Init statistics directory
            _instance.StatInitDirectory();
            // Write collision progression
            switch (_instance.SettingConfig.LogFileLevel)
            {
                case Configurations.LogFileLevel.All:
                    bool alreadyExists = File.Exists(Path.Combine(_instance.SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.PerformancePollingRaw]));
                    using (StreamWriter sw = new StreamWriter(Path.Combine(_instance.SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.PerformancePollingRaw]), true))
                    {
                        if (!alreadyExists)
                            sw.WriteLine(IOConstants.COMMENT_LINE + PerformanceDatapoint.GetHeader());
                        foreach (var d in _logPerformancePolling)
                            sw.WriteLine(d.GetLine());
                    }
                    break;
                case Configurations.LogFileLevel.FootprintOnly:
                    break;
                default: throw new ArgumentException("Unknown log level: " + _instance.SettingConfig.LogFileLevel);
            }
            // Clear the data points
            _logPerformancePolling.Clear();
        }

        #endregion

        #region Position polling

        /// <summary>
        /// The simulation time of the next snapshot.
        /// </summary>
        private double _nextSnapshotLocationPolling;

        /// <summary>
        /// Log of the polled positions over time.
        /// </summary>
        private List<LocationDatapoint> _logLocationPolling = new List<LocationDatapoint>();

        private void FlushLocationsPolled()
        {
            // Init statistics directory
            _instance.StatInitDirectory();
            // Write collision progression
            switch (_instance.SettingConfig.LogFileLevel)
            {
                case Configurations.LogFileLevel.All:
                    bool alreadyExists = File.Exists(Path.Combine(_instance.SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.HeatLocationPolling]));
                    using (StreamWriter sw = new StreamWriter(Path.Combine(_instance.SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.HeatLocationPolling]), true))
                    {
                        if (!alreadyExists)
                            sw.WriteLine(LocationDatapoint.GetCSVHeader());
                        foreach (var d in _logLocationPolling)
                            sw.WriteLine(d.ToCSV());
                    }
                    break;
                case Configurations.LogFileLevel.FootprintOnly:
                    break;
                default: throw new ArgumentException("Unknown log level: " + _instance.SettingConfig.LogFileLevel);
            }
            // Clear the data points
            _logLocationPolling.Clear();
        }

        #endregion

        #region Pod polling

        /// <summary>
        /// The simulation time of the next snapshot.
        /// </summary>
        private double _nextSnapshotStorageLocationInfoPolling;

        /// <summary>
        /// Log of the polled storage location info over time.
        /// </summary>
        private List<StorageLocationInfoDatapoint> _logStorageLocationInfoPolling = new List<StorageLocationInfoDatapoint>();

        private void FlushStorageLocationInfoPolled()
        {
            // Init statistics directory
            _instance.StatInitDirectory();
            // Write storage location info polled
            switch (_instance.SettingConfig.LogFileLevel)
            {
                case Configurations.LogFileLevel.All:
                    bool alreadyExists = File.Exists(Path.Combine(_instance.SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.HeatStorageLocationPolling]));
                    using (StreamWriter sw = new StreamWriter(Path.Combine(_instance.SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.HeatStorageLocationPolling]), true))
                    {
                        if (!alreadyExists)
                            sw.WriteLine(StorageLocationInfoDatapoint.GetCSVHeader());
                        foreach (var d in _logStorageLocationInfoPolling)
                            sw.WriteLine(d.ToCSV());
                    }
                    break;
                case Configurations.LogFileLevel.FootprintOnly:
                    break;
                default: throw new ArgumentException("Unknown log level: " + _instance.SettingConfig.LogFileLevel);
            }
            // Clear the data points
            _logStorageLocationInfoPolling.Clear();
        }

        #endregion

        #region Bot info polling

        /// <summary>
        /// The simulation time of the next snapshot.
        /// </summary>
        private double _nextSnapshotBotInfoPolling;
        /// <summary>
        /// Log of the polled bot information over time.
        /// </summary>
        private List<BotDatapoint> _logBotInfoPolling = new List<BotDatapoint>();

        private void FlushBotInfoPolled()
        {
            // Init statistics directory
            _instance.StatInitDirectory();
            // Write backlog level progression
            switch (_instance.SettingConfig.LogFileLevel)
            {
                case Configurations.LogFileLevel.All:
                    bool alreadyExists = File.Exists(Path.Combine(_instance.SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.BotInfoPollingRaw]));
                    using (StreamWriter sw = new StreamWriter(Path.Combine(_instance.SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.BotInfoPollingRaw]), true))
                    {
                        if (!alreadyExists)
                            sw.WriteLine(IOConstants.COMMENT_LINE + BotDatapoint.GetHeader());
                        foreach (var d in _logBotInfoPolling)
                            sw.WriteLine(d.GetLine());
                    }
                    break;
                case Configurations.LogFileLevel.FootprintOnly:
                    break;
                default: throw new ArgumentException("Unknown log level: " + _instance.SettingConfig.LogFileLevel);
            }
            // Clear the data points
            _logBotInfoPolling.Clear();
        }

        #endregion

        #region Inventory level polling

        /// <summary>
        /// The simulation time of the next snapshot.
        /// </summary>
        private double _nextSnapshotInventoryLevelPolling;

        /// <summary>
        /// The maximal capacity of the inventory storage.
        /// </summary>
        private double _maxInventoryCapacity = double.NaN;
        /// <summary>
        /// Log of the polled inventory over time.
        /// </summary>
        private List<InventoryLevelDatapoint> _logInventoryLevelPolling = new List<InventoryLevelDatapoint>();
        internal IEnumerable<InventoryLevelDatapoint> InventoryLevelLog { get { return _logInventoryLevelPolling; } }

        private void FlushInventoryLevelPolled()
        {
            // Init statistics directory
            _instance.StatInitDirectory();
            // Write inventory level progression
            switch (_instance.SettingConfig.LogFileLevel)
            {
                case Configurations.LogFileLevel.All:
                    bool alreadyExists = File.Exists(Path.Combine(_instance.SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.InventoryLevelPollingRaw]));
                    using (StreamWriter sw = new StreamWriter(Path.Combine(_instance.SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.InventoryLevelPollingRaw]), true))
                    {
                        if (!alreadyExists)
                            sw.WriteLine(IOConstants.COMMENT_LINE + InventoryLevelDatapoint.GetHeader());
                        foreach (var d in _logInventoryLevelPolling)
                            sw.WriteLine(d.GetLine());
                    }
                    break;
                case Configurations.LogFileLevel.FootprintOnly:
                    break;
                default: throw new ArgumentException("Unknown log level: " + _instance.SettingConfig.LogFileLevel);
            }
            // Clear the data points
            _logInventoryLevelPolling.Clear();
        }

        #endregion

        #region Backlog level polling

        /// <summary>
        /// The simulation time of the next snapshot.
        /// </summary>
        private double _nextSnapshotBundleOrderSituationPolling;

        /// <summary>
        /// Log of the polled backlog level over time.
        /// </summary>
        private List<BundleOrderSituationDatapoint> _logBundleOrderSituationPolling = new List<BundleOrderSituationDatapoint>();

        /// <summary>
        /// Log of the polled backlog level over time.
        /// </summary>
        public IEnumerable<BundleOrderSituationDatapoint> BundleOrderSituationLog { get { return _logBundleOrderSituationPolling; } }

        private void FlushBundleOrderSituationPolled()
        {
            // Init statistics directory
            _instance.StatInitDirectory();
            // Write backlog level progression
            switch (_instance.SettingConfig.LogFileLevel)
            {
                case Configurations.LogFileLevel.All:
                    bool alreadyExists = File.Exists(Path.Combine(_instance.SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.BundleOrderSituationPollingRaw]));
                    using (StreamWriter sw = new StreamWriter(Path.Combine(_instance.SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.BundleOrderSituationPollingRaw]), true))
                    {
                        if (!alreadyExists)
                            sw.WriteLine(IOConstants.COMMENT_LINE + BundleOrderSituationDatapoint.GetHeader());
                        foreach (var d in _logBundleOrderSituationPolling)
                            sw.WriteLine(d.GetLine());
                    }
                    break;
                case Configurations.LogFileLevel.FootprintOnly:
                    break;
                default: throw new ArgumentException("Unknown log level: " + _instance.SettingConfig.LogFileLevel);
            }
            // Clear the data points
            _logBundleOrderSituationPolling.Clear();
        }

        #endregion

        #region Pod handling polling

        /// <summary>
        /// The simulation time of the next snapshot.
        /// </summary>
        private double _nextSnapshotPodHandlingPolling;
        /// <summary>
        /// Log of the polled pod handling over time.
        /// </summary>
        private List<StationDataPoint> _logPodHandlingPolling = new List<StationDataPoint>();
        /// <summary>
        /// Log of the polled pod handling over time.
        /// </summary>
        public IEnumerable<StationDataPoint> PodHandlingLog { get { return _logPodHandlingPolling; } }

        private void FlushPodHandlingPolled()
        {
            // Init statistics directory
            _instance.StatInitDirectory();
            // Write backlog level progression
            switch (_instance.SettingConfig.LogFileLevel)
            {
                case Configurations.LogFileLevel.All:
                    bool alreadyExists = File.Exists(Path.Combine(_instance.SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.StationPollingRaw]));
                    using (StreamWriter sw = new StreamWriter(Path.Combine(_instance.SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.StationPollingRaw]), true))
                    {
                        if (!alreadyExists)
                            sw.WriteLine(IOConstants.COMMENT_LINE + StationDataPoint.GetHeader());
                        foreach (var d in _logPodHandlingPolling)
                            sw.WriteLine(d.GetLine());
                    }
                    break;
                case Configurations.LogFileLevel.FootprintOnly:
                    break;
                default: throw new ArgumentException("Unknown log level: " + _instance.SettingConfig.LogFileLevel);
            }
            // Clear the data points
            _logPodHandlingPolling.Clear();
        }

        #endregion

        #region Well sortedness polling

        /// <summary>
        /// The simulation time of the next snapshot.
        /// </summary>
        private double _nextSnapshotWellSortednessPolling;
        /// <summary>
        /// The the count of trips to an output station during the last well-sortedness measurement.
        /// </summary>
        private Dictionary<Waypoint, int> _wellSortednessLastOStationTripCount = new Dictionary<Waypoint, int>();
        /// <summary>
        /// Log of the polled well sortedness over time.
        /// </summary>
        private List<WellSortednessDatapoint> _logWellSortednessPolling = new List<WellSortednessDatapoint>();
        /// <summary>
        /// Log of the polled well sortedness over time.
        /// </summary>
        public IEnumerable<WellSortednessDatapoint> WellSortednessLog { get { return _logWellSortednessPolling; } }

        private void FlushWellSortednessPolled()
        {
            // Init statistics directory
            _instance.StatInitDirectory();
            // Write backlog level progression
            switch (_instance.SettingConfig.LogFileLevel)
            {
                case Configurations.LogFileLevel.All:
                    bool alreadyExists = File.Exists(Path.Combine(_instance.SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.WellSortednessPollingRaw]));
                    using (StreamWriter sw = new StreamWriter(Path.Combine(_instance.SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.WellSortednessPollingRaw]), true))
                    {
                        if (!alreadyExists)
                            sw.WriteLine(WellSortednessDatapoint.GetHeader());
                        foreach (var d in _logWellSortednessPolling)
                            sw.WriteLine(d.GetLine());
                    }
                    break;
                case Configurations.LogFileLevel.FootprintOnly:
                    break;
                default: throw new ArgumentException("Unknown log level: " + _instance.SettingConfig.LogFileLevel);
            }
            // Clear the data points
            _logWellSortednessPolling.Clear();
        }

        #endregion

        #region Controller timing

        /// <summary>
        /// The overall time spent deciding / planning.
        /// </summary>
        private double _timingDecisionsOverall = 0.0;
        /// <summary>
        /// The overall time spent in computing paths.
        /// </summary>
        private double _timingPathPlanningOverall = 0.0;
        /// <summary>
        /// The number of times path planning was called for a decision.
        /// </summary>
        private int _timingPathPlanningCount = 0;
        /// <summary>
        /// The overall time spent in deciding the tasks for the bots.
        /// </summary>
        private double _timingTaskAllocationOverall = 0.0;
        /// <summary>
        /// The number of times task allocation was called for a decision.
        /// </summary>
        private int _timingTaskAllocationCount = 0;
        /// <summary>
        /// The overall time spent in deciding the pod for an item.
        /// </summary>
        private double _timingItemStorageOverall = 0.0;
        /// <summary>
        /// The number of times item storage assignment was called for a decision.
        /// </summary>
        private int _timingItemStorageCount = 0;
        /// <summary>
        /// The overall time spent in deciding the position for a pod.
        /// </summary>
        private double _timingPodStorageOverall = 0.0;
        /// <summary>
        /// The number of times pod storage assignment was called for a decision.
        /// </summary>
        private int _timingPodStorageCount = 0;
        /// <summary>
        /// The overall time spent in deciding about relocations.
        /// </summary>
        private double _timingRepositioningOverall = 0.0;
        /// <summary>
        /// The number of times the repositioning manager was called for a decision.
        /// </summary>
        private int _timingRepositioningCount = 0;
        /// <summary>
        /// The overall time spent in deciding the station for an item-bundle.
        /// </summary>
        private double _timingReplenishmentBatchingOverall = 0.0;
        /// <summary>
        /// The number of times replenishment batching was called for a decision.
        /// </summary>
        private int _timingReplenishmentBatchingCount = 0;
        /// <summary>
        /// The overall time spent in deciding the station for an order.
        /// </summary>
        private double _timingOrderBatchingOverall = 0.0;
        /// <summary>
        /// The number of times order batching was called for a decision.
        /// </summary>
        private int _timingOrderBatchingCount = 0;

        /// <summary>
        /// The overall time spent deciding / planning.
        /// </summary>
        public double TimingDecisionsOverall { get { return _timingDecisionsOverall; } }

        /// <summary>
        /// The overall time spent in path planning.
        /// </summary>
        public double TimingPathPlanningOverall { get { return _timingPathPlanningOverall; } }
        /// <summary>
        /// The average time consumed for making a single decision.
        /// </summary>
        public double TimingPathPlanningAverage { get { return _timingPathPlanningCount == 0 ? 0 : _timingPathPlanningOverall / _timingPathPlanningCount; } }
        /// <summary>
        /// The number of decisions done for path planning.
        /// </summary>
        public int TimingPathPlanningDecisionCount { get { return _timingPathPlanningCount; } }

        /// <summary>
        /// The overall time spent in task allocation.
        /// </summary>
        public double TimingTaskAllocationOverall { get { return _timingTaskAllocationOverall; } }
        /// <summary>
        /// The average time consumed for making a single decision.
        /// </summary>
        public double TimingTaskAllocationAverage { get { return _timingTaskAllocationCount == 0 ? 0 : _timingTaskAllocationOverall / _timingTaskAllocationCount; } }
        /// <summary>
        /// The number of decisions done for task allocation.
        /// </summary>
        public int TimingTaskAllocationDecisionCount { get { return _timingTaskAllocationCount; } }

        /// <summary>
        /// The overall time spent in item storage.
        /// </summary>
        public double TimingItemStorageOverall { get { return _timingItemStorageOverall; } }
        /// <summary>
        /// The average time consumed for making a single decision.
        /// </summary>
        public double TimingItemStorageAverage { get { return _timingItemStorageCount == 0 ? 0 : _timingItemStorageOverall / _timingItemStorageCount; } }
        /// <summary>
        /// The number of decisions done for item storage.
        /// </summary>
        public int TimingItemStorageDecisionCount { get { return _timingItemStorageCount; } }

        /// <summary>
        /// The overall time spent in pod storage.
        /// </summary>
        public double TimingPodStorageOverall { get { return _timingPodStorageOverall; } }
        /// <summary>
        /// The average time consumed for making a single decision.
        /// </summary>
        public double TimingPodStorageAverage { get { return _timingPodStorageCount == 0 ? 0 : _timingPodStorageOverall / _timingPodStorageCount; } }
        /// <summary>
        /// The number of decisions done for pod storage.
        /// </summary>
        public int TimingPodStorageDecisionCount { get { return _timingPodStorageCount; } }

        /// <summary>
        /// The overall time spent in repositioning.
        /// </summary>
        public double TimingRepositioningOverall { get { return _timingRepositioningOverall; } }
        /// <summary>
        /// The average time consumed for making a single decision.
        /// </summary>
        public double TimingRepositioningAverage { get { return _timingRepositioningCount == 0 ? 0 : _timingRepositioningOverall / _timingRepositioningCount; } }
        /// <summary>
        /// The number of decisions done for repositioning.
        /// </summary>
        public int TimingRepositioningDecisionCount { get { return _timingRepositioningCount; } }

        /// <summary>
        /// The overall time spent in replenishment batching.
        /// </summary>
        public double TimingReplenishmentBatchingOverall { get { return _timingReplenishmentBatchingOverall; } }
        /// <summary>
        /// The average time consumed for making a single decision.
        /// </summary>
        public double TimingReplenishmentBatchingAverage { get { return _timingReplenishmentBatchingCount == 0 ? 0 : _timingReplenishmentBatchingOverall / _timingReplenishmentBatchingCount; } }
        /// <summary>
        /// The number of decisions done for replenishment batching.
        /// </summary>
        public int TimingReplenishmentBatchingDecisionCount { get { return _timingReplenishmentBatchingCount; } }

        /// <summary>
        /// The overall time spent in order batching.
        /// </summary>
        public double TimingOrderBatchingOverall { get { return _timingOrderBatchingOverall; } }
        /// <summary>
        /// The average time consumed for making a single decision.
        /// </summary>
        public double TimingOrderBatchingAverage { get { return _timingOrderBatchingCount == 0 ? 0 : _timingOrderBatchingOverall / _timingOrderBatchingCount; } }
        /// <summary>
        /// The number of decisions done for order batching.
        /// </summary>
        public int TimingOrderBatchingDecisionCount { get { return _timingOrderBatchingCount; } }

        /// <summary>
        /// Signals this monitor that a path planning decision just happened and logs the time it took to decide in s.
        /// </summary>
        /// <param name="time">The time it took to determine the path in seconds.</param>
        public void TimePathPlanning(double time) { _timingPathPlanningOverall += time; _timingPathPlanningCount++; _timingDecisionsOverall += time; }
        /// <summary>
        /// Signals this monitor that a task allocation decision just happened and logs the time it took do decide in s.
        /// </summary>
        /// <param name="time">The time it took to determine the task for the bot.</param>
        public void TimeTaskAllocation(double time) { _timingTaskAllocationOverall += time; _timingTaskAllocationCount++; _timingDecisionsOverall += time; }
        /// <summary>
        /// Signals this monitor that a item storage assignment decision just happened and logs the time it took do decide in s.
        /// </summary>
        /// <param name="time">The time it took to determine the pod for the item-bundle.</param>
        public void TimeItemStorage(double time) { _timingItemStorageOverall += time; _timingItemStorageCount++; _timingDecisionsOverall += time; }
        /// <summary>
        /// Signals this monitor that a pod storage assignment decision just happened and logs the time it took do decide in s.
        /// </summary>
        /// <param name="time">The time it took to determine the position for the pod.</param>
        public void TimePodStorage(double time) { _timingPodStorageOverall += time; _timingPodStorageCount++; _timingDecisionsOverall += time; }
        /// <summary>
        /// Signals this monitor that a repositioning decision just happened and logs the time it took do decide in s.
        /// </summary>
        /// <param name="time">The time it took to determine a repositioning move.</param>
        public void TimeRepositioning(double time) { _timingRepositioningOverall += time; _timingRepositioningCount++; _timingDecisionsOverall += time; }
        /// <summary>
        /// Signals this monitor that a replenishment batching decision just happened and logs the time it took do decide in s.
        /// </summary>
        /// <param name="time">The time it took to determine the input-station for the item-bundle.</param>
        public void TimeReplenishmentBatching(double time) { _timingReplenishmentBatchingOverall += time; _timingReplenishmentBatchingCount++; _timingDecisionsOverall += time; }
        /// <summary>
        /// Signals this monitor that a order batching decision just happened and logs the time it took do decide in s.
        /// </summary>
        /// <param name="time">The time it took to determine the output-station for the order.</param>
        public void TimeOrderBatching(double time) { _timingOrderBatchingOverall += time; _timingOrderBatchingCount++; _timingDecisionsOverall += time; }

        #endregion

        #region IUpdateable Members

        /// <summary>
        /// The next event when this element has to be updated.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns>The next time this element has to be updated.</returns>
        public double GetNextEventTime(double currentTime)
        {
            return MathHelpers.Min(
                _nextSnapshotWellSortednessPolling,
                _nextSnapshotPodHandlingPolling,
                _nextSnapshotBundleOrderSituationPolling,
                _nextSnapshotBotInfoPolling,
                _nextSnapshotInventoryLevelPolling,
                _nextSnapshotDistanceTraveled,
                _nextSnapshotLocationPolling);
        }
        /// <summary>
        /// Updates the element to the specified time.
        /// </summary>
        /// <param name="lastTime">The time before the update.</param>
        /// <param name="currentTime">The time to update to.</param>
        public void Update(double lastTime, double currentTime)
        {
            // Monitor traveled distance
            if (currentTime >= _nextSnapshotDistanceTraveled)
            {
                // Calculate next snapshot
                _nextSnapshotDistanceTraveled += STEP_LENGTH_DISTANCE_TRAVELED;
                // Calculate travel distance difference for this snapshot
                double newOverallTraveledDistance = _instance.StatOverallDistanceTraveled;
                _logDistanceTraveled.Add(new DistanceDatapoint(_instance.StatTime, newOverallTraveledDistance - _lastDistanceTraveled));
                _lastDistanceTraveled = newOverallTraveledDistance;
                // Flush on getting too big
                if (_logDistanceTraveled.Count >= Instance.STAT_MAX_DATA_POINTS)
                    FlushTraveledDistance();
            }
            // Monitor positions
            if (currentTime >= _nextSnapshotLocationPolling)
            {
                // Calculate next snapshot
                _nextSnapshotLocationPolling += _instance.SettingConfig.IntenseLocationPolling ? STEP_LENGTH_POSITION_POLL_INTENSE : STEP_LENGTH_POSITION_POLL;
                // Add current locations
                _logLocationPolling.AddRange(
                    _instance.Bots.Select(b =>
                        new LocationDatapoint() { X = b.X, Y = b.Y, TimeStamp = currentTime - _instance.StatTimeStart, Tier = b.Tier.ID, BotTask = b.CurrentTask.Type }));
                // Flush on getting too big
                if (_logLocationPolling.Count >= Instance.STAT_MAX_DATA_POINTS)
                    FlushLocationsPolled();
            }
            // Monitor storage location info
            if (currentTime >= _nextSnapshotStorageLocationInfoPolling)
            {
                // Calculate next snapshot
                _nextSnapshotStorageLocationInfoPolling += STEP_LENGTH_STORAGE_LOCATION_INFO_POLL;
                // Add current storage location info
                _logStorageLocationInfoPolling.AddRange(
                    _instance.Waypoints.Where(wp => wp.PodStorageLocation).Select(wp =>
                            new StorageLocationInfoDatapoint()
                            {
                                X = wp.X,
                                Y = wp.Y,
                                TimeStamp = currentTime - _instance.StatTimeStart,
                                Speed = wp.Pod != null ? _instance.ElementMetaInfoTracker.GetPodSpeed(wp.Pod) : 0,
                                Utility = wp.Pod != null ? _instance.ElementMetaInfoTracker.GetPodUtility(wp.Pod) : 0,
                                Combined = wp.Pod != null ? _instance.ElementMetaInfoTracker.GetPodCombinedScore(wp.Pod, 1, 1) : 0,
                                Tier = wp.Tier.ID,
                            }));
                // Flush on getting too big
                if (_logStorageLocationInfoPolling.Count >= Instance.STAT_MAX_DATA_POINTS)
                    FlushStorageLocationInfoPolled();
            }
            // Monitor inventory level
            if (currentTime >= _nextSnapshotInventoryLevelPolling)
            {
                // Calculate next snapshot
                _nextSnapshotInventoryLevelPolling += STEP_LENGTH_INVENTORY_POLL;
                // Get maximal inventory level
                if (double.IsNaN(_maxInventoryCapacity))
                    _maxInventoryCapacity = _instance.Pods.Sum(b => b.Capacity);
                // Fetch inventory level
                double level = _instance.Pods.Sum(b => b.CapacityInUse) / _maxInventoryCapacity;
                // Get inversions
                int invCombinedTotal; int invCombinedRank; double invCombinedAvgRank;
                int invSpeedTotal; int invSpeedRank; double invSpeedAvgRank;
                int invUtilityTotal; int invUtilityRank; double invUtilityAvgRank;
                _instance.ElementMetaInfoTracker.CountInversions(
                    out invCombinedTotal, out invCombinedRank, out invCombinedAvgRank,
                    out invSpeedTotal, out invSpeedRank, out invSpeedAvgRank,
                    out invUtilityTotal, out invUtilityRank, out invUtilityAvgRank);
                // Add current situation
                _logInventoryLevelPolling.Add(new InventoryLevelDatapoint(
                    // Current time
                    _instance.StatTime,
                    // Current inventory level
                    level,
                    // Current SKUs contained
                    _instance.ItemDescriptions.Count(id => _instance.StockInfo.GetActualStock(id) > 0),
                    // Combined inversion info
                    invCombinedTotal, invCombinedRank, invCombinedAvgRank,
                    // Speed inversion info
                    invSpeedTotal, invSpeedRank, invSpeedAvgRank,
                    // Utility inversion info
                    invUtilityTotal, invUtilityRank, invUtilityAvgRank));
                // No flushing of this kind of data, because it is used (in the end) for the footprint datapoint too
            }
            // Monitor backlog level
            if (currentTime >= _nextSnapshotBundleOrderSituationPolling)
            {
                // Calculate next snapshot
                _nextSnapshotBundleOrderSituationPolling += STEP_LENGTH_BUNDLE_ORDER_SITUATION_POLL;
                //  Prepare enumerations
                IEnumerable<ItemBundle> activeBundles = _instance.InputStations.SelectMany(s => s.ItemBundles);
                IEnumerable<Order> activeOrders = _instance.OutputStations.SelectMany(s => s.AssignedOrders);
                // Add current situation
                _logBundleOrderSituationPolling.Add(new BundleOrderSituationDatapoint(
                    // Current time
                    _instance.StatTime,
                    // Number of bundles in backlog
                    _instance.ItemManager.BacklogBundleCount,
                    // Number of orders in backlog
                    _instance.ItemManager.BacklogOrderCount,
                    // Average age of bundles assigned to stations
                    activeBundles.Any() ? activeBundles.Average(b => _instance.StatTime - b.TimeStampSubmit) : 0.0,
                    // Average age of orders assigned to stations
                    activeOrders.Any() ? activeOrders.Average(o => _instance.StatTime - o.TimeStampSubmit) : 0.0,
                    // Average age of bundles assigned to stations
                    activeBundles.Any() ? activeBundles.Average(b => _instance.StatTime - b.TimeStamp) : 0.0,
                    // Average age of orders assigned to stations
                    activeOrders.Any() ? activeOrders.Average(o => _instance.StatTime - o.TimeStamp) : 0.0,
                    // Average frequency of bundles assigned to stations
                    activeBundles.Any() ? activeBundles.Average(b => _instance.FrequencyTracker.GetMeasuredFrequency(b.ItemDescription)) : 0.0,
                    // Average frequency of orders assigned to stations (take units per line into account)
                    activeOrders.Any() ? activeOrders.Average(o => o.Positions.Sum(p => _instance.FrequencyTracker.GetMeasuredFrequency(p.Key) * p.Value) / o.Positions.Sum(p => p.Value)) : 0.0));
                // Flush on getting too big
                if (_logBundleOrderSituationPolling.Count >= Instance.STAT_MAX_DATA_POINTS)
                    FlushBundleOrderSituationPolled();
            }
            // Monitor bot statistics
            if (currentTime >= _nextSnapshotBotInfoPolling)
            {
                // Calculate next snapshot
                _nextSnapshotBotInfoPolling += STEP_LENGTH_BOT_POLL;
                // Add current situation
                _logBotInfoPolling.Add(new BotDatapoint(_instance.StatTime,
                    // Queueing bots
                    _instance.Bots.Count(b => b.IsQueueing),
                    Enum.GetValues(typeof(BotTaskType)).Cast<BotTaskType>()
                        .Select(t => new Tuple<string, int>(
                            // The type of the task as a string
                            t.ToString(),
                            // The number of bots assigned to the given task type (if null, assume no task)
                            _instance.Bots.Count(b => b.CurrentTask == null && t == BotTaskType.None || b.CurrentTask.Type == t)))
                        .ToArray()));
                // Flush on getting too big
                if (_logBotInfoPolling.Count >= Instance.STAT_MAX_DATA_POINTS)
                    FlushBotInfoPolled();
            }
            // Monitor station statistics
            if (currentTime >= _nextSnapshotPodHandlingPolling)
            {
                // Calculate next snapshot
                _nextSnapshotPodHandlingPolling += STEP_LENGTH_POD_HANDLING_POLL;
                // Add current situation
                _logPodHandlingPolling.Add(new StationDataPoint(_instance.StatTime,
                    // The number of pods handled at input stations
                    _instance.InputStations.Count > 0 ? _instance.InputStations.Sum(s => s.StatPodsHandled) : 0,
                    // The number of pods handled at output stations
                    _instance.OutputStations.Count > 0 ? _instance.OutputStations.Sum(s => s.StatPodsHandled) : 0,
                    // The number of active input stations
                    _instance.InputStations.Count(s => s.Active),
                    // The number of active output stations
                    _instance.OutputStations.Count(s => s.Active),
                    // The input station idle times
                    _instance.InputStations.Select(s => new Tuple<int, double>(s.ID, s.StatIdleTime)).ToArray(),
                    // The output station idle times
                    _instance.OutputStations.Select(s => new Tuple<int, double>(s.ID, s.StatIdleTime)).ToArray(),
                    // The input station active times
                    _instance.InputStations.Select(s => new Tuple<int, double>(s.ID, s.StatActiveTime)).ToArray(),
                    // The output station active times
                    _instance.OutputStations.Select(s => new Tuple<int, double>(s.ID, s.StatActiveTime)).ToArray(),
                    // The open insert requests
                    _instance.InputStations.Select(s => new Tuple<int, int>(s.ID, s.StatCurrentlyOpenRequests)).ToArray(),
                    // The open extract requests
                    _instance.OutputStations.Select(s => new Tuple<int, int>(s.ID, s.StatCurrentlyOpenRequests)).ToArray(),
                    // The open bundles to store
                    _instance.InputStations.Select(s => new Tuple<int, int>(s.ID, s.StatCurrentlyOpenBundles)).ToArray(),
                    // The open items to pick
                    _instance.OutputStations.Select(s => new Tuple<int, int>(s.ID, s.StatCurrentlyOpenItems)).ToArray(),
                    // The bots per input station
                    _instance.InputStations.Select(s => new Tuple<int, int>(s.ID, _instance.StatGetInfoBalancedBotsPerStation(s))).ToArray(),
                    // The bots per output station
                    _instance.OutputStations.Select(s => new Tuple<int, int>(s.ID, _instance.StatGetInfoBalancedBotsPerStation(s))).ToArray(),
                    // The pods handled per input station
                    _instance.InputStations.Select(s => new Tuple<int, int>(s.ID, s.StatPodsHandled)).ToArray(),
                    // The pods handled per output station
                    _instance.OutputStations.Select(s => new Tuple<int, int>(s.ID, s.StatPodsHandled)).ToArray(),
                    // The bundles stored per input station
                    _instance.InputStations.Select(s => new Tuple<int, int>(s.ID, s.StatNumBundlesStored)).ToArray(),
                    // The items picked per output station
                    _instance.OutputStations.Select(s => new Tuple<int, int>(s.ID, s.StatNumItemsPicked)).ToArray()));
                // Flush on getting too big
                if (_logPodHandlingPolling.Count >= Instance.STAT_MAX_DATA_POINTS)
                    FlushPodHandlingPolled();
            }
            // Skip well-sortedness event, if disabled
            if (!_instance.SettingConfig.MonitorWellSortedness && currentTime >= _nextSnapshotWellSortednessPolling)
            {
                // Calculate next snapshot
                _nextSnapshotWellSortednessPolling += STEP_LENGTH_WELL_SORTEDNESS_POLL;
            }
            // Monitor well sortedness
            if (currentTime >= _nextSnapshotWellSortednessPolling)
            {
                // Calculate next snapshot
                _nextSnapshotWellSortednessPolling += STEP_LENGTH_WELL_SORTEDNESS_POLL;
                // Add current situation
                List<WellSortednessPathTimeTuple> currentTuples = new List<WellSortednessPathTimeTuple>();
                foreach (var pathTime in _instance.Waypoints
                    // Only look at storage locations
                    .Where(wp => wp.PodStorageLocation)
                    // Group all storage locations by their minimum path time to an output station - BUT round to full seconds to decrease log size while giving up only small accuracy
                    .GroupBy(wp => Math.Round(_instance.OutputStations.Min(o => Distances.CalculateShortestTimePathPodSafe(wp, o.Waypoint, _instance))))
                    // Order by the path length
                    .OrderBy(g => g.Key))
                {
                    int podCount = pathTime.Count(wp => wp.Pod != null);
                    int storageLocationCount = pathTime.Count();
                    IEnumerable<double> skuFrequencies = pathTime.Select(wp =>
                        // Check whether there is a pod containing bundles
                        (wp.Pod != null && wp.Pod.ItemDescriptionsContained.Any()) ?
                        // Calculate average frequency of this pod based on the distinct item descriptions stored in it
                        wp.Pod.ItemDescriptionsContained.Average(item => _instance.FrequencyTracker.GetStaticFrequency(item)) :
                        // Use 0 as the default value
                        0.0);
                    IEnumerable<double> contentFrequencies = pathTime.Select(wp =>
                        // Check whether there is a pod containing bundles
                        (wp.Pod != null && wp.Pod.ItemDescriptionsContained.Any()) ?
                        wp.Pod.ItemDescriptionsContained.Sum(item => _instance.FrequencyTracker.GetStaticFrequency(item) * wp.Pod.CountContained(item)) :
                        // Use 0 as the default value
                        0.0);
                    IEnumerable<int> oStationTrips = pathTime.Select(wp =>
                        // First obtain overall trips to output stations
                        _instance.OutputStations.Sum(o => wp.StatContainsTripDataOut(o.Waypoint) ? wp.StatGetTripDataOut(o.Waypoint).Count : 0) -
                        // Then subtract trips of last snashot to only log the trips of the current sector
                        (_wellSortednessLastOStationTripCount.ContainsKey(wp) ? _wellSortednessLastOStationTripCount[wp] : 0));
                    IEnumerable<double> podSpeeds = pathTime.Select(wp =>
                        // Check whether there is a pod at the location
                        (wp.Pod != null) ?
                        // Get the speed score of the pod
                        _instance.ElementMetaInfoTracker.GetPodSpeed(wp.Pod) :
                        // Use 0 as the default value
                        0.0);
                    IEnumerable<double> podUtilities = pathTime.Select(wp =>
                        // Check whether there is a pod at the location
                        (wp.Pod != null) ?
                        // Get the speed score of the pod
                        _instance.ElementMetaInfoTracker.GetPodUtility(wp.Pod) :
                        // Use 0 as the default value
                        0.0);
                    IEnumerable<double> podScores = pathTime.Select(wp =>
                        // Check whether there is a pod at the location
                        (wp.Pod != null) ?
                        // Get the speed score of the pod
                        _instance.ElementMetaInfoTracker.GetPodCombinedScore(wp.Pod, 1, 1) :
                        // Use 0 as the default value
                        0.0);
                    // Create datapoint
                    currentTuples.Add(new WellSortednessPathTimeTuple()
                    {
                        // Add the shortest travel time of this row
                        PathTime = pathTime.Key,
                        // Add the number of pods currently stored in this row
                        PodCount = podCount,
                        // Add the number of storage locations of this row.
                        StorageLocationCount = storageLocationCount,
                        // Add sum of frequencies of pods of this path time
                        SKUFrequencySum = skuFrequencies.Sum(),
                        // Add sum of content weighted frequencies of pods of this path time
                        ContentFrequencySum = contentFrequencies.Sum(),
                        // Add sum of output-station trips
                        OutputStationTripsSum = oStationTrips.Sum(),
                        // Add pod speeds
                        PodSpeedSum = podSpeeds.Sum(),
                        // Add pod speeds
                        PodUtilitySum = podUtilities.Sum(),
                        // Add pod speeds
                        PodCombinedScoreSum = podScores.Sum(),
                    });
                    // Store values of this snapshot for next update
                    foreach (var distWP in pathTime)
                        // Store overall trips measured at this snapshot
                        _wellSortednessLastOStationTripCount[distWP] = _instance.OutputStations.Sum(o => distWP.StatContainsTripDataOut(o.Waypoint) ? distWP.StatGetTripDataOut(o.Waypoint).Count : 0);
                }
                _logWellSortednessPolling.Add(new WellSortednessDatapoint(currentTime, currentTuples));
                // Flush on getting too big
                if (_logWellSortednessPolling.Count >= Instance.STAT_MAX_DATA_POINTS)
                    FlushWellSortednessPolled();
            }
            // Monitor performance
            if (currentTime >= _nextSnapshotPerformancePolling)
            {
                // Calculate next snapshot
                _nextSnapshotPerformancePolling += STEP_LENGTH_MEMORY_POLL;
                // Update max memory usage
                double memoryUsed = Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0;
                if (_instance.StatMaxMemoryUsed < memoryUsed)
                    _instance.StatMaxMemoryUsed = memoryUsed;
                // Add current situation
                _logPerformancePolling.Add(new PerformanceDatapoint(
                    // The current time
                    _instance.StatTime,
                    // The current memory usage
                    memoryUsed,
                    // The current real time elapsed
                    (_instance.SettingConfig.StartTime != DateTime.MinValue ? (DateTime.Now - _instance.SettingConfig.StartTime).TotalSeconds : 0),
                    // The times used by the controllers
                    new Tuple<string, double>[] {
                        new Tuple<string, double>(FootprintDatapoint.FootPrintEntry.PP.ToString(), _timingPathPlanningOverall),
                        new Tuple<string, double>(FootprintDatapoint.FootPrintEntry.TA.ToString(), _timingTaskAllocationOverall),
                        new Tuple<string, double>(FootprintDatapoint.FootPrintEntry.IS.ToString(), _timingItemStorageOverall),
                        new Tuple<string, double>(FootprintDatapoint.FootPrintEntry.PS.ToString(), _timingPodStorageOverall),
                        new Tuple<string, double>(FootprintDatapoint.FootPrintEntry.RB.ToString(), _timingReplenishmentBatchingOverall),
                        new Tuple<string, double>(FootprintDatapoint.FootPrintEntry.OB.ToString(), _timingOrderBatchingOverall),
                        new Tuple<string, double>(FootprintDatapoint.FootPrintEntry.RP.ToString(), _timingRepositioningOverall),
                    }
                    ));
                // Flush on getting too big
                if (_logPerformancePolling.Count >= Instance.STAT_MAX_DATA_POINTS)
                    FlushPerformancePolls();
            }
        }

        #endregion
    }
}
