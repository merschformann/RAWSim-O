using RAWSimO.Core.Statistics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.DataPreparation
{
    class Program
    {
        static void Main(string[] args)
        {
            // Say hello
            Console.WriteLine("<<< Welcome to the RAWSimO DataPreparator >>>");

            // Init
            DataProcessor preparer = new DataProcessor();

            // Use argument path if available
            if (args.Length == 1)
                preparer.PrepareAllResults(args[0]);
            // If two arguments are given and the first is constant 'scatter', generate scatter plots for all of the footprint files
            else if (args.Length == 2 && args[0] == "scatter")
            {
                string[] footprintfiles = args[1].Split(',');
                // Generate plots for all given footprint files
                foreach (var path in footprintfiles)
                {
                    // Plot
                    FootprintScatterProcessor scatterplotProcessor = new FootprintScatterProcessor(path);
                    scatterplotProcessor.ReadFootprintData();
                    scatterplotProcessor.PlotFootprintsScattered("scatterplotsungrouped", FootprintScatterProcessor.DEFAULT_UNGROUPED_SCATTERPLOT_DATA, new List<FootprintDatapoint.FootPrintEntry>());
                    foreach (var grouping in FootprintScatterProcessor.DEFAULT_GROUPINGS)
                    {
                        Console.WriteLine("Generating scatterplots for grouping by " + string.Join("/", grouping.Item1) + " ...");
                        scatterplotProcessor.PlotFootprintsScattered(grouping.Item2, FootprintScatterProcessor.DEFAULT_GROUPINGS_SCATTERPLOT_DATA, grouping.Item1);
                    }
                }
            }
            else
            {
                // Choose option
                Console.WriteLine(">>> Choose option: ");
                Console.WriteLine("1: Process experiment results");
                Console.WriteLine("2: Generate well sortedness graphs");
                Console.WriteLine("3: Plot inventory frequency graph");
                Console.WriteLine("4: Scatterplot experiment results");
                Console.WriteLine("5: Calculate correlation matrix");
                Console.WriteLine("6: Box plot experiment results");
                char optionKey = Console.ReadKey().KeyChar; Console.WriteLine();
                switch (optionKey)
                {

                    case '1':
                        {
                            // Read path
                            Console.WriteLine("Enter the path to the root result folder:");
                            string path = Console.ReadLine();
                            // Determine all results / only footprint condensation
                            Console.WriteLine("Plot graphs? (y/n)");
                            char plotGraphKey = Console.ReadKey().KeyChar;
                            Console.WriteLine();
                            if (char.ToLower(plotGraphKey) == 'y')
                                preparer.PrepareAllResults(path);
                            else
                                preparer.PrepareOnlyFootprints(path);
                        }
                        break;
                    case '2':
                        {
                            // Read path
                            Console.WriteLine("Enter the path to the root result folder:");
                            string path = Console.ReadLine();
                            // Decide data-type
                            Console.WriteLine("Select data type (separate multiple ones by ','):");
                            string[] dataTypes = Enum.GetNames(typeof(WellSortednessProcessor.WellsortednessBaseDataType));
                            for (int i = 0; i < dataTypes.Length; i++)
                                Console.WriteLine(i.ToString() + ": " + dataTypes[i]);
                            string[] datatypeSelections = Console.ReadLine().Split(',');
                            // Decide overlay
                            Console.WriteLine("Select overlay type (separate multiple ones by ','):");
                            string[] overlayTypes = Enum.GetNames(typeof(WellSortednessProcessor.WellsortednessPlotOverlay));
                            for (int i = 0; i < overlayTypes.Length; i++)
                                Console.WriteLine(i.ToString() + ": " + overlayTypes[i]);
                            string[] overlaySelections = Console.ReadLine().Split(',');
                            // Set path time aggregation length
                            Console.WriteLine("Set pathtime length (s) for aggregation (default: no aggregation):");
                            string pathtimeLength = Console.ReadLine();
                            int pathtimeAggregationLength = string.IsNullOrWhiteSpace(pathtimeLength) ? 0 : int.Parse(pathtimeLength);
                            // Set time aggregation length
                            Console.WriteLine("Set time (s) for aggregation (default: no aggregation):");
                            string timeLength = Console.ReadLine();
                            int timeAggregationLength = string.IsNullOrWhiteSpace(timeLength) ? 0 : int.Parse(timeLength);
                            // Plot all graphs
                            foreach (var datatypeSelection in datatypeSelections)
                                foreach (var overlaySelection in overlaySelections)
                                    WellSortednessProcessor.PlotAll(
                                        path,
                                        (WellSortednessProcessor.WellsortednessBaseDataType)Enum.Parse(typeof(WellSortednessProcessor.WellsortednessBaseDataType), dataTypes[int.Parse(datatypeSelection)]),
                                        (WellSortednessProcessor.WellsortednessPlotOverlay)Enum.Parse(typeof(WellSortednessProcessor.WellsortednessPlotOverlay), overlayTypes[int.Parse(overlaySelection)]),
                                        pathtimeAggregationLength,
                                        timeAggregationLength);
                        }
                        break;
                    case '3':
                        {
                            // Read path
                            Console.WriteLine("Enter the path to the inventory config file (.xgenc):");
                            string path = Console.ReadLine();
                            // Plot
                            InventoryInfoProcessor.PlotSimpleInventoryFrequencies(path);
                        }
                        break;
                    case '4':
                        {
                            // Read path
                            Console.WriteLine("Enter the path to the footprints file (.csv):");
                            string path = Console.ReadLine();
                            // Plot
                            FootprintScatterProcessor scatterplotProcessor = new FootprintScatterProcessor(path);
                            scatterplotProcessor.ReadFootprintData();
                            scatterplotProcessor.PlotFootprintsScattered("scatterplotsungrouped", FootprintScatterProcessor.DEFAULT_UNGROUPED_SCATTERPLOT_DATA, new List<FootprintDatapoint.FootPrintEntry>());
                            foreach (var grouping in FootprintScatterProcessor.DEFAULT_GROUPINGS)
                            {
                                Console.WriteLine("Generating scatterplots for grouping by " + string.Join("/", grouping.Item1) + " ...");
                                scatterplotProcessor.PlotFootprintsScattered(grouping.Item2, FootprintScatterProcessor.DEFAULT_GROUPINGS_SCATTERPLOT_DATA, grouping.Item1);
                            }
                        }
                        break;
                    case '5':
                        {
                            // Read path
                            Console.WriteLine("Enter the path to the footprints file (.csv):");
                            string path = Console.ReadLine();
                            // Calculate and write correlation matrix
                            CorrelationProcessor.WriteCorrelationMatrix(path, Path.Combine(Path.GetDirectoryName(path), "correlations.csv"), new List<FootprintDatapoint.FootPrintEntry>
                            {
                                FootprintDatapoint.FootPrintEntry.ItemThroughputRateScore,
                                FootprintDatapoint.FootPrintEntry.OrderThroughputRate,
                                FootprintDatapoint.FootPrintEntry.OrderTurnoverTimeAvg,
                                FootprintDatapoint.FootPrintEntry.DistanceTraveledPerBot,
                                FootprintDatapoint.FootPrintEntry.OrderLatenessAvg,
                                FootprintDatapoint.FootPrintEntry.LateOrdersFractional,
                                FootprintDatapoint.FootPrintEntry.ItemPileOneAvg,
                                FootprintDatapoint.FootPrintEntry.OSIdleTimeAvg,
                            });
                        }
                        break;
                    case '6':
                        {
                            // Read path
                            Console.WriteLine("Enter the path to the footprints file (.csv):");
                            string path = Console.ReadLine();
                            // Calculate and write correlation matrix
                            FootprintBoxPlotProcessor.Plot(path,
                                new List<FootprintDatapoint.FootPrintEntry>
                                {
                                    FootprintDatapoint.FootPrintEntry.PP,
                                    FootprintDatapoint.FootPrintEntry.TA,
                                    FootprintDatapoint.FootPrintEntry.IS,
                                    FootprintDatapoint.FootPrintEntry.PS,
                                    FootprintDatapoint.FootPrintEntry.RB,
                                    FootprintDatapoint.FootPrintEntry.OB,
                                    FootprintDatapoint.FootPrintEntry.BotsPerOStation,
                                    FootprintDatapoint.FootPrintEntry.NOStations,
                                },
                                new List<FootprintDatapoint.FootPrintEntry>
                                {
                                    FootprintDatapoint.FootPrintEntry.ItemThroughputRateScore,
                                    FootprintDatapoint.FootPrintEntry.ItemPileOneAvg,
                                    FootprintDatapoint.FootPrintEntry.OrderLatenessAvg,
                                    FootprintDatapoint.FootPrintEntry.LateOrdersFractional,
                                    FootprintDatapoint.FootPrintEntry.DistanceTraveledPerBot,
                                    FootprintDatapoint.FootPrintEntry.OSIdleTimeAvg,
                                });
                        }
                        break;
                    default: /* Do nothing */ break;
                }
                // End interactive mode
                Console.WriteLine(".Fin.");
                Console.ReadLine();
            }
        }
    }
}
