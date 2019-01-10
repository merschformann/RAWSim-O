using RAWSimO.Core.Bots;
using RAWSimO.Core.Control;
using RAWSimO.Core.Elements;
using RAWSimO.Core.IO;
using RAWSimO.Core.Items;
using RAWSimO.Core.Management;
using RAWSimO.Core.Statistics;
using RAWSimO.Core.Waypoints;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RAWSimO.Core.Metrics;
using static RAWSimO.Core.Statistics.StationTripDatapoint;

namespace RAWSimO.Core
{
    /// THIS PARTIAL CLASS CONTAINS ALL CORE ELEMENTS OF THE PERFORMANCE INDICATORS
    /// <summary>
    /// The core element of each simulation instance.
    /// </summary>
    public partial class Instance
    {
        #region Data fields

        /// <summary>
        /// Number of datapoints stored before a flush is done.
        /// </summary>
        public const int STAT_MAX_DATA_POINTS = 10000;

        /// <summary>
        /// The time the recording of the statistics started.
        /// </summary>
        internal double StatTimeStart = 0.0;
        /// <summary>
        /// The current time regarding all statistical measurements.
        /// </summary>
        internal double StatTime { get { return Controller.CurrentTime - StatTimeStart; } }

        /// <summary>
        /// Indicates whether the stat reset after the warmup period was done.
        /// </summary>
        public bool StatWarmupResetDone { get; private set; }
        /// <summary>
        /// Indicates whether the stats have been written.
        /// </summary>
        public bool StatResultsWritten { get; private set; }

