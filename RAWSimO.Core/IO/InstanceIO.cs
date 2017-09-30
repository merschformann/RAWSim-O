using RAWSimO.Core.Configurations;
using RAWSimO.Core.Generator;
using RAWSimO.Core.Items;
using RAWSimO.Core.Randomization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace RAWSimO.Core.IO
{
    /// <summary>
    /// Exposes methods to serialize and deserialize instances and more.
    /// </summary>
    public class InstanceIO
    {
        private static readonly XmlSerializer _instanceSerializer = new XmlSerializer(typeof(DTOInstance));
        private static readonly XmlSerializer _layoutConfigSerializer = new XmlSerializer(typeof(LayoutConfiguration));
        private static readonly XmlSerializer _settingConfigSerializer = new XmlSerializer(typeof(SettingConfiguration));
        private static readonly XmlSerializer _controlConfigSerializer = new XmlSerializer(typeof(ControlConfiguration));
        private static readonly XmlSerializer _listSerializer = new XmlSerializer(typeof(DTOOrderList));
        private static readonly XmlSerializer _simpleItemGeneratorConfigSerializer = new XmlSerializer(typeof(SimpleItemGeneratorConfiguration));

        #region Read

        /// <summary>
        /// Reads an instance from a file.
        /// </summary>
        /// <param name="instancePath">The path to either the instance or a layout configuration.</param>
        /// <param name="settingConfigPath">The path to the file specifying the setting.</param>
        /// <param name="controlConfigPath">The path to the file supplying the configuration for all controlling mechanisms.</param>
        /// <param name="overrideVisualizationAttached">Indicates whether a visualization shall be attached.</param>
        /// <param name="visualizationOnly">If this is enabled most of the initialization will be skipped.</param>
        /// <param name="logAction">A action that will be used for logging some lines.</param>
        /// <returns></returns>
        public static Instance ReadInstance(
            string instancePath,
            string settingConfigPath,
            string controlConfigPath,
            bool overrideVisualizationAttached = false,
            bool visualizationOnly = false,
            Action<string> logAction = null)
        {
            // Test for layout / instance file
            XmlDocument doc = new XmlDocument();
            doc.Load(instancePath);
            string rootName = doc.SelectSingleNode("/*").Name;
            bool layoutConfigurationGiven = false;
            if (rootName == nameof(Instance)) layoutConfigurationGiven = false;
            else if (rootName == nameof(LayoutConfiguration)) layoutConfigurationGiven = true;
            else throw new ArgumentException("No valid instance or layout file given!");
            logAction?.Invoke(rootName + " recognized!");

            // --> Read configurations
            SettingConfiguration settingConfig = null;
            ControlConfiguration controlConfig = null;
            LayoutConfiguration layoutConfig = null;
            if (!visualizationOnly)
            {
                // Read the setting configuration
                logAction?.Invoke("Parsing setting config ...");
                using (StreamReader sr = new StreamReader(settingConfigPath))
                {
                    // Deserialize the xml-file
                    settingConfig = (SettingConfiguration)_settingConfigSerializer.Deserialize(sr);
                    // If it contains a path to a word-file that is not leading to a wordlist file try the default wordlist locations
                    if (settingConfig.InventoryConfiguration.ColoredWordConfiguration != null &&
                        !File.Exists(settingConfig.InventoryConfiguration.ColoredWordConfiguration.WordFile))
                        settingConfig.InventoryConfiguration.ColoredWordConfiguration.WordFile =
                            IOHelper.FindResourceFile(settingConfig.InventoryConfiguration.ColoredWordConfiguration.WordFile, instancePath);
                    // If it contains a path to an order-file that is not leading to a orderlist file try the default orderlist locations
                    if (settingConfig.InventoryConfiguration.FixedInventoryConfiguration != null &&
                        !string.IsNullOrWhiteSpace(settingConfig.InventoryConfiguration.FixedInventoryConfiguration.OrderFile) &&
                        !File.Exists(settingConfig.InventoryConfiguration.FixedInventoryConfiguration.OrderFile))
                        settingConfig.InventoryConfiguration.FixedInventoryConfiguration.OrderFile =
                            IOHelper.FindResourceFile(settingConfig.InventoryConfiguration.FixedInventoryConfiguration.OrderFile, instancePath);
                    // If it contains a path to an simple-item-file that is not leading to a generator config file try the default locations
                    if (settingConfig.InventoryConfiguration.SimpleItemConfiguration != null &&
                        !string.IsNullOrWhiteSpace(settingConfig.InventoryConfiguration.SimpleItemConfiguration.GeneratorConfigFile) &&
                        !File.Exists(settingConfig.InventoryConfiguration.SimpleItemConfiguration.GeneratorConfigFile))
                        settingConfig.InventoryConfiguration.SimpleItemConfiguration.GeneratorConfigFile =
                            IOHelper.FindResourceFile(settingConfig.InventoryConfiguration.SimpleItemConfiguration.GeneratorConfigFile, instancePath);
                }
                // Read the control configuration
                logAction?.Invoke("Parsing control config ...");
                using (StreamReader sr = new StreamReader(controlConfigPath))
                    // Deserialize the xml-file
                    controlConfig = (ControlConfiguration)_controlConfigSerializer.Deserialize(sr);
            }
            // --> Init or generate instance
            Instance instance = null;
            if (layoutConfigurationGiven)
            {
                // Read the layout configuration
                logAction?.Invoke("Parsing layout config ...");
                using (StreamReader sr = new StreamReader(instancePath))
                    // Deserialize the xml-file
                    layoutConfig = (LayoutConfiguration)_layoutConfigSerializer.Deserialize(sr);
                // Apply override config, if available
                if (settingConfig != null && settingConfig.OverrideConfig != null)
                    layoutConfig.ApplyOverrideConfig(settingConfig.OverrideConfig);
                // Generate instance
                logAction?.Invoke("Generating instance...");
                instance = InstanceGenerator.GenerateLayout(layoutConfig, new RandomizerSimple(0), settingConfig, controlConfig, logAction);
            }
            else
            {
                // Init the instance object
                instance = new Instance();
            }

            // Check whether the config is required
            if (!visualizationOnly)
            {
                // Submit config first to the instance object
                instance.SettingConfig = settingConfig;
                instance.ControllerConfig = controlConfig;
            }
            else
            {
                // Add default config (none required though)
                instance.SettingConfig = new SettingConfiguration();
                instance.SettingConfig.VisualizationOnly = true;
                instance.ControllerConfig = new ControlConfiguration();
            }
            // If a visualization is already present set it to true
            instance.SettingConfig.VisualizationAttached = overrideVisualizationAttached;

            // --> Parse the instance from a file, if no layout was given but a specific instance
            if (!layoutConfigurationGiven)
            {
                // Read the instance
                logAction?.Invoke("Parsing instance ...");
                using (StreamReader sr = new StreamReader(instancePath))
                {
                    // Deserialize the xml-file
                    DTOInstance dtoInstance = (DTOInstance)_instanceSerializer.Deserialize(sr);
                    // Submit the data to an instance object
                    dtoInstance.Submit(instance);
                }
            }

            // Return it
            return instance;
        }

        /// <summary>
        /// Reads a DTO representation of the instance given by the file.
        /// </summary>
        /// <param name="instancePath">The file to read.</param>
        /// <returns>The instance.</returns>
        public static DTOInstance ReadDTOInstance(string instancePath)
        {
            // Init reference
            DTOInstance dtoInstance;
            // Read the instance
            using (StreamReader sr = new StreamReader(instancePath))
            {
                // Deserialize the xml-file
                dtoInstance = (DTOInstance)_instanceSerializer.Deserialize(sr);
            }
            // Return it
            return dtoInstance;
        }
        /// <summary>
        /// Reads an order list from a file.
        /// </summary>
        /// <param name="orderFile">The file.</param>
        /// <param name="instance">The instance to submit to.</param>
        /// <returns>The order list.</returns>
        public static OrderList ReadOrders(string orderFile, Instance instance)
        {
            // Read the list
            OrderList list = null;
            using (StreamReader sr = new StreamReader(orderFile))
            {
                // Deserialize the xml-file
                DTOOrderList dtoConfig = (DTOOrderList)_listSerializer.Deserialize(sr);
                // Submit list to the instance object
                list = dtoConfig.Submit(instance);
            }
            return list;
        }
        /// <summary>
        /// Reads the configuration for a simple item generator instance.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>The configuration.</returns>
        public static SimpleItemGeneratorConfiguration ReadSimpleItemGeneratorConfig(string file)
        {
            // Read the config
            SimpleItemGeneratorConfiguration config = null;
            string searchedPath = IOHelper.FindResourceFile(file, Directory.GetCurrentDirectory());
            using (StreamReader sr = new StreamReader(searchedPath))
                // Deserialize the xml-file
                config = (SimpleItemGeneratorConfiguration)_simpleItemGeneratorConfigSerializer.Deserialize(sr);
            return config;
        }

        #endregion

        #region Write

        /// <summary>
        /// Writes the instance to a file.
        /// </summary>
        /// <param name="path">The file.</param>
        /// <param name="instance">The instance.</param>
        public static void WriteInstance(string path, Instance instance)
        {
            // Implicitly convert the instance to a DTO and serialize it
            DTOInstance dtoInstance = instance;
            using (TextWriter writer = new StreamWriter(path))
                _instanceSerializer.Serialize(writer, dtoInstance);
        }
        /// <summary>
        /// Writes a DTO instance representation to a file.
        /// </summary>
        /// <param name="path">The file.</param>
        /// <param name="instance">The instance.</param>
        public static void WriteDTOInstance(string path, DTOInstance instance)
        {
            // Serialize it
            using (TextWriter writer = new StreamWriter(path))
                _instanceSerializer.Serialize(writer, instance);
        }
        /// <summary>
        /// Writes the layout configuration to a file.
        /// </summary>
        /// <param name="path">The file.</param>
        /// <param name="config">The layout configuration.</param>
        public static void WriteLayout(string path, LayoutConfiguration config)
        {
            // Serialize it
            using (TextWriter writer = new StreamWriter(path))
                _layoutConfigSerializer.Serialize(writer, config);
        }
        /// <summary>
        /// Writes the setting specification to a file.
        /// </summary>
        /// <param name="path">The file.</param>
        /// <param name="config">The setting specification.</param>
        public static void WriteSetting(string path, SettingConfiguration config)
        {
            // Serialize it
            using (TextWriter writer = new StreamWriter(path))
                _settingConfigSerializer.Serialize(writer, config);
        }
        /// <summary>
        /// Writes the configuration to a file.
        /// </summary>
        /// <param name="path">The file.</param>
        /// <param name="config">The configuration.</param>
        public static void WriteConfiguration(string path, ControlConfiguration config)
        {
            // Serialize it
            using (TextWriter writer = new StreamWriter(path))
                _controlConfigSerializer.Serialize(writer, config);
        }
        /// <summary>
        /// Writes the order list to a file.
        /// </summary>
        /// <param name="path">The file.</param>
        /// <param name="list">The order list.</param>
        public static void WriteOrders(string path, OrderList list)
        {
            // Implicitly convert the instance to a DTO and serialize it
            DTOOrderList dtoList = list;
            using (TextWriter writer = new StreamWriter(path))
                _listSerializer.Serialize(writer, dtoList);
        }
        /// <summary>
        /// Writes a simple item generator configuration to a file.
        /// </summary>
        /// <param name="path">The file.</param>
        /// <param name="config">The configuration.</param>
        public static void WriteSimpleItemGeneratorConfigFile(string path, SimpleItemGeneratorConfiguration config)
        {
            // Serialize the object to xml and write it to the given path
            using (StreamWriter sw = new StreamWriter(path))
                _simpleItemGeneratorConfigSerializer.Serialize(sw, config);
        }

        #endregion
    }
}
