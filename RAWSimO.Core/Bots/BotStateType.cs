using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Bots
{
    /// <summary>
    /// Enumerates all states a bot can be in.
    /// </summary>
    public enum BotStateType
    {
        /// <summary>
        /// Indicates that the bot is picking up a pod.
        /// </summary>
        PickupPod,
        /// <summary>
        /// Indicates that the bot is setting down a pod.
        /// </summary>
        SetdownPod,
        /// <summary>
        /// Indicates that the bot is getting a bundle for its pod.
        /// </summary>
        GetItems,
        /// <summary>
        /// Indicates that the bot is putting an item for an order.
        /// </summary>
        PutItems,
        /// <summary>
        /// Indicates that the bot is idling.
        /// </summary>
        Rest,
        /// <summary>
        /// Indicates that the bot is moving.
        /// </summary>
        Move,
        /// <summary>
        /// Indicates that the bot is evading another bot.
        /// </summary>
        Evade,
        /// <summary>
        /// Indicates that the bot is using an elevator.
        /// </summary>
        UseElevator
    }
}
