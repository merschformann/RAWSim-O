using RAWSimO.CommFramework;
using RAWSimO.Hardware.RobotHardware;
using RAWSimO.Hardware.RFID;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using System;
using System.Collections.Generic;
using System.Linq;
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
using System.IO.Ports;
using System.IO;
using RAWSimO.Hardware.RobotControl;
using System.Threading;
using System.Globalization;
using System.Management;

using System.Drawing;
using DirectShowLib;

namespace RAWSimO.AppRobot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // Read information about the robot we are controlling here
            InitRobotConfig();

            // Initiate the components
            _client = new CommunicationClient(ClientType.R, _robotConfig.ID, ReceiveTcpMsg, LogMessage, ConnectionErrorHandler);
            _iRobot = new iRobot(LogMessage);
            _robotController = new RobotCtrl(_iRobot, () => { return _botActive; }, () => { return _appQuit; }, LogMessage, RobotStatusChanged, RobotPodChanged, UpdateRFIDTag, _client, "debugsnapshots", SetFilterSettings);

            // Init this component
            InitializeComponent();

            // Initialize imageboxes here (avoid markup problems in some VS versions)
            ImageBoxCamImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            ImageBoxCVImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;

            // Initiate the GUI
            InitPidParameter();
            GetAllCOMPorts();
            GetAllCameraPorts();

            // Init meta-info
            TextBlockRobotInfo.Text = "Bot" + _robotConfig.ID + " (" + _robotConfig.Name + ")";

            // Init FPS logger
            ThreadPool.QueueUserWorkItem(new WaitCallback(LogPerformanceAndStateInfoAsync));

            // Init checkbox states
            CheckBoxActive_Checked(null, null);
            CheckBoxDummy_Checked(null, null);

            // Log all unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        private iRobot _iRobot;
        private CommunicationClient _client;
        private bool _botActive = true;
        private bool _appQuit = false;
        private StreamWriter _logWriter;

        #region Helpers

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) { LogUnhandledException(e.Exception); }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) { LogUnhandledException((Exception)e.ExceptionObject); }

        private void LogUnhandledException(Exception ex)
        {
            // Init output log
            if (_logWriter == null)
                _logWriter = new StreamWriter("robotlog.txt", true) { AutoFlush = true };
            // Log the exception to a file
            _logWriter.WriteLine("Caught an unhandled exception: " + ex.Message);
            _logWriter.WriteLine("Time: " + DateTime.Now.ToString());
            _logWriter.WriteLine("Stacktrace:");
            _logWriter.WriteLine(ex.StackTrace);
            _logWriter.Write("InnerException: ");
            if (ex.InnerException != null)
            {
                _logWriter.WriteLine(ex.InnerException.Message);
                _logWriter.WriteLine("Stacktrace:");
                _logWriter.WriteLine(ex.InnerException.StackTrace);
            }
            else
            {
                _logWriter.WriteLine("None");
            }
        }

        private void LogMessage(string message)
        {
            // Init output log
            if (_logWriter == null)
                _logWriter = new StreamWriter("robotlog.txt", false) { AutoFlush = true };
            // Write to file
            _logWriter.WriteLine(message);
            // Write to GUI
            this.Dispatcher.InvokeAsync(() =>
            {
                TextBoxOutput.AppendText(message + Environment.NewLine);
                TextBoxOutput.ScrollToEnd();
            });
        }

        private void UpdateRFIDTag(string newTag)
        {
            // Log tag to file (if desired)
            Dispatcher.InvokeAsync(() =>
            {
                if (CheckBoxLogRFIDTags.IsChecked == true)
                    // Add tags to the end of the file (if it does not exist create it)
                    using (StreamWriter sw = new StreamWriter("rfidtags.csv", true))
                        sw.WriteLine(DateTime.Now.ToString(CultureInfo.InvariantCulture) + ";" + newTag);
            });
            // Write the TAG to the GUI
            this.Dispatcher.InvokeAsync(() =>
            {
                TextBlockCurrentRFIDTag.Text = newTag;
            });
        }

        private void LogPerformanceAndStateInfoAsync(object context)
        {
            while (!_appQuit)
            {
                // Update FPS
                this.Dispatcher.InvokeAsync(() =>
                {
                    // Get current FPS / IP performance / Hough performance
                    double currentFPS = RobotController.LineFollower.CurrentFPS;
                    if (currentFPS < 7)
                        TextBlockFPSIPHough.Foreground = System.Windows.Media.Brushes.DarkRed;
                    else
                        TextBlockFPSIPHough.Foreground = System.Windows.Media.Brushes.Black;
                    TextBlockFPSIPHough.Text = "CV (FPS/IP/Hough): " +
                        currentFPS.ToString("F2", CultureInfo.InvariantCulture) + " / " +
                        RobotController.LineFollower.CurrentTimeSpentInImageProcessing.ToString("F2", CultureInfo.InvariantCulture) + " / " +
                        RobotController.LineFollower.CurrentHoughAccuracy.ToString("F2", CultureInfo.InvariantCulture);
                    // Get battery level
                    TextBlockBatteryState.Text = "Battery: " + RobotController.BatteryLevel + " / " + RobotController.BatteryCapacity + " mAh (" + RobotController.Temperature + "°C)";
                    // Get PID performance
                    TextBlockPIDPerformance.Text =
                        "Last: " + RobotController.LastCommand.ToString() +
                        " - PID-Loops: " + RobotController.LastCommandsPIDLoops.ToString() +
                        " Frames: " + RobotController.LastCommandsFrames.ToString();
                });
                // Sleep before the next update
                Thread.Sleep(500);
            }
        }

        private System.Windows.Media.Brush _movingBotColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(86, 175, 54));
        private void RobotStatusChanged()
        {
            this.Dispatcher.InvokeAsync(() =>
            {
                TextBlockBotStatus.Text = RobotController.State.Status.ToString();
                switch (RobotController.State.Status)
                {
                    case RobotStatus.Moving: CanvasBotStatus.Background = _movingBotColor; break;
                    case RobotStatus.MovingBackwards: CanvasBotStatus.Background = System.Windows.Media.Brushes.Orange; break;
                    case RobotStatus.Pickup: CanvasBotStatus.Background = System.Windows.Media.Brushes.DarkRed; break;
                    case RobotStatus.Setdown: CanvasBotStatus.Background = System.Windows.Media.Brushes.Yellow; break;
                    case RobotStatus.GetItem: CanvasBotStatus.Background = System.Windows.Media.Brushes.HotPink; break;
                    case RobotStatus.PutItem: CanvasBotStatus.Background = System.Windows.Media.Brushes.DarkViolet; break;
                    case RobotStatus.Rest: CanvasBotStatus.Background = System.Windows.Media.Brushes.DarkBlue; break;
                    case RobotStatus.Idle:
                    default: CanvasBotStatus.Background = System.Windows.Media.Brushes.DarkGray; break;
                }
            });
        }

        private void RobotPodChanged()
        {
            this.Dispatcher.InvokeAsync(() =>
            {
                if (RobotController.State.CarryingPod)
                    CanvasBotPod.Background = System.Windows.Media.Brushes.CornflowerBlue;
                else
                    CanvasBotPod.Background = System.Windows.Media.Brushes.DarkGray;
            });
        }
        /// <summary>
        /// Updates the Camera combobox with all camera devices registeres to the system 
        /// </summary>
        private void GetAllCameraPorts()
        {
            //Clear old Items
            ComboBoxCameraName.Items.Clear();

            //List all registered camera devices in Combobox
            DsDevice[] _SystemCameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            for (int i = 0; i < _SystemCameras.Length; i++)
            {
                ComboBoxCameraName.Items.Add(_SystemCameras[i].Name);
            }
            ComboBoxCameraName.SelectedIndex = _SystemCameras.Length - 1;
        }

        /// <summary>
        /// Updates the comboboxes with all com port devices registeres to the system.
        /// (thanks to <see cref="http://stackoverflow.com/questions/2837985/getting-serial-port-information"/> for the information gathering part)
        /// </summary>
        private void GetAllCOMPorts()
        {
            // Clear old items
            ComboBoxRFIDCOMPort.Items.Clear();
            ComboBoxRobotCOMPort.Items.Clear();
            // Add new com port-names
            foreach (string portname in SerialPort.GetPortNames())
            {
                ComboBoxRFIDCOMPort.Items.Add(portname);
                ComboBoxRobotCOMPort.Items.Add(portname);
            }
            // Write information about the ports to the output
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM WIN32_SerialPort"))
            {
                string[] portnames = SerialPort.GetPortNames();
                var ports = searcher.Get().Cast<ManagementBaseObject>().ToList();
                var tList = (from n in portnames
                             join p in ports on n equals p["DeviceID"].ToString()
                             select n + " - " + p["Caption"]).ToList();
                // Write port info to output
                if (tList.Any())
                {
                    LogMessage("Port information:");
                }
                else
                {
                    LogMessage("No useful ports, please connect the Roboter and refresh the Ports");
                }
                foreach (string s in tList)
                {
                    LogMessage(s);
                }
                // Set initial index, if found set recommended COM Port
                ComboBoxRFIDCOMPort.SelectedIndex = 0;
                ComboBoxRobotCOMPort.SelectedIndex = 0;
                //creates List to find the iRobot COM Port, because its not listet in the Portinformation
                List<string> portList = SerialPort.GetPortNames().ToList();
                foreach (string s in tList)
                {
                    //Set index to potential Com Port and removes it from the portList
                    if (s.Contains("Silicon Labs"))
                    {
                        String RFIDCom = s.Split('-')[0].Replace(" ", string.Empty);
                        ComboBoxRFIDCOMPort.SelectedItem = RFIDCom;
                        portList.Remove(RFIDCom);
                    }
                    //removes COM from portList
                    else if (s.Contains("Kommunikationsanschluss"))
                    {
                        String Com = s.Split('-')[0].Replace(" ", string.Empty);
                        portList.Remove(Com);
                    }
                    //set remaining Port for iRobot
                    if (portList.Any())
                    {
                        ComboBoxRobotCOMPort.SelectedItem = portList.First();
                    }
                }
            }
        }

        private void ConnectionErrorHandler(bool connected)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (connected)
                    ButtonConnectServer.Content = "Disconnect";
                else
                    ButtonConnectServer.Content = "Connect";
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _appQuit = true;
            if (_framesRefreshTicker != null)
                _framesRefreshTicker.Dispose();
            if (_client != null)
                _client.Disconnect();
            if (_iRobot != null)
                _iRobot.Disconnect();
            if (_robotController != null)
                _robotController.DisconnectRFIDReader();
        }

        private void CheckBoxActive_Checked(object sender, RoutedEventArgs e) { _botActive = CheckBoxActive.IsChecked == true; }

        private void CheckBoxDummy_Checked(object sender, RoutedEventArgs e) { RobotController.DummyMode = CheckBoxDummy.IsChecked == true; }

        private void ButtonCOMRefresh_Click(object sender, RoutedEventArgs e) { GetAllCOMPorts(); GetAllCameraPorts(); }

        #endregion

        #region Meta information

        private RobotConfig _robotConfig;
        private void InitRobotConfig()
        {
            // Robot meta information
            if (!File.Exists(RobotConfigIO.CONFIG_FILE))
            {
                RobotConfigIO.WriteConfig(new RobotConfig() { ID = 0, Name = "Dummy" }, RobotConfigIO.CONFIG_FILE);
                MessageBox.Show(
                    "No config for the bot present!\r\n" +
                    "Generating a default one at " + RobotConfigIO.CONFIG_FILE + "\r\n" +
                    "Please enter the meta-information for the robot");
            }
            _robotConfig = RobotConfigIO.ReadConfig(RobotConfigIO.CONFIG_FILE);
        }

        #endregion

        #region Communication

        private void ButtonConnectServer_Click(object sender, RoutedEventArgs e)
        {
            if (!_client.IsConnected)
            {
                string server_ip = ComboBoxServerIP.Text.Trim();
                string server_port = TextBoxServerPort.Text.Trim();
                _client.Connect(server_ip, server_port);
                ButtonConnectServer.Content = "Disconnect";
            }
            else
            {
                SendTcpMsg(CommunicationConstants.COMM_DISCONNECT_MSG);
                ButtonConnectServer.Content = "Connect";
            }
        }

        private void TextBoxChat_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (_client.IsConnected)
                {
                    SendTcpMsg(TextBoxChat.Text);
                    TextBoxChat.Text = "";
                }
            }
        }

        private void ReceiveTcpMsg(string msg)
        {
            // Submit the new command
            RobotController.SubmitCommand(msg);
        }

        private void SendTcpMsg(string str_msg)
        {
            if (_client.IsConnected)
                _client.SendMsg(str_msg);
        }

        #endregion

        #region Image capturing

        /// <summary>
        /// The image capturing control.
        /// </summary>
        private Capture _capture = null;
        /// <summary>
        /// Indicates whether the capturing is active.
        /// </summary>
        private bool _captureInProgress = false;
        /// <summary>
        /// The number of the capture camera currently in use.
        /// </summary>
        private int _captureCamNumber = -1;
        /// <summary>
        /// Lower limit for the filtering.
        /// </summary>
        private Hsv _lowerLimit = new Hsv(0, 0, 0);
        /// <summary>
        /// Upper limit for the filtering.
        /// </summary>
        private Hsv _upperLimit = new Hsv(0, 0, 0);
        /// <summary>
        /// A timer responsible for updating the images processed by the CV components asynchronously.
        /// </summary>
        private Timer _framesRefreshTicker;
        /// <summary>
        /// The processed original camera image.
        /// </summary>
        private Image<Bgr, Byte> _frameOriginal;
        /// <summary>
        /// The image as seen by the CV algorithms.
        /// </summary>
        private Image<Gray, Byte> _frameProcessed;
        /// <summary>
        /// The directory for snapshots that were taken for debug purposes.
        /// </summary>
        private string _snapshotDirectory = "snapshots";

        private void RefreshFrame(object state)
        {
            // Prepare them
            Image<Bgr, byte> original; Image<Gray, byte> processed;
            // Fetch them
            RobotController.UpdateFrames(out original, out processed);
            // Save and show them
            if (original != null && processed != null)
            {
                _frameOriginal = original; _frameProcessed = processed;
                ImageBoxCamImage.Image = original; ImageBoxCVImage.Image = processed;
            }
        }

        private void ProcessFrame(object sender, EventArgs arg)
        {
            try
            {
                // Init display refresher if not already done
                if (_framesRefreshTicker == null)
                    _framesRefreshTicker = new Timer(RefreshFrame, null, 0, 100);
                // Get and init images
                Image<Bgr, Byte> frame = new Image<Bgr, byte>(new System.Drawing.Size(_capture.Width, _capture.Height));
                _capture.Retrieve(frame);
                Image<Gray, Byte> grayFrame = null;
                // Process the image
                RobotController.ProcessImage(_lowerLimit, _upperLimit, ref frame, ref grayFrame);
            }
            catch (System.AccessViolationException ex) { MessageBox.Show("Frame Processing went wrong " + ex.Message); }
        }

        private void CheckBoxTakeSnapshots_Checked(object sender, RoutedEventArgs e)
        {
            if (CheckBoxTakeSnapshots.IsChecked == true)
                RobotController.EnableSnapshotMode(System.IO.Path.Combine(Directory.GetCurrentDirectory(), _snapshotDirectory));
            else
                RobotController.DisableSnapshotMode();
        }

        private void ButtonSetFrameUpdateDelay_Click(object sender, RoutedEventArgs e)
        {
            int delay = 0;
            if (int.TryParse(TextBoxFrameUpdateDelay.Text, out delay) && delay > 0)
                _framesRefreshTicker.Change(0, delay);
            else
                MessageBox.Show("Cannot parse the update interval - Please specify a positive integer value");
        }

        private void CheckBoxEqualizeImage_Checked(object sender, RoutedEventArgs e) { RobotController.LineFollower.EqualizeImage = CheckBoxEqualizeImage.IsChecked == true; }

        private void CheckBoxInvertImage_Checked(object sender, RoutedEventArgs e) { RobotController.LineFollower.InvertImage = CheckBoxInvertImage.IsChecked == true; }

        private void ComboBoxImageProcessingMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LineDetectionMode mode;
            bool success = Enum.TryParse<LineDetectionMode>(ComboBoxImageProcessingMode.SelectedValue.ToString(), out mode);
            if (success)
                RobotController.LineFollower.Mode = mode;
        }

        private void CheckBoxContourPostProcessing_Checked(object sender, RoutedEventArgs e) { RobotController.LineFollower.ContourPostProcessing = CheckBoxContourPostProcessing.IsChecked == true; }

        private void ComboBoxBilateralSmoothImageSigmas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int smoothSigma;
            string value = ComboBoxBilateralSmoothImageSigmas.SelectedValue.ToString();
            if (int.TryParse(value, out smoothSigma))
                RobotController.LineFollower.BilateralImageSmoothingSigmas = smoothSigma;
        }

        private void ComboBoxMedianSmoothImageSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int smoothSize;
            string value = ComboBoxMedianSmoothImageSize.SelectedValue.ToString();
            if (int.TryParse(value, out smoothSize))
                RobotController.LineFollower.MedianImageSmoothingSize = smoothSize;
        }

        private void ComboBoxErodeImagePasses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int erodePasses;
            string value = ComboBoxErodeImagePasses.SelectedValue.ToString();
            if (int.TryParse(value, out erodePasses))
                RobotController.LineFollower.ErodeImagePasses = erodePasses;
        }

        private void ComboBoxDilateImagePasses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int dilatePasses;
            string value = ComboBoxDilateImagePasses.SelectedValue.ToString();
            if (int.TryParse(value, out dilatePasses))
                RobotController.LineFollower.DilateImagePasses = dilatePasses;
        }

        private void ComboBoxClosingIterations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int closingIterations;
            string value = ComboBoxClosingIterations.SelectedValue.ToString();
            if (int.TryParse(value, out closingIterations))
                RobotController.LineFollower.ClosingIterations = closingIterations;
        }

        private void ButtonSetViewAreaAndBlockParams_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double bottomOffset = double.Parse(TextBoxViewAreaBottom.Text, CultureInfo.InvariantCulture);
                double topOffset = double.Parse(TextBoxViewAreaTop.Text, CultureInfo.InvariantCulture);
                double leftOffset = double.Parse(TextBoxViewAreaLeft.Text, CultureInfo.InvariantCulture);
                double rightOffset = double.Parse(TextBoxViewAreaRight.Text, CultureInfo.InvariantCulture);
                double threshold = double.Parse(TextBoxBlockThreshold.Text, CultureInfo.InvariantCulture);
                int horizontalCount = int.Parse(TextBoxBlockHorizontalCount.Text);
                int verticalCount = int.Parse(TextBoxBlockVerticalCount.Text);
                if (bottomOffset < 0 || bottomOffset > 1 || topOffset < 0 || topOffset > 1 || leftOffset < 0 || leftOffset > 1 || rightOffset < 0 || rightOffset > 1)
                    throw new ArgumentException("Offsets must be from range [0,1]");
                if (bottomOffset + topOffset > 1 || leftOffset + rightOffset > 1)
                    throw new ArgumentException("Offsets may not overlap");
                if (horizontalCount < 1)
                    throw new ArgumentException("Invalid number of vertical boxes: " + horizontalCount);
                if (verticalCount < 1)
                    throw new ArgumentException("Invalid number of vertical boxes: " + verticalCount);
                if (threshold < 0 || threshold > 1)
                    throw new ArgumentException("Invalid threshold: " + threshold);
                RobotController.LineFollower.SetBlockParams(bottomOffset, topOffset, leftOffset, rightOffset, horizontalCount, verticalCount);
                RobotController.LineFollower.Threshold = threshold;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void ButtonStartCapture_Click(object sender, RoutedEventArgs e)
        {
            int camNumber = ComboBoxCameraName.SelectedIndex;
            if (!_captureInProgress)
            {
                try
                {
                    // Start the selected camera
                    _capture = new Capture(camNumber);
                    // Add image handler
                    _capture.ImageGrabbed += ProcessFrame;
                    // Start the capture
                    ButtonStartCapture.Content = "Stop";
                    _capture.Start();
                    _captureInProgress = true;
                    _captureCamNumber = camNumber;
                    ComboBoxCameraName.IsEnabled = false;
                }
                catch (Exception excpt) { MessageBox.Show("Starting the capture process failed:" + Environment.NewLine + excpt.Message); }
            }
            else
            {
                // Stop the capture
                ButtonStartCapture.Content = "Start";
                _capture.Stop();
                _capture = null;
                _captureInProgress = false;
                ComboBoxCameraName.IsEnabled = true;
            }

        }

        private void ButtonTakeDebugSnapshot_Click(object sender, RoutedEventArgs e)
        {
            if (_captureInProgress)
                RobotController.TakeDebugSnapshot();
        }

        #endregion

        #region HSV filtering

        private void SliderHLow_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Transform into openCV values
            double value = SliderHLow.Value / 360.0 * 180.0;
            // Set it
            _lowerLimit.Hue = value;
        }
        private void SliderHHigh_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Transform into openCV values
            double value = SliderHHigh.Value / 360.0 * 180.0;
            // Set it
            _upperLimit.Hue = value;
        }
        private void SliderSLow_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Transform into openCV values
            double value = SliderSLow.Value / 100.0 * 255.0;
            // Set it
            _lowerLimit.Satuation = value;
        }
        private void SliderSHigh_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Transform into openCV values
            double value = SliderSHigh.Value / 100.0 * 255.0;
            // Set it
            _upperLimit.Satuation = value;
        }
        private void SliderVLow_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Transform into openCV values
            double value = SliderVLow.Value / 100.0 * 255.0;
            // Set it
            _lowerLimit.Value = value;
        }
        private void SliderVHigh_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Transform into openCV values
            double value = SliderVHigh.Value / 100.0 * 255.0;
            // Set it
            _upperLimit.Value = value;
        }
        private void SetFilterSettings(Hsv newValue)
        {
            // Sets the filter in the GUI by Hsv-value
            this.Dispatcher.InvokeAsync(() =>
            {
                SliderHLow.Value = (newValue.Hue / 180) * 360 - 30;
                SliderHHigh.Value = (newValue.Hue / 180) * 360 + 30;
                SliderSLow.Value = (newValue.Satuation / 255) * 100 - 40;
                SliderSHigh.Value = (newValue.Satuation / 255) * 100 + 40;
                SliderVLow.Value = (newValue.Value / 255) * 100 - 40;
                SliderVHigh.Value = (newValue.Value / 255) * 100 + 40;

                LogMessage("Filtersettings changed. " + FilterToString());
            });
        }
        private string FilterToString()
        {
            // Returns the filter values as formatted string
            return "Hue: " + Math.Round(SliderHLow.Value, 2) + "/" + Math.Round(SliderHHigh.Value, 2) +
                   " Saturation: " + Math.Round(SliderSLow.Value, 2) + "/" + Math.Round(SliderSHigh.Value, 2) +
                   " Brightness: " + Math.Round(SliderVLow.Value, 2) + "/" + Math.Round(SliderVHigh.Value, 2);
        }
        #endregion

        #region HSV presets

        private void MenuItemColorPresetMediumBlue_Click(object sender, RoutedEventArgs e)
        {
            SliderHLow.Value = 180;
            SliderHHigh.Value = 230;
            SliderSLow.Value = 0;
            SliderSHigh.Value = 100;
            SliderVLow.Value = 0;
            SliderVHigh.Value = 100;
            CheckBoxInvertImage.IsChecked = true;
            RobotController.LineFollower.SetBlockParams(0.2, 0.2, 0.15, 0.15, 5, 2);
            RobotController.LineFollower.FocusedLineSectionLeftOffset = 0.2;
            RobotController.LineFollower.FocusedLineSectionRightOffset = 0.2;
            RobotController.LineFollower.FocusedLineSectionBottomOffset = 0.1;
            RobotController.LineFollower.FocusedLineSectionTopOffset = 0.65;
            TextBoxBotTurnSpeed.Text = "150";
            TextBoxBotSpeed.Text = "200";
        }

        private void MenuItemColorPresetMediumGreen_Click(object sender, RoutedEventArgs e)
        {
            SliderHLow.Value = 140;
            SliderHHigh.Value = 230;
            SliderSLow.Value = 0;
            SliderSHigh.Value = 100;
            SliderVLow.Value = 0;
            SliderVHigh.Value = 100;
            CheckBoxInvertImage.IsChecked = true;
            RobotController.LineFollower.SetBlockParams(0.2, 0.2, 0.15, 0.15, 5, 2);
            RobotController.LineFollower.FocusedLineSectionLeftOffset = 0.2;
            RobotController.LineFollower.FocusedLineSectionRightOffset = 0.2;
            RobotController.LineFollower.FocusedLineSectionBottomOffset = 0.1;
            RobotController.LineFollower.FocusedLineSectionTopOffset = 0.65;
            TextBoxBotTurnSpeed.Text = "150";
            TextBoxBotSpeed.Text = "200";
        }

        private void MenuItemColorPresetBlack_Click(object sender, RoutedEventArgs e)
        {
            SliderHLow.Value = 0;
            SliderHHigh.Value = 360;
            SliderSLow.Value = 0;
            SliderSHigh.Value = 100;
            SliderVLow.Value = 50;
            SliderVHigh.Value = 100;
            CheckBoxInvertImage.IsChecked = false;
            RobotController.LineFollower.SetBlockParams(0.2, 0.2, 0.15, 0.15, 5, 2);
            RobotController.LineFollower.FocusedLineSectionLeftOffset = 0.2;
            RobotController.LineFollower.FocusedLineSectionRightOffset = 0.2;
            RobotController.LineFollower.FocusedLineSectionBottomOffset = 0.1;
            RobotController.LineFollower.FocusedLineSectionTopOffset = 0.65;
            TextBoxBotTurnSpeed.Text = "120";
            TextBoxBotSpeed.Text = "140";
        }


        private void MenuItemColorPresetBlackStrict_Click(object sender, RoutedEventArgs e)
        {
            SliderHLow.Value = 0;
            SliderHHigh.Value = 360;
            SliderSLow.Value = 0;
            SliderSHigh.Value = 100;
            SliderVLow.Value = 20;
            SliderVHigh.Value = 100;
            CheckBoxInvertImage.IsChecked = false;
            RobotController.LineFollower.SetBlockParams(0.4, 0.35, 0.1, 0.1, 5, 2);
            RobotController.LineFollower.FocusedLineSectionLeftOffset = 0.15;
            RobotController.LineFollower.FocusedLineSectionRightOffset = 0.15;
            RobotController.LineFollower.FocusedLineSectionBottomOffset = 0.35;
            RobotController.LineFollower.FocusedLineSectionTopOffset = 0.3;
            TextBoxBotTurnSpeed.Text = "120";
            TextBoxBotSpeed.Text = "140";
        }

        private void MenuItemColorPresetBlackQuietStrict_Click(object sender, RoutedEventArgs e)
        {
            SliderHLow.Value = 0;
            SliderHHigh.Value = 360;
            SliderSLow.Value = 0;
            SliderSHigh.Value = 100;
            SliderVLow.Value = 20;
            SliderVHigh.Value = 100;
            CheckBoxInvertImage.IsChecked = false;
            RobotController.LineFollower.SetBlockParams(0.4, 0.2, 0.1, 0.1, 5, 2);
            RobotController.LineFollower.FocusedLineSectionLeftOffset = 0.15;
            RobotController.LineFollower.FocusedLineSectionRightOffset = 0.15;
            RobotController.LineFollower.FocusedLineSectionBottomOffset = 0.35;
            RobotController.LineFollower.FocusedLineSectionTopOffset = 0.3;
            TextBoxBotTurnSpeed.Text = "120";
            TextBoxBotSpeed.Text = "140";
        }

        #endregion

        #region iRobot connect

        private void ButtonOpenCloseRobot_Click(object sender, RoutedEventArgs e)
        {
            if (!_iRobot.IsOpen())
            {
                _iRobot.Connect(ComboBoxRobotCOMPort.Text);

                if (_iRobot.IsOpen())
                {
                    LogMessage("iRobot Create 2 connected");
                    ButtonOpenCloseRobot.Content = "Close";
                }
                else
                    LogMessage("iRobot Create 2 could not connect");
            }
            else
            {
                _iRobot.Disconnect();
                LogMessage("iRobot Create 2 disconnected");
                ButtonOpenCloseRobot.Content = "Open";
            }
        }

        private void TextBoxDirectCommands_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                try
                {
                    // Add the command identifier and a wait time, if it was not supplied
                    string command = TextBoxDirectCommands.Text;
                    if (!command.StartsWith(RobotMessageResultServer.IDENTIFIER_GO))
                        command =
                            RobotMessageResultServer.IDENTIFIER_GO + CommunicationConstants.MSG_SUB_DELIMITER +
                            "0" + CommunicationConstants.MSG_SUB_DELIMITER +
                            command;

                    // Check whether the translation would raise an exception
                    RobotTranslator.DecodeTo(command);

                    // Submit the command
                    RobotController.SubmitCommand(command);

                    // Clear message window
                    TextBoxDirectCommands.Text = "";
                }
                catch (Exception)
                {
                    LogMessage("Invalid command!");
                }
            }
        }

        #endregion

        #region iRobot control

        private RobotCtrl _robotController;
        private RobotCtrl RobotController { get { return _robotController; } }

        private void ButtonManualGoForward_Click(object sender, RoutedEventArgs e) { RobotController.SubmitCommand(RobotTranslator.EncodeTo(0, new List<RobotActions>() { RobotActions.Forward })); }
        private void ButtonManualGoBackward_Click(object sender, RoutedEventArgs e) { RobotController.SubmitCommand(RobotTranslator.EncodeTo(0, new List<RobotActions>() { RobotActions.Backward })); }
        private void ButtonManualGoStop_Click(object sender, RoutedEventArgs e) { RobotController.SubmitCommand(RobotTranslator.EncodeToRest()); }
        private void ButtonManualGoLeft_Click(object sender, RoutedEventArgs e) { RobotController.SubmitCommand(RobotTranslator.EncodeTo(0, new List<RobotActions>() { RobotActions.TurnLeft })); }
        private void ButtonManualGoRight_Click(object sender, RoutedEventArgs e) { RobotController.SubmitCommand(RobotTranslator.EncodeTo(0, new List<RobotActions>() { RobotActions.TurnRight })); }
        private void ButtonManualGoFourForward_Click(object sender, RoutedEventArgs e) { RobotController.SubmitCommand(RobotTranslator.EncodeTo(0, new List<RobotActions>() { RobotActions.Forward, RobotActions.Forward, RobotActions.Forward, RobotActions.Forward })); }

        private void ButtonManualGoEvaluation_Click(object sender, RoutedEventArgs e)
        {
            RobotController.SubmitCommand(RobotTranslator.EncodeTo(0, new List<RobotActions>() {
        RobotActions.Forward, RobotActions.Forward, RobotActions.Forward, RobotActions.Forward,
        RobotActions.TurnRight, RobotActions.Forward, RobotActions.Forward, RobotActions.Forward,
        RobotActions.TurnRight, RobotActions.Forward, RobotActions.Forward, RobotActions.Forward, RobotActions.Forward,
        RobotActions.TurnRight, RobotActions.Forward, RobotActions.Forward,
        RobotActions.TurnRight, RobotActions.Forward, RobotActions.Forward, RobotActions.Forward,
        RobotActions.TurnRight, RobotActions.Forward,
        RobotActions.TurnRight, RobotActions.Forward, RobotActions.Forward
        }));
        }

        private void ButtonRandomWalk_Click(object sender, RoutedEventArgs e)
        {
            string stopContent = "Stop";
            if (ButtonRandomWalk.Content.ToString() == stopContent) { RobotController.StopRandomWalk(); ButtonRandomWalk.Content = "Random walk"; }
            else { RobotController.StartRandomWalk(); ButtonRandomWalk.Content = stopContent; }
        }
        private void ButtonSetTurningParams_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool activated = CheckBoxTurningTimed.IsChecked == true;
                double delay = double.Parse(TextBoxTurningDelay.Text, CultureInfo.InvariantCulture);
                double focusLeft = double.Parse(TextBoxFocusAreaLeft.Text, CultureInfo.InvariantCulture);
                double focusRight = double.Parse(TextBoxFocusAreaRight.Text, CultureInfo.InvariantCulture);
                double focusBottom = double.Parse(TextBoxFocusAreaBottom.Text, CultureInfo.InvariantCulture);
                double focusTop = double.Parse(TextBoxFocusAreaTop.Text, CultureInfo.InvariantCulture);
                if (delay < 0)
                    throw new ArgumentException("Invalid delay parameter: " + delay);
                if (focusLeft < 0 || focusLeft > 0.5)
                    throw new ArgumentException("Invalid focus area border (left): " + focusLeft);
                if (focusRight < 0 || focusRight > 1)
                    throw new ArgumentException("Invalid focus area border (right): " + focusRight);
                if (focusBottom < 0 || focusBottom > 1)
                    throw new ArgumentException("Invalid focus area border (bottom): " + focusBottom);
                if (focusTop < 0 || focusTop > 1)
                    throw new ArgumentException("Invalid focus area border (top): " + focusTop);
                RobotController.TimedTurningDelay = TimeSpan.FromSeconds(delay);
                RobotController.TimedTurning = activated;
                RobotController.LineFollower.FocusedLineSectionLeftOffset = focusLeft;
                RobotController.LineFollower.FocusedLineSectionRightOffset = focusRight;
                RobotController.LineFollower.FocusedLineSectionBottomOffset = focusBottom;
                RobotController.LineFollower.FocusedLineSectionTopOffset = focusTop;
            }
            catch (Exception ex) { MessageBox.Show("Error setting turning params:" + Environment.NewLine + ex.Message); }
        }

        #endregion

        #region PID

        private void CheckBoxPIDResolveByTurning_Checked(object sender, RoutedEventArgs e) { RobotController.TurnResolve = CheckBoxPIDResolveByTurning.IsChecked == true; }

        private void InitPidParameter()
        {
            TextBoxPIDKp.Text = RobotController.PIDKp.ToString();
            TextBoxPIDKi.Text = RobotController.PIDKi.ToString();
            CheckBoxPIDResolveByTurning.IsChecked = RobotController.TurnResolve;
            TextBoxPIDResolveDelay.Text = RobotController.TurnResolveDelay.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            TextBoxPIDLoopDelay.Text = RobotController.PIDLoopDelay.ToString();
        }

        private void ButtonSetPIDParams_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RobotController.PIDKp = int.Parse(TextBoxPIDKp.Text);
                RobotController.PIDKi = int.Parse(TextBoxPIDKi.Text);
                RobotController.TurnResolve = CheckBoxPIDResolveByTurning.IsChecked == true;
                RobotController.TurnResolveDelay = TimeSpan.FromSeconds(double.Parse(TextBoxPIDResolveDelay.Text, CultureInfo.InvariantCulture));
                RobotController.PIDLoopDelay = int.Parse(TextBoxPIDLoopDelay.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error parsing PID parameters: " + ex.Message);
            }

            //MessageBox.Show(" Kp: experiment to determine this, start by something small that just makes your bot follow the line at a slow speed.\n Ki: experiment to determine this, slowly increase the speeds and adjust this value. ( Note: Kp < Ki), Kp = " + TextBoxPIDKp.Text + ", Ki = " + TextBoxPIDKi.Text);
        }

        #endregion

        #region RFID reader

        private void ButtonOpenCloseRFID_Click(object sender, RoutedEventArgs e)
        {
            if (!RobotController.RFIDReaderConnected)
            {
                RobotController.ConnectRFIDReader(ComboBoxRFIDCOMPort.Text);
                ButtonOpenCloseRFID.Content = "Close";
            }
            else
            {
                RobotController.DisconnectRFIDReader();
                ButtonOpenCloseRFID.Content = "Open";
            }
        }
        #endregion

        #region Status LED

        private void CheckBoxLEDFlipMode_Checked(object sender, RoutedEventArgs e)
        {
            RobotController.StatusLED.FlipMode = CheckBoxLEDFlipMode.IsChecked == true;
        }

        #endregion

        #region Camera improvement
        private void ButtonResetRecognition_Click(object sender, RoutedEventArgs e)
        {
            // Resets the global average line color
            RobotController.LineFollower.ResetCameraImprovement();
        }

        private void ButtonSetFilterByTestPoints_Click(object sender, RoutedEventArgs e)
        {
            // Tunes the camera settings automatically
            try
            {
                RobotController.ColorSimilarityDeltaE = int.Parse(TextBoxColorSimilarity.Text);
                Hsv newValue = RobotController.LineFollower.ImproveCamera(_frameOriginal, _frameProcessed);
                SetFilterSettings(newValue);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Autoset Filter"); }
        }

        private void CheckBoxAutosetFilter_Checked(object sender, RoutedEventArgs e)
        {
            // Sets the attribute for automatic camera improvement
            bool isChecked = ((CheckBox)sender).IsChecked.Value;

            if (isChecked)
            {
                RobotController.CameraImprovementDelay = int.Parse(TextBoxImprovementDelay.Text);
                RobotController.ColorSimilarityDeltaE = int.Parse(TextBoxColorSimilarity.Text);
                TextBoxImprovementDelay.IsEnabled = false;
                TextBoxColorSimilarity.IsEnabled = false;
            }
            else
            {
                TextBoxImprovementDelay.IsEnabled = true;
                TextBoxColorSimilarity.IsEnabled = true;
            }

            RobotController.EnableCameraImprovement = isChecked;
        }

        private void ButtonSetErrorSettings_Click(object sender, RoutedEventArgs e)
        {
            RobotController.LineFollower.LineWidth = int.Parse(TextBoxWidthOfLine.Text);
            RobotController.LineFollower.LineWidthMargin = int.Parse(TextBoxMargin.Text);
            RobotController.LineFollower.ErrorLogCameraImprovementDelay = int.Parse(TextBoxImprovementErrorLogDelay.Text);
            RobotController.LineFollower.DrawAreasForErrorLog = true;
            RobotController.LineFollower.ErrorLogId = 0;
            ButtonStartLogging.IsEnabled = true;
        }

        private void ButtonStartLogging_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            bool start = btn.Content.ToString().StartsWith("Start");
            RobotController.LineFollower.EnableErrorLogForCameraImprovement = start;
            RobotController.LineFollower.DrawAreasForErrorLog = start;
            btn.Content = (start ? "End" : "Start") + " Logging";
            btn.IsEnabled = start;
        }

        #endregion

        private void TextBoxBotSpeed_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _robotController.PIDControl.BaseSpeed = int.Parse(TextBoxBotSpeed.Text);
            }
            catch (Exception) { }
        }

        private void TextBoxBotTurnSpeed_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _robotController.TurnSpeed = int.Parse(TextBoxBotTurnSpeed.Text);
            }
            catch (Exception) { }
        }

        private void ButtonManualTestDrive_Click(object sender, RoutedEventArgs e)
        {
            _robotController.TestDrive();
        }
    }
}
