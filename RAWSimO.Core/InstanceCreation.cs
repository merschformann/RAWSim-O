using RAWSimO.Core.Configurations;
using RAWSimO.Core.Bots;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Items;
using RAWSimO.Core.Waypoints;
using RAWSimO.MultiAgentPathFinding;
using System;
using System.Linq;
using System.Collections.Generic;

namespace RAWSimO.Core
{
    /// THIS PARTIAL CLASS CONTAINS ALL METHODS AND ADDITIONAL FIELDS FOR CREATION OF NEW ELEMENTS OF THE INSTANCE
    /// <summary>
    /// The core element of each simulation instance.
    /// </summary>
    public partial class Instance
    {
        #region Element creation

        #region Instance

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="settingConfig">The configuration for the setting to emulate.</param>
        /// <param name="controlConfig">The configuration for the controllers.</param>
        /// <returns>The newly created instance.</returns>
        public static Instance CreateInstance(SettingConfiguration settingConfig, ControlConfiguration controlConfig)
        {
            Instance instance = new Instance()
            {
                SettingConfig = (settingConfig != null) ? settingConfig : new SettingConfiguration(),
                ControllerConfig = (controlConfig != null) ? controlConfig : new ControlConfiguration(),
            };
            return instance;
        }

        #endregion

        #region ItemDescription

        /// <summary>
        /// Current ID to identify the corresponding instance element.
        /// </summary>
        private int _itemDescriptionID;
        /// <summary>
        /// Registers and returns a new ID for an object of the given type.
        /// </summary>
        /// <returns>A new unique ID that can be used to identify the object.</returns>
        public int RegisterItemDescriptionID()
        {
            if (ItemDescriptions.Any() && _itemDescriptionID <= ItemDescriptions.Max(e => e.ID)) { _itemDescriptionID = ItemDescriptions.Max(e => e.ID) + 1; }
            return _itemDescriptionID++;
        }
        /// <summary>
        /// All volative IDs used for item descriptions so far.
        /// </summary>
        private HashSet<int> _volatileItemDescriptionIDs = new HashSet<int>();
        /// <summary>
        /// Creates an abstract item description for an item of the specified type.
        /// </summary>
        /// <param name="id">The ID of the item description.</param>
        /// <param name="itemType">The type of the item.</param>
        /// <returns>An abstract item description.</returns>
        public ItemDescription CreateItemDescription(int id, ItemType itemType)
        {
            ItemDescription item = null;
            switch (itemType)
            {
                case ItemType.Letter: { item = new ColoredLetterDescription(this); } break;
                case ItemType.SimpleItem: { item = new SimpleItemDescription(this); } break;
                default: throw new ArgumentException("Unknown item type: " + itemType.ToString());
            }
            item.ID = id;
            item.Instance = this;
            ItemDescriptions.Add(item);
            // Determine volatile ID
            int volatileID = 0;
            while (_volatileItemDescriptionIDs.Contains(volatileID)) { volatileID++; }
            item.VolatileID = volatileID;
            _volatileItemDescriptionIDs.Add(item.VolatileID);
            // Maintain actual ID
            if (_idToItemDescription.ContainsKey(item.ID))
                throw new ArgumentException("Already have an item with this ID: " + id);
            _idToItemDescription[item.ID] = item;
            return item;
        }

        #endregion

        #region ItemBundle (and Item)

        /// <summary>
        /// Current ID to identify the corresponding instance element.
        /// </summary>
        private int _itemBundleID;

        /// <summary>
        /// Creates a bundle of items.
        /// </summary>
        /// <param name="itemDescription">An element describing the characteristics of the item.</param>
        /// <param name="count">The number of items in the bundle.</param>
        /// <returns>A bundle of items.</returns>
        public ItemBundle CreateItemBundle(ItemDescription itemDescription, int count)
        {
            // Create bundle
            ItemBundle bundle = null;
            switch (itemDescription.Type)
            {
                case ItemType.Letter: { bundle = new ColoredLetterBundle(this); } break;
                case ItemType.SimpleItem: { bundle = new SimpleItemBundle(this); } break;
                default: throw new ArgumentException("Unknown item type: " + itemDescription.Type);
            }
            bundle.ID = _itemBundleID++;
            bundle.Instance = this;
            bundle.ItemDescription = itemDescription;
            bundle.ItemCount = count;
            ItemBundles.Add(bundle);
            // Return the filled bundle
            return bundle;
        }

