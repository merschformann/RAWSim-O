using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.CommFramework
{
    /// <summary>
    /// All types of messages sent by a control client.
    /// </summary>
    public enum ControlMessageTypesClient
    {
        /// <summary>
        /// Indicates a path that a robot shall execute.
        /// </summary>
        Path,
        /// <summary>
        /// Indicates that a robot shall pickup a pod at the current position.
        /// </summary>
        Pickup,
        /// <summary>
        /// Indicates that a robot shall setdown a pod at the current position.
        /// </summary>
        Setdown,
        /// <summary>
        /// Indicates that the robot is used to store an item on the pod it is carrying.
        /// </summary>
        GetItem,
        /// <summary>
        /// Indicates that the robot is used to extract an item from the pod it is carrying.
        /// </summary>
        PutItem,
        /// <summary>
        /// Indicates that a robot shall stop all actions.
        /// </summary>
        Rest,
    }
    /// <summary>
    /// The result of a message received from the control. The properties are set depending on the message type.
    /// </summary>
    public class ControlMessageResultClient
    {
        /// <summary>
        /// The identifier for a path message.
        /// </summary>
        public static readonly string IDENTIFIER_PATH = ControlMessageTypesClient.Path.ToString();
        /// <summary>
        /// The identifier for a pickup message.
        /// </summary>
        public static readonly string IDENTIFIER_PICKUP = ControlMessageTypesClient.Pickup.ToString();
        /// <summary>
        /// The identifier for a setdown message.
        /// </summary>
        public static readonly string IDENTIFIER_SETDOWN = ControlMessageTypesClient.Setdown.ToString();
        /// <summary>
        /// The identifier for a getitem message.
        /// </summary>
        public static readonly string IDENTIFIER_GETITEM = ControlMessageTypesClient.GetItem.ToString();
        /// <summary>
        /// The identifier for a putitem message.
        /// </summary>
        public static readonly string IDENTIFIER_PUTITEM = ControlMessageTypesClient.PutItem.ToString();
        /// <summary>
        /// The identifier for a rest message.
        /// </summary>
        public static readonly string IDENTIFIER_REST = ControlMessageTypesClient.Rest.ToString();
        /// <summary>
        /// The type of the message.
        /// </summary>
        public ControlMessageTypesClient Type { get; set; }
        /// <summary>
        /// A list of waypoints specifying the path.
        /// </summary>
        public List<int> Path { get; set; }
        /// <summary>
        /// Time that shall be waited before executing the path.
        /// </summary>
        public double WaitTime { get; set; }
        /// <summary>
        /// The robot this command is send to.
        /// </summary>
        public int RobotID { get; set; }
    }
    /// <summary>
    /// All types of messages sent to a control client.
    /// </summary>
    public enum ControlMessageTypesServer
    {
        /// <summary>
        /// A position info message including position and orientation.
        /// </summary>
        Pos,
        /// <summary>
        /// Indicates that a pickup was finished by a robot.
        /// </summary>
        Pickup,
        /// <summary>
        /// Indicates that a setdown was finished by a robot.
        /// </summary>
        Setdown,
    }
    /// <summary>
    /// The result of a message received by a control. The properties are set depending on the message type.
    /// </summary>
    public class ControlMessageResultServer
    {
        /// <summary>
        /// The identifier for a pos message.
        /// </summary>
        public static readonly string IDENTIFIER_POS = ControlMessageTypesServer.Pos.ToString();
        /// <summary>
        /// The identifier for a pickup message.
        /// </summary>
        public static readonly string IDENTIFIER_PICKUP = ControlMessageTypesServer.Pickup.ToString();
        /// <summary>
        /// The identifier for a setdown message.
        /// </summary>
        public static readonly string IDENTIFIER_SETDOWN = ControlMessageTypesServer.Setdown.ToString();
        /// <summary>
        /// The type of the message.
        /// </summary>
        public ControlMessageTypesServer Type { get; set; }
        /// <summary>
        /// The position information containing the waypoint-ID the robot is at.
        /// </summary>
        public int Waypoint { get; set; }
        /// <summary>
        /// The orientation of the robot in radians.
        /// </summary>
        public double Orientation { get; set; }
        /// <summary>
        /// The robot this message is about.
        /// </summary>
        public int RobotID { get; set; }
        /// <summary>
        /// Indicates whether the pickup operation was successful.
        /// </summary>
        public bool PickupSuccess { get; set; }
        /// <summary>
        /// Indicates whether the setdown operation was successful.
        /// </summary>
        public bool SetdownSuccess { get; set; }
    }

    /// <summary>
    /// Enables translations between a control client and a server.
    /// </summary>
    public class ControlTranslator
    {
        #region Translation helpers

        /// <summary>
        /// Checks the validness of an incoming message. (A message received from the server.)
        /// </summary>
        /// <param name="msg">The message to check.</param>
        /// <returns><code>true</code> if valid, <code>false</code> otherwise.</returns>
        public static bool CheckMessageValidnessTo(string msg)
        {
            return
                msg.StartsWith(ControlMessageResultServer.IDENTIFIER_POS) ||
                msg.StartsWith(ControlMessageResultServer.IDENTIFIER_PICKUP) ||
                msg.StartsWith(ControlMessageResultServer.IDENTIFIER_SETDOWN);
        }
        /// <summary>
        /// Checks the validness of an incoming message. (A message received from the control-client.)
        /// </summary>
        /// <param name="msg">The message to check.</param>
        /// <returns><code>true</code> if valid, <code>false</code> otherwise.</returns>
        public static bool CheckMessageValidnessFrom(string msg)
        {
            return
                msg.StartsWith(ControlMessageResultClient.IDENTIFIER_PATH) ||
                msg.StartsWith(ControlMessageResultClient.IDENTIFIER_PICKUP) ||
                msg.StartsWith(ControlMessageResultClient.IDENTIFIER_SETDOWN) ||
                msg.StartsWith(ControlMessageResultClient.IDENTIFIER_REST) ||
                msg.StartsWith(ControlMessageResultClient.IDENTIFIER_GETITEM) ||
                msg.StartsWith(ControlMessageResultClient.IDENTIFIER_PUTITEM);
        }

        #endregion

        #region Translators

        /// <summary>
        /// Encodes a position info message to a transmittable string.
        /// </summary>
        /// <param name="robotID">The robot the position info is about.</param>
        /// <param name="waypointID">The waypoint the robot is positioned at.</param>
        /// <param name="robotOrientation">The orientation the robot is currently in.</param>
        /// <returns>A transmittable string of the information.</returns>
        public static string EncodeTo(int robotID, int waypointID, RobotOrientation robotOrientation)
        {
            // Translate orientation to radians
            double orientation = 0.0;
            switch (robotOrientation)
            {
                case RobotOrientation.North: orientation = Math.PI * 1.5; break;
                case RobotOrientation.West: orientation = Math.PI; break;
                case RobotOrientation.South: orientation = Math.PI * 0.5; break;
                case RobotOrientation.East: orientation = 0; break;
                default: throw new ArgumentException("Unknown orientation type: " + robotOrientation);
            }
            // Return resulting string
            return
                ControlMessageResultServer.IDENTIFIER_POS +
                CommunicationConstants.MSG_SUB_DELIMITER +
                robotID.ToString() +
                CommunicationConstants.MSG_SUB_DELIMITER +
                waypointID.ToString() +
                CommunicationConstants.MSG_SUB_DELIMITER +
                orientation.ToString();
        }
        /// <summary>
        /// Encodes a pickup finished message.
        /// </summary>
        /// <param name="robotID">The robot signaling the finished pickup.</param>
        /// <param name="success">Indicates whether the operation was successful.</param>
        /// <returns>A transmittable string.</returns>
        public static string EncodeToPickup(int robotID, bool success)
        {
            return
                ControlMessageResultServer.IDENTIFIER_PICKUP +
                CommunicationConstants.MSG_SUB_DELIMITER +
                robotID.ToString() +
                CommunicationConstants.MSG_SUB_DELIMITER +
                success.ToString();
        }
        /// <summary>
        /// Encodes a setdown finished message.
        /// </summary>
        /// <param name="robotID">The robot signaling the finished setdown.</param>
        /// <param name="success">Indicates whether the operation was successful.</param>
        /// <returns>A transmittable string.</returns>
        public static string EncodeToSetdown(int robotID, bool success)
        {
            return
                ControlMessageResultServer.IDENTIFIER_SETDOWN +
                CommunicationConstants.MSG_SUB_DELIMITER +
                robotID.ToString() +
                CommunicationConstants.MSG_SUB_DELIMITER +
                success.ToString();
        }
        /// <summary>
        /// Decodes a transmittable string and returns the result of parsing the message.
        /// </summary>
        /// <param name="msg">The message to parse.</param>
        /// <returns>The result of parsing the message.</returns>
        public static ControlMessageResultServer DecodeTo(string msg)
        {
            string[] elements = msg.Split(CommunicationConstants.MSG_SUB_DELIMITER);
            ControlMessageTypesServer messageType = (ControlMessageTypesServer)Enum.Parse(typeof(ControlMessageTypesServer), elements.First());
            switch (messageType)
            {
                case ControlMessageTypesServer.Pos:
                    return new ControlMessageResultServer()
                    {
                        Type = messageType,
                        RobotID = int.Parse(elements[1]),
                        Waypoint = int.Parse(elements[2]),
                        Orientation = double.Parse(elements[3], CommunicationConstants.FORMATTER)
                    };
                case ControlMessageTypesServer.Pickup:
                    return new ControlMessageResultServer()
                    {
                        Type = messageType,
                        RobotID = int.Parse(elements[1]),
                        PickupSuccess = bool.Parse(elements[2])
                    };
                case ControlMessageTypesServer.Setdown:
                    return new ControlMessageResultServer()
                    {
                        Type = messageType,
                        RobotID = int.Parse(elements[1]),
                        SetdownSuccess = bool.Parse(elements[2])
                    };
                default: throw new ArgumentException("Unknown message type: " + messageType);
            }
        }
        /// <summary>
        /// Encodes a path command to a transmittable string.
        /// </summary>
        /// <param name="robotID">The robot that shall execute the path.</param>
        /// <param name="waitTime">The time the robot shall wait before executing the path.</param>
        /// <param name="path">The path to execute beginning with the current position of the robot.</param>
        /// <returns>A transmittable string with all the information.</returns>
        public static string EncodeFrom(int robotID, double waitTime, IEnumerable<int> path)
        {
            return
                ControlMessageResultClient.IDENTIFIER_PATH +
                CommunicationConstants.MSG_SUB_DELIMITER +
                robotID.ToString() +
                CommunicationConstants.MSG_SUB_DELIMITER +
                waitTime.ToString("F2", CommunicationConstants.FORMATTER) +
                CommunicationConstants.MSG_SUB_DELIMITER +
                string.Join(CommunicationConstants.MSG_SUB_DELIMITER.ToString(), path);
        }
        /// <summary>
        /// Encodes a pickup command.
        /// </summary>
        /// <param name="robotID">The robot that shall execute the pickup.</param>
        /// <returns>The transmittable string of the command.</returns>
        public static string EncodeFromPickup(int robotID)
        {
            return ControlMessageResultClient.IDENTIFIER_PICKUP +
                CommunicationConstants.MSG_SUB_DELIMITER +
                robotID.ToString();
        }
        /// <summary>
        /// Encodes a setdown command.
        /// </summary>
        /// <param name="robotID">The robot that shall execute the setdown.</param>
        /// <returns>The transmittable string of the command.</returns>
        public static string EncodeFromSetdown(int robotID)
        {
            return ControlMessageResultClient.IDENTIFIER_SETDOWN +
                CommunicationConstants.MSG_SUB_DELIMITER +
                robotID.ToString();
        }
        /// <summary>
        /// Encodes a rest command.
        /// </summary>
        /// <param name="robotID">The robot that shall rest.</param>
        /// <returns>The transmittable string of the command.</returns>
        public static string EncodeFromRest(int robotID)
        {
            return
                ControlMessageResultClient.IDENTIFIER_REST +
                CommunicationConstants.MSG_SUB_DELIMITER +
                robotID.ToString();
        }
        /// <summary>
        /// Encodes a getitem command.
        /// </summary>
        /// <param name="robotID">The robot that shall be notified that its pod is used.</param>
        /// <returns>The transmittable string of the command.</returns>
        public static string EncodeFromGetItem(int robotID)
        {
            return
                ControlMessageResultClient.IDENTIFIER_GETITEM +
                CommunicationConstants.MSG_SUB_DELIMITER +
                robotID.ToString();
        }
        /// <summary>
        /// Encodes a putitem command.
        /// </summary>
        /// <param name="robotID">The robot that shall be notified that its pod is used.</param>
        /// <returns>The transmittable string of the command.</returns>
        public static string EncodeFromPutItem(int robotID)
        {
            return
                ControlMessageResultClient.IDENTIFIER_PUTITEM +
                CommunicationConstants.MSG_SUB_DELIMITER +
                robotID.ToString();
        }
        /// <summary>
        /// Decodes a transmittable string and returns the result of parsing the message.
        /// </summary>
        /// <param name="msg">The message to parse.</param>
        /// <returns>The result of parsing the message.</returns>
        public static ControlMessageResultClient DecodeFrom(string msg)
        {
            string[] elements = msg.Split(CommunicationConstants.MSG_SUB_DELIMITER);
            ControlMessageTypesClient messageType = (ControlMessageTypesClient)Enum.Parse(typeof(ControlMessageTypesClient), elements.First());
            switch (messageType)
            {
                case ControlMessageTypesClient.Path:
                    return new ControlMessageResultClient()
                    {
                        Type = messageType,
                        RobotID = int.Parse(elements[1]),
                        WaitTime = double.Parse(elements[2], CommunicationConstants.FORMATTER),
                        Path = elements.Skip(3).Select(e => int.Parse(e)).ToList()
                    };
                case ControlMessageTypesClient.Pickup:
                case ControlMessageTypesClient.Setdown:
                case ControlMessageTypesClient.Rest:
                case ControlMessageTypesClient.GetItem:
                case ControlMessageTypesClient.PutItem:
                    return new ControlMessageResultClient()
                    {
                        Type = messageType,
                        RobotID = int.Parse(elements[1])
                    };
                default: throw new ArgumentException("Unknown message type: " + messageType);
            }
        }

        #endregion
    }
}
