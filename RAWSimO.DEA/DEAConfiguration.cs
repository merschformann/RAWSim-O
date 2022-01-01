using Atto.LinearWrap;
using RAWSimO.Core.Statistics;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.DEA
{
    /// <summary>
    /// Specifies all parameters for conducting a Data Envelopment Analysis.
    /// </summary>
    public class DEAConfiguration
    {
        #region Logging

        /// <summary>
        /// The action to use for logging.
        /// </summary>
        public Action<string> LogAction;
        /// <summary>
        /// Logs the given message.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public void Log(string msg) { LogAction?.Invoke(msg); }
        /// <summary>
        /// Logs the given message and terminates the line.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public void LogLine(string msg) { LogAction?.Invoke(msg + Environment.NewLine); }

        #endregion

        #region I/O

        /// <summary>
        /// The data file containing all service unit results.
        /// </summary>
        public string Datafile;
        /// <summary>
        /// The directory to write all output files to.
        /// </summary>
        public string OutputDirectory { get { return !string.IsNullOrEmpty(Path.GetDirectoryName(Datafile)) ? Path.GetDirectoryName(Datafile) : Directory.GetCurrentDirectory(); } }
        /// <summary>
        /// The name of the result files containing only the service units and their respective scores in the given groups and inputs / outputs.
        /// </summary>
        public string ResultFileCondensed = "deascore.csv";
        /// <summary>
        /// The base name to use when generating the box plots.
        /// </summary>
        public string BoxPlotBaseFilename = "deaboxplots";

        #endregion

        #region Basic settings

        /// <summary>
        /// The name of the config.
        /// </summary>
        public string Name;
        /// <summary>
        /// The solver to use for solving the linear model.
        /// </summary>
        public SolverType SolverChoice;
        /// <summary>
        /// Indicates whether an input or ouput oriented model formulation will be used.
        /// </summary>
        public bool InputOriented;
        /// <summary>
        /// Indicates whether output oriented efficiency will be written out as raw or transformed.
        /// </summary>
        public bool TransformOutputOrientedEfficiency;
        /// <summary>
        /// Indicates whether the weights will be summed to one.
        /// </summary>
        public bool WeightsSumToOne = true;
        /// <summary>
        /// Contains all idents that identify a service unit.
        /// </summary>
        public List<FootprintDatapoint.FootPrintEntry> ServiceUnitIdents;
        /// <summary>
        /// Contains all idents to determine the groups to assess independently.
        /// </summary>
        public List<FootprintDatapoint.FootPrintEntry> Groups;
        /// <summary>
        /// All input entries.
        /// </summary>
        public List<Tuple<FootprintDatapoint.FootPrintEntry, InputType>> Inputs;
        /// <summary>
        /// All output entries.
        /// </summary>
        public List<Tuple<FootprintDatapoint.FootPrintEntry, OutputType>> Outputs;

        #endregion

        #region Support methods

        /// <summary>
        /// Indicates whether this config was initialized with the actual data.
        /// </summary>
        private bool _initialized = false;

        private List<List<Tuple<FootprintDatapoint.FootPrintEntry, string>>> _groupsIndividuals;
        /// <summary>
        /// The actual groups as indicated by the group field.
        /// </summary>
        public List<List<Tuple<FootprintDatapoint.FootPrintEntry, string>>> GroupsIndividuals
        {
            get { if (!_initialized) throw new InvalidOperationException("Initialize first!"); return _groupsIndividuals; }
        }

        /// <summary>
        /// Returns a string identifying the group within this configuration.
        /// </summary>
        /// <param name="groupIndividual">The group to get a representing string for.</param>
        /// <returns>The string representing the group.</returns>
        public string GetGroupName(List<Tuple<FootprintDatapoint.FootPrintEntry, string>> groupIndividual) { return string.Join("/", groupIndividual.Select(e => e.Item2)); }
        /// <summary>
        /// Returns the name to use, if no group exists.
        /// </summary>
        /// <returns>The name to describe the overall (groupless) results.</returns>
        public string GetNoGroupName() { return "Overall"; }

        /// <summary>
        /// Initializes the config with the actual data.
        /// </summary>
        /// <param name="footprints">The actual data to be assessed with DEA.</param>
        public void Init(IEnumerable<FootprintDatapoint> footprints)
        {
            _initialized = true;
            if (Groups != null && Groups.Any())
                _groupsIndividuals =
                    EnumerationHelpers.CrossProduct(Groups.Select(i => footprints.Select(d => new Tuple<FootprintDatapoint.FootPrintEntry, string>(i, d[i].ToString())).Distinct().ToList()).ToList()).ToList();
            else
                _groupsIndividuals = new List<List<Tuple<FootprintDatapoint.FootPrintEntry, string>>>() { new List<Tuple<FootprintDatapoint.FootPrintEntry, string>>() };
        }

        #endregion
    }

    /// <summary>
    /// Defines whether the given input type is helping the performance of the system or does potentially negatively impact it.
    /// </summary>
    public enum InputType
    {
        /// <summary>
        /// Indicates that the given resource can be used to increase system performance.
        /// </summary>
        Resource,
        /// <summary>
        /// Indicates that using more of the given resource does have positive effects outside the system, but does hinder the performance of it.
        /// </summary>
        Hindrance,
    }
    /// <summary>
    /// Defines whether we want to have a high value or a low one of the respective output factor.
    /// </summary>
    public enum OutputType
    {
        /// <summary>
        /// This indicates an output factor that is beneficial, hence, the more the better.
        /// </summary>
        Benefit,
        /// <summary>
        /// This indicates an output factor that causes a loss, hence, lesse is better.
        /// </summary>
        Loss
    }
}
