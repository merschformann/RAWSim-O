using RAWSimO.Core.Elements;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Bots
{
    /// <summary>
    /// Used to keep track of potential collisions that might occur due to the asynchronous update of the robot agents.
    /// </summary>
    internal class BotCrashHandler : IUpdateable
    {
        /// <summary>
        /// Creates a new handler.
        /// </summary>
        /// <param name="instance">The instance this handler belongs to.</param>
        public BotCrashHandler(Instance instance) { _instance = instance; }
        /// <summary>
        /// The instance this handler belongs to.
        /// </summary>
        private Instance _instance;
        /// <summary>
        /// All potential crash bots that need to be checked during the next update.
        /// </summary>
        private HashSet<Bot> _potentialCrashBots = new HashSet<Bot>();
        /// <summary>
        /// Adds a bot the the list of potentially crashed bots.
        /// </summary>
        /// <param name="bot">The bot that might have seen a collision.</param>
        public void AddPotentialCrashBot(Bot bot) { _potentialCrashBots.Add(bot); }

        /// <summary>
        /// The next event when this element has to be updated.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns>The next time this element has to be updated.</returns>
        public double GetNextEventTime(double currentTime) { return double.PositiveInfinity; /* This handler does not generate events by itself but reacts according to potential collisions */ }
        /// <summary>
        /// Updates the element to the specified time.
        /// </summary>
        /// <param name="lastTime">The time before the update.</param>
        /// <param name="currentTime">The time to update to.</param>
        public void Update(double lastTime, double currentTime)
        {
            if (_potentialCrashBots.Any())
            {
                // Remember whether there was a collision overall
                bool anyCollision = false;
                // Check all bots that sensed a potential collision
                foreach (var crashBot in _potentialCrashBots)
                {
                    // Log the potential collision
                    _instance.LogInfo("Investigating potential collision of " + crashBot.GetIdentfierString() + " ...");
                    // Check quad-tree for collisions (consider the pod too)
                    if (// Check for collisions with other bots
                        crashBot.Tier.BotQuadTree.IsCollision(crashBot) ||
                        // Check for collisions with the tier boundaries
                        (crashBot.X - crashBot.Radius < 0 || crashBot.X + crashBot.Radius > crashBot.Tier.Length || crashBot.Y - crashBot.Radius < 0 || crashBot.Y + crashBot.Radius > crashBot.Tier.Width) ||
                        // Check for collisions with other pods
                        (crashBot.Pod != null && crashBot.Pod.Tier.PodQuadTree.IsCollision(crashBot.Pod)))
                    {
                        // Remember that we have a collision
                        anyCollision = true;
                        // ---> Debug: Log info about the crash pilot
                        _instance.LogSevere("Bot" + crashBot.ID.ToString() + " crashed! Information:");
                        _instance.LogSevere(
                            " X: " + crashBot.X.ToString(IOConstants.FORMATTER) +
                            " Y: " + crashBot.Y.ToString(IOConstants.FORMATTER) +
                            " Radius: " + crashBot.Radius.ToString(IOConstants.FORMATTER) +
                            " Orientation: " + crashBot.Orientation.ToString(IOConstants.FORMATTER) +
                            " Bucket: " + (crashBot.Pod != null ? crashBot.Pod.ID.ToString() : " <null>"));
                        // Check multiple radii for other bots and pods
                        for (int i = 2; i < 5; i++)
                        {
                            _instance.LogSevere("Potential crash partners (bots) within " + i.ToString() + " * radius:");
                            foreach (var potentialCrashBot in _instance.Compound.BotCurrentTier[crashBot].GetBotsWithinDistance(crashBot.X, crashBot.Y, i * crashBot.Radius).Where(b => b != crashBot))
                                _instance.LogSevere(
                                    "Bot: " + potentialCrashBot.ID.ToString() +
                                    " X: " + potentialCrashBot.X.ToString(IOConstants.FORMATTER) +
                                    " Y: " + potentialCrashBot.Y.ToString(IOConstants.FORMATTER) +
                                    " Orientation: " + potentialCrashBot.Orientation.ToString(IOConstants.FORMATTER));
                            _instance.LogSevere("Potential crash partners (pods) within " + i.ToString() + " * radius:");
                            foreach (var potentialCrashPod in _instance.Compound.BotCurrentTier[crashBot].GetPodsWithinDistance(crashBot.X, crashBot.Y, i * crashBot.Radius).Where(p => p != crashBot.Pod))
                                _instance.LogSevere(
                                    "Pod: " + potentialCrashPod.ID.ToString() +
                                    " X: " + potentialCrashPod.X.ToString(IOConstants.FORMATTER) +
                                    " Y: " + potentialCrashPod.Y.ToString(IOConstants.FORMATTER) +
                                    " Orientation: " + potentialCrashPod.Orientation.ToString(IOConstants.FORMATTER));
                        }
                    }
                }
                // Handle collision, if there was one
                if (anyCollision)
                {
                    // Save instance
                    string debugInstanceFile = System.IO.Path.Combine(_instance.SettingConfig.StatisticsDirectory, "debug.xinst");
                    _instance.LogSevere("Logging snapshot of crash situation to " + debugInstanceFile + " ...");
                    InstanceIO.WriteInstance(debugInstanceFile, _instance);
                    _instance.LogSevere("We're done here!");
                    // This is not allowed anymore
                    throw new Exception("Unmanaged Operation: collisions are not expected anymore!");
                }
                else
                {
                    // There was no collision at all - we can clear the potential crash bots
                    _potentialCrashBots.Clear();
                }
            }
        }
    }
}
