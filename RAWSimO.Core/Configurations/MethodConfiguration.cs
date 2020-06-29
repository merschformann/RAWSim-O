using RAWSimO.Core.Control.Defaults.ItemStorage;
using RAWSimO.Core.Control.Defaults.OrderBatching;
using RAWSimO.Core.Control.Defaults.PodStorage;
using RAWSimO.Core.Control.Defaults.ReplenishmentBatching;
using RAWSimO.Core.Control.Shared;
using RAWSimO.Core.IO;
using RAWSimO.Core.Metrics;
using RAWSimO.MultiAgentPathFinding.Methods;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RAWSimO.Core.Configurations
{
    #region Controller types

    /// <summary>
    /// All types of implemented path planning strategies.
    /// </summary>
    public enum PathPlanningMethodType
    {
        /// <summary>
        /// The default path planning algorithm utilizing an A* implementation.
        /// </summary>
        Simple,

        /// <summary>
        /// Dummy Random Hop
        /// </summary>
        Dummy,

        /// <summary>
        /// Silver 06 - Cooperative pathfinding
        /// </summary>
        WHCAvStar,

        /// <summary>
        /// Silver 06 - Cooperative pathfinding
        /// </summary>
        WHCAnStar,

        /* Heavy Approach with many drawbacks for continues time slots 
         * Missing: Distinguish between pod holding and non-pod holding bots
        /// <summary>
        /// Wang - Tractable Multi-Agent Path Planning on Grid Maps
        /// </summary>
        MAPP,
         * */

        /// <summary>
        /// Wang - Fast and Memory-Efficient Multi-Agent Pathfinding
        /// </summary>
        FAR,

        /// <summary>
        /// Geramifard - Biased Cost Pathﬁnding
        /// </summary>
        BCP,

        /// <summary>
        /// Standley - Finding Optimal Solutions to Cooperative Pathﬁnding Problems
        /// </summary>
        OD_ID,

        /*
         * Based on discrete time slots and can not be converted to continues time slots
        /// <summary>
        /// Sharon - The Increasing Cost Tree Search for Optimal Multi-agent Pathfinding
        /// </summary>
        ICTS,
         * */

        /// <summary>
        /// Sharon - Conflict-based search for optimal multi-agent pathfinding
        /// </summary>
        CBS,

        /// <summary>
        /// Erdmann - Parallel Multi-Agent Pathfinding
        /// </summary>
        PAS

    }
    /// <summary>
    /// All types of implemented task allocation strategies.
    /// </summary>
    public enum TaskAllocationMethodType
    {
        /// <summary>
        /// A method aiming to choose the task with the least execution time from all available tasks.
        /// </summary>
        BruteForce,
        /// <summary>
        /// A mainly random approach to the task allocation.
        /// </summary>
        Random,
        /// <summary>
        /// A method aiming to distribute bots evenly among the stations and only allocate tasks fitting their currently assigned station.
        /// </summary>
        Balanced,
        /// <summary>
        /// A swarm intelligence inspired task allocation using stygmergic values to distribute bots among the stations.
        /// </summary>
        Swarm,
        /// <summary>
        /// A method keeping the ratios between robots assigned for picking and for replenishment fixed and also does not change the equal distribution of them across the stations.
        /// </summary>
        ConstantRatio,
        /// <summary>
        /// A new method that has no concept yet.
        /// </summary>
        Concept,
    }
    /// <summary>
    /// All types of implemented station activation strategies.
    /// </summary>
    public enum StationActivationMethodType
    {
        /// <summary>
        /// A method simply keeping all stations activated.
        /// </summary>
        ActivateAll,
        /// <summary>
        /// A method activating and deactivating stations depending on the current backlog size.
        /// </summary>
        BacklogThreshold,
        /// <summary>
        /// A method activating pick and replenishment stations in a constant ratio to each other.
        /// </summary>
        ConstantRatio,
        /// <summary>
        /// A method that emulates working shifts and activates / deactivates stations accordingly.
        /// </summary>
        WorkShift,
    }
    /// <summary>
    /// All types of implemented item storage strategies.
    /// </summary>
    public enum ItemStorageMethodType
    {
        /// <summary>
        /// A dummy approach that does nothing at all. (Used in case another manager integrates this problem)
        /// </summary>
        Dummy,
        /// <summary>
        /// A mainly random approach to the item storage problem.
        /// </summary>
        Random,
        /// <summary>
        /// An approach that aims to allocate items to pods to which highly correlating items are already assigned.
        /// </summary>
        Correlative,
        /// <summary>
        /// An approach that aims to build high-speed and low-speed pods.
        /// </summary>
        Turnover,
        /// <summary>
        /// An approach that aims to allocate items to pods with smallest distance.
        /// </summary>
        ClosestLocation,
        /// <summary>
        /// An approach that waits for the allocation of bundles to input-stations and then applies a metric to allocate them to pods.
        /// </summary>
        Reactive,
        /// <summary>
        /// An approach that aims to allocate items to the emptiest pods.
        /// </summary>
        Emptiest,
        /// <summary>
        /// An approach that aims to allocate items to the least demanded pods.
        /// </summary>
        LeastDemand,
    }
    /// <summary>
    /// All types of implemented pod storage strategies.
    /// </summary>
    public enum PodStorageMethodType
    {
        /// <summary>
        /// A dummy approach that does nothing at all. (Used in case another manager integrates this problem)
        /// </summary>
        Dummy,
        /// <summary>
        /// This method locks the positions at which the pods are stored.
        /// </summary>
        Fixed,
        /// <summary>
        /// This method uses only distance information to determine the position of the pod to store.
        /// </summary>
        Nearest,
        /// <summary>
        /// This method aims to store pods near to the stations.
        /// </summary>
        StationBased,
        /// <summary>
        /// This method aims to keep pods needed in near future near the station, while bringing others back to the remaining inventory.
        /// </summary>
        Cache,
        /// <summary>
        /// This method determines scores for the pods and matches them with scores for the storage locations.
        /// </summary>
        Utility,
        /// <summary>
        /// This method allocates a random storage position to the pod.
        /// </summary>
        Random,
        /// <summary>
        /// This method uses item-frequency information to assign the pods to the positions.
        /// </summary>
        Turnover
    }
    /// <summary>
    /// All types of implemented repositioning strategies.
    /// </summary>
    public enum RepositioningMethodType
    {
        /// <summary>
        /// A dummy approach that does not supply any repositioning moves. Pods will exclusively be positioned by the pod storage manager.
        /// </summary>
        Dummy,
        /// <summary>
        /// A method aiming to store useful pods in a cache zone while freeing up space in front of the output-stations.
        /// </summary>
        Cache,
        /// <summary>
        /// A method aiming to store useful pods in a cache zone while freeing up space in front of the output-stations.
        /// </summary>
        CacheDropoff,
        /// <summary>
        /// A method that repositions pods which utility value is most out of sync with what the current value suggested by a pod utility manager component says.
        /// </summary>
        Utility,
        /// <summary>
        /// A concept repositioning method.
        /// </summary>
        Concept,
    }
    /// <summary>
    /// All types of implemented order batching strategies.
    /// </summary>
    public enum OrderBatchingMethodType
    {
        /// <summary>
        /// Default random order to output station assignments.
        /// </summary>
        Default,
        /// <summary>
        /// A complete random assignment of orders to output-stations.
        /// </summary>
        Random,
        /// <summary>
        /// An approach assigning orders based on the current workload of the stations.
        /// </summary>
        Workload,
        /// <summary>
        /// An approach assigning orders to stations whose orders have the most lines in common.
        /// </summary>
        Related,
        /// <summary>
        /// An approach assigning orders to stations near the best pod for the order.
        /// </summary>
        NearBestPod,
        /// <summary>
        /// An approach selecting an order for a station where there is the best match of inbound items.
        /// </summary>
        PodMatching,
        /// <summary>
        /// An approach selecting an order that is most similar to the ones already assigned to a station.
        /// </summary>
        LinesInCommon,
        /// <summary>
        /// An approach building queues per station and selecting orders from these.
        /// </summary>
        Queue,
        /// <summary>
        /// An approach exploiting information about the backlog to increase similarities of orders at the stations.
        /// </summary>
        Foresight,
    }
    /// <summary>
    /// All types of implemented replenishment batching strategies.
    /// </summary>
    public enum ReplenishmentBatchingMethodType
    {
        /// <summary>
        /// A complete random assignment of bundles to input-stations.
        /// </summary>
        Random,
        /// <summary>
        /// Bundles for the same pod are assigned to the same input-station.
        /// </summary>
        SamePod
    }
    /// <summary>
    /// All types of implemented meta manager strategies.
    /// </summary>
    public enum MethodManagementType
    {
        /// <summary>
        /// Does not change any of the previously set mechanisms.
        /// </summary>
        NoChange,
        /// <summary>
        /// Does change managers periodically to new random ones.
        /// </summary>
        Random,
        /// <summary>
        /// Does change managers at predefined timepoints to predefined ones.
        /// </summary>
        Scheduled,
    }

    #endregion

    #region Root method configuration

    /// <summary>
    /// Exposes the different parameters used by the different mechanisms.
    /// </summary>
    public class ControlConfiguration
    {
        /// <summary>
        /// Creates a new default control configuration.
        /// </summary>
        public ControlConfiguration()
        {
            PathPlanningConfig = new FARPathPlanningConfiguration();
            TaskAllocationConfig = new BalancedTaskAllocationConfiguration();
            StationActivationConfig = new ActivateAllStationActivationConfiguration();
            ItemStorageConfig = new EmptiestItemStorageConfiguration();
            PodStorageConfig = new NearestPodStorageConfiguration();
            RepositioningConfig = new DummyRepositioningConfiguration();
            OrderBatchingConfig = new PodMatchingOrderBatchingConfiguration();
            ReplenishmentBatchingConfig = new SamePodReplenishmentBatchingConfiguration();
            MethodManagementConfig = new NoChangeMethodManagementConfiguration();
        }
        /// <summary>
        /// The name of the config.
        /// </summary>
        public string Name = "default";
        /// <summary>
        /// The configuration for the path planning control.
        /// </summary>
        public PathPlanningConfiguration PathPlanningConfig;
        /// <summary>
        /// The configuration for the task alocation to use.
        /// </summary>
        public TaskAllocationConfiguration TaskAllocationConfig;
        /// <summary>
        /// The configuration for the station activation in use.
        /// </summary>
        public StationActivationConfiguration StationActivationConfig;
        /// <summary>
        /// The configuration for the item storage strategy.
        /// </summary>
        public ItemStorageConfiguration ItemStorageConfig;
        /// <summary>
        /// The configuration of the pod storage strategy.
        /// </summary>
        public PodStorageConfiguration PodStorageConfig;
        /// <summary>
        /// The configuration of the repositioning strategy.
        /// </summary>
        public RepositioningConfiguration RepositioningConfig;
        /// <summary>
        /// The configuration for the pod storage strategy to use.
        /// </summary>
        public OrderBatchingConfiguration OrderBatchingConfig;
        /// <summary>
        /// The configuration of the replenishment strategy to use.
        /// </summary>
        public ReplenishmentBatchingConfiguration ReplenishmentBatchingConfig;
        /// <summary>
        /// The configuration for the meta method manager to use.
        /// </summary>
        public MethodManagementConfiguration MethodManagementConfig;
        /// <summary>
        /// Some optional comment tag that will be written to the footprint.
        /// </summary>
        public string CommentTag1 = "";
        /// <summary>
        /// Some optional comment tag that will be written to the footprint.
        /// </summary>
        public string CommentTag2 = "";
        /// <summary>
        /// Some optional comment tag that will be written to the footprint.
        /// </summary>
        public string CommentTag3 = "";
        /// <summary>
        /// Checks wether all Configuartions are initialized and returns a simple string identifying the strategies set by the config.
        /// </summary>
        /// <returns>A simple string that can be used as an abstract identifier.</returns>
        public string GetMetaInfoBasedConfigName()
        {
            if (PathPlanningConfig == null)
            {
                throw new ArgumentException("PathPlanningConfig is null. Please initialize PathPlanning Configuration");
            }
            if (TaskAllocationConfig == null)
            {
                throw new ArgumentException("TaskAllocationConfig is null. Please initialize TaskAllocation Configuration");
            }
            if (StationActivationConfig == null)
            {
                throw new ArgumentException("StationActivationConfig is null. Please initialize StationActivation Configuration");
            }
            if (ItemStorageConfig == null)
            {
                throw new ArgumentException("ItemStorageConfig is null. Please initialize ItemStorage Configuration");
            }
            if (PodStorageConfig == null)
            {
                throw new ArgumentException("PodStorageConfig is null. Please initialize PodStorage Configuration");
            }
            if (RepositioningConfig == null)
            {
                throw new ArgumentException("RepositioningConfig is null. Please initialize Repositioning Configuration");
            }
            if (OrderBatchingConfig == null)
            {
                throw new ArgumentException("OrderBatchingConfig is null. Please initialize OrderBatching Configuration");
            }
            if (ReplenishmentBatchingConfig == null)
            {
                throw new ArgumentException("ReplenishmentBatchingConfig is null. Please initialize ReplenishmentBatching Configuration");
            }
            if (MethodManagementConfig == null)
            {
                throw new ArgumentException("MethodManagementConfig is null. Please initialize MethodManagement Configuration");
            }
            return
                "PP" + PathPlanningConfig.GetMethodType() + "-" +
                "TA" + TaskAllocationConfig.GetMethodType() + "-" +
                "SA" + StationActivationConfig.GetMethodType() + "-" +
                "IS" + ItemStorageConfig.GetMethodType() + "-" +
                "PS" + PodStorageConfig.GetMethodType() + "-" +
                "RP" + RepositioningConfig.GetMethodType() + "-" +
                "OB" + OrderBatchingConfig.GetMethodType() + "-" +
                "RB" + ReplenishmentBatchingConfig.GetMethodType() + "-" +
                "MM" + MethodManagementConfig.GetMethodType();
        }

        /// <summary>
        /// Checks whether the method configuration is valid.
        /// </summary>
        /// <param name="errorMessage">A message describing the error if a method are not valid.</param>
        /// <returns>Indicates whether the method configuration is valid.</returns>
        public bool IsValid(out String errorMessage)
        {
            // checking PathPlanningConfig
            if (!PathPlanningConfig.AttributesAreValid(out errorMessage)) { return false; }
            if (!TaskAllocationConfig.AttributesAreValid(out errorMessage)) { return false; }
            if (!ItemStorageConfig.AttributesAreValid(out errorMessage)) { return false; }
            if (!PodStorageConfig.AttributesAreValid(out errorMessage)) { return false; }
            if (!MethodManagementConfig.AttributesAreValid(out errorMessage)) { return false; }
            errorMessage = "";
            return true;
        }
    }

    /// <summary>
    /// The base class for all controller configurations.
    /// </summary>
    public abstract class ControllerConfigurationBase
    {
        /// <summary>
        /// A field that can be used to provide a name for the method. If no name is given, a default one is returned instead.
        /// </summary>
        public string Name = "";
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public abstract string GetMethodName();
    }

    /// <summary>
    /// Base class for the path planning configuration.
    /// </summary>
    [XmlInclude(typeof(SimplePathPlanningConfiguration))]
    [XmlInclude(typeof(DummyPathPlanningConfiguration))]
    [XmlInclude(typeof(WHCAvStarPathPlanningConfiguration))]
    [XmlInclude(typeof(WHCAnStarPathPlanningConfiguration))]
    [XmlInclude(typeof(FARPathPlanningConfiguration))]
    [XmlInclude(typeof(ODIDPathPlanningConfiguration))]
    [XmlInclude(typeof(BCPPathPlanningConfiguration))]
    [XmlInclude(typeof(CBSPathPlanningConfiguration))]
    [XmlInclude(typeof(PASPathPlanningConfiguration))]
    [XmlInclude(typeof(PathPlanningConfiguration))]
    public abstract class PathPlanningConfiguration : ControllerConfigurationBase
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public abstract PathPlanningMethodType GetMethodType();

        /// <summary>
        /// Set parameter automatically and ignore the other ones
        /// </summary>
        public bool AutoSetParameter = false;

        /// <summary>
        /// Indicates whether the robot can drive under stored pods (tunnel them).
        /// </summary>
        public bool CanTunnel = true;
        /// <summary>
        /// The length of a wait step
        /// </summary>
        public double LengthOfAWaitStep = 2.0;

        /// <summary>
        /// The runtime limit per call per agent
        /// </summary>
        public double RuntimeLimitPerAgent = .1;

        /// <summary>
        /// The runtime limit per call
        /// </summary>
        public double RunTimeLimitOverall = 1.0;

        /// <summary>
        /// Minimum time space between two calls
        /// </summary>
        public double Clocking = 1.0;

        /// <summary>
        /// Parses the specified arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public virtual void Parse(string[] args)
        {
            LengthOfAWaitStep = double.Parse(args[0], new CultureInfo("en"));
            RuntimeLimitPerAgent = double.Parse(args[1], new CultureInfo("en"));
        }

        /// <summary>
        /// Small sanity check for the path planning configuration.
        /// </summary>
        /// <param name="errorMessage">A message describing the error if the configuration is not valid.</param>
        /// <returns>Indicates whether the path planning configuration is valid.</returns>
        public virtual bool AttributesAreValid(out String errorMessage)
        {

            if (LengthOfAWaitStep < 0)
            {
                errorMessage = "Please check Pathplanning Configurations and make sure, that LengthOfAWaitStep is >= 0";
                return false;
            }
            if (RuntimeLimitPerAgent < 0)
            {
                errorMessage = "Please check Pathplanning Configurations and make sure, that RuntimeLimitPerAgent is >= 0";
                return false;
            }
            if (RunTimeLimitOverall < 0)
            {
                errorMessage = "Please check Pathplanning Configurations and make sure, that RunTimeLimitOverall is >= 0";
                return false;
            }
            if (Clocking < 0)
            {
                errorMessage = "Please check Pathplanning Configurations and make sure, that Clocking is >= 0";
                return false;
            }
            errorMessage = "";
            return true;
        }
    }

    /// <summary>
    /// Base class for the task allocation configuration.
    /// </summary>
    [XmlInclude(typeof(BruteForceTaskAllocationConfiguration))]
    [XmlInclude(typeof(RandomTaskAllocationConfiguration))]
    [XmlInclude(typeof(BalancedTaskAllocationConfiguration))]
    [XmlInclude(typeof(SwarmTaskAllocationConfiguration))]
    [XmlInclude(typeof(ConstantRatioTaskAllocationConfiguration))]
    [XmlInclude(typeof(ConceptTaskAllocationConfiguration))]
    public abstract class TaskAllocationConfiguration : ControllerConfigurationBase
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public abstract TaskAllocationMethodType GetMethodType();
        /// <summary>
        /// Checks whether the task allacation configuration is valid.
        /// </summary>
        /// <param name="errorMessage">A message describing the error if the configuration is not valid.</param>
        /// <returns>Indicates whether the task allocation configuration is valid.</returns>
        public virtual bool AttributesAreValid(out String errorMessage)
        {
            errorMessage = "";
            return true;
        }
    }
    /// <summary>
    /// Base class for the station activation configuration.
    /// </summary>
    [XmlInclude(typeof(ActivateAllStationActivationConfiguration))]
    [XmlInclude(typeof(BacklogThresholdStationActivationConfiguration))]
    [XmlInclude(typeof(ConstantRatioStationActivationConfiguration))]
    [XmlInclude(typeof(WorkShiftStationActivationConfiguration))]
    public abstract class StationActivationConfiguration : ControllerConfigurationBase
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public abstract StationActivationMethodType GetMethodType();
    }
    /// <summary>
    /// Base class for the item storage configuration.
    /// </summary>
    [XmlInclude(typeof(CorrelativeItemStorageConfiguration))]
    [XmlInclude(typeof(RandomItemStorageConfiguration))]
    [XmlInclude(typeof(DummyItemStorageConfiguration))]
    [XmlInclude(typeof(TurnoverItemStorageConfiguration))]
    [XmlInclude(typeof(ClosestLocationItemStorageConfiguration))]
    [XmlInclude(typeof(ReactiveItemStorageConfiguration))]
    [XmlInclude(typeof(EmptiestItemStorageConfiguration))]
    [XmlInclude(typeof(LeastDemandItemStorageConfiguration))]
    public abstract class ItemStorageConfiguration : ControllerConfigurationBase
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public abstract ItemStorageMethodType GetMethodType();
        /// <summary>
        /// Checks whether the item storage configuration is valid.
        /// </summary>
        /// <param name="errorMessage">A message describing the error if the configuration is not valid.</param>
        /// <returns>Indicates whether the item storage configuration is valid.</returns>
        public virtual bool AttributesAreValid(out String errorMessage)
        {
            errorMessage = "";
            return true;
        }
    }
    /// <summary>
    /// Base class for the pod storage configuration.
    /// </summary>
    [XmlInclude(typeof(DummyPodStorageConfiguration))]
    [XmlInclude(typeof(RandomPodStorageConfiguration))]
    [XmlInclude(typeof(NearestPodStorageConfiguration))]
    [XmlInclude(typeof(StationBasedPodStorageConfiguration))]
    [XmlInclude(typeof(CachePodStorageConfiguration))]
    [XmlInclude(typeof(UtilityPodStorageConfiguration))]
    [XmlInclude(typeof(FixedPodStorageConfiguration))]
    [XmlInclude(typeof(TurnoverPodStorageConfiguration))]
    public abstract class PodStorageConfiguration : ControllerConfigurationBase
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public abstract PodStorageMethodType GetMethodType();
        /// <summary>
        /// Checks whether the pod storage configuration is valid.
        /// </summary>
        /// <param name="errorMessage">A message describing the error if the configuration is not valid.</param>
        /// <returns>Indicates whether the pod storage configuration is valid.</returns>
        public virtual bool AttributesAreValid(out String errorMessage)
        {
            errorMessage = "";
            return true;
        }
    }
    /// <summary>
    /// Base class for the repositioning configuration.
    /// </summary>
    [XmlInclude(typeof(DummyRepositioningConfiguration))]
    [XmlInclude(typeof(CacheRepositioningConfiguration))]
    [XmlInclude(typeof(CacheDropoffRepositioningConfiguration))]
    [XmlInclude(typeof(UtilityRepositioningConfiguration))]
    [XmlInclude(typeof(ConceptRepositioningConfiguration))]
    public abstract class RepositioningConfiguration : ControllerConfigurationBase
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public abstract RepositioningMethodType GetMethodType();
    }
    /// <summary>
    /// Base class for the order batching configuration.
    /// </summary>
    [XmlInclude(typeof(DefaultOrderBatchingConfiguration))]
    [XmlInclude(typeof(RandomOrderBatchingConfiguration))]
    [XmlInclude(typeof(WorkloadOrderBatchingConfiguration))]
    [XmlInclude(typeof(RelatedOrderBatchingConfiguration))]
    [XmlInclude(typeof(NearBestPodOrderBatchingConfiguration))]
    [XmlInclude(typeof(ForesightOrderBatchingConfiguration))]
    [XmlInclude(typeof(PodMatchingOrderBatchingConfiguration))]
    [XmlInclude(typeof(LinesInCommonOrderBatchingConfiguration))]
    [XmlInclude(typeof(QueueOrderBatchingConfiguration))]
    public abstract class OrderBatchingConfiguration : ControllerConfigurationBase
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public abstract OrderBatchingMethodType GetMethodType();
    }
    /// <summary>
    /// Base class for the replenishment batching configuration.
    /// </summary>
    [XmlInclude(typeof(RandomReplenishmentBatchingConfiguration))]
    [XmlInclude(typeof(SamePodReplenishmentBatchingConfiguration))]
    public abstract class ReplenishmentBatchingConfiguration : ControllerConfigurationBase
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public abstract ReplenishmentBatchingMethodType GetMethodType();
    }
    /// <summary>
    /// Base class for the meta method configuration.
    /// </summary>
    [XmlInclude(typeof(NoChangeMethodManagementConfiguration))]
    [XmlInclude(typeof(RandomMethodManagementConfiguration))]
    [XmlInclude(typeof(ScheduledMethodManagementConfiguration))]
    public abstract class MethodManagementConfiguration : ControllerConfigurationBase
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public abstract MethodManagementType GetMethodType();
        /// <summary>
        /// Checks whether the method management configuration is valid.
        /// </summary>
        /// <param name="errorMessage">A message describing the error if the configuration is not valid.</param>
        /// <returns>Indicates whether the method management configuration is valid.</returns>
        public virtual bool AttributesAreValid(out String errorMessage)
        {
            errorMessage = "";
            return true;
        }
    }

    #endregion
}
