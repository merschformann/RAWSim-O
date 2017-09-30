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
    /// EXposes functionality for calculating correlations between output measures.
    /// </summary>
    public class CorrelationProcessor
    {
        /// <summary>
        /// Writes a correlation matrix of the given measures to the given file.
        /// </summary>
        /// <param name="footprintFile">The file containing the data.</param>
        /// <param name="outputFile">The file to write to.</param>
        /// <param name="measures">The measures to calculate correlations for.</param>
        public static void WriteCorrelationMatrix(string footprintFile, string outputFile, IEnumerable<FootprintDatapoint.FootPrintEntry> measures)
        {
            // Get footprint lines
            Console.Write("Reading data ... ");
            List<string> footprintLines = File.ReadAllLines(footprintFile).Skip(1).ToList(); // Skip the header
            Console.WriteLine(footprintLines.Count.ToString(IOConstants.FORMATTER) + " lines read!");
            // Convert to footprint datapoints
            Console.Write("Converting to footprints ... ");
            List<FootprintDatapoint> footprints = footprintLines.Select(l => new FootprintDatapoint(l)).ToList();
            Console.WriteLine("Done!");
            // Calculate correlations
            Console.Write("Calculating correlations for " + measures.Count().ToString(IOConstants.FORMATTER) + " measures ... ");
            MultiKeyDictionary<FootprintDatapoint.FootPrintEntry, FootprintDatapoint.FootPrintEntry, double> correlations = Cor(footprints, measures);
            Dictionary<FootprintDatapoint.FootPrintEntry, double> means = Mean(footprints, measures);
            Dictionary<FootprintDatapoint.FootPrintEntry, double> stddev = StdDev(footprints, measures);
            Console.WriteLine("Done!");
            // Write to file
            Console.Write("Saving to file " + outputFile + " ... ");
            string sep = ";";
            using (StreamWriter sw = new StreamWriter(outputFile))
            {
                sw.WriteLine(sep + string.Join(sep, measures.Select(m => m.ToString())) + sep + "Mean" + sep + "StdDev");
                foreach (var measure1 in measures)
                    sw.WriteLine(
                        measure1.ToString() + sep +
                        string.Join(sep, measures.Select(measure2 => correlations[measure1, measure2].ToString(IOConstants.FORMATTER))) + sep +
                        means[measure1].ToString(IOConstants.FORMATTER) + sep +
                        stddev[measure1].ToString(IOConstants.FORMATTER));
            }
            Console.WriteLine("Done!");
        }
        /// <summary>
        /// Calculates the correlation matrix for the given measures in the given data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="measures">The measures.</param>
        /// <returns>The correlation matrix as a two key dictionary.</returns>
        public static MultiKeyDictionary<FootprintDatapoint.FootPrintEntry, FootprintDatapoint.FootPrintEntry, double> Cor(IEnumerable<FootprintDatapoint> data, IEnumerable<FootprintDatapoint.FootPrintEntry> measures)
        {
            // Init
            MultiKeyDictionary<FootprintDatapoint.FootPrintEntry, FootprintDatapoint.FootPrintEntry, double> results = new MultiKeyDictionary<FootprintDatapoint.FootPrintEntry, FootprintDatapoint.FootPrintEntry, double>();
            // Prepare means
            Dictionary<FootprintDatapoint.FootPrintEntry, double> means = measures.ToDictionary(m => m, m => data.Average(i => (double)i[m])); // Expecting double measures or at least int ones
            // Calculate correlation
            foreach (var measure1 in measures)
                foreach (var measure2 in measures)
                    results[measure1, measure2] =
                        // Covariance
                        data.Sum(i => ((double)i[measure1] - means[measure1]) * ((double)i[measure2] - means[measure2]))
                        // Divided by
                        /
                        // Standard deviation of first
                        (Math.Sqrt(data.Sum(i => Math.Pow((double)i[measure1] - means[measure1], 2)))
                        // Times
                        *
                        // Standard deviation of second
                        Math.Sqrt(data.Sum(i => Math.Pow((double)i[measure2] - means[measure2], 2))));
            // Return it
            return results;
        }
        /// <summary>
        /// Calculates the means for all given measures.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="measures">The measures.</param>
        /// <returns>All means.</returns>
        public static Dictionary<FootprintDatapoint.FootPrintEntry, double> Mean(IEnumerable<FootprintDatapoint> data, IEnumerable<FootprintDatapoint.FootPrintEntry> measures)
        {
            return measures.ToDictionary(k => k, v => data.Average(d => (double)d[v]));
        }
        /// <summary>
        /// Calculates the standard deviation for all given measures.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="measures">The measures.</param>
        /// <returns>All standard deviation values.</returns>
        public static Dictionary<FootprintDatapoint.FootPrintEntry, double> StdDev(IEnumerable<FootprintDatapoint> data, IEnumerable<FootprintDatapoint.FootPrintEntry> measures)
        {
            // Init and get means
            Dictionary<FootprintDatapoint.FootPrintEntry, double> stddev = new Dictionary<FootprintDatapoint.FootPrintEntry, double>();
            Dictionary<FootprintDatapoint.FootPrintEntry, double> means = Mean(data, measures);
            // Calculate std dev
            foreach (var measure in measures)
                stddev[measure] = Math.Sqrt(data.Sum(d => Math.Pow(((double)d[measure]) - means[measure], 2)) / data.Count());
            // Return it
            return stddev;
        }
    }
}
