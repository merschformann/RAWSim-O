using RAWSimO.Core.Control.Shared;
using RAWSimO.Core.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Configurations
{
    #region Repositioning configurations

    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class DummyRepositioningConfiguration : RepositioningConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override RepositioningMethodType GetMethodType() { return RepositioningMethodType.Dummy; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "rpD"; }
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class CacheDropoffRepositioningConfiguration : RepositioningConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override RepositioningMethodType GetMethodType() { return RepositioningMethodType.CacheDropoff; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            string name = "rpHC";
            name += ZoningConfiguration.DropoffCount.ToString(IOConstants.FORMATTER);
            name += ZoningConfiguration.CacheFraction.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER);
            name += WeightSpeed.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER);
            name += WeightUtility.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER);
            return name;
        }

        /// <summary>
        /// The config to use for creating the different zones.
        /// </summary>
        public CacheConfiguration ZoningConfiguration = new CacheConfiguration();
        /// <summary>
        /// Prefers same tier moves first.
        /// </summary>
        public bool PreferSameTierMoves = false;
        /// <summary>
        /// The targeted fill level of the cache.
        /// </summary>
        public double TargetFillCache = 0.8;
        /// <summary>
        /// The weight for the utility score of the pod.
        /// </summary>
        public double WeightUtility = 1;
        /// <summary>
        /// The weight for the speed score of the pod.
        /// </summary>
        public double WeightSpeed = 0;
        /// <summary>
        /// The fractional amount of pods that need to be less useful than the pod to remove from a drop-off location, so that the pod will be cached.
        /// </summary>
        public double PodCacheableThreshold = 0.5;
        /// <summary>
        /// The rule to use for selecting the new storage location for the pod. This is superimposed by the decision about whether a pod is stored in the cache or not.
        /// </summary>
        public CacheStorageLocationSelectionRule PodDisposeRule = CacheStorageLocationSelectionRule.Manhattan;
        /// <summary>
        /// A global timeout that is used to block calls for a while, if no suitable move could be determined (in order to safe computational ressources).
        /// </summary>
        public double GlobalTimeout = 120;
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class CacheRepositioningConfiguration : RepositioningConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override RepositioningMethodType GetMethodType() { return RepositioningMethodType.Cache; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            string name = "rpMS";
            name += ZoningConfiguration.DropoffCount.ToString(IOConstants.FORMATTER);
            name += ZoningConfiguration.CacheFraction.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER);
            name += CacheClearing.ToString(IOConstants.FORMATTER);
            name += PodEmptyThreshold.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER);
            return name;
        }

        /// <summary>
        /// The config to use for creating the different zones.
        /// </summary>
        public CacheConfiguration ZoningConfiguration = new CacheConfiguration();
        /// <summary>
        /// Number of storage locations within cache to keep free per station.
        /// </summary>
        public int CacheClearing = 8;
        /// <summary>
        /// A threshold beneath which a pod is considered empty as a fraction of its capacity.
        /// </summary>
        public double PodEmptyThreshold = 0.4;
        /// <summary>
        /// Indicates whether the complete backlog is considered for uselessness assessment.
        /// </summary>
        public bool UselessConsiderBacklog = true;
        /// <summary>
        /// A global timeout that is used to block calls for a while, if no suitable move could be determined (in order to safe computational ressources).
        /// </summary>
        public double GlobalTimeout = 120;
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class UtilityRepositioningConfiguration : RepositioningConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override RepositioningMethodType GetMethodType() { return RepositioningMethodType.Utility; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            string name = "rpU";
            name += UtilityConfig.WeightSpeed.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER);
            name += UtilityConfig.WeightUtility.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER);
            name += UtilityConfig.RankCorridor.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER);
            return name;
        }

        /// <summary>
        /// The config for the pod utility manager component.
        /// </summary>
        public PodUtilityConfiguration UtilityConfig = new PodUtilityConfiguration();
        /// <summary>
        /// A global timeout that is used to block calls for a while, if no suitable move could be determined (in order to safe computational ressources).
        /// </summary>
        public double GlobalTimeout = 120;
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class ConceptRepositioningConfiguration : RepositioningConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override RepositioningMethodType GetMethodType() { return RepositioningMethodType.Concept; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            string name = "rpC";
            // Modify name by using parameters to obtain a more or less unique identifier of this configuration
            return name;
        }
        /// <summary>
        /// Just a sample variable. Remove me.
        /// </summary>
        public double SampleParameterDouble = 0.1;
        /// <summary>
        /// Just a sample variable. Remove me.
        /// </summary>
        public bool SampleParameterBool = true;
    }

    #endregion
}
