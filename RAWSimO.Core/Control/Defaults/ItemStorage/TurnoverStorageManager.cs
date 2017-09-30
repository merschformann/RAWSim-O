using RAWSimO.Core.Configurations;
using RAWSimO.Core.Control.Shared;
using RAWSimO.Core.Elements;
using RAWSimO.Core.IO;
using RAWSimO.Core.Items;
using RAWSimO.Core.Metrics;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.ItemStorage
{
    class TurnoverStorageManager : ItemStorageManager
    {
        public TurnoverStorageManager(Instance instance)
            : base(instance)
        {
            _config = instance.ControllerConfig.ItemStorageConfig as TurnoverItemStorageConfiguration;
            // Parse config
            _classReservedCapacityPercentageForRefill = _config.BufferThresholdPerClass
                .Split(IOConstants.DELIMITER_LIST)
                .Select(e => double.Parse(e, IOConstants.FORMATTER))
                .ToArray();
            _classBufferTimeout = _config.BufferTimeoutPerClass
                .Split(IOConstants.DELIMITER_LIST)
                .Select(e => double.Parse(e, IOConstants.FORMATTER))
                .ToArray();
            // Initialize class manager
            _classManager = instance.SharedControlElements.TurnoverClassBuilder;
            _classManager.ParseConfigAndEnsureCompatibility(
                _config.ClassBorders.Split(IOConstants.DELIMITER_LIST).Select(e => double.Parse(e, IOConstants.FORMATTER)).OrderBy(v => v).ToArray(),
                _config.ReallocationDelay,
                _config.ReallocationOrderCount);
            // Add event callbacks
            instance.PodHandled += PodHandled;
        }

        private void PodHandled(Pod pod, InputStation iStation, OutputStation oStation)
        {
            // If the recycled pod was just handled at an input-station, do not assign any more bundles to it (we do not want to bring it back immediately after replenishing it)
            if (iStation != null && _lastChosenPodByClassReverse.ContainsKey(pod))
            {
                _lastChosenPodByClass.Remove(_lastChosenPodByClassReverse[pod]);
                _lastChosenPodByClassReverse.Remove(pod);
            }
        }

        /// <summary>
        /// Selects a pod for a bundle generated during initialization.
        /// </summary>
        /// <param name="instance">The active instance.</param>
        /// <param name="bundle">The bundle to assign to a pod.</param>
        /// <returns>The selected pod.</returns>
        public override Pod SelectPodForInititalInventory(Instance instance, ItemBundle bundle)
        {
            // Just choose a pod as if the system was already running
            Pod chosenPod = ChoosePod(bundle);
            if (chosenPod == null)
                throw new InvalidOperationException("Could not find a pod for the given bundle - are we at capacity?");
            else
                return chosenPod;
        }

        /// <summary>
        /// The config for this manager.
        /// </summary>
        private TurnoverItemStorageConfiguration _config;
        /// <summary>
        /// The class manager in use.
        /// </summary>
        private TurnoverClassBuilder _classManager;


        /// <summary>
        /// The last pod chosen per class.
        /// </summary>
        private Dictionary<int, Pod> _lastChosenPodByClass = new Dictionary<int, Pod>();
        /// <summary>
        /// The last pod chosen per class in reverse.
        /// </summary>
        private Dictionary<Pod, int> _lastChosenPodByClassReverse = new Dictionary<Pod, int>();
        /// <summary>
        /// Relative reserved capacity by class above which the pod gets refilled.
        /// </summary>
        private double[] _classReservedCapacityPercentageForRefill;
        /// <summary>
        /// The buffer timeout per storage class.
        /// </summary>
        private double[] _classBufferTimeout;

        /// <summary>
        /// Retrieves the threshold value above which buffered decisions for that respective pod are submitted to the system.
        /// </summary>
        /// <param name="pod">The pod to get the threshold value for.</param>
        /// <returns>The threshold value above which buffered decisions are submitted. Use 0 to immediately submit decisions.</returns>
        protected override double GetStorageBufferThreshold(Pod pod) { return _classReservedCapacityPercentageForRefill[_classManager.DetermineStorageClass(pod)]; }
        /// <summary>
        /// Retrieves the time after which buffered bundles will be allocated even if they do not meet the threshold criterion.
        /// </summary>
        /// <param name="pod">The pod to get the timeout value for.</param>
        /// <returns>The buffer timeout.</returns>
        protected override double GetStorageBufferTimeout(Pod pod) { return _classBufferTimeout[_classManager.DetermineStorageClass(pod)]; }

        /// <summary>
        /// This is called to decide about potentially pending bundles.
        /// This method is being timed for statistical purposes and is also ONLY called when <code>SituationInvestigated</code> is <code>false</code>.
        /// Hence, set the field accordingly to react on events not tracked by this outer skeleton.
        /// </summary>
        protected override void DecideAboutPendingBundles()
        {
            foreach (var bundle in _pendingBundles.ToArray())
            {
                // Choose it
                Pod chosenPod = ChoosePod(bundle);

                // If we found a pod, assign the bundle to it
                if (chosenPod != null)
                    AddToReadyList(bundle, chosenPod);
            }
        }

        /// <summary>
        /// Selects a pod for the given bundle.
        /// </summary>
        /// <param name="bundle">The bundle to select a pod for.</param>
        /// <returns>The chosen pod.</returns>
        private Pod ChoosePod(ItemBundle bundle)
        {
            // Get the storage class the item should end up in
            int desiredStorageClass = _classManager.DetermineStorageClass(bundle);

            // Try to allocate the item to its storage class - if not possible try neighboring classes
            int currentClassTriedLow = desiredStorageClass; int currentClassTriedHigh = desiredStorageClass;
            Pod chosenPod = null;
            while (true)
            {
                // Try the less frequent class first
                if (currentClassTriedLow < _classManager.ClassCount)
                    chosenPod = ChoosePodByConfig(currentClassTriedLow, bundle);
                // Check whether we found a suitable pod of this class
                if (chosenPod != null)
                    break;

                // Try the higher frequent class next
                if (currentClassTriedHigh >= 0 && currentClassTriedHigh != currentClassTriedLow)
                    chosenPod = ChoosePodByConfig(currentClassTriedHigh, bundle);
                // Check whether we found a suitable pod of this class
                if (chosenPod != null)
                    break;

                // Update the class indeces to check next
                currentClassTriedLow++; currentClassTriedHigh--;
                // Check index correctness
                if (currentClassTriedHigh < 0 && currentClassTriedLow >= _classManager.ClassCount)
                    // We tried all classes - it won't fit
                    break;
            }
            // Return it
            return chosenPod;
        }

        /// <summary>
        /// Selects a pod for the bundle according to the desired class.
        /// </summary>
        /// <param name="classId">The class of the desired pod.</param>
        /// <param name="bundle">The bundle to assign to a pod.</param>
        /// <returns>The pod for the given bundle or <code>null</code> in case it does not fit any pod.</returns>
        private Pod ChoosePodByConfig(int classId, ItemBundle bundle)
        {
            Pod chosenPod = null;
            // See whether we can recycle the last pod
            if (_lastChosenPodByClass.ContainsKey(classId) && _lastChosenPodByClass[classId].FitsForReservation(bundle))
            {
                chosenPod = _lastChosenPodByClass[classId];
            }
            else
            {
                // We can not recycle the last pod
                if (_config.EmptiestInsteadOfRandom)
                {
                    // Find the new emptiest pod
                    chosenPod = _classManager.GetClassPods(classId)
                        .Where(b => b.FitsForReservation(bundle))
                        .OrderBy(b => (b.CapacityInUse + b.CapacityReserved) / b.Capacity)
                        .FirstOrDefault();
                }
                else
                {
                    // Find a new random pod
                    chosenPod = _classManager.GetClassPods(classId)
                        .Where(b => b.FitsForReservation(bundle))
                        .OrderBy(b => b.Instance.Randomizer.NextDouble())
                        .FirstOrDefault();
                }
                // Remember the pod for next time
                if (chosenPod != null)
                {
                    _lastChosenPodByClass[classId] = chosenPod;
                    _lastChosenPodByClassReverse[chosenPod] = classId;
                }
            }
            // Return the choice
            return chosenPod;
        }

        #region IOptimize Members

        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        public override void SignalCurrentTime(double currentTime) { /* Ignore since this simple manager is always ready. */ }

        #endregion
    }
}
