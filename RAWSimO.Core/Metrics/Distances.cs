using RAWSimO.Core.Geometrics;
using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Metrics
{
    /// <summary>
    /// This class implements some basic distance measures.
    /// </summary>
    public class Distances
    {
        /// <summary>
        /// Calculates the shortest path from the first to the second waypoint.
        /// </summary>
        /// <param name="from">The waypoint at which the path starts.</param>
        /// <param name="to">The waypoint at which the path ends.</param>
        /// <param name="instance">The instance containing the graph both waypoints belong to.</param>
        /// <returns>The distance of the shortest path. If both waypoints are not located on the same tier, penalty costs will be added. If there is no connection, an infinite length is returned.</returns>
        public static double CalculateShortestPath(Waypoint from, Waypoint to, Instance instance)
        {
            return instance.MetaInfoManager.ShortestPathManager.GetShortestPath(from, to, instance, false);
        }
        /// <summary>
        /// Calculates the shortest path from the first to the second waypoint avoiding pod storage locations.
        /// </summary>
        /// <param name="from">The waypoint at which the path starts.</param>
        /// <param name="to">The waypoint at which the path ends.</param>
        /// <param name="instance">The instance containing the graph both waypoints belong to.</param>
        /// <returns>The distance of the shortest path. If both waypoints are not located on the same tier, penalty costs will be added. If there is no connection, an infinite length is returned.</returns>
        public static double CalculateShortestPathPodSafe(Waypoint from, Waypoint to, Instance instance)
        {
            return instance.MetaInfoManager.ShortestPathManager.GetShortestPath(from, to, instance, true);
        }
        /// <summary>
        /// Calculates the shortest path from the first to the second waypoint.
        /// </summary>
        /// <param name="from">The waypoint at which the path starts.</param>
        /// <param name="to">The waypoint at which the path ends.</param>
        /// <param name="instance">The instance containing the graph both waypoints belong to.</param>
        /// <returns>The distance of the shortest path. If both waypoints are not located on the same tier, penalty costs will be added. If there is no connection, an infinite length is returned.</returns>
        public static double CalculateShortestTimePath(Waypoint from, Waypoint to, Instance instance)
        {
            return instance.MetaInfoManager.TimeEfficientPathManager.GetShortestPath(from, to, instance, false);
        }
        /// <summary>
        /// Calculates the shortest path from the first to the second waypoint avoiding pod storage locations.
        /// </summary>
        /// <param name="from">The waypoint at which the path starts.</param>
        /// <param name="to">The waypoint at which the path ends.</param>
        /// <param name="instance">The instance containing the graph both waypoints belong to.</param>
        /// <returns>The distance of the shortest path. If both waypoints are not located on the same tier, penalty costs will be added. If there is no connection, an infinite length is returned.</returns>
        public static double CalculateShortestTimePathPodSafe(Waypoint from, Waypoint to, Instance instance)
        {
            return instance.MetaInfoManager.TimeEfficientPathManager.GetShortestPath(from, to, instance, true);
        }
        /// <summary>
        /// Estimates the time for traveling using the euclidean metric.
        /// </summary>
        /// <param name="from">The from part of the trip.</param>
        /// <param name="to">The to part of the trip.</param>
        /// <param name="instance">The instance.</param>
        /// <returns>The time needed to for conducting the trip according to the euclidean metric.</returns>
        public static double EstimateEuclidTime(Circle from, Circle to, Instance instance)
        {
            return instance.MetaInfoManager.TimeEfficientPathManager.EstimateShortestPathEuclid(from, to, instance);
        }
        /// <summary>
        /// Estimates the time for traveling using the manhattan metric.
        /// </summary>
        /// <param name="from">The from part of the trip.</param>
        /// <param name="to">The to part of the trip.</param>
        /// <param name="instance">The instance.</param>
        /// <returns>The time needed to for conducting the trip according to the manhattan metric.</returns>
        public static double EstimateManhattanTime(Circle from, Circle to, Instance instance)
        {
            return instance.MetaInfoManager.TimeEfficientPathManager.EstimateShortestPathManhattan(from, to, instance);
        }
        /// <summary>
        /// Calculates the euclidean distance between the two circles.
        /// </summary>
        /// <param name="c1">First circle.</param>
        /// <param name="c2">Second circle.</param>
        /// <param name="wrongTierPenalty">The penalty for not being on the same tier.</param>
        /// <returns>The euclidean distance between the two.</returns>
        public static double CalculateEuclid(Circle c1, Circle c2, double wrongTierPenalty)
        {
            double distance = c1.GetDistance(c2);
            return c1.Tier == c2.Tier ? distance : distance + wrongTierPenalty;
        }
        /// <summary>
        /// Calculates the manhattan distance between the two circles.
        /// </summary>
        /// <param name="c1">First circle.</param>
        /// <param name="c2">Second circle.</param>
        /// <param name="wrongTierPenalty">The penalty for not being on the same tier.</param>
        /// <returns>The manhattan distance between the two.</returns>
        public static double CalculateManhattan(Circle c1, Circle c2, double wrongTierPenalty)
        {
            double distance = Math.Abs(c1.X - c2.X) + Math.Abs(c1.Y - c2.Y);
            return c1.Tier == c2.Tier ? distance : distance + wrongTierPenalty;
        }
        /// <summary>
        /// Simply calculates the euclidean distance for the given two points.
        /// </summary>
        /// <param name="x1">The x-value of the first point.</param>
        /// <param name="y1">The y-value of the first point.</param>
        /// <param name="x2">The x-value of the second point.</param>
        /// <param name="y2">The y-value of the second point.</param>
        /// <returns>The euclidean distance.</returns>
        public static double CalculateEuclid(double x1, double y1, double x2, double y2) { return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2)); }
        /// <summary>
        /// Simply calculates the manhattan distance for the given two points.
        /// </summary>
        /// <param name="x1">The x-value of the first point.</param>
        /// <param name="y1">The y-value of the first point.</param>
        /// <param name="x2">The x-value of the second point.</param>
        /// <param name="y2">The y-value of the second point.</param>
        /// <returns>The manhattan distance.</returns>
        public static double CalculateManhattan(double x1, double y1, double x2, double y2) { return Math.Abs(x1 - x2) + Math.Abs(y1 - y2); }
    }
}
