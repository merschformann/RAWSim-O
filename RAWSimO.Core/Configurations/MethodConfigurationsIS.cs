using RAWSimO.Core.Control.Defaults.ItemStorage;
using RAWSimO.Core.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RAWSimO.Core.Configurations
{
    #region Item storage configurations

    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class DummyItemStorageConfiguration : ItemStorageConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override ItemStorageMethodType GetMethodType() { return ItemStorageMethodType.Dummy; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "isD"; }
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class RandomItemStorageConfiguration : ItemStorageConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override ItemStorageMethodType GetMethodType() { return ItemStorageMethodType.Random; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "isR" + (StickToPodUntilFull ? "t" : "f") + BufferThreshold.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER); }
        /// <summary>
        /// Indicates whether the randomly chosen pod will be used again for other bundles until it is full or setdown.
        /// </summary>
        public bool StickToPodUntilFull = true;
        /// <summary>
        /// Specifies the threshold of the reserved capacity above which the pods are refilled.
        /// </summary>
        public double BufferThreshold = 0.8;
        /// <summary>
        /// The time after which buffered bundles will be allocated even if they do not meet the threshold criterion.
        /// </summary>
        public double BufferTimeout = 1200;
        /// <summary>
        /// Checks whether the item storage configuration is valid.
        /// </summary>
        /// <param name="errorMessage">A message describing the error if the configuration is not valid.</param>
        /// <returns>Indicates whether the item storage configuration is valid.</returns>
        public override bool AttributesAreValid(out String errorMessage)
        {
            if (BufferThreshold < 0)
            {
                errorMessage = "Problem with item storage configuration: BufferThreshold has to be >= 0.";
                return false;
            }
            if (BufferTimeout < 0)
            {
                errorMessage = "Problem with item storage configuration: BufferTimeout has to be >= 0";
                return false;
            }
            errorMessage = "";
            return true;

        }
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class CorrelativeItemStorageConfiguration : ItemStorageConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override ItemStorageMethodType GetMethodType() { return ItemStorageMethodType.Correlative; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "isCR" + BufferThreshold.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER); }
        /// <summary>
        /// Specifies the threshold of the reserved capacity above which the pods are refilled.
        /// </summary>
        public double BufferThreshold = 0.8;
        /// <summary>
        /// The time after which buffered bundles will be allocated even if they do not meet the threshold criterion.
        /// </summary>
        public double BufferTimeout = 1200;
        /// <summary>
        /// Checks whether the item storage configuration is valid.
        /// </summary>
        /// <param name="errorMessage">A message describing the error if the configuration is not valid.</param>
        /// <returns>Indicates whether the item storage configuration is valid.</returns>
        public override bool AttributesAreValid(out String errorMessage)
        {
            if (BufferThreshold < 0)
            {
                errorMessage = "Problem with item storage configuration: BufferThreshold has to be >= 0.";
                return false;
            }
            if (BufferTimeout < 0)
            {
                errorMessage = "Problem with item storage configuration: BufferTimeout has to be >= 0";
                return false;
            }
            errorMessage = "";
            return true;
        }
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class TurnoverItemStorageConfiguration : ItemStorageConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override ItemStorageMethodType GetMethodType() { return ItemStorageMethodType.Turnover; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "isT" + ClassBorders.Count(c => c == IOConstants.DELIMITER_LIST) + (EmptiestInsteadOfRandom ? "t" : "f"); }
        /// <summary>
        /// Defines the class borders that specify the relative amount of storage locations per class.
        /// The first border specifies the high frequency section, the next one the next high frequency section and so on.
        /// The last border will always be 1.0, because we want to use all our storage locations.
        /// As one example: "0.1,0.3,1.0" will lead to 3 classes that contain 10 % of the pods in the first class, 20 % in the second one and the rest in the last class.
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
        /// Indicates whether the emptiest pod is filled or a random one.
        /// </summary>
        public bool EmptiestInsteadOfRandom = true;
        /// <summary>
        /// Specifies the threshold of the reserved capacity above which the pods are refilled.
        /// </summary>
        public string BufferThresholdPerClass = "0.8" + IOConstants.DELIMITER_LIST + "0.8" + IOConstants.DELIMITER_LIST + "0.8";
        /// <summary>
        /// The time after which buffered bundles will be allocated even if they do not meet the threshold criterion.
        /// </summary>
        public string BufferTimeoutPerClass = "1200" + IOConstants.DELIMITER_LIST + "1200" + IOConstants.DELIMITER_LIST + "1200";
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class ClosestLocationItemStorageConfiguration : ItemStorageConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override ItemStorageMethodType GetMethodType() { return ItemStorageMethodType.ClosestLocation; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "isCL" + BufferThreshold.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER); }
        /// <summary>
        /// Specifies the threshold of the reserved capacity above which the pods are refilled.
        /// </summary>
        public double BufferThreshold = 0.8;
        /// <summary>
        /// The time after which buffered bundles will be allocated even if they do not meet the threshold criterion.
        /// </summary>
        public double BufferTimeout = 1200;
        /// <summary>
        /// Checks whether the item storage configuration is valid.
        /// </summary>
        /// <param name="errorMessage">A message describing the error if the configuration is not valid.</param>
        /// <returns>Indicates whether the item storage configuration is valid.</returns>
        public override bool AttributesAreValid(out String errorMessage)
        {
            if (BufferThreshold < 0)
            {
                errorMessage = "Problem with item storage configuration: BufferThreshold has to be >= 0.";
                return false;
            }
            if (BufferTimeout < 0)
            {
                errorMessage = "Problem with item storage configuration: BufferTimeout has to be >= 0";
                return false;
            }

            errorMessage = "";
            return true;

        }
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class ReactiveItemStorageConfiguration : ItemStorageConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override ItemStorageMethodType GetMethodType() { return ItemStorageMethodType.Reactive; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "isRE" + BufferThreshold.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER); }
        /// <summary>
        /// The rule that is used to select a pod for a bundle.
        /// </summary>
        public ReactiveItemStorageAllocationRule AllocationRule = ReactiveItemStorageAllocationRule.RandomSameTier;
        /// <summary>
        /// Specifies the threshold of the reserved capacity above which the pods are refilled.
        /// </summary>
        public double BufferThreshold = 0.8;
        /// <summary>
        /// The time after which buffered bundles will be allocated even if they do not meet the threshold criterion.
        /// </summary>
        public double BufferTimeout = 1200;
        /// <summary>
        /// Checks whether the item storage configuration is valid.
        /// </summary>
        /// <param name="errorMessage">A message describing the error if the configuration is not valid.</param>
        /// <returns>Indicates whether the item storage configuration is valid.</returns>
        public override bool AttributesAreValid(out String errorMessage)
        {
            if (BufferThreshold < 0)
            {
                errorMessage = "Probelm with item storage configuration: BufferThreshold has to be >= 0.";
                return false;
            }
            if (BufferTimeout < 0)
            {
                errorMessage = "Problem with item storage configuration: BufferTimeout has to be >= 0";
                return false;
            }
            errorMessage = "";
            return true;

        }
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class EmptiestItemStorageConfiguration : ItemStorageConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override ItemStorageMethodType GetMethodType() { return ItemStorageMethodType.Emptiest; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "isE" + (StickToPodUntilFull ? "t" : "f") + BufferThreshold.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER); }
        /// <summary>
        /// Indicates whether the chosen pod will be used again for other bundles until it is full or setdown.
        /// </summary>
        public bool StickToPodUntilFull = true;
        /// <summary>
        /// Specifies the threshold of the reserved capacity above which the pods are refilled.
        /// </summary>
        public double BufferThreshold = 0.8;
        /// <summary>
        /// The time after which buffered bundles will be allocated even if they do not meet the threshold criterion.
        /// </summary>
        public double BufferTimeout = 1200;
        /// <summary>
        /// Checks whether the item storage configuration is valid.
        /// </summary>
        /// <param name="errorMessage">A message describing the error if the configuration is not valid.</param>
        /// <returns>Indicates whether the item storage configuration is valid.</returns>
        public override bool AttributesAreValid(out String errorMessage)
        {
            if (BufferThreshold < 0)
            {
                errorMessage = "Problem with item storage configuration: BufferThreshold has to be >= 0.";
                return false;
            }
            if (BufferTimeout < 0)
            {
                errorMessage = "Problem with item storage configuration: BufferTimeout has to be >= 0";
                return false;
            }
            errorMessage = "";
            return true;
        }
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class LeastDemandItemStorageConfiguration : ItemStorageConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override ItemStorageMethodType GetMethodType() { return ItemStorageMethodType.LeastDemand; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "isLD" + (StickToPodUntilFull ? "t" : "f") + BufferThreshold.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER); }
        /// <summary>
        /// Indicates whether the chosen pod will be used again for other bundles until it is full or setdown.
        /// </summary>
        public bool StickToPodUntilFull = true;
        /// <summary>
        /// Specifies the threshold of the reserved capacity above which the pods are refilled.
        /// </summary>
        public double BufferThreshold = 0.8;
        /// <summary>
        /// The time after which buffered bundles will be allocated even if they do not meet the threshold criterion.
        /// </summary>
        public double BufferTimeout = 1200;
        /// <summary>
        /// Checks whether the item storage configuration is valid.
        /// </summary>
        /// <param name="errorMessage">A message describing the error if the configuration is not valid.</param>
        /// <returns>Indicates whether the item storage configuration is valid.</returns>
        public override bool AttributesAreValid(out String errorMessage)
        {
            if (BufferThreshold < 0)
            {
                errorMessage = "Problem with item storage configuration: BufferThreshold has to be >= 0.";
                return false;
            }
            if (BufferTimeout < 0)
            {
                errorMessage = "Problem with item storage configuration: BufferTimeout has to be >= 0";
                return false;
            }
            errorMessage = "";
            return true;
        }
    }

    #endregion
}
