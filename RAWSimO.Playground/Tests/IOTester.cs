using RAWSimO.Core;
using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Generator;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.IO;
using RAWSimO.Core.Items;
using RAWSimO.Core.Management;
using RAWSimO.Core.Randomization;
using RAWSimO.Core.Control;
using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RAWSimO.Playground.Tests
{
    public class IOTester
    {
        public static void ExecuteInstance(Instance instance)
        {
            // Deus ex machina
            SimulationExecutor.Execute(instance);
        }
    }
}
