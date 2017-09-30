using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Info
{
    /// <summary>
    /// The interface for getting information about a guard object.
    /// </summary>
    public interface IGuardInfo : IImmovableObjectInfo
    {
        /// <summary>
        /// Indicates whether this guard is monitoring the entrance or exit of a queue.
        /// </summary>
        /// <returns><code>true</code> if the guard monitors an entrance, <code>false</code> otherwise.</returns>
        bool GetInfoIsBarrier();
        /// <summary>
        /// Indicates whether this guard serves as an entry to the protected area or an exit.
        /// </summary>
        /// <returns><code>true</code> if it is an entry, <code>false</code> otherwise.</returns>
        bool GetInfoIsEntry();
        /// <summary>
        /// Indicates whether this guard is currently blocked.
        /// </summary>
        /// <returns><code>false</code> if the guard is in block mode, <code>true</code> otherwise.</returns>
        bool GetInfoIsAccessible();
        /// <summary>
        /// Returns the current number of requests.
        /// </summary>
        /// <returns>The number of requests.</returns>
        int GetInfoRequests();
        /// <summary>
        /// Returns the maximal capacity.
        /// </summary>
        /// <returns>The maximal capacity.</returns>
        int GetInfoCapacity();
        /// <summary>
        /// Returns the start waypoint of the guarded path.
        /// </summary>
        /// <returns>The start waypoint.</returns>
        IWaypointInfo GetInfoFrom();
        /// <summary>
        /// Returns the end waypoint of the guarded path.
        /// </summary>
        /// <returns>The end waypoint.</returns>
        IWaypointInfo GetInfoTo();
        /// <summary>
        /// Returns the corresponding semaphore.
        /// </summary>
        /// <returns>The semaphore this guard belongs to.</returns>
        ISemaphoreInfo GetInfoSemaphore();
    }
}
