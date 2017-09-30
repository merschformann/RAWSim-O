using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Toolbox
{
    /// <summary>
    /// Contains some functionality for multiple platform support.
    /// </summary>
    public class PlatformHelpers
    {
        /// <summary>
        /// Determines whether we are executing on a linux platform.
        /// </summary>
        public static bool IsLinux
        {
            get
            {
                int p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }
        /// <summary>
        /// Marks the file executable when executing on a linux machine.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="logLineAction"></param>
        public static void SetExecutableAttribute(string fileName, Action<string> logLineAction)
        {
            // Check for linux first
            if (IsLinux)
            {
                // Execute it
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.FileName = "chmod";
                startInfo.Arguments = "+x " + fileName;
                logLineAction("Marking file executable: " + startInfo.FileName);
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            }
        }
    }
}
