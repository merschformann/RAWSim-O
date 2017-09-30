using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Shared
{
    /// <summary>
    /// Types for the tie-breakers of fast lane assignment decisions.
    /// </summary>
    public enum FastLaneTieBreaker
    {
        /// <summary>
        /// Breaks the tie randomly.
        /// </summary>
        Random,
        /// <summary>
        /// Selects the order with the earliest due time.
        /// </summary>
        EarliestDueTime,
        /// <summary>
        /// Selects the oldest order.
        /// </summary>
        FCFS,
    }
    /// <summary>
    /// Types of default order selection tie-breakers.
    /// </summary>
    public enum OrderSelectionTieBreaker
    {
        /// <summary>
        /// Breaks ties randomly.
        /// </summary>
        Random,
        /// <summary>
        /// Selects the order with the earliest due time.
        /// </summary>
        EarliestDueTime,
        /// <summary>
        /// Selects the oldest order.
        /// </summary>
        FCFS,
    }
}
