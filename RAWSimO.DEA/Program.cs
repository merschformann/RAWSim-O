using Atto.LinearWrap;
using RAWSimO.Core.Statistics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.DEA
{
    class Program
    {
        static void Main(string[] args)
        {
            // Init logging
            using (StreamWriter sw = new StreamWriter("DEA.log", false))
            {
                // Create log action
                Action<string> log = (string msg) => { Console.Write(msg); sw.Write(msg); };

                // Say hello
                log("<<< Welcome to the RAWSimO DEA handler >>>" + Environment.NewLine);

                // Read path to footprints file
                string path;
                if (args.Length == 1)
                {
                    path = args[0];
                    log("Using footprints.csv from argument: " + path + Environment.NewLine);
                }
                else
                {
                    log("Enter the path to the footprints.csv file:" + Environment.NewLine);
                    path = Console.ReadLine();
                }
                sw.WriteLine(path);

                // Build default configurations to run
                List<DEAConfiguration> configurations = new List<DEAConfiguration>() {
                    new DEAConfiguration()
                    {
                        Name = "Mu",
                        Datafile = path,
                        LogAction = log,
                        SolverChoice = SolverType.Gurobi,
                        InputOriented = false,
                        WeightsSumToOne = true,
                        TransformOutputOrientedEfficiency = false,
                        ResultFileCondensed = "deascoremu.csv",
                        BoxPlotBaseFilename = "deaboxplotsmu",
                        Groups = new List<FootprintDatapoint.FootPrintEntry>()
                        {
                            FootprintDatapoint.FootPrintEntry.TagSetting1,
                        },
                        ServiceUnitIdents = new List<FootprintDatapoint.FootPrintEntry>()
                        {
                            FootprintDatapoint.FootPrintEntry.NOStations,
                            //FootprintDatapoint.FootPrintEntry.NIStations,
                            FootprintDatapoint.FootPrintEntry.BotsPerOStation,
                            //FootprintDatapoint.FootPrintEntry.OStationCapacityAvg,
                            //FootprintDatapoint.FootPrintEntry.Controller,
                            FootprintDatapoint.FootPrintEntry.TA,
                            FootprintDatapoint.FootPrintEntry.IS,
                            FootprintDatapoint.FootPrintEntry.PS,
                            FootprintDatapoint.FootPrintEntry.RB,
                            FootprintDatapoint.FootPrintEntry.OB,
                        },
                        Inputs = new List<Tuple<FootprintDatapoint.FootPrintEntry, InputType>>()
                        {
                            new Tuple<FootprintDatapoint.FootPrintEntry, InputType>(FootprintDatapoint.FootPrintEntry.NOStations, InputType.Resource),
                            //new Tuple<FootprintDatapoint.FootPrintEntry, InputType>(FootprintDatapoint.FootPrintEntry.NIStations, InputType.Resource),
                            new Tuple<FootprintDatapoint.FootPrintEntry, InputType>(FootprintDatapoint.FootPrintEntry.BotsPerOStation, InputType.Resource),
                            //new Tuple<FootprintDatapoint.FootPrintEntry, InputType>(FootprintDatapoint.FootPrintEntry.OStationCapacityAvg, InputType.Resource),
                        },
                        Outputs = new List<Tuple<FootprintDatapoint.FootPrintEntry, OutputType>>()
                        {
                            //new Tuple<FootprintDatapoint.FootPrintEntry, OutputType>(FootprintDatapoint.FootPrintEntry.OrderThroughputRate, OutputType.Benefit),
                            new Tuple<FootprintDatapoint.FootPrintEntry, OutputType>(FootprintDatapoint.FootPrintEntry.ItemThroughputRateScore, OutputType.Benefit),
                            new Tuple<FootprintDatapoint.FootPrintEntry, OutputType>(FootprintDatapoint.FootPrintEntry.OrderLatenessAvg, OutputType.Loss),
                            new Tuple<FootprintDatapoint.FootPrintEntry, OutputType>(FootprintDatapoint.FootPrintEntry.LateOrdersFractional , OutputType.Loss),
                        },
                    },
                };

                // Create and solve models
                foreach (var config in configurations)
                {
                    log(">>> Creating model: " + config.Name + Environment.NewLine);
                    DEAModel modelmu1 = new DEAModel(config);
                    log(">>> Solving model: " + config.Name + Environment.NewLine);
                    modelmu1.Solve();
                }
            }

            // Wait for it ....
            Console.WriteLine(".Fin.");
            Console.ReadLine();
        }
    }
}