        /// <summary>
        /// The number of times bundle generation was paused.
        /// </summary>
        public int StatBundleGenerationStops { get; private set; }
        /// <summary>
        /// The number of times order generation was paused.
        /// </summary>
        public int StatOrderGenerationStops { get; private set; }
        /// <summary>
        /// The number of handled items overall.
        /// </summary>
        public int StatOverallItemsHandled { get; private set; }
        /// <summary>
        /// The number of handled bundles overall.
        /// </summary>
        public int StatOverallBundlesHandled { get; private set; }
        /// <summary>
        /// The number of handled order lines overall.
        /// </summary>
        public int StatOverallLinesHandled { get; private set; }
        /// <summary>
        /// The number of handled orders overall.
        /// </summary>
        public int StatOverallOrdersHandled { get; private set; }
        /// <summary>
        /// The number of handled orders overall that were not completed in time.
        /// </summary>
        public int StatOverallOrdersLate { get; private set; }
        /// <summary>
        /// The absolute number of items ordered.
        /// </summary>
        public int StatOverallItemsOrdered { get; private set; }
        /// <summary>
        /// The absolute number of bundles placed.
        /// </summary>
        public int StatOverallBundlesPlaced { get; private set; }
        /// <summary>
        /// The absolute number of orders placed.
        /// </summary>
        public int StatOverallOrdersPlaced { get; private set; }
        /// <summary>
        /// The absolute number of bundles placed.
        /// </summary>
        public int StatOverallBundlesRejected { get; private set; }
        /// <summary>
        /// The absolute number of orders placed.
        /// </summary>
        public int StatOverallOrdersRejected { get; private set; }
        /// <summary>
        /// The total number of repositioning moves that were executed.
        /// </summary>
        public int StatRepositioningMoves { get; private set; }
        /// <summary>
        /// The total distance traveled by the bots so far.
        /// </summary>
        public double StatOverallDistanceTraveled { get { return Bots.Sum(b => b.StatDistanceTraveled); } }
        /// <summary>
        /// The estimated distance by the bots.
        /// </summary>
        public double StatOverallDistanceEstimated { get { return Bots.Sum(b => b.StatDistanceEstimated); } }
        /// <summary>
        /// The total number of assigned tasks so far.
        /// </summary>
        public int StatOverallAssignedTasks { get { return Bots.Sum(b => b.StatAssignedTasks); } }
        /// <summary>
        /// The maximum Memory Usage in megabyte.
        /// </summary>
        public double StatMaxMemoryUsed { get; set; }
        /// <summary>
        /// The turnover times for all completed orders.
        /// </summary>
        internal List<double> _statOrderTurnoverTimes = new List<double>();
        /// <summary>
        /// The throughput times for all completed orders.
        /// </summary>
        internal List<double> _statOrderThroughputTimes = new List<double>();
        /// <summary>
        /// The lateness for all completed orders.
        /// </summary>
        internal List<double> _statOrderLatenessTimes = new List<double>();
        /// <summary>
        /// The turnover times for all completed bundles.
        /// </summary>
        internal List<double> _statBundleTurnoverTimes = new List<double>();
        /// <summary>
        /// The throughput times for all completed bundles.
        /// </summary>
        internal List<double> _statBundleThroughputTimes = new List<double>();
        /// <summary>
        /// The average order throughput time.
        /// </summary>
        public double StatOrderThroughputTimeAvg { get { return _statOrderThroughputTimes.Any() ? _statOrderThroughputTimes.Average(t => t) : 0; } }
        /// <summary>
        /// Stores all times at which an item was handled.
        /// </summary>
        internal List<ItemHandledDatapoint> _statItemHandlingTimestamps = new List<ItemHandledDatapoint>();
        /// <summary>
        /// Stores all times at which an bundle was handled.
        /// </summary>
        internal List<BundleHandledDatapoint> _statBundleHandlingTimestamps = new List<BundleHandledDatapoint>();
        /// <summary>
        /// Stores all times at which an order was handled.
        /// </summary>
        internal List<OrderHandledDatapoint> _statOrderHandlingTimestamps = new List<OrderHandledDatapoint>();
        /// <summary>
        /// Stores all times at which an incoming bundle was placed.
        /// </summary>
        internal List<BundlePlacedDatapoint> _statBundlePlacementTimestamps = new List<BundlePlacedDatapoint>();
        /// <summary>
        /// Stores all times at which a new order was placed.
        /// </summary>
        internal List<OrderPlacedDatapoint> _statOrderPlacementTimestamps = new List<OrderPlacedDatapoint>();
        /// <summary>
        /// Stores all times at which a collision happened.
        /// </summary>
        internal List<CollisionDatapoint> _statCollisionTimestamps = new List<CollisionDatapoint>();
        /// <summary>
        /// Stores all completed trips to stations queueing areas.
        /// </summary>
        internal List<StationTripDatapoint> _statStationTripTimestamps = new List<StationTripDatapoint>();
        /// <summary>
        /// The number of collisions that happened in this instance. This value may count a collision of two robots twice, hence it should only be used as an indicator.
        /// </summary>
        public int StatOverallCollisions { get; private set; }
        /// <summary>
        /// The number of bots that reached their targeted output station queueing area.
        /// </summary>
        private int _oStationTripCount = 0;
        /// <summary>
        /// The average time it took a bot for their last trip towards an output station queueing area.
        /// </summary>
        private double _oStationTripTimeAvg = 0;
        /// <summary>
        /// The number of bots that reached their targeted input station queueing area.
        /// </summary>
        private int _iStationTripCount = 0;
        /// <summary>
        /// The average time it took a bot for their last trip towards an input station queueing area.
        /// </summary>
        private double _iStationTripTimeAvg = 0;
        /// <summary>
        /// Registers another trip completed trip to a station queueing area.
        /// </summary>
        /// <param name="tripType">The type of the trip.</param>
        /// <param name="tripTime">The time for completing the trip.</param>
        protected void StatAddTrip(StationTripType tripType, double tripTime)
        {
            switch (tripType)
            {
                case StationTripType.O: _oStationTripCount++; _oStationTripTimeAvg = _oStationTripTimeAvg + (tripTime - _oStationTripTimeAvg) / (_oStationTripCount); break;
                case StationTripType.I: _iStationTripCount++; _iStationTripTimeAvg = _iStationTripTimeAvg + (tripTime - _iStationTripTimeAvg) / (_iStationTripCount); break;
                default: throw new ArgumentException("Unknown trip type: " + tripType);
            }
        }
        /// <summary>
        /// The number of bots that reached their targeted output station queueing area.
        /// </summary>
        public double OStationTripCount { get { return _oStationTripCount; } }
        /// <summary>
        /// The average time it took a bot for their last trip towards an output station queueing area.
        /// </summary>
        public double OStationTripTimeAvg { get { return _oStationTripTimeAvg; } }
        /// <summary>
        /// The number of bots that reached their targeted input station queueing area.
        /// </summary>
        public double IStationTripCount { get { return _iStationTripCount; } }
        /// <summary>
        /// The number of bots that reached their targeted output station queueing area.
        /// </summary>
        public double IStationTripTimeAvg { get { return _iStationTripTimeAvg; } }
        /// <summary>
        /// The number of times a move could not be executed due to a failed reservation.
        /// </summary>
        public int StatOverallFailedReservations { get; internal set; }
        /// <summary>
        /// The number of times the runtime limit was reached by the path planning method.
        /// </summary>
        public int StatOverallPathPlanningTimeouts { get; internal set; }
        /// <summary>
        /// The overall storage capacity.
        /// </summary>
        private double _storageCapacity = double.NaN;
        /// <summary>
        /// The current overall storage usage.
        /// </summary>
        private double _storageUsage = double.NaN;
        /// <summary>
        /// The current overall storage reserved for bundles.
        /// </summary>
        private double _storageReserved = double.NaN;
        /// <summary>
        /// The current storage capacity required by the backlog bundles.
        /// </summary>
        private double _storageBacklog = double.NaN;
        /// <summary>
        /// The overall storage capacity.
        /// </summary>
        internal double StorageCapacity
        {
            get { if (double.IsNaN(_storageCapacity)) InitStorageTracking(); return _storageCapacity; }
            set { if (double.IsNaN(_storageCapacity)) InitStorageTracking(); _storageCapacity = value; }
        }
        /// <summary>
        /// The current overall storage usage.
        /// </summary>
        internal double StorageUsage
        {
            get { if (double.IsNaN(_storageUsage)) InitStorageTracking(); return _storageUsage; }
            set { if (double.IsNaN(_storageUsage)) InitStorageTracking(); _storageUsage = value; }
        }
        /// <summary>
        /// The current overall storage reserved for bundles.
        /// </summary>
        internal double StorageReserved
        {
            get { if (double.IsNaN(_storageReserved)) InitStorageTracking(); return _storageReserved; }
            set { if (double.IsNaN(_storageReserved)) InitStorageTracking(); _storageReserved = value; }
        }
        /// <summary>
        /// The current storage capacity required by the backlog bundles.
        /// </summary>
        internal double StorageBacklog
        {
            get { if (double.IsNaN(_storageBacklog)) InitStorageTracking(); return _storageBacklog; }
            set { if (double.IsNaN(_storageBacklog)) InitStorageTracking(); _storageBacklog = value; }
        }
        /// <summary>
        /// Initializes storage tracking.
        /// </summary>
        private void InitStorageTracking()
        {
            _storageCapacity = Pods.Sum(p => p.Capacity);
            _storageUsage = Pods.Sum(p => p.CapacityInUse);
            _storageReserved = Pods.Sum(p => p.CapacityReserved);
            _storageBacklog = 0;
        }
        /// <summary>
        /// The overall storage fill level.
        /// </summary>
        public double StatStorageFillLevel { get { return StorageUsage / StorageCapacity; } }
        /// <summary>
        /// The overall storage fill level including the reservations present for the pods.
        /// </summary>
        public double StatStorageFillAndReservedLevel { get { return (StorageUsage + StorageReserved) / StorageCapacity; } }
        /// <summary>
        /// The overall storage fill level including the reservations present for the pods and the capacity consumed by the backlog bundles.
        /// </summary>
        public double StatStorageFillAndReservedAndBacklogLevel { get { return (StorageUsage + StorageReserved + StorageBacklog) / StorageCapacity; } }
        /// <summary>
        /// The maximal number of items handled by a pod.
        /// </summary>
        public int StatMaxItemsHandledByPod { get; private set; }
        /// <summary>
        /// The maximal number of bundles handled by a pod.
        /// </summary>
        public int StatMaxBundlesHandledByPod { get; private set; }
        /// <summary>
        /// Contains custom info written by the different controllers.
        /// </summary>
        public CustomControllerDatapoint StatCustomControllerInfo { get; private set; } = new CustomControllerDatapoint();

