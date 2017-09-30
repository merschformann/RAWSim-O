using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Toolbox
{
    /// <summary>
    /// Extension methods to help determine the best candidate while iterating.
    /// </summary>
    public class BestCandidateSelector
    {
        /// <summary>
        /// Creates a new best candidate selection support object.
        /// </summary>
        /// <param name="max">Indicates whether the best value is the maximal one or the minimal one.</param>
        /// <param name="scorers">The scoring functions.</param>
        public BestCandidateSelector(bool max, params Func<double>[] scorers)
        {
            _max = max;
            _scorers = scorers;
            _currentValue = new double[scorers.Length];
            _bestScores = Enumerable.Repeat(double.MinValue, scorers.Length).ToArray();
        }

        /// <summary>
        /// Indicates whether the best candidate is the one with the lowest or the highest value.
        /// </summary>
        private bool _max;
        /// <summary>
        /// Indicates whether a first candidate was found at all.
        /// </summary>
        private bool _assigned = false;
        /// <summary>
        /// Contains all scoring functions.
        /// </summary>
        private Func<double>[] _scorers;
        /// <summary>
        /// Contains all current values for the different scores. This is just a helper variable.
        /// </summary>
        private double[] _currentValue;
        /// <summary>
        /// Contains the current best score per scoring function.
        /// </summary>
        private double[] _bestScores;
        /// <summary>
        /// Returns the current best scores per scoring function.
        /// </summary>
        public double[] BestScores { get { return _bestScores; } }

        /// <summary>
        /// Updates the current set of scorers.
        /// </summary>
        /// <param name="scorers">The scorers in the order of their assessment for determining the best candidate and breaking ties.</param>
        public void SetScorers(params Func<double>[] scorers) { _scorers = scorers; }

        /// <summary>
        /// Recycles this candidate selector by re-initializing it.
        /// </summary>
        public void Recycle()
        {
            _assigned = false;
            for (int i = 0; i < _scorers.Length; i++)
            {
                if (_max)
                {
                    _currentValue[i] = double.MinValue;
                    _bestScores[i] = double.MinValue;
                }
                else
                {
                    _currentValue[i] = double.MaxValue;
                    _bestScores[i] = double.MaxValue;
                }
            }
        }

        /// <summary>
        /// Reassess all scorers. It is assumed that the scoring functions outside this one ensure a valid assessment of the current context.
        /// </summary>
        /// <returns>Returns <code>true</code> if a new best was found, <code>false</code> otherwise.</returns>
        public bool Reassess() { return _max ? ReassessMax() : ReassessMin(); }

        /// <summary>
        /// Reassess all scorers. It is assumed that the scoring functions outside this one ensure a valid assessment of the current context.
        /// </summary>
        /// <returns>Returns <code>true</code> if a new best was found, <code>false</code> otherwise.</returns>
        private bool ReassessMax()
        {
            bool better = false;
            // Assess using all scorers
            for (int scorerIndex = 0; scorerIndex < _scorers.Length; scorerIndex++)
            {
                // Obtain value for the current element
                _currentValue[scorerIndex] = _scorers[scorerIndex]();
                // If we already confirmed a new best one just keep iterating until all best values are updated for the new candidate
                if (better)
                {
                    // Update the value for the current scorer index for the new best one
                    _bestScores[scorerIndex] = _currentValue[scorerIndex];
                    continue;
                }
                // Check for new maximum
                else if ((_bestScores[scorerIndex] < _currentValue[scorerIndex]) || (!_assigned))
                {
                    // New maximum found - store value for current scorer and the item itself - also indicate that we found a better one
                    _assigned = true;
                    _bestScores[scorerIndex] = _currentValue[scorerIndex];
                    better = true;
                }
                else if (_bestScores[scorerIndex] == _currentValue[scorerIndex])
                {
                    // The element is equal regarding this score - we need to go on with the examination of this element using the next tie breaker
                    // Also: no need to update the max - we have equality for this index
                    continue;
                }
                else
                {
                    // The element is not better than the current best
                    break;
                }
            }
            // Return result
            return better;
        }

        /// <summary>
        /// Reassess all scorers. It is assumed that the scoring functions outside this one ensure a valid assessment of the current context.
        /// </summary>
        /// <returns>Returns <code>true</code> if a new best was found, <code>false</code> otherwise.</returns>
        private bool ReassessMin()
        {
            bool better = false;
            // Assess using all scorers
            for (int scorerIndex = 0; scorerIndex < _scorers.Length; scorerIndex++)
            {
                // Obtain value for the current element
                _currentValue[scorerIndex] = _scorers[scorerIndex]();
                // If we already confirmed a new best one just keep iterating until all best values are updated for the new candidate
                if (better)
                {
                    // Update the value for the current scorer index for the new best one
                    _bestScores[scorerIndex] = _currentValue[scorerIndex];
                    continue;
                }
                // Check for new minimum
                else if ((_bestScores[scorerIndex] > _currentValue[scorerIndex]) || (!_assigned))
                {
                    // New minimum found - store value for current scorer and the item itself - also indicate that we found a better one
                    _assigned = true;
                    _bestScores[scorerIndex] = _currentValue[scorerIndex];
                    better = true;
                }
                else if (_bestScores[scorerIndex] == _currentValue[scorerIndex])
                {
                    // The element is equal regarding this score - we need to go on with the examination of this element using the next tie breaker
                    // Also: no need to update the max - we have equality for this index
                    continue;
                }
                else
                {
                    // The element is not better than the current best
                    break;
                }
            }
            // Return result
            return better;
        }
    }
}
