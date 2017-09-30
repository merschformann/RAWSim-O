using RAWSimO.Core.IO;
using RAWSimO.Core.Statistics;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.DataPreparation
{
    /// <summary>
    /// Exposes methods for box-blotting footprint based data.
    /// </summary>
    public class FootprintBoxPlotProcessor
    {
        /// <summary>
        /// The prefix that is added to all boxplot related files.
        /// </summary>
        public const string BOX_PLOT_PREFIX = "boxplot";

        /// <summary>
        /// Plots a number of boxplots for the given data and groups.
        /// </summary>
        /// <param name="footprintFile">The file containing the footprint data to plot.</param>
        /// <param name="plotGroups">The groups to distinguish.</param>
        /// <param name="plotData">The data types to plot.</param>
        public static void Plot(string footprintFile, IEnumerable<FootprintDatapoint.FootPrintEntry> plotGroups, IEnumerable<FootprintDatapoint.FootPrintEntry> plotData)
        {
            // Get output directory
            string outputDir = Path.GetDirectoryName(footprintFile);
            // Parse footprints
            List<FootprintDatapoint> datapoints = SharedDataPreparators.ParseFootprints(footprintFile, (string msg) => { Console.WriteLine(msg); });
            // Group data
            List<FootprintDatapoint.FootPrintEntry> orderedEntries = plotGroups.Concat(plotData).ToList();
            Dictionary<FootprintDatapoint.FootPrintEntry, int> dataIndeces = new Dictionary<FootprintDatapoint.FootPrintEntry, int>();
            int currentIndex = 0;
            foreach (var index in orderedEntries)
                dataIndeces[index] = ++currentIndex;
            // Generate dat file
            string datFilename = BOX_PLOT_PREFIX + ".dat";
            using (StreamWriter sw = new StreamWriter(Path.Combine(outputDir, datFilename)))
            {
                sw.WriteLine(IOConstants.GNU_PLOT_COMMENT_LINE + string.Join(" ", orderedEntries.Select(e => e.ToString())));
                foreach (var footprint in datapoints)
                    sw.WriteLine(string.Join(IOConstants.GNU_PLOT_VALUE_SPLIT.ToString(), orderedEntries.Select(e =>
                    {
                        object value = footprint[e];
                        if (value is double)
                            return ((double)value).ToString(IOConstants.FORMATTER);
                        else if (value is int)
                            return ((int)value).ToString(IOConstants.FORMATTER);
                        else
                            return value.ToString();
                    })));
            }
            // Generate plot script
            string plotScript = BOX_PLOT_PREFIX + ".gp";
            using (StreamWriter sw = new StreamWriter(Path.Combine(outputDir, plotScript), false))
            {
                sw.WriteLine("reset");
                sw.WriteLine("# Output definition");
                sw.WriteLine("set terminal pdfcairo enhanced size 7, 3 font \"Consolas, 12\"");
                sw.WriteLine("set output \"" + BOX_PLOT_PREFIX + ".pdf\"");
                sw.WriteLine("set lmargin 13");
                sw.WriteLine("set rmargin 13");
                sw.WriteLine("# Parameters");
                sw.WriteLine("set grid");
                sw.WriteLine("unset key");
                sw.WriteLine("set pointsize 0.5");
                sw.WriteLine("set style data boxplot");
                string lineColor = "#000000";
                string lineWidth = "1.2";
                // Make one diagram per group and service unit ident
                foreach (var dataType in plotData)
                {
                    foreach (var group in plotGroups)
                    {
                        sw.WriteLine("set title \"" + group.ToString() + " / " + dataType.ToString() + "\"");
                        sw.WriteLine("set xlabel \"" + group.ToString() + "-values\"");
                        sw.WriteLine("set ylabel \"" + dataType.ToString() + "\"");
                        sw.WriteLine("plot \"" + datFilename + "\" using (1.0):" + dataIndeces[dataType] + ":(0):" + dataIndeces[group] + " lc \"" + lineColor + "\" lw " + lineWidth);
                    }
                }
                sw.WriteLine("reset");
                sw.WriteLine("exit");
            }
            // Generate command script
            string commandScript = BOX_PLOT_PREFIX + ".cmd";
            using (StreamWriter sw = new StreamWriter(Path.Combine(outputDir, commandScript), false))
                sw.WriteLine("gnuplot " + plotScript);
            // Execute command
            DataProcessor.ExecuteScript(Path.Combine(outputDir, commandScript), (string msg) => { Console.WriteLine(msg); });
        }
    }
}
