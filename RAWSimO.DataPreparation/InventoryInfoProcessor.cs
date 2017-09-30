using RAWSimO.Core.Configurations;
using RAWSimO.Core.IO;
using RAWSimO.Core.Statistics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.DataPreparation
{
    /// <summary>
    /// Controls the plotting of inventory related data.
    /// </summary>
    public class InventoryInfoProcessor
    {
        /// <summary>
        /// Plots the frequencies of a given item configuration file.
        /// </summary>
        /// <param name="filepath">The item configuration.</param>
        public static void PlotSimpleInventoryFrequencies(string filepath)
        {
            // Set group count
            int groupCount = 10;
            // Parse the given file
            Console.WriteLine("Parsing generator config ...");
            SimpleItemGeneratorConfiguration config = InstanceIO.ReadSimpleItemGeneratorConfig(filepath);
            string outputBasename = config.Name;
            string frequencyFilename = config.Name;
            string directory = Path.GetDirectoryName(filepath);
            // Build frequency groups of the items
            Console.WriteLine("Building frequency groups ...");
            int[] frequencyGroups = new int[groupCount]; double maxItemWeight = config.ItemWeights.Max(w => w.Value);
            bool plotWeights = config.ItemDescriptionWeights != null && config.ItemDescriptionWeights.Count > 0;
            List<double> weights = plotWeights ? config.ItemDescriptionWeights.Select(w => w.Value).OrderBy(w => w).ToList() : null;
            bool plotBundleSize = config.ItemDescriptionBundleSizes != null && config.ItemDescriptionBundleSizes.Count > 0;
            List<int> bundleSizes = plotBundleSize ? config.ItemDescriptionBundleSizes.Select(w => w.Value).OrderBy(w => w).ToList() : null;
            double overallWeight = config.ItemWeights.Sum(w => w.Value);
            List<double> probabilities = config.ItemWeights.Select(w => w.Value / overallWeight).OrderBy(p => p).ToList();
            List<double> frequencies = config.ItemWeights.Select(w => w.Value / maxItemWeight).OrderBy(f => f).ToList();
            List<double> frequenciesForGroupBuilding = frequencies.ToList();
            for (int i = 0; i < frequencyGroups.Length; i++)
            {
                double groupFreqCap = (i + 1.0) / frequencyGroups.Length;
                int countInRange = frequenciesForGroupBuilding.TakeWhile(f => f <= groupFreqCap).Count();
                frequencyGroups[i] = countInRange;
                frequenciesForGroupBuilding = frequenciesForGroupBuilding.Skip(countInRange).ToList();
            }
            // Write data files
            Console.WriteLine("Generating plot data files ...");
            // Write the data file for the group based plot
            string frequencyGroupsPlotDataFile = outputBasename + "groups.dat";
            using (StreamWriter sw = new StreamWriter(Path.Combine(directory, frequencyGroupsPlotDataFile), false))
            {
                sw.WriteLine(IOConstants.GNU_PLOT_COMMENT_LINE + " frequencyvalue groupcount");
                for (int i = 0; i < frequencyGroups.Length; i++)
                {
                    // Calculate x-value for group (use mid between lower and upper group boundaries)
                    double groupXValue =
                        (((double)i / frequencyGroups.Length)) +
                        ((((double)(i + 1) / frequencyGroups.Length) - ((double)i / frequencyGroups.Length)) / 2.0);
                    sw.WriteLine(groupXValue.ToString(IOConstants.FORMATTER) + IOConstants.GNU_PLOT_VALUE_SPLIT + frequencyGroups[i].ToString());
                }
            }
            // Write the data file for a simple SKU/frequency plot
            string frequencySimplePlotDataFile = outputBasename + "simple.dat";
            using (StreamWriter sw = new StreamWriter(Path.Combine(directory, frequencySimplePlotDataFile), false))
            {
                sw.WriteLine(IOConstants.GNU_PLOT_COMMENT_LINE + " sku frequency");
                // Write frequencies in reverse order
                int sku = 0;
                foreach (var freq in frequencies.OrderByDescending(f => f))
                {
                    // Update
                    sku++;
                    // Flush
                    sw.WriteLine(sku.ToString() + IOConstants.GNU_PLOT_VALUE_SPLIT + freq.ToString(IOConstants.FORMATTER));
                }
            }
            // Write the data file for a cumulative SKU/frequency plot
            string frequencyCumSimplePlotDataFile = outputBasename + "cumulative.dat";
            using (StreamWriter sw = new StreamWriter(Path.Combine(directory, frequencyCumSimplePlotDataFile), false))
            {
                sw.WriteLine(IOConstants.GNU_PLOT_COMMENT_LINE + " sku cum.frequency");
                // Prepare frequency info
                double cumFrequency = 0; double overallFrequency = frequencies.Sum(); int sku = 0;
                // Aggregate and write cumulative frequencies starting with the largest
                foreach (var freq in frequencies.OrderByDescending(f => f))
                {
                    // Update
                    sku++; cumFrequency += freq;
                    // Flush
                    sw.WriteLine(sku.ToString() + IOConstants.GNU_PLOT_VALUE_SPLIT + (cumFrequency / overallFrequency).ToString(IOConstants.FORMATTER));
                }
            }
            // Write data file for the real probability plot
            string probabilityPlotDataFile = outputBasename + "probability.dat";
            using (StreamWriter sw = new StreamWriter(Path.Combine(directory, probabilityPlotDataFile), false))
            {
                sw.WriteLine(IOConstants.GNU_PLOT_COMMENT_LINE + " sku probability");
                for (int i = 0; i < probabilities.Count; i++)
                {
                    // Reverse order (start with the highest probability)
                    sw.WriteLine((probabilities.Count - i).ToString() + IOConstants.GNU_PLOT_VALUE_SPLIT + probabilities[i].ToString(IOConstants.FORMATTER));
                }
            }
            // Write data file for the item description weights plot
            string itemDescriptionWeightsPlotDataFile = outputBasename + "weights.dat";
            if (plotWeights)
            {
                using (StreamWriter sw = new StreamWriter(Path.Combine(directory, itemDescriptionWeightsPlotDataFile), false))
                {
                    sw.WriteLine(IOConstants.GNU_PLOT_COMMENT_LINE + " sku weight");
                    for (int i = 0; i < weights.Count; i++)
                    {
                        // Write
                        sw.WriteLine((i + 1).ToString() + IOConstants.GNU_PLOT_VALUE_SPLIT + weights[i].ToString(IOConstants.FORMATTER));
                    }
                }
            }
            // Write data file for the item description weights plot
            string itemDescriptionBundleSizesPlotDataFile = outputBasename + "bundlesizes.dat";
            if (plotBundleSize)
            {
                using (StreamWriter sw = new StreamWriter(Path.Combine(directory, itemDescriptionBundleSizesPlotDataFile), false))
                {
                    sw.WriteLine(IOConstants.GNU_PLOT_COMMENT_LINE + " sku bundlesize");
                    for (int i = 0; i < bundleSizes.Count; i++)
                    {
                        // Write
                        sw.WriteLine((i + 1).ToString() + IOConstants.GNU_PLOT_VALUE_SPLIT + bundleSizes[i].ToString(IOConstants.FORMATTER));
                    }
                }
            }
            // Generate plot script
            Console.WriteLine("Generating plot scipt ...");
            string plotScriptName = outputBasename + ".gp";
            using (StreamWriter sw = new StreamWriter(Path.Combine(directory, plotScriptName)))
            {
                sw.WriteLine("reset");
                sw.WriteLine("# Output definition");
                sw.WriteLine("set terminal pdfcairo enhanced size 7, 3 font \"Consolas, 12\"");
                sw.WriteLine("set lmargin 13");
                sw.WriteLine("set rmargin 13");
                sw.WriteLine("# Parameters");
                sw.WriteLine("set key right top Right");
                sw.WriteLine("set grid");
                sw.WriteLine("set style fill solid 0.75");
                sw.WriteLine("# Line-Styles");
                sw.WriteLine("set style line 1 linetype 1 linecolor rgb \"" + PlotColoring.GetHexCode(PlotColors.MediumBlue) + "\" linewidth 1");
                sw.WriteLine("set output \"" + frequencyFilename + ".pdf\"");
                sw.WriteLine("set title \"" + frequencyFilename + "\"");
                sw.WriteLine("set xlabel \"Frequency\"");
                sw.WriteLine("set ylabel \"SKU count\"");
                sw.WriteLine("plot \\");
                sw.WriteLine("\"" + frequencyGroupsPlotDataFile + "\" u 1:2 w boxes linestyle 1 t \"SKU frequencies\"");
                sw.WriteLine("set title \"" + frequencyFilename + "\"");
                sw.WriteLine("set xlabel \"SKU\"");
                sw.WriteLine("set ylabel \"frequency\"");
                sw.WriteLine("plot \\");
                sw.WriteLine("\"" + frequencySimplePlotDataFile + "\" u 1:2 w steps linestyle 1 t \"SKU frequencies\"");
                sw.WriteLine("set title \"" + frequencyFilename + "\"");
                sw.WriteLine("set xlabel \"SKU\"");
                sw.WriteLine("set ylabel \"rel. cum. frequency\"");
                sw.WriteLine("plot \\");
                sw.WriteLine("\"" + frequencyCumSimplePlotDataFile + "\" u 1:2 w steps linestyle 1 t \"cum. SKU frequencies\"");
                sw.WriteLine("set title \"" + frequencyFilename + "\"");
                sw.WriteLine("set xlabel \"SKU\"");
                sw.WriteLine("set ylabel \"probability\"");
                sw.WriteLine("plot \\");
                sw.WriteLine("\"" + probabilityPlotDataFile + "\" u 1:2 w steps linestyle 1 t \"SKU probabilities\"");
                if (plotWeights)
                {
                    sw.WriteLine("set title \"" + frequencyFilename + "\"");
                    sw.WriteLine("set xlabel \"SKU\"");
                    sw.WriteLine("set ylabel \"size\"");
                    sw.WriteLine("plot \\");
                    sw.WriteLine("\"" + itemDescriptionWeightsPlotDataFile + "\" u 1:2 w steps linestyle 1 t \"SKU size\"");
                }
                if (plotBundleSize)
                {
                    sw.WriteLine("set title \"" + frequencyFilename + "\"");
                    sw.WriteLine("set xlabel \"SKU\"");
                    sw.WriteLine("set ylabel \"units\"");
                    sw.WriteLine("plot \\");
                    sw.WriteLine("\"" + itemDescriptionBundleSizesPlotDataFile + "\" u 1:2 w steps linestyle 1 t \"SKU replenishment order size\"");
                }
                sw.WriteLine("reset");
                sw.WriteLine("exit");
            }
            string commandScriptName = outputBasename + ".cmd";
            using (StreamWriter sw = new StreamWriter(Path.Combine(directory, commandScriptName)))
            {
                sw.WriteLine("gnuplot " + plotScriptName);
            }
            // Log
            Console.WriteLine("Calling plot script ...");
            // Execute plot script
            DataProcessor.ExecuteScript(Path.Combine(directory, commandScriptName), (string msg) => { Console.WriteLine(msg); });
        }

        /// <summary>
        /// Plot information about the SKUs used during the execution of a simulation.
        /// </summary>
        /// <param name="resultDir">The result directory containing all necessary files.</param>
        /// <param name="logger">A logger used to log some progress.</param>
        public static void PlotExecutionItemDescriptionInfo(string resultDir, Action<string> logger)
        {
            // Set base name
            string filesBasename = "itemdescriptionplot";
            logger?.Invoke("Parsing results and preparing intermediate files ...");
            // Get instance name
            string instanceName;
            using (StreamReader sr = new StreamReader(Path.Combine(resultDir, IOConstants.StatFileNames[IOConstants.StatFile.InstanceName])))
            {
                string instanceNameLine = "";
                while (string.IsNullOrWhiteSpace((instanceNameLine = sr.ReadLine()))) { /* Nothing to do - the head of the loop does the work */ }
                instanceName = instanceNameLine.Trim();
            }
            // Fetch config name
            string configName;
            using (StreamReader sr = new StreamReader(Path.Combine(resultDir, IOConstants.StatFileNames[IOConstants.StatFile.ControllerName])))
            {
                string configNameLine = "";
                while (string.IsNullOrWhiteSpace((configNameLine = sr.ReadLine()))) { /* Nothing to do - the head of the loop does the work */ }
                configName = configNameLine.Trim();
            }
            // Parse measured information
            List<ItemDescriptionFrequencyDatapoint> datapoints = new List<ItemDescriptionFrequencyDatapoint>();
            using (StreamReader sr = new StreamReader(Path.Combine(resultDir, IOConstants.StatFileNames[IOConstants.StatFile.ItemDescriptionStatistics])))
            {
                // Parse item description information
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    // Trim
                    line = line.Trim();
                    // Skip empty or comment lines
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith(IOConstants.COMMENT_LINE))
                        continue;
                    // Actually parse the line
                    datapoints.Add(new ItemDescriptionFrequencyDatapoint(line));
                }
            }
            logger?.Invoke("Found " + datapoints.Count + " datapoints!");
            // Output intermediate file
            string datFilename = filesBasename + ".dat";
            using (StreamWriter sw = new StreamWriter(Path.Combine(resultDir, datFilename), false))
            {
                sw.WriteLine(IOConstants.GNU_PLOT_COMMENT_LINE + "OrderedIndex" + IOConstants.GNU_PLOT_VALUE_SPLIT + "StaticFrequency" + IOConstants.GNU_PLOT_VALUE_SPLIT + "MeasuredFrequency" + IOConstants.GNU_PLOT_VALUE_SPLIT + "OrderCount");
                int newIndex = 0;
                foreach (var datapoint in datapoints.OrderByDescending(dp => dp.StaticFrequency))
                    sw.WriteLine(
                        (++newIndex).ToString() + IOConstants.GNU_PLOT_VALUE_SPLIT +
                        datapoint.StaticFrequency.ToString(IOConstants.FORMATTER) + IOConstants.GNU_PLOT_VALUE_SPLIT +
                        datapoint.MeasuredFrequency.ToString(IOConstants.FORMATTER) + IOConstants.GNU_PLOT_VALUE_SPLIT +
                        datapoint.OrderCount.ToString(IOConstants.FORMATTER));
            }
            // Generate plot script
            logger?.Invoke("Generating plot script ...");
            string plotScriptname = filesBasename + ".gp";
            using (StreamWriter sw = new StreamWriter(Path.Combine(resultDir, plotScriptname), false))
            {
                sw.WriteLine("reset");
                sw.WriteLine("# Output definition");
                sw.WriteLine("set terminal pdfcairo enhanced size 7, 3 font \"Consolas, 12\"");
                sw.WriteLine("set lmargin 13");
                sw.WriteLine("set rmargin 13");
                sw.WriteLine("# Parameters");
                sw.WriteLine("set key right top Right");
                sw.WriteLine("set grid");
                sw.WriteLine("# Line-Styles");
                sw.WriteLine("set style line 1 linetype 1 linecolor rgb \"" + PlotColoring.GetHexCode(PlotColors.MediumGrey) + "\" linewidth 3");
                sw.WriteLine("set style line 2 linetype 1 linecolor rgb \"" + PlotColoring.GetHexCode(PlotColors.MediumGreen) + "\" linewidth 3");
                sw.WriteLine("set style line 3 linetype 1 linecolor rgb \"" + PlotColoring.GetHexCode(PlotColors.MediumRed) + "\" linewidth 3");
                sw.WriteLine("set output \"" + filesBasename + ".pdf\"");
                sw.WriteLine("set title \"" + instanceName + " / " + configName + "\"");
                sw.WriteLine("set xlabel \"SKUs\"");
                sw.WriteLine("set ylabel \"Frequency\"");
                sw.WriteLine("set y2label \"# items ordered\"");
                sw.WriteLine("set y2tics");
                sw.WriteLine("set yrange [" + 0.ToString(IOConstants.FORMATTER) + ":" + 1.ToString(IOConstants.FORMATTER) + "]");
                sw.WriteLine("set y2range [" + 0.ToString(IOConstants.FORMATTER) + ":" + (datapoints.Max(d => d.OrderCount)).ToString(IOConstants.FORMATTER) + "]");
                sw.WriteLine("plot \\");
                sw.WriteLine("\"" + datFilename + "\" u 1:2 w lines linestyle 1 t \"Static frequencies\", \\");
                sw.WriteLine("\"" + datFilename + "\" u 1:3 w lines linestyle 2 t \"Measured frequencies\", \\");
                sw.WriteLine("\"" + datFilename + "\" u 1:4 w lines axes x1y2 linestyle 3 t \"Order count\"");
                sw.WriteLine("reset");
                sw.WriteLine("exit");
            }
            // Execute plot script
            string commandScriptname = filesBasename + ".cmd";
            using (StreamWriter sw = new StreamWriter(Path.Combine(resultDir, commandScriptname), false))
                sw.WriteLine("gnuplot " + Path.GetFileName(plotScriptname));
            // Execute plot script
            DataProcessor.ExecuteScript(Path.Combine(resultDir, commandScriptname), logger);
        }
    }
}
