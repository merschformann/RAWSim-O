using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.TaskAllocation
{
    /// <summary>
    /// Implements a manager that uses swarm intelligence based devision of labor techniques to assign jobs to the robots.
    /// </summary>
    public class SwarmBotManager : BotManager
    {
        #region Attributes and Constructor
        private SwarmTaskAllocationConfiguration _config;

        private BotToStationMatch _botToStationMatch;

        Dictionary<object, double> _multiplikatorPerStation = new Dictionary<object, double>();

        Dictionary<object, double> _stimulusPerStation = new Dictionary<object, double>();

        /// Threshold for Threshold formula
        private double _threshold;

        /// <summary>
        /// Number of Stations where are enough Robots
        /// </summary>
        public int numberOfFullStations = 0;

        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public SwarmBotManager(Instance instance)
           : this(instance, 0)
        { }
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        /// <param name="seed">The seed to use.</param>
        public SwarmBotManager(Instance instance, int seed)
            : base(instance)
        {
            // Save config and instance
            Instance = instance;
            _config = instance.ControllerConfig.TaskAllocationConfig as SwarmTaskAllocationConfiguration;

            _threshold = _config.maximumBotsPerStation / 2;

            //Init the Dictionaries
            Dictionary<object, int> tmp = new Dictionary<object, int>();
            FillDictionaryPerStation<int>(ref tmp, 0);
            _botToStationMatch = new BotToStationMatch(tmp);
            FillDictionaryPerStation<double>(ref _stimulusPerStation, 0);
            FillDictionaryPerStation<double>(ref _multiplikatorPerStation, 1);
        }
        #endregion

        #region GetNextStation

        /// <summary>
        /// Main decision routine that determines the next task the bot will do.
        /// </summary>
        /// <param name="bot">The bot to assign a task to.</param>
        protected override void GetNextTask(Bot bot)
        {
            Object station;
            //calculates a new Station by its Probabilities or uses the last one if bot has pod
            if (bot.Pod == null || !_botToStationMatch.TryGetStationFromBot(bot, out station))
            {
                station = CalculateStation(GetProbabiltyPerStation(bot));
                if (_botToStationMatch.BotHasMatchedStation(bot))
                    _botToStationMatch.RemoveMatch(bot);
                _botToStationMatch.AddMatch(bot, station);
            }

            //Do task for Inputstation
            bool success = false;
            if (station is InputStation)
            {
                success = DoStoreTaskForStation(bot, station as InputStation,
                            // Extended search options
                            _config.ExtendSearch, _config.ExtendedSearchRadius,
                            // Pod selection rules
                            _config.PodSelectionConfig);
            }
            //Do task for Outputstation
            if (station is OutputStation)
            {
                success = DoExtractTaskForStation(bot, station as OutputStation,
                            // Extended search options
                            _config.ExtendSearch, _config.ExtendedSearchRadius,
                            // Pod selection rules
                            _config.PodSelectionConfig);
            }
            //Do rest task, if no other task was assigned
            if (!success)
            {
                // If we still carry a pod at this point, get rid of it
                if (DoParkPodTask(bot))
                    return;
                // Simply rest
                DoRestTask(bot, _config.RestLocationOrderType);
            }
        }

        /// <summary>
        /// gets each Probabilty per Station for the spezific bot
        /// </summary>
        private Dictionary<Object, double> GetProbabiltyPerStation(Bot bot)
        {
            Dictionary<Object, double> probabilityPerStation = new Dictionary<object, double>();
            RefreshStimuliPerStation();
            foreach (KeyValuePair<Object, double> pair in _stimulusPerStation)
            {
                //                      s_j ^2
                //--------------------------------------------------------
                //s_j^2 + alpha * Thresh_ij ^2 + beta * Distance_ij ^2
                double probabilty = Math.Pow(pair.Value, 2) / (Math.Pow(pair.Value, 2) + _config.alpha * Math.Pow(_threshold, 2) + _config.beta * Math.Pow(GetDistance(bot, pair.Key), 2));
                probabilityPerStation.Add(pair.Key, probabilty);
            }
            //probabilty for he Rest task
            //if the number of stations that have not enough tasks to complete is higher or equal to thresholdForRestTask, a rest task is possible
            int thresholdForRestTask = Convert.ToInt32(_stimulusPerStation.Count() * _config.restPercentage);
            if (numberOfFullStations > thresholdForRestTask)
            {
                probabilityPerStation.Add(new Rest(), Math.Pow(numberOfFullStations - thresholdForRestTask, 3) / Math.Pow(_stimulusPerStation.Count - thresholdForRestTask, 3));
            }

            return probabilityPerStation;
        }

        /// <summary>
        /// chooses a Station by the probability in the dictionary
        /// </summary>
        /// <param name="probabilityPerStation">Dictionary with probabilityvalue(not in percentage) for each Station</param>
        /// <returns>the chosen station</returns>
        private object CalculateStation(Dictionary<Object, double> probabilityPerStation)
        {
            //calculate the sum of all Probabilityvalues (its not in percentage)
            double sum = 0;
            foreach (double value in probabilityPerStation.Values)
            {
                sum += value;
            }

            //create a Random value and choose a Station by its probability
            double random = Instance.Randomizer.NextDouble() * sum;
            double marker = 0;
            foreach (KeyValuePair<object, double> entry in probabilityPerStation)
            {
                marker += entry.Value;
                if (random <= marker)//then choose the station
                {
                    //increase the multiplikator of all stations and reset it from the chosen one
                    List<Object> keys = _multiplikatorPerStation.Keys.ToList();
                    foreach (Object multiplikatorKey in keys)
                    {
                        if (multiplikatorKey.Equals(entry.Key))
                        {
                            _multiplikatorPerStation[multiplikatorKey] = 1;
                        }
                        else
                        {
                            _multiplikatorPerStation[multiplikatorKey] *= _config.multiplikator;
                            if (_multiplikatorPerStation[multiplikatorKey] > _config.maxMultiplikatorValue)
                                _multiplikatorPerStation[multiplikatorKey] = _config.maxMultiplikatorValue;
                        }
                    }
                    return entry.Key;

                }
            }
            //should never be returned
            return new Rest();
        }

        /// <summary>
        /// calculates the probability for each Station by the use of Threshold Model
        /// </summary>
        /// <returns>Dictionary for the Values for each Station</returns>
        private void RefreshStimuliPerStation()
        {
            numberOfFullStations = 0;
            //Inputstation
            foreach (InputStation inStation in Instance.InputStations)
            {
                //Number of Bundles - Bots currently at this station
                double stimulus = Math.Max(Math.Min(inStation.ItemBundles.Count(), _config.maximumBotsPerStation) - _botToStationMatch.getCountBotsPerStation(inStation), 0);
                if (stimulus == 0)
                {
                    numberOfFullStations++;
                    _multiplikatorPerStation[inStation] = 1;
                }
                stimulus *= _multiplikatorPerStation[inStation];
                _stimulusPerStation[inStation] = stimulus;
            }
            //Outputstation
            foreach (OutputStation outStation in Instance.OutputStations)
            {
                double stimulus = 0;
                //Number of Bundles - Bots currently at this station
                stimulus = Math.Max(Math.Min(outStation.CapacityInUse, _config.maximumBotsPerStation) - _botToStationMatch.getCountBotsPerStation(outStation), 0);
                if (stimulus == 0)
                {
                    numberOfFullStations++;
                    _multiplikatorPerStation[outStation] = 1;
                }

                stimulus *= _multiplikatorPerStation[outStation];
                _stimulusPerStation[outStation] = stimulus;
            }


        }
        /// <summary>
        /// return the Distance between the bot and the station
        /// </summary>
        /// TODO Change the way to calculate Distance by different Tiers
        private double GetDistance(Bot bot, Object station)
        {
            double distance;
            if (station is InputStation)
            {
                InputStation inStation = station as InputStation;
                //Manhatten Distance
                distance = Math.Abs(inStation.GetInfoCenterX() - bot.GetInfoCenterX()) + Math.Abs(inStation.GetInfoCenterY() - bot.GetInfoCenterY());
                if (inStation.GetInfoCurrentTier().GetInfoZ() != bot.GetInfoCurrentTier().GetInfoZ())
                {
                    distance += bot.GetInfoCurrentTier().GetInfoWidth();
                    distance += Math.Abs(inStation.GetInfoCurrentTier().GetInfoZ() - bot.GetInfoCurrentTier().GetInfoZ());
                }

            }
            else if (station is OutputStation)
            {
                OutputStation outStation = station as OutputStation;
                //Manhatten Distance
                distance = Math.Abs(outStation.GetInfoCenterX() - bot.GetInfoCenterX()) + Math.Abs(outStation.GetInfoCenterY() - bot.GetInfoCenterY());
                if (outStation.GetInfoCurrentTier().GetInfoZ() != bot.GetInfoCurrentTier().GetInfoZ())
                {
                    distance += bot.GetInfoCurrentTier().GetInfoWidth();
                    distance += Math.Abs(outStation.GetInfoCurrentTier().GetInfoZ() - bot.GetInfoCurrentTier().GetInfoZ());
                }
            }
            else
            {
                distance = Int32.MaxValue;
            }
            return distance;
        }

        #endregion

        #region Helper Methods
        private void FillDictionaryPerStation<T>(ref Dictionary<Object, T> toFill, T fillValue)
        {
            foreach (InputStation inStation in Instance.InputStations)
            {
                toFill.Add(inStation, fillValue);
            }
            foreach (OutputStation outStation in Instance.OutputStations)
            {
                toFill.Add(outStation, fillValue);
            }
        }
        #endregion

        #region noImplementation
        /// <summary>
        /// The next event when this element has to be updated.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns>The next time this element has to be updated.</returns>
        public override double GetNextEventTime(double currentTime) { return Double.PositiveInfinity; }
        /// <summary>
        /// Updates the element to the specified time.
        /// </summary>
        /// <param name="lastTime">The time before the update.</param>
        /// <param name="currentTime">The time to update to.</param>
        public override void Update(double lastTime, double currentTime) { }
        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        public override void SignalCurrentTime(double currentTime) { }
        #endregion

    }

    /// <summary>
    /// Helper class to store Matched Bots
    /// </summary>
    internal class BotToStationMatch
    {
        private Dictionary<Bot, Object> _lastStation = new Dictionary<Bot, Object>();
        private Dictionary<Object, int> _botPerStationCount = new Dictionary<Object, int>();

        public BotToStationMatch(Dictionary<Object, int> stations)
        {
            _botPerStationCount = stations;
        }

        /// <summary>
        /// Returns the station of the given Bot
        /// </summary>
        /// <param name="bot">The Bot for which the station should be returned</param>
        /// <param name="station"></param>
        /// <returns>True if a Station was found, false if not</returns>
        internal bool TryGetStationFromBot(Bot bot, out Object station)
        {
            return _lastStation.TryGetValue(bot, out station);
        }
        /// <summary>
        /// Returns whether the Bot has a Station or not
        /// </summary>
        internal bool BotHasMatchedStation(Bot bot)
        {
            return _lastStation.ContainsKey(bot);
        }

        /// <summary>
        /// Removes a Match from a Bot to a Station
        /// </summary>
        /// <param name="bot"></param>
        internal void RemoveMatch(Bot bot)
        {
            _botPerStationCount[_lastStation[bot]]--;
            _lastStation.Remove(bot);
        }
        /// <summary>
        ///  Adds a Match from a Bot to a Station
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="station"></param>
        internal void AddMatch(Bot bot, object station)
        {
            if (!(station is Rest))
            {
                _lastStation.Add(bot, station);
                _botPerStationCount[station]++;
            }
        }
        internal int getCountBotsPerStation(Object station)
        {
            int tmp = 0;
            _botPerStationCount.TryGetValue(station, out tmp);
            return tmp;
        }
    }
    internal class Rest
    {
        public override string ToString() { return "Rest"; }
    }
}