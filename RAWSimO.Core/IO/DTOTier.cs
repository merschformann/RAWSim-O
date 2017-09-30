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
    [XmlRootAttribute("Tier")]
    public class DTOTier : IDataTransferObject<Tier, DTOTier>
    {
        /// <summary>
        /// Creates a new instance of this object.
        /// </summary>
        public DTOTier() { }

        /// <summary>
        /// The ID of this element.
        /// </summary>
        [XmlAttribute]
        public int ID;
        /// <summary>
        /// The length of the tier.
        /// </summary>
        [XmlAttribute]
        public double Length;
        /// <summary>
        /// The width of the tier.
        /// </summary>
        [XmlAttribute]
        public double Width;
        /// <summary>
        /// The relative x-position of the tier.
        /// </summary>
        [XmlAttribute]
        public double RelativePositionX;
        /// <summary>
        /// The relative y-position of the tier.
        /// </summary>
        [XmlAttribute]
        public double RelativePositionY;
        /// <summary>
        /// The relative z-position of the tier.
        /// </summary>
        [XmlAttribute]
        public double RelativePositionZ;
        /// <summary>
        /// Creates a DTO representation of the original object.
        /// </summary>
        /// <param name="value">The original object.</param>
        public static implicit operator DTOTier(Tier value)
        {
            return value == null ? null : new DTOTier
            {
                ID = value.ID,
                Length = value.Length,
                Width = value.Width,
                RelativePositionX = value.RelativePositionX,
                RelativePositionY = value.RelativePositionY,
                RelativePositionZ = value.RelativePositionZ
            };
        }

        #region IDataTransferObject<Tier,DTOTier> Members

        /// <summary>
        /// Creates a clone out of the original.
        /// </summary>
        /// <param name="original">The original object.</param>
        /// <returns>The cloned object.</returns>
        public DTOTier FromOrig(Tier original) { return original; }
        /// <summary>
        /// Submits all this object to the instance.
        /// </summary>
        /// <param name="instance">The instance to submit to.</param>
        /// <returns>The original object created at the instance.</returns>
        public Tier Submit(Instance instance)
        {
            return instance.CreateTier(ID, Length, Width, RelativePositionX, RelativePositionY, RelativePositionZ);
        }

        #endregion
    }
}
