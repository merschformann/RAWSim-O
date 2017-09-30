using RAWSimO.Core.Elements;
using RAWSimO.Core.Items;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Management
{
    /// <summary>
    /// Contains information about the current stock situation.
    /// </summary>
    public class StockInformation
    {
        /// <summary>
        /// Creates a new instance of the stock information tracker.
        /// </summary>
        /// <param name="instance">The instance this tracker belongs to.</param>
        public StockInformation(Instance instance)
        {
            Instance = instance;
            // Register events
            instance.BundleStored += SignalBundleStored;
            instance.NewOrder += SignalNewOrderAvailable;
            instance.ItemExtracted += SignalItemExtracted;
            instance.NewBundle += SignalNewBundleAvailable;
            instance.InitialBundleStored += SignalInitialBundleStored;
            instance.NewPod += SignalNewPod;
        }

        /// <summary>
        /// The instance this manager is associated with.
        /// </summary>
        private Instance Instance { get; set; }

        /// <summary>
        /// The current overall capacity utilization.
        /// </summary>
        public double CurrentActualOverallLoad { get; private set; }
        /// <summary>
        /// The current overall capacity utilization including the already reserved capacity.
        /// </summary>
        public double CurrentReservedOverallLoad { get; private set; }
        /// <summary>
        /// The overall capacity of the system.
        /// </summary>
        public double OverallLoadCapacity { get; private set; }
        /// <summary>
        /// Contains information about the actual inventory.
        /// </summary>
        private VolatileIDDictionary<ItemDescription, int> _currentActualStock;
        /// <summary>
        /// Contains information about the inventory that is still available and not yet reserved by an incoming order.
        /// </summary>
        private VolatileIDDictionary<ItemDescription, int> _currentAvailableStock;
        /// <summary>
        /// Contains information about the current demand for specific SKUs.
        /// </summary>
        private VolatileIDDictionary<ItemDescription, int> _currentOverallDemand;
        /// <summary>
        /// Initializes the stock information.
        /// </summary>
        private void InitStockInfo()
        {
            _currentActualStock = new VolatileIDDictionary<ItemDescription, int>(Instance.ItemDescriptions.Select(i => new VolatileKeyValuePair<ItemDescription, int>(i, 0)).ToList());
            _currentAvailableStock = new VolatileIDDictionary<ItemDescription, int>(Instance.ItemDescriptions.Select(i => new VolatileKeyValuePair<ItemDescription, int>(i, 0)).ToList());
            _currentOverallDemand = new VolatileIDDictionary<ItemDescription, int>(Instance.ItemDescriptions.Select(i => new VolatileKeyValuePair<ItemDescription, int>(i, 0)).ToList());
        }

        /// <summary>
        /// Gets information about the actual inventory. Only reservations for already allocated orders are respected by this method.
        /// </summary>
        /// <param name="item">The item description to check.</param>
        /// <returns>The amount of inventory left for that particular item.</returns>
        public int GetActualStock(ItemDescription item) { return _currentActualStock[item]; }
        /// <summary>
        /// Gets information about the available inventory. Items reserved by all available orders are already excluded.
        /// </summary>
        /// <param name="item">The item description to check.</param>
        /// <returns>The amount of inventory left for that particular item.</returns>
        public int GetAvailableStock(ItemDescription item) { return _currentAvailableStock[item]; }
        /// <summary>
        /// Gets information about the current demand for the given SKU.
        /// </summary>
        /// <param name="item">The SKU to lookup the demand for.</param>
        /// <returns>The current demand for the given SKU as indicated by all unpicked / open order lines and quantities.</returns>
        public int GetCurrentDemand(ItemDescription item) { return _currentOverallDemand[item]; }

        #region Event handlers

        private void SignalNewPod(Pod pod)
        {
            // Update overall load information
            OverallLoadCapacity += pod.Capacity;
        }

        private void SignalItemExtracted(Pod pod, ItemDescription item)
        {
            // Update overall load information
            CurrentActualOverallLoad -= item.Weight;
            CurrentReservedOverallLoad -= item.Weight;
            // Update demand info
            _currentOverallDemand[item]--;
        }

        private void SignalBundleStored(InputStation station, Bot bot, Pod pod, ItemBundle bundle)
        {
            // Init, if necessary
            if (_currentActualStock == null)
                InitStockInfo();
            // Update overall load information
            CurrentActualOverallLoad += bundle.BundleWeight;
            // Update actual stock information
            _currentActualStock[bundle.ItemDescription] += bundle.ItemCount;
            // Update available stock information
            _currentAvailableStock[bundle.ItemDescription] += bundle.ItemCount;
        }

        private void SignalInitialBundleStored(ItemBundle bundle, Pod pod)
        {
            // Init, if necessary
            if (_currentActualStock == null)
                InitStockInfo();
            // Update overall load information
            CurrentActualOverallLoad += bundle.BundleWeight;
            CurrentReservedOverallLoad += bundle.BundleWeight;
            // Update actual stock information
            _currentActualStock[bundle.ItemDescription] += bundle.ItemCount;
            // Update available stock information
            _currentAvailableStock[bundle.ItemDescription] += bundle.ItemCount;
        }

        private void SignalNewBundleAvailable(ItemBundle bundle)
        {
            // Update overall load information
            CurrentReservedOverallLoad += bundle.BundleWeight;
        }

        private void SignalNewOrderAvailable(Order order)
        {
            // Init, if necessary
            if (_currentActualStock == null)
                InitStockInfo();
            // Update stock information
            foreach (var position in order.Positions)
            {
                _currentAvailableStock[position.Key] -= position.Value;
                if (_currentAvailableStock[position.Key] < 0)
                    Instance.LogInfo("Warning! Unfulfillable order-position submitted: " + position.Key.ToDescriptiveString() + " (" + _currentAvailableStock[position.Key] + ")");
                // Update demand info
                _currentOverallDemand[position.Key] += position.Value;
            }
        }

        #endregion
    }
}
