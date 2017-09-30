using RAWSimO.Core;
using RAWSimO.Core.Configurations;
using RAWSimO.Core.Generator;
using RAWSimO.Core.IO;
using RAWSimO.Core.Randomization;
using RAWSimO.Playground.Generators;
using RAWSimO.Playground.Tests;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RAWSimO.Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            // If arguments given in the right length handle the jenkins call
            if (args.Length == JenkinsHandler.CliArgs.Length)
            {
                JenkinsHandler.HandleJenkinsCall(args);
                return;
            }
            // Choose option
            Console.WriteLine(">>> Choose option: ");
            Console.WriteLine("1: ExecuteInstance");
            Console.WriteLine("2: SendResourcesToCluster");
            Console.WriteLine("3: FetchResultsFromCluster");
            Console.WriteLine("4: FetchOutputFilesFromCluster");
            Console.WriteLine("5: GenerateMaTiInstances");
            Console.WriteLine("6: GenerateMuControlConfigs");
            Console.WriteLine("7: GenerateMuSettingConfigs");
            Console.WriteLine("8: ExecuteDirectory");
            Console.WriteLine("0: Experimental");
            char optionKey = Console.ReadKey().KeyChar; Console.WriteLine();
            switch (optionKey)
            {

                case '1': { ExecuteInstance(); } break;
                case '2':
                    {
                        /* ClusterHelper.SendBinDirToCluster(); TODO skip bin dir for now - use hg clone repo instead */
                        ClusterHelper.SendScriptDirToCluster();
                        ClusterHelper.SendInstanceDirToCluster();
                        ClusterHelper.SendResourceDirToCluster();
                    }
                    break;
                case '3': { ClusterHelper.FetchResultsFromCluster(); } break;
                case '4': { ClusterHelper.FetchOutputFilesFromCluster(); } break;
                case '5': { InstanceGenerators.GenerateMaTiInstances(); } break;
                case '6': { ConfigGenerators.GenerateRotterdamControllers(); } break;
                case '7': { SettingGenerator.GenerateRotterdamMark2Set(); } break;
                case '8': { ExecuteInstances(); } break;
                case '0': { Experimental(); } break;
                default:
                    break;
            }
            Console.WriteLine(".Fin.");
            Console.ReadLine();
        }

        /// <summary>
        /// Asks the user for the instance and configuration to execute.
        /// </summary>
        static void ExecuteInstance()
        {
            // Choose base directory
            Console.WriteLine("Choose directory:");
            string[] directories = Directory.EnumerateDirectories(Path.Combine("..", "..", "..", "..", "Material", "Instances")).ToArray();
            for (int i = 0; i < directories.Length; i++)
            {
                Console.WriteLine(i + ": " + Path.GetFileName(directories[i]));
            }
            int directoryID = 0;
            try
            {
                // Get the ID of the instance
                directoryID = int.Parse(Console.ReadLine());
            }
            catch (FormatException)
            {
                Console.WriteLine("Error: Could not find a directory with that ID!");
                return;
            }
            // Choose instance
            string[] instances = Directory.EnumerateFiles(directories[directoryID], "*.xinst").Concat(Directory.EnumerateFiles(directories[directoryID], "*.xlayo")).ToArray();
            Console.WriteLine("Choose instance:");
            for (int i = 0; i < instances.Length; i++)
            {
                Console.WriteLine(i + ": " + Path.GetFileName(instances[i]));
            }
            int instanceID = 0;
            try
            {
                // Get the ID of the instance
                instanceID = int.Parse(Console.ReadLine());
            }
            catch (FormatException)
            {
                Console.WriteLine("Error: Could not find instance with that ID!");
                return;
            }
            // Choose configuration
            string[] settings = Directory.EnumerateFiles(directories[directoryID], "*.xsett").ToArray();
            Console.WriteLine("Choose setting:");
            for (int i = 0; i < settings.Length; i++)
            {
                Console.WriteLine(i + ": " + Path.GetFileName(settings[i]));
            }
            int settingID = 0;
            try
            {
                // Get the ID of the config
                settingID = int.Parse(Console.ReadLine());
            }
            catch (FormatException)
            {
                Console.WriteLine("Error: Could not find configuration with that ID!");
                return;
            }
            // Choose configuration
            string[] configurations = Directory.EnumerateFiles(directories[directoryID], "*.xconf").ToArray();
            Console.WriteLine("Choose configuration:");
            for (int i = 0; i < configurations.Length; i++)
            {
                Console.WriteLine(i + ": " + Path.GetFileName(configurations[i]));
            }
            int configID = 0;
            try
            {
                // Get the ID of the config
                configID = int.Parse(Console.ReadLine());
            }
            catch (FormatException)
            {
                Console.WriteLine("Error: Could not find configuration with that ID!");
                return;
            }
            // Choose seed
            Console.WriteLine("Choose seed:");
            int seed = 0;
            try
            {
                // Get the seed
                seed = int.Parse(Console.ReadLine());
            }
            catch (FormatException)
            {
                Console.WriteLine("Error: Could not parse the seed!");
                return;
            }
            // Parse the instance
            Console.WriteLine("Calling wrapped RAWSimO.CLI ...");
            string[] cliArgs = { instances[instanceID], settings[settingID], configurations[configID], ".", seed.ToString() };
            RAWSimO.CLI.Program.Main(cliArgs);
            Console.WriteLine("Returned from RAWSimO.CLI !");
        }

        /// <summary>
        /// Asks a user for a directory and a seed-count then executes all combinations that can be obtained.
        /// </summary>
        static void ExecuteInstances()
        {
            // Ask for the directory to execute
            Console.WriteLine("Directory to execute [current]:");
            string directory = Console.ReadLine().Trim();
            if (!Directory.Exists(directory))
                directory = Directory.GetCurrentDirectory();
            // Ask for seed count
            uint seedCount = 1;
            Console.WriteLine("Number of seeds to execute [1]:");
            try { seedCount = uint.Parse(Console.ReadLine()); }
            catch (FormatException) { }
            // Ask for degree of parallelism
            uint paraDeg = 1;
            Console.WriteLine("Degree of parallelism [1]:");
            try { paraDeg = uint.Parse(Console.ReadLine()); }
            catch (FormatException) { }
            // Get all files
            List<string> instances = Directory.EnumerateFiles(directory, "*.xinst").Concat(Directory.EnumerateFiles(directory, "*.xlayo")).ToList();
            List<string> settings = Directory.EnumerateFiles(directory, "*.xsett").ToList();
            List<string> configs = Directory.EnumerateFiles(directory, "*.xconf").ToList();
            List<string> seeds = Enumerable.Range(1, (int)seedCount).Select(s => s.ToString()).ToList();
            var combinations = EnumerationHelpers.CrossProduct(new List<List<string>>() { instances, settings, configs, seeds }).ToList();
            int counter = 0;
            if (paraDeg < 2)
            {
                foreach (var combination in combinations)
                {
                    // Execute the next one
                    Console.WriteLine("######################################");
                    Console.WriteLine("---> Executing combination " + (++counter).ToString() + " / " + combinations.Count());
                    Console.WriteLine("######################################");
                    Console.WriteLine("--> Calling wrapped RAWSimO.CLI ...");
                    string[] cliArgs = { combination[0], combination[1], combination[2], directory, combination[3] };
                    RAWSimO.CLI.Program.Main(cliArgs);
                    Console.WriteLine("--> Returned from RAWSimO.CLI !");
                    Console.WriteLine("######################################");
                }
            }
            else
            {
                // Prepare
                Console.WriteLine("######################################");
                Console.WriteLine("---> Executing " + combinations.Count + " combinations in parallel");
                Console.WriteLine("######################################");
                string programName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RAWSimO.CLI.exe");
                int total = combinations.Count; int completed = 0; int started = 0;
                // Store last lines of all jobs and periodically log them for feedback
                List<string> lastLines = new List<string>();
                Action log = () =>
                {
                    Console.WriteLine("Progress: " + started + "/" + completed + "/" + total + " (started/completed/total)");
                    lock (lastLines)
                        for (int i = 0; i < lastLines.Count; i++)
                            Console.WriteLine(i + ": " + lastLines[i]);
                };
                Timer logTimer = new Timer(new TimerCallback((object unused) => { log(); }), null, 2000, 5000);
                // Execute jobs in parallel (limited by the given degree of parallelism)
                Parallel.ForEach(combinations, new ParallelOptions() { MaxDegreeOfParallelism = (int)paraDeg }, (List<string> combination) =>
                {
                    // Prepare sub-process
                    string arguments = combination[0] + " " + combination[1] + " " + combination[2] + " " + directory + " " + combination[3];
                    ProcessStartInfo startInfo = new ProcessStartInfo(programName, arguments) { };
                    startInfo.UseShellExecute = false;
                    startInfo.RedirectStandardOutput = true;
                    int index = Interlocked.Increment(ref started) - 1;
                    lock (lastLines)
                        lastLines.Add("<null>");
                    Process process = new Process() { StartInfo = startInfo };
                    process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                            lastLines[index] = e.Data;
                    };
                    // Start the job
                    process.Start();
                    process.BeginOutputReadLine();
                    // Wait for the job to finish
                    process.WaitForExit();
                    Interlocked.Increment(ref completed);
                });
                // Finish logging
                logTimer.Change(Timeout.Infinite, Timeout.Infinite);
                log();
            }
        }

        /// <summary>
        /// Used to test stuff. Change the interiors of the method as often as you like. Do not put anything meaningful in here.
        /// </summary>
        public static void Experimental()
        {
            // Insert a test from RAWSimO.Playground.Tests or similar calls here
            //SolverTests.TestSolverBasic();
            //MDPLPConverter.RoundModels(Directory.GetCurrentDirectory());
            //ConfigGenerators.GenerateLenaKSet();
            //ConfigGenerators.GenerateRepositioningSet3();
            // Choose option
            Console.WriteLine(">>> Choose option: ");
            Console.WriteLine("1: Generate phase 1 configs");
            Console.WriteLine("2: Generate phase 2 settings");
            char optionKey = Console.ReadKey().KeyChar; Console.WriteLine();
            switch (optionKey)
            {
                case '1': { ConfigGenerators.GenerateRotterdamControllers(); } break;
                case '2': { ConfigGenerators.GenerateRotterdamPhase2Settings(); } break;
                default: break;
            }
        }
    }
}
