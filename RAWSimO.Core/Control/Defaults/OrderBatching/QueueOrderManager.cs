using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Items;
using RAWSimO.Core.Management;
using RAWSimO.Core.Metrics;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RAWSimO.Core.Configurations.QueueOrderBatchingConfiguration;

namespace RAWSimO.Core.Control.Defaults.OrderBatching
{
    /// <summary>
    /// An order manager building order queues per station.
    /// </summary>
    public class QueueOrderManager : OrderManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public QueueOrderManager(Instance instance) : base(instance)
        {
            _config = instance.ControllerConfig.OrderBatchingConfig as QueueOrderBatchingConfiguration;
            _stationQueues = new VolatileIDDictionary<OutputStation, HashSet<Order>>(instance.OutputStations.Select(s => new VolatileKeyValuePair<OutputStation, HashSet<Order>>(s, new HashSet<Order>())).ToList());
            _inboundPodsByDistance = new VolatileIDDictionary<OutputStation, List<Pod>>(instance.OutputStations.Select(s => new VolatileKeyValuePair<OutputStation, List<Pod>>(s, new List<Pod>())).ToList());
            _nearestInboundPod = new VolatileIDDictionary<OutputStation, Pod>(instance.OutputStations.Select(s => new VolatileKeyValuePair<OutputStation, Pod>(s, null)).ToList());
        }

        /// <summary>
        /// The config of this controller.
        /// </summary>
        private QueueOrderBatchingConfiguration _config;

        /// <summary>
        /// The queues of orders per station.
        /// </summary>
        private VolatileIDDictionary<OutputStation, HashSet<Order>> _stationQueues;

        /// <summary>
        /// Helper to select the next order for an output station queue.
        /// </summary>
        private BestCandidateSelector _bestCandidateSelectorQueue;
        /// <summary>
        /// Helper to select the next order for the fast lane of an output station.
        /// </summary>
        private BestCandidateSelector _bestCandidateSelectorFastLane;
        /// <summary>
        /// Helper to select the next order for an output station itself.
        /// </summary>
        private BestCandidateSelector _bestCandidateSelectorStation;

        /// <summary>
        /// Current order to assess.
        /// </summary>
        private Order _currentOrder;
        /// <summary>
        /// Current station to assess.
        /// </summary>
        private OutputStation _currentStation;

        private VolatileIDDictionary<OutputStation, List<Pod>> _inboundPodsByDistance;
        private VolatileIDDictionary<OutputStation, Pod> _nearestInboundPod;
        private VolatileIDDictionary<ItemDescription, int> _orderItemDemand;

        #region Init functions

        /// <summary>
        /// Inits the controller.
        /// </summary>
        private void Init()
        {
            // Init queue assignment assessment
            if (_bestCandidateSelectorQueue == null)
            {
                List<Func<double>> scorers = new List<Func<double>>();
                if (_config.QueueOrderSelectionRule1 != null)
                    scorers.Add(GenerateScorer(_config.QueueOrderSelectionRule1, false));
                if (_config.QueueOrderSelectionRule2 != null)
                    scorers.Add(GenerateScorer(_config.QueueOrderSelectionRule2, false));
                if (_config.QueueOrderSelectionRule3 != null)
                    scorers.Add(GenerateScorer(_config.QueueOrderSelectionRule3, false));
                _bestCandidateSelectorQueue = new BestCandidateSelector(true, scorers.ToArray());
            }
            // Init fast lane assignment assessment
            if (_bestCandidateSelectorFastLane == null)
            {
                List<Func<double>> scorers = new List<Func<double>>();
                if (_config.FastLaneOrderSelectionRule1 != null)
                    scorers.Add(GenerateScorer(_config.FastLaneOrderSelectionRule1, true));
                if (_config.FastLaneOrderSelectionRule2 != null)
                    scorers.Add(GenerateScorer(_config.FastLaneOrderSelectionRule2, true));
                if (_config.FastLaneOrderSelectionRule3 != null)
                    scorers.Add(GenerateScorer(_config.FastLaneOrderSelectionRule3, true));
                _bestCandidateSelectorFastLane = new BestCandidateSelector(true, scorers.ToArray());
            }
            // Init station assignment assessment
            if (_bestCandidateSelectorStation == null)
            {
                List<Func<double>> scorers = new List<Func<double>>();
                if (_config.StationOrderSelectionRule1 != null)
                    scorers.Add(GenerateScorer(_config.StationOrderSelectionRule1, true));
                if (_config.StationOrderSelectionRule2 != null)
                    scorers.Add(GenerateScorer(_config.StationOrderSelectionRule2, true));
                if (_config.StationOrderSelectionRule3 != null)
                    scorers.Add(GenerateScorer(_config.StationOrderSelectionRule3, true));
                _bestCandidateSelectorStation = new BestCandidateSelector(true, scorers.ToArray());
            }
        }

