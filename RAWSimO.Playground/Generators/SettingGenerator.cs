using RAWSimO.Core.Configurations;
using RAWSimO.Core.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Playground.Generators
{
    /// <summary>
    /// Used to generate setting files programmatically.
    /// </summary>
    internal class SettingGenerator
    {
        internal static void GenerateRotterdamMark2Set()
        {
            // --> Begin
            Console.WriteLine("Initializing ...");

            // SKUs
            List<string> skuScenarios = new List<string>
            {
                // 100 SKUs
                "Mu-100.xgenc",
                // 500 SKUs
                "Mu-500.xgenc",
                // 1000 SKUs
                "Mu-1000.xgenc",
            };
            // BotsPerOStation
            List<int> botsPerOStationCounts = new List<int>
            {
                // 2 bots per output station
                2,
                // 4 bots per output station
                4,
                // 6 bots per output station
                6,
                // 8 bots per output station
                8,
            };
            // OStationCapacity
            List<int> oStationCapacities = new List<int>
            {
                // 6 orders in parallel
                6,
                // 12 orders in parallel
                12,
                // 18 orders in parallel
                18,
            };
            // ReplenishmentStations
            List<double> iStationAmounts = new List<double>
            {
                // Use only one (the override will always save the last station)
                0,
                // Use only 50 %
                0.5,
                // Use all
                1,
            };

            // --> Generate the settings
            Console.WriteLine("Generating all settings ...");
            List<SettingConfiguration> settings = new List<SettingConfiguration>();
            foreach (var skuScenario in skuScenarios)
            {
                foreach (var botsPerOStationCount in botsPerOStationCounts)
                {
                    foreach (var oStationCapacity in oStationCapacities)
                    {
                        foreach (var iStationAmount in iStationAmounts)
                        {
                            settings.Add(new SettingConfiguration()
                            {
                                Name = "Mu" +
                                    "-SKU" + skuScenario.Replace("Mu-", "").Replace(".xgenc", "") +
                                    "-B" + botsPerOStationCount +
                                    "-O" + oStationCapacity +
                                    "-I" + iStationAmount.ToString(IOConstants.FORMATTER),
                                SimulationWarmupTime = 0,
                                SimulationDuration = 86400,
                                InventoryConfiguration = new InventoryConfiguration()
                                {
                                    ItemType = Core.Items.ItemType.SimpleItem,
                                    OrderMode = Core.Management.OrderMode.Fill,
                                    InitialInventory = 0.7,
                                    WarmupOrderCount = 5000,
                                    // Specify item weight and bundle size (though they will be overriden by the simple item config)
                                    ItemWeightMin = 2,
                                    ItemWeightMax = 8,
                                    BundleSizeMin = 4,
                                    BundleSizeMax = 12,
                                    // Lines per order
                                    OrderPositionCountMean = 1,
                                    OrderPositionCountStdDev = 1,
                                    OrderPositionCountMin = 1,
                                    OrderPositionCountMax = 4,
                                    // Units per order line
                                    PositionCountMean = 1,
                                    PositionCountStdDev = 0.3,
                                    PositionCountMin = 1,
                                    PositionCountMax = 3,
                                    // Further demand / fill mode settings
                                    DemandInventoryConfiguration = new DemandInventoryConfiguration()
                                    {
                                        OrderCount = 200,
                                        BundleCount = 200,
                                        InventoryLevelDrivenBundleGeneration = true,
                                        InventoryLevelBundleStopThreshold = 0.85,
                                        InventoryLevelBundleRestartThreshold = 0.65,
                                        InventoryLevelDrivenOrderGeneration = true,
                                        InventoryLevelOrderStopThreshold = 0.1,
                                        InventoryLevelOrderRestartThreshold = 0.6,
                                    },
                                    // Simple item scenario
                                    SimpleItemConfiguration = new SimpleItemConfiguration()
                                    {
                                        GeneratorConfigFile = skuScenario
                                    },
                                },
                                // Override values
                                OverrideConfig = new OverrideConfiguration()
                                {
                                    OverrideInputStationCount = true,
                                    OverrideInputStationCountValue = iStationAmount,
                                    OverrideOutputStationCapacity = true,
                                    OverrideOutputStationCapacityValue = oStationCapacity,
                                    OverrideBotCountPerOStation = true,
                                    OverrideBotCountPerOStationValue = botsPerOStationCount,
                                }
                            });
                        }
                    }
                }
            }

            // --> Save configurations
            Console.WriteLine("Writing all settings ...");
            foreach (var config in settings)
            {
                // Save
                string fileName = config.Name + ".xsett";
                Console.WriteLine("Saving " + fileName + " ...");
                InstanceIO.WriteSetting(fileName, config);
            }
        }
    }
}
