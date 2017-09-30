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
    /// Implements the core functionality of a bundle manager.
    /// </summary>
    public abstract class BundleManager : IUpdateable, IOptimize, IStatTracker
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public BundleManager(Instance instance)
        {
            Instance = instance;
            // Subscribe to events
            Instance.BundleStored += SignalBundleStored;
            Instance.NewBundle += SignalNewBundleAvailable;
        }

        /// <summary>
        /// The instance this manager is assigned to.
        /// </summary>
        protected Instance Instance { get; set; }

        /// <summary>
        /// All not yet decided item-bundles.
        /// </summary>
        protected HashSet<ItemBundle> _pendingBundles = new HashSet<ItemBundle>();

        /// <summary>
        /// Indicates that the current situation has already been investigated. So that it will be ignored.
        /// </summary>
        protected bool SituationInvestigated { get; set; }

        /// <summary>
        /// This is called to decide about potentially pending bundles.
        /// This method is being timed for statistical purposes and is also ONLY called when <code>SituationInvestigated</code> is <code>false</code>.
        /// Hence, set the field accordingly to react on events not tracked by this outer skeleton.
        /// </summary>
        protected abstract void DecideAboutPendingBundles();

        /// <summary>
        /// Adds the transaction to the ready list.
        /// </summary>
        /// <param name="bundle">The bundle that is going to be transferred.</param>
        /// <param name="station">The station the bundle is distributed from.</param>
        protected void AddToReadyList(ItemBundle bundle, InputStation station)
        {
            // Update capacity information
            station.RegisterBundle(bundle);
            // Update lists
            _pendingBundles.Remove(bundle);
            // Submit the decision
            Instance.Controller.Allocator.Submit(bundle, station);
            // Notify the instance about the decision
            Instance.NotifyReplenishmentBatchingDecided(station, bundle);
        }
        /// <summary>
        /// Signals the manager that the bundle was stored in the pod.
        /// </summary>
        /// <param name="station">The station.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="pod">The pod.</param>
        /// <param name="bundle">The bundle.</param>
        public void SignalBundleStored(InputStation station, Bot bot, Pod pod, ItemBundle bundle) { SituationInvestigated = false; }
        /// <summary>
        /// Signals the manager that a new bundle became available.
        /// </summary>
        /// <param name="bundle">The new bundle.</param>
        public void SignalNewBundleAvailable(ItemBundle bundle) { SituationInvestigated = false; }
        /// <summary>
        /// Signals the manager that a station that was previously not in use can now be assigned bundles.
        /// </summary>
        /// <param name="station">The newly activated station.</param>
        public void SignalStationActivated(InputStation station) { SituationInvestigated = false; }

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
                Instance.Observer.TimeReplenishmentBatching((DateTime.Now - before).TotalSeconds);
                // Remember that we had a look at the situation
                SituationInvestigated = true;
            }
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
