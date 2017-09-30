using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Info
{
    /// <summary>
    /// The interface for supplying information about a semaphore object.
    /// </summary>
    public interface ISemaphoreInfo
    {
        /// <summary>
        /// The ID of this semaphore.
        /// </summary>
        /// <returns>The ID of this semaphore.</returns>
        int GetInfoID();
        /// <summary>
        /// The number of guards associated with this semaphore.
        /// </summary>
        /// <returns>The number of guards belonging to this semaphore.</returns>
        int GetInfoGuards();
    }
}
