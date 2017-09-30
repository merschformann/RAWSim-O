using RAWSimO.Core.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Items;
using RAWSimO.Toolbox;
using RAWSimO.Core.Management;
using RAWSimO.Core.Geometrics;
using RAWSimO.Core.Metrics;

namespace RAWSimO.Core.Control.Defaults.TaskAllocation
{
    /// <summary>
    /// A task allocation controller that aims to simply balance the robots across the stations.
    /// </summary>
    public class ConstantRatioBotManager : BotManager
    {
        /// <summary>
        /// Creates a new instance of this controller.
        /// </summary>
        /// <param name="instance">The instance this controller belongs to.</param>
        public ConstantRatioBotManager(Instance instance) : base(instance)
        {
            // Save config and instance
            Instance = instance;
            _config = instance.ControllerConfig.TaskAllocationConfig as ConstantRatioTaskAllocationConfiguration;
            _stations = instance.InputStations.Cast<Circle>().Concat(instance.OutputStations).ToList();
            _stationBots = _stations.ToDictionary(k => k, v => new HashSet<Bot>());
            foreach (var bot in instance.Bots)
                _unassignedBots.Add(bot);
            // Assign all bots
            UpdateRobotAllocation(true);
        }
        /// <summary>
        /// The configuration.
        /// </summary>
        private ConstantRatioTaskAllocationConfiguration _config;
        /// <summary>
        /// The last update of the robot allocation.
        /// </summary>
        private double _lastAllocationUpdate = double.NegativeInfinity;
        /// <summary>
        /// Contains all bot to station allocations.
        /// </summary>
        private Dictionary<Bot, Circle> _botStations = new Dictionary<Bot, Circle>();
        /// <summary>
        /// The bots per station.
        /// </summary>
        private Dictionary<Circle, HashSet<Bot>> _stationBots = new Dictionary<Circle, HashSet<Bot>>();
        /// <summary>
        /// All stations.
        /// </summary>
        private List<Circle> _stations;
        /// <summary>
        /// Stores the stations that were active during the last update (to avoid unnecessary updates).
        /// </summary>
        private HashSet<Circle> _lastActiveStations;
        /// <summary>
        /// All currently unemployed bots. Should only contain bots at start.
        /// </summary>
        private HashSet<Bot> _unassignedBots = new HashSet<Bot>();

        /// <summary>
        /// Gets the number of bots currently assigned to the given station.
        /// </summary>
        /// <param name="station">The station.</param>
        /// <returns>The number of robots assigned to the station.</returns>
        internal int GetAssignedBotCount(Circle station) { return _stationBots.ContainsKey(station) ? _stationBots[station].Count : 0; }

