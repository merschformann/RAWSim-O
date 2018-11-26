using RAWSimO.Core.Configurations;
using RAWSimO.Core.Control.Defaults.ItemStorage;
using RAWSimO.Core.Control.Defaults.MethodManagement;
using RAWSimO.Core.Control.Defaults.OrderBatching;
using RAWSimO.Core.Control.Defaults.PathPlanning;
using RAWSimO.Core.Control.Defaults.PodStorage;
using RAWSimO.Core.Control.Defaults.ReplenishmentBatching;
using RAWSimO.Core.Control.Defaults.Repositioning;
using RAWSimO.Core.Control.Defaults.StationActivation;
using RAWSimO.Core.Control.Defaults.TaskAllocation;
using System;
using System.Linq;

namespace RAWSimO.Core.Control
{
    /// <summary>
    /// The main class containing all control mechanisms for decisions conducted during simulation.
    /// </summary>
    public class Controller
    {
        /// <summary>
        /// Creates a new controller instance.
        /// </summary>
        /// <param name="instance">The instance this controller belongs to.</param>
        public Controller(Instance instance)
        {
            Instance = instance;
            // Init path manager
            switch (instance.ControllerConfig.PathPlanningConfig.GetMethodType())
            {
                case PathPlanningMethodType.Simple: PathManager = null; break;
                case PathPlanningMethodType.Dummy: PathManager = new DummyPathManager(instance); break;
                case PathPlanningMethodType.WHCAvStar: PathManager = new WHCAvStarPathManager(instance); break;
                case PathPlanningMethodType.FAR: PathManager = new FARPathManager(instance); break;
                case PathPlanningMethodType.BCP: PathManager = new BCPPathManager(instance); break;
                case PathPlanningMethodType.CBS: PathManager = new CBSPathManager(instance); break;
                case PathPlanningMethodType.OD_ID: PathManager = new ODIDPathManager(instance); break;
                case PathPlanningMethodType.WHCAnStar: PathManager = new WHCAnStarPathManager(instance); break;
                case PathPlanningMethodType.PAS: PathManager = new PASPathManager(instance); break;
                default: throw new ArgumentException("Unknown path planning engine: " + instance.ControllerConfig.PathPlanningConfig.GetMethodType());
            }
            // Init bot manager
            switch (instance.ControllerConfig.TaskAllocationConfig.GetMethodType())
            {
                case TaskAllocationMethodType.BruteForce: BotManager = new BruteForceBotManager(instance); break;
                case TaskAllocationMethodType.Random: BotManager = new RandomBotManager(instance); break;
                case TaskAllocationMethodType.Balanced: BotManager = new BalancedBotManager(instance); break;
                case TaskAllocationMethodType.Swarm: BotManager = new SwarmBotManager(instance); break;
                case TaskAllocationMethodType.ConstantRatio: BotManager = new ConstantRatioBotManager(instance); break;
                case TaskAllocationMethodType.Concept: BotManager = new ConceptBotManager(instance); break;
                default: throw new ArgumentException("Unknown bot manager: " + instance.ControllerConfig.TaskAllocationConfig.GetMethodType());
            }
            // Init station manager
            switch (instance.ControllerConfig.StationActivationConfig.GetMethodType())
            {
                case StationActivationMethodType.ActivateAll: StationManager = new ActivateAllStationManager(instance); break;
                case StationActivationMethodType.BacklogThreshold: StationManager = new BacklogThresholdStationManager(instance); break;
                case StationActivationMethodType.ConstantRatio: StationManager = new ConstantRatioStationManager(instance); break;
                case StationActivationMethodType.WorkShift: StationManager = new WorkShiftStationActivationManager(instance); break;
                default: throw new ArgumentException("Unknown station manager: " + instance.ControllerConfig.StationActivationConfig.GetMethodType());
            }
            // Init item storage manager
            switch (instance.ControllerConfig.ItemStorageConfig.GetMethodType())
            {
                case ItemStorageMethodType.Dummy: StorageManager = new DummyStorageManager(instance); break;
                case ItemStorageMethodType.Random: StorageManager = new RandomStorageManager(instance); break;
                case ItemStorageMethodType.Correlative: StorageManager = new CorrelativeStorageManager(instance); break;
                case ItemStorageMethodType.Turnover: StorageManager = new TurnoverStorageManager(instance); break;
                case ItemStorageMethodType.ClosestLocation: StorageManager = new ClosestLocationStorageManager(instance); break;
                case ItemStorageMethodType.Reactive: StorageManager = new ReactiveStorageManager(instance); break;
                case ItemStorageMethodType.Emptiest: StorageManager = new EmptiestStorageManager(instance); break;
                case ItemStorageMethodType.LeastDemand: StorageManager = new LeastDemandStorageManager(instance); break;
                default: throw new ArgumentException("Unknown storage manager: " + instance.ControllerConfig.ItemStorageConfig.GetMethodType());
            }
            // Init pod storage manager
            switch (instance.ControllerConfig.PodStorageConfig.GetMethodType())
            {
                case PodStorageMethodType.Dummy: PodStorageManager = new DummyPodStorageManager(instance); break;
                case PodStorageMethodType.Fixed: PodStorageManager = new FixedPodStorageManager(instance); break;
                case PodStorageMethodType.Nearest: PodStorageManager = new NearestPodStorageManager(instance); break;
                case PodStorageMethodType.StationBased: PodStorageManager = new StationBasedPodStorageManager(instance); break;
                case PodStorageMethodType.Cache: PodStorageManager = new CachePodStorageManager(instance); break;
                case PodStorageMethodType.Utility: PodStorageManager = new UtilityPodStorageManager(instance); break;
                case PodStorageMethodType.Random: PodStorageManager = new RandomPodStorageManager(instance); break;
                case PodStorageMethodType.Test: PodStorageManager = new TestPodStorageManager(instance); break;
                case PodStorageMethodType.Turnover: PodStorageManager = new TurnoverPodStorageManager(instance); break;
                default: throw new ArgumentException("Unknown pod manager: " + instance.ControllerConfig.PodStorageConfig.GetMethodType());
            }
            // Init repositioning manager
            switch (instance.ControllerConfig.RepositioningConfig.GetMethodType())
            {
                case RepositioningMethodType.Dummy: RepositioningManager = new DummyRepositioningManager(instance); break;
                case RepositioningMethodType.Cache: RepositioningManager = new CacheRepositioningManager(instance); break;
                case RepositioningMethodType.CacheDropoff: RepositioningManager = new CacheDropoffRepositioningManager(instance); break;
                case RepositioningMethodType.Utility: RepositioningManager = new UtilityRepositioningManager(instance); break;
                case RepositioningMethodType.Concept: RepositioningManager = new ConceptRepositioningManager(instance); break;
                default: throw new ArgumentException("Unknown repositioning manager: " + instance.ControllerConfig.RepositioningConfig.GetMethodType());
            }
            // Init order batching manager
            switch (instance.ControllerConfig.OrderBatchingConfig.GetMethodType())
            {
                case OrderBatchingMethodType.Default: OrderManager = new DefaultOrderManager(instance); break;
                case OrderBatchingMethodType.Random: OrderManager = new RandomOrderManager(instance); break;
                case OrderBatchingMethodType.Workload: OrderManager = new WorkloadOrderManager(instance); break;
                case OrderBatchingMethodType.Related: OrderManager = new RelatedOrderManager(instance); break;
                case OrderBatchingMethodType.NearBestPod: OrderManager = new NearBestPodOrderManager(instance); break;
                case OrderBatchingMethodType.Foresight: OrderManager = new ForesightOrderManager(instance); break;
                case OrderBatchingMethodType.PodMatching: OrderManager = new PodMatchingOrderManager(instance); break;
                case OrderBatchingMethodType.LinesInCommon: OrderManager = new LinesInCommonOrderManager(instance); break;
                case OrderBatchingMethodType.Queue: OrderManager = new QueueOrderManager(instance); break;
                default: throw new ArgumentException("Unknown order manager: " + instance.ControllerConfig.OrderBatchingConfig.GetMethodType());
            }
            // Init replenishment batching manger
            switch (instance.ControllerConfig.ReplenishmentBatchingConfig.GetMethodType())
            {
                case ReplenishmentBatchingMethodType.Random: BundleManager = new RandomBundleManager(instance); break;
                case ReplenishmentBatchingMethodType.SamePod: BundleManager = new SamePodBundleManager(instance); break;
                default: throw new ArgumentException("Unknown replenishment manager: " + instance.ControllerConfig.ReplenishmentBatchingConfig.GetMethodType());
            }
            // Init meta method manager
            switch (instance.ControllerConfig.MethodManagementConfig.GetMethodType())
            {
                case MethodManagementType.NoChange: MethodManager = new NoChangeMethodManager(instance); break;
                case MethodManagementType.Random: MethodManager = new RandomMethodManager(instance); break;
                case MethodManagementType.Scheduled: MethodManager = new ScheduleMethodManager(instance); break;
                default: throw new ArgumentException("Unknown method manager: " + instance.ControllerConfig.MethodManagementConfig.GetMethodType());
            }
            // Init allocator
            Allocator = new Allocator(instance);
        }

