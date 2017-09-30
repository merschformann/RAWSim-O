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
    [XmlRootAttribute("QueueGuard")]
    public class DTOQueueGuard : IDataTransferObject<QueueGuard, DTOQueueGuard>
    {
        /// <summary>
        /// The from part of the connection.
        /// </summary>
        [XmlAttribute]
        public int From;
        /// <summary>
        /// The to part of the connection.
        /// </summary>
        [XmlAttribute]
        public int To;
        /// <summary>
        /// Indicates whether this is an entry or exit guard.
        /// </summary>
        [XmlAttribute]
        public bool Entry;
        /// <summary>
        /// Indicates whether this guard can block the connection.
        /// </summary>
        [XmlAttribute]
        public bool Barrier;
        /// <summary>
        /// The semaphore this guard belongs to.
        /// </summary>
        [XmlAttribute]
        public int Semaphore;
        /// <summary>
        /// Creates a DTO representation of the original object.
        /// </summary>
        /// <param name="value">The original object.</param>
        public static implicit operator DTOQueueGuard(QueueGuard value)
        {
            return value == null ? null : new DTOQueueGuard
            {
                From = value.From.ID,
                To = value.To.ID,
                Entry = value.Entry,
                Barrier = value.Barrier,
                Semaphore = value.Semaphore.ID
            };
        }

        #region IDataTransferObject<WaypointConnectionGuard,DTOWaypointConnectionGuard> Members

        /// <summary>
        /// Creates a clone out of the original.
        /// </summary>
        /// <param name="original">The original object.</param>
        /// <returns>The cloned object.</returns>
        public DTOQueueGuard FromOrig(QueueGuard original) { return original; }
        /// <summary>
        /// Submits all this object to the instance.
        /// </summary>
        /// <param name="instance">The instance to submit to.</param>
        /// <returns>The original object created at the instance.</returns>
        public QueueGuard Submit(Instance instance)
        {
            Waypoint from = instance.GetWaypointByID(From);
            Waypoint to = instance.GetWaypointByID(To);
            QueueSemaphore semaphore = instance.GetSemaphoreByID(Semaphore);
            return semaphore.RegisterGuard(from, to, Entry, Barrier);
        }

        #endregion
    }
}
