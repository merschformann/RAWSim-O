using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.Items;
using RAWSimO.Core.Management;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.OrderBatching
{
    /// <summary>
    /// Implements a manager that randomly assigns orders to output-stations.
    /// </summary>
    public class RandomOrderManager : OrderManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public RandomOrderManager(Instance instance) : base(instance) { _config = instance.ControllerConfig.OrderBatchingConfig as RandomOrderBatchingConfiguration; }

        /// <summary>
        /// The config of this controller.
        /// </summary>
        private RandomOrderBatchingConfiguration _config;
        /// <summary>
        /// The station that was chosen last time.
        /// </summary>
        private OutputStation _lastChosenStation = null;

        /// <summary>
        /// This is called to decide about potentially pending orders.
        /// This method is being timed for statistical purposes and is also ONLY called when <code>SituationInvestigated</code> is <code>false</code>.
        /// Hence, set the field accordingly to react on events not tracked by this outer skeleton.
        /// </summary>
        protected override void DecideAboutPendingOrders()
        {
            foreach (var order in _pendingOrders.Where(o => o.Positions.All(p => Instance.StockInfo.GetActualStock(p.Key) >= p.Value)).ToArray())
            {
                OutputStation chosenStation = null;
                // Try to reuse the last station for this order
                if (_config.Recycle && _lastChosenStation != null && _lastChosenStation.Active && _lastChosenStation.FitsForReservation(order))
                {
                    // Last chosen station can be used for this order too
                    chosenStation = _lastChosenStation;
                }
                else
                {
                    // Choose a random station
                    chosenStation = Instance.OutputStations
                        .Where(s => s.Active && s.FitsForReservation(order)) // There has to be sufficient capacity left at the station
                        .OrderBy(s => Instance.Randomizer.NextDouble()) // Choose a random one
                        .FirstOrDefault();
                    _lastChosenStation = chosenStation;
                }
                // If we found a station, assign the bundle to it
                if (chosenStation != null)
                {
                    AllocateOrder(order, chosenStation);
                }
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
