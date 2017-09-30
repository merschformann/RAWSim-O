using RAWSimO.Core.Bots;
using RAWSimO.Core.Configurations;
using RAWSimO.Core.Control;
using RAWSimO.Core.Control.Shared;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Geometrics;
using RAWSimO.Core.Helper;
using RAWSimO.Core.Items;
using RAWSimO.Core.Management;
using RAWSimO.Core.Statistics;
using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RAWSimO.Core
{
    /// THIS PARTIAL CLASS CONTAINS THE CORE FIELDS OF AN INSTANCE
    /// <summary>
    /// The core element of each simulation instance.
    /// </summary>
    public partial class Instance
    {
        #region Constructors

        internal Instance()
        {
            Observer = new SimulationObserver(this);
            StockInfo = new StockInformation(this);
            MetaInfoManager = new MetaInformationManager(this);
            FrequencyTracker = new FrequencyTracker(this);
            ElementMetaInfoTracker = new ElementMetaInfoTracker(this);
            BotCrashHandler = new BotCrashHandler(this);
            SharedControlElements = new SharedControlElementsContainer(this);
        }

        #endregion

        #region Core

        /// <summary>
        /// The name of the instance.
        /// </summary>
        public string Name;
        /// <summary>
        /// The configuration to use while executing the instance.
        /// </summary>
        public SettingConfiguration SettingConfig { get; set; }
        /// <summary>
        /// The configuration for all controlling mechanisms.
        /// </summary>
        public ControlConfiguration ControllerConfig { get; set; }
        /// <summary>
        /// All SKUs available in this instance.
        /// </summary>
        public List<ItemDescription> ItemDescriptions = new List<ItemDescription>();
        /// <summary>
        /// All item bundles known so far.
        /// </summary>
        public List<ItemBundle> ItemBundles = new List<ItemBundle>();
        /// <summary>
        /// A list of given orders that will be passed to the item manager.
        /// </summary>
        public OrderList OrderList;
        /// <summary>
        /// The compound declaring all physical attributes of the instance.
        /// </summary>
        public Compound Compound;
        /// <summary>
        /// All robots of this instance.
        /// </summary>
        public List<Bot> Bots = new List<Bot>();
        /// <summary>
        /// All pods of this instance.
        /// </summary>
        public List<Pod> Pods = new List<Pod>();
        /// <summary>
        /// All elevators of this instance.
        /// </summary>
        public List<Elevator> Elevators = new List<Elevator>();
        /// <summary>
        /// All input-stations of this instance.
        /// </summary>
        public List<InputStation> InputStations = new List<InputStation>();
        /// <summary>
        /// All output-stations of this instance.
        /// </summary>
        public List<OutputStation> OutputStations = new List<OutputStation>();
        /// <summary>
        /// All waypoints of this instance.
        /// </summary>
        public List<Waypoint> Waypoints = new List<Waypoint>();
        /// <summary>
        /// All semaphors of this instance.
        /// </summary>
        public List<QueueSemaphore> Semaphores = new List<QueueSemaphore>();

        #endregion
    }
}
