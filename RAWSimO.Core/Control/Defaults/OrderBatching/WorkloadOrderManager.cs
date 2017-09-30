using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.OrderBatching
{
    /// <summary>
    /// Indicates the rule for ordering the stations when assigning orders.
    /// </summary>
    public enum WorkloadOrderingRule
    {
        /// <summary>
        /// Prefers stations with the lowest count of orders already assigned to.
        /// </summary>
        LowestOrderCount,
        /// <summary>
        /// Preferes stations with the highest count of orders already assigned to.
        /// </summary>
        HighestOrderCount,
    }
    /// <summary>
    /// Implements a manager that assigns orders to output-stations depending on the workload of these.
    /// </summary>
    public class WorkloadOrderManager : OrderManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public WorkloadOrderManager(Instance instance) : base(instance) { _config = instance.ControllerConfig.OrderBatchingConfig as WorkloadOrderBatchingConfiguration; }

        /// <summary>
        /// The config of this controller.
        /// </summary>
        private WorkloadOrderBatchingConfiguration _config;

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
                switch (_config.OrderingRule)
                {
                    case WorkloadOrderingRule.LowestOrderCount:
                        chosenStation = Instance.OutputStations
                            .Where(s => s.Active && s.FitsForReservation(order)) // There has to be sufficient capacity left at the station
                            .OrderBy(s => s.CapacityInUse + s.CapacityReserved) // Choose the one with the least orders
                            .ThenBy(s => Instance.Randomizer.NextDouble()) // Use a random tie-breaker
                            .FirstOrDefault();
                        break;
                    case WorkloadOrderingRule.HighestOrderCount:
                        chosenStation = Instance.OutputStations
                            .Where(s => s.Active && s.FitsForReservation(order)) // There has to be sufficient capacity left at the station
                            .OrderByDescending(s => s.CapacityInUse + s.CapacityReserved) // Choose the one with the most orders
                            .ThenBy(s => Instance.Randomizer.NextDouble()) // Use a random tie-breaker
                            .FirstOrDefault();
                        break;
                    default: throw new ArgumentException("Unknown ordering rule: " + _config.OrderingRule);
                }
                // If we found a station, assign the bundle to it
                if (chosenStation != null)
                    AllocateOrder(order, chosenStation);
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
