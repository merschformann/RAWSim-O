using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Tests.Resources
{
    /// <summary>
    /// Exposes auxiliary functionality for handling test resources.
    /// </summary>
    public static class ResourceHelper
    {
        /// <summary>
        /// Reads a given embedded resource file into a single string.
        /// </summary>
        /// <param name="file">The namespace and filename of the file to read.</param>
        /// <returns>The file as a single string.</returns>
        private static string ReadTextResourceFile(string file)
        {
            // Read file content
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(file);
            using var reader = new StreamReader(stream ?? throw new InvalidOperationException("Resource file for mini-instance not found!"));
            return reader.ReadToEnd();
        }
    }
}
