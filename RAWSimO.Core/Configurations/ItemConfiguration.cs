using RAWSimO.Core.IO;
using RAWSimO.Core.Items;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Configurations
{
    /// <summary>
    /// A configuration to use when colored letters are used as items.
    /// </summary>
    public class ColoredWordConfiguration
    {
        /// <summary>
        /// Parameter-less constructor mainly used by the xml-serializer.
        /// </summary>
        public ColoredWordConfiguration() { }
        /// <summary>
        /// Constructor that generates default values for all fields.
        /// </summary>
        /// <param name="param">Not used.</param>
        public ColoredWordConfiguration(DefaultConstructorIdentificationClass param) : this()
        {
            ColorProbabilities = new List<Skvp<LetterColors, double>>() {
                new Skvp<LetterColors, double>() { Key = LetterColors.Blue, Value = 1.0 / 3.0 },
                new Skvp<LetterColors, double>() { Key = LetterColors.Green, Value = 1.0 / 3.0 },
                new Skvp<LetterColors, double>() { Key = LetterColors.Red, Value = 1.0 / 3.0} };
        }
        /// <summary>
        /// A file containing a list of words used to generate colored words used as orders.
        /// </summary>
        public string WordFile = "SmallJargon.txt";
        /// <summary>
        /// The probabilities of the different colors.
        /// </summary>
        public List<Skvp<LetterColors, double>> ColorProbabilities;
    }

    /// <summary>
    /// A configuration to use when ID-based items are used that can have meaningless coloring (to distinguish them).
    /// </summary>
    public class SimpleItemConfiguration
    {
        /// <summary>
        /// The resource file containing the config for the generator.
        /// </summary>
        public string GeneratorConfigFile = "Mu-1000.xgenc";
    }
    /// <summary>
    /// A configuration that is used when simple items are generated randomly.
    /// </summary>
    public class SimpleItemGeneratorConfiguration
    {
        /// <summary>
        /// The name of this generator config.
        /// </summary>
        public string Name;
        /// <summary>
        /// Some description of the file.
        /// </summary>
        public string Description;
        /// <summary>
        /// The default weight to use when no weight is present for the given item.
        /// </summary>
        public double DefaultWeight;
        /// <summary>
        /// The default weight to use when no weight is present for the given combination of items.
        /// </summary>
        public double DefaultCoWeight;
        /// <summary>
        /// The probability of using a co-weight for generating the next item instead of just generating a non-related item based on the single item probabilities.
        /// </summary>
        public double ProbToUseCoWeight;
        /// <summary>
        /// All item descriptions that can be generated with this configuration with their respective ID and hue.
        /// </summary>
        public List<Skvp<int, double>> ItemDescriptions;
        /// <summary>
        /// Specifies for each item description how much space is consumed by one unit of it.
        /// </summary>
        public List<Skvp<int, double>> ItemDescriptionWeights;
        /// <summary>
        /// Specifies for each item description the size of a bundle when replenishing it.
        /// </summary>
        public List<Skvp<int, int>> ItemDescriptionBundleSizes;
        /// <summary>
        /// The weights of the items. These will be transformed into probabilities to use in order to generate new item-bundles and also the first item of an order.
        /// </summary>
        public List<Skvp<int, double>> ItemWeights;
        /// <summary>
        /// The weights of the item combinations. 
        /// These will be transformed into probabilities and used when one item was generated for an order, but another one shall be generated. 
        /// A higher weight results in a higher probability that the given item is generated next for the current order.
        /// </summary>
        public List<Skkvt<int, int, double>> ItemCoWeights;
        /// <summary>
        /// Returns a simple describing name for this config.
        /// </summary>
        /// <returns>A string that can be used as a config name.</returns>
        public string GetMetaInfoBasedName()
        {
            string delimiter = "-";
            return "ItemGen" + delimiter + ItemDescriptions.Count + delimiter + ItemWeights.Count + delimiter + ProbToUseCoWeight.ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + delimiter + ItemCoWeights.Count;
        }
    }
}
