using RAWSimO.Core.Elements;
using RAWSimO.Core.Info;
using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core
{
    public partial class Instance
    {
        #region Modification for visualization only

        /// <summary>
        /// Remove all robots from the instance. This is only usable for visualization purposes.
        /// </summary>
        public void VisClearBots()
        {
            Bots.Clear();
            foreach (var tier in Compound.Tiers)
                tier.Bots.Clear();
        }

        /// <summary>
        /// Remove all pods from the instance. This is only usable for visualization purposes.
        /// </summary>
        public void VisClearPods()
        {
            Pods.Clear();
            foreach (var tier in Compound.Tiers)
                tier.Pods.Clear();
        }

        /// <summary>
        /// Remove all semaphores and guards from the instance. This is only usable for visualization purposes.
        /// </summary>
        public void VisClearSemaphores()
        {
            Semaphores.Clear();
        }

        /// <summary>
        /// Remove all stations from the instance. This is only usable for visualization purposes.
        /// </summary>
        public void VisClearStations()
        {
            InputStations.Clear();
            OutputStations.Clear();
            foreach (var tier in Compound.Tiers)
            {
                tier.InputStations.Clear();
                tier.OutputStations.Clear();
            }
        }

        #endregion

        #region Waypoint system modification

        /// <summary>
        /// Removes all elements in scope from the instance.
        /// </summary>
        /// <param name="theTier">The tier to remove the elements from.</param>
        /// <param name="xMin">The min x-value of the scope.</param>
        /// <param name="xMax">The max x-value of the scope.</param>
        /// <param name="yMin">The min y-value of the scope.</param>
        /// <param name="yMax">The max y-value of the scope.</param>
        public void ModRemoveWaypoints(ITierInfo theTier, double xMin, double yMin, double xMax, double yMax)
        {
            Tier tier = theTier as Tier;
            // Remove waypoints
            List<Waypoint> remWaypoints = Waypoints.Where(w => w.Tier == tier && xMin <= w.X && w.X <= xMax && yMin <= w.Y && w.Y <= yMax).ToList();
            // Remove all of them
            foreach (var waypoint in remWaypoints)
            {
                // Remove the waypoint - the waypoint graph handles all cascading path removals
                waypoint.Tier.RemoveWaypoint(waypoint);
                // Remove all elements that were connected to the waypoint
                foreach (var guard in Semaphores.SelectMany(s => s.Guards).Where(guard => guard.From == waypoint || guard.To == waypoint).ToArray())
                {
                    guard.Semaphore.UnregisterGuard(guard);
                    if (guard.Semaphore.Guards.Count() == 0)
                        Semaphores.Remove(guard.Semaphore);
                }
                foreach (var station in InputStations.Where(s => s.Waypoint == waypoint).ToArray())
                {
                    station.Tier.RemoveInputStation(station);
                    InputStations.Remove(station);
                }
                foreach (var station in OutputStations.Where(s => s.Waypoint == waypoint).ToArray())
                {
                    station.Tier.RemoveOutputStation(station);
                    OutputStations.Remove(station);
                }
                foreach (var pod in Pods.Where(p => p.Waypoint == waypoint).ToArray())
                {
                    pod.Tier.RemovePod(pod);
                    Pods.Remove(pod);
                }
                foreach (var elevator in Elevators.Where(e => e.ConnectedPoints.Contains(waypoint)))
                {
                    elevator.UnregisterPoint(waypoint);
                    if (elevator.ConnectedPoints.Count == 0)
                        Elevators.Remove(elevator);
                }
                // Make sure it is not on the list of storage locations anymore
                if (waypoint.PodStorageLocation)
                    ResourceManager.RemovePodStorageLocation(waypoint);
                // Finally remove from the overall waypoint list
                Waypoints.Remove(waypoint);
            }
            // Also remove movables in scope
            List<Bot> remBots = Bots.Where(b => b.Tier == tier && xMin <= b.X && b.X <= xMax && yMin <= b.Y && b.Y <= yMax).ToList();
            foreach (var bot in remBots)
            {
                Bots.Remove(bot);
                bot.Tier.RemoveBot(bot);
            }
            List<Pod> remPods = Pods.Where(p => p.Tier == tier && xMin <= p.X && p.X <= xMax && yMin <= p.Y && p.Y <= yMax).ToList();
            foreach (var pod in remPods)
            {
                Pods.Remove(pod);
                pod.Tier.RemovePod(pod);
            }
        }

        #endregion
    }
}
