using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Metrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.OrderBatching
{
    /// <summary>
    /// The rule for determining which of the best pods is nearest to the respective station.
    /// </summary>
    public enum NearBestPodOrderBatchingDistanceRule
    {
        /// <summary>
        /// Uses the euclidean distance measure to determine the shortest distance between a best pod and an output-station.
        /// </summary>
        Euclid,
        /// <summary>
        /// Uses the manhattan distance measure to determine the shortest distance between a best pod and an output-station.
        /// </summary>
        Manhattan,
        /// <summary>
        /// Uses the shortest path (calculated by A*) to determine the shortest distance between a best pod and an output-station.
        /// </summary>
        ShortestPath,
    }
    /// <summary>
    /// Implements a manager that assigns orders to output-stations with most lines in common.
    /// </summary>
    public class NearBestPodOrderManager : OrderManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public NearBestPodOrderManager(Instance instance) : base(instance) { _config = instance.ControllerConfig.OrderBatchingConfig as NearBestPodOrderBatchingConfiguration; }

        /// <summary>
        /// The config of this controller.
        /// </summary>
        private NearBestPodOrderBatchingConfiguration _config;

        /// <summary>
        /// This is called to decide about potentially pending orders.
        /// This method is being timed for statistical purposes and is also ONLY called when <code>SituationInvestigated</code> is <code>false</code>.
        /// Hence, set the field accordingly to react on events not tracked by this outer skeleton.
        /// </summary>
        protected override void DecideAboutPendingOrders()
        {
            foreach (var order in _pendingOrders.Where(o => o.Positions.All(p => Instance.StockInfo.GetActualStock(p.Key) >= p.Value)).ToArray())
            {
                // Check all pods for the maximum number of picks that can be done with them
                List<Pod> bestPods = new List<Pod>();
                int bestPodPicks = -1;
                foreach (var pod in Instance.Pods.Where(p => !p.InUse))
                {
                    // Calculate picks that can potentially be done with the pod
                    int picks = order.Positions.Sum(pos => Math.Min(pod.CountAvailable(pos.Key), pos.Value));
                    // Check whether we found even more possible picks with this pod
                    if (bestPodPicks < picks)
                    {
                        bestPods.Clear();
                        bestPods.Add(pod);
                        bestPodPicks = picks;
                    }
                    else
                    {
                        // Check whether the current pod belongs into the winner group
                        if (bestPodPicks == picks) { bestPods.Add(pod); }
                    }
                }
                // Choose station nearest to one of the best pods
                OutputStation chosenStation = null;
                double shortestDistance = double.PositiveInfinity;
                foreach (var station in Instance.OutputStations.Where(s => s.Active && s.FitsForReservation(order)))
                {
                    foreach (var pod in bestPods)
                    {
                        double distance;
                        switch (_config.DistanceRule)
                        {
                            case NearBestPodOrderBatchingDistanceRule.Euclid: distance = Distances.CalculateEuclid(pod, station, Instance.WrongTierPenaltyDistance); break;
                            case NearBestPodOrderBatchingDistanceRule.Manhattan: distance = Distances.CalculateManhattan(pod, station, Instance.WrongTierPenaltyDistance); break;
                            case NearBestPodOrderBatchingDistanceRule.ShortestPath: distance = Distances.CalculateShortestPathPodSafe(pod.Waypoint, station.Waypoint, Instance); break;
                            default: throw new ArgumentException("Unknown distance rule: " + _config.DistanceRule);
                        }
                        if (distance < shortestDistance)
                        {
                            shortestDistance = distance;
                            chosenStation = station;
                        }
                    }
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
