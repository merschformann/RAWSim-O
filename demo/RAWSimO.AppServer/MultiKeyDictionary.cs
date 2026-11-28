using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.AppServer
{
    // TODO remove all of this and use the toolbox implementation

    /// <summary>
    /// A simple multi-dimensional dictionary.
    /// </summary>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    public class MultiKeyDictionary<TValue>
    {
        /// <summary>
        /// The actual storage.
        /// </summary>
        private Dictionary<object[], TValue> _storage;
        /// <summary>
        /// Creates a new instance of the dictionary.
        /// </summary>
        public MultiKeyDictionary() { _storage = new Dictionary<object[], TValue>(new ObjectArrayEqualityComparer<object>()); }
        /// <summary>
        /// Gets or sets the value with the given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The value at this index.</returns>
        public TValue this[params object[] index] { get { return _storage[index]; } set { _storage[index] = value; } }
        /// <summary>
        /// All values of this dictionary.
        /// </summary>
        public IEnumerable<TValue> Values { get { return _storage.Values; } }
        /// <summary>
        /// All keys of this dictionary.
        /// </summary>
        public IEnumerable<object[]> Keys { get { return _storage.Keys; } }
        /// <summary>
        /// Check whether the specified key is contained in this dictionary.
        /// </summary>
        /// <param name="index">The key to look for.</param>
        /// <returns><code>true</code> if the key is present, <code>false</code> otherwise.</returns>
        public bool ContainsKey(params object[] index) { return _storage.ContainsKey(index); }
        /// <summary>
        /// Removes the element at the given index.
        /// </summary>
        /// <param name="index">The index of the element.</param>
        public void Remove(params object[] index) { _storage.Remove(index); }
        /// <summary>
        /// Clears the dictionary.
        /// </summary>
        public void Clear() { _storage.Clear(); }
    }
    public class SymmetricKeyDictionary<TKeys, TValue>
    {
        /// <summary>
        /// The actual storage.
        /// </summary>
        private MultiKeyDictionary<TValue> _storage;
        /// <summary>
        /// All keys.
        /// </summary>
        private HashSet<Tuple<TKeys, TKeys>> _keys;
        /// <summary>
        /// Creates a new instance of the dictionary.
        /// </summary>
        public SymmetricKeyDictionary() { _storage = new MultiKeyDictionary<TValue>(); _keys = new HashSet<Tuple<TKeys, TKeys>>(); }
        /// <summary>
        /// Gets or sets the value with the given index.
        /// </summary>
        /// <param name="key1">The first key defining the index.</param>
        /// <param name="key2">The second key defining the index.</param>
        /// <returns>The value at this index.</returns>
        public TValue this[TKeys key1, TKeys key2]
        {
            get { if (_storage.ContainsKey(key1, key2)) return _storage[key1, key2]; else return _storage[key2, key1]; }
            set { _storage[key1, key2] = value; if (!(_storage.ContainsKey(key1, key2) || _storage.ContainsKey(key2, key1))) _keys.Add(new Tuple<TKeys, TKeys>(key1, key2)); }
        }
        /// <summary>
        /// All keys of the dictionary.
        /// </summary>
        public IEnumerable<Tuple<TKeys, TKeys>> KeysCombined { get { return _keys; } }
        /// <summary>
        /// All values of this dictionary.
        /// </summary>
        public IEnumerable<TValue> Values { get { return _storage.Values; } }
        /// <summary>
        /// Applies the specified operation or initialization to all combinations of keys.
        /// </summary>
        /// <param name="keys">The keys to update.</param>
        /// <param name="operation"></param>
        /// <param name="initializer"></param>
        public void ApplyAllCombinations(IEnumerable<TKeys> keys, Func<TValue, TValue> operation, Func<TValue> initializer)
        {
            // Iterate all keys
            foreach (var key1 in keys)
                foreach (var key2 in keys)
                {
                    // If at diagonal: break
                    if (key1.Equals(key2))
                        break;
                    // Determine order
                    if (this.ContainsKey(key1, key2))
                        // Update entry
                        this[key1, key2] = operation(this[key1, key2]);
                    else
                        // Check whether there is a value yet
                        if (this.ContainsKey(key2, key1))
                            // Update entry
                            this[key2, key1] = operation(this[key2, key1]);
                        else
                        {
                            // Init the entry
                            this[key1, key2] = initializer();
                            _keys.Add(new Tuple<TKeys, TKeys>(key1, key2));
                        }
                }
        }
        /// Check whether the specified key is contained in this dictionary.
        /// </summary>
        /// <param name="key1">The first key defining the index.</param>
        /// <param name="key2">The second key defining the index.</param>
        /// <returns><code>true</code> if the key is present, <code>false</code> otherwise.</returns>
        public bool ContainsKey(TKeys key1, TKeys key2) { return _storage.ContainsKey(key1, key2) || _storage.ContainsKey(key2, key1); ;  }
        /// <summary>
        /// Clears the dictionary.
        /// </summary>
        public void Clear() { _storage.Clear(); _keys.Clear(); }
    }
    /// <summary>
    /// A simple typed two-key dictionary.
    /// </summary>
    /// <typeparam name="K1">Type of the first key.</typeparam>
    /// <typeparam name="K2">Type of the second key.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    public class MultiKeyDictionary<K1, K2, TValue>
    {
        /// <summary>
        /// The actual storage.
        /// </summary>
        private MultiKeyDictionary<TValue> _storage;
        /// <summary>
        /// Creates a new instance of the dictionary.
        /// </summary>
        public MultiKeyDictionary() { _storage = new MultiKeyDictionary<TValue>(); }
        /// <summary>
        /// Gets or sets the value with the given index.
        /// </summary>
        /// <param name="key1">The first key defining the index.</param>
        /// <param name="key2">The second key defining the index.</param>
        /// <returns>The value at this index.</returns>
        public TValue this[K1 key1, K2 key2] { get { return _storage[key1, key2]; } set { _storage[key1, key2] = value; } }
        /// <summary>
        /// All values of this dictionary.
        /// </summary>
        public IEnumerable<TValue> Values { get { return _storage.Values; } }
        /// Check whether the specified key is contained in this dictionary.
        /// </summary>
        /// <param name="key1">The first key defining the index.</param>
        /// <param name="key2">The second key defining the index.</param>
        /// <returns><code>true</code> if the key is present, <code>false</code> otherwise.</returns>
        public bool ContainsKey(K1 key1, K2 key2) { return _storage.ContainsKey(key1, key2); }
        /// <summary>
        /// Removes the element at the given index.
        /// </summary>
        /// <param name="key1">The first index.</param>
        /// <param name="key2">The second index.</param>
        public void Remove(K1 key1, K2 key2) { _storage.Remove(key1, key2); }
        /// <summary>
        /// Clears the dictionary.
        /// </summary>
        public void Clear() { _storage.Clear(); }
    }
    /// <summary>
    /// A simple typed three-key dictionary.
    /// </summary>
    /// <typeparam name="K1">Type of the first key.</typeparam>
    /// <typeparam name="K2">Type of the second key.</typeparam>
    /// <typeparam name="K3">Type of the third key.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    public class MultiKeyDictionary<K1, K2, K3, TValue>
    {
        /// <summary>
        /// The actual storage.
        /// </summary>
        private MultiKeyDictionary<TValue> _storage;
        /// <summary>
        /// Creates a new instance of the dictionary.
        /// </summary>
        public MultiKeyDictionary() { _storage = new MultiKeyDictionary<TValue>(); }
        /// <summary>
        /// Gets or sets the value with the given index.
        /// </summary>
        /// <param name="key1">The first key defining the index.</param>
        /// <param name="key2">The second key defining the index.</param>
        /// <param name="key3">The third key defining the index.</param>
        /// <returns>The value at this index.</returns>
        public TValue this[K1 key1, K2 key2, K3 key3] { get { return _storage[key1, key2, key3]; } set { _storage[key1, key2, key3] = value; } }
        /// <summary>
        /// All values of this dictionary.
        /// </summary>
        public IEnumerable<TValue> Values { get { return _storage.Values; } }
        /// Check whether the specified key is contained in this dictionary.
        /// </summary>
        /// <param name="key1">The first key defining the index.</param>
        /// <param name="key2">The second key defining the index.</param>
        /// <param name="key3">The third key defining the index.</param>
        /// <returns><code>true</code> if the key is present, <code>false</code> otherwise.</returns>
        public bool ContainsKey(K1 key1, K2 key2, K3 key3) { return _storage.ContainsKey(key1, key2, key3); }
        /// <summary>
        /// Removes the element at the given index.
        /// </summary>
        /// <param name="key1">The first key.</param>
        /// <param name="key2">The second key.</param>
        /// <param name="key3">The third key.</param>
        public void Remove(K1 key1, K2 key2, K3 key3) { _storage.Remove(key1, key2, key3); }
        /// <summary>
        /// Clears the dictionary.
        /// </summary>
        public void Clear() { _storage.Clear(); }
    }
}
