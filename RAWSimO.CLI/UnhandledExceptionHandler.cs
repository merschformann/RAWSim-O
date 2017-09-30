using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.CLI
{
    /// <summary>
    /// USed to write information about not handled exceptions.
    /// </summary>
    class UnhandledExceptionHandler
    {
        /// <summary>
        /// The name of the instance.
        /// </summary>
        public string Instance { get; set; }
        /// <summary>
        /// The name of the setting configuration.
        /// </summary>
        public string SettingConfig { get; set; }
        /// <summary>
        /// The name of the controller configuration.
        /// </summary>
        public string ControlConfig { get; set; }
        /// <summary>
        /// The used seed.
        /// </summary>
        public string Seed { get; set; }
        /// <summary>
        /// The given tag (if available).
        /// </summary>
        public string Tag { get; set; }
        /// <summary>
        /// The log action to use.
        /// </summary>
        public Action<string> LogAction { get; set; }
        /// <summary>
        /// Passes the line to the outer logging action.
        /// </summary>
        /// <param name="line">The line to pass.</param>
        private void LogToOuter(string line) { if (LogAction != null) LogAction(line); }
        /// <summary>
        /// Logs an unhandled exception.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        public void LogUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Init output log
            Exception ex = (Exception)e.ExceptionObject;
            using (StreamWriter sw = new StreamWriter("exception.txt", true))
            {
                Action<string> logLine = (string msg) => { sw.WriteLine(msg); LogToOuter(msg); };
                logLine("-----------------------------------------------------------");
                logLine("!!! Caught an unhandled exception: " + ex.Message + " timestamp: " + DateTime.Now.ToString(CultureInfo.InvariantCulture));
                logLine("-----------------------------------------------------------");
                logLine("Time: " + DateTime.Now.ToString());
                logLine("Instance: " + Instance);
                logLine("Setting: " + SettingConfig);
                logLine("ControlConfig: " + ControlConfig);
                logLine("Seed: " + Seed);
                logLine("Tag: " + ((Tag != null) ? Tag : "<null>"));
                logLine("Stacktrace:");
                logLine(ex.StackTrace);
                logLine("InnerException: ");
                if (ex.InnerException != null)
                {
                    logLine(ex.InnerException.Message);
                    logLine("Stacktrace:");
                    logLine(ex.InnerException.StackTrace);
                }
                else
                {
                    logLine("None");
                }
                logLine("-----------------------------------------------------------");
            }
        }
    }
}