        /// <summary>
        /// Instantiates a scoring function from the given config.
        /// </summary>
        /// <param name="scorerConfig">The config.</param>
        /// <param name="stationAssignment">Indicates whether the order shall be assigned to the station directly.</param>
        /// <returns>The scoring function.</returns>
        private Func<double> GenerateScorer(QueueOrderSelectionRuleConfig scorerConfig, bool stationAssignment)
        {
            switch (scorerConfig.Type())
            {
                case QueueOrderSelectionRuleType.Random:
                    { QueueOrderSelectionRandom tempcfg = scorerConfig as QueueOrderSelectionRandom; return () => { return Score(tempcfg, stationAssignment); }; }
                case QueueOrderSelectionRuleType.FCFS:
                    { QueueOrderSelectionFCFS tempcfg = scorerConfig as QueueOrderSelectionFCFS; return () => { return Score(tempcfg, stationAssignment); }; }
                case QueueOrderSelectionRuleType.EarliestDeadline:
                    { QueueOrderSelectionEarliestDeadline tempcfg = scorerConfig as QueueOrderSelectionEarliestDeadline; return () => { return Score(tempcfg, stationAssignment); }; }
                case QueueOrderSelectionRuleType.VacantDeadline:
                    { QueueOrderSelectionDeadlineVacant tempcfg = scorerConfig as QueueOrderSelectionDeadlineVacant; return () => { return Score(tempcfg, stationAssignment); }; }
                case QueueOrderSelectionRuleType.InboundMatches:
                    { QueueOrderSelectionInboundMatches tempcfg = scorerConfig as QueueOrderSelectionInboundMatches; return () => { return Score(tempcfg, stationAssignment); }; }
                case QueueOrderSelectionRuleType.Completable:
                    { QueueOrderSelectionCompleteable tempcfg = scorerConfig as QueueOrderSelectionCompleteable; return () => { return Score(tempcfg, stationAssignment); }; }
                case QueueOrderSelectionRuleType.Lines:
                    { QueueOrderSelectionMostLines tempcfg = scorerConfig as QueueOrderSelectionMostLines; return () => { return Score(tempcfg, stationAssignment); }; }
                case QueueOrderSelectionRuleType.Related:
                    { QueueOrderSelectionRelated tempcfg = scorerConfig as QueueOrderSelectionRelated; return () => { return Score(tempcfg, stationAssignment); }; }
                default: throw new ArgumentException("Unknown scorer type: " + scorerConfig.Type().ToString());
            }
        }

        #endregion

        #region Helper functions

