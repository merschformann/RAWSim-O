using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using RAWSimO.Core.Control;
using RAWSimO.Core.IO;
using RAWSimO.Core.Randomization;
using Xunit;

namespace RAWSimO.Core.Tests
{
    public class GoldenFileTests
    {
        /// <summary>
        /// Read and prepare a simulation instance to run.
        /// </summary>
        /// <param name="layout">Path to the layout file.</param>
        /// <param name="setting">Path to the setting file.</param>
        /// <param name="control">Path to the control file.</param>
        /// <param name="seed">The randomizer seed.</param>
        /// <returns>The instance ready to be executed.</returns>
        private static Instance ReadInstance(string layout, string setting, string control, int seed)
        {
            Action<string> logAction = Console.WriteLine;
            var instance = InstanceIO.ReadInstance(layout, setting, control, logAction: logAction);
            instance.SettingConfig.LogAction = logAction;
            instance.SettingConfig.Seed = seed;
            instance.Randomizer = new RandomizerSimple(seed);
            return instance;
        }

        /// <summary>
        /// Executes full simulation runs for pre-defined instance files.
        /// </summary>
        [Fact]
        public void GoldenFiles()
        {
            // Define tests (each test is the base filename of the test files - .xlayo, .xsett, .xconf, .golden)
            var tests = new List<string>
            {
                "BasicInstance"
            };

            // Run all tests
            foreach (var test in tests)
            {
                // Read instance and run simulation
                var instance = ReadInstance($"Resources/{test}.xlayo", $"Resources/{test}.xsett", $"Resources/{test}.xconf", 0);
                SimulationExecutor.Execute(instance);
                // Analyze the statistics
                //  - simulation is deterministic, thus, stats should not change
                //  - except for some algorithm runtime limit related reasons (shouldn't apply here)
                // Unfortunately, there seems to be no good way to automatically update golden files / fixtures in .Net like in other languages :(
                //  - feel free to recommend a way, until then the fixtures need to be manually defined
                var lines = new List<string>();
                instance.PrintStatistics(s => lines.AddRange(s.Split(Environment.NewLine)));
                var stats = string.Join(Environment.NewLine, lines.Where(s => s.StartsWith("StatOverall")));
                var golden = File.ReadAllText($"Resources/{test}.golden").Trim();
                Assert.Equal(golden, stats);
            }
        }
    }
}
