using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RAWSimO.Core.IO
{
    /// <summary>
    /// A simplified representation of the original object used for serialization.
    /// </summary>
    [XmlRootAttribute("Waypoint")]
    public class DTOWaypoint : IDataTransferObject<Waypoint, DTOWaypoint>
    {
        /// <summary>
        /// The int value to represent a <code>null</code>.
        /// </summary>
        const int INT_NULL = -1;
        /// <summary>
        /// Creates a new instance of this object.
        /// </summary>
        public DTOWaypoint() { OutputStation = INT_NULL; InputStation = INT_NULL; Pod = INT_NULL; Elevator = INT_NULL; }

        /// <summary>
        /// The ID of this element.
        /// </summary>
        [XmlAttribute]
        public int ID;
        /// <summary>
        /// The x-position.
        /// </summary>
        [XmlAttribute]
        public double X;
        /// <summary>
        /// The y-position.
        /// </summary>
        [XmlAttribute]
        public double Y;
        /// <summary>
        /// The tier this element is located on.
        /// </summary>
        [XmlAttribute]
        public int Tier;
        /// <summary>
        /// The outgoing connections of the waypoint.
        /// </summary>
        [XmlArrayItem("Waypoint")]
        public List<int> Paths;
        /// <summary>
        /// The output-station at this waypoint (if there is one).
        /// </summary>
        [XmlAttribute]
        public int OutputStation;
        /// <summary>
        /// The input-station at this waypoint (if there is one).
        /// </summary>
        [XmlAttribute]
        public int InputStation;
        /// <summary>
        /// The elevator at this waypoint (if there is one).
        /// </summary>
        [XmlAttribute]
        public int Elevator;
        /// <summary>
        /// The pod at this waypoint (if there is one).
        /// </summary>
        [XmlAttribute]
        public int Pod;
        /// <summary>
        /// Indicates whether this waypoint is a storage location.
        /// </summary>
        [XmlAttribute]
        public bool PodStorageLocation;
        /// <summary>
        /// Indicates whether this waypoint belongs to a queue.
        /// </summary>
        [XmlAttribute]
        public bool IsQueueWaypoint;
        /// <summary>
        /// Creates a DTO representation of the original object.
        /// </summary>
        /// <param name="value">The original object.</param>
        public static implicit operator DTOWaypoint(Waypoint value)
        {
            if (value == null)
                return null;
            // Init the DTO object with the values of the given waypoint
            DTOWaypoint waypoint = new DTOWaypoint
            {
                ID = value.ID,
                Tier = value.Tier.ID,
                X = value.X,
                Y = value.Y,
                Paths = value.Paths.Select(w => w.ID).ToList(),
                OutputStation = value.OutputStation != null ? value.OutputStation.ID : INT_NULL,
                InputStation = value.InputStation != null ? value.InputStation.ID : INT_NULL,
                Elevator = value.Elevator != null ? value.Elevator.ID : INT_NULL,
                Pod = value.Pod != null ? value.Pod.ID : INT_NULL,
                PodStorageLocation = value.PodStorageLocation,
                IsQueueWaypoint = value.IsQueueWaypoint
            };
            // Return it
            return waypoint;
        }
        /// <summary>
        /// Submits the connection of this waypoint.
        /// </summary>
        /// <param name="instance">The instance to register the connections with.</param>
        /// <param name="waypoints">The connected waypoints.</param>
        public static void SetConnections(Instance instance, List<DTOWaypoint> waypoints)
        {
            foreach (var waypoint in waypoints)
            {
                foreach (var otherID in waypoint.Paths)
                {
                    instance.GetWaypointByID(waypoint.ID).AddPath(instance.GetWaypointByID(otherID));
                }
            }
        }

        #region IDataTransferObject<Waypoint,DTOWaypoint> Members

        /// <summary>
        /// Creates a clone out of the original.
        /// </summary>
        /// <param name="original">The original object.</param>
        /// <returns>The cloned object.</returns>
        public DTOWaypoint FromOrig(Waypoint original) { return original; }
        /// <summary>
        /// Submits all this object to the instance.
        /// </summary>
        /// <param name="instance">The instance to submit to.</param>
        /// <returns>The original object created at the instance.</returns>
        public Waypoint Submit(Instance instance)
        {
            Waypoint element = null;
            if (Pod != INT_NULL)
                element = instance.CreateWaypoint(ID, instance.GetTierByID(Tier), instance.GetPodByID(Pod));
            else
                if (InputStation != INT_NULL)
                    element = instance.CreateWaypoint(ID, instance.GetTierByID(Tier), instance.GetInputStationByID(InputStation), IsQueueWaypoint);
                else
                    if (OutputStation != INT_NULL)
                        element = instance.CreateWaypoint(ID, instance.GetTierByID(Tier), instance.GetOutputStationByID(OutputStation), IsQueueWaypoint);
                    else
                        if (Elevator != INT_NULL)
                            element = instance.CreateWaypoint(ID, instance.GetTierByID(Tier), instance.GetElevatorByID(Elevator), X, Y, IsQueueWaypoint);
                        else
                            element = instance.CreateWaypoint(ID, instance.GetTierByID(Tier), X, Y, PodStorageLocation, IsQueueWaypoint);
            element.ID = ID;
            return element;
        }

        #endregion
    }
}