        /// <summary>
        /// Prepares another assessment run.
        /// </summary>
        private void PrepareAssessment()
        {
            if (_orderItemDemand == null)
                _orderItemDemand = new VolatileIDDictionary<ItemDescription, int>(Instance.ItemDescriptions.Select(i => new VolatileKeyValuePair<ItemDescription, int>(i, 0)).ToList());
            Instance.OutputStations.ForEach(station => _inboundPodsByDistance[station] = station.InboundPods.OrderBy(p =>
            {
                if (p.Bot != null && p.Bot.CurrentWaypoint != null)
                    // Use the path distance (this should always be possible)
                    return Distances.CalculateShortestPathPodSafe(p.Bot.CurrentWaypoint, station.Waypoint, Instance);
                else
                    // Use manhattan distance as a fallback
                    return Distances.CalculateManhattan(p, station, Instance.WrongTierPenaltyDistance);
            }).ToList());
            Instance.OutputStations.ForEach(station => _nearestInboundPod[station] = _inboundPodsByDistance[station].FirstOrDefault()); ;
        }
        /// <summary>
        /// Indicates whether the station is available for assigning an order to it.
        /// </summary>
        /// <param name="s">The station to check.</param>
        /// <returns><code>true</code> if an order can be assigned to the station, <code>false</code> otherwise.</returns>
        private bool ValidStationIgnoreFastLane(OutputStation s) { return s.Active && s.CapacityReserved + s.CapacityInUse < s.Capacity; }
        /// <summary>
        /// Indicates whether the station is available for assigning an order to it while keeping one slot open for fast lane.
        /// </summary>
        /// <param name="s">The station to check.</param>
        /// <returns><code>true</code> if an order can be assigned to the station, <code>false</code> otherwise.</returns>
        private bool ValidStationKeepFastLane(OutputStation s) { return s.Active && s.CapacityReserved + s.CapacityInUse < s.Capacity - 1; }
        /// <summary>
        /// Validates that the given combination is a valid assignment to the fast-lane slot of a station.
        /// </summary>
        /// <param name="s">The station.</param>
        /// <param name="o">The order.</param>
        /// <returns><code>true</code> if the assignment is valid, <code>false</code> otherwise.</returns>
        private bool ValidFastLaneAssignment(OutputStation s, Order o)
        {
            return _nearestInboundPod[s] == null || _nearestInboundPod[s].GetDistance(s) > Instance.SettingConfig.Tolerance ? false :
                    o.Requests.Any(r => r.State != Management.RequestState.Finished && r.Pod != null && r.Pod != _nearestInboundPod[s]) ? false :
                    o.Requests.Where(r => r.State != Management.RequestState.Finished && r.Pod == null).GroupBy(r => r.Item)
                        .All(i => _nearestInboundPod[s].CountAvailable(i.Key) >= i.Count()) ? true : false;
        }

        #endregion

        #region Scorers

