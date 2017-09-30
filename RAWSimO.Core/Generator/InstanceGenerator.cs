using RAWSimO.Core.Configurations;
using RAWSimO.Core.Bots;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.Items;
using RAWSimO.Core.Management;
using RAWSimO.Core.Randomization;
using RAWSimO.Core.Control;
using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAWSimO.Core.IO;

namespace RAWSimO.Core.Generator
{

    /// <summary>
    /// Supplies methods to generate new instances according to given parameters.
    /// </summary>
    public class InstanceGenerator
    {
        /// <summary>
        /// Generates an instance with the given layout and configuration attached.
        /// </summary>
        /// <param name="layoutConfiguration">The layout configuration defining all the instance characteristics.</param>
        /// <param name="rand">A randomizer that is used during generation.</param>
        /// <param name="settingConfig">The configuration for the setting to emulate that will be attached for executing the simulation afterwards.</param>
        /// <param name="controlConfig">The configuration for the controlling mechanisms that will be attached for executing the simulation afterwards.</param>
        /// <param name="logAction">An optional action for logging.</param>
        /// <returns>The generated instance.</returns>
        public static Instance GenerateLayout(
            LayoutConfiguration layoutConfiguration,
            IRandomizer rand,
            SettingConfiguration settingConfig,
            ControlConfiguration controlConfig,
            Action<string> logAction = null)
        {
            LayoutGenerator layoutGenerator = new LayoutGenerator(layoutConfiguration, rand, settingConfig, controlConfig, logAction);
            Instance instance = layoutGenerator.GenerateLayout();
            InitializeInstance(instance);
            return instance;
        }

        /// <summary>
        /// Initializes a given instance.
        /// </summary>
        /// <param name="instance">The instance to initialize.</param>
        public static void InitializeInstance(Instance instance)
        {
            // Add managers
            instance.Randomizer = new RandomizerSimple(0);
            instance.Controller = new Controller(instance);
            instance.ResourceManager = new ResourceManager(instance);
            instance.ItemManager = new ItemManager(instance);
            // Notify instance about completed initializiation (time to initialize all stuff that relies on all managers being in place)
            instance.LateInitialize();
        }

        #region Expanded MaTi set

