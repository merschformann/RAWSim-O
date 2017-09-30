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
    /// Indicates the rule to use for tie-breaking due to same number of order lines in common.
    /// </summary>
    public enum RelatedOrderBatchingTieBreaker
    {
        /// <summary>
        /// Uses random values for tie breaking.
        /// </summary>
        Random,
        /// <summary>
        /// The least busy station will be used in a tie situation.
        /// </summary>
        LeastBusy,
        /// <summary>
        /// The most busy station will be used in a tie situation.
        /// </summary>
        MostBusy,
    }
    /// <summary>
    /// Implements a manager that assigns orders to output-stations with most lines in common.
    /// </summary>
    public class RelatedOrderManager : OrderManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public RelatedOrderManager(Instance instance) : base(instance) { _config = instance.ControllerConfig.OrderBatchingConfig as RelatedOrderBatchingConfiguration; }

        /// <summary>
        /// The config of this controller.
        /// </summary>
        private RelatedOrderBatchingConfiguration _config;

        /// <summary>
        /// This is called to decide about potentially pending orders.
        /// This method is being timed for statistical purposes and is also ONLY called when <code>SituationInvestigated</code> is <code>false</code>.
        /// Hence, set the field accordingly to react on events not tracked by this outer skeleton.
        /// </summary>
        protected override void DecideAboutPendingOrders()
        {
            foreach (var order in _pendingOrders.Where(o => o.Positions.All(p => Instance.StockInfo.GetActualStock(p.Key) >= p.Value)).ToArray())
            {
                OutputStation chosenStation = Instance.OutputStations
                    // There has to be sufficient capacity left at the station
                    .Where(s => s.Active && s.FitsForReservation(order))
                    // Choose the one with the most lines in common
                    .OrderByDescending(s => s.AssignedOrders.Sum(other =>
                        other.Positions.Select(p => p.Key).Intersect(order.Positions.Select(p => p.Key)).Count()))
                    // Use a tie breaker (especially for situations where stations are empty)
                    .ThenBy(s =>
                    {
                        switch (_config.TieBreaker)
                        {
                            case RelatedOrderBatchingTieBreaker.Random: return Instance.Randomizer.NextDouble();
                            case RelatedOrderBatchingTieBreaker.LeastBusy: return s.AssignedOrders.Count();
                            case RelatedOrderBatchingTieBreaker.MostBusy: return -s.AssignedOrders.Count();
                            default: throw new ArgumentException("Unknown tie-breaker: " + _config.TieBreaker);
                        }
                    })
                    // The first one is the best
                    .FirstOrDefault();
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
