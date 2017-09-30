using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Statistics
{
    /// <summary>
    /// Supplies some helping methods for the statistics.
    /// </summary>
    public class StatisticsHelper
    {
        /// <summary>
        /// Updates certain statistical values.
        /// </summary>
        /// <param name="count">The number of datapoints.</param>
        /// <param name="mean">The current average.</param>
        /// <param name="variance">The variance within the values.</param>
        /// <param name="min">The min value.</param>
        /// <param name="max">The max value.</param>
        /// <param name="sum">The sum of the values.</param>
        /// <param name="newValue">The new values / datapoint.</param>
        public static void UpdateAvgVarData(ref int count, ref double mean, ref double variance, ref double min, ref double max, ref double sum, double newValue)
        {
            double previousMean = mean;
            mean = mean + (newValue - mean) / (count + 1);
            variance = count == 0 ?
                0 :
                (1 - 1.0 / count) * variance + (count + 1) * Math.Pow(previousMean - mean, 2);
            max = newValue > max ? newValue : max;
            min = newValue < min ? newValue : min;
            count++;
            sum += newValue;
        }
        /// <summary>
        /// Gets the lower quartile value of a list of values.
        /// </summary>
        /// <param name="values">The list of values.</param>
        /// <returns>The lower quartile.</returns>
        public static double GetLowerQuartile(IEnumerable<double> values)
        {
            // Calculate position
            int position = (int)(values.Count() * (1.0 / 4.0)); // In case of two quantiles return the lower quantile
            // Order the values and return the element at the calculated position
            return values.OrderBy(v => v).ElementAt(position);
        }
        /// <summary>
        /// Gets the median value of a list of values.
        /// </summary>
        /// <param name="values">The list of values.</param>
        /// <returns>The median.</returns>
        public static double GetMedian(IEnumerable<double> values)
        {
            // Calculate position
            int position = (int)(values.Count() * (2.0 / 4.0)); // In case of two quantiles return the lower quantile
            // Order the values and return the element at the calculated position
            return values.OrderBy(v => v).ElementAt(position);
        }
        /// <summary>
        /// Gets the upper quartile value of a list of values.
        /// </summary>
        /// <param name="values">The list of values.</param>
        /// <returns>The upper quartile.</returns>
        public static double GetUpperQuartile(IEnumerable<double> values)
        {
            // Calculate position
            int position = (int)(values.Count() * (3.0 / 4.0)); // In case of two quantiles return the lower quantile
            // Order the values and return the element at the calculated position
            return values.OrderBy(v => v).ElementAt(position);
        }
    }
}
