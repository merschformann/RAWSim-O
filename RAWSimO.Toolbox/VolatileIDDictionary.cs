using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Toolbox
{
    /// <summary>
    /// Implements a key-value-pair as used by the volatile dictionary.
    /// </summary>
    /// <typeparam name="K">The type of the key.</typeparam>
    /// <typeparam name="V">The type of the value.</typeparam>
    public class VolatileKeyValuePair<K, V> where K : IExposeVolatileID
    {
        /// <summary>
        /// Creates a new key-value-pair.
        /// </summary>
        /// <param name="key">The key of the tuple.</param>
        /// <param name="value">The value of the tuple.</param>
        public VolatileKeyValuePair(K key, V value) { Key = key; Value = value; }
        /// <summary>
        /// The key.
        /// </summary>
        public K Key;
        /// <summary>
        /// The value.
        /// </summary>
        public V Value;
        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        /// <returns>A string representing this object.</returns>
        public override string ToString() { return Key.ToString() + "-" + Value.ToString(); }
    }
    /// <summary>
    /// Implements a faster (though fixed) version of a dictionary. Also: not all side-effects are taken care of for speed sakes.
    /// </summary>
    public class VolatileIDDictionary<K, V> : IEnumerable<VolatileKeyValuePair<K, V>>, IInflexibleDictionary<K, V>
        where K : IExposeVolatileID
    {
        /// <summary>
        /// All keys ordered by their ID.
        /// </summary>
        private List<VolatileKeyValuePair<K, V>> _entries;
        /// <summary>
        /// The storage that is used for the keys after the collection is sealed.
        /// </summary>
        private K[] _keyStorage;
        /// <summary>
        /// The storage that is used for the values after the collection is sealed.
        /// </summary>
        private VolatileKeyValuePair<K, V>[] _entryStorage;
        /// <summary>
        /// Creates a new volatile key dictionary from the given list of entries.
        /// </summary>
        /// <param name="entries">The finalized list of entries. No further entries can be added to the dictionary after creation.</param>
        public VolatileIDDictionary(IList<VolatileKeyValuePair<K, V>> entries)
        {
            if (entries.Any(e => e.Key.VolatileID < 0))
                throw new ArgumentException("Cannot use negative volatile IDs!");
            _keyStorage = new K[entries.Max(e => e.Key.VolatileID) + 1];
            _entryStorage = new VolatileKeyValuePair<K, V>[entries.Max(e => e.Key.VolatileID) + 1];
            foreach (var entry in entries)
            {
                _keyStorage[entry.Key.VolatileID] = entry.Key;
                _entryStorage[entry.Key.VolatileID] = entry;
            }
            _entries = entries.OrderBy(e => e.Key.VolatileID).ToList();
            if (_entries.Select(e => e.Key.VolatileID).Distinct().Count() != _entries.Count)
                throw new ArgumentException("Volatile IDs of the keys have to distinct!");
        }
        /// <summary>
        /// Gets or sets the element at the given key.
        /// </summary>
        /// <param name="key">The key of the element to get / set.</param>
        /// <returns>The element at the given key.</returns>
        public V this[K key]
        {
            get
            {
                // Get the value at the given index (depending on the current state)
                return _entryStorage[key.VolatileID].Value;
            }
            set
            {
                // Set the value
                _entryStorage[key.VolatileID].Value = value;
            }
        }
        /// <summary>
        /// All keys stored in this dictionary.
        /// </summary>
        public IEnumerable<K> Keys { get { return _entries.Select(e => e.Key); } }
        /// <summary>
        /// All values stored in this dictionary.
        /// </summary>
        public IEnumerable<V> Values { get { return _entries.Select(e => e.Value); } }
        /// <summary>
        /// The number of elements stored in this dictionary.
        /// </summary>
        public int Count { get { return _entries.Count; } }
        /// <summary>
        /// Determines whether there is a corresponding key in this dictionary. Note that all keys within volatile ID range are assumed to be part of this dictionary.
        /// </summary>
        /// <param name="key">The key to lookup.</param>
        /// <returns><code>true</code> if a lookup on this key is safe. <code>false</code> if the key is out-of-bounds.</returns>
        public bool ContainsKey(K key) { return key.VolatileID >= 0 && key.VolatileID < _entryStorage.Length; }

        #region IEnumerable members

        /// <summary>
        /// Returns the enumerator of this collection.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<VolatileKeyValuePair<K, V>> GetEnumerator() { return ((IEnumerable<VolatileKeyValuePair<K, V>>)_entries).GetEnumerator(); }
        /// <summary>
        /// Returns the enumerator of this collection.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator() { return ((IEnumerable<VolatileKeyValuePair<K, V>>)_entries).GetEnumerator(); }

        #endregion
    }

    /// <summary>
    /// An inflexible dictionary for which only values but not keys can be modified.
    /// </summary>
    /// <typeparam name="K">The type of the keys.</typeparam>
    public class InflexibleIntDictionary<K> : IInflexibleDictionary<K, int>
    {
        /// <summary>
        /// The actual storage of this dictionary (simply wrapping an ordinary dictionary).
        /// </summary>
        private Dictionary<K, int> _storage;

        /// <summary>
        /// Creates a new inflexible dictionary from the given list of entries.
        /// </summary>
        /// <param name="entries">The finalized list of entries. No further entries can be added to the dictionary after creation.</param>
        public InflexibleIntDictionary(IList<KeyValuePair<K, int>> entries) { _storage = entries.Where(kvp => kvp.Value > 0).ToDictionary(entry => entry.Key, entry => entry.Value); }

        /// <summary>
        /// Gets or sets the value for the given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value.</returns>
        public int this[K key]
        {
            get { return _storage.ContainsKey(key) ? _storage[key] : 0; }
            set { if (value == 0) _storage.Remove(key); else _storage[key] = value; }
        }
    }

    /// <summary>
    /// Interface for dictionaries where keys are fixed but values may be modified.
    /// </summary>
    /// <typeparam name="K">The type of the keys.</typeparam>
    /// <typeparam name="V">The type of the values.</typeparam>
    public interface IInflexibleDictionary<K, V>
    {
        /// <summary>
        /// Gets or sets the value for the given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value.</returns>
        V this[K key] { get; set; }
    }

    /// <summary>
    /// An interface marking an object suitable for the use with the <code>QuickDictionary</code>.
    /// </summary>
    public interface IExposeVolatileID
    {
        /// <summary>
        /// A volatile ID that is unique, greater / equals zero and the volatile IDs overall are as low as possible (to reduce memory consumption).
        /// </summary>
        int VolatileID { get; }
    }
}
