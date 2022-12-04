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
            var fileName = Path.GetFileName(resourceFile);

            // Define all base locations to check
            var locations = new List<string>();
            // Add instance location
            var instanceFolder = Path.GetDirectoryName(instancePath);
            if (!string.IsNullOrWhiteSpace(instanceFolder))
                locations.Add(instanceFolder);
            // Add all paths from current directory up to root
            var currentPath = Directory.GetCurrentDirectory();
            while (currentPath != null)
            {
                locations.Add(currentPath);
                currentPath = Path.GetDirectoryName(currentPath);
            }

            // Enhance by typical subdirectories for all locations
            var baseDirs = locations.ToArray();
            foreach (var subDir in IOConstants.DEFAULT_RESOURCE_SUB_DIRS)
                locations.AddRange(baseDirs.Select(bd => Path.Combine(bd, subDir)));

            // Check all locations
            foreach (var location in locations)
            {
                var fullPath = Path.Combine(location, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            // If we get here, no file was found
            throw new ArgumentException("Cannot find the resource file:" + resourceFile + Environment.NewLine +
                                        "Make sure that the file is available in the instance dir, working dir or a resources sub-dir");
        }
    }
}
