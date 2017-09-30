using RAWSimO.Core.Bots;
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
    [XmlRootAttribute("Bot")]
    public class DTOBot : IDataTransferObject<Bot, DTOBot>
    {
        /// <summary>
        /// Creates a new instance of this object.
        /// </summary>
        public DTOBot() { }
        /// <summary>
        /// The ID of this element.
        /// </summary>
        [XmlAttribute]
        public int ID;
        /// <summary>
        /// The time it takes for picking up and setting down a pod.
        /// </summary>
        [XmlAttribute]
        public double PodTransferTime;
        /// <summary>
        /// The maximal acceleration of the robot in m/s^2.
        /// </summary>
        [XmlAttribute]
        public double MaxAcceleration;
        /// <summary>
        /// The maximal deceleration of the robot in m/s^2.
        /// </summary>
        [XmlAttribute]
        public double MaxDeceleration;
        /// <summary>
        /// The maximal velocity of the robot in m/s.
        /// </summary>
        [XmlAttribute]
        public double MaxVelocity;
        /// <summary>
        /// The time it takes the robot to do a full turn in s.
        /// </summary>
        [XmlAttribute]
        public double TurnSpeed;
        /// <summary>
        /// The collision penalty time in s.
        /// </summary>
        [XmlAttribute]
        public double CollisionPenaltyTime;
        /// <summary>
        /// The initial position of the robot (x-value).
        /// </summary>
        [XmlAttribute]
        public double X;
        /// <summary>
        /// The initial position of the robot (y-value).
        /// </summary>
        [XmlAttribute]
        public double Y;
        /// <summary>
        /// The radius of the robot in m.
        /// </summary>
        [XmlAttribute]
        public double Radius;
        /// <summary>
        /// The initial orientation of the robot.
        /// </summary>
        [XmlAttribute]
        public double Orientation;
        /// <summary>
        /// The initial tier the robot is located on.
        /// </summary>
        [XmlAttribute]
        public int Tier;
        /// <summary>
        /// Creates a DTO representation of the original object.
        /// </summary>
        /// <param name="value">The original object.</param>
        public static implicit operator DTOBot(Bot value)
        {
            return value == null ? null : new DTOBot
            {
                ID = value.ID,
                PodTransferTime = value.PodTransferTime,
                MaxAcceleration = value.MaxAcceleration,
                MaxDeceleration = value.MaxDeceleration,
                MaxVelocity = value.MaxVelocity,
                TurnSpeed = value.TurnSpeed,
                CollisionPenaltyTime = value.CollisionPenaltyTime,
                X = value.X,
                Y = value.Y,
                Radius = value.Radius,
                Orientation = value.Orientation,
                Tier = value.Tier.ID
            };
        }

        #region IDataTransferObject<Bot,DTOBot> Members

        /// <summary>
        /// Creates a clone out of the original.
        /// </summary>
        /// <param name="original">The original object.</param>
        /// <returns>The cloned object.</returns>
        public DTOBot FromOrig(Bot original) { return original; }
        /// <summary>
        /// Submits all this object to the instance.
        /// </summary>
        /// <param name="instance">The instance to submit to.</param>
        /// <returns>The original object created at the instance.</returns>
        public Bot Submit(Instance instance)
        {
            return instance.CreateBot(
                ID,
                instance.GetTierByID(Tier),
                X,
                Y,
                Radius,
                Orientation,
                PodTransferTime,
                MaxAcceleration,
                MaxDeceleration,
                MaxVelocity,
                TurnSpeed,
                CollisionPenaltyTime);
        }

        #endregion
    }
}
