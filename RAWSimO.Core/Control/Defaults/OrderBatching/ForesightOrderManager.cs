using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Items;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.OrderBatching
{
    /// <summary>
    /// Defines the different available functions for scoring a station order tuple.
    /// </summary>
    public enum ScoreFunctionStationOrder
    {
        /// <summary>
        /// Determines the already available picks from incoming pods.
        /// </summary>
        InboundPodsAvailablePicks,
        /// <summary>
        /// Determines the already available picks from incoming pods and gives an extra bonus for depleting an item from a pod.
        /// </summary>
        InboundPodsAvailablePicksDepletePod,
        /// <summary>
        /// Simply picks the combination with the order which deadline is next.
        /// </summary>
        Deadline,
        /// <summary>
        /// Determines the already available picks from incoming pods and gives an extra bonus for not depleting an item from a pod.
        /// </summary>
        InboundPodsAvailablePicksNotDepletePod,

    }
    /// <summary>
    /// Implements a manager that uses information of the backlog to exploit similarities in orders when assigning them.
    /// </summary>
    public class ForesightOrderManager : OrderManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public ForesightOrderManager(Instance instance) : base(instance) { _config = instance.ControllerConfig.OrderBatchingConfig as ForesightOrderBatchingConfiguration; }

        /// <summary>
        /// The config of this controller.
        /// </summary>
        private ForesightOrderBatchingConfiguration _config;

        private double ScoreNormal(OutputStation station, Order order)
        {
            return order.Positions.Sum(position => Math.Min(station.InboundPods.Sum(pod => pod.CountAvailable(position.Key)), position.Value));
        }

        private double ScoreWeightedDepletePod(OutputStation station, Order order)
        {
            double score = 0;
            // Go through all lines of the order to determine the score
            foreach (var position in order.Positions)
            {
                bool podMatchesPosition = false;
                // Determine the inventory available for this position from all inbound pods
                int inboundInventoryAvailable = 0;
                foreach (var pod in station.InboundPods)
                {
                    int podInventoryAvailable = pod.CountAvailable(position.Key);
                    // Check whether we can remove that item type from the pod completely
                    if (podInventoryAvailable == position.Value)
                        podMatchesPosition = true;
                    // Return inbound inventory count
                    inboundInventoryAvailable += podInventoryAvailable;
                }
                //if (inboundInventoryAvailable == position.Value)
                if (podMatchesPosition)
                    // Bonus score for matches that deplete this item type from a pod completely
                    score += position.Value + 1;
                else
                    // Score is at least as much as the inventory we can immediately use for the order's position / line
                    score += Math.Min(inboundInventoryAvailable, position.Value);
            }
            // Return the score for this station and order combination
            return score;
        }

        private double ScoreWeightedOTW(OutputStation station, Order order)
        {
            double score = 0;
            // Go through all lines of the order to determine the score
            foreach (var position in order.Positions)
            {
                bool podDoesNotMatchPosition = false;
                // Determine the inventory available for this position from all inbound pods
                int inboundInventoryAvailable = 0;
                foreach (var pod in station.InboundPods)
                {
                    int podInventoryAvailable = pod.CountAvailable(position.Key);
                    // Check whether we can remove that item type from the pod completely
                    if (podInventoryAvailable >= position.Value)
                        podDoesNotMatchPosition = true;
                    // Return inbound inventory count
                    inboundInventoryAvailable += podInventoryAvailable;
                }
                //if (inboundInventoryAvailable == position.Value)
                if (podDoesNotMatchPosition)
                    // Bonus score if the Positioncount on the Pod is greater than needed
                    score += position.Value + 1 ;
                else
                    // Score is at least as much as the inventory we can immediately use for the order's position / line
                    score += Math.Min(inboundInventoryAvailable, position.Value);
            }
            // Return the score for this station and order combination
            return score;
        }

        private double ScoreDeadline(OutputStation station, Order order)
        {
            return -order.DueTime;
        }

        private double Score(ScoreFunctionStationOrder scoreFunction, OutputStation station, Order order)
        {
            double score;
            switch (scoreFunction)
            {
                case ScoreFunctionStationOrder.InboundPodsAvailablePicks: score = ScoreNormal(station, order); break;
                case ScoreFunctionStationOrder.InboundPodsAvailablePicksDepletePod: score = ScoreWeightedDepletePod(station, order); break;
                case ScoreFunctionStationOrder.Deadline: score = ScoreDeadline(station, order); break;
                case ScoreFunctionStationOrder.InboundPodsAvailablePicksNotDepletePod: score = ScoreWeightedOTW(station, order); break;
                default: throw new ArgumentException("Unknown score function: " + _config.ScoreFunctionStationOrder.ToString());
            }
            return score;
        }

        /// <summary>
        /// This is called to decide about potentially pending orders.
        /// This method is being timed for statistical purposes and is also ONLY called when <code>SituationInvestigated</code> is <code>false</code>.
        /// Hence, set the field accordingly to react on events not tracked by this outer skeleton.
        /// </summary>
        protected override void DecideAboutPendingOrders()
        {
            
            // Proposal:
            // Define filter function
            Func<OutputStation, bool> validStation = (OutputStation s) => { return s.Active && s.CapacityReserved + s.CapacityInUse < s.Capacity; };
           
            List<Order> validOrders = _pendingOrders.Where(o => o.Positions.All(p => Instance.StockInfo.GetActualStock(p.Key) >= p.Value)).ToList();
            // Assign orders while possible
            while (
                // Check whether there are orders left at all
                validOrders.Any() &&
                // Only go on, if there is an active station with sufficient capacity left
                Instance.OutputStations.Any(s => validStation(s)))
            {
                // Look for next station to assign orders to
                Order chosenOrder = null;
                int bestOrderIndex = -1;
                double bestScore = double.NegativeInfinity;
                double bestScoreSecondLevel = double.NegativeInfinity;
                OutputStation chosenStation = null;
                foreach (var station in Instance.OutputStations
                    // Station has to be valid
                    .Where(s => validStation(s))
                    // Order stations by best one to assign to
                    .OrderByDescending(s => s.Capacity - s.CapacityInUse - s.CapacityReserved))
                {
                    // Search for best order for the station
                    for (int orderIndex = 0; orderIndex < validOrders.Count; orderIndex++)
                    {
                        // Look at next order
                        Order order = validOrders[orderIndex];
                        // Determine score
                        double score = Score(_config.ScoreFunctionStationOrder, station, order);
                        // double score = order.Positions.Sum(position => Math.Min(station.InboundPods.Sum(pod => pod.CountAvailable(position.Key)), position.Value));
                        // Update best order and station combination, if it's better
                        if (bestScore < score)
                        {
                            chosenStation = station;
                            chosenOrder = order;
                            bestOrderIndex = orderIndex;
                            bestScore = score;
                            bestScoreSecondLevel = Score(_config.ScoreFunctionStationOrderSecondLevel, station, order);
                        }
                        // If we have a tie, check the second score function
                        else if (bestScore == score)
                        {
                            double scoreSecondLevel = Score(_config.ScoreFunctionStationOrderSecondLevel, station, order);
                            // Update best order and station combination, if the tie-breaker says it's better
                            if (bestScoreSecondLevel < scoreSecondLevel)
                            {
                                chosenStation = station;
                                chosenOrder = order;
                                bestOrderIndex = orderIndex;
                                bestScore = score;
                                bestScoreSecondLevel = scoreSecondLevel;
                            }
                        }
                    }
                    // Alternative:
                    //Order chosenOrder = validOrders.ArgMax(order => order.Positions.Sum(position => Math.Max(station.InboundPods.Sum(pod => pod.CountAvailable(position.Key)), position.Value)));
                }
                // Assign best order if available
                if (chosenOrder != null)
                {
                    validOrders.RemoveAt(bestOrderIndex);
                    AllocateOrder(chosenOrder, chosenStation);
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
