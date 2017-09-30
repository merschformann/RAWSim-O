using RAWSimO.Core;
using RAWSimO.Core.Control;
using RAWSimO.Core.IO;
using RAWSimO.Core.Randomization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Playground
{
    class JenkinsHandler
    {
        /// <summary>
        /// The list of CLI arguments with a corresponding short explanation.
        /// </summary>
        public static readonly Tuple<string, string>[] CliArgs = new Tuple<string, string>[] {
            new Tuple<string,string>("Instance","The path to the instance file"),
            new Tuple<string,string>("Setting","The path to the setting configuration file"),
            new Tuple<string,string>("ControlConfig","The path to the controller configuration file"),
            new Tuple<string,string>("StatisticsDir","The path to the directory into which the results are written"),
            new Tuple<string,string>("Seed","The seed to pass to the simulator"),
            new Tuple<string,string>("Build#","The number of the build"),
        };

        public static void HandleJenkinsCall(string[] args)
        {
            // On invalid arguments show info
            if (args.Length != CliArgs.Length)
            {
                Console.WriteLine("Usage: RAWSimO.CLI.exe " + string.Join(" ", CliArgs.Select(a => "<" + a.Item1 + ">")));
                Console.WriteLine("Parameters:");
                foreach (var item in CliArgs)
                    Console.WriteLine(item.Item1 + ": " + item.Item2);
                Console.WriteLine("Actual call's arguments were: " + string.Join(" ", args));
                return;
            }

            // Say hello
            Console.WriteLine("<<< Welcome to the RAWSimO Jenkins Handler >>>");

            // Echo the arguments passed
            Console.WriteLine("Starting RAWSimO wrapper with the following arguments:");
            for (int i = 0; i < CliArgs.Length; i++)
                Console.WriteLine(CliArgs[i].Item1 + ": " + args[i]);

            // Setup instance
            Console.Write("Initializing ... ");
            int seed = int.Parse(args[4]);
            string buildNumber = args[5];
            Action<string> logAction = (string message) => { Console.WriteLine(message); };
            Instance instance = InstanceIO.ReadInstance(args[0], args[1], args[2], logAction: logAction);
            instance.SettingConfig.LogAction = logAction;
            instance.SettingConfig.Seed = seed;
            instance.SettingConfig.StatisticsDirectory = Path.Combine(args[3], instance.Name + "-" + instance.SettingConfig.Name + "-" + instance.ControllerConfig.Name + "-" + instance.SettingConfig.Seed.ToString());
            instance.Randomizer = new RandomizerSimple(seed);
            Console.WriteLine("Done!");
            // Deus ex machina
            Console.WriteLine("Executing ... ");
            DateTime before = DateTime.Now;
            SimulationExecutor.Execute(instance);
            TimeSpan executionTime = DateTime.Now - before;
            Console.WriteLine("Simulation finished.");
            // Write short statistics to output
            instance.PrintStatistics((string s) => Console.WriteLine(s));
            // Log the evaluation statistics
            AppendStatLine(instance, seed, buildNumber, executionTime);
            // Finished
            Console.WriteLine(".Fin. - SUCCESS");
        }

        static void AppendStatLine(Instance instance, int seed, string buildNumber, TimeSpan executionTime)
        {
            // Basic params
            string statFilePrefix = "stats";
            string statFileEnding = ".csv";
            string delimiter = ";";
            string plotDelimiter = ",";
            // Init file and write head, if not existing
            if (!File.Exists(statFilePrefix + statFileEnding))
                using (StreamWriter sw = new StreamWriter(statFilePrefix + statFileEnding))
                    sw.WriteLine(
                        "TimeStamp" + delimiter +
                        "TimeSpan" + delimiter +
                        "Instance" + delimiter +
                        "Setting" + delimiter +
                        "Config" + delimiter +
                        "Seed" + delimiter +
                        "BuildNumber" + delimiter +
                        "StatOverallBundlesHandled" + delimiter +
                        "StatOverallItemsHandled" + delimiter +
                        "StatOverallOrdersHandled" + delimiter +
                        "StatOverallItemsOrdered" + delimiter +
                        "StatOverallCollisions" + delimiter +
                        "StatOverallDistanceTraveled"
                        );
            // Write evaluation results
            using (StreamWriter sw = new StreamWriter(statFilePrefix + statFileEnding, true))
                sw.WriteLine(
                    DateTime.Now.ToString(IOConstants.FORMATTER) + delimiter +
                    executionTime.TotalSeconds.ToString(IOConstants.FORMATTER) + delimiter +
                    instance.Name + delimiter +
                    instance.ControllerConfig.Name + delimiter +
                    instance.SettingConfig.Name + delimiter +
                    seed.ToString(IOConstants.FORMATTER) + delimiter +
                    buildNumber + delimiter +
                    instance.StatOverallBundlesHandled.ToString(IOConstants.FORMATTER) + delimiter +
                    instance.StatOverallItemsHandled.ToString(IOConstants.FORMATTER) + delimiter +
                    instance.StatOverallOrdersHandled.ToString(IOConstants.FORMATTER) + delimiter +
                    instance.StatOverallItemsOrdered.ToString(IOConstants.FORMATTER) + delimiter +
                    instance.StatOverallCollisions.ToString(IOConstants.FORMATTER) + delimiter +
                    instance.StatOverallDistanceTraveled.ToString(IOConstants.FORMATTER)
                    );
            // Init instance specific stat-file for plotting
            string plotStatFile = statFilePrefix + "-" + instance.Name + "-" + instance.ControllerConfig.Name + statFileEnding;
            using (StreamWriter sw = new StreamWriter(plotStatFile))
            {
                sw.WriteLine(
                    "TimeSpan" + plotDelimiter +
                    "BundlesHandled" + plotDelimiter +
                    "OrdersHandled" + plotDelimiter +
                    "Collisions" + plotDelimiter +
                    "DistanceTraveled" + plotDelimiter +
                    "OrderThroughputTime"
                    );
                sw.WriteLine(
                    executionTime.TotalSeconds.ToString(IOConstants.FORMATTER) + plotDelimiter +
                    instance.StatOverallBundlesHandled.ToString(IOConstants.FORMATTER) + plotDelimiter +
                    instance.StatOverallOrdersHandled.ToString(IOConstants.FORMATTER) + plotDelimiter +
                    instance.StatOverallCollisions.ToString(IOConstants.FORMATTER) + plotDelimiter +
                    instance.StatOverallDistanceTraveled.ToString(IOConstants.FORMATTER) + plotDelimiter +
                    instance.StatOrderThroughputTimeAvg.ToString(IOConstants.FORMATTER)
                    );
            }
            // ---> Init aggregated path planning plot file
            if (instance.Name.Contains("jenPP") && instance.SettingConfig.Name.Contains("jenPP") && instance.ControllerConfig.Name.Contains("jenPP"))
            {
                // --> Add aggregated path planning order stat file
                string aggregatedPathPlanningOrders = "stat-jenPP-aggregated-orders.csv";
                // Parse already existing file
                List<string> lines = new List<string>();
                if (File.Exists(aggregatedPathPlanningOrders))
                {
                    using (StreamReader sr = new StreamReader(aggregatedPathPlanningOrders))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                    }
                }
                // Add values of this run
                using (StreamWriter sw = new StreamWriter(aggregatedPathPlanningOrders, false))
                {
                    if (lines.Count > 0)
                    {
                        sw.WriteLine(lines[0] + plotDelimiter + instance.Name + instance.ControllerConfig.Name);
                        sw.WriteLine(lines[1] + plotDelimiter + instance.StatOverallOrdersHandled.ToString(IOConstants.FORMATTER));
                    }
                    else
                    {
                        sw.WriteLine(instance.Name + instance.ControllerConfig.Name);
                        sw.WriteLine(instance.StatOverallOrdersHandled.ToString(IOConstants.FORMATTER));
                    }
                }
                // --> Add aggregated path planning bundle stat file
                string aggregatedPathPlanningBundles = "stat-jenPP-aggregated-bundles.csv";
                // Parse already existing file
                lines = new List<string>();
                if (File.Exists(aggregatedPathPlanningBundles))
                {
                    using (StreamReader sr = new StreamReader(aggregatedPathPlanningBundles))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                    }
                }
                // Add values of this run
                using (StreamWriter sw = new StreamWriter(aggregatedPathPlanningBundles, false))
                {
                    if (lines.Count > 0)
                    {
                        sw.WriteLine(lines[0] + plotDelimiter + instance.Name + instance.ControllerConfig.Name);
                        sw.WriteLine(lines[1] + plotDelimiter + instance.StatOverallBundlesHandled.ToString(IOConstants.FORMATTER));
                    }
                    else
                    {
                        sw.WriteLine(instance.Name + instance.ControllerConfig.Name);
                        sw.WriteLine(instance.StatOverallBundlesHandled.ToString(IOConstants.FORMATTER));
                    }
                }
                // --> Add aggregated path planning order stat file
                string aggregatedPathPlanningDistanceTraveled = "stat-jenPP-aggregated-distance.csv";
                // Parse already existing file
                lines = new List<string>();
                if (File.Exists(aggregatedPathPlanningDistanceTraveled))
                {
                    using (StreamReader sr = new StreamReader(aggregatedPathPlanningDistanceTraveled))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                    }
                }
                // Add values of this run
                using (StreamWriter sw = new StreamWriter(aggregatedPathPlanningDistanceTraveled, false))
                {
                    if (lines.Count > 0)
                    {
                        sw.WriteLine(lines[0] + plotDelimiter + instance.Name + instance.ControllerConfig.Name);
                        sw.WriteLine(lines[1] + plotDelimiter + instance.StatOverallDistanceTraveled.ToString(IOConstants.FORMATTER));
                    }
                    else
                    {
                        sw.WriteLine(instance.Name + instance.ControllerConfig.Name);
                        sw.WriteLine(instance.StatOverallDistanceTraveled.ToString(IOConstants.FORMATTER));
                    }
                }
                // --> Add aggregated path planning time stat file
                string aggregatedPathPlanningTime = "stat-jenPP-aggregated-time.csv";
                // Parse already existing file
                lines = new List<string>();
                if (File.Exists(aggregatedPathPlanningTime))
                {
                    using (StreamReader sr = new StreamReader(aggregatedPathPlanningTime))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                    }
                }
                // Add values of this run
                using (StreamWriter sw = new StreamWriter(aggregatedPathPlanningTime, false))
                {
                    if (lines.Count > 0)
                    {
                        sw.WriteLine(lines[0] + plotDelimiter + instance.Name + instance.ControllerConfig.Name);
                        sw.WriteLine(lines[1] + plotDelimiter + executionTime.TotalSeconds.ToString(IOConstants.FORMATTER));
                    }
                    else
                    {
                        sw.WriteLine(instance.Name + instance.ControllerConfig.Name);
                        sw.WriteLine(executionTime.TotalSeconds.ToString(IOConstants.FORMATTER));
                    }
                }
            }
            // ---> Init aggregated jenkins plot file
            if (instance.Name.Contains("jenkins") && instance.SettingConfig.Name.Contains("jenkins") && instance.ControllerConfig.Name.Contains("jenkins"))
            {
                // --> Add aggregated throughput stat file
                string aggregatedThroughputStatFile = "stat-jenkins-aggregated-throughput.csv";
                // Parse already existing file
                List<string> lines = new List<string>();
                if (File.Exists(aggregatedThroughputStatFile))
                {
                    using (StreamReader sr = new StreamReader(aggregatedThroughputStatFile))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                    }
                }
                // Add values of this run
                using (StreamWriter sw = new StreamWriter(aggregatedThroughputStatFile, false))
                {
                    if (lines.Count > 0)
                    {
                        sw.WriteLine(lines[0] + plotDelimiter + instance.Name + instance.ControllerConfig.Name);
                        sw.WriteLine(lines[1] + plotDelimiter + instance.StatOrderThroughputTimeAvg.ToString(IOConstants.FORMATTER));
                    }
                    else
                    {
                        sw.WriteLine(instance.Name + instance.ControllerConfig.Name);
                        sw.WriteLine(instance.StatOrderThroughputTimeAvg.ToString(IOConstants.FORMATTER));
                    }
                }
                // --> Add aggregated orders handled stat file
                string aggregatedOrdersHandledStatFile = "stat-jenkins-aggregated-orders.csv";
                // Parse already existing file
                lines = new List<string>();
                if (File.Exists(aggregatedOrdersHandledStatFile))
                {
                    using (StreamReader sr = new StreamReader(aggregatedOrdersHandledStatFile))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                    }
                }
                // Add values of this run
                using (StreamWriter sw = new StreamWriter(aggregatedOrdersHandledStatFile, false))
                {
                    if (lines.Count > 0)
                    {
                        sw.WriteLine(lines[0] + plotDelimiter + instance.Name + instance.ControllerConfig.Name);
                        sw.WriteLine(lines[1] + plotDelimiter + instance.StatOverallOrdersHandled.ToString());
                    }
                    else
                    {
                        sw.WriteLine(instance.Name + instance.ControllerConfig.Name);
                        sw.WriteLine(instance.StatOverallOrdersHandled.ToString());
                    }
                }
                // --> Add aggregated bundles handled stat file
                string aggregatedBundlesHandledStatFile = "stat-jenkins-aggregated-bundles.csv";
                // Parse already existing file
                lines = new List<string>();
                if (File.Exists(aggregatedBundlesHandledStatFile))
                {
                    using (StreamReader sr = new StreamReader(aggregatedBundlesHandledStatFile))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                    }
                }
                // Add values of this run
                using (StreamWriter sw = new StreamWriter(aggregatedBundlesHandledStatFile, false))
                {
                    if (lines.Count > 0)
                    {
                        sw.WriteLine(lines[0] + plotDelimiter + instance.Name + instance.ControllerConfig.Name);
                        sw.WriteLine(lines[1] + plotDelimiter + instance.StatOverallBundlesHandled.ToString());
                    }
                    else
                    {
                        sw.WriteLine(instance.Name + instance.ControllerConfig.Name);
                        sw.WriteLine(instance.StatOverallBundlesHandled.ToString());
                    }
                }
                // --> Add aggregated time stat file
                string aggregatedTimeStatFile = "stat-jenkins-aggregated-time.csv";
                // Parse already existing file
                lines = new List<string>();
                if (File.Exists(aggregatedTimeStatFile))
                {
                    using (StreamReader sr = new StreamReader(aggregatedTimeStatFile))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                    }
                }
                // Add values of this run
                using (StreamWriter sw = new StreamWriter(aggregatedTimeStatFile, false))
                {
                    if (lines.Count > 0)
                    {
                        sw.WriteLine(lines[0] + plotDelimiter + instance.Name + instance.ControllerConfig.Name);
                        sw.WriteLine(lines[1] + plotDelimiter + executionTime.TotalSeconds.ToString());
                    }
                    else
                    {
                        sw.WriteLine(instance.Name + instance.ControllerConfig.Name);
                        sw.WriteLine(executionTime.TotalSeconds.ToString());
                    }
                }
            }
        }
    }
}
