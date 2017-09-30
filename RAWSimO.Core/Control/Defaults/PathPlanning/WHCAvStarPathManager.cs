using RAWSimO.Core.Bots;
using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Helper;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.Waypoints;
using RAWSimO.MultiAgentPathFinding;
using RAWSimO.MultiAgentPathFinding.Elements;
using RAWSimO.MultiAgentPathFinding.Methods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.PathPlanning
{

    /// <summary>
    /// Controller of the bot.
    /// </summary>
    public class WHCAvStarPathManager : PathManager
    {

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="instance">instance</param>
        public WHCAvStarPathManager(Instance instance)
            : base(instance)
        {
            //Need a Request on Fail
            BotNormal.RequestReoptimizationAfterFailingOfNextWaypointReservation = true;

            //translate to lightweight graph
            var graph = GenerateGraph();
            var config = instance.ControllerConfig.PathPlanningConfig as WHCAvStarPathPlanningConfiguration;

            PathFinder = new WHCAvStarMethod(graph, instance.SettingConfig.Seed, new PathPlanningCommunicator(
                instance.LogSevere,
                instance.LogDefault,
                instance.LogInfo,
                instance.LogVerbose,
                () => { instance.StatOverallPathPlanningTimeouts++; }));
            var method = PathFinder as WHCAvStarMethod;
            method.LengthOfAWaitStep = config.LengthOfAWaitStep;
            method.RuntimeLimitPerAgent = config.RuntimeLimitPerAgent;
            method.RunTimeLimitOverall = config.RunTimeLimitOverall;
            method.LengthOfAWindow = config.LengthOfAWindow;
            method.AbortAtFirstConflict = config.AbortAtFirstConflict;
            method.UseDeadlockHandler = config.UseDeadlockHandler;

            if (config.AutoSetParameter)
            {
                //best parameter determined my master thesis
                method.LengthOfAWindow = 20;
                method.RuntimeLimitPerAgent = config.Clocking / instance.Bots.Count;
                method.RunTimeLimitOverall = config.Clocking;
            }
        }
    }
}