        /// <summary>
        /// The instance to simulate.
        /// </summary>
        Instance Instance { get; set; }
        /// <summary>
        /// The method manager.
        /// </summary>
        public MethodManager MethodManager { get; private set; }
        /// <summary>
        /// The order manager.
        /// </summary>
        public OrderManager OrderManager { get; private set; }
        /// <summary>
        /// The bundle manager.
        /// </summary>
        public BundleManager BundleManager { get; private set; }
        /// <summary>
        /// The storage manager.
        /// </summary>
        public ItemStorageManager StorageManager { get; private set; }
        /// <summary>
        /// The pod storage manager.
        /// </summary>
        public PodStorageManager PodStorageManager { get; private set; }
        /// <summary>
        /// The repositioning manager.
        /// </summary>
        public RepositioningManager RepositioningManager { get; private set; }
        /// <summary>
        /// The station manager.
        /// </summary>
        public StationManager StationManager { get; private set; }
        /// <summary>
        /// The bot manager.
        /// </summary>
        public BotManager BotManager { get; private set; }
        /// <summary>
        /// The path planner.
        /// </summary>
        public PathManager PathManager { get; private set; }
        /// <summary>
        /// The allocator.
        /// </summary>
        public Allocator Allocator { get; private set; }

        /// <summary>
        /// The current time.
        /// </summary>
        private double _currentTime = 0.0;

