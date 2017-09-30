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
    /// A station activation controller that simply keeps all stations active over time.
    /// </summary>
    public class ConstantRatioStationManager : StationManager
    {
        /// <summary>
        /// Creates a new instance of this controller.
        /// </summary>
        /// <param name="instance">The instance this controller belongs to.</param>
        public ConstantRatioStationManager(Instance instance) : base(instance)
        {
            Instance = instance;
            _config = instance.ControllerConfig.StationActivationConfig as ConstantRatioStationActivationConfiguration;
            // Just handle activation now and do not change it anymore
            int activeStationCount = (int)((instance.OutputStations.Count + instance.InputStations.Count) * _config.ActiveRatio);
            int activeOutputStationCount = (int)(activeStationCount * _config.PickReplenishRatio);
            int activeInputStationCount = activeStationCount - activeOutputStationCount;
            List<InputStation> activeIStations = instance.InputStations.OrderBy(s => s.ActivationOrderID).Take(activeInputStationCount).ToList();
            List<InputStation> inactiveIStations = instance.InputStations.Except(activeIStations).ToList();
            List<OutputStation> activeOStations = instance.OutputStations.OrderBy(s => s.ActivationOrderID).Take(activeOutputStationCount).ToList();
            List<OutputStation> inactiveOStations = instance.OutputStations.Except(activeOStations).ToList();
            activeIStations.ForEach(s => s.Activate());
            inactiveIStations.ForEach(s => s.Deactivate());
            activeOStations.ForEach(s => s.Activate());
            inactiveOStations.ForEach(s => s.Deactivate());
        }

        /// <summary>
        /// The config of this controller.
        /// </summary>
        private ConstantRatioStationActivationConfiguration _config;

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
        public override void Update(double lastTime, double currentTime) { /* Nothing to do here - keeping all stations active anyway */ }

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
