using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Interfaces
{
    /// <summary>
    /// Defines a must implement interface for non-adhoc control mechanisms.
    /// </summary>
    public interface IOptimize
    {
        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        void SignalCurrentTime(double currentTime);
    }
}
