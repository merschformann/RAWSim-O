using RAWSimO.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Randomization
{
    /// <summary>
    /// Determines the mode of the poisson process.
    /// </summary>
    public enum PoissonMode
    {
        /// <summary>
        /// Generates random numbers according to a homogeneous poisson process.
        /// </summary>
        Simple,
        /// <summary>
        /// Generates random numbers according to an inhomogeneous (time-dependent) poisson process.
        /// </summary>
        TimeDependent,
        /// <summary>
        /// Switches between a high and a low order / bundle rate according to another poisson process.
        /// </summary>
        HighLow,
    }
    /// <summary>
    /// Distinguishes different types for distorting the rate parameter based on live simulation meta information.
    /// </summary>
    public enum PoissonDistortionType
    {
        /// <summary>
        /// The rate is not distorted at all.
        /// </summary>
        None,
        /// <summary>
        /// The rate is distorted by the number of activated pick stations.
        /// </summary>
        PickStationsActivated,
        /// <summary>
        /// The rate is distorted by the number of activated replenishment stations.
        /// </summary>
        ReplenishmentStationsActivated,
    }

    /// <summary>
    /// A simple poisson generator implementation. (extension of the idea of <see href="http://preshing.com/20111007/how-to-generate-random-timings-for-a-poisson-process/"/>)
    /// </summary>
    public class PoissonGenerator
    {
        /// <summary>
        /// Creates a new instance of the poisson generator in simple mode.
        /// </summary>
        /// <param name="randomizer">The randomizer to use.</param>
        /// <param name="rate">The rate of the poisson generator.</param>
        public PoissonGenerator(IRandomizer randomizer, double rate)
        {
            Mode = PoissonMode.Simple;
            Randomizer = randomizer;
            Rate = rate;
        }
        /// <summary>
        /// Creates a new instance of the poisson generator in high/low mode.
        /// </summary>
        /// <param name="randomizer">The randomizer to use.</param>
        /// <param name="rate">The rate of the poisson generator.</param>
        /// <param name="rateHigh">The rate to use in high periods.</param>
        /// <param name="rateHighLowSwitch">The rate parameter to use to obtain the next switch from low to high period.</param>
        /// <param name="rateLowHighSwtich">The rate parameter to use to obtain the next switch from high to low period.</param>
        /// <param name="affiliation">The affiliation this generator belongs to.</param>
        /// <param name="logAction">An action used for logging the switches between high / low.</param>
        public PoissonGenerator(IRandomizer randomizer, double rate, double rateHigh, double rateLowHighSwtich, double rateHighLowSwitch, Action<string> logAction, string affiliation)
        {
            Mode = PoissonMode.HighLow;
            Randomizer = randomizer;
            Rate = rate;
            RateHigh = rateHigh;
            RateLowHighSwitch = rateLowHighSwtich;
            RateHighLowSwitch = rateHighLowSwitch;
            _logMessage = logAction;
            Affiliation = affiliation;
        }
        /// <summary>
        /// Creates a new instance of the poisson generator in time-dependent mode.
        /// </summary>
        /// <param name="randomizer">The randomizer to use.</param>
        /// <param name="maxTime">The time after which the time-dependent values loop.</param>
        /// <param name="timeDependentRateSteps">The time dependent rates.</param>
        public PoissonGenerator(IRandomizer randomizer, double maxTime, IEnumerable<KeyValuePair<double, double>> timeDependentRateSteps)
        {
            Mode = PoissonMode.TimeDependent;
            Randomizer = randomizer;
            MaxTime = maxTime;
            TimeDependentRateSteps = new List<double>(timeDependentRateSteps.Select(kvp => kvp.Key));
            TimeDependentRateSteps.Sort();
            if (TimeDependentRateSteps.First() != 0)
                throw new ArgumentException("The first element of the rate steps has to define the rate at the beginning of the maximal span, hence it has to be set to 0");
            TimeDependentRates = timeDependentRateSteps.ToDictionary(k => k.Key, v => v.Value);
        }

        /// <summary>
        /// Used to log messages.
        /// </summary>
        private Action<string> _logMessage;
        /// <summary>
        /// The affiliation of this generator (used to log messages).
        /// </summary>
        public string Affiliation { get; private set; }
        /// <summary>
        /// The random number generator that is used.
        /// </summary>
        public IRandomizer Randomizer { get; private set; }
        /// <summary>
        /// The mode this generator is using.
        /// </summary>
        public PoissonMode Mode { get; private set; }
        /// <summary>
        /// The rate parameter of this poisson generator.
        /// </summary>
        public double Rate { get; private set; }

        #region High low mode fields
        /// <summary>
        /// The rate parameter when in high mode.
        /// </summary>
        public double RateHigh { get; private set; }
        /// <summary>
        /// The rate parameter to use to obtain the next switch from low to high period.
        /// </summary>
        public double RateHighLowSwitch { get; private set; }
        /// <summary>
        /// The rate parameter to use to obtain the next switch from high to low period.
        /// </summary>
        public double RateLowHighSwitch { get; private set; }
        /// <summary>
        /// Indicates the next time a switch between high and low period is performed.
        /// </summary>
        public double NextHighLowSwitch { get { return Mode == PoissonMode.HighLow ? _nextHighLowSwitch : double.PositiveInfinity; } }
        /// <summary>
        /// Indicates the next time a switch between high and low period is performed.
        /// </summary>
        private double _nextHighLowSwitch = double.NegativeInfinity;
        /// <summary>
        /// Indicates whether we are currently in a low period.
        /// </summary>
        private bool _isLowPeriod = false;
        #endregion

        #region Time dependent mode fields
        /// <summary>
        /// The time after which the time-dependent rate loops.
        /// </summary>
        public double MaxTime { get; private set; }
        /// <summary>
        /// The time-steps at which the rate changes.
        /// </summary>
        public List<double> TimeDependentRateSteps { get; private set; }
        /// <summary>
        /// The rates depending on the current time.
        /// </summary>
        public Dictionary<double, double> TimeDependentRates { get; private set; }
        #endregion

        /// <summary>
        /// Translates a desired number of events that shall occur during a given timespan into the corresponding rate parameter to use for the poisson generator.
        /// </summary>
        /// <param name="timeFrame">The timespan during which the number of events shall occur.</param>
        /// <param name="eventsPerTimeFrame">The number of events per timespan.</param>
        /// <returns>The translated rate parameter.</returns>
        public static double TranslateIntoRateParameter(TimeSpan timeFrame, double eventsPerTimeFrame) { return eventsPerTimeFrame / timeFrame.TotalSeconds; }

        /// <summary>
        /// Generates the next poisson process value depending on the current time (if desired).
        /// </summary>
        /// <param name="currentTime">The current time in seconds (this is ignored if simple mode is used).</param>
        /// <param name="distortionFactor">A factor that is applied to the rate parameter, hence, it can be used to temporarily modify the rate by some factor.</param>
        /// <returns>The next time according to the poisson process (in seconds).</returns>
        public double Next(double currentTime, double distortionFactor = 1)
        {
            // Manage currently active period (high <-> low)
            if (Mode == PoissonMode.HighLow)
            {
                // See whether we need to switch between periods
                if (_nextHighLowSwitch <= currentTime)
                {
                    // TODO remove debug:
                    _logMessage("Poisson (" + Affiliation + "): Switching from " + (_isLowPeriod ? "low" : "high") + " to " + (_isLowPeriod ? "high" : "low"));
                    // Flip period indicator
                    _isLowPeriod = !_isLowPeriod;
                    // Obtain next switch time
                    _nextHighLowSwitch = currentTime + (_isLowPeriod ?
                        // We are now in low period -> use the low to high rate to obtain the next switch
                        -Math.Log(1.0 - Randomizer.NextDouble()) / RateLowHighSwitch :
                        // We are now in high period -> use the high to low rate to obtain the next switch
                        -Math.Log(1.0 - Randomizer.NextDouble()) / RateHighLowSwitch);
                }
            }
            // --> Obtain next event
            switch (Mode)
            {
                case PoissonMode.Simple: return -Math.Log(1.0 - Randomizer.NextDouble()) / (Rate * distortionFactor);
                case PoissonMode.TimeDependent:
                    {
                        // Close the loop if we are out of the original time-range
                        while (currentTime >= MaxTime)
                            currentTime -= MaxTime;
                        // Search for the range we are in
                        int index = TimeDependentRateSteps.BinarySearch(currentTime);
                        if (index < 0)
                            index = (~index) - 1;
                        // Calculate the value
                        return -Math.Log(1.0 - Randomizer.NextDouble()) / (TimeDependentRates[TimeDependentRateSteps[index]] * distortionFactor);
                    }
                case PoissonMode.HighLow:
                    return _isLowPeriod ?
                        -Math.Log(1.0 - Randomizer.NextDouble()) / (Rate * distortionFactor) :
                        -Math.Log(1.0 - Randomizer.NextDouble()) / (RateHigh * distortionFactor);
                default: throw new ArgumentException("Unknown poisson mode: " + Mode.ToString());
            }
        }

    }
}
