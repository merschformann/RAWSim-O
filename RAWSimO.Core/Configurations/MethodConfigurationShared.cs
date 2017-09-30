using RAWSimO.Core.Control.Shared;
using RAWSimO.Core.IO;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Configurations
{
    /// <summary>
    /// Contains parameters for cache zones.
    /// </summary>
    public class CacheConfiguration
    {
        /// <summary>
        /// The relative number of cache storage locations of all storage locations.
        /// </summary>
        public double CacheFraction = 0.25;
        /// <summary>
        /// The absolute number of drop-off locations per station.
        /// </summary>
        public int DropoffCount = 0;
        /// <summary>
        /// Indicates the preference between the different zones.
        /// </summary>
        public ZonePriority ZonePriority = ZonePriority.DropoffFirst;
        /// <summary>
        /// Checks whether this configuration matches the other given one.
        /// </summary>
        /// <param name="other">The other configuration.</param>
        /// <returns><code>true</code> if both configurations match, <code>false</code> otherwise.</returns>
        public bool Match(CacheConfiguration other) { return this.SimplePublicInstanceFieldsEqual(other); }
        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        /// <returns>A string representing this object.</returns>
        public override string ToString() { return "(" + string.Join("/", GetType().GetFields().Select(f => f.GetValue(this))) + ")"; }
    }
    /// <summary>
    /// Contains parameters for the pod utility ordering strategy.
    /// </summary>
    public class PodUtilityConfiguration
    {
        /// <summary>
        /// An optional timeout for updating the scores of pods and storage locations.
        /// </summary>
        public double ScoreUpdateTimeout = 300;
        /// <summary>
        /// The fractional amount of storage locations to use in addition to the ones necessary given by the number of pods.
        /// </summary>
        public double BufferStorageLocations = 0.05;
        /// <summary>
        /// The number of ranks to be indifferent when determining the perfect position for a pod.
        /// </summary>
        public int RankCorridor = 3;
        /// <summary>
        /// The weight of the speed (item frequencies contained in pod) of a pod.
        /// </summary>
        public double WeightSpeed = 1;
        /// <summary>
        /// The weight of the utility (useful items in a pod) of a pod.
        /// </summary>
        public double WeightUtility = 1;
        /// <summary>
        /// Indicates whether the complete backlog demand will be considered, or only the demand of orders assigned or queued at the stations.
        /// </summary>
        public bool ConsiderBacklogDemand = true;
        /// <summary>
        /// Indicates whether determining the speed (aggregated frequency) of a pod does consider the number of units contained or only whether the SKU is contained at all.
        /// </summary>
        public bool WeighSpeedByContainedCount = true;
        /// <summary>
        /// Indicates whether to use the static or measured item frequency.
        /// </summary>
        public bool UseStaticItemFrequency = true;
        /// <summary>
        /// Checks whether this configuration matches the other given one.
        /// </summary>
        /// <param name="other">The other configuration.</param>
        /// <returns><code>true</code> if both configurations match, <code>false</code> otherwise.</returns>
        public bool Match(PodUtilityConfiguration other) { return this.SimplePublicInstanceFieldsEqual(other); }
        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        /// <returns>A string representing this object.</returns>
        public override string ToString() { return "(" + string.Join("/", GetType().GetFields().Select(f => f.GetValue(this))) + ")"; }
    }
}
