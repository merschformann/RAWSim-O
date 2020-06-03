using RAWSimO.Core.Control.Defaults.ReplenishmentBatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Configurations
{
    #region Replenishment batching configurations

    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class RandomReplenishmentBatchingConfiguration : ReplenishmentBatchingConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override ReplenishmentBatchingMethodType GetMethodType() { return ReplenishmentBatchingMethodType.Random; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "rbR" + ((Recycle == true) ? "t" : "f"); }
        /// <summary>
        /// Indicates whether stations are recycled, i.e. one station is filled with bundles as long as there is capacity left.
        /// </summary>
        public bool Recycle = true;

    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class SamePodReplenishmentBatchingConfiguration : ReplenishmentBatchingConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override ReplenishmentBatchingMethodType GetMethodType() { return ReplenishmentBatchingMethodType.SamePod; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            string name = "rbSP";
            switch (FirstStationRule)
            {
                case SamePodFirstStationRule.Emptiest: name += "e"; break;
                case SamePodFirstStationRule.Fullest: name += "f"; break;
                case SamePodFirstStationRule.LeastBusy: name += "l"; break;
                case SamePodFirstStationRule.MostBusy: name += "m"; break;
                case SamePodFirstStationRule.Random: name += "r"; break;
                case SamePodFirstStationRule.DistanceEuclid: name += "d"; break;
                default: throw new ArgumentException("Unexpected argument!");
            }
            name += BreakBatches ? "t" : "f";
            return name;
        }
        /// <summary>
        /// Indicates how the first station is selected for a set of incoming bundles. If all bundles fit the station, it is the only station used for the bundles.
        /// </summary>
        public SamePodFirstStationRule FirstStationRule = SamePodFirstStationRule.DistanceEuclid;
        /// <summary>
        /// Tells the mechanism whether batches of bundles for a single pod can be divided across multiple stations at all.
        /// </summary>
        public bool BreakBatches = false;
        /// <summary>
        /// Tells the mechanism to process batches in the order they arrive instead of aiming to allocate them as quickly as possible.
        /// </summary>
        public bool FCFS = true;
        /// <summary>
        /// Tells the mechanism to only accept pods for an input station that are located on the same tier.
        /// </summary>
        public bool OnlySameTier = true;
    }

    #endregion
}
