using RAWSimO.Core.Elements;
using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Metrics
{
    /// <summary>
    /// Exposes methods to estimate different values instead of hard-calculating them.
    /// </summary>
    public class Estimators
    {
        /// <summary>
        /// Optimistically estimates the time it will take bot to reach the specified waypoint, but penalizes targets on different tiers.
        /// </summary>
        /// <param name="w">The destination waypoint.</param>
        /// <param name="bot">The bot to consider.</param>
        /// <returns>The estimated time to reach the destination.</returns>
        public static double EstimateTravelTimeEuclid(Bot bot, Waypoint w)
        {
            double travelTime = (bot.GetDistance(w) + ((bot.Tier == w.Tier) ? 0 : bot.Instance.WrongTierPenaltyDistance)) / bot.MaxVelocity;
            return travelTime;
        }

        /// <summary>
        /// Optimistically estimates the time it will take the bot to go from start to end, but penalizes targets on different tiers.
        /// </summary>
        /// <param name="start">The starting waypoint.</param>
        /// <param name="end">The destination waypoint.</param>
        /// <param name="bot">The bot to consider.</param>
        /// <returns>The estimated time.</returns>
        public static double EstimateTravelTimeEuclid(Bot bot, Waypoint start, Waypoint end)
        {
            double travelTime = (start.GetDistance(end) + ((start.Tier == end.Tier) ? 0 : bot.Instance.WrongTierPenaltyDistance)) / bot.MaxVelocity;
            return travelTime;
        }

        /// <summary>
        /// Estimates the amount of time it will take before a new item can be delivered to the output-station.
        /// </summary>
        /// <param name="w">The waypoint of the output-station.</param>
        /// <param name="bot">The bot to consider.</param>
        /// <returns>The estimated time.</returns>
        public static double EstimateOutputStationWaitTime(Bot bot, Waypoint w)
        {
            OutputStation ws = w.OutputStation;
            // Assume the wait time is 2 * length of the bot there plus the time for all items
            double waitTime = ((ws.ItemTransferTime + 5 * 2 * bot.Radius) / bot.MaxVelocity) * w.BotCountOverall * w.BotCountOverall; // TODO this time estimate cannot hold when transferring multiple items at once
            return waitTime;
        }

        /// <summary>
        /// Estimates the amount of time it will take before a new item can be picked up from the input-station.
        /// </summary>
        /// <param name="bot">The bot to consider.</param>
        /// <param name="w">The waypoint of the input-station.</param>
        /// <returns>The estimated time.</returns>
        public static double EstimateInputStationWaitTime(Bot bot, Waypoint w)
        {
            InputStation ls = w.InputStation;
            // Assume the wait time is 2 * length of the podbot there plus the time for all bundles 
            double waitTime = ((ls.ItemBundleTransferTime + 5 * 2 * bot.Radius) / bot.MaxVelocity) * w.BotCountOverall * w.BotCountOverall; // TODO this estimate cannot hold when transferring many items
            return waitTime;
        }
    }
}
