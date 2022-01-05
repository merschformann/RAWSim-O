using Atto.LinearWrap;
using RAWSimO.Core.IO;
using RAWSimO.Core.Statistics;
using RAWSimO.DataPreparation;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.DEA
{
    /// <summary>
    /// Contains the implementation of the Data Envelopment Analysis model.
    /// </summary>
    public class DEAModel
    {
        /// <summary>
        /// Creates a new model.
        /// </summary>
        /// <param name="config">The configuration for the DEA model.</param>
        public DEAModel(DEAConfiguration config) { _config = config; }

        /// <summary>
        /// The config specifying the parameters.
        /// </summary>
        private DEAConfiguration _config;
        /// <summary>
        /// All underlying datapoints.
        /// </summary>
        private List<FootprintDatapoint> _datapoints = new List<FootprintDatapoint>();
        /// <summary>
        /// All service units to investigate.
        /// </summary>
        private List<ServiceUnit> _serviceUnits = new List<ServiceUnit>();
        /// <summary>
        /// All output measures for all service units.
        /// </summary>
        private MultiKeyDictionary<ServiceUnit, FootprintDatapoint.FootPrintEntry, double> _serviceUnitOutputMeasures = new MultiKeyDictionary<ServiceUnit, FootprintDatapoint.FootPrintEntry, double>();
        /// <summary>
        /// A dummy group that will be used, if no group is given at all.
        /// </summary>
        private List<List<Tuple<FootprintDatapoint.FootPrintEntry, string>>> _singleGroupDummy = new List<List<Tuple<FootprintDatapoint.FootPrintEntry, string>>>() { new List<Tuple<FootprintDatapoint.FootPrintEntry, string>>() };
        /// <summary>
        /// The scores of the different service units per group.
        /// </summary>
        private MultiKeyDictionary<List<Tuple<FootprintDatapoint.FootPrintEntry, string>>, ServiceUnit, double> _serviceUnitScores =
                new MultiKeyDictionary<List<Tuple<FootprintDatapoint.FootPrintEntry, string>>, ServiceUnit, double>();

        /// <summary>
        /// Calculates all efficiency results.
        /// </summary>
        public void Solve()
        {
            // Parse the data
            ParseData();
            // Solve DEA for each service unit
            AnalyzeServiceUnits();
            // Output all results
            WriteScoreBasedFile(_config.ResultFileCondensed, "", IOConstants.DELIMITER_VALUE.ToString());
            // Generate box plots
            GenerateBoxPlots();
        }

        /// <summary>
        /// Parses all datapoints from the footprint-file.
        /// </summary>
        private void ParseData()
        {
            // Log
            _config.LogLine("Parsing data from file " + _config.Datafile + " ...");
            // Read all data points from the file
            int counter = 0;
            using (StreamReader sr = new StreamReader(_config.Datafile))
            {
                // Init line and skip first line
                string line = sr.ReadLine();
                while ((line = sr.ReadLine()) != null)
                {
                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    // Parse the line into a new datapoint
                    _datapoints.Add(new FootprintDatapoint(line));
                    // Count
                    counter++;
                }
            }
            // Log
            _config.LogLine("Read " + counter + " datapoints!");
            _config.LogLine("Aggregating service units ...");
            // Build service units
            counter = 0;
            // Determine all relevant service unit identifiers
            Dictionary<FootprintDatapoint.FootPrintEntry, List<string>> groupIdents = _config.ServiceUnitIdents.ToDictionary(s => s, s => _datapoints.Select(d => d[s].ToString()).Distinct().ToList());
            // Build all service units and add the corresponding datapoints
            foreach (var serviceUnitIdentSet in EnumerationHelpers.CrossProduct(groupIdents.Select(g => g.Value).ToList()))
            {
                // Get the types and values of the idents of this service unit
                var ident = groupIdents.Keys.Zip(serviceUnitIdentSet, (FootprintDatapoint.FootPrintEntry identType, string identValue) => new Tuple<FootprintDatapoint.FootPrintEntry, string>(identType, identValue));
                // Get associated datapoints
                IEnumerable<FootprintDatapoint> datapoints = _datapoints.Where(d => ident.All(i => d[i.Item1].ToString() == i.Item2));
                // Create the service unit and submit all datapoints associated with it
                if (datapoints.Any())
                    _serviceUnits.Add(new ServiceUnit(ident, datapoints));
                // Count
                counter++;
            }
            // Log
            _config.LogLine("Data contained " + counter + " service units!");
            _config.LogLine("Initializing scenario ...");
            _config.Init(_datapoints);
        }

        private Dictionary<string, int> WriteScoreBasedFile(string filename, string firstLinePrefix, string delimiter)
        {
            // Output results
            _config.LogLine("Writing condensed results to " + filename + " ...");
            using (StreamWriter sw = new StreamWriter(Path.Combine(_config.OutputDirectory, filename), false))
            {
                // Write header
                sw.WriteLine(
                    // Service unit name
                    firstLinePrefix + "ServiceUnit" + delimiter +
                    // Service unit idents
                    string.Join(delimiter, _config.ServiceUnitIdents.Select(i => i.ToString())) + delimiter +
                    // Output measures
                    string.Join(delimiter, _config.Outputs.Select(o => o.Item1.ToString())) + delimiter +
                    // Group idents
                    (_config.GroupsIndividuals != null && _config.GroupsIndividuals.Any() && _config.GroupsIndividuals.First().Any() ?
                        string.Join(delimiter, _config.GroupsIndividuals.Select(i => _config.GetGroupName(i))) :
                        _config.GetNoGroupName()));
                foreach (var serviceUnit in _serviceUnits.OrderBy(s => s.Ident))
                {
                    // Write unit results
                    sw.Write(
                        // Write service unit name
                        serviceUnit.Ident + delimiter +
                        // Write service unit idents
                        string.Join(delimiter, serviceUnit.Idents.Select(i => i.Item2)) + delimiter +
                        // Write output measures
                        string.Join(delimiter, _config.Outputs.Select(o => _serviceUnitOutputMeasures[serviceUnit, o.Item1].ToString(IOConstants.FORMATTER))) + delimiter +
                        // Write scores
                        string.Join(delimiter, _config.GroupsIndividuals.Select(g => _serviceUnitScores[g, serviceUnit].ToString(IOConstants.FORMATTER))));
                    // Move to next line
                    sw.WriteLine();
                }
            }
            // Build index dictionary - just reflecting the same columns like above
            Dictionary<string, int> valueColumns = new Dictionary<string, int>();
            int index = 0;
            foreach (var entry in new List<string>() { "ServiceUnit" }
                .Concat(_config.ServiceUnitIdents.Select(i => i.ToString()))
                .Concat(_config.Outputs.Select(o => o.Item1.ToString()))
                .Concat(_config.GroupsIndividuals != null && _config.GroupsIndividuals.Any() && _config.GroupsIndividuals.First().Any() ? _config.GroupsIndividuals.Select(i => _config.GetGroupName(i)) : new List<string>() { _config.GetNoGroupName() }))
                valueColumns[entry] = ++index;
            return valueColumns;
        }

        private void GenerateBoxPlots()
        {
            // Generate dat file
            string datFilename = _config.BoxPlotBaseFilename + ".dat";
            var valueIndices = WriteScoreBasedFile(datFilename, IOConstants.GNU_PLOT_COMMENT_LINE.ToString(), IOConstants.GNU_PLOT_VALUE_SPLIT.ToString());
            // Generate plot script
            string plotScript = _config.BoxPlotBaseFilename + ".gp";
            using (StreamWriter sw = new StreamWriter(Path.Combine(_config.OutputDirectory, plotScript), false))
            {
                sw.WriteLine("reset");
                sw.WriteLine("# Output definition");
                sw.WriteLine("set terminal pdfcairo enhanced size 7, 3 font \"Consolas, 12\"");
                sw.WriteLine("set output \"" + _config.BoxPlotBaseFilename + ".pdf\"");
                sw.WriteLine("set lmargin 13");
                sw.WriteLine("set rmargin 13");
                sw.WriteLine("# Parameters");
                sw.WriteLine("set grid");
                sw.WriteLine("unset key");
                sw.WriteLine("set pointsize 0.5");
                sw.WriteLine("set style data boxplot");
                sw.WriteLine("set ylabel \"Efficiency scores\"");
                string lineColor = "#000000";
                string lineWidth = "1.2";
                // Make one diagram per group and service unit ident
                foreach (var group in _config.GroupsIndividuals)
                {
                    string groupIdentifier = _config.GroupsIndividuals != null && _config.GroupsIndividuals.Any() && _config.GroupsIndividuals.First().Any() ?
                        _config.GetGroupName(group) :
                        _config.GetNoGroupName();
                    foreach (var ident in _serviceUnits.First().Idents)
                    {
                        sw.WriteLine("set title \"" + (string.IsNullOrWhiteSpace(groupIdentifier) ? "" : groupIdentifier + " / ") + ident.Item1.ToString() + "\"");
                        sw.WriteLine("set xlabel \"" + ident.Item1.ToString() + "-values\"");
                        sw.WriteLine("plot \"" + datFilename + "\" using (1.0):" + valueIndices[groupIdentifier] + ":(0):" + valueIndices[ident.Item1.ToString()] +
                            " lc \"" + lineColor + "\" lw " + lineWidth);
                    }
                }
                sw.WriteLine("reset");
                sw.WriteLine("exit");
            }
            // Generate command script
            string commandScript = _config.BoxPlotBaseFilename + ".cmd";
            using (StreamWriter sw = new StreamWriter(Path.Combine(_config.OutputDirectory, commandScript), false))
            {
                sw.WriteLine("gnuplot " + plotScript);
            }
            // Execute command
            DataProcessor.ExecuteScript(Path.Combine(_config.OutputDirectory, commandScript), (string msg) => { _config.LogLine(msg); });
        }

        private void AnalyzeServiceUnits()
        {
            // Keep track of count
            int overallCount = _serviceUnits.Count * _config.GroupsIndividuals.Count;
            int counter = 0;
            // --> Solve all
            _config.LogLine("Analyzing all service units within all given groups - " + overallCount + " to go!");
            // Iterate all groups (use a dummy group if no grouping has to be done - this should work ok with the interior part of the loops)
            foreach (var groupIdents in _config.GroupsIndividuals)
            {
                // Investigate all service units for this group within the given scenario
                foreach (var serviceUnitInFocus in _serviceUnits)
                {
                    // Log
                    _config.Log((++counter) + "/" + overallCount + ": " + serviceUnitInFocus.Ident + "/" + (groupIdents.Any() ? string.Join(",", groupIdents.Select(i => i.Item2)) : "Overall") + " (SU/group)");
                    // Init
                    LinearModel model = new LinearModel(_config.SolverChoice, null /* Disable output flood for now, use following to enable: (string s) => { _config.Log(s); } */ );
                    // --> Init variables
                    Variable efficiencyRating = new Variable(model, VariableType.Continuous, double.NegativeInfinity, double.PositiveInfinity, "EfficiencyRating");
                    VariableCollection<ServiceUnit> weights = new VariableCollection<ServiceUnit>(model, VariableType.Continuous, 0, double.PositiveInfinity, (ServiceUnit u) => { return u.Ident; });

                    // --> Build model
                    // Add objective
                    if (_config.InputOriented)
                        model.SetObjective(efficiencyRating + 0, OptimizationSense.Minimize);
                    else
                        model.SetObjective(efficiencyRating + 0, OptimizationSense.Maximize);
                    // Add input constraints
                    if (_config.Inputs.Any())
                    {
                        foreach (var inputEntry in _config.Inputs)
                        {
                            // Shift and flip (if required) all values
                            Dictionary<ServiceUnit, double> inputValues = _serviceUnits.ToDictionary(
                                k => k,
                                s => s.GetValue(inputEntry.Item1, (FootprintDatapoint f) => { return groupIdents.All(g => g.Item2 == f[g.Item1].ToString()); }));
                            // In case of loss values, convert them
                            if (inputEntry.Item2 == InputType.Hindrance)
                            {
                                switch (inputEntry.Item1)
                                {
                                    case FootprintDatapoint.FootPrintEntry.SKUs:
                                        // Simply inverse them
                                        foreach (var serviceUnit in inputValues.Keys.ToList())
                                            inputValues[serviceUnit] = 1.0 / inputValues[serviceUnit];
                                        break;
                                    default:
                                        // Simply flip them (next step will convert the numbers to positive ones again)
                                        foreach (var serviceUnit in inputValues.Keys.ToList())
                                            inputValues[serviceUnit] *= -1;
                                        break;
                                }
                            }
                            // If there is a negative value, shift all values to positive ones
                            double minOutputValue = inputValues.Min(v => v.Value);
                            if (minOutputValue < 0)
                                foreach (var serviceUnit in inputValues.Keys.ToList())
                                    inputValues[serviceUnit] += Math.Abs(minOutputValue);
                            // Add constraint
                            if (_config.InputOriented)
                                model.AddConstr(
                                    // Sum of all other weighted inputs
                                    LinearExpression.Sum(_serviceUnits.Select(s => inputValues[s] * weights[s])) <=
                                    // Shall be smaller than the weighted input of the service unit in focus
                                    efficiencyRating * inputValues[serviceUnitInFocus], "Input" + inputEntry);
                            else
                                model.AddConstr(
                                    // Sum of all other weighted inputs
                                    LinearExpression.Sum(_serviceUnits.Select(s => inputValues[s] * weights[s])) <=
                                    // Shall be smaller than the weighted input of the service unit in focus
                                    inputValues[serviceUnitInFocus], "Input" + inputEntry);
                        }
                    }
                    else
                    {
                        // Add constant uniform inputs for all service units
                        if (_config.InputOriented)
                            model.AddConstr(
                                // Sum of all other weighted inputs
                                LinearExpression.Sum(_serviceUnits.Select(s => weights[s])) <=
                                // Shall be smaller than the weighted input of the service unit in focus
                                efficiencyRating * 1.0, "InputConstant");
                        else
                            model.AddConstr(
                                // Sum of all other weighted inputs
                                LinearExpression.Sum(_serviceUnits.Select(s => weights[s])) <=
                                // Shall be smaller than the weighted input of the service unit in focus
                                1.0, "InputConstant");
                    }
                    // Add output constraints
                    if (_config.Outputs.Any())
                    {
                        // Add output constraint using the actual entry
                        foreach (var outputEntry in _config.Outputs)
                        {
                            // Shift and flip (if required) all values
                            Dictionary<ServiceUnit, double> outputValues = _serviceUnits.ToDictionary(
                                k => k,
                                s => s.GetValue(outputEntry.Item1, (FootprintDatapoint f) => { return groupIdents.All(g => g.Item2 == f[g.Item1].ToString()); }));
                            // Store performance for this measure
                            foreach (var serviceUnit in _serviceUnits)
                                _serviceUnitOutputMeasures[serviceUnit, outputEntry.Item1] = outputValues[serviceUnit];
                            // In case of loss values, convert them
                            if (outputEntry.Item2 == OutputType.Loss)
                            {
                                switch (outputEntry.Item1)
                                {
                                    case FootprintDatapoint.FootPrintEntry.OrderTurnoverTimeAvg:
                                    case FootprintDatapoint.FootPrintEntry.OrderTurnoverTimeMed:
                                    case FootprintDatapoint.FootPrintEntry.OrderTurnoverTimeLQ:
                                    case FootprintDatapoint.FootPrintEntry.OrderTurnoverTimeUQ:
                                    case FootprintDatapoint.FootPrintEntry.OrderThroughputTimeAvg:
                                    case FootprintDatapoint.FootPrintEntry.OrderThroughputTimeMed:
                                    case FootprintDatapoint.FootPrintEntry.OrderThroughputTimeLQ:
                                    case FootprintDatapoint.FootPrintEntry.OrderThroughputTimeUQ:
                                    case FootprintDatapoint.FootPrintEntry.BundleTurnoverTimeAvg:
                                    case FootprintDatapoint.FootPrintEntry.BundleTurnoverTimeMed:
                                    case FootprintDatapoint.FootPrintEntry.BundleTurnoverTimeLQ:
                                    case FootprintDatapoint.FootPrintEntry.BundleTurnoverTimeUQ:
                                    case FootprintDatapoint.FootPrintEntry.BundleThroughputTimeAvg:
                                    case FootprintDatapoint.FootPrintEntry.BundleThroughputTimeMed:
                                    case FootprintDatapoint.FootPrintEntry.BundleThroughputTimeLQ:
                                    case FootprintDatapoint.FootPrintEntry.BundleThroughputTimeUQ:
                                        // Simply inverse them
                                        foreach (var serviceUnit in outputValues.Keys.ToList())
                                            outputValues[serviceUnit] = 1.0 / outputValues[serviceUnit];
                                        break;
                                    case FootprintDatapoint.FootPrintEntry.OSIdleTimeAvg:
                                    case FootprintDatapoint.FootPrintEntry.OSIdleTimeMed:
                                    case FootprintDatapoint.FootPrintEntry.OSIdleTimeLQ:
                                    case FootprintDatapoint.FootPrintEntry.OSIdleTimeUQ:
                                    case FootprintDatapoint.FootPrintEntry.ISIdleTimeAvg:
                                    case FootprintDatapoint.FootPrintEntry.ISIdleTimeMed:
                                    case FootprintDatapoint.FootPrintEntry.ISIdleTimeLQ:
                                    case FootprintDatapoint.FootPrintEntry.ISIdleTimeUQ:
                                    case FootprintDatapoint.FootPrintEntry.OSDownTimeAvg:
                                    case FootprintDatapoint.FootPrintEntry.OSDownTimeMed:
                                    case FootprintDatapoint.FootPrintEntry.OSDownTimeLQ:
                                    case FootprintDatapoint.FootPrintEntry.OSDownTimeUQ:
                                    case FootprintDatapoint.FootPrintEntry.ISDownTimeAvg:
                                    case FootprintDatapoint.FootPrintEntry.ISDownTimeMed:
                                    case FootprintDatapoint.FootPrintEntry.ISDownTimeLQ:
                                    case FootprintDatapoint.FootPrintEntry.ISDownTimeUQ:
                                    case FootprintDatapoint.FootPrintEntry.LateOrdersFractional:
                                        // As these should be values between 0.0 and 1.0 just flip them within the range
                                        {
                                            if (outputValues.Values.Any(v => v < 0 || v > 1))
                                                throw new ArgumentException("Expected values to be within the range [0,1], but found one out of range!");
                                            foreach (var serviceUnit in outputValues.Keys.ToList())
                                                outputValues[serviceUnit] = 1.0 - outputValues[serviceUnit];
                                        }
                                        break;
                                    case FootprintDatapoint.FootPrintEntry.TimingDecisionsOverall:
                                    case FootprintDatapoint.FootPrintEntry.TimingPathPlanningAvg:
                                    case FootprintDatapoint.FootPrintEntry.TimingPathPlanningOverall:
                                    case FootprintDatapoint.FootPrintEntry.TimingPathPlanningCount:
                                    case FootprintDatapoint.FootPrintEntry.TimingTaskAllocationAvg:
                                    case FootprintDatapoint.FootPrintEntry.TimingTaskAllocationOverall:
                                    case FootprintDatapoint.FootPrintEntry.TimingTaskAllocationCount:
                                    case FootprintDatapoint.FootPrintEntry.TimingItemStorageAvg:
                                    case FootprintDatapoint.FootPrintEntry.TimingItemStorageOverall:
                                    case FootprintDatapoint.FootPrintEntry.TimingItemStorageCount:
                                    case FootprintDatapoint.FootPrintEntry.TimingPodStorageAvg:
                                    case FootprintDatapoint.FootPrintEntry.TimingPodStorageOverall:
                                    case FootprintDatapoint.FootPrintEntry.TimingPodStorageCount:
                                    case FootprintDatapoint.FootPrintEntry.TimingRepositioningAvg:
                                    case FootprintDatapoint.FootPrintEntry.TimingRepositioningOverall:
                                    case FootprintDatapoint.FootPrintEntry.TimingRepositioningCount:
                                    case FootprintDatapoint.FootPrintEntry.TimingReplenishmentBatchingAvg:
                                    case FootprintDatapoint.FootPrintEntry.TimingReplenishmentBatchingOverall:
                                    case FootprintDatapoint.FootPrintEntry.TimingReplenishmentBatchingCount:
                                    case FootprintDatapoint.FootPrintEntry.TimingOrderBatchingAvg:
                                    case FootprintDatapoint.FootPrintEntry.TimingOrderBatchingOverall:
                                    case FootprintDatapoint.FootPrintEntry.TimingOrderBatchingCount:
                                        // Simply inverse them
                                        foreach (var serviceUnit in outputValues.Keys.ToList())
                                            outputValues[serviceUnit] = 1.0 / outputValues[serviceUnit];
                                        break;
                                    default:
                                        // Simply flip them (next step will convert the numbers to positive ones again)
                                        foreach (var serviceUnit in outputValues.Keys.ToList())
                                            outputValues[serviceUnit] *= -1;
                                        break;
                                }
                            }
                            // If there is a negative value, shift all values to positive ones
                            double minOutputValue = outputValues.Min(v => v.Value);
                            if (minOutputValue < 0)
                                foreach (var serviceUnit in outputValues.Keys.ToList())
                                    outputValues[serviceUnit] += Math.Abs(minOutputValue);
                            // Add constraint
                            if (_config.InputOriented)
                                model.AddConstr(
                                    // Sum of all other weighted inputs
                                    LinearExpression.Sum(_serviceUnits.Select(s => outputValues[s] * weights[s])) >=
                                    // Shall be smaller than the weighted input of the service unit in focus
                                    outputValues[serviceUnitInFocus], "Output" + outputEntry);
                            else
                                model.AddConstr(
                                    // Sum of all other weighted inputs
                                    LinearExpression.Sum(_serviceUnits.Select(s => outputValues[s] * weights[s])) >=
                                    // Shall be smaller than the weighted input of the service unit in focus
                                    efficiencyRating * outputValues[serviceUnitInFocus], "Output" + outputEntry);
                        }
                    }
                    else
                    {
                        // Add constant uniform outputs for all service units
                        if (_config.InputOriented)
                            model.AddConstr(
                                // Sum of all other weighted inputs
                                LinearExpression.Sum(_serviceUnits.Select(s => weights[s])) >=
                                // Shall be smaller than the weighted input of the service unit in focus
                                1.0, "OutputConstant");
                        else
                            model.AddConstr(
                                // Sum of all other weighted inputs
                                LinearExpression.Sum(_serviceUnits.Select(s => weights[s])) >=
                                // Shall be smaller than the weighted input of the service unit in focus
                                efficiencyRating * 1.0, "OutputConstant");
                    }

                    // Sum weights
                    if (_config.WeightsSumToOne)
                        // Summed weights have to be equal to one
                        model.AddConstr(LinearExpression.Sum(_serviceUnits.Select(s => weights[s])) == 1.0, "WeightSum");

                    // Commit changes
                    model.Update();

                    // --> Solve model
                    model.Optimize();

                    // --> Get solution
                    if (!model.HasSolution())
                        throw new InvalidOperationException("Model is infeasible for service unit: " + serviceUnitInFocus.Ident);
                    _serviceUnitScores[groupIdents, serviceUnitInFocus] = efficiencyRating.Value;
                    _config.LogLine(" - Efficiency: " + (efficiencyRating.Value * 100).ToString(IOConstants.FORMATTER) + " %");
                }

                // Transform efficiency
                if (!_config.InputOriented && _config.TransformOutputOrientedEfficiency)
                {
                    //double maxEfficiency = _serviceUnits.Max(s => _serviceUnitScores[groupIdents, s]);
                    foreach (var serviceUnit in _serviceUnits)
                        _serviceUnitScores[groupIdents, serviceUnit] = 1.0 / _serviceUnitScores[groupIdents, serviceUnit];
                    // _serviceUnitScores[groupIdents, serviceUnit] /= maxEfficiency;
                }
            }
        }
    }
}
