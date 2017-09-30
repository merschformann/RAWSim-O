using RAWSimO.Core.Configurations;
using RAWSimO.Core.Control.Defaults.ReplenishmentBatching;
using RAWSimO.Core.IO;
using RAWSimO.Core.Metrics;
using RAWSimO.MultiAgentPathFinding.Methods;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Playground.Generators
{
    class ConfigGenerators
    {
        private static SettingConfiguration GetRotterdamBaseSetting()
        {
            return new SettingConfiguration()
            {
                SimulationDuration = TimeSpan.FromDays(2).TotalSeconds,
                LogFileLevel = LogFileLevel.FootprintOnly,
                InventoryConfiguration = new InventoryConfiguration()
                {
                    InitialInventory = 0.7,
                    OrderMode = Core.Management.OrderMode.Fill,
                    OrderPositionCountMean = 1,
                    OrderPositionCountStdDev = 0.3,
                    OrderPositionCountMin = 1,
                    OrderPositionCountMax = 4,
                    PositionCountMean = 1,
                    PositionCountStdDev = 0.3,
                    PositionCountMin = 1,
                    PositionCountMax = 3,
                    DemandInventoryConfiguration = new DemandInventoryConfiguration()
                    {
                        BundleCount = 200,
                        OrderCount = 200,
                        InventoryLevelDrivenBundleGeneration = true,
                        InventoryLevelDrivenOrderGeneration = true,
                        InventoryLevelBundleRestartThreshold = 0.65,
                        InventoryLevelBundleStopThreshold = 0.85,
                        InventoryLevelOrderRestartThreshold = 0.6,
                        InventoryLevelOrderStopThreshold = 0.1,
                    },
                },
            };
        }

        public static void GenerateRotterdamPhase2Settings()
        {
            // --> Begin
            Console.WriteLine("Initializing ...");


            Dictionary<string, string> skuFiles = new Dictionary<string, string> { { "Mu-1000.xgenc", "1000" }, { "Mu-10000.xgenc", "10000" }, };
            List<int> orderSizeScenarios = new List<int> { 1, 2, 3 };
            List<double> returnOrderProbabilities = new List<double> { 0, 0.3 };

            //List<int> pickStationCapacities = new List<int> { 6, 12, 18 };
            List<double> pickStationCounts = new List<double> { 1.0 / 6.0, 2.0 / 6.0, 3.0 / 6.0, 4.0 / 6.0, 5.0 / 6.0, 6.0 / 6.0, };
            //List<double> replStationCounts = new List<double> { 1.0 / 3.0, 2.0 / 3.0, 1.0 };
            List<int> botsPerStations = new List<int> { 2, 3, 4, 5, 6 };

            // Build all combinations
            foreach (var botsPerStation in botsPerStations)
            {
                foreach (var skuFile in skuFiles)
                {
                    foreach (var pickStationCount in pickStationCounts)
                    {
                        foreach (var orderSizeScenario in orderSizeScenarios)
                        {
                            foreach (var returnOrderProbability in returnOrderProbabilities)
                            {
                                SettingConfiguration setting = GetRotterdamBaseSetting();
                                setting.OverrideConfig = new OverrideConfiguration()
                                {
                                    OverrideBotCountPerOStation = true,
                                    OverrideBotCountPerOStationValue = botsPerStation,
                                    OverrideOutputStationCapacity = true,
                                    OverrideOutputStationCapacityValue = 8,
                                    OverrideInputStationCount = true,
                                    OverrideInputStationCountValue = 2.0 / 6.0,
                                    OverrideOutputStationCount = true,
                                    OverrideOutputStationCountValue = pickStationCount,
                                };
                                setting.InventoryConfiguration.SimpleItemConfiguration.GeneratorConfigFile = skuFile.Key;
                                switch (orderSizeScenario)
                                {
                                    case 1: /* Do nothing - default scenario */ break;
                                    case 2:
                                        setting.InventoryConfiguration.OrderPositionCountMin = 1;
                                        setting.InventoryConfiguration.OrderPositionCountMax = 1;
                                        setting.InventoryConfiguration.PositionCountMin = 1;
                                        setting.InventoryConfiguration.PositionCountMax = 1;
                                        break;
                                    case 3:
                                        setting.InventoryConfiguration.OrderPositionCountMin = 2;
                                        setting.InventoryConfiguration.PositionCountMin = 2;
                                        break;
                                    default: throw new ArgumentException("Unknown order size scenario: " + orderSizeScenario);
                                }
                                setting.InventoryConfiguration.ReturnOrderProbability = returnOrderProbability;
                                setting.CommentTag1 = "Scen" +
                                    "-SKU" + skuFile.Value +
                                    "-O" + (orderSizeScenario == 1 ? "M" : (orderSizeScenario == 2 ? "S" : "L")) +
                                    "-RO" + (returnOrderProbability > 0 ? "t" : "f");
                                string name = "MaTi" +
                                    "-SKU" + skuFile.Value +
                                    "-O" + (orderSizeScenario == 1 ? "M" : (orderSizeScenario == 2 ? "S" : "L")) +
                                    "-RO" + (returnOrderProbability > 0 ? "t" : "f") +
                                    "-BPOS" + botsPerStation.ToString(IOConstants.EXPORT_FORMAT_SHORTEST_BY_ROUNDING, IOConstants.FORMATTER) +
                                    //"-C" + pickStationCapacity.ToString(IOConstants.FORMATTER) +
                                    "-P" + pickStationCount.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) +
                                    //"-R" + replStationCount.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) +
                                    "";
                                setting.Name = name;
                                string fileName = setting.Name + ".xsett";
                                Console.WriteLine("Saving " + fileName + " ...");
                                InstanceIO.WriteSetting(fileName, setting);
                            }
                        }
                    }
                }
            }
        }

        public static void GenerateRotterdamControllers()
        {
            // --> Begin
            Console.WriteLine("Initializing ...");

            // --> Initialize sets of configurations to generate later on
            List<PathPlanningConfiguration> pathplanners = new List<PathPlanningConfiguration>()
            {
                // WHCA_v^*
                new WHCAvStarPathPlanningConfiguration() {
                    Name = "WHCAv",
                    AutoSetParameter = false,
                    LengthOfAWaitStep = 2,
                    RuntimeLimitPerAgent = 0.1,
                    Clocking = 1,
                    LengthOfAWindow = 20,
                    AbortAtFirstConflict = true,
                    UseDeadlockHandler = true,
                },
            };
            List<TaskAllocationConfiguration> taskAllocaters = new List<TaskAllocationConfiguration>()
            {
                // Balanced work amount
                new BalancedTaskAllocationConfiguration() {
                    Name = "Pile-on",
                    PodSelectionConfig = new DefaultPodSelectionConfiguration()
                    {
                        OutputPodScorer = new PCScorerPodForOStationBotWorkAmount()
                        { ValueMetric = PCScorerWorkAmountValueMetric.Picks, BacklogWeight = 0, CompleteableOrderBoost = 0, CompleteableQueuedOrders = false, TimeCosts = 0 },
                        OutputPodScorerTieBreaker1 = new PCScorerPodForOStationBotCompleteable()
                        { IncludeQueuedOrders = false },
                        OutputPodScorerTieBreaker2 = new PCScorerPodForOStationBotRandom() { },
                        OutputExtendedSearchScorer = new PCScorerOStationForBotWithPodWorkAmount()
                        { ValueMetric = PCScorerWorkAmountValueMetric.Picks, OnlyPositiveLateness = true },
                        OutputExtendedSearchScorerTieBreaker1 = new PCScorerOStationForBotWithPodWorkAmount()
                        { ValueMetric = PCScorerWorkAmountValueMetric.Picks, OnlyPositiveLateness = false },
                        OutputExtendedSearchScorerTieBreaker2 = new PCScorerOStationForBotWithPodRandom() { },
                        InputPodScorer = new PCScorerPodForIStationBotWorkAmount() { },
                        InputPodScorerTieBreaker1 = new PCScorerPodForIStationBotRandom() { },
                        InputPodScorerTieBreaker2 = new PCScorerPodForIStationBotRandom() { },
                        InputExtendedSearchScorer = new PCScorerIStationForBotWithPodWorkAmount() { },
                        InputExtendedSearchScorerTieBreaker1 = new PCScorerIStationForBotWithPodRandom() { },
                        InputExtendedSearchScorerTieBreaker2 = new PCScorerIStationForBotWithPodRandom() { },
                    },
                },
                // Balanced work amount
                new BalancedTaskAllocationConfiguration() {
                    Name = "Age",
                    PodSelectionConfig = new DefaultPodSelectionConfiguration()
                    {
                        OutputPodScorer = new PCScorerPodForOStationBotWorkAmount()
                        { ValueMetric = PCScorerWorkAmountValueMetric.OrderAge, BacklogWeight = 0, CompleteableOrderBoost = 0, CompleteableQueuedOrders = false, TimeCosts = 0 },
                        OutputPodScorerTieBreaker1 = new PCScorerPodForOStationBotCompleteable()
                        { IncludeQueuedOrders = false },
                        OutputPodScorerTieBreaker2 = new PCScorerPodForOStationBotRandom() { },
                        OutputExtendedSearchScorer = new PCScorerOStationForBotWithPodWorkAmount()
                        { ValueMetric = PCScorerWorkAmountValueMetric.OrderAge, OnlyPositiveLateness = true },
                        OutputExtendedSearchScorerTieBreaker1 = new PCScorerOStationForBotWithPodWorkAmount()
                        { ValueMetric = PCScorerWorkAmountValueMetric.OrderAge, OnlyPositiveLateness = false },
                        OutputExtendedSearchScorerTieBreaker2 = new PCScorerOStationForBotWithPodRandom() { },
                        InputPodScorer = new PCScorerPodForIStationBotWorkAmount() { },
                        InputPodScorerTieBreaker1 = new PCScorerPodForIStationBotRandom() { },
                        InputPodScorerTieBreaker2 = new PCScorerPodForIStationBotRandom() { },
                        InputExtendedSearchScorer = new PCScorerIStationForBotWithPodWorkAmount() { },
                        InputExtendedSearchScorerTieBreaker1 = new PCScorerIStationForBotWithPodRandom() { },
                        InputExtendedSearchScorerTieBreaker2 = new PCScorerIStationForBotWithPodRandom() { },
                    },
                },
                // Balanced pile on age
                new BalancedTaskAllocationConfiguration() {
                    Name = "Lateness",
                    PodSelectionConfig = new DefaultPodSelectionConfiguration()
                    {
                        OutputPodScorer = new PCScorerPodForOStationBotWorkAmount()
                        { ValueMetric = PCScorerWorkAmountValueMetric.OrderDueTime, OnlyPositiveLateness = true, BacklogWeight = 0, CompleteableOrderBoost = 0, CompleteableQueuedOrders = false, TimeCosts = 0 },
                        OutputPodScorerTieBreaker1 = new PCScorerPodForOStationBotWorkAmount()
                        { ValueMetric = PCScorerWorkAmountValueMetric.OrderDueTime, OnlyPositiveLateness = false, BacklogWeight = 0, CompleteableOrderBoost = 0, CompleteableQueuedOrders = false, TimeCosts = 0 },
                        OutputPodScorerTieBreaker2 = new PCScorerPodForOStationBotRandom() { },
                        OutputExtendedSearchScorer = new PCScorerOStationForBotWithPodWorkAmount()
                        { ValueMetric = PCScorerWorkAmountValueMetric.OrderDueTime, OnlyPositiveLateness = true },
                        OutputExtendedSearchScorerTieBreaker1 = new PCScorerOStationForBotWithPodWorkAmount()
                        { ValueMetric = PCScorerWorkAmountValueMetric.OrderDueTime, OnlyPositiveLateness = false },
                        OutputExtendedSearchScorerTieBreaker2 = new PCScorerOStationForBotWithPodRandom() { },
                        InputPodScorer = new PCScorerPodForIStationBotWorkAmount() { },
                        InputPodScorerTieBreaker1 = new PCScorerPodForIStationBotRandom() { },
                        InputPodScorerTieBreaker2 = new PCScorerPodForIStationBotRandom() { },
                        InputExtendedSearchScorer = new PCScorerIStationForBotWithPodWorkAmount() { },
                        InputExtendedSearchScorerTieBreaker1 = new PCScorerIStationForBotWithPodRandom() { },
                        InputExtendedSearchScorerTieBreaker2 = new PCScorerIStationForBotWithPodRandom() { },
                    },
                },
                // Nearest
                new BalancedTaskAllocationConfiguration() {
                    Name = "Nearest",
                    PodSelectionConfig = new DefaultPodSelectionConfiguration()
                    {
                        OutputPodScorer = new PCScorerPodForOStationBotNearest() { },
                        OutputPodScorerTieBreaker1 = new PCScorerPodForOStationBotRandom() { },
                        OutputPodScorerTieBreaker2 = new PCScorerPodForOStationBotRandom() { },
                        OutputExtendedSearchScorer = new PCScorerOStationForBotWithPodNearest() { },
                        OutputExtendedSearchScorerTieBreaker1 = new PCScorerOStationForBotWithPodRandom() { },
                        OutputExtendedSearchScorerTieBreaker2 = new PCScorerOStationForBotWithPodRandom() { },
                        InputPodScorer = new PCScorerPodForIStationBotNearest() { },
                        InputPodScorerTieBreaker1 = new PCScorerPodForIStationBotRandom() { },
                        InputPodScorerTieBreaker2 = new PCScorerPodForIStationBotRandom() { },
                        InputExtendedSearchScorer = new PCScorerIStationForBotWithPodNearest() { },
                        InputExtendedSearchScorerTieBreaker1 = new PCScorerIStationForBotWithPodRandom() { },
                        InputExtendedSearchScorerTieBreaker2 = new PCScorerIStationForBotWithPodRandom() { },
                    },
                },
                // Demand
                new BalancedTaskAllocationConfiguration() {
                    Name = "Demand",
                    PodSelectionConfig = new DefaultPodSelectionConfiguration()
                    {
                        OutputPodScorer = new PCScorerPodForOStationBotDemand() { },
                        OutputPodScorerTieBreaker1 = new PCScorerPodForOStationBotRandom() { },
                        OutputPodScorerTieBreaker2 = new PCScorerPodForOStationBotRandom() { },
                        OutputExtendedSearchScorer = new PCScorerOStationForBotWithPodNearest() { },
                        OutputExtendedSearchScorerTieBreaker1 = new PCScorerOStationForBotWithPodRandom() { },
                        OutputExtendedSearchScorerTieBreaker2 = new PCScorerOStationForBotWithPodRandom() { },
                        InputPodScorer = new PCScorerPodForIStationBotDemand() { },
                        InputPodScorerTieBreaker1 = new PCScorerPodForIStationBotRandom() { },
                        InputPodScorerTieBreaker2 = new PCScorerPodForIStationBotRandom() { },
                        InputExtendedSearchScorer = new PCScorerIStationForBotWithPodNearest() { },
                        InputExtendedSearchScorerTieBreaker1 = new PCScorerIStationForBotWithPodRandom() { },
                        InputExtendedSearchScorerTieBreaker2 = new PCScorerIStationForBotWithPodRandom() { },
                    },
                },
                // Random
                new BalancedTaskAllocationConfiguration() {
                    Name = "Random",
                    PodSelectionConfig = new DefaultPodSelectionConfiguration()
                    {
                        OutputPodScorer = new PCScorerPodForOStationBotRandom() { },
                        OutputPodScorerTieBreaker1 = new PCScorerPodForOStationBotRandom() { },
                        OutputPodScorerTieBreaker2 = new PCScorerPodForOStationBotRandom() { },
                        OutputExtendedSearchScorer = new PCScorerOStationForBotWithPodRandom() { },
                        OutputExtendedSearchScorerTieBreaker1 = new PCScorerOStationForBotWithPodRandom() { },
                        OutputExtendedSearchScorerTieBreaker2 = new PCScorerOStationForBotWithPodRandom() { },
                        InputPodScorer = new PCScorerPodForIStationBotRandom() { },
                        InputPodScorerTieBreaker1 = new PCScorerPodForIStationBotRandom() { },
                        InputPodScorerTieBreaker2 = new PCScorerPodForIStationBotRandom() { },
                        InputExtendedSearchScorer = new PCScorerIStationForBotWithPodRandom() { },
                        InputExtendedSearchScorerTieBreaker1 = new PCScorerIStationForBotWithPodRandom() { },
                        InputExtendedSearchScorerTieBreaker2 = new PCScorerIStationForBotWithPodRandom() { },
                    },
                },
            };
            List<ItemStorageConfiguration> itemStoragers = new List<ItemStorageConfiguration>()
            {
                // Random
                new RandomItemStorageConfiguration() { Name = "Random", StickToPodUntilFull = false },
                // Closest
                new ClosestLocationItemStorageConfiguration() { Name = "Nearest" },
                // Emptiest
                new EmptiestItemStorageConfiguration() { Name = "Emptiest" },
                // Turnover
                new TurnoverItemStorageConfiguration() { Name = "Class" },
                // LeastDemand
                new LeastDemandItemStorageConfiguration() { Name = "LeastDemand" },
            };
            List<PodStorageConfiguration> podStoragers = new List<PodStorageConfiguration>()
            {
                // Random
                new RandomPodStorageConfiguration() { Name = "Random" },
                // Fixed
                new FixedPodStorageConfiguration() { Name = "Fixed"  },
                // Nearest
                new NearestPodStorageConfiguration() { Name = "Nearest" },
                // Station based
                new StationBasedPodStorageConfiguration() { Name = "StationBased" },
                // Turnover
                new TurnoverPodStorageConfiguration() { Name = "Class" },
            };
            List<ReplenishmentBatchingConfiguration> replenishmentBatchers = new List<ReplenishmentBatchingConfiguration>()
            {
                // Random
                new RandomReplenishmentBatchingConfiguration() { Name = "Random" },
                // SamePod (least busy station)
                new SamePodReplenishmentBatchingConfiguration() { Name = "SamePod", FirstStationRule = SamePodFirstStationRule.LeastBusy },
            };
            List<OrderBatchingConfiguration> orderBatchers = new List<OrderBatchingConfiguration>()
            {
                // Random
                new DefaultOrderBatchingConfiguration() {
                    Name = "Random",
                    OrderSelectionRule = Core.Control.Defaults.OrderBatching.DefaultOrderSelection.Random,
                    StationSelectionRule = Core.Control.Defaults.OrderBatching.DefaultOutputStationSelection.Random,
                },
                // FCFS
                new DefaultOrderBatchingConfiguration() {
                    Name = "FCFS",
                    OrderSelectionRule = Core.Control.Defaults.OrderBatching.DefaultOrderSelection.FCFS,
                    StationSelectionRule = Core.Control.Defaults.OrderBatching.DefaultOutputStationSelection.Random,
                },
                // Earliest due time
                new DefaultOrderBatchingConfiguration() {
                    Name = "DueTime",
                    OrderSelectionRule = Core.Control.Defaults.OrderBatching.DefaultOrderSelection.DueTime,
                    StationSelectionRule = Core.Control.Defaults.OrderBatching.DefaultOutputStationSelection.Random,
                },
                // Lines in common
                new LinesInCommonOrderBatchingConfiguration() {
                    Name = "CommonLines",
                    TieBreaker = Core.Control.Shared.OrderSelectionTieBreaker.Random,
                    FastLane = false,
                    FastLaneTieBreaker = Core.Control.Shared.FastLaneTieBreaker.Random,
                },
                // Pod matching
                new PodMatchingOrderBatchingConfiguration() {
                    Name = "PodMatch",
                    TieBreaker = Core.Control.Shared.OrderSelectionTieBreaker.Random,
                    FastLane = false,
                    FastLaneTieBreaker = Core.Control.Shared.FastLaneTieBreaker.Random,
                    LateBeforeMatch = false,
                },
                // Fast lane
                new DefaultOrderBatchingConfiguration()
                {
                    Name = "FastLane",
                    OrderSelectionRule = Core.Control.Defaults.OrderBatching.DefaultOrderSelection.Random,
                    StationSelectionRule = Core.Control.Defaults.OrderBatching.DefaultOutputStationSelection.Random,
                    FastLane = true,
                    FastLaneTieBreaker = Core.Control.Shared.FastLaneTieBreaker.Random,
                },
                //// Late matching
                //new PodMatchingOrderBatchingConfiguration() {
                //    Name = "PodMatchLate",
                //    FastLane = false,
                //    UseEarliestDueTime = false,
                //    LateBeforeMatch = true,
                //},
            };

            // --> Create collection of forbidden combinations that shall not be generated
            Console.WriteLine("Creating forbidden combinations ...");
            SymmetricKeyDictionary<ControllerConfigurationBase, bool> forbiddenCombinations = new SymmetricKeyDictionary<ControllerConfigurationBase, bool>();
            foreach (var replenishmentBatcher in replenishmentBatchers.Where(r => r is SamePodReplenishmentBatchingConfiguration))
            {
                foreach (var itemStorager in itemStoragers.Where(i => i is ClosestLocationItemStorageConfiguration))
                {
                    forbiddenCombinations[replenishmentBatcher, itemStorager] = true;
                }
            }

            // --> Generate the configurations
            Console.WriteLine("Generating all configurations ...");
            List<ControlConfiguration> configurations = new List<ControlConfiguration>();
            foreach (var pathPlanner in pathplanners)
            {
                foreach (var taskAllocater in taskAllocaters)
                {
                    foreach (var itemStorager in itemStoragers)
                    {
                        foreach (var podStorager in podStoragers)
                        {
                            foreach (var replenishmentBatcher in replenishmentBatchers)
                            {
                                foreach (var orderBatcher in orderBatchers)
                                {
                                    // Check for invalid combinations
                                    List<ControllerConfigurationBase> configObjects = new List<ControllerConfigurationBase>()
                                    { pathPlanner, taskAllocater, itemStorager, podStorager, replenishmentBatcher, orderBatcher };
                                    bool isInvalid = false;
                                    foreach (var firstConfig in configObjects)
                                        foreach (var secondConfig in configObjects.Where(c => c != firstConfig))
                                            if (forbiddenCombinations.ContainsKey(firstConfig, secondConfig))
                                                isInvalid = true;
                                    // Add the config if it is not invalid
                                    if (!isInvalid)
                                    {
                                        configurations.Add(new ControlConfiguration()
                                        {
                                            PathPlanningConfig = pathPlanner,
                                            TaskAllocationConfig = taskAllocater,
                                            ItemStorageConfig = itemStorager,
                                            PodStorageConfig = podStorager,
                                            ReplenishmentBatchingConfig = replenishmentBatcher,
                                            OrderBatchingConfig = orderBatcher,
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // --> Save configurations
            Console.WriteLine("Writing all configurations ...");
            foreach (var config in configurations)
            {
                // Prepare
                config.Name =
                    "PP" + config.PathPlanningConfig.Name + "-" +
                    "TA" + config.TaskAllocationConfig.Name + "-" +
                    "IS" + config.ItemStorageConfig.Name + "-" +
                    "PS" + config.PodStorageConfig.Name + "-" +
                    "OB" + config.OrderBatchingConfig.Name + "-" +
                    "RB" + config.ReplenishmentBatchingConfig.Name;
                // Make last minute changes
                if (config.PodStorageConfig is TurnoverPodStorageConfiguration && !(config.ItemStorageConfig is TurnoverItemStorageConfiguration))
                    (config.PodStorageConfig as TurnoverPodStorageConfiguration).ReallocationDelay = 1200;
                // Save
                string fileName = config.Name + ".xconf";
                Console.WriteLine("Saving " + fileName + " ...");
                InstanceIO.WriteConfiguration(fileName, config);
                // Revert last minute changes
                if (config.PodStorageConfig is TurnoverPodStorageConfiguration && !(config.ItemStorageConfig is TurnoverItemStorageConfiguration))
                    (config.PodStorageConfig as TurnoverPodStorageConfiguration).ReallocationDelay = 0;
            }
        }

        private static SettingConfiguration GetRepositioningBaseSetting()
        {
            return new SettingConfiguration()
            {
                SimulationDuration = TimeSpan.FromDays(7).TotalSeconds,
                InventoryConfiguration = new InventoryConfiguration()
                {
                    InitialInventory = 0.7,
                    DemandInventoryConfiguration = new DemandInventoryConfiguration()
                    {
                        BundleCount = 200,
                        OrderCount = 2000,
                        InventoryLevelDrivenBundleGeneration = true,
                        InventoryLevelDrivenOrderGeneration = true,
                        InventoryLevelBundleRestartThreshold = 0.5,
                        InventoryLevelBundleStopThreshold = 0.9,
                        InventoryLevelOrderRestartThreshold = 0.5,
                        InventoryLevelOrderStopThreshold = 0.1,
                    },
                    SimpleItemConfiguration = new SimpleItemConfiguration()
                    {
                        GeneratorConfigFile = "Repo-1000.xgenc",
                    },
                },
            };
        }

        public static void GenerateRepositioningSet3()
        {
            Dictionary<bool, StationActivationConfiguration> stationControllers = new Dictionary<bool, StationActivationConfiguration>()
            {
                { false, new ActivateAllStationActivationConfiguration() },
                { true, new WorkShiftStationActivationConfiguration(new DefaultConstructorIdentificationClass()) },
            };
            List<Tuple<double, double>> speedUtilityWeights = new List<Tuple<double, double>>()
            {
                new Tuple<double, double>(0, 1),
                new Tuple<double, double>(1, 0),
                new Tuple<double, double>(1, 1),
            };
            List<Tuple<PodStorageConfiguration, RepositioningConfiguration>> positioningControllers = new List<Tuple<PodStorageConfiguration, RepositioningConfiguration>>()
            {
                new Tuple<PodStorageConfiguration, RepositioningConfiguration>(
                    new NearestPodStorageConfiguration(),
                    new UtilityRepositioningConfiguration()),
                new Tuple<PodStorageConfiguration, RepositioningConfiguration>(
                    new NearestPodStorageConfiguration(),
                    new CacheDropoffRepositioningConfiguration()),
                new Tuple<PodStorageConfiguration, RepositioningConfiguration>(
                    new UtilityPodStorageConfiguration(),
                    new UtilityRepositioningConfiguration()),
                new Tuple<PodStorageConfiguration, RepositioningConfiguration>(
                    new CachePodStorageConfiguration(),
                    new CacheDropoffRepositioningConfiguration()),
            };
            List<OrderBatchingConfiguration> orderBatchers = new List<OrderBatchingConfiguration>()
            {
                new DefaultOrderBatchingConfiguration() {
                    OrderSelectionRule = Core.Control.Defaults.OrderBatching.DefaultOrderSelection.FCFS,
                    StationSelectionRule = Core.Control.Defaults.OrderBatching.DefaultOutputStationSelection.Random,
                },
            };
            List<RepositioningSubExperiment> subExperiments = new List<RepositioningSubExperiment>()
            {
                new RepositioningSubExperiment() { NightDown = false, BotsPerOStation = 4, BotAllocation = new Tuple<bool, double, double, double>(false, 1, 3, 0) },
                new RepositioningSubExperiment() { NightDown = false, BotsPerOStation = 4, BotAllocation = new Tuple<bool, double, double, double>(true, 1, 2, 1) },
                new RepositioningSubExperiment() { NightDown = false, BotsPerOStation = 5, BotAllocation = new Tuple<bool, double, double, double>(true, 1, 3, 1) },
                new RepositioningSubExperiment() { NightDown = true, BotsPerOStation = 4, BotAllocation = new Tuple<bool, double, double, double>(true, 1, 3, 0) },
                new RepositioningSubExperiment() { NightDown = true, BotsPerOStation = 4, BotAllocation = new Tuple<bool, double, double, double>(false, 1, 3, 0) },
            };
            int counter = 0;
            foreach (var subExperiment in subExperiments)
            {
                string dir = "SubExp" + (++counter);
                while (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                foreach (var speedUtilityWeight in speedUtilityWeights)
                {
                    foreach (var positioningController in positioningControllers)
                    {
                        foreach (var orderBatcher in orderBatchers)
                        {
                            // Prepare setting
                            SettingConfiguration setting = GetRepositioningBaseSetting();
                            setting.OverrideConfig = new OverrideConfiguration()
                            {
                                OverrideBotCountPerOStation = true,
                                OverrideBotCountPerOStationValue = subExperiment.BotsPerOStation,
                            };
                            if (subExperiment.NightDown)
                            {
                                setting.InventoryConfiguration.DemandInventoryConfiguration.BundleCount = 0;
                                setting.InventoryConfiguration.SubmitBatches = true;
                                setting.InventoryConfiguration.BatchInventoryConfiguration = new BatchInventoryConfiguration()
                                {
                                    MaxTimeForBundleSubmissions = TimeSpan.FromDays(1).TotalSeconds,
                                    MaxTimeForOrderSubmissions = TimeSpan.FromDays(1).TotalSeconds,
                                    BundleBatches = new List<Skvp<double, double>>() { new Skvp<double, double>() { Key = TimeSpan.FromHours(16).TotalSeconds, Value = 0.75 } },
                                    OrderBatches = new List<Skvp<double, int>>() { new Skvp<double, int>() { Key = TimeSpan.FromHours(22).TotalSeconds, Value = 1500 } },
                                };
                            }
                            // Prepare config
                            ControlConfiguration config = new ControlConfiguration()
                            {
                                StationActivationConfig = stationControllers[subExperiment.NightDown],
                                TaskAllocationConfig = new BalancedTaskAllocationConfiguration()
                                {
                                    RepositionBeforeRest = subExperiment.BotAllocation.Item1,
                                    WeightInputStations = subExperiment.BotAllocation.Item2,
                                    WeightOutputStations = subExperiment.BotAllocation.Item3,
                                    WeightRepositioning = subExperiment.BotAllocation.Item4,
                                },
                                OrderBatchingConfig = orderBatcher,
                                PodStorageConfig = positioningController.Item1,
                                RepositioningConfig = positioningController.Item2,
                            };
                            // Set weights
                            if (config.PodStorageConfig is CachePodStorageConfiguration)
                            {
                                (config.PodStorageConfig as CachePodStorageConfiguration).WeightSpeed = speedUtilityWeight.Item1;
                                (config.PodStorageConfig as CachePodStorageConfiguration).WeightUtility = speedUtilityWeight.Item2;
                            }
                            if (config.PodStorageConfig is UtilityPodStorageConfiguration)
                            {
                                (config.PodStorageConfig as UtilityPodStorageConfiguration).UtilityConfig.WeightSpeed = speedUtilityWeight.Item1;
                                (config.PodStorageConfig as UtilityPodStorageConfiguration).UtilityConfig.WeightUtility = speedUtilityWeight.Item2;
                            }
                            if (config.RepositioningConfig is CacheDropoffRepositioningConfiguration)
                            {
                                (config.RepositioningConfig as CacheDropoffRepositioningConfiguration).WeightSpeed = speedUtilityWeight.Item1;
                                (config.RepositioningConfig as CacheDropoffRepositioningConfiguration).WeightUtility = speedUtilityWeight.Item2;
                            }
                            if (config.RepositioningConfig is UtilityRepositioningConfiguration)
                            {
                                (config.RepositioningConfig as UtilityRepositioningConfiguration).UtilityConfig.WeightSpeed = speedUtilityWeight.Item1;
                                (config.RepositioningConfig as UtilityRepositioningConfiguration).UtilityConfig.WeightUtility = speedUtilityWeight.Item2;
                            }
                            // Name setting
                            setting.Name = "ScenRep-" + (subExperiment.NightDown ? "NightDown" : "NoDown") + "-BPOS" + subExperiment.BotsPerOStation;
                            // Name controller
                            string saTag;
                            if (config.StationActivationConfig is ActivateAllStationActivationConfiguration) saTag = "A";
                            else if (config.StationActivationConfig is WorkShiftStationActivationConfiguration) saTag = "W";
                            else throw new ArgumentException("Unknown!");
                            string psTag;
                            if (config.PodStorageConfig is RandomPodStorageConfiguration) psTag = "R";
                            else if (config.PodStorageConfig is NearestPodStorageConfiguration) psTag = "N";
                            else if (config.PodStorageConfig is CachePodStorageConfiguration) psTag = "C";
                            else if (config.PodStorageConfig is UtilityPodStorageConfiguration) psTag = "U";
                            else throw new ArgumentException("Unknown!");
                            string rpTag;
                            if (config.RepositioningConfig is DummyRepositioningConfiguration) rpTag = "D";
                            else if (config.RepositioningConfig is CacheDropoffRepositioningConfiguration) rpTag = "C";
                            else if (config.RepositioningConfig is UtilityRepositioningConfiguration) rpTag = "U";
                            else throw new ArgumentException("Unknown!");
                            string obTag;
                            if (config.OrderBatchingConfig is QueueOrderBatchingConfiguration) obTag = "Q";
                            else if (config.OrderBatchingConfig is PodMatchingOrderBatchingConfiguration) obTag = "M";
                            else if (config.OrderBatchingConfig is DefaultOrderBatchingConfiguration) obTag = "D";
                            else throw new ArgumentException("Unknown!");
                            config.Name =
                            "PS" + psTag + "-" +
                            "RP" + rpTag + "-" +
                            "SA" + saTag + "-" +
                            "OB" + obTag + "-" +
                            "Bot" + (subExperiment.BotAllocation.Item1 ? "t" : "f") + "I" + subExperiment.BotAllocation.Item2 + "O" + subExperiment.BotAllocation.Item3 + "R" + subExperiment.BotAllocation.Item4 + "-" +
                            "Wei" +
                                speedUtilityWeight.Item1.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) +
                                speedUtilityWeight.Item2.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER);
                            // Save it
                            string fileNameController = Path.Combine(dir, config.Name + ".xconf");
                            Console.WriteLine("Saving " + fileNameController + " ...");
                            InstanceIO.WriteConfiguration(fileNameController, config);
                            string fileNameSetting = Path.Combine(dir, setting.Name + ".xsett");
                            Console.WriteLine("Saving " + fileNameSetting + " ...");
                            InstanceIO.WriteSetting(fileNameSetting, setting);
                        }
                    }
                }
            }
        }

        private class RepositioningSubExperiment
        {
            public bool NightDown;
            public int BotsPerOStation;
            public Tuple<bool, double, double, double> BotAllocation;
        }
    }
}
