using System;
using System.Globalization;
using Avalonia.Interactivity;
using Avalonia.Threading;
using RAWSimO.CommFramework;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Remote;
using RAWSimO.Core.Waypoints;
using RAWSimO.Visualization.Avalonia.Controls;

namespace RAWSimO.Visualization.Avalonia
{
    /// <summary>
    /// Remote control of real robots via the demonstrator's server (port of the WPF visualization's remote features).
    /// </summary>
    public partial class MainWindow
    {
        private ControlClient _controlClient;

        private async void ButtonRemoteConnect_Click(object sender, RoutedEventArgs e)
        {
            if (_instance == null)
            {
                Log("No instance initiated - load the instance first, then connect to the server managing the communication with the agents of the instance.");
                return;
            }
            // Init both sides and connect all callbacks
            RemoteControlAdapter adapter = _instance.ActivateRemoteController();
            _controlClient = new ControlClient(
                adapter.RobotLocationUpdateCallback,
                adapter.RobotPickupFinishedCallback,
                adapter.RobotSetdownFinishedCallback,
                Log,
                connected => Dispatcher.UIThread.Post(() => SetRemoteConnected(connected))); // Called from the network thread
            adapter.PathSubmissionCallback = _controlClient.SubmitNewPath;
            adapter.PickupSubmissionCallback = _controlClient.SubmitPickupCommand;
            adapter.SetdownSubmissionCallback = _controlClient.SubmitSetdownCommand;
            adapter.RestSubmissionCallback = _controlClient.SubmitRestCommand;
            adapter.GetItemSubmissionCallback = _controlClient.SubmitGetItemNotification;
            adapter.PutItemSubmissionCallback = _controlClient.SubmitPutItemNotification;
            // Connecting may block for a while (e.g. unreachable host) - do it in the background
            ControlClient client = _controlClient;
            string ip = TextRemoteIp.Text, port = TextRemotePort.Text;
            ButtonRemoteConnect.IsEnabled = false;
            TextRemoteStatus.Text = "Connecting…";
            bool connected;
            try
            {
                // Note: a failed connection attempt is only logged by the client (no exception)
                connected = await System.Threading.Tasks.Task.Run(() => { client.Connect(ip, port); return client.IsConnected; });
            }
            catch (Exception ex)
            {
                Log("Could not connect: " + ex.Message);
                connected = false;
            }
            SetRemoteConnected(connected);
        }

        private void ButtonRemoteDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try { _controlClient?.Disconnect(); }
            catch (Exception ex) { Log("Error while disconnecting: " + ex.Message); }
            SetRemoteConnected(false);
        }

        private void SetRemoteConnected(bool connected)
        {
            ButtonRemoteConnect.IsEnabled = !connected;
            ButtonRemoteDisconnect.IsEnabled = connected;
            TextRemoteStatus.Text = connected ? "Connected" : "Not connected";
            if (!connected)
                _instance?.DeactivateRemoteController();
        }

        #region Virtual events

        private bool TryGetBot(out Bot bot)
        {
            bot = null;
            if (_instance == null || !int.TryParse(TextEventBot.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
            {
                Log("Enter a valid bot ID (and load an instance) first.");
                return false;
            }
            try { bot = _instance.GetBotByID(id); return true; }
            catch (Exception) { Log($"There is no bot with ID {id}."); return false; }
        }

        private void ButtonEventWaypointReached_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetBot(out Bot bot)) return;
            if (!int.TryParse(TextEventWaypoint.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int wpId)) { Log("Enter a valid waypoint ID first."); return; }
            Waypoint waypoint;
            try { waypoint = _instance.GetWaypointByID(wpId); }
            catch (Exception) { Log($"There is no waypoint with ID {wpId}."); return; }
            bot.OnReachedWaypoint(waypoint);
        }

        private void ButtonEventPickedUpPod_Click(object sender, RoutedEventArgs e) { if (TryGetBot(out Bot bot)) bot.OnPickedUpPod(); }

        private void ButtonEventSetDownPod_Click(object sender, RoutedEventArgs e) { if (TryGetBot(out Bot bot)) bot.OnSetDownPod(); }

        #endregion
    }
}