        #endregion

        #region Stat I/O and reset

        /// <summary>
        /// Resets the statistics
        /// </summary>
        public void StatReset()
        {
            // Indicate reset for controlling processes
            StatWarmupResetDone = true;
            // Reset basics
            StatTimeStart = Controller.CurrentTime;
            StatMaxItemsHandledByPod = 0;
            StatMaxBundlesHandledByPod = 0;
            StatBundleGenerationStops = 0;
            StatOrderGenerationStops = 0;
            StatOverallItemsHandled = 0;
            StatOverallBundlesHandled = 0;
            StatOverallLinesHandled = 0;
            StatOverallOrdersHandled = 0;
            StatOverallOrdersLate = 0;
            _statBundleHandlingTimestamps.Clear();
            _statItemHandlingTimestamps.Clear();
            _statOrderHandlingTimestamps.Clear();
            _statBundlePlacementTimestamps.Clear();
            _statOrderPlacementTimestamps.Clear();
            StatOverallCollisions = 0;
            StatOverallFailedReservations = 0;
            StatOverallPathPlanningTimeouts = 0;
            _statCollisionTimestamps.Clear();
            _oStationTripCount = 0;
            _oStationTripTimeAvg = 0;
            _iStationTripCount = 0;
            _iStationTripTimeAvg = 0;
            _statStationTripTimestamps.Clear();
            StatOverallItemsOrdered = 0;
            StatOverallOrdersPlaced = 0;
            StatOverallBundlesPlaced = 0;
            StatOverallOrdersRejected = 0;
            StatOverallBundlesRejected = 0;
            StatRepositioningMoves = 0;
            _statOrderTurnoverTimes.Clear();
            _statOrderThroughputTimes.Clear();
            _statOrderLatenessTimes.Clear();
            _statBundleThroughputTimes.Clear();
            _statBundleTurnoverTimes.Clear();

            // Reset custom controller info
            StatCustomControllerInfo = new CustomControllerDatapoint();
            Controller.MethodManager?.StatReset();
            Controller.StationManager?.StatReset();
            Controller.OrderManager?.StatReset();
            Controller.BundleManager?.StatReset();
            Controller.StorageManager?.StatReset();
            Controller.PodStorageManager?.StatReset();
            Controller.RepositioningManager?.StatReset();
            Controller.BotManager?.StatReset();
            Controller.PathManager?.StatReset();

            // Reset observer
            Observer.Reset();

            // Reset frequency tracker
            FrequencyTracker.Reset();

            // Reset all objects
            foreach (var b in Bots)
                b.ResetStatistics();
            foreach (var ls in InputStations)
                ls.ResetStatistics();
            foreach (var ws in OutputStations)
                ws.ResetStatistics();
            foreach (var b in Pods)
                b.ResetStatistics();
            foreach (var wp in Waypoints)
                wp.ResetStatistics();
            ItemManager.ResetStatistics();

            // Init statistics directory
            StatInitDirectory();

            // Clean up potential previous data
            if (Directory.Exists(SettingConfig.StatisticsDirectory))
            {
                // Delete all stat files one by one (sparing other files in the directory)
                foreach (var statFileName in IOConstants.StatFileNames.Values)
                {
                    string statFilePath = Path.Combine(SettingConfig.StatisticsDirectory, statFileName);
                    if (File.Exists(statFilePath))
                        File.Delete(statFilePath);
                }
            }
        }

        /// <summary>
        /// Finalizes some potentially incomplete statistics.
        /// </summary>
        public void StatFinish()
        {
            // Finalize potentially incomplete trips
            foreach (var bot in Bots)
                bot.LogIncompleteTrip();
            // Submit custom statistics
            Controller.MethodManager?.StatFinish();
            Controller.StationManager?.StatFinish();
            Controller.OrderManager?.StatFinish();
            Controller.BundleManager?.StatFinish();
            Controller.StorageManager?.StatFinish();
            Controller.PodStorageManager?.StatFinish();
            Controller.RepositioningManager?.StatFinish();
            Controller.BotManager?.StatFinish();
            Controller.PathManager?.StatFinish();
        }