        /// <summary>
        /// The time the simulation step is completed.
        /// </summary>
        private double _updateFinishTime = 0.0;

        /// <summary>
        /// The current time.
        /// </summary>
        public double CurrentTime { get { return _currentTime; } }

        /// <summary>
        /// The progress of the simulation.
        /// </summary>
        public double Progress { get { return _currentTime / (Instance.SettingConfig.SimulationWarmupTime + Instance.SettingConfig.SimulationDuration); } }

        /// <summary>
        /// Used to wait for workers that are still busy. (In case we simulated faster than real-time)
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        protected void WaitForUnfinishedWorker(double currentTime)
        {
            BotManager.SignalCurrentTime(currentTime);
            StationManager.SignalCurrentTime(currentTime);
            StorageManager.SignalCurrentTime(currentTime);
            PodStorageManager.SignalCurrentTime(currentTime);
            RepositioningManager.SignalCurrentTime(currentTime);
            OrderManager.SignalCurrentTime(currentTime);
            BundleManager.SignalCurrentTime(currentTime);
        }

        /// <summary>
        /// Moves the simulation forward by the specified amount of time.
        /// </summary>
        /// <param name="elapsedTime">The relative amount of time by which the simulation is forwarded.</param>
        public void Update(double elapsedTime)
        {
            // Don't want to update less than the time required for something to move past 1/3 of the tolerance in a given time interval
            // TODO this probably results in inaccurate timing statistics - is it necessary to change this? (minimum updatetime influences constant times of tasks - they are not constant anymore, because their finish event might be skipped)
            double minimumUpdateTime = Instance.SettingConfig.Tolerance / 3.0 / Instance.Bots.Max(b => b.MaxVelocity);

            _updateFinishTime = _currentTime + elapsedTime;
            while (_currentTime < _updateFinishTime)
            {
                // --> Get the next event time
                double nextTime =
                    Math.Min(_updateFinishTime, // Stop after all time is elapsed
                    Math.Min(MethodManager.GetNextEventTime(_currentTime), // Check the meta manager
                    Instance.Updateables.Min(u => u.GetNextEventTime(_currentTime)))); // Jump to next event of all agents

                // See if a potential collision will happen before the next event
                double minTimeDelta = Math.Min(Instance.Compound.GetShortestTimeWithoutCollision(), nextTime - _currentTime);
                minTimeDelta = Math.Max(minTimeDelta, minimumUpdateTime);	// Make sure update rate never gets too slow

                // Update by at least the minimum, but don't go past the next time
                nextTime = Math.Min(_updateFinishTime, _currentTime + minTimeDelta);

                // Wait for unfinished optimization workers
                WaitForUnfinishedWorker(nextTime);

                // --> Run up til the next event
                // Update method manager (needs to be updated first, because it might change the update-list)
                MethodManager.Update(_currentTime, nextTime);
                // Update all agents in the list
                foreach (var updateable in Instance.Updateables)
                    updateable.Update(_currentTime, nextTime);

                // Set new time
                _currentTime = nextTime;
            }
        }

        #region Manager exchange handling

        /// <summary>
        /// Exchanges the active pod storage manager with the given one.
        /// </summary>
        /// <param name="newManager">The new manager.</param>
        public void ExchangePodStorageManager(PodStorageManager newManager)
        {
            Instance.RemoveUpdateable(PodStorageManager);
            PodStorageManager = newManager;
            Instance.AddUpdateable(newManager);
        }

        #endregion

    }
}
