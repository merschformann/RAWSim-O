using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MultiAgentPathFinding.Elements
{

    /// <summary>
    /// An edge of a graph
    /// </summary>
    public class Edge
    {
        /// <summary>
        /// from node
        /// </summary>
        public int From;

        /// <summary>
        /// to node
        /// </summary>
        public int To;

        /// <summary>
        /// distance from fromNode to toNode in meter
        /// </summary>
        public double Distance;

        /// <summary>
        /// Angle of the edge 0°-360°
        /// 0° = East and is increasing clockwise
        /// </summary>
        public short Angle;

        /// <summary>
        /// Contains meta information about the from part of the edge.
        /// </summary>
        public NodeInfo FromNodeInfo;
        /// <summary>
        /// Contains meta information about the to part of the edge.
        /// </summary>
        public NodeInfo ToNodeInfo;
    }

    /// <summary>
    /// An elevator edge of a graph
    /// </summary>
    public class ElevatorEdge : Edge
    {
        /// <summary>
        /// distance from fromNode to toNode in meter
        /// </summary>
        public double TimeTravel;

        /// <summary>
        /// The reference to the elevator
        /// </summary>
        public object Reference;
    }

}
