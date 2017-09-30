using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Geometrics;
using RAWSimO.Core.Items;
using RAWSimO.Core.Management;
using RAWSimO.Core.Metrics;
using RAWSimO.Core.Waypoints;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.TaskAllocation
{
    /// <summary>
    /// A task allocation controller that aims to simply balance the robots across the stations.
    /// </summary>
    public class BalancedBotManager : BotManager
    {
        /// <summary>
        /// Creates a new instance of this controller.
        /// </summary>
        /// <param name="instance">The instance this controller belongs to.</param>
        public BalancedBotManager(Instance instance) : base(instance)
        {
            // Save config and instance
            Instance = instance;
            _config = instance.ControllerConfig.TaskAllocationConfig as BalancedTaskAllocationConfiguration;
            // Build list of bots used for station work
            _bots = instance.Bots
                // Take all bots that shall be used for the dynamic station assignment
                .Take((int)Math.Ceiling((_config.WeightInputStations + _config.WeightOutputStations) / (_config.WeightInputStations + _config.WeightOutputStations + _config.WeightRepositioning) * instance.Bots.Count))
                .ToList();
            // Use all remaining bots for repositioning
            _repositioningBots = instance.Bots.Except(_bots).ToHashSet();
            // Keep expended search radius within limits, if same tier is preferred
            if (_config.ExtendedSearchRadius > instance.WrongTierPenaltyDistance && _config.PreferSameTier)
                _config.ExtendedSearchRadius = instance.WrongTierPenaltyDistance;
            _stations = instance.InputStations.Cast<Circle>().Concat(instance.OutputStations).ToList();
            _singleStationLists = _stations.ToDictionary(k => k, v => new List<Circle>() { v });
            _allStationsList = _stations.ToDictionary(k => k, v => _stations.OrderBy(s => Distances.CalculateEuclid(v, s, instance.WrongTierPenaltyDistance)).ThenByDescending(s => s.GetDistance(s.Tier.Length / 2.0, s.Tier.Width / 2.0)).ToList());
            _stationBots = _stations.ToDictionary(k => k, v => new HashSet<Bot>());
            foreach (var bot in _bots)
                _unassignedBots.Add(bot);
            // Perform first allocation
            ReallocateBots(true);
            // Keep track of the stations the bots are currently working for
            _workerStations = instance.Bots.ToDictionary(k => k, v => _botStations.ContainsKey(v) ? _botStations[v] : null);
            _stationWorkerCount = _stations.ToDictionary(k => k, v => _workerStations.Count(s => s.Value == v));
            // Do not immediately reallocate again
            _nextReallocation = instance.Controller != null ? instance.Controller.CurrentTime + _config.BotReallocationTimeout : _config.BotReallocationTimeout;
        }

        /// <summary>
        /// The configuration.
        /// </summary>
        private BalancedTaskAllocationConfiguration _config;
        /// <summary>
        /// All stations.
        /// </summary>
        private List<Circle> _stations;
        /// <summary>
        /// All bots that are available to be allocated to stations.
        /// </summary>
        private List<Bot> _bots;
        /// <summary>
        /// Contains all stations in a list ordered by the distance to the key station.
        /// </summary>
        private Dictionary<Circle, List<Circle>> _allStationsList;
        /// <summary>
        /// Contains a list per station with the station in it as the only element.
        /// </summary>
        private Dictionary<Circle, List<Circle>> _singleStationLists;
        /// <summary>
        /// The stations the bots are assigned to.
        /// </summary>
        private Dictionary<Bot, Circle> _botStations = new Dictionary<Bot, Circle>();
        /// <summary>
        /// The bots per station.
        /// </summary>
        private Dictionary<Circle, HashSet<Bot>> _stationBots = new Dictionary<Circle, HashSet<Bot>>();
        /// <summary>
        /// The number of bots working for the respective station at the current moment (including bots not assigned but extending to that station).
        /// </summary>
        private Dictionary<Circle, int> _stationWorkerCount = new Dictionary<Circle, int>();
        /// <summary>
        /// The stations the bots are currently working for (respects the real work - not only the assignment).
        /// </summary>
        private Dictionary<Bot, Circle> _workerStations = new Dictionary<Bot, Circle>();
        /// <summary>
        /// Bots currently assigned to do repositioning jobs exclusively.
        /// </summary>
        private HashSet<Bot> _repositioningBots;
        /// <summary>
        /// All currently unemployed bots
        /// </summary>
        private HashSet<Bot> _unassignedBots = new HashSet<Bot>();
        /// <summary>
        /// Stores the stations that were active during the last update (to avoid unnecessary updates).
        /// </summary>
        private HashSet<Circle> _lastActiveStations;
        /// <summary>
        /// Keeps track of the next reallocation event.
        /// </summary>
        private double _nextReallocation = double.MinValue;

        /// <summary>
        /// Reallocates the bots between the stations.
        /// </summary>
        /// <param name="overrideActivity">Indicates whether the stations activity  will be ignored.</param>
        private void ReallocateBots(bool overrideActivity)
        {
            // Check active stations
            List<InputStation> activeInputStations = Instance.InputStations.Where(s => overrideActivity || s.ItemBundles.Any()).ToList();
            List<OutputStation> activeOutputStations = Instance.OutputStations.Where(s => overrideActivity || s.AssignedOrders.Any()).ToList();
            // Check whether something changed since last time
            IEnumerable<Circle> currentlyActiveStations = activeInputStations.Cast<Circle>().Concat(activeOutputStations);
            if (_lastActiveStations == null)
            { _lastActiveStations = new HashSet<Circle>(activeInputStations.Cast<Circle>().Concat(activeOutputStations)); }
            else if (currentlyActiveStations.All(s => _lastActiveStations.Contains(s)) && currentlyActiveStations.Count() == _lastActiveStations.Count)
            { return; }
            else
            { _lastActiveStations = new HashSet<Circle>(activeInputStations.Cast<Circle>().Concat(activeOutputStations)); }
            // Defining stations as active when there are available requests is too volatile, but maybe consider the request count somehow in the future?
            //List<InputStation> activeInputStations = Instance.ResourceManager.AvailableStoreRequests.Select(r => r.Station).Distinct().ToList();
            //List<OutputStation> activeOutputStations = Instance.ResourceManager.AvailableExtractRequests.Select(r => r.Station).Distinct().ToList();

            // Get count if input / output bots
            double activeWeightInputStations = _config.WeightInputStations * ((double)activeInputStations.Count / Instance.InputStations.Count);
            double activeWeightOutputStations = _config.WeightOutputStations * ((double)activeOutputStations.Count / Instance.OutputStations.Count);
            // Handle the extreme case of no active stations
            if (activeWeightInputStations == 0 && activeWeightOutputStations == 0)
            {
                // Set all bots to unassigned and quit
                foreach (var bot in _bots)
                {
                    _botStations[bot] = null;
                    _unassignedBots.Add(bot);
                }
                foreach (var station in _stations)
                {
                    _stationBots[station].Clear();
                }
                return;
            }
            // Divide input and output station bots
            int inputStationBots = Convert.ToInt32(Math.Floor(_bots.Count * (activeWeightInputStations / (activeWeightInputStations + activeWeightOutputStations))));
            int outputStationBots = Convert.ToInt32(Math.Floor(_bots.Count * (activeWeightOutputStations / (activeWeightInputStations + activeWeightOutputStations))));
            // If there is a 'fractional' bot, add it to the lower number
            if (inputStationBots + outputStationBots < _bots.Count)
                if (inputStationBots < outputStationBots)
                    inputStationBots += _bots.Count - inputStationBots - outputStationBots;
                else
                    outputStationBots += _bots.Count - inputStationBots - outputStationBots;
            // Order active stations by their preference for assigning bots
            activeInputStations =
                // Order active input stations by their distance to the middle of their tier (prefer stations more far away, because others are more likely to share bots)
                activeInputStations.OrderByDescending(s => s.GetDistance(s.Tier.Length / 2.0, s.Tier.Width / 2.0)).ToList();
            activeOutputStations =
                // Order active output stations by their distance to the middle of their tier (prefer stations more far away, because others are more likely to share bots)
                activeOutputStations.OrderByDescending(s => s.GetDistance(s.Tier.Length / 2.0, s.Tier.Width / 2.0)).ToList();
            // Obtain goal numbers per station
            Dictionary<Circle, int> stationBotGoals = _stations.ToDictionary(k => k, v => 0);
            for (int i = 0; inputStationBots > 0; i = (i + 1) % activeInputStations.Count)
            {
                if (stationBotGoals.ContainsKey(activeInputStations[i]))
                    stationBotGoals[activeInputStations[i]]++;
                else
                    stationBotGoals[activeInputStations[i]] = 1;
                inputStationBots--;
            }
            for (int i = 0; outputStationBots > 0; i = (i + 1) % activeOutputStations.Count)
            {
                if (stationBotGoals.ContainsKey(activeOutputStations[i]))
                    stationBotGoals[activeOutputStations[i]]++;
                else
                    stationBotGoals[activeOutputStations[i]] = 1;
                outputStationBots--;
            }
            // Limit bots per station
            int overflowBots = 0;
            foreach (var station in _stations)
            {
                if (stationBotGoals[station] > _config.BotsPerStationLimit)
                {
                    overflowBots += stationBotGoals[station] - _config.BotsPerStationLimit;
                    stationBotGoals[station] = _config.BotsPerStationLimit;
                }
            }
            // Distribute overflow bots across other stations, if possible
            while (overflowBots > 0 && stationBotGoals.Any(s => s.Value < _config.BotsPerStationLimit))
            {
                // Select a station at the border of its tier (stations nearer to the middle should share bots more easily)
                Circle receivingStation = _stations.Where(s => stationBotGoals[s] < _config.BotsPerStationLimit).ArgMax(s => s.GetDistance(s.Tier.Length / 2.0, s.Tier.Width / 2.0));
                stationBotGoals[receivingStation]++;
                overflowBots--;
            }
            // Keep on reassigning until all stations meet their goal
            List<Tuple<Bot, Circle, Circle>> reassignments = null;
            Dictionary<Circle, int> previousAssignment = null;
            while (_stations.Any(s => stationBotGoals[s] != _stationBots[s].Count))
            {
                // Remember current assignment counts
                if (previousAssignment == null)
                    previousAssignment = _stationBots.ToDictionary(k => k.Key, v => v.Value.Count);
                // Get next switching partners
                Bot bestBot = null; Circle receivingStation = null; double bestValue = double.PositiveInfinity; bool unassignedBot = true;
                // Check all station not yet at their goal
                foreach (var station in _stations.Where(s => _stationBots[s].Count < stationBotGoals[s]))
                {
                    // Check all unassigned bots or bots that are assigned to a station with an overflow of bots
                    foreach (var bot in _unassignedBots.Concat(_stationBots.Where(s => _stationBots[s.Key].Count > stationBotGoals[s.Key]).SelectMany(s => s.Value)))
                    {
                        // See whether the bot is nearer to the station than the previous ones
                        double distance = (bot.TargetWaypoint != null ? bot.TargetWaypoint.GetDistance(station) : Distances.CalculateEuclid(bot, station, Instance.WrongTierPenaltyDistance));
                        if (distance < bestValue)
                        {
                            // Set new best pair
                            bestValue = distance;
                            bestBot = bot;
                            receivingStation = station;
                            unassignedBot = _unassignedBots.Contains(bot);
                        }
                    }
                }
                // Store the switch
                if (reassignments != null)
                    reassignments.Add(new Tuple<Bot, Circle, Circle>(bestBot, unassignedBot ? null : _botStations[bestBot], receivingStation));
                else
                    reassignments = new List<Tuple<Bot, Circle, Circle>>() { new Tuple<Bot, Circle, Circle>(bestBot, unassignedBot ? null : _botStations[bestBot], receivingStation) };
                // Perform the switch
                if (unassignedBot)
                    _unassignedBots.Remove(bestBot);
                else
                    _stationBots[_botStations[bestBot]].Remove(bestBot);
                _botStations[bestBot] = receivingStation;
                _stationBots[receivingStation].Add(bestBot);
            }
            // Log reassignments
            if (reassignments != null)
            {
                Instance.LogVerbose("Reassigning " + reassignments.Count + " bots:");
                Instance.LogVerbose("ID:   " + string.Join(",", Instance.InputStations.Select(s => "I" + s.ID).Concat(Instance.OutputStations.Select(s => "O" + s.ID)).Select(s => s.PadLeft(3))));
                Instance.LogVerbose("Work: " + string.Join(",",
                    Instance.InputStations.Select(s => (s.ItemBundles.Any() ? s.ItemBundles.Count().ToString() : " ")).Concat(
                    Instance.OutputStations.Select(s => (s.AssignedOrders.Any() ? s.AssignedOrders.Count().ToString() : " "))).Select(s => s.PadLeft(3))));
                Instance.LogVerbose("Bots: " + string.Join(",",
                    Instance.InputStations.Select(s => _stationBots[s].Count.ToString()).Concat(
                    Instance.OutputStations.Select(s => _stationBots[s].Count.ToString())).Select(s => s.PadLeft(3))));
                Instance.LogVerbose("Was:  " + string.Join(",",
                    Instance.InputStations.Select(s => previousAssignment[s].ToString()).Concat(
                    Instance.OutputStations.Select(s => previousAssignment[s].ToString())).Select(s => s.PadLeft(3))));
            }
        }

        /// <summary>
        /// Gets the next task for the specified bot.
        /// </summary>
        /// <param name="bot">The bot to get a task for.</param>
        protected override void GetNextTask(Bot bot)
        {
            // Search all stations only if no resting or repositioning is intended for the robot
            if (// Ensure robot is not set for resting
                (_botStations.ContainsKey(bot) && _botStations[bot] != null) &&
                // Ensure robot is not set for repositioning
                !_repositioningBots.Contains(bot))
            {
                // Get enumeration of stations to check for work for the bot
                IEnumerable<Circle> potentialStations =
                    // If other stations shall be considered in addition to the assigned one, pass the respective ordered list of all stations
                    _config.SearchAll ? _allStationsList[_botStations[bot]] :
                    // Only the assigned station shall be considered - pass a list with only that station
                    _singleStationLists[_botStations[bot]];
                // Check all stations 
                bool success = false;
                foreach (var station in potentialStations.Where(s =>
                    // Check only stations not dynamically at their limit regarding worker bot count
                    _stationWorkerCount[s] < _config.BotsPerStationLimit))
                {
                    // If the bot is allocated to an input-station search for a store task for that station
                    if (station is InputStation)
                    {
                        // Try to do an insertion task
                        success = DoStoreTaskForStation(bot, station as InputStation,
                            // Extended search options
                            _config.ExtendSearch, _config.ExtendedSearchRadius,
                            // Pod selection rules
                            _config.PodSelectionConfig);
                        if (success)
                        {
                            // Keep track of who is working for whom
                            if (_workerStations[bot] != null)
                                _stationWorkerCount[_workerStations[bot]]--;
                            _workerStations[bot] = station;
                            _stationWorkerCount[station]++;
                            return;
                        }
                    }
                    // If the bot is allocated to an output-station search for an extract task for that station
                    if (station is OutputStation)
                    {
                        // Try to do an extraction task
                        success = DoExtractTaskForStation(bot, station as OutputStation,
                            // Extended search options
                            _config.ExtendSearch, _config.ExtendedSearchRadius,
                            // Pod selection rules
                            _config.PodSelectionConfig);
                        if (success)
                        {
                            // Keep track of who is working for whom
                            if (_workerStations[bot] != null)
                                _stationWorkerCount[_workerStations[bot]]--;
                            _workerStations[bot] = station;
                            _stationWorkerCount[station]++;
                            return;
                        }
                    }
                }
            }
            // If we still carry a pod at this point, get rid of it
            if (DoParkPodTask(bot))
                return;
            // Do repositioning, if assigned to it or no other task to do and repositioning is allowed as a substitute task
            if (_config.RepositionBeforeRest || _repositioningBots.Contains(bot))
                // Attempt to do a repositioning move - if none available, just rest
                DoRepositioningTaskOrRest(bot, _config.RestLocationOrderType);
            else
                // Just rest
                DoRestTask(bot, _config.RestLocationOrderType);
            // Keep track of who is working for whom
            if (_workerStations[bot] != null)
                _stationWorkerCount[_workerStations[bot]]--;
            _workerStations[bot] = null;
        }

        /// <summary>
        /// Gets the number of bots currently assigned to the given station.
        /// </summary>
        /// <param name="station">The station.</param>
        /// <returns>The number of robots assigned to the station.</returns>
        internal int GetAssignedBotCount(Circle station) { return _stationBots.ContainsKey(station) ? _stationBots[station].Count : 0; }

        /// <summary>
        /// The next event when this element has to be updated.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns>The next time this element has to be updated.</returns>
        public override double GetNextEventTime(double currentTime) { return _nextReallocation; }
        /// <summary>
        /// Updates the element to the specified time.
        /// </summary>
        /// <param name="lastTime">The time before the update.</param>
        /// <param name="currentTime">The time to update to.</param>
        public override void Update(double lastTime, double currentTime)
        {
            // Redistribute bots (if required)
            if (_config.BotReallocationTimeout > 0 && _nextReallocation <= currentTime)
            {
                // Update event time
                _nextReallocation = currentTime + _config.BotReallocationTimeout;
                // Reallocate bots
                ReallocateBots(false);
            }
            // Try to do on-the-fly work
            AssignOnTheFlyWork(_config.PodSelectionConfig);
        }

        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        public override void SignalCurrentTime(double currentTime) { /* Not necessary */ }
    }
}
