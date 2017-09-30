using RAWSimO.Core.Bots;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Interfaces
{
    /// <summary>
    /// PodbotState is an interface which should be inherited to provide states for the Bot to be in. <code>Bot</code> keeps track of the fraction of time in each state, and also maintains a stateQueue.
    /// </summary>
    public interface IBotState
    {
        /// <summary>
        /// Act will be called whenever the <code>Bot</code> is not blocked or waiting for an event to finish.
        /// </summary>
        /// <param name="self">A reference to the affected bot.</param>
        /// <param name="lastTime">The last time an update happened.</param>
        /// <param name="currentTime">The current simulation time.</param>
        void Act(Bot self, double lastTime, double currentTime);

        /// <summary>
        /// Returns the current waypoint this bot wants to drive to
        /// </summary>
        Waypoint DestinationWaypoint { get; }

        /// <summary>
        /// The type of this state.
        /// </summary>
        BotStateType Type { get; }
    }
}
