using RAWSimO.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Randomization
{
    /// <summary>
    /// A simple randomizer implementation.
    /// </summary>
    public class RandomizerSimple : IRandomizer
    {
        /// <summary>
        /// The random number generator used by this randomizer instance.
        /// </summary>
        private Random _randomizer;

        private bool _secondNormallyDistributedNumberIsCached = false;
        private double _secondNormallyDistributedNumberCached;

        /// <summary>
        /// Creates a new instance of this randomizer.
        /// </summary>
        /// <param name="seed">The seed to use.</param>
        public RandomizerSimple(int seed) { _randomizer = new Random(seed); }

        #region IRandomizer Members

        /// <summary>
        /// Returns a random <code>int</code> number within the interval [0,<code>maxValue</code>).
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound of the interval.</param>
        /// <returns>A random number within the specified interval.</returns>
        public int NextInt(int maxValue) { return _randomizer.Next(maxValue); }

        /// <summary>
        /// Returns a random <code>int</code> number within the interval [<code>minValue</code>,<code>maxValue</code>).
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the interval.</param>
        /// <param name="maxValue">The exclusive upper bound of the interval.</param>
        /// <returns>A random number within the specified interval.</returns>
        public int NextInt(int minValue, int maxValue) { return _randomizer.Next(minValue, maxValue); }

        /// <summary>
        /// Returns the next normally distributed random number according to the given mean value and standard deviation.
        /// </summary>
        /// <param name="mean">The mean of the normal distribution.</param>
        /// <param name="std">The standard deviation of the normal distribution.</param>
        /// <returns>The next normally ditributed random value.</returns>
        public int NextNormalInt(double mean, double std) { return (int)Math.Round(NextNormalDouble(mean, std)); }

        /// <summary>
        /// Returns the next normally distributed random number according to the given mean value and standard deviation.
        /// </summary>
        /// <param name="mean">The mean of the normal distribution.</param>
        /// <param name="std">The standard deviation of the normal distribution.</param>
        /// <param name="minValue">The minimal value.</param>
        /// <param name="maxValue">The maximal value.</param>
        /// <returns>The next normally ditributed random value.</returns>
        public int NextNormalInt(double mean, double std, int minValue, int maxValue) { return (int)Math.Round(NextNormalDouble(mean, std, minValue - 0.5, maxValue + 0.5 - double.Epsilon)); }

        /// <summary>
        /// Returns a random <code>double</code> number within the interval [0,1).
        /// </summary>
        /// <returns>A random number from the interval [0,1).</returns>
        public double NextDouble() { return _randomizer.NextDouble(); }

        /// <summary>
        /// Returns a random <code>double</code> number within the interval [0,<code>maxValue</code>).
        /// </summary>
        /// <param name="maxValue">The inclusive upper bound of the interval.</param>
        /// <returns>A random number within the specified interval.</returns>
        public double NextDouble(double maxValue) { return _randomizer.NextDouble() * maxValue; }

        /// <summary>
        /// Returns a random <code>double</code> number within the interval [<code>minValue</code>,<code>maxValue</code>).
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the interval.</param>
        /// <param name="maxValue">The exclusive upper bound of the interval.</param>
        /// <returns>A random number within the specified interval.</returns>
        public double NextDouble(double minValue, double maxValue) { return minValue + _randomizer.NextDouble() * (maxValue - minValue); }

        /// <summary>
        /// Returns the next normally distributed random number according to the given mean value and standard deviation.
        /// Implementation analog to the implementation at <see href="http://en.literateprograms.org/Box-Muller_transform_(C)"/>
        /// </summary>
        /// <param name="mean">The mean of the normal distribution.</param>
        /// <param name="std">The standard deviation of the normal distribution.</param>
        /// <returns>The next normally ditributed random value.</returns>
        public double NextNormalDouble(double mean, double std)
        {
            // See if we have a stored random number
            if (!_secondNormallyDistributedNumberIsCached)
            {
                // Calculate a new pair of random values
                double x, y, r;
                do
                {
                    x = 2.0 * _randomizer.NextDouble() - 1;
                    y = 2.0 * _randomizer.NextDouble() - 1;
                    r = x * x + y * y;
                }
                while (r == 0.0 || r > 1.0);
                // Apply Box-Muller transform
                double d = Math.Sqrt(-2.0 * Math.Log(r) / r);
                // Cache second number
                _secondNormallyDistributedNumberCached = y * d;
                _secondNormallyDistributedNumberIsCached = true;
                // Scale and translate to mean and sdtdev
                return x * d * std + mean;
            }
            else
            {
                // Use the already available cached value
                _secondNormallyDistributedNumberIsCached = false;
                return _secondNormallyDistributedNumberCached * std + mean;
            }
        }

        /// <summary>
        /// Returns the next normally distributed random number according to the given mean value and standard deviation.
        /// </summary>
        /// <param name="mean">The mean of the normal distribution.</param>
        /// <param name="std">The standard deviation of the normal distribution.</param>
        /// <param name="lowerBound">The lower bound for the value.</param>
        /// <param name="upperBound">The upper bound for the value.</param>
        /// <returns>The next normally ditributed random value.</returns>
        public double NextNormalDouble(double mean, double std, double lowerBound, double upperBound)
        {
            double result;
            // Simply draw random normally distributed numbers until one satisfies the bounds
            do
            {
                result = NextNormalDouble(mean, std);
            } while (result < lowerBound || result > upperBound);
            // Return it
            return result;
        }

        /// <summary>
        /// Returns the next variable drawn from an exponential distribution with the given parameters.
        /// </summary>
        /// <param name="lambda">The lambda parameter of the distribution.</param>
        /// <returns>The next random number drawn from the distribution with the given parameters.</returns>
        public double NextExponentialDouble(double lambda)
        {
            return -Math.Log(_randomizer.NextDouble()) / lambda;
        }

        /// <summary>
        /// Returns the next variable drawn from an exponential distribution with the given parameters.
        /// </summary>
        /// <param name="lambda">The lambda parameter of the distribution.</param>
        /// <param name="lowerBound">All numbers below the lower bound will be dropped.</param>
        /// <param name="upperBound">All numbers below the upper bound will be dropped.</param>
        /// <returns>The next random number drawn from the distribution with the given parameters.</returns>
        public double NextExponentialDouble(double lambda, double lowerBound, double upperBound)
        {
            double number = NextExponentialDouble(lambda);
            while (number < lowerBound || upperBound < number)
                number = NextExponentialDouble(lambda);
            return number;
        }

        /// <summary>
        /// Returns the next variable drawn from a gamma distribution with the given parameters.
        /// Note: "A simple method for generating Gamma variables" by George Marsaglia and Wai Wan Tsang
        /// </summary>
        /// <param name="k">The k parameter of the distribution.</param>
        /// <param name="theta">The theta parameter of the distribution.</param>
        /// <returns>The next random number drawn from the distribution with the given parameters.</returns>
        public double NextGammaDouble(double k, double theta)
        {
            if (k < 0)
                throw new ArgumentException("k cannot be smaller than zero!");
            if (theta < 0)
                throw new ArgumentException("theta cannot be smaller than zero!");
            if (k < 1)
                k++;
            double d = k - (1.0 / 3.0);
            double cc = 1 - Math.Sqrt(9.0 * d);
            double gammaVariate = -1.0;
            double u;
            double x = 0;
            double v = 0;
            bool generateAnotherV;
            bool gammaVariateStillToBeGenerated = true;
            while (gammaVariateStillToBeGenerated)
            {
                generateAnotherV = true;
                while (generateAnotherV)
                {
                    x = NextNormalDouble(0, 1);
                    v = Math.Pow((1.0 + cc * x), 3.0);
                    if (v > 0)
                        generateAnotherV = false;
                    else
                        generateAnotherV = true;
                }
                u = _randomizer.NextDouble();
                if (u < (1 - 0.0331 * Math.Pow(x, 4.0)))
                {
                    gammaVariate = d * v;
                    gammaVariateStillToBeGenerated = false;
                }
                else if (Math.Log(u) < 0.5 * Math.Pow(x, 2.0) + d * (1.0 - v + Math.Log(v)))
                {
                    gammaVariate = d * v;
                    gammaVariateStillToBeGenerated = false;
                }
            }
            //now you have a standard gammaVariate, that should be multiplied by theta to arrive at a real gammaVariate
            gammaVariate *= theta;
            return gammaVariate;
        }

        /// <summary>
        /// Returns the next variable drawn from a gamma distribution with the given parameters.
        /// Note: "A simple method for generating Gamma variables" by George Marsaglia and Wai Wan Tsang
        /// </summary>
        /// <param name="k">The k parameter of the distribution.</param>
        /// <param name="theta">The theta parameter of the distribution.</param>
        /// <param name="lowerBound">All numbers below the lower bound will be dropped.</param>
        /// <param name="upperBound">All numbers below the upper bound will be dropped.</param>
        /// <returns>The next random number drawn from the distribution with the given parameters.</returns>
        public double NextGammaDouble(double k, double theta, double lowerBound, double upperBound)
        {
            double number = NextGammaDouble(k, theta);
            while (number < lowerBound || upperBound < number)
                number = NextGammaDouble(k, theta);
            return number;
        }

        #endregion
    }
}