        #endregion

        #region OrderList

        /// <summary>
        /// Creates a new order list.
        /// </summary>
        /// <param name="itemType">The type of the items in the list.</param>
        /// <returns></returns>
        public OrderList CreateOrderList(ItemType itemType)
        {
            OrderList = new OrderList(itemType);
            return OrderList;
        }

        #endregion

        #region Compound

        /// <summary>
        /// Creates the compound that manages all the tiers.
        /// </summary>
        /// <returns>The newly created compound.</returns>
        public Compound CreateCompound()
        {
            if (Compound != null)
            {
                throw new InvalidOperationException("This instance already contains a compound element.");
            }
            Compound = new Compound(this) { ID = 0 };
            return Compound;
        }

        /// <summary>
        /// Current ID to identify the corresponding instance element.
        /// </summary>
        private int _tierID;
        /// <summary>
        /// Registers and returns a new ID for an object of the given type.
        /// </summary>
        /// <returns>A new unique ID that can be used to identify the object.</returns>
        public int RegisterTierID()
        {
            if (Compound != null && Compound.Tiers.Any() && _tierID <= Compound.Tiers.Max(e => e.ID)) { _tierID = Compound.Tiers.Max(e => e.ID) + 1; }
            return _tierID++;
        }

        /// <summary>
        /// Adds a new tier to the compound.
        /// </summary>
        public Tier CreateTier(int id, double length, double width, double relativePositionX, double relativePositionY, double relativePositionZ)
        {
            if (Compound == null)
                Compound = CreateCompound();
            Tier tier = new Tier(this, length, width)
            {
                ID = id,
                VolatileID = _tierID,
                RelativePositionX = relativePositionX,
                RelativePositionY = relativePositionY,
                RelativePositionZ = relativePositionZ
            };
            Compound.Tiers.Add(tier);
            _idToTier[tier.ID] = tier;
            return tier;
        }

        #endregion

        #region Bot

