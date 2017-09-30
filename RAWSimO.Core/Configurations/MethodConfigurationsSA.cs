using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Configurations
{
    #region Station activation configurations

    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class ActivateAllStationActivationConfiguration : StationActivationConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override StationActivationMethodType GetMethodType() { return StationActivationMethodType.ActivateAll; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "saAA"; }
    }

    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class BacklogThresholdStationActivationConfiguration : StationActivationConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override StationActivationMethodType GetMethodType() { return StationActivationMethodType.BacklogThreshold; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "saBT"; }
        /// <summary>
        /// The timeout between two decisions of activating / deactivating a station.
        /// </summary>
        public double Timeout = 600;
        /// <summary>
        /// The number of orders in backlog above which another output-station gets activated.
        /// </summary>
        public int OStationActivateThreshold = 50;
        /// <summary>
        /// The overall utilization (relative used capacity) of output-stations below which a station gets deactivated.
        /// </summary>
        public double OStationDeactivateThreshold = 0.7;
        /// <summary>
        /// The number of bundles in backlog above which another input-station gets activated.
        /// </summary>
        public int IStationActivateThreshold = 50;
        /// <summary>
        /// The overall utilization (relative used capacity) of input-stations below which a station gets deactivated.
        /// </summary>
        public double IStationDeactivateThreshold = 0.7;
    }

    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class ConstantRatioStationActivationConfiguration : StationActivationConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override StationActivationMethodType GetMethodType() { return StationActivationMethodType.ConstantRatio; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "saCR"; }
        /// <summary>
        /// The ratio of stations overall being activated, i.e. 0.75 activates 75 % of all stations. The remaining ones will be split by the replenish / pick ratio.
        /// </summary>
        public double ActiveRatio = 0.5;
        /// <summary>
        /// The ratio of pick vs. replenishment stations being activated, i.e. 0.75 will activate three times more pick than replenishment stations. Pick station count will be 'floored', if it does not divide nicely.
        /// </summary>
        public double PickReplenishRatio = 0.5;
    }

    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class WorkShiftStationActivationConfiguration : StationActivationConfiguration
    {
        /// <summary>
        /// Parameter-less constructor mainly used by the xml-serializer.
        /// </summary>
        public WorkShiftStationActivationConfiguration() { }
        /// <summary>
        /// Constructor that generates default values for all fields.
        /// </summary>
        /// <param name="param">Not used.</param>
        public WorkShiftStationActivationConfiguration(DefaultConstructorIdentificationClass param) : this()
        {
            Shifts = new List<Skvp<double, bool>>() {
                new Skvp<double, bool>() { Key = TimeSpan.FromHours(0).TotalSeconds, Value = false },
                new Skvp<double, bool>() { Key = TimeSpan.FromHours(6).TotalSeconds, Value = true },
                new Skvp<double, bool>() { Key = TimeSpan.FromHours(22).TotalSeconds, Value = false },
            };
        }
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override StationActivationMethodType GetMethodType() { return StationActivationMethodType.WorkShift; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name))
                return Name;
            string name = "saWS";
            name += Shifts != null ? Shifts.Count.ToString() : "0";
            return name;
        }
        /// <summary>
        /// Defines all shifts of the period. The boolean field indicates whether the shift activates or deactivates the stations.
        /// </summary>
        public List<Skvp<double, bool>> Shifts;
        /// <summary>
        /// The time after which the shifts are read again from the beginning, i.e. the length of a "period". 
        /// Usually this is one day, but can also be used to plan an entire week by defining appropriate shifts and using a loop time of one week in seconds.
        /// </summary>
        public double LoopTime = TimeSpan.FromDays(1).TotalSeconds;
    }

    #endregion
}
