using RAWSimO.Core.Items;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Statistics
{
    /// <summary>
    /// Tracks the frequency information of all item-descriptions of the instance.
    /// </summary>
    public class FrequencyTracker
    {
        /// <summary>
        /// Creates a new frequency tracker object for the given instance.
        /// </summary>
        /// <param name="instance">The instance the tracker belongs to.</param>
        public FrequencyTracker(Instance instance)
        {
            _instance = instance;
            // Subscribe to events
            instance.NewOrder += NewOrderCallback;
        }
        /// <summary>
        /// The instance this tracker belongs to.
        /// </summary>
        private Instance _instance;
        /// <summary>
        /// Indicates whether combined frequencies will be tracked at all.
        /// </summary>
        private bool _combinedTracking;
        /// <summary>
        /// Indicates whether this tracker was initiated.
        /// </summary>
        private bool _initiated;

        /// <summary>
        /// The method that is called everytime a new order is submitted.
        /// </summary>
        /// <param name="order">The order that was just submitted to the system.</param>
        internal void NewOrderCallback(Order order)
        {
            // Only enable combined frequency tracking if necessary
            if (!_initiated)
            {
                _combinedTracking =
                    _instance.SettingConfig.CorrelativeFrequencyTracking ||
                    _instance.ControllerConfig.ItemStorageConfig.GetMethodType() == Configurations.ItemStorageMethodType.Correlative;
                _initiated = true;
            }
            // Log combined count
            _combinedItemOrderCount.ApplyAllCombinations(order.Positions.Select(p => p.Key), (int value) => value + 1, () => 1);
            // Increase the order count of each position's item and the overall count
            foreach (var position in order.Positions)
            {
                // Increase the order count
                if (!_itemOrderCount.ContainsKey(position.Key))
                    _itemOrderCount[position.Key] = position.Value;
                else
                    _itemOrderCount[position.Key] += position.Value;
                // Increase the order count for the customized tracking
                if (!_modifiableOrderCount.ContainsKey(position.Key))
                    _modifiableOrderCount[position.Key] = position.Value;
                else
                    _modifiableOrderCount[position.Key] += position.Value;
                // Keep track of maximal order count per item
                if (_itemOrderCount[position.Key] > _maxItemOrdered)
                    _maxItemOrdered = _itemOrderCount[position.Key];
                // Keep track of maximal order count per item-tuple
                if (_combinedTracking)
                    foreach (var otherPosition in order.Positions.Where(p => p.Key != position.Key))
                        if (_combinedItemOrderCount[position.Key, otherPosition.Key] > _statMaxCombinedItemsOrdered)
                            _statMaxCombinedItemsOrdered = _combinedItemOrderCount[position.Key, otherPosition.Key];
            }
            // Refresh frequencies
            foreach (var item in _itemOrderCount.Keys)
                _itemFrequency[item] = (double)_itemOrderCount[item] / (double)_maxItemOrdered;
            // Refresh combined frequencies
            if (_combinedTracking)
                foreach (var keyTuple in _combinedItemOrderCount.KeysCombined)
                    _combinedItemFrequencies[keyTuple.Item1, keyTuple.Item2] = (double)_combinedItemOrderCount[keyTuple.Item1, keyTuple.Item2] / (double)_statMaxCombinedItemsOrdered;
        }

        /// <summary>
        /// Resets the frequency information. (this method does nothing for now, because resetting the measured frequencies seems inappropriate)
        /// </summary>
        internal void Reset()
        {
            //StatMaxItemOrdered = 0;
            //_statItemOrderCount.Clear();
            //_statItemFrequency.Clear();
            //StatMaxCombinedItemsOrdered = 0;
            //_statCombinedItemOrderCount.Clear();
            //_statCombinedItemFrequencies.Clear();
        }

        /// <summary>
        /// The number of times the item with the maximal order count was ordered.
        /// </summary>
        private int _maxItemOrdered;
        /// <summary>
        /// The number of times a specific item-type was ordered.
        /// </summary>
        private Dictionary<ItemDescription, int> _itemOrderCount = new Dictionary<ItemDescription, int>();
        /// <summary>
        /// The number of times a specific item-type was ordered. This is the custom version which can be modified by control mechanisms.
        /// </summary>
        private Dictionary<ItemDescription, int> _modifiableOrderCount = new Dictionary<ItemDescription, int>();
        /// <summary>
        /// The frequencies of all items.
        /// </summary>
        private Dictionary<ItemDescription, double> _itemFrequency = new Dictionary<ItemDescription, double>();
        /// <summary>
        /// The number of times the tuple of items with the maximal order count was ordered together.
        /// </summary>
        private int _statMaxCombinedItemsOrdered;
        /// <summary>
        /// The number of times a tuple of items was ordered.
        /// </summary>
        private SymmetricKeyDictionary<ItemDescription, int> _combinedItemOrderCount = new SymmetricKeyDictionary<ItemDescription, int>();
        /// <summary>
        /// The correlations between the different item-types.
        /// </summary>
        private SymmetricKeyDictionary<ItemDescription, double> _combinedItemFrequencies = new SymmetricKeyDictionary<ItemDescription, double>();

        /// <summary>
        /// Returns the current static frequency of the item. This somewhat reflects the probability for generating the item in form of a new bundle or as a line of an order.
        /// Note: Combined item frequencies and the currently used item type have to be taken into account.
        /// </summary>
        /// <param name="item">The item to lookup.</param>
        /// <returns>The frequency of the item.</returns>
        public double GetStaticFrequency(ItemDescription item) { return _instance.ItemManager.GetItemProbability(item) / _instance.ItemManager.GetItemProbabilityMax(); }
        /// <summary>
        /// Returns the measured frequency of the given item type. This value is updated throughout the simulation.
        /// </summary>
        /// <param name="item">The item type to lookup.</param>
        /// <returns>The frequency of the item. This is a value between 0 and 1.</returns>
        public double GetMeasuredFrequency(ItemDescription item)
        {
            if (_itemFrequency.ContainsKey(item))
                return _itemFrequency[item];
            else
                return 0;
        }
        /// <summary>
        /// Returns the measured combined frequency of the given item type tuple. This value is updated throughout the simulation.
        /// </summary>
        /// <param name="item1">The first part of the item tuple.</param>
        /// <param name="item2">The second part of the item tuple.</param>
        /// <returns>The combined frequency of both items. This is a value between 0 and 1.</returns>
        public double GetMeasuredFrequency(ItemDescription item1, ItemDescription item2)
        {
            if (!_combinedTracking)
                throw new InvalidOperationException("Combined frequency tracking is disabled!");
            if (_combinedItemFrequencies.ContainsKey(item1, item2))
                return _combinedItemFrequencies[item1, item2];
            else
                return 0;
        }
        /// <summary>
        /// Returns the order count of the given item type. This value is updated throughout the simulation but can be reset if desired.
        /// </summary>
        /// <param name="item">The item to lookup.</param>
        /// <returns>The number of times the given item was ordered.</returns>
        internal int GetModifiableOrderCount(ItemDescription item)
        {
            if (_modifiableOrderCount.ContainsKey(item))
                return _modifiableOrderCount[item];
            else
                return 0;
        }
        /// <summary>
        /// Resets the modifiable order count.
        /// </summary>
        internal void ResetModifiableOrderCount() { _modifiableOrderCount.Clear(); }
    }
}
