using RAWSimO.Core.Elements;
using RAWSimO.Core.Geometrics;
using RAWSimO.Core.Waypoints;
using RAWSimO.MultiAgentPathFinding.Physic;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Statistics
{
    /// <summary>
    /// Exposes methods to calculate upper bounds on throughput or similar.
    /// </summary>
    public class UpperBoundHelper
    {
        /// <summary>
        /// Determines the upper bound for the item throughput rate for the given instance and a reference item pile-on.
        /// The item pile-on is neglected when the total handling is longer than the time it takes to switch the robot in front of the station.
        /// </summary>
        /// <param name="instance">The instance to determine the upper bound for.</param>
        /// <param name="itemPileOn">The reference item pile-on.</param>
        /// <returns>The upper bound on the item throughput rate.</returns>
        public static double CalcUBItemThroughputRate(Instance instance, double itemPileOn)
        {
            // Determine robot characteristics (worst case / slowest robots)
            double acc = instance.Bots.Min(b => b.MaxAcceleration);
            double dec = instance.Bots.Min(b => b.MaxDeceleration);
            double speed = instance.Bots.Min(b => b.MaxVelocity);
            double turnSpeed = instance.Bots.Min(b => b.TurnSpeed);
            Physics physicsObject = new Physics(acc, dec, speed, turnSpeed);
            // Define function for obtaining the UB of one specific station
            Func<OutputStation, double> stationUB = (OutputStation station) =>
            {
                //double inboundTime = instance.Waypoints
                //    .Where(wp => wp.ContainsPath(station.Waypoint))
                //    .Max(wp => Math.Sqrt(wp.GetDistance(station.Waypoint) / (acc / 2.0 + Math.Pow(acc, 2) / 2.0 * dec)) + Math.Sqrt(wp.GetDistance(station.Waypoint) / (dec / 2.0 + Math.Pow(dec, 2) / 2.0 * acc)));
                // Expect inbound WP with shortest distance to be entry point of the station
                KeyValuePair<Waypoint, double> inboundWPTuple = instance.Waypoints.Where(wp => wp.ContainsPath(station.Waypoint)).ArgValueMin(wp => wp[station.Waypoint]);
                // Calculate drive time for a robot standing at that position
                double inboundDriveTime = physicsObject.getTimeNeededToMove(0, inboundWPTuple.Value);
                // Determine outbound distance (see whether decelerating is necessary at all)
                KeyValuePair<Waypoint, double> outboundDistanceMaxWPTuple = station.Waypoint.Paths.ArgValueMax(wp =>
                {
                    // Determine longest straight outbound path using this exit waypoint of the station
                    Waypoint currentWP = station.Waypoint;
                    Waypoint nextWP = wp;
                    double distance = 0;
                    while (nextWP != null)
                    {
                        // Accumulate distance for outbound path
                        distance += currentWP[nextWP];
                        // Select next "most straight" waypoint for the outbound path
                        nextWP = nextWP.Paths
                            .Where(w => Math.Abs(Circle.GetOrientationDifference(
                                Circle.GetOrientation(currentWP.X, currentWP.Y, nextWP.X, nextWP.Y),
                                Circle.GetOrientation(nextWP.X, nextWP.Y, w.X, w.Y))) <= instance.StraightOrientationTolerance)
                            .OrderBy(w =>
                                Math.Abs(Circle.GetOrientationDifference(
                                Circle.GetOrientation(currentWP.X, currentWP.Y, nextWP.X, nextWP.Y),
                                Circle.GetOrientation(nextWP.X, nextWP.Y, w.X, w.Y))))
                            .FirstOrDefault();
                    }
                    // Return the maximal straight outbound distance
                    return distance;
                });
                double actualOutboundDistance = station.Waypoint[outboundDistanceMaxWPTuple.Key];
                // Calculate drive time for a robot leaving the station (consider whether full acceleration is possible)
                double outboundDriveTime;
                if (outboundDistanceMaxWPTuple.Value > physicsObject.getDistanceToFullSpeed(0) + physicsObject.getDistanceToStop(physicsObject.MaxSpeed))
                {
                    outboundDriveTime =
                        // See whether there is enough room to do a full acceleration
                        physicsObject.getDistanceToFullSpeed(0) > actualOutboundDistance ?
                        // Not enough room - only accelerate as long as possible
                        Math.Sqrt((2.0 * actualOutboundDistance) / physicsObject.Acceleration) :
                        // Enough room - accelerate first and then cruise the rest
                        physicsObject.getTimeNeededFromZeroToFullSpeed() + ((actualOutboundDistance - physicsObject.getDistanceToFullSpeed(0)) / physicsObject.MaxSpeed);
                }
                else if (outboundDistanceMaxWPTuple.Value == actualOutboundDistance)
                {
                    // We need to stop at the next waypoint
                    outboundDriveTime = physicsObject.getTimeNeededToMove(0, actualOutboundDistance);
                }
                else
                {
                    // We already need to start to stop (this case is ignored for now)
                    throw new NotImplementedException("This case is not considered for now.");
                }
                // Determine time needed for turning
                double turnAwayTime = physicsObject.getTimeNeededToTurn(
                    Circle.GetOrientation(inboundWPTuple.Key.X, inboundWPTuple.Key.Y, station.Waypoint.X, station.Waypoint.Y),
                    Circle.GetOrientation(station.Waypoint.X, station.Waypoint.Y, outboundDistanceMaxWPTuple.Key.X, outboundDistanceMaxWPTuple.Key.Y));
                // Determine pick time
                double pickTime = station.ItemPickTime;
                // Determine handling time
                double handlingTime = station.ItemTransferTime;
                // Deetermine upper bound
                double upperBound =
                    itemPileOn *
                    (3600.0 / (Math.Max(inboundDriveTime + turnAwayTime + outboundDriveTime + pickTime - handlingTime, 0) + itemPileOn * handlingTime));
                // Return upper bound for this station
                return upperBound;
            };
            // Determine overall upper bound
            List<double> ubOfStations = instance.OutputStations.Select(s => stationUB(s)).ToList();
            // Return overall upper bound
            return ubOfStations.Sum();
        }
    }
}