        /// <summary>
        /// Current ID to identify the corresponding instance element.
        /// </summary>
        private int _botID;
        /// <summary>
        /// Registers and returns a new ID for an object of the given type.
        /// </summary>
        /// <returns>A new unique ID that can be used to identify the object.</returns>
        public int RegisterBotID()
        {
            if (Bots.Any() && _botID <= Bots.Max(e => e.ID)) { _botID = Bots.Max(e => e.ID) + 1; }
            return _botID++;
        }
        /// <summary>
        /// All volative IDs used for bots so far.
        /// </summary>
        private HashSet<int> _volatileBotIDs = new HashSet<int>();
        /// <summary>
        /// Creates a bot with the given characteristics.
        /// </summary>
        /// <param name="id">The ID of the bot.</param>
        /// <param name="tier">The initial position (tier).</param>
        /// <param name="x">The initial position (x-coordinate).</param>
        /// <param name="y">The initial position (y-coordinate).</param>
        /// <param name="radius">The radius of the bot.</param>
        /// <param name="orientation">The initial orientation.</param>
        /// <param name="podTransferTime">The time for picking up and setting down a pod.</param>
        /// <param name="maxAcceleration">The maximal acceleration in m/s^2.</param>
        /// <param name="maxDeceleration">The maximal deceleration in m/s^2.</param>
        /// <param name="maxVelocity">The maximal velocity in m/s.</param>
        /// <param name="turnSpeed">The time it takes the bot to take a full turn in s.</param>
        /// <param name="collisionPenaltyTime">The penalty time for a collision in s.</param>
        /// <returns>The newly created bot.</returns>
        public Bot CreateBot(int id, Tier tier, double x, double y, double radius, double orientation, double podTransferTime, double maxAcceleration, double maxDeceleration, double maxVelocity, double turnSpeed, double collisionPenaltyTime)
        {
            // Consider override values
            if (SettingConfig.OverrideConfig != null && SettingConfig.OverrideConfig.OverrideBotPodTransferTime)
                podTransferTime = SettingConfig.OverrideConfig.OverrideBotPodTransferTimeValue;
            if (SettingConfig.OverrideConfig != null && SettingConfig.OverrideConfig.OverrideBotMaxAcceleration)
                maxAcceleration = SettingConfig.OverrideConfig.OverrideBotMaxAccelerationValue;
            if (SettingConfig.OverrideConfig != null && SettingConfig.OverrideConfig.OverrideBotMaxDeceleration)
                maxDeceleration = SettingConfig.OverrideConfig.OverrideBotMaxDecelerationValue;
            if (SettingConfig.OverrideConfig != null && SettingConfig.OverrideConfig.OverrideBotMaxVelocity)
                maxVelocity = SettingConfig.OverrideConfig.OverrideBotMaxVelocityValue;
            if (SettingConfig.OverrideConfig != null && SettingConfig.OverrideConfig.OverrideBotTurnSpeed)
                turnSpeed = SettingConfig.OverrideConfig.OverrideBotTurnSpeedValue;
            // Init
            Bot bot = null;
            switch (ControllerConfig.PathPlanningConfig.GetMethodType())
            {
                case PathPlanningMethodType.Simple:
                    bot = new BotHazard(this, ControllerConfig.PathPlanningConfig as SimplePathPlanningConfiguration);
                    break;
                case PathPlanningMethodType.Dummy:
                case PathPlanningMethodType.WHCAvStar:
                case PathPlanningMethodType.WHCAnStar:
                case PathPlanningMethodType.FAR:
                case PathPlanningMethodType.BCP:
                case PathPlanningMethodType.OD_ID:
                case PathPlanningMethodType.CBS:
                case PathPlanningMethodType.PAS:
                    bot = new BotNormal(id, this, radius, podTransferTime, maxAcceleration, maxDeceleration, maxVelocity, turnSpeed, collisionPenaltyTime, x, y);
                    break;
                default: throw new ArgumentException("Unknown path planning engine: " + ControllerConfig.PathPlanningConfig.GetMethodType());
            }
            // Set values
            bot.ID = id;
            bot.Tier = tier;
            bot.Instance = this;
            bot.Radius = radius;
            bot.X = x;
            bot.Y = y;
            bot.PodTransferTime = podTransferTime;
            bot.MaxAcceleration = maxAcceleration;
            bot.MaxDeceleration = maxDeceleration;
            bot.MaxVelocity = maxVelocity;
            bot.TurnSpeed = turnSpeed;
            bot.CollisionPenaltyTime = collisionPenaltyTime;
            bot.Orientation = orientation;
            if (bot is BotHazard)
            {
                ((BotHazard)bot).EvadeDistance = 2.3 * radius;
                ((BotHazard)bot).SetTargetOrientation(orientation);
            }
            // Add bot
            Bots.Add(bot);
            tier.AddBot(bot);
            _idToBots[bot.ID] = bot;
            // Determine volatile ID
            int volatileID = 0;
            while (_volatileBotIDs.Contains(volatileID)) { volatileID++; }
            bot.VolatileID = volatileID;
            _volatileBotIDs.Add(bot.VolatileID);
            // Return it
            return bot;
        }

        #endregion

        #region Pod

