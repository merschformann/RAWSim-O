using RAWSimO.CommFramework;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RAWSimO.AppServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            showIPAddress();
            ButtonStartStopServer_Click(this, new RoutedEventArgs());
            ButtonLoadWaypoints_Click(this, new RoutedEventArgs());
        }

        /// <summary>
        /// The actual server instance handling the communication.
        /// </summary>
        private CommunicationServer _server;
        /// <summary>
        /// Contains all visual elements belonging to all clients by their type and ID.
        /// </summary>
        private MultiKeyDictionary<ClientType, int, TreeViewItem> _clientItems = new MultiKeyDictionary<ClientType, int, TreeViewItem>();
        /// <summary>
        /// Contains all client-info related to the textboxes they belong to.
        /// </summary>
        private Dictionary<TextBox, ClientInfo> _inputBoxClients = new Dictionary<TextBox, ClientInfo>();
        /// <summary>
        /// Contains all client-info related to the textboxes emulating commands received by a control.
        /// </summary>
        private Dictionary<TextBox, ClientInfo> _inputBoxEmulatedControls = new Dictionary<TextBox, ClientInfo>();
        /// <summary>
        /// Text-blocks containing position information of the robots.
        /// </summary>
        private Dictionary<int, TextBlock> _botPositionBlocks = new Dictionary<int, TextBlock>();
        /// <summary>
        /// Text-blocks containing orientation information of the robots.
        /// </summary>
        private Dictionary<int, TextBlock> _botOrientationBlocks = new Dictionary<int, TextBlock>();
        /// <summary>
        /// The log stream to write the output to.
        /// </summary>
        private StreamWriter _logWriter;



        #region Helpers

        private void ShowMessage(string message)
        {
            // Init logger
            if (_logWriter == null)
                _logWriter = new StreamWriter("serverlog.txt", false) { AutoFlush = true };
            // Write to file
            _logWriter.WriteLine(message);
            // Write to GUI
            this.Dispatcher.Invoke(() =>
            {
                TextBoxOutputLog.AppendText(message + Environment.NewLine);
                TextBoxOutputLog.ScrollToEnd();
            });
        }

        /// <summary>
        /// Writes the current IP-Adress in the Title of the App
        /// </summary>
        private void showIPAddress()
        {
            try
            {
                Title = "AppServer IP-Address: " + Dns.GetHostEntry(Dns.GetHostName()).AddressList[1].ToString();
            }
            catch (IndexOutOfRangeException) { }

        }

        private void ClientTreeViewItem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                TextBox castedSender = (TextBox)sender;
                ClientInfo clientInfo = _inputBoxClients[castedSender];
                string message = castedSender.Text;
                castedSender.Text = "";
                _server.SendMsg(clientInfo.Type, clientInfo.ID, message);
            }
        }

        private void EmulatedControlTreeViewItem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                TextBox castedSender = (TextBox)sender;
                ClientInfo clientInfo = _inputBoxEmulatedControls[castedSender];
                string message = castedSender.Text;
                castedSender.Text = "";
                try
                {
                    ControlMessageResultClient messageResult = ControlTranslator.DecodeFrom(message);
                    string robotCommand;
                    switch (messageResult.Type)
                    {
                        case ControlMessageTypesClient.Path: robotCommand = _server.ConvertToRobCmd(messageResult.RobotID, messageResult.WaitTime, messageResult.Path); break;
                        case ControlMessageTypesClient.Pickup: robotCommand = RobotTranslator.EncodeToPickup(); break;
                        case ControlMessageTypesClient.Setdown: robotCommand = RobotTranslator.EncodeToSetdown(); break;
                        case ControlMessageTypesClient.Rest: robotCommand = RobotTranslator.EncodeToRest(); break;
                        case ControlMessageTypesClient.GetItem: robotCommand = RobotTranslator.EncodeToGetItem(); break;
                        case ControlMessageTypesClient.PutItem: robotCommand = RobotTranslator.EncodeToPutItem(); break;
                        default: throw new ArgumentException("Unknown command type: " + messageResult.Type.ToString());
                    }
                    _server.SendMsg(ClientType.R, messageResult.RobotID, robotCommand);
                }
                catch (Exception ex)
                {
                    ShowMessage("Error: " + ex.Message);
                }
            }
        }

        private void AddClient(ClientType clientType, int clientID, string ip)
        {
            this.Dispatcher.Invoke(() =>
                {
                    // Create element holding information about the client and create basic controls with meta-information
                    TreeViewItem clientItem = new TreeViewItem() { Header = clientType.ToString() + clientID.ToString(), IsExpanded = true };
                    StackPanel panel = new StackPanel() { Orientation = Orientation.Vertical };
                    TextBlock blockType = new TextBlock() { Text = "Type: " + clientType.ToString() };
                    panel.Children.Add(blockType);
                    TextBlock blockID = new TextBlock() { Text = "ID: " + clientID.ToString() };
                    panel.Children.Add(blockID);
                    TextBlock blockIP = new TextBlock() { Text = "IP: " + ip };
                    panel.Children.Add(blockIP);
                    // If the client is a robot add the position and orientation too
                    if (clientType == ClientType.R)
                    {
                        TextBlock blockPosition = new TextBlock() { Text = "Position: n/a" };
                        TextBlock blockOrientation = new TextBlock() { Text = "Orientation: n/a" };
                        _botPositionBlocks[clientID] = blockPosition;
                        _botOrientationBlocks[clientID] = blockOrientation;
                        panel.Children.Add(blockPosition);
                        panel.Children.Add(blockOrientation);
                    }
                    // Add a box for commands
                    TextBlock blockInputBoxDirectDescription = new TextBlock() { Text = "Command:" };
                    panel.Children.Add(blockInputBoxDirectDescription);
                    TextBox inputBox = new TextBox() { Width = 120 };
                    inputBox.KeyDown += ClientTreeViewItem_KeyDown;
                    _inputBoxClients[inputBox] = new ClientInfo() { Type = clientType, ID = clientID };
                    panel.Children.Add(inputBox);
                    // Add a controller emulating textbox if it's not a controller
                    if (clientType != ClientType.C)
                    {
                        TextBlock blockInputBoxEmulatedControlDescription = new TextBlock() { Text = "Emulated control:" };
                        panel.Children.Add(blockInputBoxEmulatedControlDescription);
                        TextBox inputBoxEmulatedControl = new TextBox() { Width = 120 };
                        inputBoxEmulatedControl.KeyDown += EmulatedControlTreeViewItem_KeyDown;
                        _inputBoxEmulatedControls[inputBoxEmulatedControl] = new ClientInfo() { Type = clientType, ID = clientID };
                        panel.Children.Add(inputBoxEmulatedControl);
                    }
                    // Integrate the controls
                    clientItem.Items.Add(panel);
                    _clientItems[clientType, clientID] = clientItem;
                    // Add the newly created control to the right tree depending on the type of client
                    switch (clientType)
                    {
                        case ClientType.R: TreeViewItemRobots.Items.Add(clientItem); break;
                        case ClientType.I: TreeViewItemIStations.Items.Add(clientItem); break;
                        case ClientType.O: TreeViewItemOStations.Items.Add(clientItem); break;
                        case ClientType.C: TreeViewItemControllers.Items.Add(clientItem); break;
                        default: throw new ArgumentException("Unknown client-type: " + clientType.ToString());
                    }
                });
        }

        private void RemoveClient(ClientType clientType, int clientID)
        {
            if (_clientItems.ContainsKey(clientType, clientID))
                this.Dispatcher.Invoke(() =>
                        {
                            // Remove the controls
                            switch (clientType)
                            {
                                case ClientType.R:
                                    {
                                        TreeViewItemRobots.Items.Remove(_clientItems[clientType, clientID]);
                                        _botPositionBlocks.Remove(clientID);
                                        _botOrientationBlocks.Remove(clientID);
                                    }
                                    break;
                                case ClientType.I: { TreeViewItemIStations.Items.Remove(_clientItems[clientType, clientID]); } break;
                                case ClientType.O: { TreeViewItemOStations.Items.Remove(_clientItems[clientType, clientID]); } break;
                                case ClientType.C: { TreeViewItemControllers.Items.Remove(_clientItems[clientType, clientID]); } break;
                                default: throw new ArgumentException("Unknown client-type: " + clientType.ToString());
                            }
                            _clientItems.Remove(clientType, clientID);
                            // Remove direct command box
                            TextBox relatedDirectTextBox = _inputBoxClients.First(e => e.Value.Type == clientType && e.Value.ID == clientID).Key;
                            _inputBoxClients.Remove(relatedDirectTextBox);
                            // Remove control emulation box if one was attached
                            if (clientType != ClientType.C)
                            {
                                TextBox relatedEmulatedControlTextBox = _inputBoxEmulatedControls.First(e => e.Value.Type == clientType && e.Value.ID == clientID).Key;
                                _inputBoxEmulatedControls.Remove(relatedEmulatedControlTextBox);
                            }
                        });
        }

        private void UpdatePosition(int robotID, int waypointID, RobotOrientation orientation)
        {
            this.Dispatcher.Invoke(() =>
            {
                _botPositionBlocks[robotID].Text = "Position: " + waypointID;
                _botOrientationBlocks[robotID].Text = "Orientation: " + orientation;
            });
        }

        private Brush _originalLogColor;
        private void IndicateCurrentMode(bool errorMode)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (_originalLogColor == null)
                    _originalLogColor = TextBoxOutputLog.Background;
                TextBoxOutputLog.Background = errorMode ? Brushes.Orange : _originalLogColor;
            });
        }

        #endregion

        #region Event handlers

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) { if (_server != null) _server.StopServer(); }

        private void ButtonStartStopServer_Click(object sender, RoutedEventArgs e)
        {
            if (_server == null || !_server.IsRunning)
            {
                _server = new CommunicationServer(
                    int.Parse(TextBoxPortNumber.Text.Trim()),
                    ShowMessage,
                    AddClient,
                    RemoveClient,
                    UpdatePosition,
                    IndicateCurrentMode);
                _server.StartServer();
                ButtonStartStopServer.Content = "Stop Services";
            }
            else
            {
                _server.StopServer();
                ButtonStartStopServer.Content = "Start Services";
            }
        }

        private void ButtonLoadWaypoints_Click(object sender, RoutedEventArgs e)
        {
            // Create an instance of the open file dialog box for the waypoint file
            OpenFileDialog waypointFileDialog = new OpenFileDialog();

            // Set filter options and filter index.
            waypointFileDialog.Filter = "Waypoints Files (.xinst) | *.xinst";

            bool? resultWP = waypointFileDialog.ShowDialog();
            if (resultWP == true)
            {
                // Get the waypoints file
                string filePathWP = waypointFileDialog.FileName;

                // Create an instance of the open file dialog box for the dictionary file
                OpenFileDialog dictionaryFileDialog = new OpenFileDialog();

                // Set filter options and filter index
                dictionaryFileDialog.Filter = "RFID Dictionary Files (.csv) | *.csv";

                bool? resultDict = dictionaryFileDialog.ShowDialog();
                if (resultDict == true)
                {
                    // Get the dictionary file
                    string filePathDict = dictionaryFileDialog.FileName;
                    // Load the new waypoints
                    _server.LoadWaypoints(filePathWP, filePathDict, ShowMessage);
                }
            }
        }

        #endregion
    }
}
