using RAWSimO.Core.Interfaces;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Shared
{
    /// <summary>
    /// Contains control elements that are shared by multiple control mechanisms.
    /// </summary>
    internal class SharedControlElementsContainer : IUpdateable
    {
        /// <summary>
        /// Creates a new shared control elements container.
        /// </summary>
        /// <param name="instance">The instance this container belongs to.</param>
        public SharedControlElementsContainer(Instance instance) { _instance = instance; }

        /// <summary>
        /// The instance this container belongs to.
        /// </summary>
        private Instance _instance;

        /// <summary>
        /// Class builder for turnover based mechanisms.
        /// </summary>
        public TurnoverClassBuilder _turnoverClassBuilder;
        /// <summary>
        /// Class builder for turnover based mechanisms.
        /// </summary>
        public TurnoverClassBuilder TurnoverClassBuilder { get { if (_turnoverClassBuilder == null) _turnoverClassBuilder = new TurnoverClassBuilder(_instance); return _turnoverClassBuilder; } }
        /// <summary>
        /// Partitions storage locations by different characteristics.
        /// </summary>
        public StoragePartitioner _storagePartitioner;
        /// <summary>
        /// Partitions storage locations by different characteristics.
        /// </summary>
        public StoragePartitioner StoragePartitioner { get { if (_storagePartitioner == null) _storagePartitioner = new StoragePartitioner(_instance); return _storagePartitioner; } }
        /// <summary>
        /// Partitions storage locations by different characteristics.
        /// </summary>
        public PodUtilityManager _podUtilityManager;
        /// <summary>
        /// Partitions storage locations by different characteristics.
        /// </summary>
        public PodUtilityManager PodUtilityManager { get { if (_podUtilityManager == null) _podUtilityManager = new PodUtilityManager(_instance); return _podUtilityManager; } }

        /// <summary>
        /// The next event when this element has to be updated.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns>The next time this element has to be updated.</returns>
        public double GetNextEventTime(double currentTime)
        {
            // Return the minimum of all next events
            return MathHelpers.Min(
                _turnoverClassBuilder != null ? _turnoverClassBuilder.GetNextEventTime(currentTime) : double.PositiveInfinity
                    );
        }
        /// <summary>
        /// Updates the element to the specified time.
        /// </summary>
        /// <param name="lastTime">The time before the update.</param>
        /// <param name="currentTime">The time to update to.</param>
        public void Update(double lastTime, double currentTime)
        {
            // --> Update all elements that are available
            _turnoverClassBuilder?.Update(lastTime, currentTime);
        }
    }
}