        /// <summary>
        /// Current ID to identify the corresponding instance element.
        /// </summary>
        private int _podID;
        /// <summary>
        /// Registers and returns a new ID for an object of the given type.
        /// </summary>
        /// <returns>A new unique ID that can be used to identify the object.</returns>
        public int RegisterPodID()
        {
            if (Pods.Any() && _podID <= Pods.Max(e => e.ID)) { _podID = Pods.Max(e => e.ID) + 1; }
            return _podID++;
        }
        /// <summary>
        /// All volative IDs used for pods so far.
        /// </summary>
        private HashSet<int> _volatilePodIDs = new HashSet<int>();
        /// <summary>
        /// Determines and sets a volatile ID for the given pod. This must be called, if volatile IDs will be used.
        /// </summary>
        /// <param name="pod">The pod to determine the volatile ID for.</param>
        private void SetVolatileIDForPod(Pod pod)
        {
            // Determine volatile ID
            int volatileID = 0;
            while (_volatilePodIDs.Contains(volatileID)) { volatileID++; }
            pod.VolatileID = volatileID;
            _volatilePodIDs.Add(pod.VolatileID);
        }
        /// <summary>
        /// Creates a pod with the given characteristics.
        /// </summary>
        /// <param name="id">The ID of the pod.</param>
        /// <param name="tier">The initial position (tier).</param>
        /// <param name="x">The initial position (x-coordinate).</param>
        /// <param name="y">The initial position (y-coordinate).</param>
        /// <param name="radius">The radius of the pod.</param>
        /// <param name="orientation">The initial orientation of the pod.</param>
        /// <param name="capacity">The capacity of the pod.</param>
        /// <returns>The newly created pod.</returns>
        public Pod CreatePod(int id, Tier tier, double x, double y, double radius, double orientation, double capacity)
        {
            // Consider override values
            if (SettingConfig.OverrideConfig != null && SettingConfig.OverrideConfig.OverridePodCapacity)
                capacity = SettingConfig.OverrideConfig.OverridePodCapacityValue;
            // Create the pod
            Pod pod = new Pod(this) { ID = id, Tier = tier, Radius = radius, X = x, Y = y, Orientation = orientation, Capacity = capacity };
            Pods.Add(pod);
            tier.AddPod(pod);
            _idToPods[pod.ID] = pod;
            // Set volatile ID
            SetVolatileIDForPod(pod);
            // Notify listeners
            NewPod(pod);
            // Return it
            return pod;
        }
        /// <summary>
        /// Creates a pod with the given characteristics.
        /// </summary>
        /// <param name="id">The ID of the pod.</param>
        /// <param name="tier">The initial position (tier).</param>
        /// <param name="waypoint">The waypoint to place the pod at.</param>
        /// <param name="radius">The radius of the pod.</param>
        /// <param name="orientation">The initial orientation of the pod.</param>
        /// <param name="capacity">The capacity of the pod.</param>
        /// <returns>The newly created pod.</returns>
        public Pod CreatePod(int id, Tier tier, Waypoint waypoint, double radius, double orientation, double capacity)
        {
            // Consider override values
            if (SettingConfig.OverrideConfig != null && SettingConfig.OverrideConfig.OverridePodCapacity)
                capacity = SettingConfig.OverrideConfig.OverridePodCapacityValue;
            // Create the pod
            Pod pod = new Pod(this) { ID = id, Tier = tier, Radius = radius, X = waypoint.X, Y = waypoint.Y, Orientation = orientation, Capacity = capacity, Waypoint = waypoint };
            Pods.Add(pod);
            tier.AddPod(pod);
            _idToPods[pod.ID] = pod;
            // Set volatile ID
            SetVolatileIDForPod(pod);
            // Emulate setdown operation
            WaypointGraph.PodSetdown(pod, waypoint);
            // Notify listeners
            NewPod(pod);
            // Return it
            return pod;
        }

        #endregion

        #region Elevator

