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
    public class ActivateAllStationManager : StationManager
    {
        /// <summary>
        /// Creates a new instance of this controller.
        /// </summary>
        /// <param name="instance">The instance this controller belongs to.</param>
        public ActivateAllStationManager(Instance instance) : base(instance)
        {
            Instance = instance;
            // Just activate them all and we are done here
            instance.OutputStations.ForEach(s => s.Activate());
            instance.InputStations.ForEach(s => s.Activate());
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
