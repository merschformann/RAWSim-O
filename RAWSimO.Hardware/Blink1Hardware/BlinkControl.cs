using RAWSimO.Hardware.RobotControl;
using RAWSimO.Toolbox;
using HidLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThingM.Blink1;
using ThingM.Blink1.ColorProcessor;

namespace RAWSimO.Hardware.Blink1Hardware
{
    /// <summary>
    /// Enables communication with a status LED.
    /// </summary>
    public class BlinkControl
    {
        /// <summary>
        /// Creates a new instance of the blink-control starting the first suitable device found.
        /// </summary>
        /// <param name="terminationRequested">Function indicating whether termination of all processing was requested.</param>
        /// <param name="botState">Contains information about the current state of the robot.</param>
        /// <param name="logger">The logger to output messages.</param>
        public BlinkControl(RobotState botState, Func<bool> terminationRequested, Action<string> logger)
        {
            // Populate color dictionaries
            _carryingPodColors = new Dictionary<bool, IColorProcessor>() {
                { false, new Hsb(0, 0, 0) },
                { true, new Hsb(240, 100, 100) }
            };
            _statusColors = new Dictionary<RobotStatus, IColorProcessor>() {
                { RobotStatus.Idle, new Hsb(0, 0, 0) },
                { RobotStatus.Moving, new Hsb(120, 100, 100) },
                { RobotStatus.MovingBackwards, new Hsb(120, 100, 100) },
                { RobotStatus.Pickup, new Hsb(0, 100, 100) },
                { RobotStatus.Setdown, new Hsb(50, 100, 100) },
                { RobotStatus.GetItem, new Hsb(330, 100, 100) },
                { RobotStatus.PutItem, new Hsb(282, 100, 100) },
                { RobotStatus.Rest, new Hsb(240, 100, 100) },
            };
            _combinedColors = new MultiKeyDictionary<RobotStatus, bool, IColorProcessor>();
            _combinedColors[RobotStatus.Idle, false] = new Hsb(0, 0, 0);
            _combinedColors[RobotStatus.Idle, true] = new Hsb(0, 0, 0);
            _combinedColors[RobotStatus.Moving, false] = new Hsb(120, 100, 100);
            _combinedColors[RobotStatus.Moving, true] = new Hsb(240, 100, 100);
            _combinedColors[RobotStatus.MovingBackwards, false] = new Hsb(120, 100, 100);
            _combinedColors[RobotStatus.MovingBackwards, true] = new Hsb(240, 100, 100);
            _combinedColors[RobotStatus.Pickup, false] = new Hsb(0, 100, 100);
            _combinedColors[RobotStatus.Pickup, true] = new Hsb(0, 100, 100);
            _combinedColors[RobotStatus.Setdown, false] = new Hsb(50, 100, 100);
            _combinedColors[RobotStatus.Setdown, true] = new Hsb(50, 100, 100);
            _combinedColors[RobotStatus.GetItem, false] = new Hsb(330, 100, 100);
            _combinedColors[RobotStatus.GetItem, true] = new Hsb(330, 100, 100);
            _combinedColors[RobotStatus.PutItem, false] = new Hsb(282, 100, 100);
            _combinedColors[RobotStatus.PutItem, true] = new Hsb(282, 100, 100);
            _combinedColors[RobotStatus.Rest, false] = new Hsb(240, 100, 100);
            _combinedColors[RobotStatus.Rest, true] = new Hsb(240, 100, 100);
            // Init and look for available devices
            _botState = botState;
            _terminationRequested = terminationRequested;
            _logger = logger;
            IEnumerable<HidDevice> availableBlink1s = HidDevices.Enumerate(Blink1Constant.VendorId, Blink1Constant.ProductId);
            _blink1 = new Blink1();
            if (availableBlink1s.Any())
            {
                // Initialize and start first blink(1) device found
                Log("Starting blink(1) device ...");
                _blink1.Open();
            }
            else
            {
                // Notify no blink(1) found
                Log("No blink(1) device found");
            }
            // Start the color manager
            ThreadPool.QueueUserWorkItem(new WaitCallback(Run));
        }

