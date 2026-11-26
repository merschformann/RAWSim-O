using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RAWSimO.Hardware.RobotHardware
{
    public class iRobot
    {
        public enum RoombaOpCode : byte
        {
            INVALID = 0,
            START = 128,    //0x80
            BAUD = 129,     //0x81
            CONTROL = 130,  //0x82
            SAFE = 131,     //0x83
            FULL = 132,     //0x84
            POWER = 133,    //0x85
            SPOT = 134,     //0x86
            CLEAN = 135,    //0x87
            MAX = 136,      //0x88
            DRIVE = 137,    //0x89
            MOTORS = 138,   //0x8A
            LEDS = 139,     //0x8B
            SONG = 140,     //0x8C
            PLAY = 141,     //0x8D
            SENSORS = 142,  //0x8E
            DOCK = 143,      //0x8F
            DRIVE_DIRECT = 145
        }

        /// <summary>
        /// The serial port to communicate with the robot.
        /// </summary>
        private SerialPort serialPort;
        /// <summary>
        /// Default speed to use.
        /// </summary>
        private int defaultSpeed = 30;
        /// <summary>
        /// Times the sensor polling.
        /// </summary>
        private Timer Clock;
        /// <summary>
        /// The delay between two consecutive sensor polls.
        /// </summary>
        private TimeSpan delay = TimeSpan.FromSeconds(30);
        /// <summary>
        /// An optional logger for debug purposes.
        /// </summary>
        private Action<string> _logger;
        /// <summary>
        /// saves the last sended left Speed of drive_Direct
        /// </summary>
        private int _lastLeftSpeed;
        /// <summary>
        /// saves the last sended right Speed of drive_Direct
        /// </summary>
        private int _lastRightSpeed;
        /// <summary>
        /// lock Object
        /// </summary>
        private Object thisLock = new Object();
        /// <summary>
        /// true if commands should send in Log
        /// </summary>
        public bool logCommands = false;

        /// <summary>
        /// resets the last Stored Speeds
        /// </summary>
        public void resetLastStoredSpeeds() { _lastLeftSpeed = 0; _lastRightSpeed = 0; }

        public iRobot(Action<string> logger) { _logger = logger; }

        ~iRobot()
        {
            //close and dispose serial port object
            Disconnect();
        }

        public void Log(string msg)
        {
            if (_logger != null)
                _logger(msg);
        }

        public bool Connect(string com_port)
        {
            try
            {
                this.serialPort = new SerialPort();
                this.serialPort.PortName = com_port;
                this.serialPort.BaudRate = 115200;
                //this.serialPort1.DataBits = 8;
                //this.serialPort1.DtrEnable = false;
                //this.serialPort1.StopBits = StopBits.One;
                //this.serialPort1.Handshake = Handshake.None;
                //this.serialPort1.Parity = Parity.None;
                //this.serialPort1.RtsEnable = false;
                //this.serialPort1.Close();
                this.serialPort.Open();

                Prepare();

                //---create a event handler for serial data recieved from irobot
                serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceived);

                //---create a thread that acts like a timer, to control the data flow from irobot sensors
                TimerCallback cb = new TimerCallback(Timer_Tick);       // Create the timer callback delegate.
                Clock = new System.Threading.Timer(cb, null, TimeSpan.Zero, delay); // Create the timer. It is autostart, so creating the timer will start it.


                return serialPort.IsOpen;
            }
            catch { return false; }
        }

        public bool Disconnect()
        {
            try
            {
                if (serialPort.IsOpen)
                {
                    Clock.Dispose();
                    PowerDown();
                    serialPort.Close();
                    return true;
                }
            }
            catch { return false; }
            return false;
        }

        public bool IsOpen() { try { return serialPort != null && serialPort.IsOpen; } catch { return false; } }

        private void Prepare()
        {
            serialPort.Write(new byte[] { (byte)RoombaOpCode.START }, 0, 1);
            serialPort.Write(new byte[] { (byte)RoombaOpCode.CONTROL }, 0, 1);
        }

        /// <summary>
        /// Prepare the robot for the next command.
        /// </summary>
        public void PrepareForNewCommand() { Prepare(); }

        #region Commands

        public void DriveDirect(int left_speed, int right_speed)
        {
            //only send new DriveDirect Command, if there is a new Speed
            if (left_speed != _lastLeftSpeed && right_speed != _lastRightSpeed)
            {
                //Drive Direct
                //Serial sequence: [145] [Right velocity high byte] [Right velocity low byte] [Left velocity high byte] [Left velocity low byte]
                byte left_speed_hi = (byte)(((Velocity)left_speed).ToInt >> 8); //velocity, a positive number = foreward
                byte left_speed_lo = (byte)(((Velocity)left_speed).ToInt & 255);

                byte right_speed_hi = (byte)(((Velocity)right_speed).ToInt >> 8);
                byte right_speed_lo = (byte)(((Velocity)right_speed).ToInt & 255);

                byte[] cmd = { (byte)RoombaOpCode.DRIVE_DIRECT, right_speed_hi, right_speed_lo, left_speed_hi, left_speed_lo };
                sendCommand(cmd);

                _lastLeftSpeed = left_speed;
                _lastRightSpeed = right_speed;
            }

        }
        public void Drive(int speed)
        {
            //Direct
            //Serial sequence: [137] [Velocity high byte] [Velocity low byte] [Radius high byte] [Radius low byte]
            byte speed_hi = (byte)(((Velocity)speed).ToInt >> 8); //velocity, a positive number = foreward
            byte speed_lo = (byte)(((Velocity)speed).ToInt & 255);

            byte radius_hi = (byte)(80);
            byte radius_lo = (byte)(00);

            byte[] cmd = { (byte)RoombaOpCode.DRIVE, speed_hi, speed_lo, radius_hi, radius_lo };
            sendCommand(cmd);
        }

        /// <summary>
        /// Turns the robot right with the given speed.
        /// </summary>
        /// <param name="speed">The speed of the turn.</param>
        public void MoveRight(int speed)
        {
            if (speed == 0)
                speed = defaultSpeed;

            byte speedHi = (byte)(speed >> 8);
            byte speedLo = (byte)(speed & 255);

            //Special cases: Turn in place clockwise = -1 = 0xFFFF
            byte byAngleHi = (byte)255;  //radius
            byte byAngleLo = (byte)255;

            byte[] cmd = { (byte)RoombaOpCode.DRIVE, speedHi, speedLo, byAngleHi, byAngleLo };
            sendCommand(cmd);

        }

        /// <summary>
        /// Turns the robot left with the given speed.
        /// </summary>
        /// <param name="speed">The speed of the turn.</param>
        public void MoveLeft(int speed)
        {
            if (speed == 0)
                speed = defaultSpeed;

            byte speedHi = (byte)(speed >> 8);
            byte speedLo = (byte)(speed & 255);

            //Special cases: Turn in place counter-clockwise = 1 = 0x0001
            byte byAngleHi = (byte)0;
            byte byAngleLo = (byte)1;

            byte[] cmd = { (byte)RoombaOpCode.DRIVE, speedHi, speedLo, byAngleHi, byAngleLo };
            sendCommand(cmd);

        }


        /// <summary>
        /// The command causing the robot to stop all movement.
        /// </summary>
        private byte[] _commandStop = new byte[] { (byte)RoombaOpCode.DRIVE, 0, 0, 0, 0 };
        /// <summary>
        /// Stops all movement of the robot.
        /// </summary>
        public void MoveStop()
        {
            // Send command
            sendCommand(_commandStop);
        }

        public void SetLED(int type)
        {
            //To turn on the Home LED and light the Power LED green at half intensity, send the serial byte sequence
            //[139] [4] [0] [128].
            //Serial sequence: [139] [LED Bits] [Power Color] [Power Intensity]
            if (type == 1)
            {
                byte[] cmd = { (byte)RoombaOpCode.LEDS, 4, 255, 255 };
                sendCommand(cmd);
            }
            else if (type == 2)
            {
                byte[] cmd = { (byte)RoombaOpCode.LEDS, 4, 10, 255 };
                sendCommand(cmd);
            }
            else
            {
                byte[] cmd = { (byte)RoombaOpCode.LEDS, 4, 0, 255 };
                sendCommand(cmd);
            }
        }

        /// <summary>
        /// Plays the specified sound.
        /// </summary>
        /// <param name="note">The sound to play.</param>
        private void PlayNote(int note)
        {
            byte[] cmd = { (byte)RoombaOpCode.SONG, 3, 1, (byte)note, (byte)10, (byte)RoombaOpCode.PLAY, 3 };
            sendCommand(cmd);
        }

        /// <summary>
        /// The start command.
        /// </summary>
        private byte[] _commandStart = new byte[] { 128 };
        /// <summary>
        /// Starts the robot's system.
        /// </summary>
        private void Start()
        {
            sendCommand(_commandStart);
        }
        /// <summary>
        /// The power down command.
        /// </summary>
        private byte[] _commandPowerDown = new byte[] { 133 };
        private byte[] _commandOff = new byte[] { 173 };
        /// <summary>
        /// Powers down the robot.
        /// </summary>
        public void PowerDown()
        {
            //TODO eventually send Passive Mode
            sendCommand(_commandPowerDown);
            sendCommand(_commandOff);
        }

        #endregion

        #region Data retrieval

        /// <summary>
        /// The current battery level.
        /// </summary>
        public int BatteryLevel { get; private set; }
        /// <summary>
        /// The capacity of the battery.
        /// </summary>
        public int BatteryCapacity { get; private set; }
        /// <summary>
        /// The temperature of the battery.
        /// </summary>
        public int Temperature { get; private set; }

        /// <summary>
        /// The number of expected bytes.
        /// </summary>
        private int _packetsExpected = 0;
        /// <summary>
        /// The last time packets were requested.
        /// </summary>
        private DateTime _packetRequestTime = DateTime.MinValue;
        /// <summary>
        /// The timeout after which packets are requested again.
        /// </summary>
        private TimeSpan _packetRequestTimeout = TimeSpan.FromSeconds(3);
        /// <summary>
        /// The incoming bytes.
        /// </summary>
        private List<byte> _packetsIncoming = new List<byte>();

        /// <summary>
        /// Requests a specific group of packets.
        /// </summary>
        /// <param name="packetCode">The ID of the packet group to request.</param>
        public void RequestSensorData(byte packetCode)
        {
            byte[] getDataCmd = new byte[] { 142, packetCode };
            sendCommand(getDataCmd);

        }

        private void ClearRequest()
        {
            if (serialPort.IsOpen)
            {
                lock (thisLock)
                {
                    _packetsExpected = 0;
                    _packetsIncoming.Clear();
                    serialPort.ReadExisting();
                }
            }
        }

        private void ProcessMessage()
        {
            // If we received the complete message: process it
            lock (thisLock)
            {
                if (_packetsIncoming.Count == _packetsExpected)
                {
                    BatteryLevel = (_packetsIncoming[6] * 256) + _packetsIncoming[7];
                    BatteryCapacity = (_packetsIncoming[8] * 256) + _packetsIncoming[9];
                    Temperature = _packetsIncoming[5];
                    _packetsIncoming.Clear();
                    _packetsExpected = 0;
                }
            }
        }

        private void Timer_Tick(object sender)
        {
            // See if we have to timeout the awaited packet
            if (DateTime.Now - _packetRequestTime > _packetRequestTimeout)
            {
                ClearRequest();
            }
            // If there currently are no expected packets request the next bunch
            bool requestPending = false;
            lock (thisLock)
            {
                if (_packetsExpected == 0)
                {
                    _packetsExpected = 10;
                    _packetRequestTime = DateTime.Now;
                    requestPending = true;
                }
            }
            // Submit the request
            if (requestPending)
                RequestSensorData(3);
        }

        public void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (serialPort.IsOpen)
            {
                if (e.EventType != System.IO.Ports.SerialData.Chars) return;
                try
                {
                    // Read all available data
                    byte[] buffer = new byte[serialPort.BytesToRead];
                    serialPort.Read(buffer, 0, serialPort.BytesToRead);
                    lock (thisLock)
                    {
                        _packetsIncoming.AddRange(buffer);
                    }

                    // Process message
                    ProcessMessage();
                }
                catch (Exception ex)
                {
                    // Clear the request
                    ClearRequest();
                    // Log the exception
                    Log("Exception occurred while reading data from robot: " + ex.Message);
                    Log("Stacktrace:");
                    Log(ex.StackTrace);
                    Log("InnerException: ");
                    if (ex.InnerException != null)
                    {
                        Log(ex.InnerException.Message);
                        Log("Stacktrace (inner):");
                        Log(ex.InnerException.StackTrace);
                    }
                    else
                    {
                        Log("None");
                    }
                }
            }
        }
        private void sendCommand(byte[] cmd)
        {
            lock (thisLock)
            {
                if (serialPort.IsOpen)
                {
                    if (logCommands)
                    {
                        StringBuilder sb = new StringBuilder("Write: ");
                        foreach (byte b in cmd)
                        {
                            sb.Append(b);
                            sb.Append(" ");
                        }
                        Log(sb.ToString());
                    }

                    //Prepare();
                    serialPort.Write(cmd, 0, cmd.Length);
                    //serialPort.BaseStream.Flush();
                }
                else
                {
                    Log("Serial Port is closed, couldn't send Command");
                }
            }
        }
        #endregion
    }
}
