using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Toolbox
{
    /// <summary>
    /// A bidirectional dictonary containing tuples.
    /// </summary>
    /// <typeparam name="TFirst">The first type of the tuples.</typeparam>
    /// <typeparam name="TSecond">The second type of the tuples.</typeparam>
    public class BiDictionary<TFirst, TSecond> : IEnumerable<KeyValuePair<TFirst, TSecond>>
    {
        /// <summary>
        /// Storage: first -> second
        /// </summary>
        private IDictionary<TFirst, TSecond> _firstToSecond = new Dictionary<TFirst, TSecond>();
        /// <summary>
        /// Storage: second -> first
        /// </summary>
        private IDictionary<TSecond, TFirst> _secondToFirst = new Dictionary<TSecond, TFirst>();

        /// <summary>
        /// Gets the tuple partner or sets a new tuple (replacing a present one, if it exists).
        /// </summary>
        /// <param name="first">The first element of the tuple.</param>
        /// <returns>The second element of the tuple.</returns>
        public TSecond this[TFirst first] { get { return _firstToSecond[first]; } set { _firstToSecond[first] = value; _secondToFirst[value] = first; } }
        /// <summary>
        /// Gets the tuple partner or sets a new tuple (replacing a present one, if it exists).
        /// </summary>
        /// <param name="second">The second element of the tuple.</param>
        /// <returns>The first element of the tuple.</returns>
        public TFirst this[TSecond second] { get { return _secondToFirst[second]; } set { _secondToFirst[second] = value; _firstToSecond[value] = second; } }

        /// <summary>
        /// Adds an entry.
        /// </summary>
        /// <param name="first">The first element of the tuple.</param>
        /// <param name="second">The second element of the tuple.</param>
        public void Add(TFirst first, TSecond second)
        {
            if (_firstToSecond.ContainsKey(first) || _secondToFirst.ContainsKey(second))
                throw new Exception("Duplicate Key");

            //add to dict
            _firstToSecond.Add(first, second);
            _secondToFirst.Add(second, first);
        }
        /// <summary>
        /// Remove the pair with the given first entry.
        /// </summary>
        /// <param name="first">The first entry of the pair to remove.</param>
        public void Remove(TFirst first)
        {
            // If contained, remove
            if (_firstToSecond.ContainsKey(first))
            {
                // Remove from both
                _secondToFirst.Remove(_firstToSecond[first]);
                _firstToSecond.Remove(first);
            }
        }
        /// <summary>
        /// Remove the pair with the given second entry.
        /// </summary>
        /// <param name="second">The second entry of the pair to remove.</param>
        public void Remove(TSecond second)
        {
            // If contained, remove
            if (_secondToFirst.ContainsKey(second))
            {
                // Remove from both
                _firstToSecond.Remove(_secondToFirst[second]);
                _secondToFirst.Remove(second);
            }
        }

        /// <summary>
        /// The overall count of available elements.
        /// </summary>
        public int Count { get { return _firstToSecond.Count; } }

        /// <summary>
        /// Get only the first elements of all tuples.
        /// </summary>
        /// <returns>Only the first elements of all tuples.</returns>
        public ICollection<TFirst> ValuesFirst { get { return _firstToSecond.Keys; } }
        /// <summary>
        /// Get only the second elements of all tuples.
        /// </summary>
        /// <returns>Only the second elements of all tuples.</returns>
        public ICollection<TSecond> ValuesSecond { get { return _secondToFirst.Keys; } }

        /// <summary>
        /// Gets the enumerator of this collection.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<KeyValuePair<TFirst, TSecond>> GetEnumerator() { return _firstToSecond.GetEnumerator(); }
        /// <summary>
        /// Gets the enumerator of this collection.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator() { return _firstToSecond.GetEnumerator(); }
    }
}