        /// <summary>
        /// Generates the pico default layout.
        /// </summary>
        /// <param name="rand">A randomizer that is used during generation.</param>
        /// <param name="settingConfig">The configuration for the setting to emulate that will be attached for executing the simulation afterwards.</param>
        /// <param name="controlConfig">The configuration for the controlling mechanisms that will be attached for executing the simulation afterwards.</param>
        /// <returns>The generated instance.</returns>
        public static Instance GenerateMaTiLayoutPico(IRandomizer rand, SettingConfiguration settingConfig, ControlConfiguration controlConfig)
        {
            LayoutConfiguration layoutConfiguration = GenerateMaTiLayoutConfiguration();
            layoutConfiguration.BotCount = 4;
            layoutConfiguration.NameLayout = "MaTiPico";
            layoutConfiguration.NrHorizontalAisles = 2;
            layoutConfiguration.NrVerticalAisles = 2;
            layoutConfiguration.NPickStationWest = 0;
            layoutConfiguration.NPickStationEast = 1;
            layoutConfiguration.NPickStationSouth = 0;
            layoutConfiguration.NPickStationNorth = 0;
            layoutConfiguration.NReplenishmentStationWest = 1;
            layoutConfiguration.NReplenishmentStationEast = 0;
            layoutConfiguration.NReplenishmentStationSouth = 0;
            layoutConfiguration.NReplenishmentStationNorth = 0;
            return GenerateLayout(layoutConfiguration, rand, settingConfig, controlConfig);
        }
        /// <summary>
        /// Generates the nano default layout.
        /// </summary>
        /// <param name="rand">A randomizer that is used during generation.</param>
        /// <param name="settingConfig">The configuration for the setting to emulate that will be attached for executing the simulation afterwards.</param>
        /// <param name="controlConfig">The configuration for the controlling mechanisms that will be attached for executing the simulation afterwards.</param>
        /// <returns>The generated instance.</returns>
        public static Instance GenerateMaTiLayoutNano(IRandomizer rand, SettingConfiguration settingConfig, ControlConfiguration controlConfig)
        {
            LayoutConfiguration layoutConfiguration = GenerateMaTiLayoutConfiguration();
            layoutConfiguration.BotCount = 8;
            layoutConfiguration.NameLayout = "MaTiNano";
            layoutConfiguration.NrHorizontalAisles = 4;
            layoutConfiguration.NrVerticalAisles = 4;
            layoutConfiguration.NPickStationWest = 0;
            layoutConfiguration.NPickStationEast = 2;
            layoutConfiguration.NPickStationSouth = 0;
            layoutConfiguration.NPickStationNorth = 0;
            layoutConfiguration.NReplenishmentStationWest = 2;
            layoutConfiguration.NReplenishmentStationEast = 0;
            layoutConfiguration.NReplenishmentStationSouth = 0;
            layoutConfiguration.NReplenishmentStationNorth = 0;
            return GenerateLayout(layoutConfiguration, rand, settingConfig, controlConfig);
        }
        /// <summary>
        /// Generates the micro default layout.
        /// </summary>
        /// <param name="rand">A randomizer that is used during generation.</param>
        /// <param name="settingConfig">The configuration for the setting to emulate that will be attached for executing the simulation afterwards.</param>
        /// <param name="controlConfig">The configuration for the controlling mechanisms that will be attached for executing the simulation afterwards.</param>
        /// <returns>The generated instance.</returns>
        public static Instance GenerateMaTiLayoutMicro(IRandomizer rand, SettingConfiguration settingConfig, ControlConfiguration controlConfig)
        {
            LayoutConfiguration layoutConfiguration = GenerateMaTiLayoutConfiguration();
            layoutConfiguration.BotCount = 12;
            layoutConfiguration.NameLayout = "MaTiMicro";
            layoutConfiguration.NrHorizontalAisles = 6;
            layoutConfiguration.NrVerticalAisles = 6;
            layoutConfiguration.NPickStationWest = 0;
            layoutConfiguration.NPickStationEast = 3;
            layoutConfiguration.NPickStationSouth = 0;
            layoutConfiguration.NPickStationNorth = 0;
            layoutConfiguration.NReplenishmentStationWest = 3;
            layoutConfiguration.NReplenishmentStationEast = 0;
            layoutConfiguration.NReplenishmentStationSouth = 0;
            layoutConfiguration.NReplenishmentStationNorth = 0;
            return GenerateLayout(layoutConfiguration, rand, settingConfig, controlConfig);
        }
        /// <summary>
        /// Generates the milli default layout.
        /// </summary>
        /// <param name="rand">A randomizer that is used during generation.</param>
        /// <param name="settingConfig">The configuration for the setting to emulate that will be attached for executing the simulation afterwards.</param>
        /// <param name="controlConfig">The configuration for the controlling mechanisms that will be attached for executing the simulation afterwards.</param>
        /// <returns>The generated instance.</returns>
        public static Instance GenerateMaTiLayoutMilli(IRandomizer rand, SettingConfiguration settingConfig, ControlConfiguration controlConfig)
        {
            LayoutConfiguration layoutConfiguration = GenerateMaTiLayoutConfiguration();
            layoutConfiguration.BotCount = 16;
            layoutConfiguration.NameLayout = "MaTiMilli";
            layoutConfiguration.NrHorizontalAisles = 8;
            layoutConfiguration.NrVerticalAisles = 8;
            layoutConfiguration.NPickStationWest = 0;
            layoutConfiguration.NPickStationEast = 4;
            layoutConfiguration.NPickStationSouth = 0;
            layoutConfiguration.NPickStationNorth = 0;
            layoutConfiguration.NReplenishmentStationWest = 4;
            layoutConfiguration.NReplenishmentStationEast = 0;
            layoutConfiguration.NReplenishmentStationSouth = 0;
            layoutConfiguration.NReplenishmentStationNorth = 0;
            return GenerateLayout(layoutConfiguration, rand, settingConfig, controlConfig);
        }
        /// <summary>
        /// Generates the centi default layout.
        /// </summary>
        /// <param name="rand">A randomizer that is used during generation.</param>
        /// <param name="settingConfig">The configuration for the setting to emulate that will be attached for executing the simulation afterwards.</param>
        /// <param name="controlConfig">The configuration for the controlling mechanisms that will be attached for executing the simulation afterwards.</param>
        /// <returns>The generated instance.</returns>
        public static Instance GenerateMaTiLayoutCenti(IRandomizer rand, SettingConfiguration settingConfig, ControlConfiguration controlConfig)
        {
            LayoutConfiguration layoutConfiguration = GenerateMaTiLayoutConfiguration();
            layoutConfiguration.BotCount = 20;
            layoutConfiguration.NameLayout = "MaTiCenti";
            layoutConfiguration.NrHorizontalAisles = 10;
            layoutConfiguration.NrVerticalAisles = 10;
            layoutConfiguration.NPickStationWest = 0;
            layoutConfiguration.NPickStationEast = 5;
            layoutConfiguration.NPickStationSouth = 0;
            layoutConfiguration.NPickStationNorth = 0;
            layoutConfiguration.NReplenishmentStationWest = 5;
            layoutConfiguration.NReplenishmentStationEast = 0;
            layoutConfiguration.NReplenishmentStationSouth = 0;
            layoutConfiguration.NReplenishmentStationNorth = 0;
            return GenerateLayout(layoutConfiguration, rand, settingConfig, controlConfig);
        }
        /// <summary>
        /// Generates the deca default layout.
        /// </summary>
        /// <param name="rand">A randomizer that is used during generation.</param>
        /// <param name="settingConfig">The configuration for the setting to emulate that will be attached for executing the simulation afterwards.</param>
        /// <param name="controlConfig">The configuration for the controlling mechanisms that will be attached for executing the simulation afterwards.</param>
        /// <returns>The generated instance.</returns>
        public static Instance GenerateMaTiLayoutDeca(IRandomizer rand, SettingConfiguration settingConfig, ControlConfiguration controlConfig)
        {
            LayoutConfiguration layoutConfiguration = GenerateMaTiLayoutConfiguration();
            layoutConfiguration.BotCount = 24;
            layoutConfiguration.NameLayout = "MaTiDeca";
            layoutConfiguration.NrHorizontalAisles = 12;
            layoutConfiguration.NrVerticalAisles = 12;
            layoutConfiguration.NPickStationWest = 0;
            layoutConfiguration.NPickStationEast = 6;
            layoutConfiguration.NPickStationSouth = 0;
            layoutConfiguration.NPickStationNorth = 0;
            layoutConfiguration.NReplenishmentStationWest = 6;
            layoutConfiguration.NReplenishmentStationEast = 0;
            layoutConfiguration.NReplenishmentStationSouth = 0;
            layoutConfiguration.NReplenishmentStationNorth = 0;
            return GenerateLayout(layoutConfiguration, rand, settingConfig, controlConfig);
        }
        /// <summary>
        /// Generates the hecto default layout.
        /// </summary>
        /// <param name="rand">A randomizer that is used during generation.</param>
        /// <param name="settingConfig">The configuration for the setting to emulate that will be attached for executing the simulation afterwards.</param>
        /// <param name="controlConfig">The configuration for the controlling mechanisms that will be attached for executing the simulation afterwards.</param>
        /// <returns>The generated instance.</returns>
        public static Instance GenerateMaTiLayoutHecto(IRandomizer rand, SettingConfiguration settingConfig, ControlConfiguration controlConfig)
        {
            LayoutConfiguration layoutConfiguration = GenerateMaTiLayoutConfiguration();
            layoutConfiguration.BotCount = 28;
            layoutConfiguration.NameLayout = "MaTiHecto";
            layoutConfiguration.NrHorizontalAisles = 14;
            layoutConfiguration.NrVerticalAisles = 14;
            layoutConfiguration.NPickStationWest = 0;
            layoutConfiguration.NPickStationEast = 7;
            layoutConfiguration.NPickStationSouth = 0;
            layoutConfiguration.NPickStationNorth = 0;
            layoutConfiguration.NReplenishmentStationWest = 7;
            layoutConfiguration.NReplenishmentStationEast = 0;
            layoutConfiguration.NReplenishmentStationSouth = 0;
            layoutConfiguration.NReplenishmentStationNorth = 0;
            return GenerateLayout(layoutConfiguration, rand, settingConfig, controlConfig);
        }
        /// <summary>
        /// Generates the kilo default layout.
        /// </summary>
        /// <param name="rand">A randomizer that is used during generation.</param>
        /// <param name="settingConfig">The configuration for the setting to emulate that will be attached for executing the simulation afterwards.</param>
        /// <param name="controlConfig">The configuration for the controlling mechanisms that will be attached for executing the simulation afterwards.</param>
        /// <returns>The generated instance.</returns>
        public static Instance GenerateMaTiLayoutKilo(IRandomizer rand, SettingConfiguration settingConfig, ControlConfiguration controlConfig)
        {
            LayoutConfiguration layoutConfiguration = GenerateMaTiLayoutConfiguration();
            layoutConfiguration.BotCount = 32;
            layoutConfiguration.NameLayout = "MaTiKilo";
            layoutConfiguration.NrHorizontalAisles = 16;
            layoutConfiguration.NrVerticalAisles = 16;
            layoutConfiguration.NPickStationWest = 0;
            layoutConfiguration.NPickStationEast = 8;
            layoutConfiguration.NPickStationSouth = 0;
            layoutConfiguration.NPickStationNorth = 0;
            layoutConfiguration.NReplenishmentStationWest = 8;
            layoutConfiguration.NReplenishmentStationEast = 0;
            layoutConfiguration.NReplenishmentStationSouth = 0;
            layoutConfiguration.NReplenishmentStationNorth = 0;
            return GenerateLayout(layoutConfiguration, rand, settingConfig, controlConfig);
        }
        /// <summary>
        /// Generates the mega default layout.
        /// </summary>
        /// <param name="rand">A randomizer that is used during generation.</param>
        /// <param name="settingConfig">The configuration for the setting to emulate that will be attached for executing the simulation afterwards.</param>
        /// <param name="controlConfig">The configuration for the controlling mechanisms that will be attached for executing the simulation afterwards.</param>
        /// <returns>The generated instance.</returns>
        public static Instance GenerateMaTiLayoutMega(IRandomizer rand, SettingConfiguration settingConfig, ControlConfiguration controlConfig)
        {
            LayoutConfiguration layoutConfiguration = GenerateMaTiLayoutConfiguration();
            layoutConfiguration.BotCount = 36;
            layoutConfiguration.NameLayout = "MaTiMega";
            layoutConfiguration.NrHorizontalAisles = 18;
            layoutConfiguration.NrVerticalAisles = 18;
            layoutConfiguration.NPickStationWest = 0;
            layoutConfiguration.NPickStationEast = 9;
            layoutConfiguration.NPickStationSouth = 0;
            layoutConfiguration.NPickStationNorth = 0;
            layoutConfiguration.NReplenishmentStationWest = 9;
            layoutConfiguration.NReplenishmentStationEast = 0;
            layoutConfiguration.NReplenishmentStationSouth = 0;
            layoutConfiguration.NReplenishmentStationNorth = 0;
            return GenerateLayout(layoutConfiguration, rand, settingConfig, controlConfig);
        }
        /// <summary>
        /// Generates the giga default layout.
        /// </summary>
        /// <param name="rand">A randomizer that is used during generation.</param>
        /// <param name="settingConfig">The configuration for the setting to emulate that will be attached for executing the simulation afterwards.</param>
        /// <param name="controlConfig">The configuration for the controlling mechanisms that will be attached for executing the simulation afterwards.</param>
        /// <returns>The generated instance.</returns>
        public static Instance GenerateMaTiLayoutGiga(IRandomizer rand, SettingConfiguration settingConfig, ControlConfiguration controlConfig)
        {
            LayoutConfiguration layoutConfiguration = GenerateMaTiLayoutConfiguration();
            layoutConfiguration.BotCount = 40;
            layoutConfiguration.NameLayout = "MaTiGiga";
            layoutConfiguration.NrHorizontalAisles = 20;
            layoutConfiguration.NrVerticalAisles = 20;
            layoutConfiguration.NPickStationWest = 0;
            layoutConfiguration.NPickStationEast = 10;
            layoutConfiguration.NPickStationSouth = 0;
            layoutConfiguration.NPickStationNorth = 0;
            layoutConfiguration.NReplenishmentStationWest = 10;
            layoutConfiguration.NReplenishmentStationEast = 0;
            layoutConfiguration.NReplenishmentStationSouth = 0;
            layoutConfiguration.NReplenishmentStationNorth = 0;
            return GenerateLayout(layoutConfiguration, rand, settingConfig, controlConfig);
        }

