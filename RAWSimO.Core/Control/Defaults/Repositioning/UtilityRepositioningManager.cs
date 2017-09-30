using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RAWSimO.Core.Control.RepositioningManager;

namespace RAWSimO.Core.Control.Defaults.Repositioning
{
    /// <summary>
    /// Implements a repositioning manager that aims to move pods to more suitable storage locations as indicated by a pod utility manager component.
    /// </summary>
    public class UtilityRepositioningManager : RepositioningManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public UtilityRepositioningManager(Instance instance) : base(instance) { Instance = instance; _config = instance.ControllerConfig.RepositioningConfig as UtilityRepositioningConfiguration; }
        /// <summary>
        /// The configuration.
        /// </summary>
        private UtilityRepositioningConfiguration _config;
        /// <summary>
        /// Indicates whether the shared components have been initialized.
        /// </summary>
        private bool _initialized = false;
        /// <summary>
        /// Decides the next repositioning move to do for the given robot.
        /// </summary>
        /// <param name="robot">The robot that is asking to conduct such a move.</param>
        /// <returns>A repositioning move or <code>null</code> if no such move was available.</returns>
        protected override RepositioningMove GetRepositioningMove(Bot robot)
        {
            // Init shared components, if not done yet
            if (!_initialized)
            {
                // Init or ensure consistency of configurations
                Instance.SharedControlElements.PodUtilityManager.InitOrEnsureInit(_config.UtilityConfig);
                _initialized = true;
            }
            // Prepare values for faster assessment
            Instance.SharedControlElements.PodUtilityManager.PrepareAllPodRanks();
            // Search for best move
            RepositioningMove move = new RepositioningMove();
            int bestRankOffset = 0;
            foreach (var pod in Instance.ResourceManager.UnusedPods)
            {
                // Determine rank the pod should be located at
                int desiredRank = Instance.SharedControlElements.PodUtilityManager.DetermineRank(pod);
                // Determine rank offset
                int currentRank = Instance.ElementMetaInfoTracker.GetStorageLocationRank(pod.Waypoint);
                int rankOffset = Math.Abs(currentRank - desiredRank);
                // Update pod to reposition, if better
                if (rankOffset > bestRankOffset)
                {
                    bestRankOffset = rankOffset;
                    move.Pod = pod;
                }
            }
            // Get new position for pod (if there is a pod to move)
            if (move.Pod != null)
                move.StorageLocation = Instance.SharedControlElements.PodUtilityManager.GetStorageLocation(move.Pod, move.Pod.Waypoint);
            // Check for unavailable or useless move
            if (move.Pod == null || move.StorageLocation == null)
            {
                // Move does not change storage rank of the pod or no move was found at all ... penalize with a timeout
                GlobalTimeout = Instance.Controller.CurrentTime + _config.GlobalTimeout;
                return null;
            }
            // Return it
            return move;
        }
        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        public override void SignalCurrentTime(double currentTime) { /* Nothing to do, because this manager is always ready */ }
    }
}