        /// <summary>
        /// The hardware device interface for the LED.
        /// </summary>
        private Blink1 _blink1;
        /// <summary>
        /// Contains information about the current status of the robot.
        /// </summary>
        private RobotState _botState;
        /// <summary>
        /// Function indicating whether the termination of all processing was requested.
        /// </summary>
        private Func<bool> _terminationRequested;
        /// <summary>
        /// The logger to write messages.
        /// </summary>
        private Action<string> _logger;
        /// <summary>
        /// The timeout for updating the colors.
        /// </summary>
        private int _updateTimeout = 100;
        /// <summary>
        /// The time for color fading.
        /// </summary>
        private ushort _fadeTime = 200;
        /// <summary>
        /// The timeout for flipping the colors.
        /// </summary>
        private TimeSpan _flipTimeout = TimeSpan.FromSeconds(1.5);
        /// <summary>
        /// The last time the color was flipped.
        /// </summary>
        private DateTime _lastColorFlip = DateTime.MinValue;
        /// <summary>
        /// Indicates whether to use flip-mode or combined color mode.
        /// </summary>
        private bool _flipMode = false;
        /// <summary>
        /// Indicates a mode change.
        /// </summary>
        private bool _modeChange = false;
        /// <summary>
        /// Indicates whether to use flip-mode or combined color mode.
        /// </summary>
        public bool FlipMode { get { return _flipMode; } set { _flipMode = value; _modeChange = true; } }
        /// <summary>
        /// Indicates whether the colors are currently flipped.
        /// </summary>
        private bool _flipped = false;
        /// <summary>
        /// Indicates the state of the robot last time we checked.
        /// </summary>
        private RobotStatus _lastState;
        /// <summary>
        /// Indicates whether the robot was carrying last time we checked.
        /// </summary>
        private bool _lastCarryingPod;
        /// <summary>
        /// Contains the colors for the respective status.
        /// </summary>
        private Dictionary<RobotStatus, IColorProcessor> _statusColors;
        /// <summary>
        /// Contains the colors for the respective status.
        /// </summary>
        private Dictionary<bool, IColorProcessor> _carryingPodColors;
        /// <summary>
        /// Contains the colors for the respective status.
        /// </summary>
        private MultiKeyDictionary<RobotStatus, bool, IColorProcessor> _combinedColors;
        /// <summary>
        /// Used to log messages.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void Log(string message) { if (_logger != null) _logger(message); }

        /// <summary>
        /// Disconnects the device.
        /// </summary>
        public void Disconnect()
        {
            // Close the connection if there is any
            if (_blink1.IsConnected)
            {
                _blink1.Close();
            }
        }

        private void Run(object state)
        {
            while (!_terminationRequested())
            {
                // Only act if there is a connected LED
                if (_blink1.IsConnected)
                {
                    // Get current state
                    RobotStatus status = _botState.Status;
                    bool carryingPod = _botState.CarryingPod;

                    // Adapt colors to new situation
                    if (_flipMode)
                    {
                        // Flip the colors if it is time
                        bool flip = false;
                        if (DateTime.Now - _lastColorFlip > _flipTimeout)
                        {
                            // Remember flip
                            flip = true;
                            _flipped = !_flipped;
                            _lastColorFlip = DateTime.Now;
                        }

                        // Update the colors, if necessary
                        if (_modeChange || flip || _lastCarryingPod != carryingPod || _lastState != status)
                        {
                            // See whether the mode is flipped currently
                            if (_flipped)
                            {
                                _blink1.FadeToColor(_fadeTime, _statusColors[status], false, 2);
                                _blink1.FadeToColor(_fadeTime, _carryingPodColors[carryingPod], false, 1);
                            }
                            else
                            {
                                _blink1.FadeToColor(_fadeTime, _statusColors[status], false, 1);
                                _blink1.FadeToColor(_fadeTime, _carryingPodColors[carryingPod], false, 2);
                            }
                            // Wait for fade to finish
                            Thread.Sleep(_fadeTime);
                        }
                    }
                    else
                    {
                        // If something changed fade to the new color
                        if (_modeChange || _lastCarryingPod != carryingPod || _lastState != status)
                            _blink1.FadeToColor(_fadeTime, _combinedColors[status, carryingPod], true);
                    }

                    // Remember current robot state
                    _lastCarryingPod = carryingPod;
                    _lastState = status;
                    _modeChange = false;
                }

                // Sleep until next time
                Thread.Sleep(_updateTimeout);
            }
        }
    }
}
