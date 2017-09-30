using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Interfaces
{
    /// <summary>
    /// Listens for real world events
    /// </summary>
    public interface IBotEventListener
    {
        /// <summary>
        /// Called when [bot reached way point].
        /// </summary>
        /// <param name="waypoint">The way point.</param>
        void OnReachedWaypoint(Waypoint waypoint);

        /// <summary>
        /// Called when [bot picked up the pod].
        /// </summary>
        void OnPickedUpPod();

        /// <summary>
        /// Called when [bot set down pod].
        /// </summary>
        void OnSetDownPod();

    }
}
