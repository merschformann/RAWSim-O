using RAWSimO.Core.Bots;
using RAWSimO.Core.Configurations;
using RAWSimO.Core.Control;
using RAWSimO.Core.Control.Defaults.TaskAllocation;
using RAWSimO.Core.Control.Shared;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Info;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.IO;
using RAWSimO.Core.Items;
using RAWSimO.Core.Management;
using RAWSimO.Core.Remote;
using RAWSimO.Core.Statistics;
using RAWSimO.Core.Waypoints;
using RAWSimO.Toolbox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RAWSimO.Core
{
    /// THIS PARTIAL CLASS CONTAINS ADDITIONAL FIELDS AND META-INFORMATION
    /// <summary>
    /// The core element of each simulation instance.
    /// </summary>
    public partial class Instance : IInstanceInfo
    {
        /// <summary>
        /// This value is set to <code>true</code> whenever a change happened in the simulation that makes a visual refresh necessary.
        /// </summary>
        public bool Changed { get; set; }

        /// <summary>
        /// The randomizer instance used throughout the simulation.
        /// </summary>
        public IRandomizer Randomizer { get; set; }

        /// <summary>
        /// The waypoint-graph containing all waypoints.
        /// </summary>
        public WaypointGraph WaypointGraph = new WaypointGraph();

        /// <summary>
        /// Keeps track of potential collisions and checks whether they were problematic or just a cause of the asynchronous update of the robots.
        /// </summary>
        internal BotCrashHandler BotCrashHandler { get; set; }

        /// <summary>
        /// Manages certain resources like pods in use and available storage positions.
        /// </summary>
        public ResourceManager ResourceManager { get; set; }

        /// <summary>
        /// The entitiy managing all items and incoming orders.
        /// </summary>
        public ItemManager ItemManager { get; set; }

        /// <summary>
        /// The simulation controller.
        /// </summary>
        public Controller Controller { get; set; }

        /// <summary>
        /// The shared control elements container.
        /// </summary>
        internal SharedControlElementsContainer SharedControlElements { get; private set; }

        /// <summary>
        /// Activates real-world integration and returns an adapter that can be used for communication with the outer world.
        /// </summary>
        /// <returns>The adapter.</returns>
        public RemoteControlAdapter ActivateRemoteController()
        {
            SettingConfig.RealWorldIntegrationCommandOutput = true;
            SettingConfig.RealWorldIntegrationEventDriven = true;
            RemoteController = new RemoteControlAdapter(this) { };
            return RemoteController;
        }

        /// <summary>
        /// Deactivates real-world integration.
        /// </summary>
        public void DeactivateRemoteController()
        {
            SettingConfig.RealWorldIntegrationCommandOutput = false;
            SettingConfig.RealWorldIntegrationEventDriven = false;
            RemoteController = null;
        }

        /// <summary>
        /// A remote control adapter enabling communication with a 'not-simulated world'.
        /// </summary>
        public RemoteControlAdapter RemoteController { get; private set; }

        /// <summary>
        /// All elements that have to be updated in each simulation step.
        /// </summary>
        private List<IUpdateable> _updateables;
        /// <summary>
        /// Removes an updateable object from the list.
        /// </summary>
        /// <param name="updateable">The object to remove</param>
        internal void RemoveUpdateable(IUpdateable updateable) { _updateables.Remove(updateable); }
        /// <summary>
        /// Add an updateable object to the list.
        /// </summary>
        /// <param name="updateable">The object to add.</param>
        internal void AddUpdateable(IUpdateable updateable) { _updateables.Add(updateable); }
        /// <summary>
        /// All elements that have to be updated in each simulation step.
        /// </summary>
        public IEnumerable<IUpdateable> Updateables
        {
            get
            {
                if (_updateables == null)
                {
                    // Set updateables
                    _updateables = new List<IUpdateable>();
                    _updateables.Add(ResourceManager);
                    _updateables.Add(Compound);
                    _updateables.Add(Controller.OrderManager);
                    _updateables.Add(Controller.Allocator);
                    _updateables.Add(Controller.BundleManager);
                    _updateables.Add(Controller.Allocator);
                    _updateables.Add(Controller.BotManager);
                    _updateables.Add(Controller.StationManager);
                    _updateables.Add(Controller.PodStorageManager);
                    _updateables.Add(Controller.RepositioningManager);
                    _updateables.Add(Controller.StorageManager);
                    if (Controller.PathManager != null)
                        _updateables.Add(Controller.PathManager);
                    _updateables.Add(SharedControlElements);
                    _updateables.Add(ItemManager);
                    _updateables.Add(Observer);
                    _updateables.AddRange(Bots);
                    _updateables.AddRange(InputStations);
                    _updateables.AddRange(OutputStations);
                    _updateables.Add(BotCrashHandler);
                }
                return _updateables;
            }
        }

        /// <summary>
        /// Stores the current time as the reference time for the start of the execution.
        /// </summary>
        public void StartExecutionTiming() { SettingConfig.StartTime = DateTime.Now; StartAsyncLogger(); }

        /// <summary>
        /// Stores the current time as the reference time for the finish of the execution.
        /// </summary>
        public void StopExecutionTiming() { SettingConfig.StopTime = DateTime.Now; StopAsyncLogger(); }

        /// <summary>
        /// Returns a simple instance describing name.
        /// </summary>
        /// <returns>A string that can be used as an instance name.</returns>
        public string GetMetaInfoBasedInstanceName()
        {
            string delimiter = "-";
            return Compound.Tiers.Count + delimiter + InputStations.Count + delimiter + OutputStations.Count + delimiter + Bots.Count + delimiter + Pods.Count;
        }

        #region Logging

        /// <summary>
        /// The timer used for async logging.
        /// </summary>
        private Timer _timedLogger;
        /// <summary>
        /// Indicates whether the first timed log was already written.
        /// </summary>
        private bool _timedLogStarted;
        /// <summary>
        /// Starts async logging.
        /// </summary>
        private void StartAsyncLogger() { _timedLogger = new Timer(TimedLog, null, 0, 5000); }
        /// <summary>
        /// Stops async logging.
        /// </summary>
        private void StopAsyncLogger() { if (_timedLogger != null) { _timedLogger.Change(Timeout.Infinite, Timeout.Infinite); TimedLog(null); } }
        /// <summary>
        /// The separator used for the columns in logging.
        /// </summary>
        private string _loggingColumnSep = "|";
        /// <summary>
        /// Returns the header for the time info.
        /// </summary>
        /// <returns>The time info.</returns>
        private string LoggingGetRealTimeHead() { return "Realtime".PadBoth(10); }
        /// <summary>
        /// Returns the time info.
        /// </summary>
        /// <returns>The time info.</returns>
        private string LoggingGetRealTimeValue()
        {
            return
                // Show time spent for simulation so far (if available)
                (SettingConfig.StartTime != DateTime.MinValue ?
                    (DateTime.Now - SettingConfig.StartTime).ToString("hh\\:mm\\:ss") :
                    DateTime.Now.ToString("HH:mm:ss")).PadBoth(10);
        }
        /// <summary>
        /// Returns the header for the ETA info.
        /// </summary>
        /// <returns>The ETA info.</returns>
        private string LoggingGetETATimeHead() { return "ETA".PadBoth(10); }
        /// <summary>
        /// Returns the ETA info.
        /// </summary>
        /// <returns>The ETA info.</returns>
        private string LoggingGetETATimeValue()
        {
            return
                // Show estimated time of accomplishment (not estimated time of arrival :) ) (if available)
                (SettingConfig.StartTime != DateTime.MinValue && Controller.Progress > 0 ?
                    // Cope with overflows above 24 hours (we cannot show them)
                    ((DateTime.Now - SettingConfig.StartTime).TotalSeconds / Controller.Progress * (1 - Controller.Progress)) < TimeSpan.FromDays(1).TotalSeconds ?
                        TimeSpan.FromSeconds((DateTime.Now - SettingConfig.StartTime).TotalSeconds / Controller.Progress * (1 - Controller.Progress)).ToString("hh\\:mm\\:ss") :
                        "> 24h" :
                    "n/a").PadBoth(10);
        }
        /// <summary>
        /// Returns the header for the memory info.
        /// </summary>
        /// <returns>The memory info header.</returns>
        private string LoggingGetMemoryHead() { return "Memory".PadBoth(13); }
        /// <summary>
        /// Returns the memory info.
        /// </summary>
        /// <returns>The memory info.</returns>
        private string LoggingGetMemoryValue()
        {
            return ((System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0).ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "MB").PadBoth(11);
        }
        /// <summary>
        /// Method that is called by the async logging timer.
        /// </summary>
        /// <param name="state">The state (not used).</param>
        private void TimedLog(object state)
        {
            if (SettingConfig.LogAction != null && SettingConfig.LogLevel > LogLevel.Silent)
            {
                // Write headline if not already done
                if (!_timedLogStarted)
                {
                    string header =
                        "Sim.Time".PadBoth(12) + _loggingColumnSep +
                        "Progress".PadBoth(10) + _loggingColumnSep +
                        "#Orders".PadBoth(9) + _loggingColumnSep +
                        "#Bundles".PadBoth(10) + _loggingColumnSep +
                        "Inv.".PadBoth(6) + _loggingColumnSep +
                        "Distance".PadBoth(10) + _loggingColumnSep +
                        "#Coll.".PadBoth(8);
                    switch (SettingConfig.DebugMode)
                    {
                        case DebugMode.RealTime:
                            header =
                               LoggingGetRealTimeHead() + _loggingColumnSep +
                               LoggingGetETATimeHead() + _loggingColumnSep +
                               header;
                            break;
                        case DebugMode.RealTimeAndMemory:
                            header =
                                LoggingGetRealTimeHead() + _loggingColumnSep +
                                LoggingGetETATimeHead() + _loggingColumnSep +
                                header + _loggingColumnSep +
                                LoggingGetMemoryHead();
                            break;
                        default: break;
                    }
                    SettingConfig.LogAction(header);
                    _timedLogStarted = true;
                }
                // Build log message
                string message =
                    TimeSpan.FromSeconds(Controller.CurrentTime).ToString(IOConstants.TIMESPAN_FORMAT_HUMAN_READABLE_DAYS).PadBoth(12) + _loggingColumnSep + // The current simulation time
                    ((Controller.Progress * 100).ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "%").PadBoth(10) + _loggingColumnSep + // The progress of the simulation
                    StatOverallOrdersHandled.ToString().PadBoth(9) + _loggingColumnSep + // The number of handled orders so far
                    StatOverallBundlesHandled.ToString().PadBoth(10) + _loggingColumnSep + // The number of handled bundles so far
                    ((StatStorageFillLevel * 100).ToString("F0", IOConstants.FORMATTER) + "%").PadBoth(6) + _loggingColumnSep + // The current inventory level
                    StatOverallDistanceTraveled.ToString("F0", IOConstants.FORMATTER).PadBoth(10) + _loggingColumnSep + // The distance traveled so far
                    StatOverallCollisions.ToString().PadBoth(8); // The number of collisions that happened so far

                switch (SettingConfig.DebugMode)
                {
                    case DebugMode.RealTime:
                        SettingConfig.LogAction(
                            // Add time info
                            LoggingGetRealTimeValue() + _loggingColumnSep +
                            // Add ETA info
                            LoggingGetETATimeValue() + _loggingColumnSep +
                            // Actually add the message
                            message);
                        break;
                    case DebugMode.RealTimeAndMemory:
                        SettingConfig.LogAction(
                            // Add time info
                            LoggingGetRealTimeValue() + _loggingColumnSep +
                            // Add ETA info
                            LoggingGetETATimeValue() + _loggingColumnSep +
                            // Actually add the message
                            message + _loggingColumnSep +
                            // Add memory info
                            LoggingGetMemoryValue());
                        break;
                    default: SettingConfig.LogAction(message); break;
                }
            }
        }

        /// <summary>
        /// The method to call in case of logging.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void Log(string message)
        {
            if (SettingConfig.LogAction != null && SettingConfig.LogLevel > LogLevel.Silent)
                if (SettingConfig.DebugMode == DebugMode.RealTimeAndMemory)
                    SettingConfig.LogAction(
                        // Add time info (skip memory info for these log messages)
                        LoggingGetRealTimeValue() + _loggingColumnSep + " " +
                        // Actually add the message
                        message);
                else
                    if (SettingConfig.DebugMode == DebugMode.RealTime)
                    SettingConfig.LogAction(
                        // Add time info
                        LoggingGetRealTimeValue() + _loggingColumnSep + " " +
                        // Actually add the message
                        message);
        }
        /// <summary>
        /// Used to log severe errors. (This is log level 1)
        /// </summary>
        /// <param name="message">The message to log.</param>
        internal void LogSevere(string message) { if (SettingConfig.LogLevel >= LogLevel.Severe) Log(message); }
        /// <summary>
        /// Used to log standard messages of the simulation progress or severe warnings / bug info. (This is log level 2)
        /// </summary>
        /// <param name="message">The message to log.</param>
        internal void LogDefault(string message) { if (SettingConfig.LogLevel >= LogLevel.Default) Log(message); }
        /// <summary>
        /// Used to log informative messages about things potentially going wrong or the state of the system. (This is log level 3)
        /// </summary>
        /// <param name="message">The message to log.</param>
        internal void LogInfo(string message) { if (SettingConfig.LogLevel >= LogLevel.Info) Log(message); }
        /// <summary>
        /// Used to log everything that can be logged. (This is log level 4)
        /// </summary>
        /// <param name="message">The message to log.</param>
        internal void LogVerbose(string message) { if (SettingConfig.LogLevel >= LogLevel.Verbose) Log(message); }

        #endregion

        #region Meta information

        /// <summary>
        /// A tag that can be set to better identify an execution of the simulation.
        /// </summary>
        public string Tag { get; set; } = "n.a.";

        /// <summary>
        /// An observer instance monitoring statistics of this instance.
        /// </summary>
        internal SimulationObserver Observer { get; set; }

        /// <summary>
        /// The tracker that keeps all frequency information up-to-date and accessible at one single object.
        /// </summary>
        internal FrequencyTracker FrequencyTracker { get; set; }

        /// <summary>
        /// Keeps track of pod meta info.
        /// </summary>
        internal ElementMetaInfoTracker ElementMetaInfoTracker { get; set; }

        /// <summary>
        /// Supplies information about the current stock situation.
        /// </summary>
        public StockInformation StockInfo { get; private set; }

        /// <summary>
        /// A manager keeping track of instance specific meta-information.
        /// </summary>
        internal MetaInformationManager MetaInfoManager { get; set; }

        /// <summary>
        /// Gets the number of robots currently assigned to the given station. Does only work for the balanced bot manager. For others it will only return 0.
        /// </summary>
        /// <param name="station">The station to get the number for.</param>
        /// <returns>The number of bots currently assigned to the station or always 0 if the bot manager does not support these assignments.</returns>
        internal int StatGetInfoBalancedBotsPerStation(InputStation station)
        {
            return
                Controller.BotManager is BalancedBotManager ? (Controller.BotManager as BalancedBotManager).GetAssignedBotCount(station) :
                Controller.BotManager is ConstantRatioBotManager ? (Controller.BotManager as ConstantRatioBotManager).GetAssignedBotCount(station) :
                0;
        }
        /// <summary>
        /// Gets the number of robots currently assigned to the given station. Does only work for the balanced bot manager. For others it will only return 0.
        /// </summary>
        /// <param name="station">The station to get the number for.</param>
        /// <returns>The number of bots currently assigned to the station or always 0 if the bot manager does not support these assignments.</returns>
        internal int StatGetInfoBalancedBotsPerStation(OutputStation station)
        {
            return
                Controller.BotManager is BalancedBotManager ? (Controller.BotManager as BalancedBotManager).GetAssignedBotCount(station) :
                Controller.BotManager is ConstantRatioBotManager ? (Controller.BotManager as ConstantRatioBotManager).GetAssignedBotCount(station) :
                0;
        }

        /// <summary>
        /// The penalty distance to add for a node not residing on the destination tier.
        /// </summary>
        private double _wrongTierPenaltyDistance = double.NaN;

        /// <summary>
        /// The penalty distance to add for a node not residing on the destination tier.
        /// </summary>
        public double WrongTierPenaltyDistance
        {
            get
            {
                if (double.IsNaN(_wrongTierPenaltyDistance)) { _wrongTierPenaltyDistance = Compound.Tiers.Max(t => t.Length + t.Width) + 1; }
                return _wrongTierPenaltyDistance;
            }
        }

        /// <summary>
        /// The penalty time for a bot for which a station just refused a request.
        /// </summary>
        public double RefusedRequestPenaltyTime { get; private set; } = 0.1;

        /// <summary>
        /// The tolerance for a straight path in radians.
        /// </summary>
        public double StraightOrientationTolerance { get; private set; } = 0.174533;

        #endregion

        #region IInstanceInfo Members

        /// <summary>
        /// Returns an enumeration of all pods of this instance.
        /// </summary>
        /// <returns>All pods of this instance.</returns>
        public IEnumerable<IPodInfo> GetInfoPods() { return Pods; }
        /// <summary>
        /// Returns an enumeration of all bots of this instance.
        /// </summary>
        /// <returns>All bots of this instance.</returns>
        public IEnumerable<IBotInfo> GetInfoBots() { return Bots; }
        /// <summary>
        /// Returns an enumeration of all tiers of this instance.
        /// </summary>
        /// <returns>All tiers of this instance.</returns>
        public IEnumerable<ITierInfo> GetInfoTiers() { return Compound.Tiers; }
        /// <summary>
        /// Returns the elevators connected to this tier.
        /// </summary>
        /// <returns>All elevators connected to this tier.</returns>
        public IEnumerable<IElevatorInfo> GetInfoElevators() { return Elevators; }
        /// <summary>
        /// Indicates whether anything has changed in the instance.
        /// </summary>
        /// <returns><code>false</code> if nothing changed since the last query, <code>true</code> otherwise.</returns>
        public bool GetInfoChanged() { bool was = Changed; Changed = false; return was; }
        /// <summary>
        /// Returns the item manager of this instance.
        /// </summary>
        /// <returns>The item manager of this instance.</returns>
        public IItemManagerInfo GetInfoItemManager() { return ItemManager; }
        /// <summary>
        /// Returns all item descriptions used in the instance.
        /// </summary>
        /// <returns>All item descriptions used by the instance.</returns>
        public IEnumerable<IItemDescriptionInfo> GetInfoItemDescriptions() { return ItemDescriptions; }
        /// <summary>
        /// Returns the count of bundles handled by the system.
        /// </summary>
        /// <returns>The number of bundles handled.</returns>
        public int GetInfoStatItemsHandled() { return StatOverallItemsHandled; }
        /// <summary>
        /// Returns the count of orders handled by the system.
        /// </summary>
        /// <returns>The number of orders handled.</returns>
        public int GetInfoStatBundlesHandled() { return StatOverallBundlesHandled; }
        /// <summary>
        /// Returns the count of orders handled by the system.
        /// </summary>
        /// <returns>The number of orders handled.</returns>
        public int GetInfoStatOrdersHandled() { return StatOverallOrdersHandled; }
        /// <summary>
        /// Returns the count of orders handled that were not completed in time.
        /// </summary>
        /// <returns>The number of orders not completed in time.</returns>
        public int GetInfoStatOrdersLate() { return StatOverallOrdersLate; }
        /// <summary>
        /// Returns the count of repositioning moves started so far.
        /// </summary>
        /// <returns>The number of repositioning moves started.</returns>
        public int GetInfoStatRepositioningMoves() { return StatRepositioningMoves; }
        /// <summary>
        /// Returns the count of occurred collisions.
        /// </summary>
        /// <returns>The number of occurred collisions.</returns>
        public int GetInfoStatCollisions() { return StatOverallCollisions; }
        /// <summary>
        /// Returns the storage fill level.
        /// </summary>
        /// <returns>The storage fill level.</returns>
        public double GetInfoStatStorageFillLevel() { return StatStorageFillLevel; }
        /// <summary>
        /// Returns the storage fill level including the already present reservations.
        /// </summary>
        /// <returns>The storage fill level.</returns>
        public double GetInfoStatStorageFillAndReservedLevel() { return StatStorageFillAndReservedLevel; }
        /// <summary>
        /// Returns the storage fill level including the already present reservations and the capacity consumed by backlog bundles.
        /// </summary>
        /// <returns>The storage fill level.</returns>
        public double GetInfoStatStorageFillAndReservedAndBacklogLevel() { return StatStorageFillAndReservedAndBacklogLevel; }
        /// <summary>
        /// Returns the current name of the instance.
        /// </summary>
        /// <returns>The name of the instance.</returns>
        public string GetInfoName() { return Name; }

        #endregion

        #region ID referencing

        /// <summary>
        /// Stores the corresponding elements by their ID.
        /// </summary>
        private Dictionary<int, Bot> _idToBots = new Dictionary<int, Bot>();

        /// <summary>
        /// Gets the corresponding element by ID.
        /// </summary>
        /// <param name="id">The ID of the element.</param>
        /// <returns>The object matching the ID.</returns>
        public Bot GetBotByID(int id) { return _idToBots[id]; }

        /// <summary>
        /// Stores the corresponding elements by their ID.
        /// </summary>
        private Dictionary<int, Pod> _idToPods = new Dictionary<int, Pod>();

        /// <summary>
        /// Gets the corresponding element by ID.
        /// </summary>
        /// <param name="id">The ID of the element.</param>
        /// <returns>The object matching the ID.</returns>
        public Pod GetPodByID(int id) { return _idToPods[id]; }

        /// <summary>
        /// Stores the corresponding elements by their ID.
        /// </summary>
        private Dictionary<int, InputStation> _idToInputStations = new Dictionary<int, InputStation>();

        /// <summary>
        /// Gets the corresponding element by ID.
        /// </summary>
        /// <param name="id">The ID of the element.</param>
        /// <returns>The object matching the ID.</returns>
        public InputStation GetInputStationByID(int id) { return _idToInputStations[id]; }

        /// <summary>
        /// Stores the corresponding elements by their ID.
        /// </summary>
        private Dictionary<int, OutputStation> _idToOutputStations = new Dictionary<int, OutputStation>();

        /// <summary>
        /// Gets the corresponding element by ID.
        /// </summary>
        /// <param name="id">The ID of the element.</param>
        /// <returns>The object matching the ID.</returns>
        public OutputStation GetOutputStationByID(int id) { return _idToOutputStations[id]; }

        /// <summary>
        /// Stores the corresponding elements by their ID.
        /// </summary>
        private Dictionary<int, Elevator> _idToElevators = new Dictionary<int, Elevator>();

        /// <summary>
        /// Gets the corresponding element by ID.
        /// </summary>
        /// <param name="id">The ID of the element.</param>
        /// <returns>The object matching the ID.</returns>
        public Elevator GetElevatorByID(int id) { return _idToElevators[id]; }

        /// <summary>
        /// Stores the corresponding elements by their ID.
        /// </summary>
        private Dictionary<int, Tier> _idToTier = new Dictionary<int, Tier>();

        /// <summary>
        /// Gets the corresponding element by ID.
        /// </summary>
        /// <param name="id">The ID of the element.</param>
        /// <returns>The object matching the ID.</returns>
        public Tier GetTierByID(int id) { return _idToTier[id]; }

        /// <summary>
        /// Stores the corresponding elements by their ID.
        /// </summary>
        private Dictionary<int, Waypoint> _idToWaypoint = new Dictionary<int, Waypoint>();

        /// <summary>
        /// Gets the corresponding element by ID.
        /// </summary>
        /// <param name="id">The ID of the element.</param>
        /// <returns>The object matching the ID.</returns>
        public Waypoint GetWaypointByID(int id) { return _idToWaypoint[id]; }

        /// <summary>
        /// Stores the corresponding elements by their ID.
        /// </summary>
        private Dictionary<int, QueueSemaphore> _idToSemaphore = new Dictionary<int, QueueSemaphore>();

        /// <summary>
        /// Gets the corresponding element by ID.
        /// </summary>
        /// <param name="id">The ID of the element.</param>
        /// <returns>The object matching the ID.</returns>
        public QueueSemaphore GetSemaphoreByID(int id) { return _idToSemaphore[id]; }

        /// <summary>
        /// Stores the corresponding elements by their ID.
        /// </summary>
        private Dictionary<int, ItemDescription> _idToItemDescription = new Dictionary<int, ItemDescription>();

        /// <summary>
        /// Gets the corresponding element by ID.
        /// </summary>
        /// <param name="id">The ID of the element.</param>
        /// <returns>The object matching the ID.</returns>
        public ItemDescription GetItemDescriptionByID(int id) { return _idToItemDescription[id]; }

        #endregion
    }
}
