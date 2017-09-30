using RAWSimO.Core.Control.Defaults.PodStorage;
using RAWSimO.Core.Control.Shared;
using RAWSimO.Core.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Configurations
{
    #region Pod storage configurations

    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class DummyPodStorageConfiguration : PodStorageConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override PodStorageMethodType GetMethodType() { return PodStorageMethodType.Dummy; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "psD"; }
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class RandomPodStorageConfiguration : PodStorageConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override PodStorageMethodType GetMethodType() { return PodStorageMethodType.Random; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "psR" + (PreferSameTier ? "t" : "f"); }
        /// <summary>
        /// Indicates whether the controller prefers storage locations of the same tier over others. Locations of the same tier are still chosen randomly.
        /// </summary>
        public bool PreferSameTier = true;
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class FixedPodStorageConfiguration : PodStorageConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override PodStorageMethodType GetMethodType() { return PodStorageMethodType.Fixed; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "psF"; }
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class NearestPodStorageConfiguration : PodStorageConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override PodStorageMethodType GetMethodType() { return PodStorageMethodType.Nearest; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            string name = "psN";
            switch (PodDisposeRule)
            {
                case NearestPodStorageLocationDisposeRule.Euclid: name += "e"; break;
                case NearestPodStorageLocationDisposeRule.Manhattan: name += "m"; break;
                case NearestPodStorageLocationDisposeRule.ShortestPath: name += "s"; break;
                case NearestPodStorageLocationDisposeRule.ShortestTime: name += "t"; break;
                default: throw new ArgumentException("Unexpected argument!");
            }
            return name;
        }
        /// <summary>
        /// Indicates which distance metric is used to select a free pod storage location.
        /// </summary>
        public NearestPodStorageLocationDisposeRule PodDisposeRule = NearestPodStorageLocationDisposeRule.ShortestTime;
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class StationBasedPodStorageConfiguration : PodStorageConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override PodStorageMethodType GetMethodType() { return PodStorageMethodType.StationBased; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            string name = "psSB" + (OutputStationMode ? "t" : "f");
            switch (PodDisposeRule)
            {
                case StationBasedPodStorageLocationDisposeRule.Euclid: name += "e"; break;
                case StationBasedPodStorageLocationDisposeRule.Manhattan: name += "m"; break;
                case StationBasedPodStorageLocationDisposeRule.ShortestPath: name += "s"; break;
                case StationBasedPodStorageLocationDisposeRule.ShortestTime: name += "t"; break;
                default: throw new ArgumentException("Unexpected argument!");
            }
            return name;
        }
        /// <summary>
        /// Indicates whether to store the pods near the output-stations or the input-stations.
        /// </summary>
        public bool OutputStationMode = true;
        /// <summary>
        /// Indicates which distance metric is used to select a free pod storage location.
        /// </summary>
        public StationBasedPodStorageLocationDisposeRule PodDisposeRule = StationBasedPodStorageLocationDisposeRule.ShortestTime;
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class CachePodStorageConfiguration : PodStorageConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override PodStorageMethodType GetMethodType() { return PodStorageMethodType.Cache; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            string name = "psHC";
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
        /// The weight for the utility score of the pod.
        /// </summary>
        public double WeightUtility = 1;
        /// <summary>
        /// The weight for the speed score of the pod.
        /// </summary>
        public double WeightSpeed = 0;
        /// <summary>
        /// The weight of the current cache fill level when deciding whether to store a pod in the cache (compared to the already stored pods).
        /// </summary>
        public double WeightCacheFill = 1;
        /// <summary>
        /// The weight of the utility when deciding whether to store a pod in the cache (compared to the already stored pods).
        /// </summary>
        public double WeightCacheUtility = 1;
        /// <summary>
        /// If the combined value of cache-fill score and utility of the pod (value is of range [0,1]) is higher than this threshold the pod will be brought to the cache.
        /// </summary>
        public double PodCacheableThreshold = 0.5;
        /// <summary>
        /// The rule to use for selecting the storage location for the pod. This is superimposed by the decision about whether a pod is stored in the cache or not.
        /// </summary>
        public CacheStorageLocationSelectionRule PodDisposeRule = CacheStorageLocationSelectionRule.ShortestTime;
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class UtilityPodStorageConfiguration : PodStorageConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override PodStorageMethodType GetMethodType() { return PodStorageMethodType.Utility; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            string name = "psU";
            name += UtilityConfig.WeightSpeed.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER);
            name += UtilityConfig.WeightUtility.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER);
            name += UtilityConfig.RankCorridor.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER);
            return name;
        }
        /// <summary>
        /// The fractional amount of storage locations considered to be the most popular locations (by their distance to the output-stations).
        /// </summary>
        public PodUtilityConfiguration UtilityConfig = new PodUtilityConfiguration();
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class TurnoverPodStorageConfiguration : PodStorageConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override PodStorageMethodType GetMethodType() { return PodStorageMethodType.Turnover; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            string name = "psT" + ClassBorders.Count(c => c == IOConstants.DELIMITER_LIST);
            switch (StorageLocationClassRule)
            {
                case TurnoverPodStorageLocationClassRule.OutputStationDistanceEuclidean: name += "e"; break;
                case TurnoverPodStorageLocationClassRule.OutputStationDistanceManhattan: name += "m"; break;
                case TurnoverPodStorageLocationClassRule.OutputStationDistanceShortestPath: name += "s"; break;
                case TurnoverPodStorageLocationClassRule.OutputStationDistanceShortestTime: name += "t"; break;
                default: throw new ArgumentException("Unexpected argument!");
            }
            switch (PodDisposeRule)
            {
                case TurnoverPodStorageLocationDisposeRule.NearestEuclid: name += "e"; break;
                case TurnoverPodStorageLocationDisposeRule.NearestManhattan: name += "m"; break;
                case TurnoverPodStorageLocationDisposeRule.NearestShortestPath: name += "s"; break;
                case TurnoverPodStorageLocationDisposeRule.NearestShortestTime: name += "d"; break;
                case TurnoverPodStorageLocationDisposeRule.OStationNearestEuclid: name += "n"; break;
                case TurnoverPodStorageLocationDisposeRule.OStationNearestManhattan: name += "t"; break;
                case TurnoverPodStorageLocationDisposeRule.OStationNearestShortestPath: name += "p"; break;
                case TurnoverPodStorageLocationDisposeRule.OStationNearestShortestTime: name += "a"; break;
                case TurnoverPodStorageLocationDisposeRule.Random: name += "r"; break;
                default: throw new ArgumentException("Unexpected argument!");
            }
            return name;
        }
        /// <summary>
        /// The fraction of the storage used for A items.
        /// </summary>
        public string ClassBorders = "0.1" + IOConstants.DELIMITER_LIST + "0.3" + IOConstants.DELIMITER_LIST + "1.0";
        /// <summary>
        /// The time between two subsequent runs of the re-allocation of item-descriptions and pods to the storage classes.
        /// </summary>
        public double ReallocationDelay = 0.0;
        /// <summary>
        /// The number of orders between two subsequent runs of the re-allocation of item-descriptions and pods to the storage classes.
        /// </summary>
        public int ReallocationOrderCount = 0;
        /// <summary>
        /// Indicates which rule to use to assign the storage locations to the different classes.
        /// </summary>
        public TurnoverPodStorageLocationClassRule StorageLocationClassRule = TurnoverPodStorageLocationClassRule.OutputStationDistanceShortestTime;
        /// <summary>
        /// Indicates how a free storage location is selected from all free storage locations of a class.
        /// </summary>
        public TurnoverPodStorageLocationDisposeRule PodDisposeRule = TurnoverPodStorageLocationDisposeRule.NearestShortestTime;
        /// <summary>
        /// Checks whether the pod storage configuration is valid.
        /// </summary>
        /// <param name="errorMessage">A message describing the error if the configuration is not valid.</param>
        /// <returns>Indicates whether the pod storage configuration is valid.</returns>
        public override bool AttributesAreValid(out String errorMessage)
        {
            if (ReallocationDelay < 0)
            {
                errorMessage = "Problem with pod storage configuration: ReallocationDelay has to be >= 0";
                return false;
            }
            if (ReallocationOrderCount < 0)
            {
                errorMessage = "Problem with pod storage configuration: ReallocationOrderCount has to be >= 0";
                return false;
            }
            errorMessage = "";
            return true;
        }
    }

    #endregion
}
