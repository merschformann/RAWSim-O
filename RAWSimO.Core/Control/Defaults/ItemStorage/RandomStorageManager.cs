using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.ItemStorage
{
    /// <summary>
    /// Implements a random storage manager that just randomly assigns bundles to pods.
    /// </summary>
    public class RandomStorageManager : ItemStorageManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public RandomStorageManager(Instance instance) : base(instance)
        {
            _config = instance.ControllerConfig.ItemStorageConfig as RandomItemStorageConfiguration;
            instance.PodHandled += PodHandled;
        }

        private void PodHandled(Pod pod, InputStation iStation, OutputStation oStation)
        {
            // If the recycled pod was just handled at an input-station, do not assign any more bundles to it (we do not want to bring it back immediately after replenishing it)
            if (pod == _lastChosenPod && iStation != null)
                _lastChosenPod = null;
        }

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
        /// The config of this controller.
        /// </summary>
        private RandomItemStorageConfiguration _config;
        /// <summary>
        /// The last pod.
        /// </summary>
        private Pod _lastChosenPod = null;

        /// <summary>
        /// This is called to decide about potentially pending bundles.
        /// This method is being timed for statistical purposes and is also ONLY called when <code>SituationInvestigated</code> is <code>false</code>.
        /// Hence, set the field accordingly to react on events not tracked by this outer skeleton.
        /// </summary>
        protected override void DecideAboutPendingBundles()
        {
            foreach (var bundle in _pendingBundles.ToArray())
            {
                // Find a pod
                Pod chosenPod = null;
                // Check whether we can recycle the last used pod
                if (_config.StickToPodUntilFull && _lastChosenPod != null && _lastChosenPod.FitsForReservation(bundle))
                {
                    // Last chosen pod can be used for this bundle to
                    chosenPod = _lastChosenPod;
                }
                else
                {
                    // Choose the next pod to use randomly
                    chosenPod = Instance.Pods
                        .Where(b => b.FitsForReservation(bundle))
                        .OrderBy(b => Instance.Randomizer.NextDouble())
                        .FirstOrDefault();
                    _lastChosenPod = chosenPod;
                }
                // If we found a pod, assign the bundle to it
                if (chosenPod != null)
                    AddToReadyList(bundle, chosenPod);
            }
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

        #region IOptimize Members

        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        public override void SignalCurrentTime(double currentTime) { /* Ignore since this simple manager is always ready. */ }

        #endregion
    }
}
