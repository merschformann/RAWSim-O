using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Info
{
    /// <summary>
    /// The base interface for getting information about an general object.
    /// </summary>
    public interface IGeneralObjectInfo : IIdentifiableObjectInfo
    {
        /// <summary>
        /// Gets the x-position of the center of the object.
        /// </summary>
        /// <returns>The x-position.</returns>
        double GetInfoCenterX();
        /// <summary>
        /// Gets the y-position of the center of the object.
        /// </summary>
        /// <returns>The y-position.</returns>
        double GetInfoCenterY();
        /// <summary>
        /// Gets the x-position of the bottom left corner of the objects' area.
        /// </summary>
        /// <returns>The x-position.</returns>
        double GetInfoTLX();
        /// <summary>
        /// Gets the y-position of the bottom left corner of the objects' area.
        /// </summary>
        /// <returns>The y-position.</returns>
        double GetInfoTLY();
        /// <summary>
        /// Gets the current tier this object is placed on. Can't change in case of an immovable object.
        /// </summary>
        /// <returns>The current tier.</returns>
        ITierInfo GetInfoCurrentTier();
        /// <summary>
        /// Returns the active instance belonging to this element.
        /// </summary>
        /// <returns>The active instance.</returns>
        IInstanceInfo GetInfoInstance();
    }
}
