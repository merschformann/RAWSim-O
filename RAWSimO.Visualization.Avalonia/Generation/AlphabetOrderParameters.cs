using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using RAWSimO.Core.Generator;
using RAWSimO.Core.IO;
using RAWSimO.Core.Items;

namespace RAWSimO.Visualization.Avalonia.Generation
{
    /// <summary>
    /// Parameters for generating an order list based on colored words ("alphabet" mode).
    /// Edited via the generic config editor, the defaults match the WPF visualization.
    /// </summary>
    public sealed class AlphabetOrderParameters
    {
        [Description("The word file to use (searched in the working directory and the resource directories).")]
        public string WordFile = "Smalljargon.txt";
        [Description("The earliest time at which orders are placed.")]
        public double TimeMin = 0;
        [Description("The latest time at which orders are placed.")]
        public double TimeMax = 3600;
        [Description("The seed to use.")]
        public int Seed = 0;
        [Description("The number of orders to generate.")]
        public int OrderCount = 100;
        [Description("The minimal number of items per position.")]
        public int PositionMin = 1;
        [Description("The maximal number of items per position.")]
        public int PositionMax = 1;
        [Description("The relative amount of bundles to generate (compared to the items necessary for the orders).")]
        public double BundlesRelativeAmount = 1.1;
        [Description("The minimal number of items per bundle.")]
        public int BundleSizeMin = 8;
        [Description("The maximal number of items per bundle.")]
        public int BundleSizeMax = 18;
        [Description("The probability of red letters.")]
        public double ProbabilityRed = 0.5;
        [Description("The probability of green letters.")]
        public double ProbabilityGreen = 0.3;
        [Description("The probability of blue letters.")]
        public double ProbabilityBlue = 0.2;
        [Description("The probability of yellow letters.")]
        public double ProbabilityYellow = 0;
        [Description("The minimal weight of an item.")]
        public double WeightMin = 5;
        [Description("The maximal weight of an item.")]
        public double WeightMax = 5;

        /// <summary>
        /// Generates the order list.
        /// </summary>
        public OrderList Generate()
        {
            var colorProbabilities = new Dictionary<LetterColors, double>();
            if (ProbabilityRed > 0) colorProbabilities[LetterColors.Red] = ProbabilityRed;
            if (ProbabilityGreen > 0) colorProbabilities[LetterColors.Green] = ProbabilityGreen;
            if (ProbabilityBlue > 0) colorProbabilities[LetterColors.Blue] = ProbabilityBlue;
            if (ProbabilityYellow > 0) colorProbabilities[LetterColors.Yellow] = ProbabilityYellow;
            LetterColors[] colors = colorProbabilities.Keys.OrderBy(c => colorProbabilities[c]).ToArray();
            string wordFile = IOHelper.FindResourceFile(WordFile, ".");
            return OrderGenerator.GenerateOrders(wordFile, colors, colorProbabilities, Seed, TimeMin, TimeMax, OrderCount,
                WeightMin, WeightMax, PositionMin, PositionMax, BundleSizeMin, BundleSizeMax, BundlesRelativeAmount);
        }
    }
}