        /// <summary>
        /// Current ID to identify the corresponding instance element.
        /// </summary>
        private int _elevatorID;
        /// <summary>
        /// Registers and returns a new ID for an object of the given type.
        /// </summary>
        /// <returns>A new unique ID that can be used to identify the object.</returns>
        public int RegisterElevatorID()
        {
            if (Elevators.Any() && _elevatorID <= Elevators.Max(e => e.ID)) { _elevatorID = Elevators.Max(e => e.ID) + 1; }
            return _elevatorID++;
        }
        /// <summary>
        /// Creates a new elevator.
        /// </summary>
        /// <param name="id">The ID of the elevator.</param>
        /// <returns>The newly created elevator.</returns>
        public Elevator CreateElevator(int id)
        {
            Elevator elevator = new Elevator(this) { ID = id };
            elevator.Queues = new Dictionary<Waypoint, List<Waypoint>>();
            Elevators.Add(elevator);
            _idToElevators[elevator.ID] = elevator;
            return elevator;
        }

        #endregion

        #region InputStation

        /// <summary>
        /// Current ID to identify the corresponding instance element.
        /// </summary>
        private int _inputStationID;
        /// <summary>
        /// Registers and returns a new ID for an object of the given type.
        /// </summary>
        /// <returns>A new unique ID that can be used to identify the object.</returns>
        public int RegisterInputStationID()
        {
            if (InputStations.Any() && _inputStationID <= InputStations.Max(e => e.ID)) { _inputStationID = InputStations.Max(e => e.ID) + 1; }
            return _inputStationID++;
        }
        /// <summary>
        /// All volative IDs used for input-stations so far.
        /// </summary>
        private HashSet<int> _volatileInputStationIDs = new HashSet<int>();
        /// <summary>
        /// Creates a new input-station.
        /// </summary>
        /// <param name="id">The ID of the input station.</param>
        /// <param name="tier">The position (tier).</param>
        /// <param name="x">The position (x-coordinate).</param>
        /// <param name="y">The position (y-coordinate).</param>
        /// <param name="radius">The radius of the station.</param>
        /// <param name="capacity">The capacity of the station.</param>
        /// <param name="itemBundleTransfertime">The time it takes to handle one bundle at the station.</param>
        /// <param name="activationOrderID">The order ID of the station that defines the sequence in which the stations have to be activated.</param>
        /// <returns>The newly created input station.</returns>
        public InputStation CreateInputStation(int id, Tier tier, double x, double y, double radius, double capacity, double itemBundleTransfertime, int activationOrderID)
        {
            // Consider override values
            if (SettingConfig.OverrideConfig != null && SettingConfig.OverrideConfig.OverrideInputStationCapacity)
                capacity = SettingConfig.OverrideConfig.OverrideInputStationCapacityValue;
            if (SettingConfig.OverrideConfig != null && SettingConfig.OverrideConfig.OverrideInputStationItemBundleTransferTime)
                itemBundleTransfertime = SettingConfig.OverrideConfig.OverrideInputStationItemBundleTransferTimeValue;
            // Init
            InputStation inputStation = new InputStation(this)
            { ID = id, Tier = tier, Radius = radius, X = x, Y = y, Capacity = capacity, ItemBundleTransferTime = itemBundleTransfertime, ActivationOrderID = activationOrderID };
            inputStation.Queues = new Dictionary<Waypoint, List<Waypoint>>();
            InputStations.Add(inputStation);
            tier.AddInputStation(inputStation);
            _idToInputStations[inputStation.ID] = inputStation;
            // Determine volatile ID
            int volatileID = 0;
            while (_volatileInputStationIDs.Contains(volatileID)) { volatileID++; }
            inputStation.VolatileID = volatileID;
            _volatileInputStationIDs.Add(inputStation.VolatileID);
            return inputStation;
        }

        #endregion

        #region OutputStation

