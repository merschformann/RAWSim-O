using RAWSimO.Core.Elements;
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
    [XmlRootAttribute("OutputStation")]
    public class DTOOutputStation : IDataTransferObject<OutputStation, DTOOutputStation>
    {
        /// <summary>
        /// Creates a new instance of this object.
        /// </summary>
        public DTOOutputStation() { }
        /// <summary>
        /// The ID of this element.
        /// </summary>
        [XmlAttribute]
        public int ID;
        /// <summary>
        /// The initial position of the station (x-value).
        /// </summary>
        [XmlAttribute]
        public double X;
        /// <summary>
        /// The initial position of the station (y-value).
        /// </summary>
        [XmlAttribute]
        public double Y;
        /// <summary>
        /// The radius of this station.
        /// </summary>
        [XmlAttribute]
        public double Radius;
        /// <summary>
        /// The tier this station is located on.
        /// </summary>
        [XmlAttribute]
        public int Tier;
        /// <summary>
        /// The capacity of this station.
        /// </summary>
        [XmlAttribute]
        public int Capacity;
        /// <summary>
        /// The time it takes to pick one item at the station.
        /// </summary>
        [XmlAttribute]
        public double ItemTransferTime;
        /// <summary>
        /// The time it takes to pick one item from a pod (excluding other handling times).
        /// </summary>
        [XmlAttribute]
        public double ItemPickTime;
        /// <summary>
        /// The order ID of this station that defines the sequence in which the stations have to be activated.
        /// </summary>
        [XmlAttribute]
        public int ActivationOrderID;
        /// <summary>
        /// The queues of this station.
        /// </summary>
        [XmlArrayItem("Queues")]
        public List<string> Queues;
        /// <summary>
        /// Creates a DTO representation of the original object.
        /// </summary>
        /// <param name="value">The original object.</param>
        public static implicit operator DTOOutputStation(OutputStation value)
        {
            var queues = new List<string>();
            foreach (var queueStart in value.Queues.Keys)
                queues.AddRange(value.Queues[queueStart].Select(w => queueStart.ID.ToString() + IOConstants.DELIMITER_TUPLE + w.ID.ToString()));

            return value == null ? null : new DTOOutputStation
            {
                ID = value.ID,
                X = value.X,
                Y = value.Y,
                Radius = value.Radius,
                Tier = value.Tier.ID,
                Capacity = value.Capacity,
                ItemTransferTime = value.ItemTransferTime,
                ItemPickTime = value.ItemPickTime,
                ActivationOrderID = value.ActivationOrderID,
                Queues = queues
            };
        }
        /// <summary>
        /// Finishes the submission of this element.
        /// </summary>
        /// <param name="instance">The instance to submit to.</param>
        public void Flush(Instance instance)
        {
            //set queue
            var me = instance.OutputStations.Find(iStation => iStation.ID == this.ID);
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

        #region IDataTransferObject<OutputStation,DTOOutputStation> Members

        /// <summary>
        /// Creates a clone out of the original.
        /// </summary>
        /// <param name="original">The original object.</param>
        /// <returns>The cloned object.</returns>
        public DTOOutputStation FromOrig(OutputStation original) { return original; }
        /// <summary>
        /// Submits all this object to the instance.
        /// </summary>
        /// <param name="instance">The instance to submit to.</param>
        /// <returns>The original object created at the instance.</returns>
        public OutputStation Submit(Instance instance)
        {
            if (ItemPickTime == 0)
            {
                instance.LogSevere("Warning! Station does have a pick time of 0 - using handline time instead!");
                ItemPickTime = ItemTransferTime;
            }
            return instance.CreateOutputStation(ID, instance.GetTierByID(Tier), X, Y, Radius, Capacity, ItemTransferTime, ItemPickTime, ActivationOrderID);
        }

        #endregion
    }
}
