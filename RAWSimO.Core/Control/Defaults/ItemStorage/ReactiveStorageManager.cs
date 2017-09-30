using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.ItemStorage
{
    /// <summary>
    /// The rule that is used to select a pod for a bundle.
    /// </summary>
    public enum ReactiveItemStorageAllocationRule
    {
        /// <summary>
        /// Uses a random pod, but prefers pods of the same tier as the input-station is located on.
        /// </summary>
        RandomSameTier,
        /// <summary>
        /// Uses the emptiest pod, but prefers pods of the same tier as the input-station is located on.
        /// </summary>
        EmptiestSameTier,
    }
    /// <summary>
    /// Implements a storage manager using the closest free pod.
    /// </summary>
    public class ReactiveStorageManager : ItemStorageManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public ReactiveStorageManager(Instance instance) : base(instance)
        {
            _config = instance.ControllerConfig.ItemStorageConfig as ReactiveItemStorageConfiguration;
            instance.ReplenishmentBatchingDecided += ReplenishmentBatchingDecided;
        }

        /// <summary>
        /// The config of this controller.
        /// </summary>
        private ReactiveItemStorageConfiguration _config;
        /// <summary>
        /// Tracks the assignments of bundles to input-stations.
        /// </summary>
        private Dictionary<ItemBundle, InputStation> _bundleToStation = new Dictionary<ItemBundle, InputStation>();

        /// <summary>
        /// Selects a pod for a bundle generated during initialization.
        /// </summary>
        /// <param name="instance">The active instance.</param>
        /// <param name="bundle">The bundle to assign to a pod.</param>
        /// <returns>The selected pod.</returns>
        public override Pod SelectPodForInititalInventory(Instance instance, ItemBundle bundle)
        {
            // Add to a random pod
            return instance.Pods
                .Where(p => p.FitsForReservation(bundle))
                .OrderBy(p => instance.Randomizer.NextDouble())
                .First();
        }

        /// <summary>
        /// Retrieves the threshold value above which buffered decisions for that respective pod are submitted to the system.
        /// </summary>
        /// <param name="pod">The pod to get the threshold value for.</param>
        /// <returns>The threshold value above which buffered decisions are submitted. Use 0 to immediately submit decisions.</returns>
        protected override double GetStorageBufferThreshold(Pod pod) { return _config.BufferThreshold; }
        /// <summary>
        /// Retrieves the time after which buffered bundles will be allocated even if they do not meet the threshold criterion.
        /// </summary>
        /// <param name="pod">The pod to get the timeout value for.</param>
        /// <returns>The buffer timeout.</returns>
        protected override double GetStorageBufferTimeout(Pod pod) { return _config.BufferTimeout; }

        /// <summary>
        /// This is called to decide about potentially pending bundles.
        /// This method is being timed for statistical purposes and is also ONLY called when <code>SituationInvestigated</code> is <code>false</code>.
        /// Hence, set the field accordingly to react on events not tracked by this outer skeleton.
        /// </summary>
        protected override void DecideAboutPendingBundles()
        {
            foreach (var bundle in _bundleToStation.Keys.ToArray())
            {
                // Find a pod
                Pod chosenPod = null;
                switch (_config.AllocationRule)
                {
                    case ReactiveItemStorageAllocationRule.RandomSameTier:
                        chosenPod = Instance.Pods
                           .Where(b => b.FitsForReservation(bundle))
                           .OrderBy(b =>
                           {
                               // Smalles == best == random
                               double orderValue = Instance.Randomizer.NextDouble();
                               // Punish pod in use
                               if (b.InUse)
                                   orderValue += 1;
                               // Punish wrong tier
                               if (b.Tier != _bundleToStation[bundle].Tier)
                                   orderValue += 1;
                               return orderValue;
                           })
                           .FirstOrDefault();
                        break;
                    case ReactiveItemStorageAllocationRule.EmptiestSameTier:
                        chosenPod = Instance.Pods
                           .Where(b => b.FitsForReservation(bundle))
                           .OrderBy(b =>
                           {
                               // Smalles == best == emptiest
                               double orderValue = (b.CapacityInUse + b.CapacityReserved) / b.Capacity;
                               // Punish pod in use
                               if (b.InUse)
                                   orderValue += 1;
                               // Punish wrong tier
                               if (b.Tier != _bundleToStation[bundle].Tier)
                                   orderValue += 1;
                               return orderValue;
                           })
                           .FirstOrDefault();
                        break;
                    default: throw new ArgumentException("Unknown allocation rule: " + _config.AllocationRule);
                }
                // If we found a pod, assign the bundle to it
                if (chosenPod != null)
                {
                    AddToReadyList(bundle, chosenPod);
                    _bundleToStation.Remove(bundle);
                }
            }
        }

        /// <summary>
        /// Used to keep track of the decisions done by replenishment batching.
        /// </summary>
        /// <param name="station">The chosen station.</param>
        /// <param name="bundle">The corresponding bundle.</param>
        private void ReplenishmentBatchingDecided(InputStation station, ItemBundle bundle) { _bundleToStation[bundle] = station; SituationInvestigated = false; }

        #region IOptimize Members

        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        public override void SignalCurrentTime(double currentTime) { /* Ignore since this simple manager is always ready. */ }

        #endregion
    }
}
