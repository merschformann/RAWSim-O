using Atto.LinearWrap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MDPSolve.Helpers
{
    public class MDPLPConverter
    {
        public static void RoundModels(string path)
        {
            int rounding = 8;
            string[] modelFileExtensions = { "*.txt", "*.txt.gz" };
            Console.WriteLine("Searching for files using the filters: " + string.Join(",", modelFileExtensions) + " ... ");
            foreach (var modelFile in modelFileExtensions.SelectMany(pattern => Directory.EnumerateFiles(path, pattern)))
            {
                // Prepare names
                string directory = Path.GetDirectoryName(modelFile);
                string extension = modelFile.EndsWith(".txt.gz") ? ".txt.gz" : Path.GetExtension(modelFile);
                string instanceName = modelFile.EndsWith(".txt.gz") ? Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(modelFile)) : Path.GetFileNameWithoutExtension(modelFile);
                // Log
                Console.Write("Converting " + modelFile + " ... ");
                // Catch any exception (because other files might easily be mistaken as models with a .txt or .txt.gz ending)
                try
                {
                    // Read the model while rounding all values
                    MDPLP model = new MDPLP(modelFile, null, SolverType.Gurobi, null, rounding);
                    // Determine filename
                    string outputFilename = Path.Combine(directory, instanceName + "-rounding" + rounding.ToString() + extension);
                    // Log
                    Console.Write("to new file " + outputFilename + " ... ");
                    // Output the rounded model
                    model.Write(outputFilename);
                    // Log
                    Console.WriteLine("Done!");
                }
                catch (Exception ex) { Console.WriteLine("Caught an exception converting the file: " + ex.Message); }
            }
        }
    }
}
