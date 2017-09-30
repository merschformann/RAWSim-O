using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Configurations
{
    /// <summary>
    /// A simple attribute identifying live members of the configuration. These are not for serialization or similar, but only for the control of special functionality.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class LiveAttribute : Attribute { }
    /// <summary>
    /// Defines a class that represents the only param to a default constructor, if supplied. If no such constructor is supplied than the parameter-less constructor serves as the default constructor.
    /// </summary>
    public class DefaultConstructorIdentificationClass { }
    /// <summary>
    /// Used to serialize dictionary entries.
    /// </summary>
    /// <typeparam name="K">The type of the key of the entry.</typeparam>
    /// <typeparam name="V">The type of the value of the entry.</typeparam>
    public struct Skvp<K, V>
    {
        /// <summary>
        /// The key of the entry.
        /// </summary>
        public K Key;
        /// <summary>
        /// The value of the entry.
        /// </summary>
        public V Value;
    }
    /// <summary>
    /// Used to serialize two-keyed dictionary entries.
    /// </summary>
    /// <typeparam name="K1">The type of the first key.</typeparam>
    /// <typeparam name="K2">The type of the second key.</typeparam>
    /// <typeparam name="V">The type of the value.</typeparam>
    public struct Skkvt<K1, K2, V>
    {
        /// <summary>
        /// The first key of the entry.
        /// </summary>
        public K1 Key1;
        /// <summary>
        /// The second key of the entry.
        /// </summary>
        public K2 Key2;
        /// <summary>
        /// The value of the entry.
        /// </summary>
        public V Value;
    }
    /// <summary>
    /// Defines the log level to use.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Absolutely nothing is logged.
        /// </summary>
        Silent = 0,
        /// <summary>
        /// Only really severe error make the cut.
        /// </summary>
        Severe = 1,
        /// <summary>
        /// Only standard simulation output makes the cut + potential severe errors / warnings.
        /// </summary>
        Default = 2,
        /// <summary>
        /// In addition to the default output further info-messages are logged that might be of interest.
        /// </summary>
        Info = 3,
        /// <summary>
        /// Everything that can be logged will be logged.
        /// </summary>
        Verbose = 4,
    }
    /// <summary>
    /// Indicates the level of logging for the output files.
    /// </summary>
    public enum LogFileLevel
    {
        /// <summary>
        /// All log files will be written.
        /// </summary>
        All,
        /// <summary>
        /// Only the footprint will be written.
        /// </summary>
        FootprintOnly,
    }
    /// <summary>
    /// States the current debug mode that sets the different logs messages that are output.
    /// </summary>
    public enum DebugMode
    {
        /// <summary>
        /// Indicates that the real-time will be added to the log message.
        /// </summary>
        RealTime,

        /// <summary>
        /// Indicates that the current memory consumption is added to the output.
        /// </summary>
        RealTimeAndMemory
    }
}
