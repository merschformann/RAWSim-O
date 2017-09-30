using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Interfaces
{
    /// <summary>
    /// The basic interface for a randomization object.
    /// </summary>
    public interface IRandomizer
    {
        /// <summary>
        /// Returns a random <code>int</code> number within the interval [0,<code>maxValue</code>).
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound of the interval.</param>
        /// <returns>A random number within the specified interval.</returns>
        int NextInt(int maxValue);

        /// <summary>
        /// Returns a random <code>int</code> number within the interval [<code>minValue</code>,<code>maxValue</code>).
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the interval.</param>
        /// <param name="maxValue">The exclusive upper bound of the interval.</param>
        /// <returns>A random number within the specified interval.</returns>
        int NextInt(int minValue, int maxValue);

        /// <summary>
        /// Returns the next normally distributed random number according to the given mean value and standard deviation.
        /// </summary>
        /// <param name="mean">The mean of the normal distribution.</param>
        /// <param name="std">The standard deviation of the normal distribution.</param>
        /// <returns>The next normally ditributed random value.</returns>
        int NextNormalInt(double mean, double std);

        /// <summary>
        /// Returns the next normally distributed random number according to the given mean value and standard deviation.
        /// </summary>
        /// <param name="mean">The mean of the normal distribution.</param>
        /// <param name="std">The standard deviation of the normal distribution.</param>
        /// <param name="minValue">The minimal value.</param>
        /// <param name="maxValue">The maximal value.</param>
        /// <returns>The next normally ditributed random value.</returns>
        int NextNormalInt(double mean, double std, int minValue, int maxValue);

        /// <summary>
        /// Returns a random <code>double</code> number within the interval [0,1).
        /// </summary>
        /// <returns>A random number from the interval [0,1).</returns>
        double NextDouble();

        /// <summary>
        /// Returns a random <code>double</code> number within the interval [0,<code>maxValue</code>).
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound of the interval.</param>
        /// <returns>A random number within the specified interval.</returns>
        double NextDouble(double maxValue);

        /// <summary>
        /// Returns a random <code>double</code> number within the interval [<code>minValue</code>,<code>maxValue</code>).
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the interval.</param>
        /// <param name="maxValue">The exclusive upper bound of the interval.</param>
        /// <returns>A random number within the specified interval.</returns>
        double NextDouble(double minValue, double maxValue);

        /// <summary>
        /// Returns the next normally distributed random number according to the given mean value and standard deviation.
        /// </summary>
        /// <param name="mean">The mean of the normal distribution.</param>
        /// <param name="std">The standard deviation of the normal distribution.</param>
        /// <returns>The next normally ditributed random value.</returns>
        double NextNormalDouble(double mean, double std);

        /// <summary>
        /// Returns the next normally distributed random number according to the given mean value and standard deviation.
        /// </summary>
        /// <param name="mean">The mean of the normal distribution.</param>
        /// <param name="std">The standard deviation of the normal distribution.</param>
        /// <param name="lowerBound">The lower bound for the value.</param>
        /// <param name="upperBound">The upper bound for the value.</param>
        /// <returns>The next normally ditributed random value.</returns>
        double NextNormalDouble(double mean, double std, double lowerBound, double upperBound);

        /// <summary>
        /// Returns the next variable drawn from an exponential distribution with the given parameters.
        /// </summary>
        /// <param name="lambda">The lambda parameter of the distribution.</param>
        /// <returns>The next random number drawn from the distribution with the given parameters.</returns>
        double NextExponentialDouble(double lambda);

        /// <summary>
        /// Returns the next variable drawn from an exponential distribution with the given parameters.
        /// </summary>
        /// <param name="lambda">The lambda parameter of the distribution.</param>
        /// <param name="lowerBound">All numbers below the lower bound will be dropped.</param>
        /// <param name="upperBound">All numbers below the upper bound will be dropped.</param>
        /// <returns>The next random number drawn from the distribution with the given parameters.</returns>
        double NextExponentialDouble(double lambda, double lowerBound, double upperBound);

        /// <summary>
        /// Returns the next variable drawn from a gamma distribution with the given parameters.
        /// Note: "A simple method for generating Gamma variables" by George Marsaglia and Wai Wan Tsang
        /// </summary>
        /// <param name="k">The k parameter of the distribution.</param>
        /// <param name="theta">The theta parameter of the distribution.</param>
        /// <returns>The next random number drawn from the distribution with the given parameters.</returns>
        double NextGammaDouble(double k, double theta);

        /// <summary>
        /// Returns the next variable drawn from a gamma distribution with the given parameters.
        /// Note: "A simple method for generating Gamma variables" by George Marsaglia and Wai Wan Tsang
        /// </summary>
        /// <param name="k">The k parameter of the distribution.</param>
        /// <param name="theta">The theta parameter of the distribution.</param>
        /// <param name="lowerBound">All numbers below the lower bound will be dropped.</param>
        /// <param name="upperBound">All numbers below the upper bound will be dropped.</param>
        /// <returns>The next random number drawn from the distribution with the given parameters.</returns>
        double NextGammaDouble(double k, double theta, double lowerBound, double upperBound);
    }
}
