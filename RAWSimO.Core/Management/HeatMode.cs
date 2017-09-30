using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Management
{
    /// <summary>
    /// Indicates which information shall be used for rendering heat informtation for the pods.
    /// </summary>
    public enum HeatMode
    {
        /// <summary>
        /// No information is given.
        /// </summary>
        None,
        /// <summary>
        /// Gives feedback about the current utilization of the pods.
        /// </summary>
        CurrentCapacityUtilization,
        /// <summary>
        /// Gives feedback about the number of items handled per pod.
        /// </summary>
        NumItemsHandled,
        /// <summary>
        /// Gives feedback about the number of bundles handled per pod.
        /// </summary>
        NumBundlesHandled,
        /// <summary>
        /// Gives feedback about the average frequency of items contained in the pods.
        /// </summary>
        AverageFrequency,
        /// <summary>
        /// Gives feedback about the maximal frequency of the items contained in the pods.
        /// </summary>
        MaxFrequency,
        /// <summary>
        /// Gives feedback about the average frequency of the items internally used for generating them.
        /// </summary>
        AverageStaticFrequency,
        /// <summary>
        /// Gives feedback about the maximal frequency of the items internally used for generating them.
        /// </summary>
        MaxStaticFrequency,
        /// <summary>
        /// Gives feedback about the type of the pod. This is set by a manager to give visual feedback.
        /// </summary>
        StorageType,
        /// <summary>
        /// Gives feedback about whether the storage location the pod is stored at belongs to a cache.
        /// </summary>
        CacheType,
        /// <summary>
        /// Gives feedback about the prominence value set by a pod utility manager component.
        /// </summary>
        ProminenceValue,
        /// <summary>
        /// Gives feedback about the speed value set by a pod utility manager component.
        /// </summary>
        PodSpeed,
        /// <summary>
        /// Gives feedback about the utility value set by a pod utility manager component.
        /// </summary>
        PodUtility,
        /// <summary>
        /// Gives feedback about the combined speed and utility value set by a pod utility manager component.
        /// </summary>
        PodCombinedValue,
    }
}
