using RAWSimO.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Randomization
{
    /// <summary>
    /// Exposes some helping randomization functionality.
    /// </summary>
    public class RandomizerHelper
    {
        /// <summary>
        /// Randomly selects an item from a dictionary based on a certain probability per item.
        /// </summary>
        /// <typeparam name="T">The type of the item to select.</typeparam>
        /// <param name="probabilities">The items with the given probabilities.</param>
        /// <param name="randomizer">The randomizer instance to use.</param>
        /// <returns>A randomly selected item.</returns>
        public static T DrawRandomly<T>(IDictionary<T, double> probabilities, IRandomizer randomizer)
        {
            // Get random value
            double randomValue = randomizer.NextDouble();
            // Select one just in case
            T selectedElement = probabilities.Keys.OrderBy(e => probabilities[e]).First();
            // Randomly select an element
            foreach (var prob in probabilities.OrderBy(p => p.Value))
            {
                if (prob.Value > randomValue)
                {
                    selectedElement = prob.Key; break;
                }
                randomValue -= prob.Value;
            }
            // Return the randomly selected element
            return selectedElement;
        }
    }
}
