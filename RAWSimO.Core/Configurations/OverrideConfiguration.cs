using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Configurations
{
    /// <summary>
    /// Exposes values that override other given values.
    /// </summary>
    public class OverrideConfiguration
    {
        /// <summary>
        /// The fractional number of input stations to add, i.e. a value of 0.5 means that only 50 % of the stations are added while the remaining ones specified by the instance file are ignored.
        /// </summary>
        public double OverrideInputStationCountValue = double.NaN;
        /// <summary>
        /// Indicate whether to override the number of active input stations.
        /// </summary>
        public bool OverrideInputStationCount = false;
        /// <summary>
        /// The fractional number of output stations to add, i.e. a value of 0.5 means that only 50 % of the stations are added while the remaining ones specified by the instance file are ignored.
        /// </summary>
        public double OverrideOutputStationCountValue = double.NaN;
        /// <summary>
        /// Indicate whether to override the number of active output stations.
        /// </summary>
        public bool OverrideOutputStationCount = false;
        /// <summary>
        /// The number of bots to use per output station.
        /// </summary>
        public int OverrideBotCountPerOStationValue = -1;
        /// <summary>
        /// Indicates whether to override the number of bots with a number of bots per output station.
        /// </summary>
        public bool OverrideBotCountPerOStation = false;
        /// <summary>
        /// The time for setting down or picking up a pod.
        /// </summary>
        public double OverrideBotPodTransferTimeValue = double.NaN;
        /// <summary>
        /// Indicates whether to override the values given by the instance for the parameter.
        /// </summary>
        public bool OverrideBotPodTransferTime = false;
        /// <summary>
        /// The acceleration of the robots.
        /// </summary>
        public double OverrideBotMaxAccelerationValue = double.NaN;
        /// <summary>
        /// Indicates whether to override the values given by the instance for the parameter.
        /// </summary>
        public bool OverrideBotMaxAcceleration = false;
        /// <summary>
        /// The deceleration of the robots.
        /// </summary>
        public double OverrideBotMaxDecelerationValue = double.NaN;
        /// <summary>
        /// Indicates whether to override the values given by the instance for the parameter.
        /// </summary>
        public bool OverrideBotMaxDeceleration = false;
        /// <summary>
        /// The maximal velocity of the robots.
        /// </summary>
        public double OverrideBotMaxVelocityValue = double.NaN;
        /// <summary>
        /// Indicates whether to override the values given by the instance for the parameter.
        /// </summary>
        public bool OverrideBotMaxVelocity = false;
        /// <summary>
        /// The turning speed of the robots.
        /// </summary>
        public double OverrideBotTurnSpeedValue = double.NaN;
        /// <summary>
        /// Indicates whether to override the values given by the instance for the parameter.
        /// </summary>
        public bool OverrideBotTurnSpeed = false;
        /// <summary>
        /// The capacity of the pods.
        /// </summary>
        public double OverridePodCapacityValue = double.NaN;
        /// <summary>
        /// Indicates whether to override the values given by the instance for the parameter.
        /// </summary>
        public bool OverridePodCapacity = false;
        /// <summary>
        /// The capacity of the input stations.
        /// </summary>
        public double OverrideInputStationCapacityValue = double.NaN;
        /// <summary>
        /// Indicates whether to override the values given by the instance for the parameter.
        /// </summary>
        public bool OverrideInputStationCapacity = false;
        /// <summary>
        /// The capacity of the output stations.
        /// </summary>
        public int OverrideOutputStationCapacityValue = -1;
        /// <summary>
        /// Indicates whether to override the values given by the instance for the parameter.
        /// </summary>
        public bool OverrideOutputStationCapacity = false;
        /// <summary>
        /// The time it takes to put a bundle on a pod.
        /// </summary>
        public double OverrideInputStationItemBundleTransferTimeValue = double.NaN;
        /// <summary>
        /// Indicates whether to override the values given by the instance for the parameter.
        /// </summary>
        public bool OverrideInputStationItemBundleTransferTime = false;
        /// <summary>
        /// The time it takes to handle an item at a pick station.
        /// </summary>
        public double OverrideOutputStationItemTransferTimeValue = double.NaN;
        /// <summary>
        /// Indicates whether to override the values given by the instance for the parameter.
        /// </summary>
        public bool OverrideOutputStationItemTransferTime = false;
        /// <summary>
        /// The time it takes for picking an item.
        /// </summary>
        public double OverrideOutputStationItemPickTimeValue = double.NaN;
        /// <summary>
        /// Indicates whether to override the values given by the instance for the parameter.
        /// </summary>
        public bool OverrideOutputStationItemPickTime = false;
    }
}