        /// <summary>
        /// Current ID to identify the corresponding instance element.
        /// </summary>
        private int _outputStationID;
        /// <summary>
        /// Registers and returns a new ID for an object of the given type.
        /// </summary>
        /// <returns>A new unique ID that can be used to identify the object.</returns>
        public int RegisterOutputStationID()
        {
            if (OutputStations.Any() && _outputStationID <= OutputStations.Max(e => e.ID)) { _outputStationID = OutputStations.Max(e => e.ID) + 1; }
            return _outputStationID++;
        }
        /// <summary>
        /// All volative IDs used for output-stations so far.
        /// </summary>
        private HashSet<int> _volatileOutputStationIDs = new HashSet<int>();
        /// <summary>
        /// Creates a new output-station.
        /// </summary>
        /// <param name="id">The ID of the input station.</param>
        /// <param name="tier">The position (tier).</param>
        /// <param name="x">The position (x-coordinate).</param>
        /// <param name="y">The position (y-coordinate).</param>
        /// <param name="radius">The radius of the station.</param>
        /// <param name="capacity">The capacity of the station.</param>
        /// <param name="itemTransferTime">The time it takes to handle one item at the station.</param>
        /// <param name="itemPickTime">The time it takes to pick the item from a pod (excluding other handling times).</param>
        /// <param name="activationOrderID">The order ID of the station that defines the sequence in which the stations have to be activated.</param>
        /// <returns>The newly created output station.</returns>
        public OutputStation CreateOutputStation(int id, Tier tier, double x, double y, double radius, int capacity, double itemTransferTime, double itemPickTime, int activationOrderID)
        {
            // Consider override values
            if (SettingConfig.OverrideConfig != null && SettingConfig.OverrideConfig.OverrideOutputStationCapacity)
                capacity = SettingConfig.OverrideConfig.OverrideOutputStationCapacityValue;
            if (SettingConfig.OverrideConfig != null && SettingConfig.OverrideConfig.OverrideOutputStationItemPickTime)
                itemPickTime = SettingConfig.OverrideConfig.OverrideOutputStationItemPickTimeValue;
            if (SettingConfig.OverrideConfig != null && SettingConfig.OverrideConfig.OverrideOutputStationItemTransferTime)
                itemTransferTime = SettingConfig.OverrideConfig.OverrideOutputStationItemTransferTimeValue;
            // Init
            OutputStation outputStation = new OutputStation(this)
            { ID = id, Radius = radius, X = x, Y = y, Capacity = capacity, ItemTransferTime = itemTransferTime, ItemPickTime = itemPickTime, ActivationOrderID = activationOrderID };
            outputStation.Queues = new Dictionary<Waypoint, List<Waypoint>>();
            OutputStations.Add(outputStation);
            tier.AddOutputStation(outputStation);
            _idToOutputStations[outputStation.ID] = outputStation;
            // Determine volatile ID
            int volatileID = 0;
            while (_volatileOutputStationIDs.Contains(volatileID)) { volatileID++; }
            outputStation.VolatileID = volatileID;
            _volatileOutputStationIDs.Add(outputStation.VolatileID);
            return outputStation;
        }

        #endregion

        #region Waypoints