        /// <summary>
        /// Updates the allocation of robots to station, if stations became active or inactive in the meantime.
        /// </summary>
        public void UpdateRobotAllocation(bool firstAllocation)
        {
            // --> Allocate bots
            int pickBots = (int)(Instance.Bots.Count * _config.PickBotRatio);
            int replenishmentBots = Instance.Bots.Count - pickBots;
            // Check active stations
            List<InputStation> activeInputStations = Instance.InputStations.Where(s => firstAllocation || s.Active).ToList();
            List<OutputStation> activeOutputStations = Instance.OutputStations.Where(s => firstAllocation || s.Active).ToList();
            // Check whether something changed since last time
            IEnumerable<Circle> currentlyActiveStations = activeInputStations.Cast<Circle>().Concat(activeOutputStations);
            // If it's the first allocation, remember the 'last' active stations for next time
            if (_lastActiveStations == null)
            { _lastActiveStations = new HashSet<Circle>(activeInputStations.Cast<Circle>().Concat(activeOutputStations)); }
            // If nothing changed, don't do anything
            else if (currentlyActiveStations.All(s => _lastActiveStations.Contains(s)) && currentlyActiveStations.Count() == _lastActiveStations.Count)
            { return; }
            // If there is no active station at all, ignore the update
            else if(!activeInputStations.Any(s=>s.Active) && ! activeOutputStations.Any(s=>s.Active))
            { return; }
            // Update the new 'last' active stations for next time
            else
            { _lastActiveStations = new HashSet<Circle>(activeInputStations.Cast<Circle>().Concat(activeOutputStations)); }
            // If there is a 'fractional' bot, add it to the lower number
            if (replenishmentBots + pickBots < Instance.Bots.Count)
                if (replenishmentBots < pickBots)
                    replenishmentBots += Instance.Bots.Count - replenishmentBots - pickBots;
                else
                    pickBots += Instance.Bots.Count - replenishmentBots - pickBots;
            // Order active stations by their preference for assigning bots
            activeInputStations =
                // Order active input stations by their distance to the middle of their tier (prefer stations more far away, because others are more likely to share bots)
                activeInputStations.OrderByDescending(s => s.GetDistance(s.Tier.Length / 2.0, s.Tier.Width / 2.0)).ToList();
            activeOutputStations =
                // Order active output stations by their distance to the middle of their tier (prefer stations more far away, because others are more likely to share bots)
                activeOutputStations.OrderByDescending(s => s.GetDistance(s.Tier.Length / 2.0, s.Tier.Width / 2.0)).ToList();
            // Obtain goal numbers per station
            Dictionary<Circle, int> stationBotGoals = _stations.ToDictionary(k => k, v => 0);
            for (int i = 0; replenishmentBots > 0; i = (i + 1) % activeInputStations.Count)
            {
                if (stationBotGoals.ContainsKey(activeInputStations[i]))
                    stationBotGoals[activeInputStations[i]]++;
                else
                    stationBotGoals[activeInputStations[i]] = 1;
                replenishmentBots--;
            }
            for (int i = 0; pickBots > 0; i = (i + 1) % activeOutputStations.Count)
            {
                if (stationBotGoals.ContainsKey(activeOutputStations[i]))
                    stationBotGoals[activeOutputStations[i]]++;
                else
                    stationBotGoals[activeOutputStations[i]] = 1;
                pickBots--;
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
                    // Check all bots that are assigned to a station with an overflow of bots
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
            // Prepare
            bool success = false;
            // Do task for Inputstation
            if (_botStations[bot] is InputStation)
                success = DoStoreTaskForStation(bot, _botStations[bot] as InputStation,
                    // Deactivate extended search
                    false, 0,
                    // Pod selection rules
                    _config.PodSelectionConfig);
            // Do task for Outputstation
            if (_botStations[bot] is OutputStation)
                success = DoExtractTaskForStation(bot, _botStations[bot] as OutputStation,
                    // Deactivate extended search
                    false, 0,
                    // Pod selection rules
                    _config.PodSelectionConfig);
            // If we still carry a pod at this point, get rid of it
            if (DoParkPodTask(bot))
                return;
            // Do rest task, if no other task was assigned
            if (!success)
                DoRestTask(bot, _config.RestLocationOrderType);
        }

        /// <summary>
        /// The next event when this element has to be updated.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns>The next time this element has to be updated.</returns>
        public override double GetNextEventTime(double currentTime) { return double.PositiveInfinity; }
        /// <summary>
        /// Updates the element to the specified time.
        /// </summary>
        /// <param name="lastTime">The time before the update.</param>
        /// <param name="currentTime">The time to update to.</param>
        public override void Update(double lastTime, double currentTime)
        {
            // Check for a new update of the robot allocation
            if (_lastAllocationUpdate + _config.RefreshAllocationTimeout < currentTime)
            {
                UpdateRobotAllocation(false);
                _lastAllocationUpdate = currentTime;
            }
            // Do on-the-fly work, if possible
            AssignOnTheFlyWork(_config.PodSelectionConfig);
        }
        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        public override void SignalCurrentTime(double currentTime) { /* Not necessary */ }
    }
}
