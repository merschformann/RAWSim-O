using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Interfaces
{
    /// <summary>
    /// Functionality to implement for an element that tracks statistics during a simulation.
    /// </summary>
    public interface IStatTracker
    {
        /// <summary>
        /// The callback indicates a reset of the statistics.
        /// </summary>
        void StatReset();
        /// <summary>
        /// The callback that indicates that the simulation is finished and statistics have to submitted to the instance.
        /// </summary>
        void StatFinish();
    }
}
