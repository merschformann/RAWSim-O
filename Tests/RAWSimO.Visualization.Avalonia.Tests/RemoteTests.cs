using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using RAWSimO.Core.Configurations;
using RAWSimO.Core.Generator;
using Xunit;

namespace RAWSimO.Visualization.Avalonia.Tests
{
    public class RemoteTests
    {
        private static void WaitFor(Func<bool> condition, int timeoutMs = 5000)
        {
            for (int waited = 0; !condition() && waited < timeoutMs; waited += 20)
            {
                Thread.Sleep(20);
                Dispatcher.UIThread.RunJobs();
            }
        }

        [AvaloniaFact]
        public void ConnectsToServerAndReportsFailures()
        {
            var window = new MainWindow();
            window.Show();
            window.SetInstance(InstanceGenerator.GenerateLayout(new LayoutConfiguration(), new SettingConfiguration(), new ControlConfiguration()));
            var status = window.FindControl<TextBlock>("TextRemoteStatus");
            var port = window.FindControl<TextBox>("TextRemotePort");
            void Click(string name) => window.FindControl<Button>(name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            // A local "server" receiving the messages of the control client
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int serverPort = ((IPEndPoint)listener.LocalEndpoint).Port;
            Task<string> received = Task.Run(() =>
            {
                using TcpClient client = listener.AcceptTcpClient();
                client.ReceiveTimeout = 5000;
                var buffer = new byte[4096];
                var text = new StringBuilder();
                try
                {
                    while (!text.ToString().Contains("Disconnect"))
                    {
                        int n = client.GetStream().Read(buffer, 0, buffer.Length);
                        if (n == 0) break;
                        text.Append(Encoding.UTF8.GetString(buffer, 0, n));
                    }
                }
                catch (Exception) { /* timeout */ }
                return text.ToString();
            });

            port.Text = serverPort.ToString();
            Click("ButtonRemoteConnect");
            WaitFor(() => status.Text != "Connecting…");
            Assert.Equal("Connected", status.Text);
            Click("ButtonRemoteDisconnect");
            Assert.Equal("Not connected", status.Text);
            Assert.True(received.Wait(5000), "Server did not receive the messages");
            Assert.Contains("Init", received.Result);
            Assert.Contains("Disconnect", received.Result);
            listener.Stop();

            // Connecting to a port without a server must not claim to be connected
            var unused = new TcpListener(IPAddress.Loopback, 0);
            unused.Start();
            int closedPort = ((IPEndPoint)unused.LocalEndpoint).Port;
            unused.Stop();
            port.Text = closedPort.ToString();
            Click("ButtonRemoteConnect");
            WaitFor(() => status.Text != "Connecting…");
            Assert.Equal("Not connected", status.Text);
            window.Close();
        }
    }
}
