using RAWSimO.Core.Configurations;
using RAWSimO.Core.Control.Defaults.PodStorage;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.MethodManagement
{
    /// <summary>
    /// Declares a scheduled method manager that changes methods at specified time points to the ones supplied by the configuration.
    /// </summary>
    public class ScheduleMethodManager : MethodManager
    {
        /// <summary>
        /// Creates a new instance of the manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public ScheduleMethodManager(Instance instance) : base(instance)
        {
            _config = instance.ControllerConfig.MethodManagementConfig as ScheduledMethodManagementConfiguration;
            // Set first change event
            _podStorageManagerQueue = new Queue<Skvp<double, PodStorageMethodType>>(_config.ScheduledPodStorageManagers);
        }

        /// <summary>
        /// The config of this controller.
        /// </summary>
        private ScheduledMethodManagementConfiguration _config;
        /// <summary>
        /// All switches that need to be done for pod storage management.
        /// </summary>
        private Queue<Skvp<double, PodStorageMethodType>> _podStorageManagerQueue;
        /// <summary>
        /// Translates the time depending on the setting for relative mode.
        /// </summary>
        /// <param name="time">The time to translate.</param>
        /// <returns>The time in simulation time.</returns>
        private double TranslateTime(double time) { return _config.RelativeMode ? time * (Instance.SettingConfig.SimulationWarmupTime + Instance.SettingConfig.SimulationDuration) : time; }

        /// <summary>
        /// The next event when this element has to be updated.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns>The next time this element has to be updated.</returns>
        public override double GetNextEventTime(double currentTime) { return _podStorageManagerQueue.Any() ? TranslateTime(_podStorageManagerQueue.First().Key) : double.PositiveInfinity; }
        /// <summary>
        /// Updates the element to the specified time.
        /// </summary>
        /// <param name="lastTime">The time before the update.</param>
        /// <param name="currentTime">The time to update to.</param>
        public override void Update(double lastTime, double currentTime)
        {
            // Check whether a change has to happen
            if (_podStorageManagerQueue.Any() && currentTime > TranslateTime(_podStorageManagerQueue.First().Key))
            {
                // --> Handle pod storage managers
                // Get next manager
                PodStorageMethodType nextPodManagerType = _podStorageManagerQueue.Dequeue().Value;
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
                    case PodStorageMethodType.Fixed: throw new ArgumentException("Cannot switch to fixed mechanism, because the system is already running!");
                    default: throw new ArgumentException("Unknown pod storage manager: " + nextPodManagerType);
                }
                // Change it
                Instance.Controller.ExchangePodStorageManager(newPodStorageManager);
            }
        }
    }
}
