using RAWSimO.Core.Configurations;
using RAWSimO.MultiAgentPathFinding;
using RAWSimO.MultiAgentPathFinding.Methods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.PathPlanning
{
    /// <summary>
    /// ODIDPathManager
    /// </summary>
    class ODIDPathManager : PathManager
    {

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="instance">instance</param>
        public ODIDPathManager(Instance instance)
            : base(instance)
        {

            //translate to lightweight graph
            var graph = GenerateGraph();
            var config = instance.ControllerConfig.PathPlanningConfig as ODIDPathPlanningConfiguration;

            PathFinder = new ODIDMethod(graph, instance.SettingConfig.Seed, new PathPlanningCommunicator(
                instance.LogSevere,
                instance.LogDefault,
                instance.LogInfo,
                instance.LogVerbose,
                () => { instance.StatOverallPathPlanningTimeouts++; }));
            var method = PathFinder as ODIDMethod;
            method.LengthOfAWaitStep = config.LengthOfAWaitStep;
            method.RuntimeLimitPerAgent = config.RuntimeLimitPerAgent;
            method.RunTimeLimitOverall = config.RunTimeLimitOverall;
            method.LengthOfAWindow = config.LengthOfAWindow;
            method.MaxNodeCountPerAgent = config.MaxNodeCountPerAgent;
            method.UseFinalReservations = config.UseFinalReservations;

            if (config.AutoSetParameter)
            {
                //best parameter determined my master thesis
                method.UseFinalReservations = false;
                method.RuntimeLimitPerAgent = config.Clocking / instance.Bots.Count;
                method.RunTimeLimitOverall = config.Clocking;
                method.MaxNodeCountPerAgent = 100;
                method.LengthOfAWindow = 15;
            }
        }
    }
}
