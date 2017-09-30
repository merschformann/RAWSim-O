using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Info
{
    /// <summary>
    /// The interface for supplying information about a bot object.
    /// </summary>
    public interface IBotInfo : IMovableObjectInfo
    {
        /// <summary>
        /// Returns the current task the bot is executing.
        /// </summary>
        /// <returns>The active task.</returns>
        string GetInfoTask();
        /// <summary>
        /// Returns the current state the bot is in.
        /// </summary>
        /// <returns>The active state.</returns>
        string GetInfoState();
        /// <summary>
        /// Gets the x-position of the goal of the bot.
        /// </summary>
        /// <returns>The x-position.</returns>
        double GetInfoGoalX();
        /// <summary>
        /// Gets the y-position of the goal of the bot.
        /// </summary>
        /// <returns>The y-position.</returns>
        double GetInfoGoalY();
        /// <summary>
        /// Gets the current waypoint that is considered by planning.
        /// </summary>
        /// <returns>The current waypoint.</returns>
        IWaypointInfo GetInfoCurrentWaypoint();
        /// <summary>
        /// Gets the destination of the bot.
        /// </summary>
        /// <returns>The destination.</returns>
        IWaypointInfo GetInfoDestinationWaypoint();
        /// <summary>
        /// Gets the goal of the bot.
        /// </summary>
        /// <returns>The goal.</returns>
        IWaypointInfo GetInfoGoalWaypoint();
        /// <summary>
        /// Gets the current path of the bot.
        /// </summary>
        /// <returns>The current path.</returns>
        List<IWaypointInfo> GetInfoPath();
        /// <summary>
        /// Gets the current speed of the bot.
        /// </summary>
        /// <returns>The speed in m/s.</returns>
        double GetInfoSpeed();
        /// <summary>
        /// Gets the target orientation in radians. (An element facing east is defined with orientation 0 or equally 2*pi.)
        /// </summary>
        /// <returns>The orientation.</returns>
        double GetInfoTargetOrientation();
        /// <summary>
        /// Indicates whether the robot is currently blocked.
        /// </summary>
        /// <returns><code>true</code> if the robot is blocked, <code>false</code> otherwise.</returns>
        bool GetInfoBlocked();
        /// <summary>
        /// The time remaining the bot is blocked.
        /// </summary>
        /// <returns>The time remaining the bot is blocked.</returns>
        double GetInfoBlockedLeft();
        /// <summary>
        /// Indicates whether the bot is currently queueing in a managed area.
        /// </summary>
        /// <returns><code>true</code> if the robot is within a queue area, <code>false</code> otherwise.</returns>
        bool GetInfoIsQueueing();
    }
}