        #endregion

        #region Original MaTi set

        /// <summary>
        /// Generates the tiny default layout.
        /// </summary>
        /// <param name="rand">A randomizer that is used during generation.</param>
        /// <param name="settingConfig">The configuration for the setting to emulate that will be attached for executing the simulation afterwards.</param>
        /// <param name="controlConfig">The configuration for the controlling mechanisms that will be attached for executing the simulation afterwards.</param>
        /// <returns>The generated instance.</returns>
        public static Instance GenerateMaTiLayoutTiny(IRandomizer rand, SettingConfiguration settingConfig, ControlConfiguration controlConfig)
        {
            LayoutConfiguration layoutConfiguration = GenerateMaTiLayoutConfiguration();
            layoutConfiguration.BotCount = 32;
            layoutConfiguration.NameLayout = "MaTiTiny";
            layoutConfiguration.NrHorizontalAisles = 8;
            layoutConfiguration.NrVerticalAisles = 6;
            layoutConfiguration.NPickStationWest = 0;
            layoutConfiguration.NPickStationEast = 4;
            layoutConfiguration.NPickStationSouth = 0;
            layoutConfiguration.NPickStationNorth = 0;
            layoutConfiguration.NReplenishmentStationWest = 4;
            layoutConfiguration.NReplenishmentStationEast = 0;
            layoutConfiguration.NReplenishmentStationSouth = 0;
            layoutConfiguration.NReplenishmentStationNorth = 0;
            return GenerateLayout(layoutConfiguration, rand, settingConfig, controlConfig);
        }
        /// <summary>
        /// Generates the tiny default layout.
        /// </summary>
        /// <param name="rand">A randomizer that is used during generation.</param>
        /// <param name="settingConfig">The configuration for the setting to emulate that will be attached for executing the simulation afterwards.</param>
        /// <param name="controlConfig">The configuration for the controlling mechanisms that will be attached for executing the simulation afterwards.</param>
        /// <returns>The generated instance.</returns>
        public static Instance GenerateMaTiLayoutSmall(IRandomizer rand, SettingConfiguration settingConfig, ControlConfiguration controlConfig)
        {
            LayoutConfiguration layoutConfiguration = GenerateMaTiLayoutConfiguration();
            layoutConfiguration.BotCount = 40;
            layoutConfiguration.NameLayout = "MaTiSmall";
            layoutConfiguration.NrHorizontalAisles = 10;
            layoutConfiguration.NrVerticalAisles = 10;
            layoutConfiguration.NPickStationWest = 0;
            layoutConfiguration.NPickStationEast = 5;
            layoutConfiguration.NPickStationSouth = 0;
            layoutConfiguration.NPickStationNorth = 0;
            layoutConfiguration.NReplenishmentStationWest = 5;
            layoutConfiguration.NReplenishmentStationEast = 0;
            layoutConfiguration.NReplenishmentStationSouth = 0;
            layoutConfiguration.NReplenishmentStationNorth = 0;
            return GenerateLayout(layoutConfiguration, rand, settingConfig, controlConfig);
        }
        /// <summary>
        /// Generates the tiny default layout.
        /// </summary>
        /// <param name="rand">A randomizer that is used during generation.</param>
        /// <param name="settingConfig">The configuration for the setting to emulate that will be attached for executing the simulation afterwards.</param>
        /// <param name="controlConfig">The configuration for the controlling mechanisms that will be attached for executing the simulation afterwards.</param>
        /// <returns>The generated instance.</returns>
        public static Instance GenerateMaTiLayoutMedium(IRandomizer rand, SettingConfiguration settingConfig, ControlConfiguration controlConfig)
        {
            LayoutConfiguration layoutConfiguration = GenerateMaTiLayoutConfiguration();
            layoutConfiguration.BotCount = 48;
            layoutConfiguration.NameLayout = "MaTiMedium";
            layoutConfiguration.NrHorizontalAisles = 12;
            layoutConfiguration.NrVerticalAisles = 14;
            layoutConfiguration.NPickStationWest = 0;
            layoutConfiguration.NPickStationEast = 6;
            layoutConfiguration.NPickStationSouth = 0;
            layoutConfiguration.NPickStationNorth = 0;
            layoutConfiguration.NReplenishmentStationWest = 6;
            layoutConfiguration.NReplenishmentStationEast = 0;
            layoutConfiguration.NReplenishmentStationSouth = 0;
            layoutConfiguration.NReplenishmentStationNorth = 0;
            return GenerateLayout(layoutConfiguration, rand, settingConfig, controlConfig);
        }
        /// <summary>
        /// Generates the tiny default layout.
        /// </summary>
        /// <param name="rand">A randomizer that is used during generation.</param>
        /// <param name="settingConfig">The configuration for the setting to emulate that will be attached for executing the simulation afterwards.</param>
        /// <param name="controlConfig">The configuration for the controlling mechanisms that will be attached for executing the simulation afterwards.</param>
        /// <returns>The generated instance.</returns>
        public static Instance GenerateMaTiLayoutLarge(IRandomizer rand, SettingConfiguration settingConfig, ControlConfiguration controlConfig)
        {
            LayoutConfiguration layoutConfiguration = GenerateMaTiLayoutConfiguration();
            layoutConfiguration.BotCount = 56;
            layoutConfiguration.NameLayout = "MaTiLarge";
            layoutConfiguration.NrHorizontalAisles = 14;
            layoutConfiguration.NrVerticalAisles = 18;
            layoutConfiguration.NPickStationWest = 0;
            layoutConfiguration.NPickStationEast = 7;
            layoutConfiguration.NPickStationSouth = 0;
            layoutConfiguration.NPickStationNorth = 0;
            layoutConfiguration.NReplenishmentStationWest = 7;
            layoutConfiguration.NReplenishmentStationEast = 0;
            layoutConfiguration.NReplenishmentStationSouth = 0;
            layoutConfiguration.NReplenishmentStationNorth = 0;
            return GenerateLayout(layoutConfiguration, rand, settingConfig, controlConfig);
        }
        /// <summary>
        /// Generates the tiny default layout.
        /// </summary>
        /// <param name="rand">A randomizer that is used during generation.</param>
        /// <param name="settingConfig">The configuration for the setting to emulate that will be attached for executing the simulation afterwards.</param>
        /// <param name="controlConfig">The configuration for the controlling mechanisms that will be attached for executing the simulation afterwards.</param>
        /// <returns>The generated instance.</returns>
        public static Instance GenerateMaTiLayoutHuge(IRandomizer rand, SettingConfiguration settingConfig, ControlConfiguration controlConfig)
        {
            LayoutConfiguration layoutConfiguration = GenerateMaTiLayoutConfiguration();
            layoutConfiguration.BotCount = 64;
            layoutConfiguration.NameLayout = "MaTiHuge";
            layoutConfiguration.NrHorizontalAisles = 16;
            layoutConfiguration.NrVerticalAisles = 22;
            layoutConfiguration.NPickStationWest = 0;
            layoutConfiguration.NPickStationEast = 8;
            layoutConfiguration.NPickStationSouth = 0;
            layoutConfiguration.NPickStationNorth = 0;
            layoutConfiguration.NReplenishmentStationWest = 8;
            layoutConfiguration.NReplenishmentStationEast = 0;
            layoutConfiguration.NReplenishmentStationSouth = 0;
            layoutConfiguration.NReplenishmentStationNorth = 0;
            return GenerateLayout(layoutConfiguration, rand, settingConfig, controlConfig);
        }
        /// <summary>
        /// Generates the default layout configuration.
        /// </summary>
        /// <returns>The default layout configuration.</returns>
        public static LayoutConfiguration GenerateMaTiLayoutConfiguration()
        {
            LayoutConfiguration layoutConfiguration = new LayoutConfiguration();

            layoutConfiguration.TierCount = 1;
            layoutConfiguration.TierHeight = 4;
            layoutConfiguration.BotRadius = 0.35;
            layoutConfiguration.MaxAcceleration = 0.5;
            layoutConfiguration.MaxDeceleration = 0.5;
            layoutConfiguration.MaxVelocity = 1.5;
            layoutConfiguration.TurnSpeed = 2.5;
            layoutConfiguration.CollisionPenaltyTime = 0.5;
            layoutConfiguration.PodTransferTime = 3;
            layoutConfiguration.PodAmount = 0.85;
            layoutConfiguration.PodRadius = 0.4;
            layoutConfiguration.PodCapacity = 500;
            layoutConfiguration.StationRadius = 0.4;
            layoutConfiguration.ItemTransferTime = 10;
            layoutConfiguration.ItemPickTime = 3;
            layoutConfiguration.ItemBundleTransferTime = 10;
            layoutConfiguration.IStationCapacity = 1000;
            layoutConfiguration.OStationCapacity = 18;
            layoutConfiguration.ElevatorTransportationTimePerTier = 10;
            layoutConfiguration.AislesTwoDirectional = false;
            layoutConfiguration.SingleLane = true;
            layoutConfiguration.HorizontalLengthBlock = 4;
            layoutConfiguration.WidthHall = 6;
            layoutConfiguration.WidthBuffer = 4;
            layoutConfiguration.DistanceEntryExitStation = 3;
            layoutConfiguration.CounterClockwiseRingwayDirection = true;

            return layoutConfiguration;
        }

        #endregion
    }
}
