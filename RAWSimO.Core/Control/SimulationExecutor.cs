using RAWSimO.Core.Configurations;
using RAWSimO.Core.Items;
using RAWSimO.Core.Randomization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control
{
    /// <summary>
    /// Used to execute simulation instances.
    /// </summary>
    public class SimulationExecutor
    {
        /// <summary>
        /// Executes the given simulation.
        /// </summary>
        /// <param name="instance">The instance to execute including the configuration to use.</param>
        public static void Execute(Instance instance)
        {
            // Set basic stuff
            double warmup_time = instance.SettingConfig.SimulationWarmupTime;
            double simulation_time = instance.SettingConfig.SimulationDuration;
            // Execute
            instance.LogDefault(">>> Warming up ...");
            instance.StartExecutionTiming();
            instance.Controller.Update(warmup_time);
            instance.LogDefault(">>> Warmup finished - starting simulation ...");
            instance.StatReset();
            instance.Controller.Update(simulation_time);
            instance.StopExecutionTiming();
            instance.LogDefault(">>> Simulation finished - writing results ...");
            // Print results
            instance.WriteStatistics();
            instance.LogDefault(">>> Results written");
        }
    }
}
