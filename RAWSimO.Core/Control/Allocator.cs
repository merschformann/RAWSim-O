using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.Items;
using RAWSimO.Core.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control
{
    /// <summary>
    /// This class helps allocating the orders and bundles after decisions about their assignment were conducted.
    /// </summary>
    public class Allocator : IUpdateable
    {
        /// <summary>
        /// Creates a new instance of this object.
        /// </summary>
        /// <param name="instance">The instance this allocated belongs to.</param>
        public Allocator(Instance instance) { Instance = instance; }

        /// <summary>
        /// The instance this manager is assigned to.
        /// </summary>
        Instance Instance { get; set; }

        /// <summary>
        /// All decided but not yet allocated bundle to input station assignments.
        /// </summary>
        private Dictionary<ItemBundle, InputStation> _iStationAssignments = new Dictionary<ItemBundle, InputStation>();
        /// <summary>
        /// All decided but not yet allocated bundle to pod assignments.
        /// </summary>
        private Dictionary<ItemBundle, Pod> _podAssignments = new Dictionary<ItemBundle, Pod>();

        /// <summary>
        /// Allocates the input request.
        /// </summary>
        /// <param name="bundle">The bundle to allocate.</param>
        /// <param name="pod">The pod the bundle shall be stored in.</param>
        /// <param name="station">The station that shall handle the bundle.</param>
        private void Allocate(ItemBundle bundle, Pod pod, InputStation station)
        {
            // Indicate change at instance
            Instance.Changed = true;
            // Check whether decision is possible
            if (station.CapacityInUse + bundle.BundleWeight > station.Capacity)
                throw new InvalidOperationException("Allocating the bundle to the station would exceed its capacity!");
            if (Instance.ControllerConfig.ItemStorageConfig.GetMethodType() != ItemStorageMethodType.Dummy && pod.CapacityInUse + bundle.BundleWeight > pod.Capacity)
                throw new InvalidOperationException("Allocating the bundle to the pod would exceed its capacity!");
            // Remove from ready lists
            _iStationAssignments.Remove(bundle);
            _podAssignments.Remove(bundle);
            // Add the bundle to the station
            station.Add(bundle);
            // Mark the pod at the bundle
            bundle.Pod = pod;
            // Add storage request
            Instance.ResourceManager.NewItemBundleAssignedToStation(bundle, station, pod);
            // Notify item manager
            Instance.ItemManager.NewBundleAssignedToStation(station, bundle);
            // Remove bundle from item manager
            (Instance.ItemManager as ItemManager).TakeAvailableBundle(bundle);
        }
        /// <summary>
        /// Allocates the order.
        /// </summary>
        /// <param name="order">The order to allocate.</param>
        /// <param name="station">The station to allocate the order to.</param>
        public void Allocate(Order order, OutputStation station)
        {
            // Indicate change at instance
            Instance.Changed = true;
            // Check whether decision is possible
            if (station.CapacityInUse + 1 > station.Capacity)
                throw new InvalidOperationException("Allocating the bundle to the station would exceed its capacity!");
            // Hand over the order
            station.AssignOrder(order);
            // Add extraction request
            Instance.ResourceManager.NewOrderAssignedToStation(order, station);
            // Mark orders allocated
            Instance.Controller.OrderManager.SignalOrderAllocated(order, station);
            // Notify item manager
            Instance.ItemManager.NewOrderAssignedToStation(station, order);
            // Remove order from item manager
            (Instance.ItemManager as ItemManager).TakeAvailableOrder(order);
        }

        /// <summary>
        /// Submits a new replenishment assignment decision to the allocator.
        /// </summary>
        /// <param name="bundle">The bundle that is being assigned to an input station.</param>
        /// <param name="station">The input station that shall be used to store the bundle.</param>
        public void Submit(ItemBundle bundle, InputStation station)
        {
            // Store assignment until the next allocation update
            _iStationAssignments[bundle] = station;
        }
        /// <summary>
        /// Submits a new bundle storage assignment decision to the allocator.
        /// </summary>
        /// <param name="bundle">The bundle that is being assigned to a storage pod.</param>
        /// <param name="pod">The pod in which the bundle shall be stored.</param>
        public void Submit(ItemBundle bundle, Pod pod)
        {
            // Store assignment until the next allocation update
            _podAssignments[bundle] = pod;
            // Notify instance about final decision
            Instance.NotifyItemStorageAllocationAvailable(pod, bundle);
        }
        /// <summary>
        /// Adds an order to the queue of a station. This is only done for information tracking purposes. The actual decision submission has to be done separately by the respective controller.
        /// </summary>
        /// <param name="order">The order to queue.</param>
        /// <param name="station">The station to queue the order to.</param>
        public void Queue(Order order, OutputStation station)
        {
            station.QueueOrder(order);
            Instance.ResourceManager.NewOrderQueuedToStation(order, station);
        }

        /// <summary>
        /// Checks whether an assignment of the bundle to a pod was already done.
        /// </summary>
        /// <param name="bundle">The bundle to check.</param>
        /// <returns><code>true</code> if the assignment is present, <code>false</code> otherwise.</returns>
        public bool IsBundleStorageDecided(ItemBundle bundle) { return _podAssignments.ContainsKey(bundle); }

        #region IUpdateable Members

        /// <summary>
        /// The next event when this element has to be updated.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns>The next time this element has to be updated.</returns>
        public double GetNextEventTime(double currentTime) { return double.PositiveInfinity; }
        /// <summary>
        /// Updates the element to the specified time.
        /// </summary>
        /// <param name="lastTime">The time before the update.</param>
        /// <param name="currentTime">The time to update to.</param>
        public void Update(double lastTime, double currentTime)
        {
            // >>> Allocate bundles
            if (_iStationAssignments.Any() && (_podAssignments.Any() || Instance.ControllerConfig.ItemStorageConfig.GetMethodType() == ItemStorageMethodType.Dummy))
            {
                ItemBundle[] bundles = Instance.ControllerConfig.ItemStorageConfig.GetMethodType() != ItemStorageMethodType.Dummy ?
                    _iStationAssignments.Keys.Intersect(_podAssignments.Keys).ToArray() :
                    _iStationAssignments.Keys.ToArray();
                foreach (var bundle in bundles)
                    Allocate(
                        bundle,
                        Instance.ControllerConfig.ItemStorageConfig.GetMethodType() != ItemStorageMethodType.Dummy ?
                            _podAssignments[bundle] :
                            null,
                        _iStationAssignments[bundle]);
            }
        }

        #endregion
    }
}
