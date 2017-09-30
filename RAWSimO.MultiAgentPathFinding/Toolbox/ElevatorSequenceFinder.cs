using RAWSimO.MultiAgentPathFinding.Algorithms.AStar;
using RAWSimO.MultiAgentPathFinding.Elements;
using RAWSimO.MultiAgentPathFinding.Physic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MultiAgentPathFinding.Toolbox
{
    /// <summary>
    /// Finds a sequence of elevators with must be visit to reach the end node.
    /// </summary>
    class ElevatorSequenceFinder
    {
        /// <summary>
        /// Finds a sequence of elevators with must be visit to reach the end node.
        /// </summary>
        /// <param name="startNode">The start node.</param>
        /// <param name="endNode">The end node.</param>
        /// <returns></returns>
        public static List<Tuple<object, int, int>> FindElevatorSequence(Graph graph, Physics agentPhysics, int startNode, int endNode, out double distance)
        {
            distance = 0;
            var search = new ElevatorAStar(graph,startNode,endNode,agentPhysics);
            var found = search.Search();

            return found ? search.getPathAsReferenceList(out distance) : null;
        }
    }
}
