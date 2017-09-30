using RAWSimO.Core.Configurations;
using RAWSimO.Core.Generator;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.IO;
using RAWSimO.Core.Randomization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Playground.Tests
{
    public class RandomizerTests
    {
        #region Poisson tests

        public static void TestBasicPoisson()
        {
            int hours = 48;
            double currentTime = 0;
            double dueTime = TimeSpan.FromHours(hours).TotalSeconds;

            // Test inhomogeneous poisson generator
            double rate = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromHours(1), 100);
            PoissonGenerator generator = new PoissonGenerator(new RandomizerSimple(0), rate);
            List<double> homogeneousSteps = new List<double>();
            while (currentTime < dueTime)
            {
                currentTime += generator.Next(currentTime);
                homogeneousSteps.Add(currentTime);
            }
            Console.WriteLine("Homogeneous Poisson generated " + homogeneousSteps.Count + " in " + hours + " seconds with a rate of " + rate);

            // Test inhomogeneous poisson generator
            currentTime = 0;
            PoissonGenerator inhomogeneousGenerator = new PoissonGenerator(
                new RandomizerSimple(0),
                TimeSpan.FromHours(24).TotalSeconds,
                new KeyValuePair<double, double>[] {
                    new KeyValuePair<double, double>(TimeSpan.FromHours(0).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 20)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(1).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 10)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(2).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 5)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(3).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 5)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(4).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 10)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(5).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 10)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(6).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 20)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(7).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 20)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(8).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 40)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(9).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 80)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(10).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 80)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(11).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 90)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(12).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 110)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(13).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 80)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(14).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 90)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(15).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 130)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(16).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 180)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(17).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 120)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(18).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 190)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(19).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 250)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(20).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 220)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(21).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 150)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(22).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 110)),
                    new KeyValuePair<double, double>(TimeSpan.FromHours(23).TotalSeconds, PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 50))
                });
            List<double> inhomogeneousSteps = new List<double>();
            while (currentTime < dueTime)
            {
                currentTime += inhomogeneousGenerator.Next(currentTime);
                inhomogeneousSteps.Add(currentTime);
            }
            Console.WriteLine("Homogeneous Poisson generated " + inhomogeneousSteps.Count + " in " + hours + " with rates " + string.Join(",", inhomogeneousGenerator.TimeDependentRates.Select(r => "(" + r.Key + "/" + r.Value + ")")));

            // Output graph
            WriteHourBasedGraph(new List<List<double>>() { homogeneousSteps, inhomogeneousSteps }, new List<string>() { "Homogeneous", "Inhomogeneous" }, hours);
        }

        public static void TestInputTranslationTimeDependentPoisson()
        {
            SettingConfiguration config = new SettingConfiguration();
            config.InventoryConfiguration.PoissonInventoryConfiguration = new PoissonInventoryConfiguration(new DefaultConstructorIdentificationClass());
            IRandomizer randomizer = new RandomizerSimple(0);
            int oStationCount = 3;
            int iStationCount = 3;
            // --> Instantiate poisson generator for orders
            // Calculate instance-specific factor to adapt the rates
            List<KeyValuePair<double, double>> relativeOrderWeights = new List<KeyValuePair<double, double>>();
            for (int i = 0; i < config.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentOrderWeights.Count; i++)
            {
                relativeOrderWeights.Add(new KeyValuePair<double, double>(
                    i > 0 ?
                    config.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentOrderWeights[i].Key - config.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentOrderWeights[i - 1].Key :
                    config.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentOrderWeights[i].Key,
                    config.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentOrderWeights[i].Value
                    ));
            }
            double unadjustedAverageOrderFrequency =
                relativeOrderWeights.Sum(w => w.Key * w.Value) /
                config.InventoryConfiguration.PoissonInventoryConfiguration.MaxTimeForTimeDependentOrderRates;
            double aimedAverageOrderFrequency =
                TimeSpan.FromSeconds(config.InventoryConfiguration.PoissonInventoryConfiguration.MaxTimeForTimeDependentOrderRates).TotalHours *
                config.InventoryConfiguration.PoissonInventoryConfiguration.AverageOrdersPerHourAndStation * oStationCount / config.InventoryConfiguration.PoissonInventoryConfiguration.MaxTimeForTimeDependentOrderRates;
            double orderSteerFactor = aimedAverageOrderFrequency / unadjustedAverageOrderFrequency;
            // Initiate order poisson generator
            PoissonGenerator TimeDependentOrderPoissonGenerator = new PoissonGenerator(
                randomizer,
                config.InventoryConfiguration.PoissonInventoryConfiguration.MaxTimeForTimeDependentOrderRates,
                config.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentOrderWeights.Select(w =>
                    new KeyValuePair<double, double>(w.Key, orderSteerFactor * w.Value)));
            // --> Instantiate poisson generator for bundles
            // Calculate instance-specific factor to adapt the rates
            List<KeyValuePair<double, double>> relativeBundleWeights = new List<KeyValuePair<double, double>>();
            for (int i = 0; i < config.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentBundleWeights.Count; i++)
            {
                relativeBundleWeights.Add(new KeyValuePair<double, double>(
                    i > 0 ?
                    config.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentBundleWeights[i].Key - config.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentBundleWeights[i - 1].Key :
                    config.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentBundleWeights[i].Key,
                    config.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentBundleWeights[i].Value
                    ));
            }
            double unadjustedAverageBundleFrequency =
                relativeBundleWeights.Sum(w => w.Key * w.Value) /
                config.InventoryConfiguration.PoissonInventoryConfiguration.MaxTimeForTimeDependentBundleRates;
            double aimedAverageBundleFrequency =
                TimeSpan.FromSeconds(config.InventoryConfiguration.PoissonInventoryConfiguration.MaxTimeForTimeDependentBundleRates).TotalHours *
                config.InventoryConfiguration.PoissonInventoryConfiguration.AverageBundlesPerHourAndStation * iStationCount / config.InventoryConfiguration.PoissonInventoryConfiguration.MaxTimeForTimeDependentBundleRates;
            double bundleSteerFactor = aimedAverageBundleFrequency / unadjustedAverageBundleFrequency;
            // Initiate bundle poisson generator
            PoissonGenerator TimeDependentBundlePoissonGenerator = new PoissonGenerator(
                  randomizer,
                  config.InventoryConfiguration.PoissonInventoryConfiguration.MaxTimeForTimeDependentBundleRates,
                  config.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentBundleWeights.Select(w =>
                      new KeyValuePair<double, double>(w.Key, bundleSteerFactor * w.Value)));
            // Initiate time-independent order poisson generator
            double orderRate = PoissonGenerator.TranslateIntoRateParameter(
                TimeSpan.FromHours(1),
                config.InventoryConfiguration.PoissonInventoryConfiguration.AverageOrdersPerHourAndStation * oStationCount);
            PoissonGenerator TimeIndependentOrderPoissonGenerator = new PoissonGenerator(randomizer, orderRate);
            // Initiate time-independent bundle poisson generator
            double bundleRate = PoissonGenerator.TranslateIntoRateParameter(
                TimeSpan.FromHours(1),
                config.InventoryConfiguration.PoissonInventoryConfiguration.AverageBundlesPerHourAndStation * iStationCount);
            PoissonGenerator TimeIndependentBundlePoissonGenerator = new PoissonGenerator(randomizer, bundleRate);

            // --> Test
            int simulationHours = 2 * 24;
            double simulationTime = simulationHours * 60 * 60;
            double currentTime = 0;
            List<double> timeDependentBundleSteps = new List<double>();
            while (currentTime < simulationTime)
            {
                currentTime += TimeDependentBundlePoissonGenerator.Next(currentTime);
                timeDependentBundleSteps.Add(currentTime);
            }
            currentTime = 0;
            List<double> timeDependentOrderSteps = new List<double>();
            while (currentTime < simulationTime)
            {
                currentTime += TimeDependentOrderPoissonGenerator.Next(currentTime);
                timeDependentOrderSteps.Add(currentTime);
            }
            currentTime = 0;
            List<double> timeIndependentBundleSteps = new List<double>();
            while (currentTime < simulationTime)
            {
                currentTime += TimeIndependentBundlePoissonGenerator.Next(currentTime);
                timeIndependentBundleSteps.Add(currentTime);
            }
            currentTime = 0;
            List<double> timeIndependentOrderSteps = new List<double>();
            while (currentTime < simulationTime)
            {
                currentTime += TimeIndependentOrderPoissonGenerator.Next(currentTime);
                timeIndependentOrderSteps.Add(currentTime);
            }

            // Output graph
            WriteHourBasedGraph(
                new List<List<double>>() { timeDependentBundleSteps, timeDependentOrderSteps, timeIndependentBundleSteps, timeIndependentOrderSteps },
                new List<string>() { "Bundles (time-dependent)", "Orders (time-dependent)", "Bundles (time-independent)", "Orders (time-independent)" },
                simulationHours);
        }
        public static void WriteHourBasedGraph(List<List<double>> data, List<string> captions, int overallHours)
        {
            for (int i = 0; i < data.Count; i++)
            {
                using (StreamWriter sw = new StreamWriter(captions[i] + "poisson.dat"))
                {
                    int hour = 1;
                    while (hour <= overallHours)
                    {
                        List<double> hourSteps = data[i].TakeWhile(s => s <= TimeSpan.FromHours(hour).TotalSeconds).ToList();
                        sw.WriteLine(hour + " " + hourSteps.Count);
                        data[i].RemoveRange(0, hourSteps.Count);
                        hour++;
                    }
                }
            }

            // Generate scripts to plot the data
            using (StreamWriter sw = new StreamWriter("poisson.gp"))
            {
                sw.WriteLine("reset");
                sw.WriteLine("# Output definition");
                sw.WriteLine("set terminal postscript clip color eps \"Arial\" 14");
                sw.WriteLine("# Parameters");
                sw.WriteLine("set key left top Left");
                sw.WriteLine("set xlabel \"Time(h)\"");
                sw.WriteLine("set ylabel \"Count(#/h)\"");
                sw.WriteLine("set grid");
                sw.WriteLine("set style fill solid 0.25");
                sw.WriteLine("# Line-Styles");
                sw.WriteLine("set style line 1 linetype 1 linecolor rgb \"#474749\" linewidth 3");
                sw.WriteLine("set style line 2 linetype 1 linecolor rgb \"#7090c8\" linewidth 3");
                sw.WriteLine("set style line 3 linetype 1 linecolor rgb \"#42b449\" linewidth 3");
                sw.WriteLine("set style line 4 linetype 1 linecolor rgb \"#f7cb38\" linewidth 3");
                sw.WriteLine("set style line 5 linetype 1 linecolor rgb \"#db4a37\" linewidth 3");
                sw.WriteLine("set title \"Poisson - Test\"");
                sw.WriteLine("set output \"poisson.eps\"");
                sw.WriteLine("plot \\");
                for (int i = 0; i < captions.Count; i++)
                {
                    if (i < captions.Count - 1)
                        sw.WriteLine("\"" + captions[i] + "poisson.dat\" u 1:2 w lines linestyle " + ((i % 5) + 1) + " t \"" + captions[i] + "\", \\");
                    else
                        sw.WriteLine("\"" + captions[i] + "poisson.dat\" u 1:2 w lines linestyle " + ((i % 5) + 1) + " t \"" + captions[i] + "\"");
                }
                sw.WriteLine("reset");
                sw.WriteLine("exit");
            }
            using (StreamWriter sw = new StreamWriter("poisson.cmd"))
            {
                sw.WriteLine("gnuplot poisson.gp");
            }
        }

        #endregion

        #region Normal distribution tests

        public static void TestGenerateNormalDistribution()
        {
            // Prepare
            RandomizerSimple randomizer = new RandomizerSimple(0);
            int randomNumberCount = 5000;

            // --> Double related
            List<List<double>> randomNumbers = new List<List<double>>();
            List<List<int>> randomNumbersRounded = new List<List<int>>();
            List<Tuple<double, double, double, double>> meanStdTuples = new List<Tuple<double, double, double, double>>()
            {
                new Tuple<double, double, double, double>(1, 2, double.NegativeInfinity, double.PositiveInfinity),
                new Tuple<double, double, double, double>(10, 0.5, double.NegativeInfinity, double.PositiveInfinity),
                new Tuple<double, double, double, double>(-7, 5, double.NegativeInfinity, double.PositiveInfinity),
                new Tuple<double, double, double, double>(15, 5, 14.5, double.PositiveInfinity),
                new Tuple<double, double, double, double>(30, 2, 29, 32),
            };
            // Draw random double numbers
            for (int j = 0; j < meanStdTuples.Count; j++)
            {
                randomNumbers.Add(new List<double>());
                for (int i = 0; i < randomNumberCount; i++)
                {
                    randomNumbers[j].Add(randomizer.NextNormalDouble(meanStdTuples[j].Item1, meanStdTuples[j].Item2, meanStdTuples[j].Item3, meanStdTuples[j].Item4));
                }
            }

            // Round them to get ints
            for (int j = 0; j < meanStdTuples.Count; j++)
            {
                randomNumbersRounded.Add(new List<int>());
                for (int i = 0; i < randomNumbers[j].Count; i++)
                {
                    randomNumbersRounded[j].Add((int)Math.Round(randomNumbers[j][i]));
                }
            }

            // Write them
            string fileNameBase = "normaldistribution";
            for (int i = 0; i < meanStdTuples.Count; i++)
            {
                using (StreamWriter sw = new StreamWriter(fileNameBase + i.ToString() + ".dat"))
                {
                    foreach (var numberGroup in randomNumbersRounded[i].GroupBy(e => e).OrderBy(g => g.Key))
                    {
                        sw.WriteLine(numberGroup.Key.ToString() + " " + numberGroup.Count().ToString());
                    }
                }
            }

            // Write plot script
            using (StreamWriter sw = new StreamWriter(fileNameBase + ".gp"))
            {
                sw.WriteLine("reset");
                sw.WriteLine("# Output definition");
                sw.WriteLine("set terminal postscript clip color eps \"Arial\" 14");
                sw.WriteLine("# Parameters");
                sw.WriteLine("set key left top Left");
                sw.WriteLine("set xlabel \"Value\"");
                sw.WriteLine("set ylabel \"Count\"");
                sw.WriteLine("set grid");
                sw.WriteLine("set style fill solid 0.25");
                sw.WriteLine("# Line-Styles");
                sw.WriteLine("set style line 1 linetype 1 linecolor rgb \"#474749\" linewidth 1");
                sw.WriteLine("set style line 2 linetype 1 linecolor rgb \"#7090c8\" linewidth 1");
                sw.WriteLine("set style line 3 linetype 1 linecolor rgb \"#42b449\" linewidth 1");
                sw.WriteLine("set style line 4 linetype 1 linecolor rgb \"#f7cb38\" linewidth 1");
                sw.WriteLine("set style line 5 linetype 1 linecolor rgb \"#db4a37\" linewidth 1");
                sw.WriteLine("set title \"Normal distribution - Test\"");
                sw.WriteLine("set output \"" + fileNameBase + ".eps\"");
                sw.WriteLine("plot \\");
                for (int i = 0; i < meanStdTuples.Count; i++)
                {
                    if (i < meanStdTuples.Count - 1)
                        sw.WriteLine("\"" + fileNameBase + i.ToString() + ".dat\" u 1:2 w boxes linestyle " + ((i % 5) + 1) +
                            " t \"mean: " + meanStdTuples[i].Item1.ToString(IOConstants.FORMATTER) +
                            " std: " + meanStdTuples[i].Item2.ToString(IOConstants.FORMATTER) +
                            " lb: " + (double.IsNegativeInfinity(meanStdTuples[i].Item3) ? "na" : meanStdTuples[i].Item3.ToString(IOConstants.FORMATTER)) +
                            " ub: " + (double.IsPositiveInfinity(meanStdTuples[i].Item4) ? "na" : meanStdTuples[i].Item4.ToString(IOConstants.FORMATTER)) + "\", \\");
                    else
                        sw.WriteLine("\"" + fileNameBase + i.ToString() + ".dat\" u 1:2 w boxes linestyle " + ((i % 5) + 1) +
                            " t \"mean: " + meanStdTuples[i].Item1.ToString(IOConstants.FORMATTER) +
                            " std: " + meanStdTuples[i].Item2.ToString(IOConstants.FORMATTER) +
                            " lb: " + (double.IsNegativeInfinity(meanStdTuples[i].Item3) ? "na" : meanStdTuples[i].Item3.ToString(IOConstants.FORMATTER)) +
                            " ub: " + (double.IsPositiveInfinity(meanStdTuples[i].Item4) ? "na" : meanStdTuples[i].Item4.ToString(IOConstants.FORMATTER)) + "\"");
                }
                sw.WriteLine("reset");
                sw.WriteLine("exit");
            }
            using (StreamWriter sw = new StreamWriter(fileNameBase + ".cmd"))
            {
                sw.WriteLine("gnuplot " + fileNameBase + ".gp");
            }

            // --> Int related
            int randomNumberCountInt = 200;
            List<List<int>> randomNumbersInt = new List<List<int>>();
            List<Tuple<double, double, int, int>> meanStdTuplesInt = new List<Tuple<double, double, int, int>>()
            {
                new Tuple<double, double, int, int>(1, 1.5, 1, 4),
                new Tuple<double, double, int, int>(4, 0.5, 0, int.MaxValue),
                new Tuple<double, double, int, int>(-7, 3, int.MinValue, int.MaxValue),
            };

            // Draw random int numbers
            for (int j = 0; j < meanStdTuplesInt.Count; j++)
            {
                randomNumbersInt.Add(new List<int>());
                for (int i = 0; i < randomNumberCountInt; i++)
                {
                    randomNumbersInt[j].Add(randomizer.NextNormalInt(meanStdTuplesInt[j].Item1, meanStdTuplesInt[j].Item2, meanStdTuplesInt[j].Item3, meanStdTuplesInt[j].Item4));
                }
            }

            // Write them
            string fileNameBaseInt = "normaldistributionint";
            for (int i = 0; i < meanStdTuplesInt.Count; i++)
            {
                using (StreamWriter sw = new StreamWriter(fileNameBaseInt + i.ToString() + ".dat"))
                {
                    foreach (var numberGroup in randomNumbersInt[i].GroupBy(e => e).OrderBy(g => g.Key))
                    {
                        sw.WriteLine(numberGroup.Key.ToString() + " " + numberGroup.Count().ToString());
                    }
                }
            }

            // Write plot script
            using (StreamWriter sw = new StreamWriter(fileNameBaseInt + ".gp"))
            {
                sw.WriteLine("reset");
                sw.WriteLine("# Output definition");
                sw.WriteLine("set terminal postscript clip color eps \"Arial\" 14");
                sw.WriteLine("# Parameters");
                sw.WriteLine("set key left top Left");
                sw.WriteLine("set xlabel \"Value\"");
                sw.WriteLine("set ylabel \"Count\"");
                sw.WriteLine("set grid");
                sw.WriteLine("set style fill solid 0.25");
                sw.WriteLine("# Line-Styles");
                sw.WriteLine("set style line 1 linetype 1 linecolor rgb \"#474749\" linewidth 1");
                sw.WriteLine("set style line 2 linetype 1 linecolor rgb \"#7090c8\" linewidth 1");
                sw.WriteLine("set style line 3 linetype 1 linecolor rgb \"#42b449\" linewidth 1");
                sw.WriteLine("set style line 4 linetype 1 linecolor rgb \"#f7cb38\" linewidth 1");
                sw.WriteLine("set style line 5 linetype 1 linecolor rgb \"#db4a37\" linewidth 1");
                sw.WriteLine("set title \"Normal distribution (int) - Test\"");
                sw.WriteLine("set output \"" + fileNameBaseInt + ".eps\"");
                sw.WriteLine("plot \\");
                for (int i = 0; i < meanStdTuplesInt.Count; i++)
                {
                    if (i < meanStdTuplesInt.Count - 1)
                        sw.WriteLine("\"" + fileNameBaseInt + i.ToString() + ".dat\" u 1:2 w boxes linestyle " + ((i % 5) + 1) +
                            " t \"mean: " + meanStdTuplesInt[i].Item1.ToString(IOConstants.FORMATTER) +
                            " std: " + meanStdTuplesInt[i].Item2.ToString(IOConstants.FORMATTER) +
                            " lb: " + ((int.MinValue == meanStdTuplesInt[i].Item3) ? "na" : meanStdTuplesInt[i].Item3.ToString(IOConstants.FORMATTER)) +
                            " ub: " + ((int.MaxValue == meanStdTuplesInt[i].Item4) ? "na" : meanStdTuplesInt[i].Item4.ToString(IOConstants.FORMATTER)) + "\", \\");
                    else
                        sw.WriteLine("\"" + fileNameBaseInt + i.ToString() + ".dat\" u 1:2 w boxes linestyle " + ((i % 5) + 1) +
                            " t \"mean: " + meanStdTuplesInt[i].Item1.ToString(IOConstants.FORMATTER) +
                            " std: " + meanStdTuplesInt[i].Item2.ToString(IOConstants.FORMATTER) +
                            " lb: " + ((int.MinValue == meanStdTuplesInt[i].Item3) ? "na" : meanStdTuplesInt[i].Item3.ToString(IOConstants.FORMATTER)) +
                            " ub: " + ((int.MaxValue == meanStdTuplesInt[i].Item4) ? "na" : meanStdTuplesInt[i].Item4.ToString(IOConstants.FORMATTER)) + "\"");
                }
                sw.WriteLine("reset");
                sw.WriteLine("exit");
            }
            using (StreamWriter sw = new StreamWriter(fileNameBaseInt + ".cmd"))
            {
                sw.WriteLine("gnuplot " + fileNameBaseInt + ".gp");
            }
        }

        #endregion
    }
}