        private int _waypointID;
        /// <summary>
        /// Registers and returns a new ID for an object of the given type.
        /// </summary>
        /// <returns>A new unique ID that can be used to identify the object.</returns>
        public int RegisterWaypointID()
        {
            if (Waypoints.Any() && _waypointID <= Waypoints.Max(e => e.ID)) { _waypointID = Waypoints.Max(e => e.ID) + 1; }
            return _waypointID++;
        }
        /// <summary>
        /// All volative IDs used for waypoints so far.
        /// </summary>
        private HashSet<int> _volatileWaypointIDs = new HashSet<int>();
        /// <summary>
        /// Determines and sets a volatile ID for the given waypoint. This must be called, if volatile IDs will be used.
        /// </summary>
        /// <param name="waypoint">The waypoint to determine the volatile ID for.</param>
        private void SetVolatileIDForWaypoint(Waypoint waypoint)
        {
            // Determine volatile ID
            int volatileID = 0;
            while (_volatileWaypointIDs.Contains(volatileID)) { volatileID++; }
            waypoint.VolatileID = volatileID;
            _volatileWaypointIDs.Add(waypoint.VolatileID);
        }
        /// <summary>
        /// Creates a new waypoint that serves as the handover point for an input station.
        /// </summary>
        /// <param name="id">The ID of the waypoint.</param>
        /// <param name="tier">The position (tier).</param>
        /// <param name="station">The station.</param>
        /// <param name="isQueueWaypoint">Indicates whether this waypoint is also a queue waypoint.</param>
        /// <returns>The newly created waypoint.</returns>
        public Waypoint CreateWaypoint(int id, Tier tier, InputStation station, bool isQueueWaypoint)
        {
            Waypoint wp = new Waypoint(this) { ID = id, X = station.X, Y = station.Y, Radius = station.Radius, InputStation = station, IsQueueWaypoint = isQueueWaypoint };
            station.Waypoint = wp;
            tier.AddWaypoint(wp);
            Waypoints.Add(wp);
            WaypointGraph.Add(wp);
            _idToWaypoint[wp.ID] = wp;
            // Set volatile ID
            SetVolatileIDForWaypoint(wp);
            // Return
            return wp;
        }
        /// <summary>
        /// Creates a new waypoint that serves as the handover point for an output station.
        /// </summary>
        /// <param name="id">The ID of the waypoint.</param>
        /// <param name="tier">The position (tier).</param>
        /// <param name="station">The station.</param>
        /// <param name="isQueueWaypoint">Indicates whether this waypoint is also a queue waypoint.</param>
        /// <returns>The newly created waypoint.</returns>
        public Waypoint CreateWaypoint(int id, Tier tier, OutputStation station, bool isQueueWaypoint)
        {
            Waypoint wp = new Waypoint(this) { ID = id, X = station.X, Y = station.Y, Radius = station.Radius, OutputStation = station, IsQueueWaypoint = isQueueWaypoint };
            station.Waypoint = wp;
            tier.AddWaypoint(wp);
            Waypoints.Add(wp);
            WaypointGraph.Add(wp);
            _idToWaypoint[wp.ID] = wp;
            // Set volatile ID
            SetVolatileIDForWaypoint(wp);
            // Return
            return wp;
        }
        /// <summary>
        /// Creates a new waypoint that serves as the handover point for an elevator.
        /// </summary>
        /// <param name="id">The ID of the waypoint.</param>
        /// <param name="tier">The position (tier).</param>
        /// <param name="x">The position (x-coordinate).</param>
        /// <param name="y">The position (y-coordinate).</param>
        /// <param name="elevator">The elevator.</param>
        /// <param name="isQueueWaypoint">Indicates whether this waypoint is also a queue waypoint.</param>
        /// <returns>The newly created waypoint.</returns>
        public Waypoint CreateWaypoint(int id, Tier tier, Elevator elevator, double x, double y, bool isQueueWaypoint)
        {
            Waypoint wp = new Waypoint(this) { ID = id, X = x, Y = y, Elevator = elevator, IsQueueWaypoint = isQueueWaypoint };
            tier.AddWaypoint(wp);
            Waypoints.Add(wp);
            WaypointGraph.Add(wp);
            _idToWaypoint[wp.ID] = wp;
            // Set volatile ID
            SetVolatileIDForWaypoint(wp);
            // Return
            return wp;
        }
        /// <summary>
        /// Creates a new waypoint that serves as a storage location.
        /// </summary>
        /// <param name="id">The ID of the waypoint.</param>
        /// <param name="tier">The position (tier).</param>
        /// <param name="pod">The pod currently stored at it.</param>
        /// <returns>The newly created waypoint.</returns>
        public Waypoint CreateWaypoint(int id, Tier tier, Pod pod)
        {
            Waypoint wp = new Waypoint(this) { ID = id, X = pod.X, Y = pod.Y, Radius = pod.Radius, PodStorageLocation = true, Pod = pod };
            pod.Waypoint = wp;
            tier.AddWaypoint(wp);
            Waypoints.Add(wp);
            WaypointGraph.Add(wp);
            _idToWaypoint[wp.ID] = wp;
            // Set volatile ID
            SetVolatileIDForWaypoint(wp);
            // Return
            return wp;
        }
        /// <summary>
        /// Creates a typical waypoint.
        /// </summary>
        /// <param name="id">The ID of the waypoint.</param>
        /// <param name="tier">The position (tier).</param>
        /// <param name="x">The position (x-coordinate).</param>
        /// <param name="y">The position (y-coordinate).</param>
        /// <param name="podStorageLocation">Indicates whether the waypoint serves as a storage location.</param>
        /// <param name="isQueueWaypoint">Indicates whether the waypoint belongs to a queue.</param>
        /// <returns>The newly created waypoint.</returns>
        public Waypoint CreateWaypoint(int id, Tier tier, double x, double y, bool podStorageLocation, bool isQueueWaypoint)
        {
            Waypoint wp = new Waypoint(this) { ID = id, Tier = tier, X = x, Y = y, PodStorageLocation = podStorageLocation, IsQueueWaypoint = isQueueWaypoint };
            tier.AddWaypoint(wp);
            Waypoints.Add(wp);
            WaypointGraph.Add(wp);
            _idToWaypoint[wp.ID] = wp;
            // Set volatile ID
            SetVolatileIDForWaypoint(wp);
            // Return
            return wp;
        }

