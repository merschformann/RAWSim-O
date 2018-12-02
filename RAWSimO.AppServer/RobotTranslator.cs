using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.CommFramework
{
    /// <summary>
    /// All types of messages sent by a robot client.
    /// </summary>
    public enum RobotMessageTypesClient
    {
        /// <summary>
        /// Indicates that the message is erroneous.
        /// </summary>
        Error,
        /// <summary>
        /// A message to notify the server about the robot's current position as a waypoint identifier.
        /// </summary>
        Pos,
        /// <summary>
        /// A message to notify the server about the robot's current orientation. The message only consists of a <code>RobotOrientation</code> enumeration item.
        /// </summary>
        Orient,
        /// <summary>
        /// A message to notify the server about a finished pickup operation.
        /// </summary>
        Pickup,
        /// <summary>
        /// A message to notify the server about a finished setdown operation.
        /// </summary>
        Setdown,
    }
    /// <summary>
    /// The result of a message received from a robot. The properties are set depending on the message type.
    /// </summary>
    public class RobotMessageResultClient
    {
        /// <summary>
        /// The ideintifier for an orientation info message.
        /// </summary>
        public static readonly string IDENTIFIER_ORIENT = RobotMessageTypesClient.Orient.ToString();
        /// <summary>
        /// The identifier for a position info message.
        /// </summary>
        public static readonly string IDENTIFIER_POS = RobotMessageTypesClient.Pos.ToString();
        /// <summary>
        /// The identifier for a pickup message.
        /// </summary>
        public static readonly string IDENTIFIER_PICKUP = RobotMessageTypesClient.Pickup.ToString();
        /// <summary>
        /// The identifier for a setdown message.
        /// </summary>
        public static readonly string IDENTIFIER_SETDOWN = RobotMessageTypesClient.Setdown.ToString();
        /// <summary>
        /// The type of the message.
        /// </summary>
        public RobotMessageTypesClient Type { get; set; }
        /// <summary>
        /// Explains the possibly erroneous message.
        /// </summary>
        public string ErrorMessage { get; set; }
        /// <summary>
        /// The RFID-tag of a position message.
        /// </summary>
        public string WaypointTag { get; set; }
        /// <summary>
        /// The orientation of an orientation message.
        /// </summary>
        public RobotOrientation Orientation { get; set; }
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
    /// All types of messages sent to a robot client.
    /// </summary>
    public enum RobotMessageTypesServer
    {
        /// <summary>
        /// A message telling the robot to execute the specified move commands.
        /// </summary>
        Go,
        /// <summary>
        /// A message telling the robot to immediately stop all actions and rest where it is currently.
        /// </summary>
        Rest,
        /// <summary>
        /// A message telling the robot to pickup a pod at the current position.
        /// </summary>
        Pickup,
        /// <summary>
        /// A message telling the robot to setdown a pod at the current position.
        /// </summary>
        Setdown,
        /// <summary>
        /// A message telling the robot that its pod is currently used for insertion.
        /// </summary>
        GetItem,
        /// <summary>
        /// A message telling the robot that its pod is currently used for extraction.
        /// </summary>
        PutItem
    }
    /// <summary>
    /// The result of a message received by a robot. The properties are set depending on the message type.
    /// </summary>
    public class RobotMessageResultServer
    {
        /// <summary>
        /// The identifier for a go command message.
        /// </summary>
        public static readonly string IDENTIFIER_GO = RobotMessageTypesServer.Go.ToString();
        /// <summary>
        /// The identifier for a rest command message.
        /// </summary>
        public static readonly string IDENTIFIER_REST = RobotMessageTypesServer.Rest.ToString();
        /// <summary>
        /// The identifier for a pickup command message.
        /// </summary>
        public static readonly string IDENTIFIER_PICKUP = RobotMessageTypesServer.Pickup.ToString();
        /// <summary>
        /// The identifier for a setdown command message.
        /// </summary>
        public static readonly string IDENTIFIER_SETDOWN = RobotMessageTypesServer.Setdown.ToString();
        /// <summary>
        /// The identifier for a getitem notification message.
        /// </summary>
        public static readonly string IDENTIFIER_GETITEM = RobotMessageTypesServer.GetItem.ToString();
        /// <summary>
        /// The identifier for a putitem notification message.
        /// </summary>
        public static readonly string IDENTIFIER_PUTITEM = RobotMessageTypesServer.PutItem.ToString();
        /// <summary>
        /// The type of the message.
        /// </summary>
        public RobotMessageTypesServer Type { get; set; }
        /// <summary>
        /// An enumeration of actions to execute in the given order if the message was a 'go' command.
        /// </summary>
        public IEnumerable<RobotActions> Actions { get; set; }
        /// <summary>
        /// The time that the robot shall wait before executing the commands.
        /// </summary>
        public double WaitTime { get; set; }
    }
    /// <summary>
    /// The basic actions the robot can perform.
    /// </summary>
    public enum RobotActions
    {
        /// <summary>
        /// Indicates that the command can be skipped.
        /// </summary>
        None,
        /// <summary>
        /// Commands the robot to go straight to the next waypoint.
        /// </summary>
        Forward,
        /// <summary>
        /// Commands the robot to go straight backwards to the next waypoint.
        /// </summary>
        Backward,
        /// <summary>
        /// Commands the robot to turn left by 90°.
        /// </summary>
        TurnLeft,
        /// <summary>
        /// Commands the robot to turn right by 90°.
        /// </summary>
        TurnRight,
        /// <summary>
        /// Commands the robot to pickup a pod.
        /// </summary>
        Pickup,
        /// <summary>
        /// Commands the robot to setdown a pod.
        /// </summary>
        Setdown,
        /// <summary>
        /// Notifies the robot that its pod is currently used for insertion.
        /// </summary>
        GetItem,
        /// <summary>
        /// Notifies the robot that its pod is currently used for extraction.
        /// </summary>
        PutItem,
        /// <summary>
        /// Indicates that the robot is entering standby mode.
        /// </summary>
        Rest,
    }
    /// <summary>
    /// Indicates the orientation the robot is facing.
    /// </summary>
    public enum RobotOrientation
    {
        /// <summary>
        /// Facing north (+x)
        /// </summary>
        North,
        /// <summary>
        /// Facing west (+y)
        /// </summary>
        West,
        /// <summary>
        /// Facing south (-x)
        /// </summary>
        South,
        /// <summary>
        /// Facing east (-y)
        /// </summary>
        East
    }
    /// <summary>
    /// Error codes the robot may signal to the listeners.
    /// </summary>
    public enum RobotErrorCodes
    {
        /// <summary>
        /// Indicates that the robot cannot execute the path, because it is invalid. (e.g.: not connected or contains an unknown waypoint)
        /// </summary>
        InvalidPath,
        /// <summary>
        /// Indicates that the robot's battery capacity is running low.
        /// </summary>
        LowBattery
    }

    public class RobotTranslator
    {
        #region Translation helpers

        /// <summary>
        /// A dictionary translating the simple actions into robot commands.
        /// </summary>
        private static readonly Dictionary<RobotActions, string> _actionTranslations = new Dictionary<RobotActions, string>()
        {
            { RobotActions.Forward, "gf" },
            { RobotActions.Backward, "gb" },
            { RobotActions.TurnLeft, "gl" },
            { RobotActions.TurnRight, "gr" },
            { RobotActions.Pickup, "Pickup" },
            { RobotActions.Setdown, "Setdown" },
            { RobotActions.GetItem, "GetItem" },
            { RobotActions.PutItem, "PutItem" },
        };
        /// <summary>
        /// A dictionary translating the robot commands into simple actions.
        /// </summary>
        private static readonly Dictionary<string, RobotActions> _commandTranslations = new Dictionary<string, RobotActions>()
        {
            { "gf", RobotActions.Forward },
            { "gb", RobotActions.Backward},
            { "gl", RobotActions.TurnLeft },
            { "gr", RobotActions.TurnRight },
            { "Pickup", RobotActions.Pickup },
            { "Setdown", RobotActions.Setdown },
            { "GetItem", RobotActions.GetItem },
            { "PutItem", RobotActions.PutItem },
        };
        /// <summary>
        /// Translates the action to a suitable command.
        /// </summary>
        /// <param name="action">The action to translate.</param>
        /// <returns>The string representation of the action.</returns>
        public static string TranslateAction(RobotActions action)
        {
            if (_actionTranslations.ContainsKey(action))
                return _actionTranslations[action];
            else
                throw new ArgumentException("Unknown action: " + action.ToString());
        }
        /// <summary>
        /// Translates the command into a valid action.
        /// </summary>
        /// <param name="cmd">The command to translate.</param>
        /// <returns>The action identified by the command.</returns>
        public static RobotActions TranslateAction(string cmd)
        {
            if (_commandTranslations.ContainsKey(cmd))
                return _commandTranslations[cmd];
            else
                return RobotActions.None;
        }
        /// <summary>
        /// Checks whether the command string seems to be valid.
        /// </summary>
        /// <returns><code>true</code> if the command seems to be valid, <code>false</code> otherwise.</returns>
        public static bool CheckMessageValidnessTo(string cmds)
        {
            return
                cmds.StartsWith(RobotMessageResultServer.IDENTIFIER_GO) ||
                cmds.StartsWith(RobotMessageResultServer.IDENTIFIER_REST) ||
                cmds.StartsWith(RobotMessageResultServer.IDENTIFIER_PICKUP) ||
                cmds.StartsWith(RobotMessageResultServer.IDENTIFIER_SETDOWN) ||
                cmds.StartsWith(RobotMessageResultServer.IDENTIFIER_GETITEM) ||
                cmds.StartsWith(RobotMessageResultServer.IDENTIFIER_PUTITEM);
        }
        /// <summary>
        /// Checks whether the message string seems to be valid.
        /// </summary>
        /// <returns><code>true</code> if the message seems to be valid, <code>false</code> otherwise.</returns>
        public static bool CheckMessageValidnessFrom(string msg)
        {
            return
                msg.StartsWith(RobotMessageResultClient.IDENTIFIER_POS) ||
                msg.StartsWith(RobotMessageResultClient.IDENTIFIER_ORIENT) ||
                msg.StartsWith(RobotMessageResultClient.IDENTIFIER_PICKUP) ||
                msg.StartsWith(RobotMessageResultClient.IDENTIFIER_SETDOWN);
        }

        #endregion

        #region Translators

        /// <summary>
        /// Translates a list of commands to a transmittable command string.
        /// </summary>
        /// <param name="waitTime">The time to wait before executing the actions.</param>
        /// <param name="actions">The list of commands / actions to be executed.</param>
        /// <returns>A transmittable representation of the actions to execute.</returns>
        public static string EncodeTo(double waitTime, IEnumerable<RobotActions> actions)
        {
            return
                RobotMessageResultServer.IDENTIFIER_GO +
                CommunicationConstants.MSG_SUB_DELIMITER +
                waitTime.ToString("F2", CommunicationConstants.FORMATTER) +
                CommunicationConstants.MSG_SUB_DELIMITER +
                string.Join(CommunicationConstants.MSG_SUB_DELIMITER.ToString(), actions.Select(a => TranslateAction(a)));
        }
        /// <summary>
        /// Translates a rest command to a transmittable string.
        /// </summary>
        /// <returns>The transmittable string telling the robot to stop.</returns>
        public static string EncodeToRest() { return RobotMessageResultServer.IDENTIFIER_REST; }
        /// <summary>
        /// Translates a pickup command to a transmittable string.
        /// </summary>
        /// <returns>The transmittable string.</returns>
        public static string EncodeToPickup() { return RobotMessageResultServer.IDENTIFIER_PICKUP; }
        /// <summary>
        /// Translates a setdown command to a transmittable string.
        /// </summary>
        /// <returns>The transmittable string.</returns>
        public static string EncodeToSetdown() { return RobotMessageResultServer.IDENTIFIER_SETDOWN; }
        /// <summary>
        /// Translates a getitem command to a transmittable string.
        /// </summary>
        /// <returns>The transmittable string.</returns>
        public static string EncodeToGetItem() { return RobotMessageResultServer.IDENTIFIER_GETITEM; }
        /// <summary>
        /// Translates a putitem command to a transmittable string.
        /// </summary>
        /// <returns>The transmittable string.</returns>
        public static string EncodeToPutItem() { return RobotMessageResultServer.IDENTIFIER_PUTITEM; }
        /// <summary>
        /// Translates a transmittable representation of actions to execute to a robot-understandable format.
        /// </summary>
        /// <param name="cmds">The transmittable message to translate.</param>
        /// <returns>The result of the message.</returns>
        public static RobotMessageResultServer DecodeTo(string cmds)
        {
            // TODO more robust checking for validness - use error type to mark messages not possible to parse
            string[] parts = cmds.Split(CommunicationConstants.MSG_SUB_DELIMITER);
            RobotMessageTypesServer type = (RobotMessageTypesServer)Enum.Parse(typeof(RobotMessageTypesServer), parts[0]);
            switch (type)
            {
                case RobotMessageTypesServer.Go:
                    return new RobotMessageResultServer()
                    {
                        Type = type,
                        WaitTime = double.Parse(parts[1], CommunicationConstants.FORMATTER),
                        Actions = parts.Skip(2).Select(a => TranslateAction(a))
                    };
                case RobotMessageTypesServer.Rest:
                case RobotMessageTypesServer.Pickup:
                case RobotMessageTypesServer.Setdown:
                case RobotMessageTypesServer.GetItem:
                case RobotMessageTypesServer.PutItem:
                    return new RobotMessageResultServer()
                    {
                        Type = type
                    };
                default: throw new FormatException("Invalid command message: " + cmds);
            }
        }
        /// <summary>
        /// Translates an orientation information into a transmittable <code>string</code>.
        /// </summary>
        /// <param name="orientation">The orientation to transmit.</param>
        /// <returns>The <code>string</code> representation of the message.</returns>
        public static string EncodeFrom(RobotOrientation orientation)
        {
            return
                RobotMessageResultClient.IDENTIFIER_ORIENT +
                CommunicationConstants.MSG_SUB_DELIMITER +
                orientation.ToString();
        }
        /// <summary>
        /// Translates a position information into a transmittable <code>string</code>.
        /// </summary>
        /// <param name="rfidTag">The RFID-tag of the waypoint-position to transmit.</param>
        /// <returns>The <code>string</code> representation of the message.</returns>
        public static string EncodeFrom(string rfidTag)
        {
            return
                RobotMessageResultClient.IDENTIFIER_POS +
                CommunicationConstants.MSG_SUB_DELIMITER +
                rfidTag;
        }
        /// <summary>
        /// Translates a notification about a finished pickup operation.
        /// </summary>
        /// <param name="success">Indicates whether the operation was successful.</param>
        /// <returns>The transmittable string.</returns>
        public static string EncodeFromPickupFinished(bool success)
        {
            return
                RobotMessageResultClient.IDENTIFIER_PICKUP +
                CommunicationConstants.MSG_SUB_DELIMITER +
                success.ToString();
        }
        /// <summary>
        /// Translates a notification about a finished setdown operation.
        /// </summary>
        /// <param name="success">Indicates whether the operation was successful.</param>
        /// <returns>The transmittable string.</returns>
        public static string EncodeFromSetdownFinished(bool success)
        {
            return
                RobotMessageResultClient.IDENTIFIER_SETDOWN +
                CommunicationConstants.MSG_SUB_DELIMITER +
                success.ToString();
        }
        /// <summary>
        /// Decodes a message received from the robot.
        /// </summary>
        /// <param name="msg">The message that was received.</param>
        /// <returns>The result of the decoding.</returns>
        public static RobotMessageResultClient DecodeFrom(string msg)
        {
            // TODO more robust checking for validness - use error type to mark messages not possible to parse
            string[] parts = msg.Split(CommunicationConstants.MSG_SUB_DELIMITER);
            RobotMessageTypesClient type = (RobotMessageTypesClient)Enum.Parse(typeof(RobotMessageTypesClient), parts[0]);
            switch (type)
            {
                case RobotMessageTypesClient.Pos:
                    return new RobotMessageResultClient()
                    {
                        Type = type,
                        WaypointTag = parts[1]
                    };
                case RobotMessageTypesClient.Orient:
                    return new RobotMessageResultClient()
                    {
                        Type = type,
                        Orientation = (RobotOrientation)Enum.Parse(typeof(RobotOrientation), parts[1])
                    };
                case RobotMessageTypesClient.Pickup:
                    return new RobotMessageResultClient()
                    {
                        Type = type,
                        PickupSuccess = bool.Parse(parts[1])
                    };
                case RobotMessageTypesClient.Setdown:
                    return new RobotMessageResultClient()
                    {
                        Type = type,
                        SetdownSuccess = bool.Parse(parts[1])
                    };
                default: throw new FormatException("Invalid message: " + msg);
            }
        }

        #endregion
    }
}
