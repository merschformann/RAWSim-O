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
    [XmlRootAttribute("QueueSemaphore")]
    public class DTOQueueSemaphore : IDataTransferObject<QueueSemaphore, DTOQueueSemaphore>
    {
        /// <summary>
        /// The ID of this element.
        /// </summary>
        [XmlAttribute]
        public int ID;
        /// <summary>
        /// The capacity of the managed section.
        /// </summary>
        [XmlAttribute]
        public int Capacity;
        /// <summary>
        /// The guards belonging to this semaphore.
        /// </summary>
        [XmlArrayItem("Guard")]
        public List<DTOQueueGuard> Guards;
        /// <summary>
        /// Creates a DTO representation of the original object.
        /// </summary>
        /// <param name="value">The original object.</param>
        public static implicit operator DTOQueueSemaphore(QueueSemaphore value)
        {
            if (value == null)
                return null;
            // Init the DTO with the given values
            DTOQueueSemaphore semaphore = new DTOQueueSemaphore
            {
                ID = value.ID,
                Capacity = value.Capacity
            };
            // Add guards
            semaphore.Guards = new List<DTOQueueGuard>();
            foreach (var guard in value.Guards)
                semaphore.Guards.Add(guard);
            // Return it
            return semaphore;
        }

        #region IDataTransferObject<QueueSemaphore,DTOQueueSemaphore> Members

        /// <summary>
        /// Creates a clone out of the original.
        /// </summary>
        /// <param name="original">The original object.</param>
        /// <returns>The cloned object.</returns>
        public DTOQueueSemaphore FromOrig(QueueSemaphore original) { return original; }
        /// <summary>
        /// Submits all this object to the instance.
        /// </summary>
        /// <param name="instance">The instance to submit to.</param>
        /// <returns>The original object created at the instance.</returns>
        public QueueSemaphore Submit(Instance instance)
        {
            QueueSemaphore semaphore = instance.CreateSemaphore(ID, Capacity);
            foreach (var guard in Guards)
                guard.Submit(instance);
            return semaphore;
        }

        #endregion
    }
}