        private double Score(QueueOrderSelectionRandom config, bool queueAssignment)
        {
            // Simply return a random number for the score
            return Instance.Randomizer.NextDouble();
        }
        private double Score(QueueOrderSelectionFCFS config, bool queueAssignment)
        {
            // Simply use the inverted arrival timestamp as the score
            return -_currentOrder.TimeStamp;
        }
        private double Score(QueueOrderSelectionEarliestDeadline config, bool queueAssignment)
        {
            // Simply use the inverted due time as the score
            return -_currentOrder.DueTime;
        }
        private double Score(QueueOrderSelectionDeadlineVacant config, bool queueAssignment)
        {
            // Prefer orders that are within vacancy range
            return _currentOrder.DueTime <= Instance.Controller.CurrentTime + config.VacantOrderCutoff ? 1 : 0;
        }
        private double Score(QueueOrderSelectionInboundMatches config, bool queueAssignment)
        {
            // Init
            int podCount = _inboundPodsByDistance[_currentStation].Count;
            int podNumber = 0;
            double score = 0;
            // Get demands given by the current order
            List<IGrouping<ItemDescription, ExtractRequest>> demands = Instance.ResourceManager.GetExtractRequestsOfOrder(_currentOrder).GroupBy(r => r.Item).ToList();
            foreach (var itemRequests in demands)
                _orderItemDemand[itemRequests.Key] = itemRequests.Count();
            // Check all inbound pods
            foreach (var pod in _inboundPodsByDistance[_currentStation])
            {
                // Get distance to pod
                double distance;
                if (pod.Bot != null && pod.Bot.CurrentWaypoint != null)
                    // Use the path distance (this should always be possible)
                    distance = Distances.CalculateShortestPathPodSafe(pod.Bot.CurrentWaypoint, _currentStation.Waypoint, Instance);
                else
                    // Use manhattan distance as a fallback
                    distance = Distances.CalculateManhattan(pod, _currentStation, Instance.WrongTierPenaltyDistance);
                // Check all demands for the order for availability in the pod
                foreach (var item in demands.Select(d => d.Key))
                {
                    // If there is no demand left, skip this item
                    if (_orderItemDemand[item] <= 0)
                        continue;
                    // Get available count
                    int availableCount = Math.Min(_orderItemDemand[item], pod.CountAvailable(item));
                    // Update demand for item with available count
                    _orderItemDemand[item] -= availableCount;
                    // Update score by taking distance to pod into account
                    score += availableCount * distance > config.DistanceForWeighting ?
                        // Pod is too far away, do not weight it's resulting score
                        1 :
                        // Pod is sufficiently near, weight it's score by it's position in queue
                        (podCount - podNumber);
                }
                // Track pod number
                podNumber++;
            }
            // Return the score
            return score;
        }
        private double Score(QueueOrderSelectionCompleteable config, bool queueAssignment)
        {
            // Prefer orders that can be completed with inbound inventory
            if (config.OnlyNearestPod)
                // Order is completable with nearest pod
                return _nearestInboundPod[_currentStation] == null ? 0 :
                    _currentOrder.Requests.Any(r => r.State != Management.RequestState.Finished && r.Pod != null && r.Pod != _nearestInboundPod[_currentStation]) ? 0 :
                    _currentOrder.Requests.Where(r => r.State != Management.RequestState.Finished && r.Pod == null).GroupBy(r => r.Item)
                        .All(i => _nearestInboundPod[_currentStation].CountAvailable(i.Key) >= i.Count()) ? 1 : 0;
            else
                // Order is completable with all inbound pods
                return _currentOrder.Requests.Where(r => r.State != Management.RequestState.Finished && r.Pod == null).GroupBy(r => r.Item)
                        .All(i => _currentStation.InboundPods.Sum(pod => pod.CountAvailable(i.Key)) >= i.Count()) ? 1 : 0;
        }
        private double Score(QueueOrderSelectionMostLines config, bool queueAssignment)
        {
            // Most lines or most units?
            if (config.UnitsInsteadOfLines)
                // Simply return the number of units overall
                return _currentOrder.Positions.Sum(p => p.Value);
            else
                // Simply return the number of lines
                return _currentOrder.Positions.Count();
        }
        private double Score(QueueOrderSelectionRelated config, bool queueAssignment)
        {
            // Prefer orders similar to the ones already in the pool
            if (queueAssignment)
                // Return fraction of SKUs necessary for other orders in the queue order pool
                return _currentOrder.Positions.Sum(p => _stationQueues[_currentStation].Any(o => o.PositionOverallCount(p.Key) > 0) ? 1 : 0) / _currentOrder.Positions.Count();
            else
                // Return fraction of SKUs still needed to fulfill orders already assigned to the station
                return _currentOrder.Positions.Sum(p => _currentStation.AssignedOrders.Any(o => o.PositionOverallCount(p.Key) - o.PositionServedCount(p.Key) > 0) ? 1 : 0) / _currentOrder.Positions.Count();
        }

        #endregion

