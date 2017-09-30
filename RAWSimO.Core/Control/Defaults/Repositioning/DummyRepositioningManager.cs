using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Waypoints;

namespace RAWSimO.Core.Control.Defaults.Repositioning
{
    /// <summary>
    /// A repositioning manager that does not generate any repositioning moves.
    /// </summary>
    public class DummyRepositioningManager : RepositioningManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public DummyRepositioningManager(Instance instance) : base(instance) { Instance = instance; }
        /// <summary>
        /// Decides the next repositioning move to do for the given robot.
        /// </summary>
        /// <param name="robot">The robot that is asking to conduct such a move.</param>
        /// <returns>A repositioning move or <code>null</code> if no such move was available.</returns>
        protected override RepositioningMove GetRepositioningMove(Bot robot)
        {
            // Set indefinite timeout for the bot such that we are never asked again for a move for this bot
            SetTimeout(robot, double.MaxValue);
            // Return no move
            return null;
        }
        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        public override void SignalCurrentTime(double currentTime) { /* Nothing to do, because this manager does nothing :) */ }
    }
}
