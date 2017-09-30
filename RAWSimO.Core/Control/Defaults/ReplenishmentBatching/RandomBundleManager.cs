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

namespace RAWSimO.Core.Control.Defaults.ReplenishmentBatching
{
    /// <summary>
    /// Implements a manager randomly assigning bundles to input-stations.
    /// </summary>
    public class RandomBundleManager : BundleManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public RandomBundleManager(Instance instance) : base(instance)
        {
            _config = instance.ControllerConfig.ReplenishmentBatchingConfig as RandomReplenishmentBatchingConfiguration;
        }

        /// <summary>
        /// The config of this controller.
        /// </summary>
        private RandomReplenishmentBatchingConfiguration _config;
        /// <summary>
        /// The station that was chosen last time.
        /// </summary>
        private InputStation _lastChosenStation = null;

        /// <summary>
        /// This is called to decide about potentially pending bundles.
        /// This method is being timed for statistical purposes and is also ONLY called when <code>SituationInvestigated</code> is <code>false</code>.
        /// Hence, set the field accordingly to react on events not tracked by this outer skeleton.
        /// </summary>
        protected override void DecideAboutPendingBundles()
        {
            foreach (var bundle in _pendingBundles.ToArray())
            {
                InputStation chosenStation = null;
                // Try to reuse the last station for this bundle
                if (_config.Recycle && _lastChosenStation != null && _lastChosenStation.Active && _lastChosenStation.FitsForReservation(bundle))
                {
                    // Last chosen station can be used for this bundle too
                    chosenStation = _lastChosenStation;
                }
                else
                {
                    // Choose a random station
                    chosenStation = Instance.InputStations
                        .Where(s => s.Active && s.FitsForReservation(bundle)) // There has to be sufficient capacity left at the station
                        .OrderBy(s => Instance.Randomizer.NextDouble()) // Choose a random one
                        .FirstOrDefault();
                    _lastChosenStation = chosenStation;
                }
                // If we found a station, assign the bundle to it
                if (chosenStation != null)
                    AddToReadyList(bundle, chosenStation);
            }
        }

        #region IOptimize Members

        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        public override void SignalCurrentTime(double currentTime) { /* Ignore since this simple manager is always ready. */ }

        #endregion
    }
}
