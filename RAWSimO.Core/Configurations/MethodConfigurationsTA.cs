using RAWSimO.Core.IO;
using RAWSimO.Core.Metrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Configurations
{
    #region Task allocation configurations

    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class BruteForceTaskAllocationConfiguration : TaskAllocationConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override TaskAllocationMethodType GetMethodType() { return TaskAllocationMethodType.BruteForce; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "taBF"; }
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class RandomTaskAllocationConfiguration : TaskAllocationConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override TaskAllocationMethodType GetMethodType() { return TaskAllocationMethodType.Random; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            return "taR" +
                (SwitchModeIfNoWork ? "t" : "f") +
                (DoRepositioningIfNoWork ? "t" : "f") +
                (PreferSameTier ? "t" : "f") +
                StickToModeProbability.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) +
                StickToPodProbability.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) +
                RepositioningProbability.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER);
        }
        /// <summary>
        /// Indicates whether to switch to the other mode automatically if there is no further work to do in the current mode.
        /// </summary>
        public bool SwitchModeIfNoWork = true;
        /// <summary>
        /// Indicates whether to do repositioning moves (if available), if no other tasks are available.
        /// </summary>
        public bool DoRepositioningIfNoWork = true;
        /// <summary>
        /// Indicates whether the bot will search for work to do on the same tier it is located on first.
        /// </summary>
        public bool PreferSameTier = true;
        /// <summary>
        /// The probability by which the bot sticks to its current mode (input or output).
        /// </summary>
        public double StickToModeProbability = 0.9;
        /// <summary>
        /// The probability by which the bot sticks to the pod it is currently carrying. The bot will still change the pod if there is no task available in its current mode.
        /// </summary>
        public double StickToPodProbability = 0.95;
        /// <summary>
        /// The probability by which the bot attempts a repositioning move instead of starting a new insert or extract task. This is only taken into account, if the robot is not currently carrying a pod.
        /// </summary>
        public double RepositioningProbability = 0.05;
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class BalancedTaskAllocationConfiguration : TaskAllocationConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override TaskAllocationMethodType GetMethodType() { return TaskAllocationMethodType.Balanced; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            string name = "taBA" +
                WeightInputStations.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) +
                WeightOutputStations.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) +
                WeightRepositioning.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) +
                (ExtendSearch ? "t" : "f") +
                (SearchAll ? "t" : "f") +
                (RepositionBeforeRest ? "t" : "f") +
                BotsPerStationLimit.ToString();
            name += PodSelectionConfig.GetMethodName();
            switch (RestLocationOrderType)
            {
                case PrefRestLocationForBot.Random: name += "r"; break;
                case PrefRestLocationForBot.RandomSameTier: name += "t"; break;
                case PrefRestLocationForBot.Middle: name += "c"; break;
                case PrefRestLocationForBot.MiddleSameTier: name += "m"; break;
                case PrefRestLocationForBot.Nearest: name += "n"; break;
                default: throw new ArgumentException("Unexpected argument!");
            }
            return name;
        }
        /// <summary>
        /// Checks whether the task allacation configuration is valid.
        /// </summary>
        /// <param name="errorMessage">A message describing the error if the configuration is not valid.</param>
        /// <returns>Indicates whether the task allocation configuration is valid.</returns>
        public override bool AttributesAreValid(out String errorMessage)
        {

            if (BotReallocationTimeout < 0)
            {
                errorMessage = "Problem with task allocation configuration: BotReallocationTimeout has to be >= 0.";
                return false;
            }
            if (WeightInputStations < 0)
            {
                errorMessage = "Problem with task allocation configuration: WeightInputStations has to be >= 0.";
                return false;
            }
            if (WeightOutputStations < 0)
            {
                errorMessage = "Problem with task allocation configuration: WeightOutputStations has to be >= 0.";
                return false;
            }
            if (BotsPerStationLimit <= 0)
            {
                errorMessage = "Problem with task allocation configuratin: BotsPerStationLimit has to be > 0.";
                return false;
            }
            if (ExtendedSearchRadius < 0)
            {
                errorMessage = "Problem with task allocation configuration: ExtendedSearchRadius has to be >= 0.";
                return false;
            }

            errorMessage = "";
            return true;

        }

        /// <summary>
        /// Timeout between two reallocation runs. Set this to 0 to disable reallocation at all.
        /// </summary>
        public double BotReallocationTimeout = 30.0;
        /// <summary>
        /// Weight to use for assigning bots to input-stations.
        /// </summary>
        public double WeightInputStations = 1.0;
        /// <summary>
        /// Weight to use for assigning bots to output-stations.
        /// </summary>
        public double WeightOutputStations = 2.0;
        /// <summary>
        /// Weight to use for assigning bots to do repositioning tasks.
        /// </summary>
        public double WeightRepositioning = 0;
        /// <summary>
        /// Limits the number of bots assigned to a single station.
        /// </summary>
        public int BotsPerStationLimit = 9;
        /// <summary>
        /// Indicates whether the bot will search for work to do on the same tier it is located on first.
        /// </summary>
        public bool PreferSameTier = true;
        /// <summary>
        /// Indicates whether the bot will search for work to do with its current pod for another station, if there is no more work for its current station-pod combination.
        /// </summary>
        public bool ExtendSearch = false;
        /// <summary>
        /// The radius in which a station has to be in order to be considered for the search expansion. The distance is measured from the station the bot is assigned to.
        /// </summary>
        public double ExtendedSearchRadius = 8;
        /// <summary>
        /// Indicates whether all stations will be searched for any work to do (in distance order from current station) over setting the bot to rest instead.
        /// </summary>
        public bool SearchAll = false;
        /// <summary>
        /// Aims to do repositioning moves, if available, before setting a bot to rest.
        /// </summary>
        public bool RepositionBeforeRest = true;
        /// <summary>
        /// The pod selection configuration to use.
        /// </summary>
        public DefaultPodSelectionConfiguration PodSelectionConfig = new DefaultPodSelectionConfiguration();
        /// <summary>
        /// The order to use when looking for a suitable resting location.
        /// </summary>
        public PrefRestLocationForBot RestLocationOrderType = PrefRestLocationForBot.MiddleSameTier;
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class SwarmTaskAllocationConfiguration : TaskAllocationConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override TaskAllocationMethodType GetMethodType() { return TaskAllocationMethodType.Swarm; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName() { if (!string.IsNullOrWhiteSpace(Name)) return Name; return "taSW"; /* TODO add further parameters to have more information about the controller behind this config */ }
        /// <summary>
        /// alpha for Threshold formula
        /// </summary>
        public double alpha = 1;
        /// <summary>
        /// beta for Threshold formula
        /// </summary>
        public double beta = 1;
        /// <summary>
        /// hardness of multiplikator, to increase prbability of unused stations
        /// </summary>
        public double multiplikator = 1.2;
        /// <summary>
        /// "Comment me"
        /// </summary>
        public int maxMultiplikatorValue = 30;
        /// <summary>
        /// hardness of probability of a rest task
        /// </summary>
        public double restPercentage = 0.8;
        /// <summary>
        /// Sets the Maximum of Bots per Station
        /// </summary>
        public int maximumBotsPerStation = 6;
        /// <summary>
        /// Indicates whether the bot will search for work to do with its current pod for another station, if there is no more work for its current station-pod combination.
        /// </summary>
        public bool ExtendSearch = false;
        /// <summary>
        /// The radius in which a station has to be in order to be considered for the search expansion. The distance is measured from the station the bot is assigned to.
        /// </summary>
        public double ExtendedSearchRadius = 5.0;
        /// <summary>
        /// The sub configuration for selecting the pods for picking and replenishment.
        /// </summary>
        public DefaultPodSelectionConfiguration PodSelectionConfig = new DefaultPodSelectionConfiguration();
        /// <summary>
        /// The order to use when looking for a suitable resting location.
        /// </summary>
        public PrefRestLocationForBot RestLocationOrderType = PrefRestLocationForBot.Nearest;
        /// <summary>
        /// Checks whether the task allacation configuration is valid.
        /// </summary>
        /// <param name="errorMessage">A message describing the error if the configuration is not valid.</param>
        /// <returns>Indicates whether the task allocation configuration is valid.</returns>
        public override bool AttributesAreValid(out String errorMessage)
        {
            if (maximumBotsPerStation <= 0)
            {
                errorMessage = "Problem with task allocation configuration: maximumBotsPerStation has to be > 0";
                return false;
            }
            if (restPercentage < 0)
            {
                errorMessage = "Problem with task allocation configuration: restPercentage has to be >= 0";
                return false;
            }
            if (ExtendedSearchRadius < 0)
            {
                errorMessage = "Problem with task allocation configuration: ExtendedSearchRadius has to be >= 0";
                return false;
            }

            errorMessage = "";
            return true;
        }
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class ConstantRatioTaskAllocationConfiguration : TaskAllocationConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override TaskAllocationMethodType GetMethodType() { return TaskAllocationMethodType.ConstantRatio; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            string name = "taCR" +
                PickBotRatio.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER) +
                (OnTheFlyExtract ? "t" : "f") +
                (OnTheFlyStore ? "t" : "f");
            name += PodSelectionConfig != null ? PodSelectionConfig.GetMethodName() : "";
            switch (RestLocationOrderType)
            {
                case PrefRestLocationForBot.Random: name += "r"; break;
                case PrefRestLocationForBot.RandomSameTier: name += "t"; break;
                case PrefRestLocationForBot.Middle: name += "c"; break;
                case PrefRestLocationForBot.MiddleSameTier: name += "m"; break;
                case PrefRestLocationForBot.Nearest: name += "n"; break;
                default: throw new ArgumentException("Unexpected argument!");
            }
            return name;
        }
        /// <summary>
        /// The ratio of bots used for picking compared to the overall count.
        /// </summary>
        public double PickBotRatio = 0.75;
        /// <summary>
        /// The time between two updates of the robot allocations. The allocations are only updated, if stations become active or inactive.
        /// </summary>
        public double RefreshAllocationTimeout = 30;
        /// <summary>
        /// Indicates whether to add further requests on-the-fly, if the robot is already carrying a suitable pod the a station where there is more work to do.
        /// </summary>
        public bool OnTheFlyExtract = true;
        /// <summary>
        /// Indicates whether to add further requests on-the-fly, if the robot is already carrying a suitable pod the a station where there is more work to do.
        /// </summary>
        public bool OnTheFlyStore = true;
        /// <summary>
        /// The sub configuration for selecting the pods for picking and replenishment.
        /// </summary>
        public DefaultPodSelectionConfiguration PodSelectionConfig = new DefaultPodSelectionConfiguration();
        /// <summary>
        /// The order to use when looking for a suitable resting location.
        /// </summary>
        public PrefRestLocationForBot RestLocationOrderType = PrefRestLocationForBot.MiddleSameTier;
    }
    /// <summary>
    /// The configuration for the corresponding method.
    /// </summary>
    public class ConceptTaskAllocationConfiguration : TaskAllocationConfiguration
    {
        /// <summary>
        /// Returns the type of the corresponding method this configuration belongs to.
        /// </summary>
        /// <returns>The type of the method.</returns>
        public override TaskAllocationMethodType GetMethodType() { return TaskAllocationMethodType.Concept; }
        /// <summary>
        /// Returns a name identifying the method.
        /// </summary>
        /// <returns>The name of the method.</returns>
        public override string GetMethodName()
        {
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            return "taCON" +
                ExtractModeProb.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER);
        }
        /// <summary>
        /// The probability for choosing an extract task.
        /// </summary>
        public double ExtractModeProb = 0.7;
    }

    #endregion
}
