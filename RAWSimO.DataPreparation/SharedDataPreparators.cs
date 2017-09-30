using RAWSimO.Core.Statistics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.DataPreparation
{
    public class SharedDataPreparators
    {
        /// <summary>
        /// Parses all footprints from a footprint data file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="logLineAction">An optional log action.</param>
        /// <returns>All footprints extracted from the file.</returns>
        public static List<FootprintDatapoint> ParseFootprints(string file, Action<string> logLineAction = null)
        {
            // Init
            List<FootprintDatapoint> datapoints = new List<FootprintDatapoint>();
            // Log
            logLineAction?.Invoke("Parsing data from file " + file + " ...");
            // Read all data points from the file
            int counter = 0;
            using (StreamReader sr = new StreamReader(file))
            {
                // Init line and skip first line
                string line = sr.ReadLine();
                while ((line = sr.ReadLine()) != null)
                {
                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    // Parse the line into a new datapoint
                    datapoints.Add(new FootprintDatapoint(line));
                    // Count
                    counter++;
                }
            }
            logLineAction?.Invoke("Parsed " + counter + " datapoints!");
            // Return it
            return datapoints;
        }
    }
}
