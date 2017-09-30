using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Configurations
{
    #region Method management

    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class NoChangeMethodManagementConfiguration : MethodManagementConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override MethodManagementType GetMethodType() { return MethodManagementType.NoChange; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "mmNC"; }
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class RandomMethodManagementConfiguration : MethodManagementConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override MethodManagementType GetMethodType() { return MethodManagementType.Random; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "mmR"; }
        /// <summary>
        /// The time after which the methods are exchanged.
        /// </summary>
        public double ChangeTimeout = 7200;
        /// <summary>
        /// Indicates whether to exchange the pod storage manager.
        /// </summary>
        public bool ExchangePodStorage = true;
        /// <summary>
        /// Checks whether the method management configuration is valid.
        /// </summary>
        /// <param name="errorMessage">A message describing the error if the configuration is not valid.</param>
        /// <returns>Indicates whether the method management configuration is valid.</returns>
        public override bool AttributesAreValid(out String errorMessage)
        {
            if (ChangeTimeout < 0)
            {
                errorMessage = "Problem with the method management configuration: ChangeTimeout has to be >= 0";
                return false;
            }
            errorMessage = "";
            return true;
        }
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class ScheduledMethodManagementConfiguration : MethodManagementConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override MethodManagementType GetMethodType() { return MethodManagementType.Scheduled; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "mmS"; }
        /// <summary>
        /// Parameter-less contructor mainly used by the xml-serializer.
        /// </summary>
        public ScheduledMethodManagementConfiguration() { }
        /// <summary>
        /// Contructor that generates default values for all fields.
        /// </summary>
        /// <param name="param">Not used.</param>
        public ScheduledMethodManagementConfiguration(DefaultConstructorIdentificationClass param)
        {
            ScheduledPodStorageManagers = new List<Skvp<double, PodStorageMethodType>>()
            {
                new Skvp<double, PodStorageMethodType>() { Key = 0.5, Value = PodStorageMethodType.Nearest },
            };
        }
        /// <summary>
        /// Indicates whether timepoints will be used relative to the overall simulation time (<code>true</code>) or as absolute values (<code>false</code>).
        /// In the case relative values the timepoints need to be from the range [0,1] and in case of absolute values the timepoints are in simulation time.
        /// </summary>
        public bool RelativeMode = true;
        /// <summary>
        /// All pod storage managers to switch to scheduled by a timepoint.
        /// </summary>
        public List<Skvp<double, PodStorageMethodType>> ScheduledPodStorageManagers;
    }

    #endregion
}
