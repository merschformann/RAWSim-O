using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Visualization.Rendering
{
    /// <summary>
    /// Distinguishes the different bot coloring modes.
    /// </summary>
    public enum BotColorMode
    {
        /// <summary>
        /// Uses the default color for the bots and state dependant coloring.
        /// </summary>
        DefaultBotDefaultState,
        /// <summary>
        /// Uses rainbow colors to better distinguish the bots as well as one color for all states other than move.
        /// </summary>
        RainbowBotSingleState,
        /// <summary>
        /// Uses rainbow colors to better disinguish the bots as well as the default state colors for all other states than move.
        /// </summary>
        RainbowBotDefaultState,
    }
}
