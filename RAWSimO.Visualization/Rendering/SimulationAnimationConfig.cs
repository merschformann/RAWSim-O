using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Visualization.Rendering
{
    /// <summary>
    /// Stores the configuration for animating the simulation.
    /// </summary>
    public class SimulationAnimationConfig
    {
        /// <summary>
        /// The level of detail to use for drawing.
        /// </summary>
        public DetailLevel DetailLevel;
        /// <summary>
        /// Specifies whether to draw the goal marker.
        /// </summary>
        public bool DrawGoal;
        /// <summary>
        /// Specifies whether to draw the destination marker.
        /// </summary>
        public bool DrawDestination;
        /// <summary>
        /// Specifies whether to draw the path marker.
        /// </summary>
        public bool DrawPath;
    }
}
