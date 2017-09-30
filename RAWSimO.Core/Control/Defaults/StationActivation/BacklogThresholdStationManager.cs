using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.StationActivation
{
    /// <summary>
    /// A station activation controller that reactively activates / deactivates the stations depending on the backlog size.
    /// </summary>
    class BacklogThresholdStationManager : StationManager
    {
        /// <summary>
        /// Creates a new instance of this controller.
        /// </summary>
        /// <param name="instance">The instance this controller belongs to.</param>
        public BacklogThresholdStationManager(Instance instance) : base(instance)
        {
            Instance = instance;
            _config = instance.ControllerConfig.StationActivationConfig as BacklogThresholdStationActivationConfiguration;
            // Activate all of them for the start
            instance.OutputStations.ForEach(s => s.Activate());
            instance.InputStations.ForEach(s => s.Activate());
            // Prepare station lists
            _oStations = instance.OutputStations.OrderBy(s => s.ActivationOrderID).ToList();
            _iStations = instance.InputStations.OrderBy(s => s.ActivationOrderID).ToList();
        }

        /// <summary>
        /// The config of this controller.
        /// </summary>
        private BacklogThresholdStationActivationConfiguration _config;
        /// <summary>
        /// The last time an output-station got deactivated / activated.
        /// </summary>
        private double _lastOStationTrigger = double.NegativeInfinity;
        /// <summary>
        /// The last time an input-station got deactivated / activated.
        /// </summary>
        private double _lastIStationTrigger = double.NegativeInfinity;
        /// <summary>
        /// All output-stations in the order in which they shall get (de-)activated.
        /// </summary>
        private List<OutputStation> _oStations;
        /// <summary>
        /// All input-stations in the order in which they shall get (de-)activated.
        /// </summary>
        private List<InputStation> _iStations;

        #region IUpdateable Members

        /// <summary>
        /// The next event when this element has to be updated.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns>The next time this element has to be updated.</returns>
        public override double GetNextEventTime(double currentTime)
        {
            /* Not necessary to generate additional events - events of new orders / bundles and finished ones will suffice */
            return double.PositiveInfinity;
        }
        /// <summary>
        /// Updates the element to the specified time.
        /// </summary>
        /// <param name="lastTime">The time before the update.</param>
        /// <param name="currentTime">The time to update to.</param>
        public override void Update(double lastTime, double currentTime)
        {
            // See whether we need to analyze the current output-station situation
            if (currentTime - _lastOStationTrigger > _config.Timeout)
            {
                // Check whether we have to (de-)activate a station
                bool activateStation = Instance.ItemManager.BacklogOrderCount > _config.OStationActivateThreshold;
                bool deactivateStation =
                    // Is any station active at all?
                    Instance.OutputStations.Any(s => s.Active) &&
                    // Used capacity of active stations
                    Instance.OutputStations.Where(s => s.Active).Sum(s => s.CapacityInUse + s.CapacityReserved) / Instance.OutputStations.Where(s => s.Active).Sum(s => s.Capacity)
                    // Smaller than the threshold?
                    < _config.OStationDeactivateThreshold;
                // Activate a station only, if an deactivation is not desired (this case might happen, but we should not do anything in that situation)
                if (activateStation && !deactivateStation)
                {
                    _oStations.FirstOrDefault(s => !s.Active)?.Activate();
                    _lastOStationTrigger = currentTime;
                }
                // Deactivate a station only, if an activation is not desired (this case might happen, but we should not do anything in that situation)
                else if (!activateStation && deactivateStation)
                {
                    _oStations.Where(s => s.Active).LastOrDefault()?.Deactivate();
                    _lastOStationTrigger = currentTime;
                }
            }
            // See whether we need to analyze the current input-station situation
            if (currentTime - _lastIStationTrigger > _config.Timeout)
            {
                // Check whether we have to (de-)activate a station
                bool activateStation = Instance.ItemManager.BacklogBundleCount > _config.IStationActivateThreshold;
                bool deactivateStation =
                    // Is any station active at all?
                    Instance.InputStations.Any(s => s.Active) &&
                    // Used capacity of active stations
                    Instance.InputStations.Where(s => s.Active).Sum(s => s.CapacityInUse + s.CapacityReserved) / Instance.InputStations.Where(s => s.Active).Sum(s => s.Capacity)
                    // Smaller than the threshold?
                    < _config.IStationDeactivateThreshold;
                // Activate a station only, if an deactivation is not desired (this case might happen, but we should not do anything in that situation)
                if (activateStation && !deactivateStation)
                {
                    _iStations.FirstOrDefault(s => !s.Active)?.Activate();
                    _lastIStationTrigger = currentTime;
                }
                // Deactivate a station only, if an activation is not desired (this case might happen, but we should not do anything in that situation)
                else if (!activateStation && deactivateStation)
                {
                    _iStations.Where(s => s.Active).LastOrDefault()?.Deactivate();
                    _lastIStationTrigger = currentTime;
                }
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
