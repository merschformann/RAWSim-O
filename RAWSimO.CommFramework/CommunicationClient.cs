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
    /// <summary>
    /// Distinguishes the different client-types to better discriminate them in the messages.
    /// </summary>
    public enum ClientType
    {
        /// <summary>
        /// Use this when the client is a robot.
        /// </summary>
        R,
        /// <summary>
        /// Use this when the client is an input-station.
        /// </summary>
        I,
        /// <summary>
        /// Use this when the client is an output-station.
        /// </summary>
        O,
        /// <summary>
        /// Use this when the client is a controller.
        /// </summary>
        C
    }

    /// <summary>
    /// All types of messages sent by an input-station client.
    /// </summary>
    public enum InputStationMessageTypesClient { }
    /// <summary>
    /// All types of messages sent to an input-station client.
    /// </summary>
    public enum InputStationMessageTypesServer { }
    /// <summary>
    /// All types of messages sent by an output-station client.
    /// </summary>
    public enum OutputStationMessageTypesClient { }
    /// <summary>
    /// All types of messages sent to an output-station client.
    /// </summary>
    public enum OutputStationMessageTypesServer { }

    /// <summary>
    /// The basic client to use for robots, input-stations and output-stations to communicate with the server.
    /// </summary>
    public class CommunicationClient
    {
        /// <summary>
        /// A prefix to identify the type of the client and the individual by its ID.
        /// </summary>
        public readonly string MsgPrefix;
        /// <summary>
        /// The type of the client this network adapter belongs to.
        /// </summary>
        private ClientType _type;
        /// <summary>
        /// The ID of the client this network adapter belongs to.
        /// </summary>
        private int _id;
        /// <summary>
        /// A callback that is used to handle all incoming messages.
        /// </summary>
        private Action<string> _receiveMessageCallback;
        /// <summary>
        /// The socket that handles the communication.
        /// </summary>
        private Socket _socket;
        /// <summary>
        /// Indicates whether we are connected.
        /// </summary>
        public bool IsConnected { get; private set; }
        /// <summary>
        /// The thread for receiving incoming messages asynchronously.
        /// </summary>
        public Thread _thread;
        /// <summary>
        /// The action that is called to log all communication.
        /// </summary>
        public Action<string> _logger;
        /// <summary>
        /// Called when the connection is closed unexpectedly.
        /// </summary>
        private Action<bool> _connectionStatusCallback;
        /// <summary>
        /// The port of the server.
        /// </summary>
        private string _serverPort = null;
        /// <summary>
        /// The IP of the server.
        /// </summary>
        private string _serverIP = null;
        /// <summary>
        /// counts how often in a row StringData is "" 
        /// </summary>
        private int _nullMessageCounter = 0;

        public CommunicationClient(ClientType type, int clientID, Action<string> receiveMessageCallback, Action<string> logger, Action<bool> connectionStatusCallBack)
        {
            _receiveMessageCallback = receiveMessageCallback;
            _logger = logger;
            _connectionStatusCallback = connectionStatusCallBack;
            _id = clientID;
            _type = type;
            MsgPrefix = _type.ToString() + CommunicationConstants.MSG_MAIN_DELIMITER + _id + CommunicationConstants.MSG_MAIN_DELIMITER;
            IsConnected = false;
        }

        /// <summary>
        /// Connects this client with the server at the given ip-address and port.
        /// </summary>
        /// <param name="server_ip">The IP-address of the server.</param>
        /// <param name="server_port">The port of the server.</param>
        public void Connect(string server_ip, string server_port)
        {
            // Setup the connection
            byte[] data = new byte[1024];
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string ipadd = server_ip;
            int port = Convert.ToInt32(server_port);
            IPEndPoint ie = new IPEndPoint(IPAddress.Parse(ipadd), port);
            // Try to connect
            try
            {
                _socket.Connect(ie);
                IsConnected = true;
                _serverPort = server_port;
                _serverIP = server_ip;
            }
            catch (SocketException e)
            {
                _logger("Connect to Server failed  " + e.Message);
                return;
            }

            // Start listening for incoming messages
            _thread = new Thread(new ThreadStart(ReceiveMsg));
            _thread.Start();

            // Tell the server who we are
            SendMsg("Init");
        }

        /// <summary>
        /// Tries to reconnect with the last known server.
        /// </summary>
        public void AttemptReconnect()
        {
            try
            {
                if (!IsConnected && _serverIP != null && _serverPort != null)
                    Connect(_serverIP, _serverPort);
                if (IsConnected && _connectionStatusCallback != null)
                    _connectionStatusCallback(true);
            }
            catch (Exception ex) { _logger("Attempt to reconnect failed: " + ex.Message); }
        }

        /// <summary>
        /// Disconnects the client.
        /// </summary>
        public void Disconnect()
        {
            IsConnected = false;
            if (_connectionStatusCallback != null)
                _connectionStatusCallback(false);
            try
            {
                _socket.Disconnect(false);
                _socket.Dispose();
            }
            catch (Exception) { /* Nothing to see here - move along */ }
        }

        /// <summary>
        /// The routine that is executed by the listening thread.
        /// </summary>
        private void ReceiveMsg()
        {
            try
            {
                // Buffer messages
                byte[] data = new byte[1024];
                MessageBuffer buffer = new MessageBuffer((string message) =>
                {
                    if (message.Equals(CommunicationConstants.COMM_DISCONNECT_MSG))
                        IsConnected = false;
                    else
                        _receiveMessageCallback(message);
                });
                // Keep listening for messages
                while (IsConnected)
                {
                    // Receive message
                    int recv = _socket.Receive(data);
                    string stringdata = Encoding.UTF8.GetString(data, 0, recv);
                    // Pass message to buffer
                    buffer.SubmitFragment(stringdata);
                    // counts empty StringData to avoid deadlock
                    if (stringdata.Equals(""))
                        _nullMessageCounter++;
                    else
                    {
                        _nullMessageCounter = 0;
                        // Log message
                        _logger("<<<<::" + stringdata);
                    }
                    if (_nullMessageCounter > 10)
                        Disconnect();


                }
            }
            catch (Exception ex)
            {
                // Disconnect when running into problems
                _logger("Terminating connection due to an error:" + Environment.NewLine + ex.Message);
                Disconnect();
            }
        }

        /// <summary>
        /// Call this when sending a message to the server. A prefix is automatically added to the message in order to identify the caller.
        /// </summary>
        /// <param name="message">The message to send to the server.</param>
        public void SendMsg(string message)
        {
            try
            {
                if (IsConnected)
                {
                    message = CommunicationConstants.MSG_PREFIX + MsgPrefix + message + CommunicationConstants.MSG_TERMINAL;
                    int m_length = message.Length;
                    byte[] data = new byte[m_length];
                    data = Encoding.UTF8.GetBytes(message);
                    int i = _socket.Send(data);
                    _logger(">>>>::" + message);
                }
            }
            catch (Exception ex)
            {
                // Disconnect when running into problems
                _logger("Terminating connection due to an error:" + Environment.NewLine + ex.Message);
                Disconnect();
            }
        }
    }
}
