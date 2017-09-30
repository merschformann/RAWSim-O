using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.CommFramework
{
    /// <summary>
    /// A client that can be used to connect a controlling mechanism to the communication server.
    /// </summary>
    public class ControlClient
    {
        public ControlClient(
            Action<int, int, double> robotPositionUpdateCallback,
            Action<int, bool> robotPickupFinishedCallback,
            Action<int, bool> robotSetdownFinishedCallback,
            Action<string> logger,
            Action<bool> connectionStatusCallback)
        {
            _robotPositionUpdateHandle = robotPositionUpdateCallback;
            _robotPickupFinishedHandle = robotPickupFinishedCallback;
            _robotSetdownFinishedHandle = robotSetdownFinishedCallback;
            _logger = logger;
            _client = new CommunicationClient(ClientType.C, 0, HandleCommunication, logger, connectionStatusCallback);
        }

        /// <summary>
        /// The client handling the network communication.
        /// </summary>
        CommunicationClient _client;
        /// <summary>
        /// A callback that gets all new position information of the robots passed.
        /// </summary>
        Action<int, int, double> _robotPositionUpdateHandle;
        /// <summary>
        /// A callback that gets finished pickup operations passed.
        /// </summary>
        Action<int, bool> _robotPickupFinishedHandle;
        /// <summary>
        /// A callback that gets finished setdown operations passed.
        /// </summary>
        Action<int, bool> _robotSetdownFinishedHandle;
        /// <summary>
        /// The function that is used for logging.
        /// </summary>
        Action<string> _logger;
        /// <summary>
        /// Initiates the connection.
        /// </summary>
        /// <param name="ip">The ip of the server.</param>
        /// <param name="port">The port the server uses.</param>
        public void Connect(string ip, string port) { _client.Connect(ip, port); }
        /// <summary>
        /// Terminates the connection.
        /// </summary>
        public void Disconnect() { _client.SendMsg(CommunicationConstants.COMM_DISCONNECT_MSG); }

        /// <summary>
        /// Handles incoming network messages.
        /// </summary>
        /// <param name="msg">The new message that was received.</param>
        public void HandleCommunication(string msg)
        {
            if (ControlTranslator.CheckMessageValidnessTo(msg))
            {
                ControlMessageResultServer result = ControlTranslator.DecodeTo(msg);
                switch (result.Type)
                {
                    case ControlMessageTypesServer.Pos: _robotPositionUpdateHandle(result.RobotID, result.Waypoint, result.Orientation); break;
                    case ControlMessageTypesServer.Pickup: _robotPickupFinishedHandle(result.RobotID, result.PickupSuccess); break;
                    case ControlMessageTypesServer.Setdown: _robotSetdownFinishedHandle(result.RobotID, result.SetdownSuccess); break;
                    default: throw new ArgumentException("Unknown message type: " + result.Type);
                }
            }
        }
        /// <summary>
        /// Sends a new path command to the server to be executed by a specific robot.
        /// </summary>
        /// <param name="robotID">The robot that shall execute the path.</param>
        /// <param name="waitTime">The time the robot shall wait before executing the path.</param>
        /// <param name="path">The valid path the robot shall execute (starting with the current position of the robot).</param>
        public void SubmitNewPath(int robotID, double waitTime, List<int> path) { _client.SendMsg(ControlTranslator.EncodeFrom(robotID, waitTime, path)); }
        /// <summary>
        /// Sends a pickup command to the given robot.
        /// </summary>
        /// <param name="robotID">The robot that shall pickup a pod.</param>
        public void SubmitPickupCommand(int robotID) { _client.SendMsg(ControlTranslator.EncodeFromPickup(robotID)); }
        /// <summary>
        /// Sends a setdown command to the given robot.
        /// </summary>
        /// <param name="robotID">The robot that shall setdown a pod.</param>
        public void SubmitSetdownCommand(int robotID) { _client.SendMsg(ControlTranslator.EncodeFromSetdown(robotID)); }
        /// <summary>
        /// Sends a rest command to the given robot.
        /// </summary>
        /// <param name="robotID">The robot that shall rest.</param>
        public void SubmitRestCommand(int robotID) { _client.SendMsg(ControlTranslator.EncodeFromRest(robotID)); }
        /// <summary>
        /// Sends a getitem notification to the given robot.
        /// </summary>
        /// <param name="robotID">The robot that shall be notified that its pod is used.</param>
        public void SubmitGetItemNotification(int robotID) { _client.SendMsg(ControlTranslator.EncodeFromGetItem(robotID)); }
        /// <summary>
        /// Sends a putitem notification to the given robot.
        /// </summary>
        /// <param name="robotID">The robot that shall be notified that its pod is used.</param>
        public void SubmitPutItemNotification(int robotID) { _client.SendMsg(ControlTranslator.EncodeFromPutItem(robotID)); }
    }
}
