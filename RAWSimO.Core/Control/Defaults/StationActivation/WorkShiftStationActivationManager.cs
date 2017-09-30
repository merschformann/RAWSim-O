using RAWSimO.Core.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.StationActivation
{
    /// <summary>
    /// A station activation controller that simply keeps all stations active over time.
    /// </summary>
    public class WorkShiftStationActivationManager : StationManager
    {
        /// <summary>
        /// Creates a new instance of this controller.
        /// </summary>
        /// <param name="instance">The instance this controller belongs to.</param>
        public WorkShiftStationActivationManager(Instance instance) : base(instance)
        {
            _config = instance.ControllerConfig.StationActivationConfig as WorkShiftStationActivationConfiguration;
            // Sanity check
            if (_config.Shifts.First().Key != 0)
                throw new ArgumentException("The first shift has to start at timepoint 0!");
            // Get a working copy of the defined shifts
            _shifts = _config.Shifts.OrderBy(s => s.Key).Select(s => new Tuple<double, bool>(s.Key, s.Value)).ToList();
            _currentShiftIndex = -1;
            PerformSwitch();
            _nextSwitch = GetNextSwitch(0);
        }

        /// <summary>
        /// The config of this controller.
        /// </summary>
        private WorkShiftStationActivationConfiguration _config;
        /// <summary>
        /// All given shifts.
        /// </summary>
        private List<Tuple<double, bool>> _shifts = null;
        /// <summary>
        /// The current shift index.
        /// </summary>
        private int _currentShiftIndex;
        /// <summary>
        /// The time of the next switch.
        /// </summary>
        private double _nextSwitch;
        /// <summary>
        /// Returns the time of the next switch.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        /// <returns>The time of the next switch.</returns>
        private double GetNextSwitch(double currentTime)
        {
            int nextIndex = (_currentShiftIndex + 1) % _shifts.Count;
            int fullLoops = (int)Math.Floor(currentTime / _config.LoopTime);
            if (nextIndex == 0)
                fullLoops++;
            return (fullLoops * _config.LoopTime) + _shifts[nextIndex].Item1;
        }
        /// <summary>
        /// Performs the switch to the current shift
        /// </summary>
        private void PerformSwitch()
        {
            _currentShiftIndex = (_currentShiftIndex + 1) % _shifts.Count;
            // See whether it's an active or inactive shift
            if (_shifts[_currentShiftIndex].Item2)
                // Activate the stations
                ActivateAll();
            else
                // Deactivate the stations
                DeactivateAll();
        }
        /// <summary>
        /// Simply activates all of the stations.
        /// </summary>
        private void ActivateAll()
        {
            Instance.OutputStations.ForEach(s => s.Activate());
            Instance.InputStations.ForEach(s => s.Activate());
        }
        /// <summary>
        /// Simply deactivates all of the stations.
        /// </summary>
        private void DeactivateAll()
        {
            Instance.OutputStations.ForEach(s => s.Deactivate());
            Instance.InputStations.ForEach(s => s.Deactivate());
        }

        #region IUpdateable Members

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
            // Switch imminent?
            if (currentTime >= _nextSwitch)
            {
                // Do the actual switch
                PerformSwitch();
                // Update switch time
                _nextSwitch = GetNextSwitch(currentTime);
            }
        }

        #endregion

        #region IOptimize Members

        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        public override void SignalCurrentTime(double currentTime) { /* Nothing to do here - the manager does not optimize anything */ }

        #endregion
    }
}
