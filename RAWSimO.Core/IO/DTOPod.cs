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
    [XmlRootAttribute("Pod")]
    public class DTOPod : IDataTransferObject<Pod, DTOPod>
    {
        /// <summary>
        /// Creates a new instance of this object.
        /// </summary>
        public DTOPod() { }
        /// <summary>
        /// The ID of this element.
        /// </summary>
        [XmlAttribute]
        public int ID;
        /// <summary>
        /// The initial position of the pod (x-value).
        /// </summary>
        [XmlAttribute]
        public double X;
        /// <summary>
        /// The initial position of the pod (y-value).
        /// </summary>
        [XmlAttribute]
        public double Y;
        /// <summary>
        /// The radius of the pod.
        /// </summary>
        [XmlAttribute]
        public double Radius;
        /// <summary>
        /// The initial orientation of the pod.
        /// </summary>
        [XmlAttribute]
        public double Orientation;
        /// <summary>
        /// The initial tier the pod is located on.
        /// </summary>
        [XmlAttribute]
        public int Tier;
        /// <summary>
        /// The capacity of the pod.
        /// </summary>
        [XmlAttribute]
        public double Capacity;
        /// <summary>
        /// Creates a DTO representation of the original object.
        /// </summary>
        /// <param name="value">The original object.</param>
        public static implicit operator DTOPod(Pod value)
        {
            return value == null ? null : new DTOPod
            {
                ID = value.ID,
                X = value.X,
                Y = value.Y,
                Radius = value.Radius,
                Orientation = value.Orientation,
                Tier = value.Tier.ID,
                Capacity = value.Capacity
            };
        }

        #region IDataTransferObject<Pod,DTOPod> Members

        /// <summary>
        /// Creates a clone out of the original.
        /// </summary>
        /// <param name="original">The original object.</param>
        /// <returns>The cloned object.</returns>
        public DTOPod FromOrig(Pod original) { return original; }
        /// <summary>
        /// Submits all this object to the instance.
        /// </summary>
        /// <param name="instance">The instance to submit to.</param>
        /// <returns>The original object created at the instance.</returns>
        public Pod Submit(Instance instance)
        {
            return instance.CreatePod(ID, instance.GetTierByID(Tier), X, Y, Radius, Orientation, Capacity);
        }

        #endregion
    }
}
