using RAWSimO.Core.Configurations;
using RAWSimO.Core.Control.Defaults.PodStorage;
using RAWSimO.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.MethodManagement
{
    /// <summary>
    /// Declares a random method manager that changes methods at specified time points randomly.
    /// </summary>
    public class RandomMethodManager : MethodManager
    {
        /// <summary>
        /// Creates a new instance of the manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public RandomMethodManager(Instance instance) : base(instance)
        {
            _config = instance.ControllerConfig.MethodManagementConfig as RandomMethodManagementConfiguration;
            // Set first change event
            _nextChange = _config.ChangeTimeout;
        }

        /// <summary>
        /// The config of this controller.
        /// </summary>
        private RandomMethodManagementConfiguration _config;
        /// <summary>
        /// Indicates when the next change has to be done.
        /// </summary>
        private double _nextChange;
        /// <summary>
        /// The pod storage managers handled by this meta method.
        /// </summary>
        private List<PodStorageMethodType> _podManagers = new List<PodStorageMethodType>()
        {
            PodStorageMethodType.Nearest,
            PodStorageMethodType.Random,
            PodStorageMethodType.StationBased,
            PodStorageMethodType.Turnover
        };

        /// <summary>
        /// The next event when this element has to be updated.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns>The next time this element has to be updated.</returns>
        public override double GetNextEventTime(double currentTime) { return _nextChange; }
        /// <summary>
        /// Updates the element to the specified time.
        /// </summary>
        /// <param name="lastTime">The time before the update.</param>
        /// <param name="currentTime">The time to update to.</param>
        public override void Update(double lastTime, double currentTime)
        {
            // Check whether a change has to happen
            if (currentTime > _nextChange)
            {
                // Handle pod storage managers
                if (_config.ExchangePodStorage)
                {
                    // Select next manager
                    PodStorageMethodType nextPodManagerType = _podManagers[Instance.Randomizer.NextInt(_podManagers.Count)];
                    PodStorageManager newPodStorageManager;
                    // Prepare it
                    switch (nextPodManagerType)
                    {
                        case PodStorageMethodType.Nearest:
                            {
                                Instance.ControllerConfig.PodStorageConfig = new NearestPodStorageConfiguration();
                                newPodStorageManager = new NearestPodStorageManager(Instance);
                            }
                            break;
                        case PodStorageMethodType.StationBased:
                            {
                                Instance.ControllerConfig.PodStorageConfig = new StationBasedPodStorageConfiguration();
                                newPodStorageManager = new StationBasedPodStorageManager(Instance);
                            }
                            break;
                        case PodStorageMethodType.Random:
                            {
                                Instance.ControllerConfig.PodStorageConfig = new RandomPodStorageConfiguration();
                                newPodStorageManager = new RandomPodStorageManager(Instance);
                            }
                            break;
                        case PodStorageMethodType.Turnover:
                            {
                                Instance.ControllerConfig.PodStorageConfig = new TurnoverPodStorageConfiguration();
                                newPodStorageManager = new TurnoverPodStorageManager(Instance);
                            }
                            break;
                        default: throw new ArgumentException("Unknown pod storage manager: " + nextPodManagerType);
                    }
                    // Change it
                    Instance.Controller.ExchangePodStorageManager(newPodStorageManager);
                }
                // Set next change event
                _nextChange += _config.ChangeTimeout;
            }
        }
    }
}
