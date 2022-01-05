using RAWSimO.Core.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RAWSimO.CommFramework
{
    public class CommunicationServer
    {
        public CommunicationServer(
            int portNumber,
            Action<string> logger,
            Action<ClientType, int, string> clientConnected,
            Action<ClientType, int> clientDisconnected,
            Action<int, int, RobotOrientation> locationUpdater,
            Action<bool> errorModeChangedCallback)
        {
            _serverPort = portNumber;
            _logger = logger;
            _clientAdded = clientConnected;
            _clientRemoved = clientDisconnected;
            _locationUpdater = locationUpdater;
            _errorModeSetter = errorModeChangedCallback;
        }

        #region TCP Server

        /// <summary>
        /// Indicates whether the server is running.
        /// </summary>
        private bool _isRunning = false;
        /// <summary>
        /// Indicates whether the server is running.
        /// </summary>
        public bool IsRunning { get { return _isRunning; } }
        /// <summary>
        /// A function used to output information as lines of text.
        /// </summary>
        private Action<string> _logger;
        /// <summary>
        /// A method that is called everytime a new client is connected.
        /// </summary>
        private Action<ClientType, int, string> _clientAdded;
        /// <summary>
        /// A method that is called everytime a client is disconnected.
        /// </summary>
        private Action<ClientType, int> _clientRemoved;
        /// <summary>
        /// Action that is called when the location and orientation of a robot is updated. (only for visual sake)
        /// </summary>
        private Action<int, int, RobotOrientation> _locationUpdater;
        /// <summary>
        /// Changes visual feedback about the current mode.
        /// </summary>
        private Action<bool> _errorModeSetter;
        /// <summary>
        /// The port to use for the server.
        /// </summary>
        private int _serverPort;
        /// <summary>
        /// The thread the server runs in.
        /// </summary>
        public Thread _tcpServerThread;
        /// <summary>
        /// A set of all connected clients
        /// </summary>
        private HashSet<Socket> _connectedClients = new HashSet<Socket>();
        /// <summary>
        /// All connected bot-clients by the ID of the robot.
        /// </summary>
        private Dictionary<int, Socket> _botClients = new Dictionary<int, Socket>();
        /// <summary>
        /// All connected input-station-clients by the ID of the station.
        /// </summary>
        private Dictionary<int, Socket> _iStationClients = new Dictionary<int, Socket>();
        /// <summary>
        /// All connected output-station-clients by the ID of the station.
        /// </summary>
        private Dictionary<int, Socket> _oStationClients = new Dictionary<int, Socket>();
        /// <summary>
        /// All connected control-clients by their ID.
        /// </summary>
        private Dictionary<int, Socket> _controlClients = new Dictionary<int, Socket>();

        #region Helpers

        private string GetPaddedString(ClientType clientType, int clientID)
        {
            return (clientType.ToString() + clientID.ToString());
        }

        #endregion

        #region Communication handling

        private void HandleMetaMessage(Socket socket, string receivedMessage, out bool terminated)
        {
            // Split the message into its main parts
            string[] words = receivedMessage.Split(CommunicationConstants.MSG_MAIN_DELIMITER);
            ClientType clientType; int clientID; string message; terminated = false;
            string ip = socket.RemoteEndPoint.ToString();
            if (words.Length == 3)
            {
                // Parse meta-info
                bool successType; bool successID;
                successType = Enum.TryParse<ClientType>(words[0], out clientType);
                successID = int.TryParse(words[1], out clientID);
                message = words[2];
                // Handle the message if successful
                if (successType && successID)
                {
                    // See whether the client wants to initiate a new connection
                    if (message.Equals(CommunicationConstants.COMM_INITIATION_MSG))
                    {
                        // Check if a client is reconnecting
                        bool replaced = false;
                        switch (clientType)
                        {
                            case ClientType.R: if (_botClients.ContainsKey(clientID)) { replaced = true; RemoveDeadClient(clientType, clientID, _botClients[clientID]); } break;
                            case ClientType.I: if (_iStationClients.ContainsKey(clientID)) { replaced = true; RemoveDeadClient(clientType, clientID, _iStationClients[clientID]); } break;
                            case ClientType.O: if (_oStationClients.ContainsKey(clientID)) { replaced = true; RemoveDeadClient(clientType, clientID, _oStationClients[clientID]); } break;
                            case ClientType.C: if (_controlClients.ContainsKey(clientID)) { replaced = true; RemoveDeadClient(clientType, clientID, _controlClients[clientID]); } break;
                            default: break;
                        }
                        // Initiate the communication
                        _connectedClients.Add(socket);
                        _clientAdded(clientType, clientID, ip);
                        // Add meta-info
                        switch (clientType)
                        {
                            case ClientType.R:
                                {
                                    _botClients[clientID] = socket;
                                    _orientations[clientID] = RobotOrientation.North;
                                    _positions[clientID] = null;
                                    _lastPositions[clientID] = null;
                                }
                                break;
                            case ClientType.I: { _iStationClients[clientID] = socket; } break;
                            case ClientType.O: { _oStationClients[clientID] = socket; } break;
                            case ClientType.C: { _controlClients[clientID] = socket; } break;
                            default: throw new ArgumentException("Unknown client type: " + clientType);
                        }
                        // Log
                        _logger(GetPaddedString(clientType, clientID) + "::" + (replaced ? "re-establishing connection" : "initiating communication") + "::" + DateTime.Now.ToString("G"));
                    }
                    // See whether the client wants to terminate the communication
                    if (message.Equals(CommunicationConstants.COMM_DISCONNECT_MSG))
                    {
                        // Shutdown communication with the client
                        SendMsg(clientType, clientID, CommunicationConstants.COMM_DISCONNECT_MSG);
                        // Disconnect client and remove client from lists
                        RemoveDeadClient(clientType, clientID, socket);
                        terminated = true;
                        // Log
                        _logger(GetPaddedString(clientType, clientID) + "::terminating communication::" + DateTime.Now.ToString("G"));
                    }
                    // Handle normal communication
                    if (!message.Equals(CommunicationConstants.COMM_INITIATION_MSG) && !message.Equals(CommunicationConstants.COMM_DISCONNECT_MSG))
                    {
                        switch (clientType)
                        {
                            case ClientType.R: HandleRobotComm(clientID, message); break;
                            case ClientType.I: HandleInputStationComm(clientID, message); break;
                            case ClientType.O: HandleOutputStationComm(clientID, message); break;
                            case ClientType.C: HandleControlComm(clientID, message); break;
                            default: throw new ArgumentException("Unknown client type: " + clientType);
                        }
                    }
                }
                else
                {
                    // Show the message
                    _logger(ip + "::<<<<::" + receivedMessage + "::" + DateTime.Now.ToString("G"));
                }
            }
        }

        /// <summary>
        /// Handles incoming communication with a robot.
        /// </summary>
        /// <param name="robotID">The ID of the robot.</param>
        /// <param name="message">The message received from the robot.</param>
        private void HandleRobotComm(int robotID, string message)
        {
            // Check message validness
            if (RobotTranslator.CheckMessageValidnessFrom(message))
            {
                // Translate the message
                RobotMessageResultClient messageResult = RobotTranslator.DecodeFrom(message);
                // Show the message (translate tag to ID if possible)
                if (messageResult.Type == RobotMessageTypesClient.Pos && _waypointManager != null && _waypointManager.IsKnownTag(messageResult.WaypointTag))
                    _logger(
                        GetPaddedString(ClientType.R, robotID) + "::<<<<::" +
                        message.Replace(messageResult.WaypointTag, _waypointManager.Translate(_waypointManager.Translate(messageResult.WaypointTag)).ID.ToString()) + "::" +
                        DateTime.Now.ToString("G"));
                else
                    _logger(
                        GetPaddedString(ClientType.R, robotID) + "::<<<<::" +
                        message + "::" +
                        DateTime.Now.ToString("G"));

                // Init robot error handling mode if not set yet
                if (!_botErrorCorrectionMode.ContainsKey(robotID))
                    _botErrorCorrectionMode[robotID] = false;

                // Split the submessage
                switch (messageResult.Type)
                {
                    case RobotMessageTypesClient.Pos:
                        {
                            if (_waypointManager != null && _waypointManager.IsKnownTag(messageResult.WaypointTag))
                            {
                                // --> Keep track of previous position
                                if (_lastPositions[robotID] != _positions[robotID])
                                    _lastPositions[robotID] = _positions[robotID];
                                // Translate new position
                                _positions[robotID] = _waypointManager.Translate(_waypointManager.Translate(messageResult.WaypointTag));

                                // --> Keep track of orientation
                                if (_lastPositions[robotID] != null && _positions[robotID] != null)
                                {
                                    if (_waypointManager.IsConnected(_lastPositions[robotID], _positions[robotID]))
                                    {
                                        // Get orientation we have to turn towards
                                        double xDelta = _positions[robotID].X - _lastPositions[robotID].X;
                                        double yDelta = _positions[robotID].Y - _lastPositions[robotID].Y;
                                        if (Math.Abs(xDelta) > Math.Abs(yDelta))
                                            if (xDelta > 0) _orientations[robotID] = RobotOrientation.East;
                                            else _orientations[robotID] = RobotOrientation.West;
                                        else
                                            if (yDelta > 0) _orientations[robotID] = RobotOrientation.North;
                                        else _orientations[robotID] = RobotOrientation.South;
                                    }
                                    else
                                    {
                                        if (_lastPositions[robotID] != _positions[robotID])
                                            _logger("Alert!:robot " + robotID + " lost its orientation");
                                    }
                                }

                                // --> Error handling
                                // See whether the robot is off his path
                                if (_lastPaths.ContainsKey(robotID) && // See whether the robot has a path currently
                                    (_botErrorCorrectionMode.ContainsKey(robotID) ? !_botErrorCorrectionMode[robotID] : true) && // See whether the robot is already in resolve mode
                                    !_lastPaths[robotID].Contains(_positions[robotID].ID)) // See whether the new position is on the robots path
                                {
                                    // We have a problem - simply abort the current actions and try to drive back to the beginning of the path
                                    List<int> resolvingPath = _waypointManager.PlanPath(_positions[robotID], _waypointManager.Translate(_lastPaths[robotID][0])).Route.Select(s => s.ID).ToList();
                                    _resolvingPaths[robotID] = resolvingPath;
                                    _logger("Alert!:robot " + robotID + " went the wrong way - trying to get it back on track with resolving path " + string.Join(",", resolvingPath));
                                    _botErrorCorrectionMode[robotID] = true;
                                    _errorModeSetter(true);
                                    // Abort actions and submit resolving path
                                    SendMsg(ClientType.R, robotID, ConvertToRobCmd(robotID, 0, resolvingPath));
                                }
                                // Continue handling the error until it is fixed
                                if (_botErrorCorrectionMode[robotID])
                                {
                                    if (_lastPaths[robotID][0] == _positions[robotID].ID)
                                    {
                                        // We hit the right waypoint - now resubmit the original path and quit resolve mode
                                        _logger("Alert!:robot " + robotID + " successfully arrived at starting waypoint of original path - trying path again");
                                        _botErrorCorrectionMode[robotID] = false;
                                        _errorModeSetter(false);
                                        SendMsg(ClientType.R, robotID, ConvertToRobCmd(robotID, 0, _lastPaths[robotID]));
                                    }
                                    else
                                    {
                                        // See whether the robot is on its resolving path
                                        if (!_resolvingPaths[robotID].Contains(_positions[robotID].ID))
                                        {
                                            // Continue driving back until we hit the right waypoint
                                            List<int> resolvingPath = _waypointManager.PlanPath(_positions[robotID], _waypointManager.Translate(_lastPaths[robotID][0])).Route.Select(s => s.ID).ToList();
                                            _resolvingPaths[robotID] = resolvingPath;
                                            _logger("Alert!:robot " + robotID + " again went the wrong way - trying to get it back on track by applying path " + string.Join(",", resolvingPath));
                                            // Abort actions and submit new resolving path
                                            SendMsg(ClientType.R, robotID, ConvertToRobCmd(robotID, 0, resolvingPath));
                                        }
                                    }
                                }

                                // --> Notify all listening controllers (if not currently trying to fix an error)
                                if (!_botErrorCorrectionMode[robotID])
                                    foreach (var controllerID in _controlClients.Keys)
                                        SendMsg(ClientType.C, controllerID, ControlTranslator.EncodeTo(robotID, _positions[robotID].ID, _orientations[robotID]));
                                // --> Notify the view
                                _locationUpdater(robotID, _positions[robotID].ID, _orientations[robotID]);
                            }
                            else
                                _logger("Alert!:robot " + robotID + " sent an unknown RFID-tag: " + messageResult.WaypointTag + " load correct Waypoints!");
                        }
                        break;
                    case RobotMessageTypesClient.Orient:
                        {
                            // Store new orientation information
                            _orientations[robotID] = messageResult.Orientation;
                            // Notify all listening controllers
                            foreach (var controllerID in _controlClients.Keys)
                                SendMsg(ClientType.C, controllerID, ControlTranslator.EncodeTo(robotID, _positions[robotID].ID, _orientations[robotID]));
                            // Notify the view
                            _locationUpdater(robotID, _positions[robotID].ID, _orientations[robotID]);
                        }
                        break;
                    case RobotMessageTypesClient.Pickup:
                        {
                            // Notify all listening controllers
                            foreach (var controllerID in _controlClients.Keys)
                                SendMsg(ClientType.C, controllerID, ControlTranslator.EncodeToPickup(robotID, messageResult.PickupSuccess));
                        }
                        break;
                    case RobotMessageTypesClient.Setdown:
                        {
                            // Notify all listening controllers
                            foreach (var controllerID in _controlClients.Keys)
                                SendMsg(ClientType.C, controllerID, ControlTranslator.EncodeToSetdown(robotID, messageResult.SetdownSuccess));
                        }
                        break;
                    default: throw new ArgumentException("Unknown message type: " + messageResult.Type);
                }
            }
            else
            {
                // Show the message
                _logger(GetPaddedString(ClientType.R, robotID) + "::<<<<::" + message + "::" + DateTime.Now.ToString("G"));
            }
        }

        /// <summary>
        /// Handles incoming communication with an input-station.
        /// </summary>
        /// <param name="iStationID">The ID of the input-station.</param>
        /// <param name="message">The message received from the input-station.</param>
        private void HandleInputStationComm(int iStationID, string message)
        {
            // Show the message
            _logger(GetPaddedString(ClientType.I, iStationID) + "::<<<<::" + message + "::" + DateTime.Now.ToString("G"));
        }
        /// <summary>
        /// Handles incoming communication with an output-station.
        /// </summary>
        /// <param name="oStationID">The ID of the output-station.</param>
        /// <param name="message">The message received from the output-station.</param>
        private void HandleOutputStationComm(int oStationID, string message)
        {
            // Show the message
            _logger(GetPaddedString(ClientType.O, oStationID) + "::<<<<::" + message + "::" + DateTime.Now.ToString("G"));
        }

        private void HandleControlComm(int controlID, string message)
        {
            // Show the message
            _logger(GetPaddedString(ClientType.C, controlID) + "::<<<<::" + message + "::" + DateTime.Now.ToString("G"));
            // Check message validness
            if (ControlTranslator.CheckMessageValidnessFrom(message))
            {
                // Translate the message
                ControlMessageResultClient messageResult = ControlTranslator.DecodeFrom(message);

                // Pass the received command to the right robot
                switch (messageResult.Type)
                {
                    case ControlMessageTypesClient.Path:
                        {
                            try
                            {
                                SendMsg(ClientType.R, messageResult.RobotID, ConvertToRobCmd(messageResult.RobotID, messageResult.WaitTime, messageResult.Path));
                            }
                            catch (Exception ex)
                            {
                                _logger("Alert! Error tranlating path: " + string.Join("-", messageResult.Path) + " - Message: " + ex.Message);
                            }
                        }
                        break;
                    case ControlMessageTypesClient.Pickup: { SendMsg(ClientType.R, messageResult.RobotID, RobotTranslator.EncodeToPickup()); } break;
                    case ControlMessageTypesClient.Setdown: { SendMsg(ClientType.R, messageResult.RobotID, RobotTranslator.EncodeToSetdown()); } break;
                    case ControlMessageTypesClient.Rest: { SendMsg(ClientType.R, messageResult.RobotID, RobotTranslator.EncodeToRest()); } break;
                    case ControlMessageTypesClient.GetItem: { SendMsg(ClientType.R, messageResult.RobotID, RobotTranslator.EncodeToGetItem()); } break;
                    case ControlMessageTypesClient.PutItem: { SendMsg(ClientType.R, messageResult.RobotID, RobotTranslator.EncodeToPutItem()); } break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Sends a broadcast message to all registered clients.
        /// </summary>
        /// <param name="msg">The message to send.</param>
        public void SendBroadMsg(string msg)
        {
            msg = CommunicationConstants.MSG_PREFIX + msg + CommunicationConstants.MSG_TERMINAL;
            Byte[] codedMsg = Encoding.UTF8.GetBytes(msg);
            foreach (Socket cl in _connectedClients)
            {
                _logger(cl.RemoteEndPoint.ToString() + "::>>>>::" + msg + "::" + DateTime.Now.ToString("G"));
                cl.Send(codedMsg);
            }
        }

        /// <summary>
        /// Sends a message to a specific client.
        /// </summary>
        /// <param name="clientType">The type of the client.</param>
        /// <param name="clientID">The ID of the client.</param>
        /// <param name="msg">The message to send.</param>
        public void SendMsg(ClientType clientType, int clientID, string msg)
        {
            // Prepare
            Byte[] codedMsg = Encoding.UTF8.GetBytes(CommunicationConstants.MSG_PREFIX + msg + CommunicationConstants.MSG_TERMINAL);
            Socket client;
            // Get the right client
            switch (clientType)
            {
                case ClientType.R: client = _botClients[clientID]; break;
                case ClientType.I: client = _iStationClients[clientID]; break;
                case ClientType.O: client = _oStationClients[clientID]; break;
                case ClientType.C: client = _controlClients[clientID]; break;
                default: throw new ArgumentException("Unknown client type:" + clientType);
            }
            // Send the message
            client.Send(codedMsg);
            _logger(GetPaddedString(clientType, clientID) + "::>>>>::" + msg + "::" + DateTime.Now.ToString("G"));
        }

        #endregion

        #region Communication establishing

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void StartServer()
        {
            ThreadStart _tcpServerThreadDelegate = new ThreadStart(Listen);
            _tcpServerThread = new Thread(_tcpServerThreadDelegate);
            _tcpServerThread.Start();
            _isRunning = true;
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void StopServer()
        {
            if (_tcpServerThread != null)
            {
                _tcpServerThread.Interrupt();
                _isRunning = false;
            }
        }

        /// <summary>
        /// Initiates listening for clients.
        /// </summary>
        public void Listen()
        {
            Socket newsock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            newsock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, _serverPort);
            try
            {
                newsock.Bind(localEP);
                newsock.Listen(10);
                newsock.BeginAccept(new AsyncCallback(OnConnectRequest), newsock);
            }
            catch (Exception ex)
            {
                _logger(ex.Message);
                _isRunning = false;
            }
        }

        /// <summary>
        /// Called for every new connection request.
        /// </summary>
        /// <param name="ar">The async result containing the server socket</param>
        public void OnConnectRequest(IAsyncResult ar)
        {
            //Init SOCKET
            Socket serverSocket = (Socket)ar.AsyncState;
            Socket newClientSocket = serverSocket.EndAccept(ar);
            string welcomeMessage = "Welcome! The Server is Connected!";
            byte[] byteDateLine = System.Text.Encoding.UTF8.GetBytes(welcomeMessage);
            //show message to client and show the connection infos
            string ip = newClientSocket.RemoteEndPoint.ToString();
            _logger(ip + "::connection established:" + DateTime.Now.ToString("G"));
            newClientSocket.Send(byteDateLine, byteDateLine.Length, 0);
            // Buffer messages from the client
            bool terminated = false;
            MessageBuffer buffer = new MessageBuffer((string message) => { HandleMetaMessage(newClientSocket, message, out terminated); });
            // Wait for another new client connection
            serverSocket.BeginAccept(new AsyncCallback(OnConnectRequest), serverSocket);
            // Start receiving messages until communication termination
            try
            {
                while (!terminated)
                {
                    // Read the message
                    int recv = newClientSocket.Receive(byteDateLine);
                    string stringdata = Encoding.UTF8.GetString(byteDateLine, 0, recv);
                    // Pass message to buffer
                    buffer.SubmitFragment(stringdata);
                }
            }
            catch (SocketException ex)
            {
                try
                {
                    // Log exception
                    _logger(newClientSocket.RemoteEndPoint.ToString() + "::exception:" + ex.Message + ":" + DateTime.Now.ToString("G"));
                    // Try to remove the client
                    bool foundClient = false; ClientType clientType = ClientType.R; int clientID = 0;
                    if (_botClients.Values.Contains(newClientSocket)) { foundClient = true; clientType = ClientType.R; clientID = _botClients.First(kvp => kvp.Value == newClientSocket).Key; }
                    if (_controlClients.Values.Contains(newClientSocket)) { foundClient = true; clientType = ClientType.C; clientID = _controlClients.First(kvp => kvp.Value == newClientSocket).Key; }
                    if (_iStationClients.Values.Contains(newClientSocket)) { foundClient = true; clientType = ClientType.I; clientID = _iStationClients.First(kvp => kvp.Value == newClientSocket).Key; }
                    if (_oStationClients.Values.Contains(newClientSocket)) { foundClient = true; clientType = ClientType.O; clientID = _oStationClients.First(kvp => kvp.Value == newClientSocket).Key; }
                    if (foundClient)
                        RemoveDeadClient(clientType, clientID, newClientSocket);
                }
                catch (ObjectDisposedException) { _logger(ip + "::exception:" + ex.Message + ":" + DateTime.Now.ToString("G")); }
            }
        }

        private void RemoveDeadClient(ClientType clientType, int clientID, Socket socket)
        {
            // Remove client from lists of connected clients and view
            _connectedClients.Remove(socket);
            // Remove meta-info
            _clientRemoved(clientType, clientID);
            switch (clientType)
            {
                case ClientType.R: { _botClients.Remove(clientID); _orientations.Remove(clientID); _positions.Remove(clientID); } break;
                case ClientType.I: { _iStationClients.Remove(clientID); } break;
                case ClientType.O: { _oStationClients.Remove(clientID); } break;
                case ClientType.C: { _controlClients.Remove(clientID); } break;
                default: throw new ArgumentException("Unknown client type: " + clientType);
            }
            // Close connection
            socket.Shutdown(SocketShutdown.Both);
            socket.Disconnect(false);
            socket.Dispose();
        }

        #endregion

        #endregion

        #region Waypoint handling

        private WaypointManager _waypointManager;

        /// <summary>
        /// Loads the waypoint and dictionary files supplied to initiate a new waypoint-manager with the given system.
        /// </summary>
        /// <param name="filePathWP">The file to load the waypoints from.</param>
        /// <param name="filePathDict">A dictionary translating RFID tags to waypoint IDs.</param>
        /// <param name="logger">A logger that is used to output information about the progress.</param>
        public void LoadWaypoints(string filePathWP, string filePathDict, Action<string> logger)
        {
            // Init the new waypoint manager
            _waypointManager = new WaypointManager(filePathWP, filePathDict, logger);
        }

        /// <summary>
        /// Keeps track of the orientations of the bots.
        /// </summary>
        private Dictionary<int, RobotOrientation> _orientations = new Dictionary<int, RobotOrientation>();
        /// <summary>
        /// Keeps track of the positions of the bots. (At least the last known one)
        /// </summary>
        private Dictionary<int, DTOWaypoint> _positions = new Dictionary<int, DTOWaypoint>();
        /// <summary>
        /// Keeps track of the previous positions of the bot in order to reconstruct orientations.
        /// </summary>
        private Dictionary<int, DTOWaypoint> _lastPositions = new Dictionary<int, DTOWaypoint>();
        /// <summary>
        /// Stores all paths submitted to the robots.
        /// </summary>
        private Dictionary<int, List<int>> _lastPaths = new Dictionary<int, List<int>>();
        /// <summary>
        /// Indicates whether the respective robot is in error handling mode.
        /// </summary>
        private Dictionary<int, bool> _botErrorCorrectionMode = new Dictionary<int, bool>();
        /// <summary>
        /// The paths used to bring robots back on track.
        /// </summary>
        private Dictionary<int, List<int>> _resolvingPaths = new Dictionary<int, List<int>>();

        public string ConvertToRobCmd(int robot, double waitTime, List<int> path)
        {
            // Get current position and orientation of the bot
            RobotOrientation currentOrientation = _orientations[robot];
            DTOWaypoint currentWaypoint = _waypointManager.Translate(path.First());
            if (currentWaypoint != _positions[robot])
                throw new InvalidOperationException("Path is not starting with the current position of the robot!");

            // Store path just in case the robot does not turn towards the right direction - we can then re-execute it after backtracking
            if (!_botErrorCorrectionMode.ContainsKey(robot) || !_botErrorCorrectionMode[robot])
            {
                _botErrorCorrectionMode[robot] = false;
                _lastPaths[robot] = path;
            }

            // Init command list
            List<RobotActions> commands = new List<RobotActions>();

            // Build all commands necessary to follow the path starting with the second waypoint of the list
            foreach (var wpID in path.Skip(1))
            {
                // Get next waypoint
                DTOWaypoint nextWaypoint = _waypointManager.Translate(wpID);
                // Check whether the waypoints are connected at all
                if (!_waypointManager.IsConnected(currentWaypoint, nextWaypoint))
                    throw new InvalidOperationException("Cannot go to a waypoint that is not connected with this one!");

                // Get orientation we have to turn towards
                RobotOrientation currentGoalOrientation;
                double xDelta = nextWaypoint.X - currentWaypoint.X; double yDelta = nextWaypoint.Y - currentWaypoint.Y;
                if (Math.Abs(xDelta) == 0 && Math.Abs(yDelta) == 0)
                    throw new InvalidOperationException("Cannot go from a waypoint to itself.");
                if (Math.Abs(xDelta) > Math.Abs(yDelta))
                    if (xDelta > 0) currentGoalOrientation = RobotOrientation.East;
                    else currentGoalOrientation = RobotOrientation.West;
                else
                    if (yDelta > 0) currentGoalOrientation = RobotOrientation.North;
                else currentGoalOrientation = RobotOrientation.South;

                // See whether we have to turn
                switch (currentOrientation)
                {
                    case RobotOrientation.North:
                        switch (currentGoalOrientation)
                        {
                            case RobotOrientation.North: /* Nothing to do */ break;
                            case RobotOrientation.West: commands.Add(RobotActions.TurnLeft); break;
                            case RobotOrientation.South: commands.Add(RobotActions.TurnLeft); commands.Add(RobotActions.TurnLeft); break;
                            case RobotOrientation.East: commands.Add(RobotActions.TurnRight); break;
                            default: throw new ArgumentException("Unknown orientation: " + currentGoalOrientation);
                        }
                        break;
                    case RobotOrientation.West:
                        switch (currentGoalOrientation)
                        {
                            case RobotOrientation.North: commands.Add(RobotActions.TurnRight); break;
                            case RobotOrientation.West: /* Nothing to do */ break;
                            case RobotOrientation.South: commands.Add(RobotActions.TurnLeft); break;
                            case RobotOrientation.East: commands.Add(RobotActions.TurnLeft); commands.Add(RobotActions.TurnLeft); break;
                            default: throw new ArgumentException("Unknown orientation: " + currentGoalOrientation);
                        }
                        break;
                    case RobotOrientation.South:
                        switch (currentGoalOrientation)
                        {
                            case RobotOrientation.North: commands.Add(RobotActions.TurnLeft); commands.Add(RobotActions.TurnLeft); break;
                            case RobotOrientation.West: commands.Add(RobotActions.TurnRight); break;
                            case RobotOrientation.South: /* Nothing to do */ break;
                            case RobotOrientation.East: commands.Add(RobotActions.TurnLeft); break;
                            default: throw new ArgumentException("Unknown orientation: " + currentGoalOrientation);
                        }
                        break;
                    case RobotOrientation.East:
                        switch (currentGoalOrientation)
                        {
                            case RobotOrientation.North: commands.Add(RobotActions.TurnLeft); break;
                            case RobotOrientation.West: commands.Add(RobotActions.TurnLeft); commands.Add(RobotActions.TurnLeft); break;
                            case RobotOrientation.South: commands.Add(RobotActions.TurnRight); break;
                            case RobotOrientation.East: /* Nothing to do */ break;
                            default: throw new ArgumentException("Unknown orientation: " + currentGoalOrientation);
                        }
                        break;
                    default: throw new ArgumentException("Unknown orientation: " + currentOrientation);
                }

                // Add the go command
                commands.Add(RobotActions.Forward);

                // Update markers of current situation
                currentWaypoint = nextWaypoint;
                currentOrientation = currentGoalOrientation;
            }

            // Build and return the command string
            return RobotTranslator.EncodeTo(waitTime, commands);
        }

        #endregion
    }
}
