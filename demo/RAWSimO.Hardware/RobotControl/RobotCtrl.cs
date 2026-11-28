using RAWSimO.CommFramework;
using RAWSimO.Hardware.Blink1Hardware;
using RAWSimO.Hardware.RFID;
using RAWSimO.Hardware.RobotHardware;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RAWSimO.Hardware.RobotControl
{
    public class RobotCtrl
    {
        public RobotCtrl(
            iRobot robot,
            Func<bool> robotActive,
            Func<bool> terminationRequested,
            Action<string> logger,
            Action statusChangedCallback,
            Action podChangedCallback,
            Action<string> newTagFoundCallback,
            CommunicationClient client,
            string debugSnapshotDirectory,
            Action<Hsv> setFilter)
        {
            State = new RobotState(this);
            _client = client;
            _robot = robot;
            _robotActive = robotActive;
            _terminationRequested = terminationRequested;
            _logger = logger;
            _setFilter = setFilter;
            LineFollower = new LineFollower(this);
            PIDControl = new PidCtrl();
            State.StatusChangedCallback = statusChangedCallback;
            State.PodChangedCallback = podChangedCallback;
            _newTagFoundCallback = newTagFoundCallback;
            _debugSnapshotDirectory = debugSnapshotDirectory;
            if (Directory.Exists(_debugSnapshotDirectory))
                Directory.Delete(_debugSnapshotDirectory, true);
            Directory.CreateDirectory(_debugSnapshotDirectory);
            Log("Starting the robot controller ...");
            ThreadPool.QueueUserWorkItem(new WaitCallback(Run));
            StatusLED = new BlinkControl(State, terminationRequested, Log);
            State.NewRFID += () => { if (_randomWalkActive) { _randomWalkCommandFinished = true; } };
        }

        #region Core fields

        /// <summary>
        /// The client associated with this robot.
        /// </summary>
        private CommunicationClient _client;
        /// <summary>
        /// Object to sync on for command queue I/O.
        /// </summary>
        private object _commandSubmissionSync = new object();
        /// <summary>
        /// Function indicating whether the robot is activated or deactivated by the GUI.
        /// </summary>
        private Func<bool> _robotActive;
        /// <summary>
        /// Function indicating whether the app requested termination of all processing.
        /// </summary>
        private Func<bool> _terminationRequested;
        /// <summary>
        /// A logging function.
        /// </summary>
        private Action<string> _logger;
        /// <summary>
        /// The command queue. Commands are processed one after another. The queue is reset when a new valid command is submitted.
        /// </summary>
        private Queue<RobotActions> _commandQueue = new Queue<RobotActions>();
        /// <summary>
        /// The helper for line following.
        /// </summary>
        public LineFollower LineFollower { get; private set; }
        /// <summary>
        /// The PID-controller calculating the speed for correcting the movement.
        /// </summary>
        public PidCtrl PIDControl { get; private set; }
        /// <summary>
        /// The status LED indicating the robots current state.
        /// </summary>
        public BlinkControl StatusLED { get; private set; }
        /// <summary>
        /// The current state information of the robot.
        /// </summary>
        public RobotState State;
        /// <summary>
        /// The robot that is controlled.
        /// </summary>
        private iRobot _robot;
        /// <summary>
        /// The speed of the robot's wheels while turning.
        /// </summary>
        private int _turnSpeed = 150;  // mm/s
        /// <summary>
        /// The speed of the robot's wheels while turning.
        /// </summary>
        public int TurnSpeed
        {
            get { return _turnSpeed; }
            set
            {
                if (value > 200) _turnSpeed = 200;
                else if (value < 20) _turnSpeed = 20;
                else _turnSpeed = value;
            }
        }
        /// <summary>
        /// Time to simulate a pickup operation.
        /// </summary>
        private int _pickupTime = (int)Math.Round(TimeSpan.FromSeconds(8).TotalMilliseconds);
        /// <summary>
        /// Time to simulate a setdown operation.
        /// </summary>
        private int _setdownTime = (int)Math.Round(TimeSpan.FromSeconds(8).TotalMilliseconds);
        /// <summary>
        /// Used to signal the controller that it shall wait for the specified amount of time.
        /// </summary>
        private double _waitTime = 0;
        /// <summary>
        /// Indicates whether beaconing is activated.
        /// </summary>
        private bool _beaconingActivated = false;
        /// <summary>
        /// The last time the position was broadcasted while being idle.
        /// </summary>
        internal DateTime LastPositionBeacon = DateTime.MinValue;
        /// <summary>
        /// The period in which the position is beaconed by an idle robot.
        /// </summary>
        private TimeSpan _positionBeaconDelay = TimeSpan.FromSeconds(30);
        /// <summary>
        /// Indicates an immediate abort of what the robot is currently doing.
        /// </summary>
        private bool _immediateAbort = false;
        /// <summary>
        /// The time after which the robot gets impatient and starts turning instead of keeping to move forward.
        /// </summary>
        private TimeSpan _turnResolveDelay = TimeSpan.FromSeconds(5);
        /// <summary>
        /// The time after which the robot gets impatient and starts turning instead of keeping to move forward.
        /// </summary>
        public TimeSpan TurnResolveDelay { get { return _turnResolveDelay; } set { _turnResolveDelay = value; } }
        /// <summary>
        /// Indicates whether turning is used to try to resolve stuck situation while moving in PID loops.
        /// </summary>
        private bool _turnResolve = true;
        /// <summary>
        /// Indicates whether turning is used to try to resolve stuck situation while moving in PID loops.
        /// </summary>
        public bool TurnResolve { get { return _turnResolve; } set { _turnResolve = value; } }
        /// <summary>
        /// The time the thread is sent to sleep between two PID iterations.
        /// </summary>
        private int _pidLoopDelay = 5;
        /// <summary>
        /// The time the thread is sent to sleep between two PID iterations.
        /// </summary>
        public int PIDLoopDelay { get { return _pidLoopDelay; } set { _pidLoopDelay = value; } }

        #endregion

        #region Meta-information

        /// <summary>
        /// The current battery level of the robot.
        /// </summary>
        public int BatteryLevel { get { return _robot.BatteryLevel; } }
        /// <summary>
        /// The current battery capacity of the robot.
        /// </summary>
        public int BatteryCapacity { get { return _robot.BatteryCapacity; } }
        /// <summary>
        /// The current battery temperature of the robot.
        /// </summary>
        public int Temperature { get { return _robot.Temperature; } }

        /// <summary>
        /// The last completely executed command.
        /// </summary>
        public RobotActions LastCommand { get; private set; }
        /// <summary>
        /// Indicates whether the robot is currently moving in a straight manner.
        /// </summary>
        public bool IsForwardMovement { get; private set; }
        /// <summary>
        /// The PID-loop counter of the command before the last command.
        /// </summary>
        private int _previousCommandPIDLoops = 0;
        /// <summary>
        /// The PID loops done during execution of the last command.
        /// </summary>
        public int LastCommandsPIDLoops { get; private set; }
        /// <summary>
        /// The frame counter of the command before the last command.
        /// </summary>
        private int _previousCommandFrames = 0;
        /// <summary>
        /// The frames calculated during execution of the last command.
        /// </summary>
        public int LastCommandsFrames { get; private set; }
        /// <summary>
        /// Used to log a lost orientation only once per PID-loop.
        /// </summary>
        private bool _orientationLostLogged = false;

        #endregion

        #region Parameters

        /// <summary>
        /// Enables or disables debug messages.
        /// </summary>
        private bool _debugMode = true;

        /// <summary>
        /// Enables or disables the dummy mode - for when there is no robot to test with.
        /// </summary>
        private bool _dummyMode = true;
        /// <summary>
        /// Enables or disables the dummy mode - for when there is no robot to test with.
        /// </summary>
        public bool DummyMode { get { return _dummyMode; } set { _dummyMode = value; } }

        /// <summary>
        /// Enables and disables time-based turning.
        /// </summary>
        public bool TimedTurning { get; set; }

        /// <summary>
        /// The delay used when turning in a time-based manner.
        /// </summary>
        private TimeSpan _timedTurningDelay = TimeSpan.FromSeconds(0.5);
        /// <summary>
        /// The delay used when turning in a time-based manner.
        /// </summary>
        public TimeSpan TimedTurningDelay { get { return _timedTurningDelay; } set { _timedTurningDelay = value; } }

        /// <summary>
        /// Directory for taking debug snapshots.
        /// </summary>
        private string _debugSnapshotDirectory = "";
        /// <summary>
        /// The number of debug snapshots taken.
        /// </summary>
        private int _currentDebugSnapshotCount = 0;
        /// <summary>
        /// The current original frame.
        /// </summary>
        private Image<Bgr, Byte> _currentFrameOriginal;
        /// <summary>
        /// The current processed frame.
        /// </summary>
        private Image<Gray, Byte> _currentFrameProcessed;
        /// <summary>
        /// Indicates whether new frames are available for drawing.
        /// </summary>
        private bool _newFramesAvailable = true;
        /// <summary>
        /// The sync-lock for updating the current frames.
        /// </summary>
        private object _frameUpdateLock = new object();
        /// <summary>
        /// The directory used to store the snapshots.
        /// </summary>
        private string _snapshotDirectory = "";
        /// <summary>
        /// The number of snapshots already taken.
        /// </summary>
        private int _currentSnapshotCount = 0;
        /// <summary>
        /// Indicates whether the robot will take snapshots periodically.
        /// </summary>
        public bool TakeSnapshots { get; private set; }
        /// <summary>
        /// The last time a snapshot was taken.
        /// </summary>
        private DateTime _lastSnapshot = DateTime.MinValue;
        /// <summary>
        /// The delay between two snapshots.
        /// </summary>
        public TimeSpan SnapshotDelay { get; set; }
        /// <summary>
        /// Starts taking periodic snapshots.
        /// </summary>
        /// <param name="snapshotPath">The path to the snapshots.</param>
        public void EnableSnapshotMode(string snapshotPath)
        {
            if (SnapshotDelay == TimeSpan.Zero)
                SnapshotDelay = TimeSpan.FromSeconds(8);
            _snapshotDirectory = snapshotPath;
            if (Directory.Exists(snapshotPath))
                Directory.Delete(snapshotPath, true);
            Directory.CreateDirectory(snapshotPath);
            _currentSnapshotCount = 0;
            TakeSnapshots = true;
        }
        /// <summary>
        /// Stops taking periodic snapshots.
        /// </summary>
        public void DisableSnapshotMode() { TakeSnapshots = false; }

        /// <summary>
        /// Function to set the filter.
        /// </summary>
        private Action<Hsv> _setFilter;
        /// <summary>
        /// Indicates if the automatic camera improvement is active.
        /// </summary>
        public bool EnableCameraImprovement = false;
        /// <summary>
        /// The delay between two camera improvements.
        /// </summary>
        public int CameraImprovementDelay = 5;
        /// <summary>
        /// Time of the last automatic camera improvement.
        /// </summary>
        private DateTime _lastImprovement = DateTime.Now;
        /// <summary>
        /// The delta E for color similarity.
        /// </summary>
        public int ColorSimilarityDeltaE = 10;

        #endregion

        #region Helpers

        /// <summary>
        /// Outputs a log message.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        private void Log(string msg)
        {
            if (_debugMode || _dummyMode)
                _logger(msg);
        }

        /// <summary>
        /// Takes a snapshot of the current original image for debug purposes.
        /// </summary>
        public void TakeDebugSnapshot()
        {
            if (_debugMode)
            {
                string debugImagePath = Path.Combine(_debugSnapshotDirectory, "debug" + _currentDebugSnapshotCount++);
                Log("Logging images to " + debugImagePath + "*.png");
                lock (_frameUpdateLock)
                {
                    Image<Bgr, Byte> original = _currentFrameOriginal;
                    Image<Gray, Byte> processed = _currentFrameProcessed;
                    if (original != null)
                        original.Save(debugImagePath + "orig.png");
                    if (processed != null)
                        processed.Save(debugImagePath + "proc.png");
                }
            }
        }

        /// <summary>
        /// Returns the current processed frames, if new ones are available.
        /// </summary>
        /// <param name="originalImage">The processed original image.</param>
        /// <param name="processedImage">The processed gray image.</param>
        public void UpdateFrames(out Image<Bgr, byte> originalImage, out Image<Gray, byte> processedImage)
        {
            if (_newFramesAvailable && _currentFrameOriginal != null && _currentFrameProcessed != null)
            {
                _newFramesAvailable = false;
                // Display the images
                lock (_frameUpdateLock)
                {
                    originalImage = _currentFrameOriginal.Copy();  // display original
                    processedImage = _currentFrameProcessed.Copy();  // display processed
                }
            }
            else
            {
                originalImage = null;  // display original
                processedImage = null;  // display processed
            }
        }

        #endregion

        /// <summary>
        /// Submits a new command to the robot. The command is checked for validity, translated and then submitted to the queue. If the queue is not empty it will be reset.
        /// </summary>
        /// <param name="cmd">The command for the robot to execute.</param>
        public void SubmitCommand(string cmd)
        {
            // Log
            Log("Got a new command: " + cmd);
            // Check validness
            if (RobotTranslator.CheckMessageValidnessTo(cmd))
            {
                // Activate beaconing when receiving the first command and keep the bot from beaconing while receiving further commands
                _beaconingActivated = true; LastPositionBeacon = DateTime.Now;
                // Abort any active command
                _immediateAbort = true;
                // Decode the command
                RobotMessageResultServer command = RobotTranslator.DecodeTo(cmd);
                switch (command.Type)
                {
                    case RobotMessageTypesServer.Go:
                        lock (_commandSubmissionSync)
                        {
                            _waitTime = command.WaitTime;
                            _commandQueue = new Queue<RobotActions>(command.Actions);
                            if (!_dummyMode)
                                _robot.PrepareForNewCommand();
                        }
                        break;
                    case RobotMessageTypesServer.Rest:
                        {
                            lock (_commandSubmissionSync)
                            {
                                _commandQueue.Clear();
                                _commandQueue.Enqueue(RobotActions.Rest);
                            }
                        }
                        break;
                    case RobotMessageTypesServer.Pickup:
                        {
                            lock (_commandSubmissionSync)
                            {
                                _commandQueue.Clear();
                                _commandQueue.Enqueue(RobotActions.Pickup);
                            }
                        }
                        break;
                    case RobotMessageTypesServer.Setdown:
                        {
                            lock (_commandSubmissionSync)
                            {
                                _commandQueue.Clear();
                                _commandQueue.Enqueue(RobotActions.Setdown);
                            }
                        }
                        break;
                    case RobotMessageTypesServer.GetItem:
                        {
                            lock (_commandSubmissionSync)
                            {
                                _commandQueue.Clear();
                                _commandQueue.Enqueue(RobotActions.GetItem);
                            }
                        }
                        break;
                    case RobotMessageTypesServer.PutItem:
                        {
                            lock (_commandSubmissionSync)
                            {
                                _commandQueue.Clear();
                                _commandQueue.Enqueue(RobotActions.PutItem);
                            }
                        }
                        break;
                    default: throw new ArgumentException("Unknown command type: " + command.Type);
                }
            }
        }

        /// <summary>
        /// The Kp of the PID-control loop.
        /// </summary>
        public int PIDKp { get { return PIDControl.Kp; } set { PIDControl.Kp = value; } }

        /// <summary>
        /// The Ki of the PID-control loop.
        /// </summary>
        public int PIDKi { get { return PIDControl.Ki; } set { PIDControl.Ki = value; } }

        /// <summary>
        /// Submits a new image to the controller. The image is processed with CV techniques and the result is made available for the PID-controller.
        /// </summary>
        /// <param name="lowerLimit">The lower limit for the filter.</param>
        /// <param name="upperLimit">The upper limit for the filter.</param>
        /// <param name="originalFrame">The frame to process.</param>
        /// <param name="grayFrame">The resulting frame.</param>
        public void ProcessImage(Hsv lowerLimit, Hsv upperLimit, ref Image<Bgr, Byte> originalFrame, ref Image<Gray, Byte> grayFrame)
        {
            // Take snapshot if required
            if (TakeSnapshots && DateTime.Now - _lastSnapshot > SnapshotDelay)
            {
                _lastSnapshot = DateTime.Now;
                string snapshotFilename = Path.Combine(_snapshotDirectory, "snapshot" + _currentSnapshotCount++ + ".png");
                Log("Saving periodic snapshot to " + snapshotFilename);
                originalFrame.Save(snapshotFilename);
            }

            // Autoset Filter
            if (IsForwardMovement)
                ImproveCamera(originalFrame, grayFrame);

            // Process the image
            LineFollower.DetectLine(ref originalFrame, ref grayFrame, lowerLimit, upperLimit);
            // Store reference to original image and processed image
            lock (_frameUpdateLock)
            {
                if (_currentFrameOriginal != null)
                    _currentFrameOriginal.Dispose();
                if (_currentFrameProcessed != null)
                    _currentFrameProcessed.Dispose();
                _currentFrameOriginal = originalFrame;  // save original
                _currentFrameProcessed = grayFrame;  // save processed 
            }
            _newFramesAvailable = true;
        }

        /// <summary>
        /// Improves the camera automatically.
        /// </summary>
        /// <param name="originalFrame">The frame to process.</param>
        /// <param name="grayFrame">The resulting frame.</param>
        private void ImproveCamera(Image<Bgr, byte> originalFrame, Image<Gray, byte> grayFrame)
        {
            // Autoset Filter
            if (EnableCameraImprovement)
            {
                // See if we have to do a new improvement
                if (DateTime.Now - _lastImprovement > TimeSpan.FromSeconds(CameraImprovementDelay))
                {
                    Hsv newValue = LineFollower.ImproveCamera(originalFrame, grayFrame);
                    _setFilter(newValue);
                    _lastImprovement = DateTime.Now;
                }
            }
        }

        #region RFID handling

        /// <summary>
        /// The RFID reader the robot uses to recognize waypoints.
        /// </summary>
        private RfidReader _rfidReader;
        /// <summary>
        /// The thread in which RFID tags are read.
        /// </summary>
        private Thread _rfidReaderThread;
        /// <summary>
        /// The last recognized RFID tag.
        /// </summary>
        private string _lastTag = "";
        /// <summary>
        /// The action that is called when a new RFID-tag was found.
        /// </summary>
        private Action<string> _newTagFoundCallback;
        /// <summary>
        /// Indicates whether this bot's RFID reader is running.
        /// </summary>
        public bool RFIDReaderConnected { get { return !(_rfidReader == null) && _rfidReader.IsConnected; } }
        /// <summary>
        /// The audio player to use to signal newly found RFID-tags.
        /// </summary>
        private SoundPlayer _rfidFoundAudioPlayer = new SoundPlayer(RAWSimO.Hardware.Properties.Resources.AlienMotionTracker);

        /// <summary>
        /// Connects the RFID reader behind the given COM interface.
        /// </summary>
        /// <param name="portName">The name of the COM port.</param>
        public void ConnectRFIDReader(string portName)
        {
            if (_rfidReader == null || !_rfidReader.IsConnected)
            {
                _rfidReader = new RfidReader();
                _rfidReader.Connect(portName);
                ThreadStart _threaddelegate = new ThreadStart(ReadTagAsync);
                _rfidReaderThread = new Thread(_threaddelegate);
                _rfidReaderThread.Start();
            }
        }

        /// <summary>
        /// Disconnects the RFID-reader.
        /// </summary>
        public void DisconnectRFIDReader() { if (_rfidReader != null) _rfidReader.Disconnect(); }

        /// <summary>
        /// The main loop that reads and processes RFID-tags.
        /// </summary>
        private void ReadTagAsync()
        {
            while (_rfidReader.IsConnected)
            {
                // Scan 20 times per second
                Thread.Sleep(50);
                string newTag = _rfidReader.GetCurrentTag();
                // Recognize new tag (if different from old one and it seems to be valid)
                if (newTag.Length == RfidReader.TAG_LENGTH && // Ignore tags of wrong length
                    !string.Equals(newTag, _lastTag)) // Ignore tags matching the old one
                {
                    // Log
                    Log("Read TAG: " + newTag);
                    // Now send it to the server.
                    if (_client.IsConnected)
                        _client.SendMsg(RobotTranslator.EncodeFrom(newTag));
                    // Submit it to the GUI
                    _newTagFoundCallback(newTag);
                    // Beep
                    _rfidFoundAudioPlayer.Play();
                    // Signal the new RFID tag
                    State.CurrentRFIDTag = newTag;
                    State.NotifyNewRFIDFound();
                }
                // Store last tag (if it seems to be valid)
                if (newTag.Length == RfidReader.TAG_LENGTH)
                    _lastTag = newTag;
            }
        }

        #endregion

        #region Robot command adapters

        /// <summary>
        /// Tells the robot to do a 90° turn to the left.
        /// </summary>
        /// <param name="timed">Indicates whether the turn is timed or just relying on visual information.</param>
        private void DoTurn90Left(bool timed)
        {
            //reset Stored Speeds
            _robot.resetLastStoredSpeeds();
            if (_robotActive() && _robot.IsOpen())
            {
                Log("Commencing turn left ... ");

                if (!timed)
                {
                    bool lineleft = false;
                    bool newLineDetected = false;
                    while (!newLineDetected)
                    {
                        // See whether we left the line now
                        if (LineFollower.Result == null)
                        {
                            Log("Warning! No CV information available - stopping turn until we have visual information again");
                            _robot.MoveStop();
                            continue;
                        }

                        // First keep turning until we don't see the first line anymore, then turn until we see the next one
                        if (!lineleft)
                        {
                            // Check whether a line was detected
                            if (!LineFollower.Result.LineInFocus)
                            {
                                // There is no line anymore - from now on look for the new line
                                lineleft = true;
                                // Log
                                Log("left the previous line ... ");
                            }

                            // Keep turning
                            _robot.DriveDirect(-_turnSpeed, _turnSpeed);
                        }
                        else
                        {
                            // Keep looking for the new line
                            if (!LineFollower.Result.LineInFocus)
                            {
                                // No new line detected - keep turning
                                _robot.DriveDirect(-_turnSpeed, _turnSpeed);
                            }
                            else
                            {
                                // We are facing the new line - stop turning
                                _robot.MoveStop();
                                newLineDetected = true;
                                // Log
                                Log("detected the new line - stopping ... ");
                            }
                        }
                    }
                }
                else
                {
                    // Stage 1 (turn for a period of time)
                    DateTime turnStart = DateTime.Now;
                    while (DateTime.Now - turnStart < TimedTurningDelay)
                    {
                        _robot.DriveDirect(-_turnSpeed, _turnSpeed);
                        Thread.Sleep(5);
                    }
                    Log("timed turn done ... ");
                    // Stage 2 (keep turning until a line is recognized)
                    while (!LineFollower.Result.LineInFocus)
                    {
                        _robot.DriveDirect(-_turnSpeed, _turnSpeed);
                        Thread.Sleep(5);
                    }
                    Log("detected the new line - stopping ... ");
                    // Stage 3 (line detected - stop the turn)
                    _robot.MoveStop();
                }
                // Log
                Log("Done!");
            }
        }

        /// <summary>
        /// Tells the robot to do a 90° turn to the right.
        /// </summary>
        /// <param name="timed">Indicates whether the turn is timed or just relying on visual information.</param>
        private void DoTurn90Right(bool timed)
        {
            //reset Stored Speeds
            _robot.resetLastStoredSpeeds();
            if (_robotActive() && _robot.IsOpen())
            {
                Log("Commencing turn right ... ");

                if (!timed)
                {
                    bool lineleft = false;
                    bool newLineDetected = false;
                    while (!newLineDetected)
                    {
                        // See whether we left the line now
                        if (LineFollower.Result == null)
                        {
                            Log("Warning! No CV information available - stopping turn until we have visual information again");
                            _robot.MoveStop();
                            continue;
                        }

                        // First keep turning until we don't see the first line anymore, then turn until we see the next one
                        if (!lineleft)
                        {
                            // Check whether a line was detected
                            if (!LineFollower.Result.LineInFocus)
                            {
                                // There is no line anymore - from now on look for the new line
                                lineleft = true;
                                // Log
                                Log("left the previous line ... ");
                            }

                            // Keep turning
                            _robot.DriveDirect(_turnSpeed, -_turnSpeed);
                        }
                        else
                        {
                            // Keep looking for the new line
                            if (!LineFollower.Result.LineInFocus)
                            {
                                // No new line detected - keep turning
                                _robot.DriveDirect(_turnSpeed, -_turnSpeed);
                            }
                            else
                            {
                                // We are facing the new line - stop turning
                                _robot.MoveStop();
                                newLineDetected = true;
                                // Log
                                Log("detected the new line - stopping ... ");
                            }
                        }
                    }
                }
                else
                {
                    // Stage 1 (turn for a period of time)
                    DateTime turnStart = DateTime.Now;
                    while (DateTime.Now - turnStart < TimedTurningDelay)
                    {
                        _robot.DriveDirect(_turnSpeed, -_turnSpeed);
                        Thread.Sleep(5);
                    }
                    Log("timed turn done ... ");
                    // Stage 2 (keep turning until a line is recognized)
                    while (!LineFollower.Result.LineInFocus)
                    {
                        _robot.DriveDirect(_turnSpeed, -_turnSpeed);
                        Thread.Sleep(5);
                    }
                    Log("detected the new line - stopping ... ");
                    // Stage 3 (line detected - stop the turn)
                    _robot.MoveStop();
                }
                // Log
                Log("Done!");
            }
        }

        /// <summary>
        /// Stops the robot.
        /// </summary>
        private void DoStop()
        {
            if (_robotActive() && _robot.IsOpen())
            {
                Log("Stopping ... ");

                // Stop the robot
                _robot.MoveStop();
            }
            //reset Stored Speeds
            _robot.resetLastStoredSpeeds();
        }

        private bool DoRobPidDrive()
        {
            //reset Stored Speeds
            _robot.resetLastStoredSpeeds();
            // Determine speed according to line recognition
            int rightSpeed = 0;
            int leftSpeed = 0;
            if (LineFollower.Result == null)
                Log("Warning! CV did not return a valid list of lines detected. Stopping.");
            else
                PIDControl.DoPidLoop(LineFollower.Result, false, ref rightSpeed, ref leftSpeed);

            // Check whether the line was recognized at all
            if (LineFollower.Result == null || !LineFollower.Result.AnyLine)
            {
                // Log a lost vision of the line only once per loop
                if (!_orientationLostLogged)
                {
                    _orientationLostLogged = true;
                    Log("Warning! Lost the line while moving in PID loop. Stopping.");
                }

                // Stop if off track
                if (_robot.IsOpen())
                    _robot.MoveStop();
                // Notify unexpected behavior
                return false;
            }
            else if (State.NewRFIDFound)
            {
                // Stop the robot on a new RFID found and continue with the next action
                if (_robot.IsOpen())
                    _robot.MoveStop();
                // Notify expected behavior
                return true;
            }
            else
            {
                // Keep moving ...
                if (_robotActive() && _robot.IsOpen())
                    _robot.DriveDirect(leftSpeed, rightSpeed);
                // Notify expected behavior
                return true;
            }
        }

        private bool DoRobBackPidDrive()
        {
            //reset Stored Speeds
            _robot.resetLastStoredSpeeds();
            // Determine speed according to line recognition
            int rightSpeed = 0;
            int leftSpeed = 0;
            if (LineFollower.Result == null)
                Log("Warning! CV did not return a valid list of lines detected. Stopping.");
            else
                PIDControl.DoPidLoop(LineFollower.Result, true, ref rightSpeed, ref leftSpeed);

            // Check whether the line was recognized at all
            if (LineFollower.Result == null || !LineFollower.Result.AnyLine)
            {
                // Log a lost vision of the line only once per loop
                if (!_orientationLostLogged)
                {
                    _orientationLostLogged = true;
                    Log("Warning! Lost the line while moving in PID loop. Stopping.");
                }

                // Stop if off track
                if (_robot.IsOpen())
                    _robot.MoveStop();
                // Notify unexpected behavior
                return false;
            }
            else if (State.NewRFIDFound)
            {
                // Stop the robot on a new RFID found and continue with the next action
                if (_robot.IsOpen())
                    _robot.MoveStop();
                // Notify expected behavior
                return true;
            }
            else
            {
                // Keep moving backwards ...
                if (_robotActive() && _robot.IsOpen())
                    _robot.DriveDirect(leftSpeed, rightSpeed);
                // Notify expected behavior
                return true;
            }

        }

        #endregion

        #region Dummy command adapters

        /// <summary>
        /// Tells the robot to do a 90° turn to the left.
        /// </summary>
        /// <param name="timed">Indicates whether the turn is timed or just relying on visual information.</param>
        private void DummyDoTurn90Left(bool timed)
        {
            Log("Commencing turn left ... ");

            if (!timed)
            {
                bool lineleft = false;
                bool newLineDetected = false;
                while (!newLineDetected)
                {
                    // Sleep just a bit
                    Thread.Sleep(5);
                    // See whether we left the line now
                    if (LineFollower.Result == null)
                    {
                        Log("Warning! No CV information available - stopping turn until we have visual information again");
                        continue;
                    }

                    // First keep turning until we don't see the first line anymore, then turn until we see the next one
                    if (!lineleft)
                    {
                        // Check whether a line was detected
                        if (!LineFollower.Result.LineInFocus)
                        {
                            // There is no line anymore - from now on look for the new line
                            lineleft = true;
                            // Log
                            Log("left the previous line ... ");
                        }
                    }
                    else
                    {
                        // Keep looking for the new line
                        if (!LineFollower.Result.LineInFocus)
                        {
                            // No new line detected - keep turning
                        }
                        else
                        {
                            // We are facing the new line - stop turning
                            newLineDetected = true;
                            // Log
                            Log("detected the new line - stopping ... ");
                        }
                    }
                }
            }
            else
            {
                // Stage 1 (turn for a period of time)
                DateTime turnStart = DateTime.Now;
                while (DateTime.Now - turnStart < TimedTurningDelay)
                {
                    Thread.Sleep(5);
                }
                Log("timed turn done ... ");
                // Stage 2 (keep turning until a line is recognized)
                while (!LineFollower.Result.LineInFocus)
                {
                    Thread.Sleep(5);
                }
                Log("detected the new line - stopping ... ");
                // Stage 3 (line detected - stop the turn)
            }
            // Log
            Log("Done!");
        }

        /// <summary>
        /// Tells the robot to do a 90° turn to the right.
        /// </summary>
        /// <param name="timed">Indicates whether the turn is timed or just relying on visual information.</param>
        private void DummyDoTurn90Right(bool timed)
        {
            Log("Commencing turn right ... ");

            if (!timed)
            {
                bool lineleft = false;
                bool newLineDetected = false;
                while (!newLineDetected)
                {
                    // Sleep just a bit
                    Thread.Sleep(5);
                    // See whether we left the line now
                    if (LineFollower.Result == null)
                    {
                        Log("Warning! No CV information available - stopping turn until we have visual information again");
                        continue;
                    }

                    // First keep turning until we don't see the first line anymore, then turn until we see the next one
                    if (!lineleft)
                    {
                        // Check whether a line was detected
                        if (!LineFollower.Result.LineInFocus)
                        {
                            // There is no line anymore - from now on look for the new line
                            lineleft = true;
                            // Log
                            Log("left the previous line ... ");
                        }
                    }
                    else
                    {
                        // Keep looking for the new line
                        if (!LineFollower.Result.LineInFocus)
                        {
                            // No new line detected - keep turning
                        }
                        else
                        {
                            // We are facing the new line - stop turning
                            newLineDetected = true;
                            // Log
                            Log("detected the new line - stopping ... ");
                        }
                    }
                }
            }
            else
            {
                // Stage 1 (turn for a period of time)
                DateTime turnStart = DateTime.Now;
                while (DateTime.Now - turnStart < TimedTurningDelay)
                {
                    Thread.Sleep(5);
                }
                Log("timed turn done ... ");
                // Stage 2 (keep turning until a line is recognized)
                while (!LineFollower.Result.LineInFocus)
                {
                    Thread.Sleep(5);
                }
                Log("detected the new line - stopping ... ");
                // Stage 3 (line detected - stop the turn)
            }
            // Log
            Log("Done!");
        }

        /// <summary>
        /// Stops the robot.
        /// </summary>
        private void DummyDoStop()
        {
            Log("Stopping ... ");
        }

        private bool DummyDoRobPidDrive()
        {
            // Determine speed according to line recognition
            int rightSpeed = 0;
            int leftSpeed = 0;
            if (LineFollower.Result == null)
                Log("Warning! CV did not return a valid list of lines detected. Stopping.");
            else
                PIDControl.DoPidLoop(LineFollower.Result, false, ref rightSpeed, ref leftSpeed);

            // Check whether the line was recognized at all
            if (LineFollower.Result == null || !LineFollower.Result.AnyLine)
            {
                // Log a lost vision of the line only once per loop
                if (!_orientationLostLogged)
                {
                    _orientationLostLogged = true;
                    Log("Warning! Lost the line while moving in PID loop. Stopping.");
                }

                // Stop if off track
                Log("Off track - stopping ...");
                Thread.Sleep(500);
                // Notify unexpected behavior
                return false;
            }
            else if (State.NewRFIDFound)
            {
                // Stop the robot on a new RFID found and continue with the next action
                Log("New tag found - stopping ...");
                Thread.Sleep(500);
                // Notify expected behavior
                return true;
            }
            else
            {
                // Keep moving ...
                Log("Moving with speed (l/r): (" + leftSpeed + "/" + rightSpeed + ") ...");
                Thread.Sleep(500);
                // Notify expected behavior
                return true;
            }
        }

        private bool DummyDoRobBackPidDrive()
        {
            // Determine speed according to line recognition
            int rightSpeed = 0;
            int leftSpeed = 0;
            if (LineFollower.Result == null)
                Log("Warning! CV did not return a valid list of lines detected. Stopping.");
            else
                PIDControl.DoPidLoop(LineFollower.Result, true, ref rightSpeed, ref leftSpeed);

            // Check whether the line was recognized at all
            if (LineFollower.Result == null || !LineFollower.Result.AnyLine)
            {
                // Log a lost vision of the line only once per loop
                if (!_orientationLostLogged)
                {
                    _orientationLostLogged = true;
                    Log("Warning! Lost the line while moving in PID loop. Stopping.");
                }

                // Stop if off track
                Log("Off track - stopping ...");
                Thread.Sleep(500);
                // Notify unexpected behavior
                return false;
            }
            else if (State.NewRFIDFound)
            {
                // Stop the robot on a new RFID found and continue with the next action
                Log("New tag found - stopping ...");
                Thread.Sleep(500);
                // Notify expected behavior
                return true;
            }
            else
            {
                // Keep moving ...
                Log("Moving with speed (l/r): (" + leftSpeed + "/" + rightSpeed + ") ...");
                Thread.Sleep(500);
                // Notify expected behavior
                return true;
            }
        }

        #endregion

        #region Command loop

        private void Run(object state)
        {
            while (!_terminationRequested())
            {
                // Retrieve the next command to execute
                RobotActions currentCommand = RobotActions.Forward;
                bool commandAvailable = false;
                double waitTime = 0;
                lock (_commandSubmissionSync)
                {
                    if (_commandQueue.Any())
                    {
                        waitTime = _waitTime; _waitTime = 0;
                        currentCommand = _commandQueue.Dequeue();
                        commandAvailable = true;
                    }
                }

                // If there is waiting time planned consume it
                if (waitTime > 0)
                {
                    // Stop movement
                    if (!_dummyMode) DoStop();
                    else DummyDoStop();
                    // Wait for the specified time
                    TimeSpan waitTimeSpan = TimeSpan.FromSeconds(waitTime);
                    Thread.Sleep(waitTimeSpan);
                    waitTime = 0;
                }

                // If there is no command just wait for another one
                if (!commandAvailable)
                {
                    // Indicate idle status and sleep
                    if (State.Status != RobotStatus.Idle && State.Status != RobotStatus.Rest && State.Status != RobotStatus.GetItem && State.Status != RobotStatus.PutItem)
                        State.Status = RobotStatus.Idle;
                    // If the robot did not get a command for while resent its position
                    if (_beaconingActivated && DateTime.Now - LastPositionBeacon > _positionBeaconDelay && State.CurrentRFIDTag.Length == RfidReader.TAG_LENGTH)
                    {
                        // Check whether we lost the connection
                        if (_client.IsConnected)
                        {
                            // There is a connection - just beacon the position
                            Log("Getting bored - beaconing position ...");
                            _client.SendMsg(RobotTranslator.EncodeFrom(State.CurrentRFIDTag));
                        }
                        else
                        {
                            // We lost the connection - try to reconnect
                            Log("Lost connection - trying to reconnect in order to beacon position ...");
                            _client.AttemptReconnect();
                            // Wait for the negotiation
                            Thread.Sleep(1000);
                            // Try to beacon the position over the hopefully re-established connection
                            _client.SendMsg(RobotTranslator.EncodeFrom(State.CurrentRFIDTag));
                        }
                        // Remember this position beaconing
                        LastPositionBeacon = DateTime.Now;
                    }
                    // Sleep for a while
                    Thread.Sleep(200);
                    continue;
                }

                // Output command
                Log("Performing new command: " + currentCommand);

                // Do not abort until told to
                _immediateAbort = false;

                // Reset any recognized RFID-tags
                State.SetRFIDRecognized();
                // Command is available - execute it
                switch (currentCommand)
                {
                    case RobotActions.Forward:
                        {
                            Log("Starting PID drive ... ");

                            // Indicate status and start moving
                            State.Status = RobotStatus.Moving;

                            // Log a lost orientation state only once per command
                            _orientationLostLogged = false;

                            // Keep track of the time the robot is "off-road"
                            DateTime lastSuccessfulLoop = DateTime.Now;

                            // Indicate forward movement
                            IsForwardMovement = true;

                            // Loop PID-drive until we hit a RFID-tag
                            while (!State.NewRFIDFound && !_immediateAbort)
                            {
                                // Do loop and see whether we are off the road
                                bool success = !_dummyMode ? DoRobPidDrive() : DummyDoRobPidDrive();
                                // Store last success if so
                                if (success)
                                    lastSuccessfulLoop = DateTime.Now;
                                // If off road for too long do a turn and repeat the command
                                if (TurnResolve && DateTime.Now - lastSuccessfulLoop > TurnResolveDelay)
                                {
                                    // Interrupt current command with a turn - then continue the commands
                                    Log("Cannot see a thing - trying to turn some more until there hopefully is a line again ...");
                                    // Log the image for debug purposes
                                    TakeDebugSnapshot();
                                    // Re-prepare for turning (just in case)
                                    if (!_dummyMode)
                                        _robot.PrepareForNewCommand();
                                    // Turn
                                    switch (LastCommand)
                                    {
                                        case RobotActions.TurnRight:
                                            if (!_dummyMode) DoTurn90Right(true);
                                            else DummyDoTurn90Right(true);
                                            break;
                                        case RobotActions.TurnLeft:
                                        default:
                                            if (!_dummyMode) DoTurn90Left(true);
                                            else DummyDoTurn90Left(true);
                                            break;
                                    }
                                }
                                // Sleep a bit if desired
                                if (PIDLoopDelay > 0)
                                    Thread.Sleep(PIDLoopDelay);
                            }

                            // Indicate end of forward movement
                            IsForwardMovement = false;

                            // Stop after arrival
                            if (!_dummyMode) DoStop();
                            else DummyDoStop();

                            Log("Done!");
                        }
                        break;
                    case RobotActions.Backward:
                        {
                            Log("Starting backward PID drive ... ");

                            // Indicate status and start moving
                            State.Status = RobotStatus.MovingBackwards;

                            // Log a lost orientation state only once per command
                            _orientationLostLogged = false;

                            // Keep track of the time the robot is "off-road"
                            DateTime lastSuccessfulLoop = DateTime.Now;

                            // Loop PID-drive until we hit a RFID-tag
                            while (!State.NewRFIDFound && !_immediateAbort)
                            {
                                // Do loop and see whether we are off the road
                                bool success = !_dummyMode ? DoRobBackPidDrive() : DummyDoRobBackPidDrive();
                                // Store last success if so
                                if (success)
                                    lastSuccessfulLoop = DateTime.Now;
                                // If off road for too long do a turn and repeat the command
                                if (TurnResolve && DateTime.Now - lastSuccessfulLoop > TurnResolveDelay)
                                {
                                    // Interrupt current command with a turn - then continue the commands
                                    Log("Cannot see a thing - trying to turn some more until there hopefully is a line again ...");
                                    // Log the image for debug purposes
                                    TakeDebugSnapshot();
                                    // Re-prepare for turning (just in case)
                                    if (!_dummyMode)
                                        _robot.PrepareForNewCommand();
                                    // Turn
                                    switch (LastCommand)
                                    {
                                        case RobotActions.TurnRight:
                                            if (!_dummyMode) DoTurn90Right(true);
                                            else DummyDoTurn90Right(true);
                                            break;
                                        case RobotActions.TurnLeft:
                                        default:
                                            if (!_dummyMode) DoTurn90Left(true);
                                            else DummyDoTurn90Left(true);
                                            break;
                                    }
                                }
                                // Sleep a bit if desired
                                if (PIDLoopDelay > 0)
                                    Thread.Sleep(PIDLoopDelay);
                            }

                            // Stop after arrival
                            if (!_dummyMode) DoStop();
                            else DummyDoStop();

                            Log("Done!");
                        }
                        break;
                    case RobotActions.TurnLeft:
                        {
                            // Indicate status and start moving
                            State.Status = RobotStatus.Moving;

                            // Simply turn left
                            if (!_dummyMode) DoTurn90Left(TimedTurning);
                            else DummyDoTurn90Left(TimedTurning);
                        }
                        break;
                    case RobotActions.TurnRight:
                        {
                            // Indicate status and start moving
                            State.Status = RobotStatus.Moving;

                            // Simply turn right
                            if (!_dummyMode) DoTurn90Right(TimedTurning);
                            else DummyDoTurn90Right(TimedTurning);
                        }
                        break;
                    case RobotActions.Pickup:
                        {
                            Log("Picking up ...");

                            // Indicate status and simulate pickup
                            State.Status = RobotStatus.Pickup;

                            // Simulate pickup by simply sleeping for a fixed time.
                            Thread.Sleep(_pickupTime);

                            // Indicate that we are carrying a pod now
                            State.CarryingPod = true;

                            // Notify the server about the finished operation
                            _client.SendMsg(RobotTranslator.EncodeFromPickupFinished(true));

                            Log("Finished!");
                        }
                        break;
                    case RobotActions.Setdown:
                        {
                            Log("Setting down ...");

                            // Indicate status and simulate setdown
                            State.Status = RobotStatus.Setdown;

                            // Skip the empty command
                            Thread.Sleep(_setdownTime);

                            // Indicate that we are not carrying a pod anymore
                            State.CarryingPod = false;

                            // Notify the server about the finished operation
                            _client.SendMsg(RobotTranslator.EncodeFromSetdownFinished(true));

                            Log("Finished!");
                        }
                        break;
                    case RobotActions.GetItem:
                        {
                            Log("Station is inserting an item (robot waits for next command)");

                            // Indicate status
                            State.Status = RobotStatus.GetItem;

                            // Nothing the robot can do here - just wait for the next command
                        }
                        break;
                    case RobotActions.PutItem:
                        {
                            Log("Station is extracting an item (robot waits for next command)");

                            // Indicate status
                            State.Status = RobotStatus.PutItem;

                            // Nothing the robot can do here - just wait for the next command
                        }
                        break;
                    case RobotActions.Rest:
                        {
                            Log("Robot is entering rest mode (robot waits for next command)");

                            // Indicate status
                            State.Status = RobotStatus.Rest;

                            // Nothing the robot can do here - just wait for the next command
                        }
                        break;
                    case RobotActions.None:
                        {
                            // Do nothing
                        }
                        break;
                    default: Log("Error - unknown command: " + currentCommand); break;
                }

                // Remember last command
                LastCommand = currentCommand;
                LastCommandsPIDLoops = PIDControl.PIDLoopsDone - _previousCommandPIDLoops;
                _previousCommandPIDLoops = PIDControl.PIDLoopsDone;
                LastCommandsFrames = LineFollower.ImagesProcessedOverall - _previousCommandFrames;
                _previousCommandFrames = LineFollower.ImagesProcessedOverall;
                // Log command statistics
                if (LastCommand == RobotActions.Forward || LastCommand == RobotActions.TurnLeft || LastCommand == RobotActions.TurnRight || LastCommand == RobotActions.Backward)
                    Log("Completed command " + LastCommand.ToString() + " with " + LastCommandsPIDLoops.ToString() + " PID-loops and " + LastCommandsFrames.ToString() + " frames");
                // Log command abort
                if (_immediateAbort)
                    Log("Command got aborted!");
            }
        }

        #endregion

        #region Random walk

        /// <summary>
        /// Indicates whether the robot is currently walking randomly.
        /// </summary>
        private bool _randomWalkActive = false;
        /// <summary>
        /// Indicates whether the last random walk command was finished.
        /// </summary>
        private bool _randomWalkCommandFinished = false;
        /// <summary>
        /// Starts the random walk of the robot.
        /// </summary>
        /// <param name="state">Unused.</param>
        private void RandomWalk(object state)
        {
            // Init
            _randomWalkActive = true;
            Random randomizer = new Random();

            // Keep walking until a stop is requested
            while (_randomWalkActive)
            {
                // Indicate movement
                State.Status = RobotStatus.Moving;

                // Get random number
                double randomNumber = randomizer.NextDouble();

                // See what to do next
                if (randomNumber < 0.5)
                    // Go forward
                    SubmitCommand(RobotTranslator.EncodeTo(0, new RobotActions[] { RobotActions.Forward }));
                else
                {
                    if (randomNumber < 0.75)
                        // Turn left then go forward
                        SubmitCommand(RobotTranslator.EncodeTo(0, new RobotActions[] { RobotActions.TurnLeft, RobotActions.Forward }));
                    else
                        // Turn right then go forward
                        SubmitCommand(RobotTranslator.EncodeTo(0, new RobotActions[] { RobotActions.TurnRight, RobotActions.Forward }));
                }

                // Wait for command to finish
                _randomWalkCommandFinished = false;
                while (!_randomWalkCommandFinished)
                    Thread.Sleep(100);
            }

            // Issue a final stop command
            _immediateAbort = true;

            // Indicate stop
            State.Status = RobotStatus.Idle;
        }
        /// <summary>
        /// Starts a random walk.
        /// </summary>
        public void StartRandomWalk() { ThreadPool.QueueUserWorkItem(new WaitCallback(RandomWalk)); }
        /// <summary>
        /// Stops an active random walk.
        /// </summary>
        public void StopRandomWalk() { _randomWalkActive = false; _randomWalkCommandFinished = true; }

        #endregion

        #region experimental

        public void TestDrive()
        {
            _robot.logCommands = true;

            Random rnd = new Random();
            Log("TestDrive");
            for (int i = 0; i < 30; i++)
            {
                _robot.resetLastStoredSpeeds();
                int tmp = rnd.Next(Convert.ToInt32(50 * 0.6)) - Convert.ToInt32(50 * 0.3);
                // _robot.Drive(50);
                _robot.DriveDirect(50 - tmp, 50 + tmp);
                Thread.Sleep(500);
            }
            _robot.MoveStop();
            _robot.logCommands = false;
        }
        #endregion
    }

    /// <summary>
    /// Contains (shared) information about the robot's current state.
    /// </summary>
    public class RobotState
    {
        public RobotState(RobotCtrl controller) { Controller = controller; }
        /// <summary>
        /// The robot controller this state info belongs to.
        /// </summary>
        private RobotCtrl Controller { get; set; }
        /// <summary>
        /// Indicates that a new RFID-tag was found.
        /// </summary>
        private bool _newRFIDFound = true;
        /// <summary>
        /// The current RFID tag.
        /// </summary>
        public string CurrentRFIDTag = "";
        /// <summary>
        /// Marks a new found RFID-tag.
        /// </summary>
        public void NotifyNewRFIDFound() { Controller.LastPositionBeacon = DateTime.Now; _newRFIDFound = true; if (NewRFID != null) { NewRFID(); } }
        /// <summary>
        /// Processes the newly found RFID-tag.
        /// </summary>
        public void SetRFIDRecognized() { _newRFIDFound = false; }
        /// <summary>
        /// Indicates that a new RFID-tag was found and not yet processed.
        /// </summary>
        public bool NewRFIDFound { get { return _newRFIDFound; } }
        /// <summary>
        /// Handles the event that is raised when a new RFID tag was scanned.
        /// </summary>
        public delegate void NewRFIDFoundHandler();
        /// <summary>
        /// The event that is raised when a new RFID was scanned.
        /// </summary>
        public event NewRFIDFoundHandler NewRFID;
        /// <summary>
        /// A callback that is notified whenever a robot picked up or setdown a pod.
        /// </summary>
        public Action PodChangedCallback { get; set; }
        /// <summary>
        /// Indicates whether the robot is currently carrying a pod.
        /// </summary>
        private bool _carryingPod = false;
        /// <summary>
        /// Indicates whether the robot is currently carrying a pod.
        /// </summary>
        public bool CarryingPod { get { return _carryingPod; } internal set { _carryingPod = value; if (PodChangedCallback != null) { PodChangedCallback(); } } }
        /// <summary>
        /// A callback that is notified when the status of the robot changed.
        /// </summary>
        public Action StatusChangedCallback { get; set; }
        /// <summary>
        /// The status the robot is currently in.
        /// </summary>
        private RobotStatus _status = RobotStatus.Idle;
        /// <summary>
        /// The status the robot is currently in.
        /// </summary>
        public RobotStatus Status { get { return _status; } internal set { _status = value; if (StatusChangedCallback != null) { StatusChangedCallback(); } } }
    }
    /// <summary>
    /// Contains that basic status the robot can obtain.
    /// </summary>
    public enum RobotStatus
    {
        /// <summary>
        /// Indicates that the robot is idling.
        /// </summary>
        Idle,
        /// <summary>
        /// Indicates that the robot is moving.
        /// </summary>
        Moving,
        /// <summary>
        /// Indicates that the robot is moving backwards.
        /// </summary>
        MovingBackwards,
        /// <summary>
        /// Indicates that the robot is picking up a pod.
        /// </summary>
        Pickup,
        /// <summary>
        /// Indicates that the robot is setting down a pod.
        /// </summary>
        Setdown,
        /// <summary>
        /// Indicates that the robot is waiting at the station because an item is inserted to its pod.
        /// </summary>
        GetItem,
        /// <summary>
        /// Indicates that the robot is waiting at the station because an item is extracted from its pod.
        /// </summary>
        PutItem,
        /// <summary>
        /// Indicates that the robot is resting.
        /// </summary>
        Rest
    }
}