        #endregion

        #region Semaphores

        private int _semaphoreID;
        /// <summary>
        /// Registers and returns a new ID for an object of the given type.
        /// </summary>
        /// <returns>A new unique ID that can be used to identify the object.</returns>
        public int RegisterSemaphoreID()
        {
            if (Semaphores.Any() && _semaphoreID <= Semaphores.Max(e => e.ID)) { _semaphoreID = Semaphores.Max(e => e.ID) + 1; }
            return _semaphoreID++;
        }
        /// <summary>
        /// Creates a new semaphore.
        /// </summary>
        /// <param name="id">The ID of the semaphore.</param>
        /// <param name="maximalCount">The maximal number of bots in the managed area.</param>
        /// <returns>The newly created semaphore.</returns>
        public QueueSemaphore CreateSemaphore(int id, int maximalCount)
        {
            QueueSemaphore semaphore = new QueueSemaphore(this, maximalCount) { ID = id };
            Semaphores.Add(semaphore);
            _idToSemaphore[semaphore.ID] = semaphore;
            return semaphore;
        }

        #endregion

        #endregion

        #region Finalizing

        /// <summary>
        /// Finalizes the instance.
        /// </summary>
        public void Flush()
        {
            // Set references to this object
            Compound.Instance = this;
            foreach (var instanceElement in
                Bots.AsEnumerable<InstanceElement>()
                .Concat(Pods.AsEnumerable<InstanceElement>())
                .Concat(Elevators.AsEnumerable<InstanceElement>())
                .Concat(InputStations.AsEnumerable<InstanceElement>())
                .Concat(OutputStations.AsEnumerable<InstanceElement>())
                .Concat(Waypoints.AsEnumerable<InstanceElement>())
                .Concat(ItemDescriptions.AsEnumerable<InstanceElement>())
                .Concat(ItemBundles.AsEnumerable<InstanceElement>()))
            {
                instanceElement.Instance = this;
            }

            //TODO:why is the code below commented out??

            //// Generate Waypointgraph
            //WaypointGraph = new WaypointGraph();
            //foreach (var waypoint in Waypoints)
            //{
            //    WaypointGraph.Add(waypoint);
            //}
        }

        #endregion

        #region Late initialization hooks

        /// <summary>
        /// The event handler for the event that is raised when the simulation is just before starting and after all other managers were initialized.
        /// </summary>
        public delegate void LateInitEventHandler();
        /// <summary>
        /// The event that is raised when the simulation is just before starting and after all other managers were initialized.
        /// </summary>
        public event LateInitEventHandler LateInit;
        /// <summary>
        /// Notifies the instance that all previous initializations are done (all managers are available) and we are almost ready to start the simulation updates.
        /// </summary>
        internal void LateInitialize()
        {
            // Call all subscribers
            LateInit?.Invoke();
        }

        #endregion
    }
}
