using RAWSimO.Core.Elements;
using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RAWSimO.Core.IO
{
    /// <summary>
    /// A simplified representation of the original object used for serialization.
    /// </summary>
    [XmlRootAttribute("Elevator")]
    public class DTOElevator : IDataTransferObject<Elevator, DTOElevator>
    {
        /// <summary>
        /// Creates a new instance of this object.
        /// </summary>
        public DTOElevator() { }
        /// <summary>
        /// The ID of this element.
        /// </summary>
        [XmlAttribute]
        public int ID;
        /// <summary>
        /// The list of waypoints that serve as an entrance to the elevator.
        /// </summary>
        [XmlArrayItem("Waypoint")]
        public List<int> Waypoints;
        /// <summary>
        /// The timings for transporting robots from one tier to another.
        /// </summary>
        [XmlArrayItem("Timing")]
        public List<string> Timings;
        /// <summary>
        /// The queues of this elevator.
        /// </summary>
        [XmlArrayItem("Queues")]
        public List<string> Queues;
        /// <summary>
        /// Creates a DTO representation of the original object.
        /// </summary>
        /// <param name="value">The original object.</param>
        public static implicit operator DTOElevator(Elevator value)
        {
            if (value == null)
                return null;

            var queues = new List<string>();
            foreach (var queueStart in value.Queues.Keys)
                queues.AddRange(value.Queues[queueStart].Select(w => queueStart.ID.ToString() + IOConstants.DELIMITER_TUPLE + w.ID.ToString()));

            // Init the DTO object with the values of the given elevator
            DTOElevator elevator = new DTOElevator
            {
                ID = value.ID,
                Waypoints = value.ConnectedPoints.Select(wp => wp.ID).ToList(),
                Timings = value.ConnectedPoints.SelectMany(wp1 => value.ConnectedPoints.Where(wp => wp != wp1).Select(wp2 =>
                {
                    return
                        wp1.ID.ToString() + IOConstants.DELIMITER_TUPLE +
                        wp2.ID.ToString() + IOConstants.DELIMITER_TUPLE +
                        value.GetTiming(wp1, wp2).ToString(IOConstants.FORMATTER);
                })).ToList(),
                Queues = queues
            };
            // Return it
            return elevator;
        }
        /// <summary>
        /// Sets all connections for this elevator.
        /// </summary>
        /// <param name="instance">The instance to register the connections with.</param>
        /// <param name="elevators">The elevators to set the connections for.</param>
        public static void SetConnections(Instance instance, List<DTOElevator> elevators)
        {
            foreach (var elevatorValue in elevators)
            {
                // Get corresponding elevator
                Elevator elevator = instance.GetElevatorByID(elevatorValue.ID);
                // Parse connections
                List<Waypoint> connectedPoints = elevatorValue.Waypoints.Select(id => instance.GetWaypointByID(id)).ToList();
                // Set connections
                elevator.RegisterPoints(0, connectedPoints);
                // Parse timings
                List<Tuple<Waypoint, Waypoint, double>> timings = elevatorValue.Timings.Select(
                    t =>
                    {
                        string[] values = t.Split(IOConstants.DELIMITER_TUPLE);
                        return new Tuple<Waypoint, Waypoint, double>(
                            instance.GetWaypointByID(int.Parse(values[0])),
                            instance.GetWaypointByID(int.Parse(values[1])),
                            double.Parse(values[2], IOConstants.FORMATTER));
                    }).ToList();
                // Set timings
                foreach (var timing in timings)
                    elevator.SetTiming(timing.Item1, timing.Item2, timing.Item3);
            }
        }
        /// <summary>
        /// Finishes the submission of this elevator.
        /// </summary>
        /// <param name="instance">The instance to submit to.</param>
        public void Flush(Instance instance)
        {
            //set queue
            var me = instance.Elevators.Find(elevator => elevator.ID == this.ID);
            me.Queues = new Dictionary<Waypoints.Waypoint, List<Waypoints.Waypoint>>();
            foreach (var kvString in Queues)
            {
                var kv = kvString.Split(IOConstants.DELIMITER_TUPLE);

                var keyWaypoint = instance.Waypoints.Find(w => w.ID == Int32.Parse(kv[0]));
                var valueWaypoint = instance.Waypoints.Find(w => w.ID == Int32.Parse(kv[1]));

                if (!me.Queues.ContainsKey(keyWaypoint))
                    me.Queues.Add(keyWaypoint, new List<Waypoints.Waypoint>());

                me.Queues[keyWaypoint].Add(valueWaypoint);

            }
        }

        #region IDataTransferObject<Elevator,DTOElevator> Members

        /// <summary>
        /// Creates a clone out of the original.
        /// </summary>
        /// <param name="original">The original object.</param>
        /// <returns>The cloned object.</returns>
        public DTOElevator FromOrig(Elevator original) { return original; }
        /// <summary>
        /// Submits all this object to the instance.
        /// </summary>
        /// <param name="instance">The instance to submit to.</param>
        /// <returns>The original object created at the instance.</returns>
        public Elevator Submit(Instance instance)
        {
            // Init the elevator
            Elevator elevator = instance.CreateElevator(ID);
            // Return it
            return elevator;
        }

        #endregion
    }
}
