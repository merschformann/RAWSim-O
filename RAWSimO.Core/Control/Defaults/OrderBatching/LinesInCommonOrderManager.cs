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
    /// Implements a manager that assigns orders most similar to the ones already assigned to a station.
    /// </summary>
    public class LinesInCommonOrderManager : OrderManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public LinesInCommonOrderManager(Instance instance) : base(instance) { _config = instance.ControllerConfig.OrderBatchingConfig as LinesInCommonOrderBatchingConfiguration; }

        /// <summary>
        /// The config of this controller.
        /// </summary>
        private LinesInCommonOrderBatchingConfiguration _config;

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

        private BestCandidateSelector _bestCandidateSelectNormal;
        private BestCandidateSelector _bestCandidateSelectFastLane;
        private OutputStation _currentStation = null;
        private Order _currentOrder = null;
        private VolatileIDDictionary<OutputStation, Pod> _nearestInboundPod;

        /// <summary>
        /// Initializes this controller.
        /// </summary>
        private void Initialize()
        {
            // Set some values for statistics
            _statLinesInCommonScoreIndex = 0;
            // Setup scorers
            _bestCandidateSelectNormal = new BestCandidateSelector(true,
                // First select order with most lines in common
                () => { return _currentOrder.Positions.Sum(line => _currentStation.AssignedOrders.Sum(o => o.GetDemandCount(line.Key) > 0 ? 1 : 0)); },
                // If we run into ties use the oldest order
                () =>
                {
                    switch (_config.TieBreaker)
                    {
                        case Shared.OrderSelectionTieBreaker.Random: return Instance.Randomizer.NextDouble();
                        case Shared.OrderSelectionTieBreaker.EarliestDueTime: return -_currentOrder.DueTime;
                        case Shared.OrderSelectionTieBreaker.FCFS: return -_currentOrder.TimeStamp;
                        default: throw new ArgumentException("Unknown tie breaker: " + _config.FastLaneTieBreaker);
                    }
                });
            _bestCandidateSelectFastLane = new BestCandidateSelector(true,
                // If we run into ties, break them
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
            if (_bestCandidateSelectNormal == null)
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
            // Assign orders while possible
            furtherOptions = true;
            while (furtherOptions)
            {
                // Prepare helpers
                OutputStation chosenStation = null;
                Order chosenOrder = null;
                _bestCandidateSelectNormal.Recycle();
                // Look for next station to assign orders to
                foreach (var station in Instance.OutputStations
                    // Station has to be valid
                    .Where(s => validStationNormalAssignment(s)))
                {
                    // Set station
                    _currentStation = station;
                    // Search for best order for the station in all fulfillable orders
                    foreach (var order in _pendingOrders.Where(o => o.Positions.All(p => Instance.StockInfo.GetActualStock(p.Key) >= p.Value)))
                    {
                        // Set order
                        _currentOrder = order;
                        // --> Assess combination
                        if (_bestCandidateSelectNormal.Reassess())
                        {
                            chosenStation = _currentStation;
                            chosenOrder = _currentOrder;
                        }
                    }
                }
                // Assign best order if available
                if (chosenOrder != null)
                {
                    // Assign the order
                    AllocateOrder(chosenOrder, chosenStation);
                    // Log score statistics
                    if (_statScorerValues == null)
                        _statScorerValues = _bestCandidateSelectNormal.BestScores.ToArray();
                    else
                        for (int i = 0; i < _bestCandidateSelectNormal.BestScores.Length; i++)
                            _statScorerValues[i] += _bestCandidateSelectNormal.BestScores[i];
                    _statAssignments++;
                    Instance.StatCustomControllerInfo.CustomLogOB2 = _statScorerValues[_statLinesInCommonScoreIndex] / _statAssignments;
                }
                else
                {
                    // No more options to assign orders to stations
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
        private double[] _statScorerValues = null;
        /// <summary>
        /// Contains the number of assignments done.
        /// </summary>
        private double _statAssignments = 0;
        /// <summary>
        /// The index of the lines in common scorer.
        /// </summary>
        private int _statLinesInCommonScoreIndex = -1;
        /// <summary>
        /// The callback indicates a reset of the statistics.
        /// </summary>
        public override void StatReset()
        {
            _statScorerValues = null;
            _statAssignments = 0;
        }
        /// <summary>
        /// The callback that indicates that the simulation is finished and statistics have to submitted to the instance.
        /// </summary>
        public override void StatFinish()
        {
            Instance.StatCustomControllerInfo.CustomLogOBString =
                _statScorerValues == null ? "" :
                string.Join(IOConstants.DELIMITER_CUSTOM_CONTROLLER_FOOTPRINT.ToString(), _statScorerValues.Select(e => e / _statAssignments).Select(e => e.ToString(IOConstants.FORMATTER)));
        }

        #endregion
    }
}
