using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using RAWSimO.Core.IO;
using RAWSimO.Core.Items;
using RAWSimO.Core.Metrics;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.OrderBatching
{
    /// <summary>
    /// Defines default order selection rules.
    /// </summary>
    public enum DefaultOrderSelection
    {
        /// <summary>
        /// Chooses a random order.
        /// </summary>
        Random,
        /// <summary>
        /// Chooses the oldest order.
        /// </summary>
        FCFS,
        /// <summary>
        /// Chooses the order with the earliest due date.
        /// </summary>
        DueTime,
        /// <summary>
        /// Chooses the order with the highest combined frequency while also considering its age.
        /// </summary>
        FrequencyAge,
    }
    /// <summary>
    /// Defines default output station selection rules.
    /// </summary>
    public enum DefaultOutputStationSelection
    {
        /// <summary>
        /// Chooses a random station.
        /// </summary>
        Random,
        /// <summary>
        /// Chooses the least busy station.
        /// </summary>
        LeastBusy,
        /// <summary>
        /// Chooses the most busy station.
        /// </summary>
        MostBusy,
    }

    /// <summary>
    /// Implements a manager that randomly assigns orders to output-stations.
    /// </summary>
    public class DefaultOrderManager : OrderManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public DefaultOrderManager(Instance instance) : base(instance) { _config = instance.ControllerConfig.OrderBatchingConfig as DefaultOrderBatchingConfiguration; }

        /// <summary>
        /// The config of this controller.
        /// </summary>
        private DefaultOrderBatchingConfiguration _config;
        /// <summary>
        /// The station that was chosen last time.
        /// </summary>
        private OutputStation _lastChosenStation = null;

        private BestCandidateSelector _bestCandidateSelectOrder;
        private BestCandidateSelector _bestCandidateSelectStation;
        private BestCandidateSelector _bestCandidateSelectFastLane;
        private OutputStation _currentStation = null;
        private Order _currentOrder = null;
        private double _oldestOrderTimestamp = -1;
        private double _newestOrderTimestamp = -1;
        private VolatileIDDictionary<OutputStation, Pod> _nearestInboundPod;

        /// <summary>
        /// Checks whether another order is assignable to the given station.
        /// </summary>
        /// <param name="station">The station to check.</param>
        /// <returns><code>true</code> if there is another open slot, <code>false</code> otherwise.</returns>
        private bool IsAssignable(OutputStation station)
        { return station.Active && station.CapacityReserved + station.CapacityInUse < station.Capacity; }
        /// <summary>
        /// Checks whether another order is assignable to the given station.
        /// </summary>
        /// <param name="station">The station to check.</param>
        /// <returns><code>true</code> if there is another open slot and another one reserved for fast-lane, <code>false</code> otherwise.</returns>
        private bool IsAssignableKeepFastLaneSlot(OutputStation station)
        { return station.Active && station.CapacityReserved + station.CapacityInUse < station.Capacity - 1; }

        /// <summary>
        /// Initializes this controller.
        /// </summary>
        private void Initialize()
        {
            // Setup scorers
            switch (_config.OrderSelectionRule)
            {
                case DefaultOrderSelection.Random:
                    _bestCandidateSelectOrder = new BestCandidateSelector(false, () =>
                    {
                        return Instance.Randomizer.NextDouble();
                    });
                    break;
                case DefaultOrderSelection.FCFS:
                    _bestCandidateSelectOrder = new BestCandidateSelector(false, () =>
                    {
                        return _currentOrder.TimeStamp;
                    });
                    break;
                case DefaultOrderSelection.DueTime:
                    _bestCandidateSelectOrder = new BestCandidateSelector(false, () =>
                    {
                        return _currentOrder.DueTime;
                    });
                    break;
                case DefaultOrderSelection.FrequencyAge:
                    _bestCandidateSelectOrder = new BestCandidateSelector(false, () =>
                    {
                        double orderBirth = 1 - ((_currentOrder.TimeStamp - _oldestOrderTimestamp) / (_newestOrderTimestamp - _oldestOrderTimestamp));
                        double frequency = _currentOrder.Positions.Average(p => Instance.FrequencyTracker.GetMeasuredFrequency(p.Key));
                        return -(orderBirth * frequency);
                    });
                    break;
                default: throw new ArgumentException("Unknown selection rule: " + _config.OrderSelectionRule);
            }
            switch (_config.StationSelectionRule)
            {
                case DefaultOutputStationSelection.Random:
                    _bestCandidateSelectStation = new BestCandidateSelector(false, () =>
                    {
                        return Instance.Randomizer.NextDouble();
                    });
                    break;
                case DefaultOutputStationSelection.LeastBusy:
                    _bestCandidateSelectStation = new BestCandidateSelector(false, () =>
                    {
                        return ((_currentStation.CapacityInUse + _currentStation.CapacityReserved) / (double)_currentStation.Capacity);
                    });
                    break;
                case DefaultOutputStationSelection.MostBusy:
                    _bestCandidateSelectStation = new BestCandidateSelector(false, () =>
                    {
                        return -((_currentStation.CapacityInUse + _currentStation.CapacityReserved) / (double)_currentStation.Capacity);
                    });
                    break;
                default: throw new ArgumentException("Unknown selection rule: " + _config.OrderSelectionRule);
            }
            // Setup fast lane helpers
            _bestCandidateSelectFastLane = new BestCandidateSelector(true,
                // If we run into ties use the oldest order
                () =>
                {
                    switch (_config.FastLaneTieBreaker)
                    {
                        case Shared.FastLaneTieBreaker.Random: return Instance.Randomizer.NextDouble();
                        case Shared.FastLaneTieBreaker.EarliestDueTime: return -_currentOrder.DueTime;
                        case Shared.FastLaneTieBreaker.FCFS: return -_currentOrder.TimeStamp;
                        default: throw new ArgumentException("Unknown tie breaker: " + _config.FastLaneTieBreaker);
                    }
                });
            if (_config.FastLane)
                _nearestInboundPod = new VolatileIDDictionary<OutputStation, Pod>(Instance.OutputStations.Select(s => new VolatileKeyValuePair<OutputStation, Pod>(s, null)).ToList());
        }
        /// <summary>
        /// Prepares some meta information.
        /// </summary>
        private void PrepareAssessment()
        {
            if (_config.FastLane)
            {
                foreach (var station in Instance.OutputStations.Where(s => IsAssignable(s)))
                {
                    _nearestInboundPod[station] = station.InboundPods.ArgMin(p =>
                    {
                        if (p.Bot != null && p.Bot.CurrentWaypoint != null)
                            // Use the path distance (this should always be possible)
                            return Distances.CalculateShortestPathPodSafe(p.Bot.CurrentWaypoint, station.Waypoint, Instance);
                        else
                            // Use manhattan distance as a fallback
                            return Distances.CalculateManhattan(p, station, Instance.WrongTierPenaltyDistance);
                    });
                }
            }
        }

        /// <summary>
        /// This is called to decide about potentially pending orders.
        /// This method is being timed for statistical purposes and is also ONLY called when <code>SituationInvestigated</code> is <code>false</code>.
        /// Hence, set the field accordingly to react on events not tracked by this outer skeleton.
        /// </summary>
        protected override void DecideAboutPendingOrders()
        {
            // If not initialized, do it now
            if (_bestCandidateSelectOrder == null)
                Initialize();
            // Define filter functions
            Func<OutputStation, bool> validStationNormalAssignment = _config.FastLane ? (Func<OutputStation, bool>)IsAssignableKeepFastLaneSlot : IsAssignable;
            Func<OutputStation, bool> validStationFastLaneAssignment = IsAssignable;
            // Get some meta info
            PrepareAssessment();
            // Assign fast lane orders while possible
            bool furtherOptions = true;
            while (furtherOptions && _config.FastLane)
            {
                // Prepare helpers
                OutputStation chosenStation = null;
                Order chosenOrder = null;
                _bestCandidateSelectFastLane.Recycle();
                // Look for next station to assign orders to
                foreach (var station in Instance.OutputStations
                    // Station has to be valid
                    .Where(s => validStationFastLaneAssignment(s)))
                {
                    // Set station
                    _currentStation = station;
                    // Check whether there is a suitable pod
                    if (_nearestInboundPod[station] != null && _nearestInboundPod[station].GetDistance(station) < Instance.SettingConfig.Tolerance)
                    {
                        // Search for best order for the station in all fulfillable orders
                        foreach (var order in _pendingOrders.Where(o =>
                            // Order needs to be immediately fulfillable
                            o.Positions.All(p => _nearestInboundPod[station].CountAvailable(p.Key) >= p.Value)))
                        {
                            // Set order
                            _currentOrder = order;
                            // --> Assess combination
                            if (_bestCandidateSelectFastLane.Reassess())
                            {
                                chosenStation = _currentStation;
                                chosenOrder = _currentOrder;
                            }
                        }
                    }
                }
                // Assign best order if available
                if (chosenOrder != null)
                {
                    // Assign the order
                    AllocateOrder(chosenOrder, chosenStation);
                    // Log fast lane assignment
                    Instance.StatCustomControllerInfo.CustomLogOB1++;
                }
                else
                {
                    // No more options to assign orders to stations
                    furtherOptions = false;
                }
            }
            // Repeat normal assignment until no further options
            furtherOptions = true;
            while (furtherOptions)
            {
                // Prepare some helper values
                if (_config.OrderSelectionRule == DefaultOrderSelection.FrequencyAge)
                {
                    _oldestOrderTimestamp = _pendingOrders.Where(o => o.Positions.All(p => Instance.StockInfo.GetActualStock(p.Key) >= p.Value)).MinOrDefault(o => o.TimeStamp, -1);
                    _newestOrderTimestamp = _pendingOrders.Where(o => o.Positions.All(p => Instance.StockInfo.GetActualStock(p.Key) >= p.Value)).MaxOrDefault(o => o.TimeStamp, -1);
                    // Avoid division by zero, if necessary
                    if (_oldestOrderTimestamp == _newestOrderTimestamp)
                        _newestOrderTimestamp += 1;
                }
                // Choose order
                _bestCandidateSelectOrder.Recycle();
                Order bestOrder = null;
                foreach (var order in _pendingOrders.Where(o => o.Positions.All(p => Instance.StockInfo.GetActualStock(p.Key) >= p.Value)))
                {
                    // Update candidate
                    _currentOrder = order;
                    // Assess next order
                    if (_bestCandidateSelectOrder.Reassess())
                        bestOrder = _currentOrder;
                }
                // Check success
                if (bestOrder != null)
                {
                    // Try to reuse the last station for this order
                    OutputStation bestStation = null;
                    bool recycling = false;
                    if (_config.Recycle && _lastChosenStation != null && _lastChosenStation.Active && _lastChosenStation.FitsForReservation(bestOrder))
                    {
                        // Last chosen station can be used for this order too
                        bestStation = _lastChosenStation;
                        recycling = true;
                    }
                    else
                    {
                        // Choose new station
                        _bestCandidateSelectStation.Recycle();
                        foreach (var station in Instance.OutputStations.Where(s => validStationNormalAssignment(s)))
                        {
                            // Update candidate
                            _currentStation = station;
                            // Assess next order
                            if (_bestCandidateSelectStation.Reassess())
                                bestStation = _currentStation;
                        }
                        // Store decision
                        _lastChosenStation = bestStation;
                    }

                    // Check success
                    if (bestStation != null)
                    {
                        // Add the assignment to the ready list
                        AllocateOrder(bestOrder, bestStation);
                        // Log score statistics (order)
                        if (_statScorerValuesOrder == null)
                            _statScorerValuesOrder = _bestCandidateSelectOrder.BestScores.ToArray();
                        else
                            for (int i = 0; i < _bestCandidateSelectOrder.BestScores.Length; i++)
                                _statScorerValuesOrder[i] += _bestCandidateSelectOrder.BestScores[i];
                        _statOrderSelections++;
                        Instance.StatCustomControllerInfo.CustomLogOB1 = _statScorerValuesOrder[0] / _statOrderSelections;
                        // Log score statistics (station)
                        if (!recycling)
                        {
                            if (_statScorerValuesStation == null)
                                _statScorerValuesStation = _bestCandidateSelectStation.BestScores.ToArray();
                            else
                                for (int i = 0; i < _bestCandidateSelectStation.BestScores.Length; i++)
                                    _statScorerValuesStation[i] += _bestCandidateSelectStation.BestScores[i];
                            _statStationSelections++;
                            Instance.StatCustomControllerInfo.CustomLogOB2 = _statScorerValuesStation[0] / _statStationSelections;
                        }
                    }
                    else
                    {
                        // No further options
                        furtherOptions = false;
                    }
                }
                else
                {
                    // No further options
                    furtherOptions = false;
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

        #region Custom stat tracking

        /// <summary>
        /// Contains the aggregated scorer values.
        /// </summary>
        private double[] _statScorerValuesOrder = null;
        /// <summary>
        /// Contains the aggregated scorer values.
        /// </summary>
        private double[] _statScorerValuesStation = null;
        /// <summary>
        /// Contains the number of selections done.
        /// </summary>
        private double _statOrderSelections = 0;
        /// <summary>
        /// Contains the number of selections done.
        /// </summary>
        private double _statStationSelections = 0;
        /// <summary>
        /// The callback indicates a reset of the statistics.
        /// </summary>
        public override void StatReset()
        {
            _statScorerValuesOrder = null;
            _statScorerValuesStation = null;
            _statOrderSelections = 0;
            _statStationSelections = 0;
        }
        /// <summary>
        /// The callback that indicates that the simulation is finished and statistics have to submitted to the instance.
        /// </summary>
        public override void StatFinish()
        {
            Instance.StatCustomControllerInfo.CustomLogOBString =
                _statScorerValuesOrder == null || _statScorerValuesStation == null ? "" :
                string.Join(IOConstants.DELIMITER_CUSTOM_CONTROLLER_FOOTPRINT.ToString(), _statScorerValuesOrder.Select(e => e / _statOrderSelections).Select(e => e.ToString(IOConstants.FORMATTER))) +
                IOConstants.DELIMITER_CUSTOM_CONTROLLER_FOOTPRINT.ToString() +
                string.Join(IOConstants.DELIMITER_CUSTOM_CONTROLLER_FOOTPRINT.ToString(), _statScorerValuesStation.Select(e => e / _statOrderSelections).Select(e => e.ToString(IOConstants.FORMATTER)));
        }

        #endregion
    }
}
