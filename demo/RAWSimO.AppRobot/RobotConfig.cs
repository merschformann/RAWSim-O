using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RAWSimO.AppRobot
{
    /// <summary>
    /// A config containing basic settings for one robot.
    /// </summary>
    public class RobotConfig
    {
        /// <summary>
        /// The ID of the robot.
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// The name of the robot.
        /// </summary>
        public string Name { get; set; }
    }
    /// <summary>
    /// Contains basic I/O methods to read and write robot-configurations.
    /// </summary>
    public class RobotConfigIO
    {
        /// <summary>
        /// The constant name of the config file.
        /// </summary>
        public const string CONFIG_FILE = "bot.xml";
        /// <summary>
        /// The serializer used for I/O.
        /// </summary>
        private static XmlSerializer _serializer = new XmlSerializer(typeof(RobotConfig));
        /// <summary>
        /// Reads the config from the given file.
        /// </summary>
        /// <param name="path">The path to the config-file.</param>
        /// <returns>The read config.</returns>
        public static RobotConfig ReadConfig(string path)
        {
            RobotConfig config;
            using (StreamReader sr = new StreamReader(path))
                config = (RobotConfig)_serializer.Deserialize(sr);
            return config;
        }
        /// <summary>
        /// Writes the given config to the specified file.
        /// </summary>
        /// <param name="config">The config to write.</param>
        /// <param name="path">The path to write the config to.</param>
        public static void WriteConfig(RobotConfig config, string path)
        {
            using (StreamWriter sw = new StreamWriter(path))
                _serializer.Serialize(sw, config);
        }
    }
}