        /// <summary>
        /// This is called to decide about potentially pending orders.
        /// This method is being timed for statistical purposes and is also ONLY called when <code>SituationInvestigated</code> is <code>false</code>.
        /// Hence, set the field accordingly to react on events not tracked by this outer skeleton.
        /// </summary>
        protected override void DecideAboutPendingOrders()
        {
            // --> Define filter functions
            // Define station filter for order to station assignment
            Func<OutputStation, bool> validStation = _config.FastLane ? (Func<OutputStation, bool>)ValidStationKeepFastLane : ValidStationIgnoreFastLane;
            // Only assign as many orders to the queue as specified
            Func<HashSet<Order>, bool> validQueue = (HashSet<Order> q) => { return q.Count < _config.QueueLength; };

            // --> Initiate
            Init();
            // Keep track of assignment success
            bool success = false;

            // --> Prepare meta information (if any valid assignment opportunity)
            if (Instance.OutputStations.Any(s => (_config.FastLane && ValidStationIgnoreFastLane(s)) || validStation(s)) || _stationQueues.Values.Any(q => validQueue(q)))
                PrepareAssessment();

            // --> ASSIGN ORDERS TO QUEUES
            do
            {
                // Reset indicator
                success = false;
                // Reset
                _bestCandidateSelectorQueue.Recycle();
                Order bestOrder = null;
                OutputStation bestStation = null;

                // Try all assignable orders
                foreach (var order in _pendingOrders.Where(o => o.Positions.All(p => Instance.StockInfo.GetActualStock(p.Key) >= p.Value)))
                {
                    // Update current order
                    _currentOrder = order;

                    // Try all station queues
                    foreach (var stationQueueKVP in _stationQueues.Where(q => validQueue(q.Value)))
                    {
                        // Update current station
                        _currentStation = stationQueueKVP.Key;

                        // Assess
                        if (_bestCandidateSelectorQueue.Reassess())
                        {
                            bestOrder = _currentOrder;
                            bestStation = _currentStation;
                        }
                    }
                }

                // Commit best assignment
                if (bestOrder != null && bestStation != null)
                {
                    success = true;
                    _pendingOrders.Remove(bestOrder);
                    _stationQueues[bestStation].Add(bestOrder);
                    // Announce queue decision
                    Instance.Controller.Allocator.Queue(bestOrder, bestStation);
                }
            }
            while (success);

            // --> ASSIGN ORDERS TO FAST-LANE OF THE STATIONS
            if (_config.FastLane)
            {
                do
                {
                    // Reset indicator
                    success = false;
                    // Assign orders from queue for all valid stations
                    foreach (var station in Instance.OutputStations.Where(s => ValidStationIgnoreFastLane(s)))
                    {
                        // Only consider, if pod is in front of station
                        if (_nearestInboundPod[station] == null || _nearestInboundPod[station].GetDistance(station) > Instance.SettingConfig.Tolerance)
                            continue;
                        // Reset
                        _bestCandidateSelectorFastLane.Recycle();
                        Order bestOrder = null;
                        _currentStation = station;
                        // Check all queued orders
                        foreach (var order in _stationQueues[station])
                        {
                            // Update current order
                            _currentOrder = order;
                            // Only consider valid assignment
                            if (!ValidFastLaneAssignment(_currentStation, _currentOrder))
                                continue;
                            // Assess
                            if (_bestCandidateSelectorFastLane.Reassess())
                            {
                                bestOrder = _currentOrder;
                            }
                        }
                        // Commit best assignment
                        if (bestOrder != null)
                        {
                            success = true;
                            _stationQueues[station].Remove(bestOrder);
                            AllocateOrder(bestOrder, station);
                        }
                        else
                        {
                            // Reset
                            _bestCandidateSelectorFastLane.Recycle();
                            bestOrder = null;
                            _currentStation = station;
                            // Check all pending orders
                            foreach (var order in _pendingOrders)
                            {
                                // Update current order
                                _currentOrder = order;
                                // Only consider valid assignment
                                if (!ValidFastLaneAssignment(_currentStation, _currentOrder))
                                    continue;
                                // Assess
                                if (_bestCandidateSelectorFastLane.Reassess())
                                {
                                    bestOrder = _currentOrder;
                                }
                            }
                            // Commit best assignment
                            if (bestOrder != null)
                            {
                                success = true;
                                _pendingOrders.Remove(bestOrder);
                                AllocateOrder(bestOrder, station);
                            }
                        }
                    }
                }
                while (success);
            }

            // --> ASSIGN ORDERS FROM QUEUES TO STATIONS
            do
            {
                // Reset indicator
                success = false;
                // Assign orders from queue for all valid stations
                foreach (var station in Instance.OutputStations.Where(s => validStation(s)))
                {
                    // Reset
                    _bestCandidateSelectorStation.Recycle();
                    Order bestOrder = null;
                    _currentStation = station;
                    // Check all queued orders
                    foreach (var order in _stationQueues[station].Where(o => o.Positions.All(p => Instance.StockInfo.GetActualStock(p.Key) >= p.Value)))
                    {
                        // Update current order
                        _currentOrder = order;
                        // Assess
                        if (_bestCandidateSelectorStation.Reassess())
                        {
                            bestOrder = _currentOrder;
                        }
                    }
                    // Commit best assignment
                    if (bestOrder != null)
                    {
                        success = true;
                        _stationQueues[station].Remove(bestOrder);
                        AllocateOrder(bestOrder, station);
                    }
                }
            }
            while (success);
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
