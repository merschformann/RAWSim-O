using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.IO
{
    /// <summary>
    /// Exposes helping methods for I/O operations.
    /// </summary>
    public class IOHelper
    {
        /// <summary>
        /// Finds the given wordlist by looking up default directories for the wordlists.
        /// </summary>
        /// <param name="resourceFile">The wordfile to look for.</param>
        /// <param name="instancePath">The path of the instance (one of the default storage locations).</param>
        /// <returns>The successfully retrieved wordfile. If no file was found an Exception is thrown.</returns>
        public static string FindResourceFile(string resourceFile, string instancePath)
        {
            // If it exists, do not search for it
            if (File.Exists(resourceFile))
                return resourceFile;
            // Adapt to environment
            resourceFile = resourceFile.Replace('\\', Path.DirectorySeparatorChar);
            resourceFile = resourceFile.Replace('/', Path.DirectorySeparatorChar);
            // Get name
            string fileName = Path.GetFileName(resourceFile);
            string resultResourceFile = "";
            // Use complete path as default
            resultResourceFile = resourceFile;
            // Try the working directory
            if (File.Exists(fileName))
                resultResourceFile = fileName;
            // Try the default directories
            foreach (var dir in IOConstants.DEFAULT_RESOURCE_DIRS)
                TryDirectory(fileName, dir, ref resultResourceFile);
            // Try the directory of the instance
            string instancePathFile = Path.Combine(Path.GetDirectoryName(instancePath), fileName);
            if (!string.IsNullOrWhiteSpace(instancePath) && File.Exists(instancePathFile))
                resultResourceFile = instancePathFile;
            // Return it
            if (File.Exists(resultResourceFile))
                return resultResourceFile;
            else
                throw new ArgumentException("Cannot find the resource file: " + resourceFile + "(Tried: " + string.Join(",", IOConstants.DEFAULT_RESOURCE_DIRS) + ")");
        }
        /// <summary>
        /// Checks whether the file exists.
        /// </summary>
        /// <param name="fileName">The name of the file to look for.</param>
        /// <param name="dir">The directory to try.</param>
        /// <param name="resultPath">This is updated to the path if the path existed.</param>
        public static void TryDirectory(string fileName, string dir, ref string resultPath)
        {
            string path = Path.Combine(dir, fileName);
            if (File.Exists(path))
                resultPath = path;
        }
    }
}
