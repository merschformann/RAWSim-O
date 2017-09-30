using RAWSimO.Core.Configurations;
using RAWSimO.Core.Control.Defaults.ItemStorage;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control
{
    /// <summary>
    /// The base class of all mechanisms deciding the pod when storing a bundle.
    /// </summary>
    public abstract class ItemStorageManager : IUpdateable, IOptimize, IStatTracker
    {
        /// <summary>
        /// Creates a new instance of the manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public ItemStorageManager(Instance instance)
        {
            Instance = instance;
            // Subscribe to events
            Instance.NewBundle += SignalNewBundle;
            Instance.BundleStored += SignalBundleStored;
            Instance.ItemExtracted += SignalItemExtracted;
        }

        /// <summary>
        /// The instance this manager is assigned to.
        /// </summary>
        protected Instance Instance { get; set; }

        /// <summary>
        /// All not yet assigned bundles.
        /// </summary>
        protected HashSet<ItemBundle> _pendingBundles = new HashSet<ItemBundle>();
        /// <summary>
        /// All decided but not yet allocated bundles.
        /// </summary>
        private Dictionary<Pod, List<ItemBundle>> _bufferedBundles = new Dictionary<Pod, List<ItemBundle>>();
        /// <summary>
        /// The last times at which bundles were buffered for the specific pods.
        /// </summary>
        private Dictionary<Pod, double> _bufferedBundlesTimes = new Dictionary<Pod, double>();

        /// <summary>
        /// Indicates that new storage, that wasn't investigated previously, is available.
        /// </summary>
        protected bool SituationInvestigated { get; set; }

        /// <summary>
        /// This is called to decide about potentially pending bundles.
        /// This method is being timed for statistical purposes and is also ONLY called when <code>SituationInvestigated</code> is <code>false</code>.
        /// Hence, set the field accordingly to react on events not tracked by this outer skeleton.
        /// </summary>
        protected abstract void DecideAboutPendingBundles();
        /// <summary>
        /// Retrieves the threshold value above which buffered decisions for that respective pod are submitted to the system.
        /// </summary>
        /// <param name="pod">The pod to get the threshold value for.</param>
        /// <returns>The threshold value above which buffered decisions are submitted. Use 0 to immediately submit decisions.</returns>
        protected abstract double GetStorageBufferThreshold(Pod pod);
        /// <summary>
        /// Retrieves the time after which buffered bundles will be allocated even if they do not meet the threshold criterion.
        /// </summary>
        /// <param name="pod">The pod to get the timeout value for.</param>
        /// <returns>The buffer timeout.</returns>
        protected abstract double GetStorageBufferTimeout(Pod pod);
        /// <summary>
        /// Selects a pod for a bundle generated during initialization.
        /// </summary>
        /// <param name="instance">The active instance.</param>
        /// <param name="bundle">The bundle to assign to a pod.</param>
        /// <returns>The selected pod.</returns>
        public abstract Pod SelectPodForInititalInventory(Instance instance, ItemBundle bundle);

        /// <summary>
        /// Adds the transaction to the ready list.
        /// </summary>
        /// <param name="bundle">The bundle that is going to be transferred.</param>
        /// <param name="pod">The pod the bundle is transferred to.</param>
        protected void AddToReadyList(ItemBundle bundle, Pod pod)
        {
            if (!IsAboveRefillThreshold(pod, bundle))
            {
                // Add decision to buffer list
                if (_bufferedBundles.ContainsKey(pod))
                    _bufferedBundles[pod].Add(bundle);
                else
                    _bufferedBundles.Add(pod, new List<ItemBundle> { bundle });
                // Remember this buffering
                _bufferedBundlesTimes[pod] = Instance.Controller.CurrentTime;
            }
            else
            {
                // Immediately submit the decision instead of buffering it
                Instance.Controller.Allocator.Submit(bundle, pod);
            }

            // Remove the bundle from the list of pending ones (if it was immediately assigned this operation is actually redundant)
            _pendingBundles.Remove(bundle);
            // Also notify the pod about the new bundle
            pod.RegisterBundle(bundle);
            // Notify the instance about the decision
            Instance.NotifyItemStorageDecided(pod, bundle);
        }

        /// <summary>
        /// Returns the last time a bundle was buffered for the given pod.
        /// </summary>
        /// <param name="pod">The pod to check.</param>
        /// <returns>The last time a bundle was buffered for the pod or max-value, if no buffering happened lately for the pod.</returns>
        private double GetLastBufferingTime(Pod pod) { return _bufferedBundlesTimes.ContainsKey(pod) ? _bufferedBundlesTimes[pod] : double.MaxValue; }
        /// <summary>
        /// Checks whether the given pod is ready for refill or has to be buffered some more.
        /// </summary>
        /// <param name="pod">The pod to check.</param>
        /// <returns><code>true</code> if the pod is ready to be refilled, <code>false</code> otherwise.</returns>
        internal bool IsAboveRefillThreshold(Pod pod)
        {
            return
                (pod.CapacityInUse + pod.CapacityReserved) / pod.Capacity >= GetStorageBufferThreshold(pod) ||
                Instance.Controller.CurrentTime - GetLastBufferingTime(pod) >= GetStorageBufferTimeout(pod);
        }
        /// <summary>
        /// Checks whether the given pod is ready for refill or has to be buffered some more.
        /// </summary>
        /// <param name="pod">The pod to check.</param>
        /// <param name="newBundle">The additional bundle to take into account.</param>
        /// <returns><code>true</code> if the pod is ready to be refilled, <code>false</code> otherwise.</returns>
        private bool IsAboveRefillThreshold(Pod pod, ItemBundle newBundle)
        {
            return
                // Check whether we are above the capacity threshold
                (pod.CapacityInUse + pod.CapacityReserved + newBundle.BundleWeight) / pod.Capacity >= GetStorageBufferThreshold(pod) ||
                // Additionally check for a buffering timeout
                Instance.Controller.CurrentTime - GetLastBufferingTime(pod) >= GetStorageBufferTimeout(pod);
        }
        /// <summary>
        /// Submits all decisions about buffered pods above their buffer threshold to the system.
        /// </summary>
        private void SubmitBufferedBundles()
        {
            // Get all pods that are above their buffer theshold
            IEnumerable<Pod> aboveThresholdPods = _bufferedBundles.Keys.Where(p => IsAboveRefillThreshold(p));

            // Check whether there are any pods above threshold
            if (aboveThresholdPods.Any())
            {
                // Iterate pods that are ready
                foreach (var pod in aboveThresholdPods.ToArray())
                {
                    // Submit decision of ready bundles
                    _bufferedBundles[pod].ForEach(b => Instance.Controller.Allocator.Submit(b, pod));
                    // Remove from buffer
                    _bufferedBundles.Remove(pod);
                    // Remove last buffering timestamp
                    _bufferedBundlesTimes.Remove(pod);
                }
            }
        }

        /// <summary>
        /// A callback for a newly stored bundle.
        /// </summary>
        /// <param name="station">The station at which the bundle was handled.</param>
        /// <param name="bot">The robot that conducted the task.</param>
        /// <param name="pod">The pod.</param>
        /// <param name="bundle">The bundle.</param>
        public virtual void SignalBundleStored(InputStation station, Bot bot, Pod pod, ItemBundle bundle) { }
        /// <summary>
        /// Signals the manager that a new bundle is available.
        /// </summary>
        /// <param name="bundle">The new bundle.</param>
        public void SignalNewBundle(ItemBundle bundle) { SituationInvestigated = false; }
        /// <summary>
        /// Signals the manager that an item was removed from the pod.
        /// </summary>
        /// <param name="item">The item that was extracted.</param>
        /// <param name="pod">The pod the item was stored in.</param>
        public void SignalItemExtracted(Pod pod, ItemDescription item) { SituationInvestigated = false; }

        #region IUpdateable Members

        /// <summary>
        /// The next event when this element has to be updated.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns>The next time this element has to be updated.</returns>
        public virtual double GetNextEventTime(double currentTime) { return double.PositiveInfinity; }
        /// <summary>
        /// Updates the element to the specified time.
        /// </summary>
        /// <param name="lastTime">The time before the update.</param>
        /// <param name="currentTime">The time to update to.</param>
        public virtual void Update(double lastTime, double currentTime)
        {
            // Retrieve the next bundle that we have not seen so far
            ItemBundle newBundle = Instance.ItemManager.RetrieveBundle(this);
            while (newBundle != null)
            {
                // Add it to the not yet decided bundles list
                _pendingBundles.Add(newBundle);
                // Retrieve the next bundle that we have not seen so far
                newBundle = Instance.ItemManager.RetrieveBundle(this);
                // Mark new situation
                SituationInvestigated = false;
            }

            // Decide about remaining bundles
            if (!SituationInvestigated)
            {
                // Measure time for decision
                DateTime before = DateTime.Now;
                // Do the actual work
                DecideAboutPendingBundles();
                // Calculate decision time
                Instance.Observer.TimeItemStorage((DateTime.Now - before).TotalSeconds);
                // Remember that we had a look at the situation
                SituationInvestigated = true;
            }

            // Submit buffered bundles that might be ready for allocation now
            SubmitBufferedBundles();
        }

        #endregion

        #region IOptimize Members

        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        public abstract void SignalCurrentTime(double currentTime);

        #endregion

        #region IStatTracker Members

        /// <summary>
        /// The callback that indicates that the simulation is finished and statistics have to submitted to the instance.
        /// </summary>
        public virtual void StatFinish() { /* Default case: do not flush any statistics */ }

        /// <summary>
        /// The callback indicates a reset of the statistics.
        /// </summary>
        public virtual void StatReset() { /* Default case: nothing to reset */ }

        #endregion
    }
}