        /// <summary>
        /// Flushes the current state of the corresponding statistics to reduce memory usage.
        /// </summary>
        public void StatFlushBundlesHandled()
        {
            // Init statistics directory
            StatInitDirectory();
            // Write bundle-handling progression
            switch (SettingConfig.LogFileLevel)
            {
                case Configurations.LogFileLevel.All:
                    bool alreadyExists = File.Exists(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.BundleProgressionRaw]));
                    using (StreamWriter sw = new StreamWriter(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.BundleProgressionRaw]), true))
                    {
                        if (!alreadyExists)
                            sw.WriteLine(IOConstants.COMMENT_LINE + BundleHandledDatapoint.GetHeader());
                        foreach (var d in _statBundleHandlingTimestamps)
                            sw.WriteLine(d.GetLine());
                    }
                    break;
                case Configurations.LogFileLevel.FootprintOnly:
                    break;
                default: throw new ArgumentException("Unknown log level: " + SettingConfig.LogFileLevel);
            }
            // Clear the data points
            _statBundleHandlingTimestamps.Clear();
        }

        /// <summary>
        /// Flushes the current state of the corresponding statistics to reduce memory usage.
        /// </summary>
        public void StatFlushItemsHandled()
        {
            // Init statistics directory
            StatInitDirectory();
            // Write item-handling progression
            switch (SettingConfig.LogFileLevel)
            {
                case Configurations.LogFileLevel.All:
                    bool alreadyExists = File.Exists(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.ItemProgressionRaw]));
                    using (StreamWriter sw = new StreamWriter(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.ItemProgressionRaw]), true))
                    {
                        if (!alreadyExists)
                            sw.WriteLine(IOConstants.COMMENT_LINE + ItemHandledDatapoint.GetHeader());
                        foreach (var d in _statItemHandlingTimestamps)
                            sw.WriteLine(d.GetLine());
                    }
                    break;
                case Configurations.LogFileLevel.FootprintOnly:
                    break;
                default: throw new ArgumentException("Unknown log level: " + SettingConfig.LogFileLevel);
            }
            // Clear the data points
            _statItemHandlingTimestamps.Clear();
        }

        /// <summary>
        /// Flushes the current state of the corresponding statistics to reduce memory usage.
        /// </summary>
        public void StatFlushOrdersHandled()
        {
            // Init statistics directory
            StatInitDirectory();
            // Write order-handling progression
            switch (SettingConfig.LogFileLevel)
            {
                case Configurations.LogFileLevel.All:
                    bool alreadyExists = File.Exists(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.OrderProgressionRaw]));
                    using (StreamWriter sw = new StreamWriter(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.OrderProgressionRaw]), true))
                    {
                        if (!alreadyExists)
                            sw.WriteLine(IOConstants.COMMENT_LINE + OrderHandledDatapoint.GetHeader());
                        foreach (var d in _statOrderHandlingTimestamps)
                            sw.WriteLine(d.GetLine());
                    }
                    break;
                case Configurations.LogFileLevel.FootprintOnly:
                    break;
                default: throw new ArgumentException("Unknown log level: " + SettingConfig.LogFileLevel);
            }
            // Clear the data points
            _statOrderHandlingTimestamps.Clear();
        }

        /// <summary>
        /// Flushes the current state of the corresponding statistics.
        /// </summary>
        public void StatFlushBundlesPlaced()
        {
            // Init statistics directory
            StatInitDirectory();
            // Write bundle-placement progression
            switch (SettingConfig.LogFileLevel)
            {
                case Configurations.LogFileLevel.All:
                    bool alreadyExists = File.Exists(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.BundlePlacementProgressionRaw]));
                    using (StreamWriter sw = new StreamWriter(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.BundlePlacementProgressionRaw]), true))
                    {
                        if (!alreadyExists)
                            sw.WriteLine(IOConstants.COMMENT_LINE + BundlePlacedDatapoint.GetHeader());
                        foreach (var d in _statBundlePlacementTimestamps)
                            sw.WriteLine(d.GetLine());
                    }
                    break;
                case Configurations.LogFileLevel.FootprintOnly:
                    break;
                default: throw new ArgumentException("Unknown log level: " + SettingConfig.LogFileLevel);
            }
            // Clear the data points
            _statBundlePlacementTimestamps.Clear();
        }

        /// <summary>
        /// Flushes the current state of the corresponding statistics.
        /// </summary>
        public void StatFlushOrdersPlaced()
        {
            // Init statistics directory
            StatInitDirectory();
            // Write order-placement progression
            switch (SettingConfig.LogFileLevel)
            {
                case Configurations.LogFileLevel.All:
                    bool alreadyExists = File.Exists(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.OrderPlacementProgressionRaw]));
                    using (StreamWriter sw = new StreamWriter(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.OrderPlacementProgressionRaw]), true))
                    {
                        if (!alreadyExists)
                            sw.WriteLine(IOConstants.COMMENT_LINE + OrderPlacedDatapoint.GetHeader());
                        foreach (var d in _statOrderPlacementTimestamps)
                            sw.WriteLine(d.GetLine());
                    }
                    break;
                case Configurations.LogFileLevel.FootprintOnly:
                    break;
                default: throw new ArgumentException("Unknown log level: " + SettingConfig.LogFileLevel);
            }
            // Clear the data points
            _statOrderPlacementTimestamps.Clear();
        }

        /// <summary>
        /// Flushes the current state of the corresponding statistics.
        /// </summary>
        private void StatFlushPathFinding()
        {
            if (Controller.PathManager == null || !Controller.PathManager.Log)
                return;

            // Init statistics directory
            StatInitDirectory();
            // Write path finding data
            switch (SettingConfig.LogFileLevel)
            {
                case Configurations.LogFileLevel.All:
                    bool alreadyExists = File.Exists(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.PathFinding]));
                    using (StreamWriter sw = new StreamWriter(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.PathFinding]), true))
                    {
                        if (!alreadyExists)
                            sw.WriteLine(IOConstants.COMMENT_LINE + PathFindingDatapoint.GetHeader());
                        foreach (var d in Controller.PathManager.StatDataPoints)
                            sw.WriteLine(d.GetLine());
                    }
                    break;
                case Configurations.LogFileLevel.FootprintOnly:
                    break;
                default: throw new ArgumentException("Unknown log level: " + SettingConfig.LogFileLevel);
            }
            // Clear the data points
            Controller.PathManager.StatDataPoints.Clear();
        }

        /// <summary>
        /// Flushes the current state of the corresponding statistics to reduce memory usage.
        /// </summary>
        public void StatFlushCollisions()
        {
            // Init statistics directory
            StatInitDirectory();
            // Write collision progression
            switch (SettingConfig.LogFileLevel)
            {
                case Configurations.LogFileLevel.All:
                    bool alreadyExists = File.Exists(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.CollisionProgressionRaw]));
                    using (StreamWriter sw = new StreamWriter(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.CollisionProgressionRaw]), true))
                    {
                        if (!alreadyExists)
                            sw.WriteLine(IOConstants.COMMENT_LINE + CollisionDatapoint.GetHeader());
                        foreach (var d in _statCollisionTimestamps)
                            sw.WriteLine(d.GetLine());
                    }
                    break;
                case Configurations.LogFileLevel.FootprintOnly:
                    break;
                default: throw new ArgumentException("Unknown log level: " + SettingConfig.LogFileLevel);
            }
            // Clear the data points
            _statCollisionTimestamps.Clear();
        }

        /// <summary>
        /// Flushes the current state of the corresponding statistics to reduce memory usage.
        /// </summary>
        public void StatFlushTripsCompleted()
        {
            // Init statistics directory
            StatInitDirectory();
            // Write station trip progression
            switch (SettingConfig.LogFileLevel)
            {
                case Configurations.LogFileLevel.All:
                    bool alreadyExists = File.Exists(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.TripsCompletedProgressionRaw]));
                    using (StreamWriter sw = new StreamWriter(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.TripsCompletedProgressionRaw]), true))
                    {
                        if (!alreadyExists)
                            sw.WriteLine(IOConstants.COMMENT_LINE + StationTripDatapoint.GetHeader());
                        foreach (var d in _statStationTripTimestamps)
                            sw.WriteLine(d.GetLine());
                    }
                    break;
                case Configurations.LogFileLevel.FootprintOnly:
                    break;
                default: throw new ArgumentException("Unknown log level: " + SettingConfig.LogFileLevel);
            }
            // Clear the data points
            _statStationTripTimestamps.Clear();
        }

        /// <summary>
        /// Flushes statistics about the trips of the robots.
        /// </summary>
        public void StatFlushTripStatistics()
        {
            // --> Flush whole trip statistics
            switch (SettingConfig.LogFileLevel)
            {
                case Configurations.LogFileLevel.All:
                    // Get station waypoints
                    List<Waypoint> outputstationWPs = Waypoints.Where(wp => wp.OutputStation != null).ToList();
                    List<Waypoint> inputstationWPs = Waypoints.Where(wp => wp.InputStation != null).ToList();
                    // Create files and flush data
                    using (StreamWriter sw = new StreamWriter(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.HeatTrips]), false))
                    {
                        // Write header
                        sw.WriteLine(TimeIndependentTripDataPoint.GetCSVHeader());
                        // Write datapoints per waypoint
                        foreach (var waypoint in Waypoints)
                        {
                            TimeIndependentTripDataPoint datapoint = new TimeIndependentTripDataPoint()
                            {
                                Tier = waypoint.Tier.ID,
                                X = waypoint.X,
                                Y = waypoint.Y,
                                Overall = Waypoints.Sum(wp => wp.StatContainsTripDataIn(waypoint) ? wp.StatGetTripDataIn(waypoint).Count : 0),
                                ToOStation = outputstationWPs.Sum(o => o.StatContainsTripDataIn(waypoint) ? o.StatGetTripDataIn(waypoint).Count : 0),
                                FromOStation = outputstationWPs.Sum(o => o.StatContainsTripDataOut(waypoint) ? o.StatGetTripDataOut(waypoint).Count : 0),
                                ToIStation = inputstationWPs.Sum(i => i.StatContainsTripDataIn(waypoint) ? i.StatGetTripDataIn(waypoint).Count : 0),
                                FromIStation = inputstationWPs.Sum(i => i.StatContainsTripDataOut(waypoint) ? i.StatGetTripDataOut(waypoint).Count : 0),
                            };
                            sw.WriteLine(datapoint.ToCSV());
                        }
                    }
                    break;
                case Configurations.LogFileLevel.FootprintOnly:
                    break;
                default: throw new ArgumentException("Unknown log level: " + SettingConfig.LogFileLevel);
            }
        }

        /// <summary>
        /// Flushes all data of all connections that have been used.
        /// </summary>
        public void StatFlushConnectionStatistics()
        {
            switch (SettingConfig.LogFileLevel)
            {
                case Configurations.LogFileLevel.All:
                    using (StreamWriter sw = new StreamWriter(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.ConnectionStatistics]), false))
                    {
                        sw.WriteLine(IOConstants.COMMENT_LINE + ConnectionStatisticsDataPoint.GetStringTupleRepresentationDescription());
                        foreach (var from in Waypoints)
                            foreach (var to in Waypoints)
                                if (from.StatContainsTripDataOut(to))
                                    sw.WriteLine(from.StatGetTripDataOut(to).GetStringTupleRepresentation());
                    }
                    break;
                case Configurations.LogFileLevel.FootprintOnly:
                    break;
                default: throw new ArgumentException("Unknown log level: " + SettingConfig.LogFileLevel);
            }
        }

        /// <summary>
        /// Initializes the statistics directory.
        /// </summary>
        internal void StatInitDirectory()
        {
            // Create a default statistics directory name if none is given
            if (string.IsNullOrWhiteSpace(SettingConfig.StatisticsDirectory))
                SettingConfig.StatisticsDirectory = GetMetaInfoBasedInstanceName() + "-" + ControllerConfig.GetMetaInfoBasedConfigName() + "-" + SettingConfig.Seed;
            // Create the new and empty directory
            if (!Directory.Exists(SettingConfig.StatisticsDirectory))
                Directory.CreateDirectory(SettingConfig.StatisticsDirectory);
        }

        /// <summary>
        /// Writes and flushes all statistics to the directory specified in the configuration.
        /// </summary>
        public void WriteStatistics()
        {
            // Indicate stats written for controlling processes
            StatResultsWritten = true;

            // Finalize statistics
            StatFinish();

            // Init statistics directory
            StatInitDirectory();

            // Write footprint
            using (StreamWriter sw = new StreamWriter(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.Footprint])))
                // Write stat line
                sw.WriteLine(new FootprintDatapoint(this).GetFootprint());

            // Write further statistics
            switch (SettingConfig.LogFileLevel)
            {
                case Configurations.LogFileLevel.All:
                    // Write readable statistics
                    using (StreamWriter sw = new StreamWriter(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.ReadableStatistics])))
                        PrintStatistics(sw.WriteLine, detailedAll: true);
                    // Write station statistics
                    using (StreamWriter sw = new StreamWriter(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.StationStatistics])))
                        WriteStationStatistics(sw.Write);
                    // Write item descriptions statistics
                    using (StreamWriter sw = new StreamWriter(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.ItemDescriptionStatistics])))
                        WriteItemDescriptionStatistics(sw.Write);
                    break;
                case Configurations.LogFileLevel.FootprintOnly:
                    break;
                default: throw new ArgumentException("Unknown log level: " + SettingConfig.LogFileLevel);
            }

            // Write instance name
            using (StreamWriter sw = new StreamWriter(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.InstanceName])))
                sw.Write((string.IsNullOrWhiteSpace(this.Name) ? this.GetMetaInfoBasedInstanceName() : this.Name));

            // Write setting name
            using (StreamWriter sw = new StreamWriter(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.SettingName])))
                sw.Write(SettingConfig.Name);

            // Write controller name
            using (StreamWriter sw = new StreamWriter(Path.Combine(SettingConfig.StatisticsDirectory, IOConstants.StatFileNames[IOConstants.StatFile.ControllerName])))
                sw.Write(ControllerConfig.Name);

            // Flush the data
            StatFlushBundlesHandled();
            StatFlushItemsHandled();
            StatFlushOrdersHandled();
            StatFlushBundlesPlaced();
            StatFlushOrdersPlaced();
            StatFlushCollisions();
            StatFlushTripsCompleted();
            StatFlushPathFinding();

            // Flush observer data
            Observer.FlushData();

            // Flush controller dependant performance information
            WriteIndividiualStatistics();

            // Flush trip statistics data
            StatFlushTripStatistics();
            StatFlushConnectionStatistics();
        }

        /// <summary>
        /// Writes basic statistics about the stations in a CSV manner.
        /// </summary>
        /// <param name="writer">The write action to use.</param>
        public void WriteStationStatistics(Action<string> writer)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(
                "Ident" + IOConstants.DELIMITER_VALUE +
                "X" + IOConstants.DELIMITER_VALUE +
                "Y" + IOConstants.DELIMITER_VALUE +
                "Transfers" + IOConstants.DELIMITER_VALUE +
                "InjectedTransfers" + IOConstants.DELIMITER_VALUE +
                "PodsHandled" + IOConstants.DELIMITER_VALUE +
                "PodHandlingTimeAvg" + IOConstants.DELIMITER_VALUE +
                "PodHandlingTimeVar" + IOConstants.DELIMITER_VALUE +
                "PodHandlingTimeMin" + IOConstants.DELIMITER_VALUE +
                "PodHandlingTimeMax" + IOConstants.DELIMITER_VALUE +
                "PileOn" + IOConstants.DELIMITER_VALUE +
                "IdleTime" + IOConstants.DELIMITER_VALUE +
                "UpTime");
            foreach (var station in InputStations)
            {
                sb.AppendLine(
                    station.GetIdentfierString() + IOConstants.DELIMITER_VALUE +
                    station.X.ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                    station.Y.ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                    station.StatNumBundlesStored.ToString() + IOConstants.DELIMITER_VALUE +
                    station.StatNumInjectedBundlesStored.ToString() + IOConstants.DELIMITER_VALUE +
                    station.StatPodsHandled.ToString() + IOConstants.DELIMITER_VALUE +
                    station.StatPodHandlingTimeAvg.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                    station.StatPodHandlingTimeVar.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                    station.StatPodHandlingTimeMin.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                    station.StatPodHandlingTimeMax.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                    station.StatBundlePileOn.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                    station.StatIdleTime.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                    station.StatActiveTime.ToString(IOConstants.FORMATTER));
            }
            foreach (var station in OutputStations)
            {
                sb.AppendLine(
                    station.GetIdentfierString() + IOConstants.DELIMITER_VALUE +
                    station.X.ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                    station.Y.ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                    station.StatNumItemsPicked.ToString() + IOConstants.DELIMITER_VALUE +
                    station.StatNumInjectedItemsPicked.ToString() + IOConstants.DELIMITER_VALUE +
                    station.StatPodsHandled.ToString() + IOConstants.DELIMITER_VALUE +
                    station.StatPodHandlingTimeAvg.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                    station.StatPodHandlingTimeVar.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                    station.StatPodHandlingTimeMin.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                    station.StatPodHandlingTimeMax.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                    station.StatItemPileOn.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                    station.StatIdleTime.ToString(IOConstants.FORMATTER) + IOConstants.DELIMITER_VALUE +
                    station.StatActiveTime.ToString(IOConstants.FORMATTER));
            }
            writer(sb.ToString());
        }

        /// <summary>
        /// Writes basic information about the SKUs and how they were ordered during simulation.
        /// </summary>
        /// <param name="writer">The writer to use.</param>
        public void WriteItemDescriptionStatistics(Action<string> writer)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(IOConstants.COMMENT_LINE + ItemDescriptionFrequencyDatapoint.GetHeader());
            // Output all item descriptions
            foreach (var itemDescription in ItemDescriptions.OrderBy(i => i.ID))
                sb.AppendLine(new ItemDescriptionFrequencyDatapoint(itemDescription, FrequencyTracker).GetLine());
            // Write it
            writer(sb.ToString());
        }

        /// <summary>
        /// Prints a statistics overview in readable format to a specified action.
        /// </summary>
        /// <param name="writer">The action to print the statistics to.</param>
        /// <param name="detailedAll">Indicates whether to print detailed or short statistics.</param>
        /// <param name="detailedBots">Indicates whether to print detailed statistics about the bots.</param>
        /// <param name="detailedPods">Indicates whether to print detailed statistics about the pods.</param>
        /// <param name="detailedStations">Indicates whether to print detailed statistics about the stations.</param>
        public void PrintStatistics(Action<string> writer, bool detailedAll = false, bool detailedBots = false, bool detailedPods = false, bool detailedStations = false)
        {
            StringBuilder sb = new StringBuilder();
            if (detailedAll || detailedBots)
            {
                sb.AppendLine(">>> Bots");
                foreach (var bot in Bots)
                {
                    sb.AppendLine(bot.ToString() + ":");
                    sb.AppendLine("DistanceTraveled: " + bot.StatDistanceTraveled);
                    sb.AppendLine("DistanceEstimated: " + bot.StatDistanceEstimated);
                    sb.AppendLine("NumberOfPickups: " + bot.StatNumberOfPickups);
                    sb.AppendLine("NumberOfSetdowns: " + bot.StatNumberOfSetdowns);
                    sb.AppendLine("NumCollisions: " + bot.StatNumCollisions);
                    sb.AppendLine("TotalTimeMoving: " + bot.StatTotalTimeMoving.ToString(IOConstants.FORMATTER));
                    //sb.AppendLine("TaskStartTime: " + bot.StatTaskStartTime);
                    foreach (var taskType in Enum.GetValues(typeof(BotTaskType)).Cast<BotTaskType>())
                        if (bot.StatTotalTaskTimes.ContainsKey(taskType))
                            sb.AppendLine(taskType + ": " + bot.StatTotalTaskTimes[taskType].ToString(IOConstants.FORMATTER));
                    foreach (var stateType in Enum.GetValues(typeof(BotStateType)).Cast<BotStateType>())
                        if (bot.StatTotalStateTimes.ContainsKey(stateType))
                            sb.AppendLine(stateType + ": " + bot.StatTotalStateTimes[stateType].ToString(IOConstants.FORMATTER));
                }
            }
            if (detailedAll || detailedPods)
            {
                sb.AppendLine(">>> Pods");
                foreach (var pod in Pods)
                {
                    sb.AppendLine(pod.ToString() + ":");
                    string itemDescriptions = "";
                    foreach (var description in pod.ItemDescriptionsContained)
                        itemDescriptions += description.ToString() + " ";
                    sb.AppendLine(itemDescriptions);
                    sb.AppendLine("StatItemsHandledAtOutputStations: " + pod.StatItemsHandled);
                    sb.AppendLine("StatBundlesHandledAtInputStations: " + pod.StatBundlesHandled);
                }
            }
            if (detailedAll || detailedStations)
            {
                sb.AppendLine(">>> InputStations");
                foreach (var iStation in InputStations)
                {
                    sb.AppendLine(iStation.ToString() + ":");
                    sb.AppendLine("IdleTime: " + iStation.StatIdleTime.ToString(IOConstants.FORMATTER));
                    sb.AppendLine("UpTime: " + iStation.StatActiveTime.ToString(IOConstants.FORMATTER));
                    sb.AppendLine("DownTime: " + iStation.StatDownTime.ToString(IOConstants.FORMATTER));
                    sb.AppendLine("NumBundlesPut: " + iStation.StatNumBundlesStored);
                    sb.AppendLine("NumInjectedBundlesPut: " + iStation.StatNumInjectedBundlesStored);
                    sb.AppendLine("BundlePileOn: " + iStation.StatBundlePileOn.ToString(IOConstants.FORMATTER));
                    sb.AppendLine("PodsHandled: " + iStation.StatPodsHandled.ToString());
                    sb.AppendLine("PodHandlingTimeAvg: " + iStation.StatPodHandlingTimeAvg.ToString(IOConstants.FORMATTER));
                    sb.AppendLine("PodHandlingTimeVar: " + iStation.StatPodHandlingTimeVar.ToString(IOConstants.FORMATTER));
                    sb.AppendLine("PodHandlingTimeMin: " + iStation.StatPodHandlingTimeMin.ToString(IOConstants.FORMATTER));
                    sb.AppendLine("PodHandlingTimeMax: " + iStation.StatPodHandlingTimeMax.ToString(IOConstants.FORMATTER));
                }
                sb.AppendLine(">>> OutputStations");
                foreach (var oStation in OutputStations)
                {
                    sb.AppendLine(oStation.ToString() + ":");
                    sb.AppendLine("IdleTime: " + oStation.StatIdleTime.ToString(IOConstants.FORMATTER));
                    sb.AppendLine("UpTime: " + oStation.StatActiveTime.ToString(IOConstants.FORMATTER));
                    sb.AppendLine("DownTime: " + oStation.StatDownTime.ToString(IOConstants.FORMATTER));
                    sb.AppendLine("NumItemsPicked: " + oStation.StatNumItemsPicked);
                    sb.AppendLine("NumInjectedItemsPicked: " + oStation.StatNumInjectedItemsPicked);
                    sb.AppendLine("NumOrdersFinished: " + oStation.StatNumOrdersFinished);
                    sb.AppendLine("ItemPileOn: " + oStation.StatItemPileOn.ToString(IOConstants.FORMATTER));
                    sb.AppendLine("OrderPileOn: " + oStation.StatOrderPileOn.ToString(IOConstants.FORMATTER));
                    sb.AppendLine("PodsHandled: " + oStation.StatPodsHandled.ToString());
                    sb.AppendLine("PodHandlingTimeAvg: " + oStation.StatPodHandlingTimeAvg.ToString(IOConstants.FORMATTER));
                    sb.AppendLine("PodHandlingTimeVar: " + oStation.StatPodHandlingTimeVar.ToString(IOConstants.FORMATTER));
                    sb.AppendLine("PodHandlingTimeMin: " + oStation.StatPodHandlingTimeMin.ToString(IOConstants.FORMATTER));
                    sb.AppendLine("PodHandlingTimeMax: " + oStation.StatPodHandlingTimeMax.ToString(IOConstants.FORMATTER));
                }
            }
            sb.AppendLine(">>> Timings");
            sb.AppendLine("StatTimingPathPlanningAverage: " + Observer.TimingPathPlanningAverage.ToString(IOConstants.FORMATTER));
            sb.AppendLine("StatTimingPathPlanningOverall: " + Observer.TimingPathPlanningOverall.ToString(IOConstants.FORMATTER));
            sb.AppendLine("StatTimingPathPlanningCount: " + Observer.TimingPathPlanningDecisionCount.ToString(IOConstants.FORMATTER));
            sb.AppendLine("StatTimingTaskAllocationAverage: " + Observer.TimingTaskAllocationAverage.ToString(IOConstants.FORMATTER));
            sb.AppendLine("StatTimingTaskAllocationOverall: " + Observer.TimingTaskAllocationOverall.ToString(IOConstants.FORMATTER));
            sb.AppendLine("StatTimingTaskAllocationCount: " + Observer.TimingTaskAllocationDecisionCount.ToString(IOConstants.FORMATTER));
            sb.AppendLine("StatTimingItemStorageAverage: " + Observer.TimingItemStorageAverage.ToString(IOConstants.FORMATTER));
            sb.AppendLine("StatTimingItemStorageOverall: " + Observer.TimingItemStorageOverall.ToString(IOConstants.FORMATTER));
            sb.AppendLine("StatTimingItemStorageCount: " + Observer.TimingItemStorageDecisionCount.ToString(IOConstants.FORMATTER));
            sb.AppendLine("StatTimingPodStorageAverage: " + Observer.TimingPodStorageAverage.ToString(IOConstants.FORMATTER));
            sb.AppendLine("StatTimingPodStorageOverall: " + Observer.TimingPodStorageOverall.ToString(IOConstants.FORMATTER));
            sb.AppendLine("StatTimingPodStorageCount: " + Observer.TimingPodStorageDecisionCount.ToString(IOConstants.FORMATTER));
            sb.AppendLine("StatTimingReplenishmentBatchingAverage: " + Observer.TimingReplenishmentBatchingAverage.ToString(IOConstants.FORMATTER));
            sb.AppendLine("StatTimingReplenishmentBatchingOverall: " + Observer.TimingReplenishmentBatchingOverall.ToString(IOConstants.FORMATTER));
            sb.AppendLine("StatTimingReplenishmentBatchingCount: " + Observer.TimingReplenishmentBatchingDecisionCount.ToString(IOConstants.FORMATTER));
            sb.AppendLine("StatTimingOrderBatchingAverage: " + Observer.TimingOrderBatchingAverage.ToString(IOConstants.FORMATTER));
            sb.AppendLine("StatTimingOrderBatchingOverall: " + Observer.TimingOrderBatchingOverall.ToString(IOConstants.FORMATTER));
            sb.AppendLine("StatTimingOrderBatchingCount: " + Observer.TimingOrderBatchingDecisionCount.ToString(IOConstants.FORMATTER));
            sb.AppendLine(">>> Overall");
            sb.AppendLine("StatOverallBundlesPlaced: " + StatOverallBundlesPlaced);
            sb.AppendLine("StatOverallItemsOrdered: " + StatOverallItemsOrdered);
            sb.AppendLine("StatOverallOrdersPlaced: " + StatOverallOrdersPlaced);
            sb.AppendLine("StatOverallBundlesRejected: " + StatOverallBundlesRejected);
            sb.AppendLine("StatOverallOrdersRejected: " + StatOverallOrdersRejected);
            sb.AppendLine("StatOverallBundlesHandled: " + StatOverallBundlesHandled);
            sb.AppendLine("StatOverallItemsHandled: " + StatOverallItemsHandled);
            sb.AppendLine("StatOverallLinesHandled: " + StatOverallLinesHandled);
            sb.AppendLine("StatOverallOrdersHandled: " + StatOverallOrdersHandled);
            sb.AppendLine("StatOverallCollisions: " + StatOverallCollisions);
            sb.AppendLine("StatOverallDistanceTraveled: " + StatOverallDistanceTraveled.ToString(IOConstants.FORMATTER));
            sb.AppendLine("StatOverallDistanceEstimated: " + StatOverallDistanceEstimated.ToString(IOConstants.FORMATTER));
            sb.AppendLine("StatOverallAssignedTasks: " + StatOverallAssignedTasks);
            sb.AppendLine("StatMaxMemoryUsed: " + StatMaxMemoryUsed);
            sb.AppendLine("StatRealTimeUsed: " + ((SettingConfig.StartTime != default(DateTime) && SettingConfig.StopTime != default(DateTime)) ? (SettingConfig.StopTime - SettingConfig.StartTime).TotalSeconds.ToString(IOConstants.FORMATTER) : "0"));
            sb.AppendLine("StatAverageTurnoverTime: " + ((_statOrderTurnoverTimes.Count == 0) ? "0" : _statOrderTurnoverTimes.Average().ToString(IOConstants.FORMATTER)));
            sb.AppendLine("StatMedianTurnoverTime: " + ((_statOrderTurnoverTimes.Count == 0) ? "0" : StatisticsHelper.GetMedian(_statOrderTurnoverTimes).ToString(IOConstants.FORMATTER)));
            sb.AppendLine("StatLowerQuartileTurnoverTime: " + ((_statOrderTurnoverTimes.Count == 0) ? "0" : StatisticsHelper.GetLowerQuartile(_statOrderTurnoverTimes).ToString(IOConstants.FORMATTER)));
            sb.AppendLine("StatUpperQuartileTurnoverTime: " + ((_statOrderTurnoverTimes.Count == 0) ? "0" : StatisticsHelper.GetUpperQuartile(_statOrderTurnoverTimes).ToString(IOConstants.FORMATTER)));
            sb.AppendLine("StatAverageThroughputTime: " + ((_statOrderThroughputTimes.Count == 0) ? "0" : _statOrderThroughputTimes.Average().ToString(IOConstants.FORMATTER)));
            sb.AppendLine("StatMedianThroughputTime: " + ((_statOrderThroughputTimes.Count == 0) ? "0" : StatisticsHelper.GetMedian(_statOrderThroughputTimes).ToString(IOConstants.FORMATTER)));
            sb.AppendLine("StatLowerQuartileThroughputTime: " + ((_statOrderThroughputTimes.Count == 0) ? "0" : StatisticsHelper.GetLowerQuartile(_statOrderThroughputTimes).ToString(IOConstants.FORMATTER)));
            sb.AppendLine("StatUpperQuartileThroughputTime: " + ((_statOrderThroughputTimes.Count == 0) ? "0" : StatisticsHelper.GetUpperQuartile(_statOrderThroughputTimes).ToString(IOConstants.FORMATTER)));
            // Write output
            writer(sb.ToString());
        }

        /// <summary>
        /// Flushes individual statistics, if the corresponding controllers are present.
        /// </summary>
        private void WriteIndividiualStatistics()
        {
            // Nothing to see here currently (experimental methods using this have been removed)
            // You may use this for dumping additional statistics for custom controllers (just check whether your controller was running)
            // e.g.: if(ControllerConfig.OrderBatchingConfig.GetMethodType() == Configurations.OrderBatchingMethodType.SimpleSavings) DumpStatisticsOfSimpleSavings
        }

        #endregion
    }
}
