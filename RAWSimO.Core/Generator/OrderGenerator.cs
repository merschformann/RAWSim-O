using RAWSimO.Core.Configurations;
using RAWSimO.Core.Helper;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.IO;
using RAWSimO.Core.Items;
using RAWSimO.Core.Metrics;
using RAWSimO.Core.Randomization;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Generator
{
    /// <summary>
    /// A class exposing methods to generate orders before starting the simulation and for generating configurations for online generation.
    /// </summary>
    public class OrderGenerator
    {
        #region Order generation for fixed mode

        /// <summary>
        /// Generates a list of orders and bundles. TODO this has to be adapted to handle simple items too. While doing so use a configuration for all the settings and feed it to the GUI.
        /// </summary>
        /// <param name="wordFile">The word file.</param>
        /// <param name="baseColors">The colors</param>
        /// <param name="baseColorProbabilities"></param>
        /// <param name="seed"></param>
        /// <param name="minTime"></param>
        /// <param name="maxTime"></param>
        /// <param name="orderCount"></param>
        /// <param name="minItemWeight"></param>
        /// <param name="maxItemWeight"></param>
        /// <param name="minPositionCount"></param>
        /// <param name="maxPositionCount"></param>
        /// <param name="minBundleSize"></param>
        /// <param name="maxBundleSize"></param>
        /// <param name="relativeBundleCount"></param>
        /// <returns></returns>
        public static OrderList GenerateOrders(
            string wordFile,
            LetterColors[] baseColors,
            IDictionary<LetterColors, double> baseColorProbabilities,
            int seed,
            double minTime,
            double maxTime,
            int orderCount,
            double minItemWeight,
            double maxItemWeight,
            int minPositionCount,
            int maxPositionCount,
            int minBundleSize,
            int maxBundleSize,
            double relativeBundleCount)
        {
            // Init random
            IRandomizer randomizer = new RandomizerSimple(seed);
            // Read the words that serve as the base for the item types
            List<string> baseWords = new List<string>();
            using (StreamReader sr = new StreamReader(wordFile))
            {
                string line = "";
                while ((line = sr.ReadLine()) != null)
                    baseWords.Add(line.Trim());
            }
            baseWords = baseWords.Distinct().ToList();
            List<char> baseLetters = baseWords.SelectMany(w => w.ToCharArray()).Distinct().ToList();

            // --> Build all necessary item descriptions
            OrderList orderList = new OrderList(ItemType.Letter);
            int currentItemDescriptionID = 0;
            foreach (var letter in baseLetters)
                foreach (var color in baseColors)
                    orderList.ItemDescriptions.Add(new ColoredLetterDescription(null) { ID = currentItemDescriptionID++, Color = color, Letter = letter, Weight = randomizer.NextDouble(minItemWeight, maxItemWeight) });

            // --> Generate orders randomly
            for (int i = 0; i < orderCount; i++)
            {
                // Choose a random word from the list
                string word = baseWords[randomizer.NextInt(baseWords.Count)];
                Order order = new Order() { TimeStamp = randomizer.NextDouble(minTime, maxTime) };

                // Add each letter to originalLetters
                for (int j = 0; j < word.Length; j++)
                {
                    // Get color based on distribution
                    double r = randomizer.NextDouble();
                    // Choose a default one just incase
                    LetterColors chosenColor = baseColors.First();
                    // Go through and check the range of each color, pulling random number down to the current range
                    foreach (var c in baseColors)
                    {
                        if (baseColorProbabilities[c] > r)
                        {
                            chosenColor = c; break;
                        }
                        r -= baseColorProbabilities[c];
                    }

                    // Add letter to order
                    order.AddPosition(
                        orderList.ItemDescriptions.Single(d => (d as ColoredLetterDescription).Letter == word[j] && (d as ColoredLetterDescription).Color == chosenColor),
                        randomizer.NextInt(minPositionCount, maxPositionCount + 1));
                }

                // Add the order
                orderList.Orders.Add(order);
            }

            // Get probability of item-descriptions
            Dictionary<ItemDescription, int> itemFrequency = orderList.Orders.SelectMany(o => o.Positions).GroupBy(p => p.Key).ToDictionary(i => i.Key, i => i.Sum(e => e.Value));
            double overallCount = itemFrequency.Sum(f => f.Value);
            Dictionary<ItemDescription, double> itemProbability = itemFrequency.ToDictionary(k => k.Key, v => (double)itemFrequency[v.Key] / overallCount);

            // --> Generate appropriate bundles for this list
            int itemsOrdered = (int)(orderList.Orders.Sum(o => o.Positions.Sum(p => p.Value)) * relativeBundleCount); int newItems = 0; int currentBundleID = 0;
            for (int itemsStored = 0; itemsStored < itemsOrdered; itemsStored += newItems)
            {
                // Draw a random item description based on the frequency of the items
                ItemDescription newBundleDescription = RandomizerHelper.DrawRandomly<ItemDescription>(itemProbability, randomizer);
                int bundleSize = randomizer.NextInt(minBundleSize, maxBundleSize);
                double timeStamp = randomizer.NextDouble(minTime, maxTime);
                ColoredLetterBundle bundle = new ColoredLetterBundle(null) { ID = currentBundleID++, ItemDescription = newBundleDescription, TimeStamp = timeStamp, ItemCount = bundleSize };
                // Add bundle to list
                orderList.Bundles.Add(bundle);
                // Signal new items
                newItems = bundleSize;
            }

            // Order the orders and bundles
            orderList.Sort();

            // Return it
            return orderList;
        }

        #endregion

        #region Item generator configuration for online generation

        /// <summary>
        /// Defines the type of distribution used when generating the probability weights for the item descritptions.
        /// </summary>
        public enum ItemDescriptionProbabilityWeightDistributionType
        {
            /// <summary>
            /// Uses a constant value for all probability weights.
            /// </summary>
            Constant,
            /// <summary>
            /// Uses a uniform distribution to generate the probability weights.
            /// </summary>
            Uniform,
            /// <summary>
            /// Uses a normal distribution to generate the probability weights.
            /// </summary>
            Normal,
            /// <summary>
            /// Uses an exponential distribution to generate the probability weights.
            /// </summary>
            Exponential,
            /// <summary>
            /// Uses a gamma distribution to generate the probability weights.
            /// </summary>
            Gamma,
        }
        /// <summary>
        /// Defines the type of distribution used when generating the weights for the item descriptions.
        /// </summary>
        public enum ItemDescriptionWeightDistributionType
        {
            /// <summary>
            /// Uses a normal distribution to generate the weights.
            /// </summary>
            Normal,
            /// <summary>
            /// Uses an uniform distribution to generate the weights.
            /// </summary>
            Uniform,
        }
        /// <summary>
        /// Defines the type of distribution used when generating the bundle size for the item descriptions.
        /// </summary>
        public enum ItemDescriptionBundleSizeDistributionType
        {
            /// <summary>
            /// Uses a normal distribution to generate the bundle size.
            /// </summary>
            Normal,
            /// <summary>
            /// Uses an uniform distribution to generate the bundle size.
            /// </summary>
            Uniform,
        }
        /// <summary>
        /// A class supplying basic parameters for online generation of simple items.
        /// </summary>
        public class SimpleItemGeneratorPreConfiguration
        {
            /// <summary>
            /// The number of item types to generate.
            /// </summary>
            public int ItemDescriptionCount = 100;
            /// <summary>
            /// The probability for using the combined probability over the simple probability per item type.
            /// </summary>
            public double ProbToUseCoWeight = 0;
            /// <summary>
            /// The default weight per item type.
            /// </summary>
            public double DefaultWeight = 1;
            /// <summary>
            /// The default co-weight for two items.
            /// </summary>
            public double DefaultCoWeight = 0.2;
            /// <summary>
            /// Defines the type of distribution used when generating the probability weights for the item descritptions.
            /// </summary>
            public ItemDescriptionProbabilityWeightDistributionType ProbWeightDistributionType = ItemDescriptionProbabilityWeightDistributionType.Gamma;
            /// <summary>
            /// The constant probability weight to use when assigning the probability weights. This actually does make no difference, because the probability will be equal anyway.
            /// </summary>
            public double ProbabilityWeightConstant = 1;
            /// <summary>
            /// The minimum value for the probability weight when drawing from a uniform distribution.
            /// </summary>
            public double ProbabilityWeightUniformMin = 1;
            /// <summary>
            /// The maximum value for the probability weight when drawing from a uniform distribution.
            /// </summary>
            public double ProbabilityWeightUniformMax = 100;
            /// <summary>
            /// The mean and also minimum value of the probability weight.
            /// </summary>
            public double ProbabilityWeightNormalMu = 1;
            /// <summary>
            /// The standard deviation for the probability weight.
            /// </summary>
            public double ProbabilityWeightNormalSigma = 3;
            /// <summary>
            /// The gamma value used for the exponential distribution to generate the probability weights.
            /// </summary>
            public double ProbabilityWeightExpLambda = 1.5;
            /// <summary>
            /// The k value used for the gamma distribution to generate the probability weights.
            /// </summary>
            public double ProbabilityWeightGammaK = 1.0;
            /// <summary>
            /// The theta value used for the gamma distribution to generate the probability weights.
            /// </summary>
            public double ProbabilityWeightGammaTheta = 2.0;
            /// <summary>
            /// A lower bound for generating the probability weight.
            /// </summary>
            public double ProbabilityWeightLB = 1.0;
            /// <summary>
            /// An upper bound for generating the probability weight.
            /// </summary>
            public double ProbabilityWeightUB = double.PositiveInfinity;
            /// <summary>
            /// Defines the type of distribution used when generating the weights for the item descriptions.
            /// </summary>
            public ItemDescriptionWeightDistributionType WeightDistributionType = ItemDescriptionWeightDistributionType.Uniform;
            /// <summary>
            /// The mean value for the actual item weight.
            /// </summary>
            public double ItemWeightMu = 2;
            /// <summary>
            /// The standard deviation for the actual item weight.
            /// </summary>
            public double ItemWeightSigma = 1;
            /// <summary>
            /// The minimal value for the actual item weight.
            /// </summary>
            public double ItemWeightLB = 2;
            /// <summary>
            /// The maximal value for the actual item weight.
            /// </summary>
            public double ItemWeightUB = 8;
            /// <summary>
            /// Indicates whether the bundle size of the items will be supplied in the file or generated during runtime instead.
            /// </summary>
            public bool SupplyBundleSize = true;
            /// <summary>
            /// Defines the type of distribution used when generating the bundle size for the item descriptions.
            /// </summary>
            public ItemDescriptionBundleSizeDistributionType BundleSizeDistributionType = ItemDescriptionBundleSizeDistributionType.Uniform;
            /// <summary>
            /// The mean value for the actual bundle size.
            /// </summary>
            public double BundleSizeMu = 8;
            /// <summary>
            /// The standard deviation for the actual bundle size.
            /// </summary>
            public double BundleSizeSigma = 2;
            /// <summary>
            /// The minimal value for the actual bundle size.
            /// </summary>
            public int BundleSizeLB = 4;
            /// <summary>
            /// The maximal value for the actual bundle size.
            /// </summary>
            public int BundleSizeUB = 12;
            /// <summary>
            /// The relative amount of given co-weights.
            /// </summary>
            public double GivenCoWeights = 0;
        }

        /// <summary>
        /// Generates a complete configuration for the simple item generator.
        /// </summary>
        /// <param name="preConfig">The pre-configuration defining characteristics of the actual configuration.</param>
        /// <returns>The complete configuration.</returns>
        public static SimpleItemGeneratorConfiguration GenerateSimpleItemConfiguration(SimpleItemGeneratorPreConfiguration preConfig)
        {
            // Init
            SimpleItemGeneratorConfiguration config = new SimpleItemGeneratorConfiguration()
            {
                DefaultWeight = preConfig.DefaultWeight,
                DefaultCoWeight = preConfig.DefaultCoWeight,
                ProbToUseCoWeight = preConfig.ProbToUseCoWeight
            };
            RandomizerSimple randomizer = new RandomizerSimple(0);
            List<SimpleItemDescription> itemDescriptions = new List<SimpleItemDescription>();
            List<Tuple<SimpleItemDescription, double>> itemDescriptionWeights = new List<Tuple<SimpleItemDescription, double>>();
            List<Tuple<SimpleItemDescription, SimpleItemDescription, double>> itemDescriptionCoWeights = new List<Tuple<SimpleItemDescription, SimpleItemDescription, double>>();

            // Add comment
            config.Description = string.Join(",", typeof(SimpleItemGeneratorPreConfiguration).GetFields().Select(f =>
            {
                string fieldValue;
                if (f.GetValue(preConfig) is double)
                    fieldValue = ((double)f.GetValue(preConfig)).ToString(IOConstants.FORMATTER);
                else
                    fieldValue = f.GetValue(preConfig).ToString();
                return f.Name + "=" + fieldValue;
            }));

            // Generate a set of item-descriptions
            for (int i = 0; i < preConfig.ItemDescriptionCount; i++)
            {
                // Generate next item
                SimpleItemDescription description = new SimpleItemDescription(null) { ID = i };
                // Randomly weight the item
                double itemDescriptionWeight = 0;
                switch (preConfig.WeightDistributionType)
                {
                    case ItemDescriptionWeightDistributionType.Normal:
                        itemDescriptionWeight = randomizer.NextNormalDouble(preConfig.ItemWeightMu, preConfig.ItemWeightSigma, preConfig.ItemWeightLB, preConfig.ItemWeightUB);
                        break;
                    case ItemDescriptionWeightDistributionType.Uniform:
                        itemDescriptionWeight = randomizer.NextDouble(preConfig.ItemWeightLB, preConfig.ItemWeightUB);
                        break;
                    default: throw new ArgumentException("Unknown distribution: " + preConfig.WeightDistributionType);
                }
                description.Weight = itemDescriptionWeight;
                // Randomly determine bundle size of the item
                if (preConfig.SupplyBundleSize)
                {
                    int itemDescriptionBundleSize = 0;
                    switch (preConfig.BundleSizeDistributionType)
                    {
                        case ItemDescriptionBundleSizeDistributionType.Normal:
                            itemDescriptionBundleSize = randomizer.NextNormalInt(preConfig.BundleSizeMu, preConfig.BundleSizeSigma, preConfig.BundleSizeLB, preConfig.BundleSizeUB);
                            break;
                        case ItemDescriptionBundleSizeDistributionType.Uniform:
                            itemDescriptionBundleSize = randomizer.NextInt(preConfig.BundleSizeLB, preConfig.BundleSizeUB + 1);
                            break;
                        default: throw new ArgumentException("Unknown distribution: " + preConfig.BundleSizeDistributionType);
                    }
                    description.BundleSize = itemDescriptionBundleSize;
                }
                // Add a random hue value to distinguish the item from others
                description.Hue = randomizer.NextDouble(360);
                // Add it
                itemDescriptions.Add(description);
                // Set a weight for the probability of the item
                double weight = 0;
                switch (preConfig.ProbWeightDistributionType)
                {
                    case ItemDescriptionProbabilityWeightDistributionType.Constant:
                        weight = preConfig.ProbabilityWeightConstant;
                        break;
                    case ItemDescriptionProbabilityWeightDistributionType.Uniform:
                        weight = randomizer.NextDouble(preConfig.ProbabilityWeightUniformMin, preConfig.ProbabilityWeightUniformMax);
                        break;
                    case ItemDescriptionProbabilityWeightDistributionType.Normal:
                        weight = randomizer.NextNormalDouble(preConfig.ProbabilityWeightNormalMu, preConfig.ProbabilityWeightNormalSigma, preConfig.ProbabilityWeightLB, preConfig.ProbabilityWeightUB);
                        break;
                    case ItemDescriptionProbabilityWeightDistributionType.Exponential:
                        weight = randomizer.NextExponentialDouble(preConfig.ProbabilityWeightExpLambda, preConfig.ProbabilityWeightLB, preConfig.ProbabilityWeightUB);
                        break;
                    case ItemDescriptionProbabilityWeightDistributionType.Gamma:
                        weight = randomizer.NextGammaDouble(preConfig.ProbabilityWeightGammaK, preConfig.ProbabilityWeightGammaTheta, preConfig.ProbabilityWeightLB, preConfig.ProbabilityWeightUB);
                        break;
                    default: throw new ArgumentException("Unknown distribution: " + preConfig.ProbWeightDistributionType);
                }
                itemDescriptionWeights.Add(new Tuple<SimpleItemDescription, double>(description, weight));
            }

            // Equally distribute items over two-dimensional space
            Dictionary<SimpleItemDescription, Tuple<double, double>> itemDescriptionPosition = new Dictionary<SimpleItemDescription, Tuple<double, double>>();
            foreach (var description in itemDescriptions)
                itemDescriptionPosition[description] = new Tuple<double, double>(randomizer.NextDouble(), randomizer.NextDouble());

            // Plot the distribution for reference
            GnuPlotter.Plot2DPoints(
                "itemdistribution",
                new List<Tuple<string, IEnumerable<Tuple<double, double>>>>() {
                    new Tuple<string, IEnumerable<Tuple<double, double>>>("Item locations in 2D", itemDescriptionPosition.Values) },
                "item distribution for co-probability emulation");

            // Set conditional weights
            double maxDistance = Distances.CalculateEuclid(0, 0, 1, 1);
            foreach (var description in itemDescriptions.OrderBy(d => randomizer.NextDouble()).Take((int)(itemDescriptions.Count * preConfig.GivenCoWeights)))
                foreach (var otherDescription in itemDescriptions.OrderBy(d => randomizer.NextDouble()).Take((int)(itemDescriptions.Count * preConfig.GivenCoWeights)))
                    itemDescriptionCoWeights.Add(new Tuple<SimpleItemDescription, SimpleItemDescription, double>(
                        description,
                        otherDescription,
                        maxDistance - Distances.CalculateEuclid(
                            itemDescriptionPosition[description].Item1, itemDescriptionPosition[description].Item2,
                            itemDescriptionPosition[otherDescription].Item1, itemDescriptionPosition[otherDescription].Item2)));

            // Submit all
            config.ItemDescriptions = itemDescriptions.Select(d => new Skvp<int, double>() { Key = d.ID, Value = d.Hue }).ToList();
            config.ItemDescriptionWeights = itemDescriptions.Select(d => new Skvp<int, double>() { Key = d.ID, Value = d.Weight }).ToList();
            if (preConfig.SupplyBundleSize)
                config.ItemDescriptionBundleSizes = itemDescriptions.Select(d => new Skvp<int, int>() { Key = d.ID, Value = d.BundleSize }).ToList();
            config.ItemWeights = itemDescriptionWeights.Select(d => new Skvp<int, double>() { Key = d.Item1.ID, Value = d.Item2 }).ToList();
            config.ItemCoWeights = itemDescriptionCoWeights.Select(d => new Skkvt<int, int, double>() { Key1 = d.Item1.ID, Key2 = d.Item2.ID, Value = d.Item3 }).ToList();

            // Name it
            config.Name = config.GetMetaInfoBasedName();

            // Return it
            return config;
        }

        #endregion
    }
}
