using RAWSimO.Core.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAWSimO.Core.Elements;

namespace RAWSimO.Core.Control.Defaults.Repositioning
{
    /// <summary>
    /// A conceptual repositioning manager.
    /// </summary>
    public class ConceptRepositioningManager : RepositioningManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public ConceptRepositioningManager(Instance instance) : base(instance) { _config = instance.ControllerConfig.RepositioningConfig as ConceptRepositioningConfiguration; }
        /// <summary>
        /// The configuration.
        /// </summary>
        private ConceptRepositioningConfiguration _config;

        /// <summary>
        /// Decides the next repositioning move to do for the given robot.
        /// </summary>
        /// <param name="robot">The robot that is asking to conduct such a move.</param>
        /// <returns>A repositioning move or <code>null</code> if no such move was available.</returns>
        protected override RepositioningMove GetRepositioningMove(Bot robot)
        {
            // TODO decide about the next move to do - remove the comments and code below

            // You can use the SetTimeout function to block calls temporarily for the given robot - this will help reduce computational effort, if there is e.g.: actually nothing to decide for a while
            SetTimeout(robot, double.MaxValue);

            // You can use all of the information the instance has to come up with a decision
            // Access the instance by using the "Instance" field!
            // Example: to get an enumeration of all orders currently in the backlog call: Instance.ItemManager.AvailableOrders
            // Another example: Access all orders that are currently assigned to a station:
            //      1. Access one station by (for example) iterating it: foreach(var station in Instance.OutputStations) { ... }
            //      2. Access an enumeration of all orders assigned to the station: station.AssignedOrders

            // Return null, if there is currently no move - otherwise return a move like this: new RepositioningMove() { Pod = podToReposition, StorageLocation = newStorageLocation }
            return null;
        }

        #region IUpdateable

        /// <summary>
        /// Updates the element to the specified time.
        /// </summary>
        /// <param name="lastTime">The time before the update.</param>
        /// <param name="currentTime">The time to update to.</param>
        public override void Update(double lastTime, double currentTime)
        {
            // Call base implementation first
            base.Update(lastTime, currentTime);
            // There is currently nothing we need to do within here
            // BUT NOTE: this function is called at every event during simulation:
            // -> i.e. everytime something changes, hence, this can be used to do some quick reactive update on some outer attributes like the order backlog size
        }

        #endregion

        #region IOptimize members

        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        public override void SignalCurrentTime(double currentTime) { /* Nothing to do, because this manager is always ready */ }

        #endregion
    }
}
