using Atto.LinearWrap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MDPSolve
{
    class Program
    {
        static void Main(string[] args)
        {
            //string instanceFile = Path.Combine("..", "..", "..", "..", "Material", "MDP", "testCase1_memoryEfficient.txt");
            Console.WriteLine("<<< Welcome to the RAWSimO MDP solver >>>");
            string instanceFile;
            if (args.Length >= 1)
            {
                instanceFile = args[0];
            }
            else
            {
                Console.WriteLine("Specify input file: ");
                instanceFile = Console.ReadLine();
            }
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string extension = instanceFile.EndsWith(".txt.gz") ? ".txt.gz" : Path.GetExtension(instanceFile);
            string instanceName = instanceFile.EndsWith(".txt.gz") ? Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(instanceFile)) : Path.GetFileNameWithoutExtension(instanceFile);
            string solutionFile = Path.Combine(Path.GetDirectoryName(instanceFile), instanceName + "." + timestamp + ".solution" + extension);
            string logFile = Path.Combine(Path.GetDirectoryName(instanceFile), instanceName + "." + timestamp + ".log" + extension);
            MDPLP model = new MDPLP(instanceFile, (string msg) => { Console.Write(msg); }, SolverType.CPLEX, args.Skip(1).ToArray());
            model.Solve();
            if (model.SolutionAvailable)
                model.Write(solutionFile);
            model.DumpLog(logFile);
            Console.WriteLine(".Fin.");
            if (args.Length == 0)
                Console.ReadLine();
        }
    }
}
