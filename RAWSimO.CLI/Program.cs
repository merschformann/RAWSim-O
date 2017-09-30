using RAWSimO.Core;
using RAWSimO.Core.Configurations;
using RAWSimO.Core.Generator;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.IO;
using RAWSimO.Core.Randomization;
using RAWSimO.Core.Control;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Globalization;

namespace RAWSimO.CLI
{
    public class Program
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
        };

        /// <summary>
        /// A buffer used to store messages before a disk writer is available.
        /// </summary>
        private static List<string> _logBuffer = new List<string>();
        /// <summary>
        /// A writer used to log messages to disk.
        /// </summary>
        private static StreamWriter _logWriter;
        /// <summary>
        /// Logs a message line to a file in the output-directory and the console.
        /// </summary>
        /// <param name="line">The line to log.</param>
        private static void LogLine(string line)
        {
            // Synchronize - just in case we want to asynchronously log
            lock (_logBuffer)
            {
                // Check whether we need to buffer the line, because no disk writer is present
                if (_logWriter == null) { _logBuffer.Add(line); }
                else
                {
                    // We have a disk logger - write out all remaining buffered lines
                    if (_logBuffer.Count > 0)
                    {
                        foreach (var bufferedLine in _logBuffer) { _logWriter.WriteLine(bufferedLine); }
                        _logBuffer.Clear();
                    }
                    // Write the actual line
                    _logWriter.WriteLine(line);
                }
                // Additionally output the line on console
                Console.WriteLine(line);
            }
        }
        /// <summary>
        /// The file to which all finished tags are written.
        /// </summary>
        public const string TAG_FILE = "tags.csv";
        /// <summary>
        /// Marks an execution as finished for a given tag.
        /// </summary>
        /// <param name="statisticsPath">The statistics directory to use.</param>
        /// <param name="instance">The instance of the execution.</param>
        /// <param name="setting">The settings.</param>
        /// <param name="config">The config.</param>
        /// <param name="seed">The seed.</param>
        /// <param name="tag">The tag of the run.</param>
        private static void MarkTagFinished(string statisticsPath, string instance, string setting, string config, string seed, string tag)
        {
            // Try to mark tag as finished (keep on trying until success)
            bool success = false; int tries = 0; Random rand = new Random(); string fileName = Path.Combine(statisticsPath, TAG_FILE);
            do
            {
                try
                {
                    // Sleep randomly before attempting a retry
                    if (tries > 0)
                        Thread.Sleep(rand.Next(100, 1000));
                    // Try it
                    tries++; bool fileExists = File.Exists(fileName);
                    using (StreamWriter sw = new StreamWriter(fileName, true))
                    {
                        if (!fileExists)
                            sw.WriteLine(
                                "Tag" + IOConstants.DELIMITER_VALUE +
                                "Instance" + IOConstants.DELIMITER_VALUE +
                                "Setting" + IOConstants.DELIMITER_VALUE +
                                "Config" + IOConstants.DELIMITER_VALUE +
                                "Seed" + IOConstants.DELIMITER_VALUE +
                                "Timestamp");
                        sw.WriteLine(
                            tag + IOConstants.DELIMITER_VALUE +
                            instance + IOConstants.DELIMITER_VALUE +
                            setting + IOConstants.DELIMITER_VALUE +
                            config + IOConstants.DELIMITER_VALUE +
                            seed + IOConstants.DELIMITER_VALUE +
                            DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                    }
                    // If we reach this point, we were successful
                    success = true;
                }
                catch (Exception) { /* Ignore exception that might happen due to parallel execution - just try again */ }
            } while (!success && tries < 50);
            // Log
            LogLine("Marked tag finished after " + tries + " tries!");
        }

        public static void Main(string[] args)
        {
            // On invalid arguments show info
            if (args.Length < CliArgs.Length || args.Length > CliArgs.Length + 1)
            {
                LogLine("Usage: RAWSimO.CLI.exe " + string.Join(" ", CliArgs.Select(a => "<" + a.Item1 + ">")));
                LogLine("Parameters:");
                foreach (var item in CliArgs)
                    LogLine(item.Item1 + ": " + item.Item2);
                LogLine("Actual call's arguments were: " + string.Join(" ", args));
                LogLine("Note: you may add one additional and last argument as a tag");
                return;
            }

            // Say hello
            LogLine("<<< Welcome to the RAWSimO CLI >>>");
            LogLine("The time is: " + DateTime.Now.ToString(IOConstants.FORMATTER));

            // Echo the arguments passed
            LogLine("Starting RAWSimO wrapper with the following arguments:");
            for (int i = 0; i < CliArgs.Length; i++)
                LogLine(CliArgs[i].Item1 + ": " + args[i]);

            // Check for tag argument
            if (args.Length == CliArgs.Length + 1)
                LogLine("Additionally the following tag was passed: " + args[CliArgs.Length]);

            // Catch unhandled exceptions
            if (!AppDomain.CurrentDomain.FriendlyName.EndsWith("vshost.exe"))
            {
                LogLine("Adding handler for unhandled exceptions ...");
                var handler = new UnhandledExceptionHandler()
                {
                    Instance = args[0],
                    SettingConfig = args[1],
                    ControlConfig = args[2],
                    Seed = args[4],
                    LogAction = LogLine,
                    Tag = ((args.Length == CliArgs.Length + 1) ? args[CliArgs.Length] : null)
                };
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(handler.LogUnhandledException);
            }

            // Setup instance
            LogLine("Initializing ... ");
            int seed = int.Parse(args[4]);
            Action<string> logAction = (string message) => { LogLine(message); };
            Instance instance = InstanceIO.ReadInstance(args[0], args[1], args[2], logAction: logAction);
            instance.SettingConfig.LogAction = logAction;
            instance.SettingConfig.Seed = seed;
            if (args.Length == CliArgs.Length + 1)
                instance.Tag = args[CliArgs.Length];
            string statisticsFolder = instance.Name + "-" + instance.SettingConfig.Name + "-" + instance.ControllerConfig.Name + "-" + instance.SettingConfig.Seed.ToString();
            instance.SettingConfig.StatisticsDirectory = Path.Combine(args[3], statisticsFolder);
            LogLine("StatisticsFolder: " + statisticsFolder);
            instance.Randomizer = new RandomizerSimple(seed);
            LogLine("Done!");
            // Setup log to disk
            if (!Directory.Exists(instance.SettingConfig.StatisticsDirectory))
                Directory.CreateDirectory(instance.SettingConfig.StatisticsDirectory);
            _logWriter = new StreamWriter(Path.Combine(instance.SettingConfig.StatisticsDirectory, IOConstants.LOG_FILE), false) { AutoFlush = true };
            // Deus ex machina
            LogLine("Executing ... ");
            SimulationExecutor.Execute(instance);
            LogLine("Simulation finished.");

            // Write short statistics to output
            instance.PrintStatistics((string s) => LogLine(s));
            // Mark tag as finished, if available
            if (instance.Tag != null)
                MarkTagFinished(args[3], args[0], args[1], args[2], args[4], instance.Tag);
            // Finished
            LogLine(".Fin. - SUCCESS");
        }
    }
}
