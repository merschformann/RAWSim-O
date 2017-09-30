using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MultiAgentPathFinding.Elements
{
    /// <summary>
    /// 2D space graph
    /// </summary>
    public class Graph
    {
        /// <summary>
        /// Number of nodes.
        /// </summary>
        public int NodeCount;

        /// <summary>
        /// x positions of nodes.
        /// </summary>
        public double[] PositionX;

        /// <summary>
        /// y positions of nodes.
        /// </summary>
        public double[] PositionY;

        /// <summary>
        /// Contains meta information objects for all nodes.
        /// </summary>
        public NodeInfo[] NodeInfo;

        /// <summary>
        /// Edges of the graph.
        /// [nodeID][edgeID]
        /// </summary>
        public Edge[][] Edges;

        /// <summary>
        /// Edges of the graph.
        /// [nodeID][edgeID]
        /// </summary>
        public Edge[][] BackwardEdges;

        /// <summary>
        /// Edges of the graph.
        /// [nodeID][edgeID]
        /// </summary>
        public Dictionary<int, ElevatorEdge[]> ElevatorEdges;

        /// <summary>
        /// Create a graph.
        /// </summary>
        /// <param name="nodeCount">sum of nodes</param>
        public Graph(int nodeCount)
        {
            NodeCount = nodeCount;
            NodeInfo = new NodeInfo[nodeCount];
            Edges = new Edge[nodeCount][];
            PositionX = new double[nodeCount];
            PositionY = new double[nodeCount];
            ElevatorEdges = new Dictionary<int, ElevatorEdge[]>();
        }

        /// <summary>
        /// Generate all backward edges.
        /// </summary>
        public void GenerateBackwardEgdes()
        {
            //create array
            BackwardEdges = new Edge[NodeCount][];

            //use a temporary data structure with dynamic memory allocation
            Dictionary<int, List<Edge>> dynBackwardEdges = new Dictionary<int, List<Edge>>();
            for (int nodeId = 0; nodeId < NodeCount; nodeId++)
                dynBackwardEdges[nodeId] = new List<Edge>();

            //fill dynamic Data Structure
            for (int nodeId = 0; nodeId < NodeCount; nodeId++)
                for (int edgeId = 0; edgeId < Edges[nodeId].Length; edgeId++)
                    dynBackwardEdges[Edges[nodeId][edgeId].To].Add(Edges[nodeId][edgeId]);

            //create Backward Edges
            for (int nodeId = 0; nodeId < NodeCount; nodeId++)
                BackwardEdges[nodeId] = dynBackwardEdges[nodeId].ToArray();

            //this helps the GC
            dynBackwardEdges.Clear();

        }

        /// <summary>
        /// Gets the intermediate nodes.
        /// </summary>
        /// <param name="node1">The node1.</param>
        /// <param name="node2">The node2.</param>
        /// <returns>intermediate nodes</returns>
        public List<int> getIntermediateNodes(int node1, int node2)
        {
            var intermediateNodes = new List<int>();
            var angle = Graph.RadToDegree(Math.Atan2(PositionY[node2] - PositionY[node1], PositionX[node2] - PositionX[node1]));

            var node = node1;

            //loop from node1 to node2
            while (node != node2)
            {
                try
                {
                    node = Edges[node].Where(e => Math.Abs(e.Angle - angle) < 10).First().To;
                }
                catch (Exception)
                {
                    return null;
                }

                if (node != node2)
                    intermediateNodes.Add(node);
            }



            return intermediateNodes;
        }

        /// <summary>
        /// Gets the node distance.
        /// </summary>
        /// <param name="node1">The node1.</param>
        /// <param name="node2">The node2.</param>
        /// <returns></returns>
        public double getDistance(int node1, int node2)
        {
            return Math.Sqrt((PositionX[node1] - PositionX[node2]) * (PositionX[node1] - PositionX[node2]) + (PositionY[node1] - PositionY[node2]) * (PositionY[node1] - PositionY[node2]));
        }

        /// <summary>
        /// Gets the squared node distance.
        /// </summary>
        /// <param name="node1">The node1.</param>
        /// <param name="node2">The node2.</param>
        /// <returns></returns>
        public double getSquaredDistance(int node1, int node2)
        {
            return (PositionX[node1] - PositionX[node2]) * (PositionX[node1] - PositionX[node2]) + (PositionY[node1] - PositionY[node2]) * (PositionY[node1] - PositionY[node2]);
        }

        /// <summary>
        /// Contains 2 times pi as a constant.
        /// </summary>
        public const double PI2 = Math.PI * 2;

        /// <summary>
        /// Convert degrees to rad.
        /// </summary>
        /// <param name="degree">degree</param>
        /// <returns>rad</returns>
        public static double DegreeToRad(int degree)
        {
            var rad = degree * PI2 / 360.0;
            rad = (rad + (PI2)) % (PI2);
            return rad;
        }

        /// <summary>
        /// Convert rad to degrees.
        /// </summary>
        /// <param name="rad">rad</param>
        /// <returns>degrees</returns>
        public static short RadToDegree(double rad)
        {
            var degree = (int)Math.Round(rad * 360 / (PI2));
            return (short)((degree + 360) % 360);
        }
    }
}
