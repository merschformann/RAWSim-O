using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using RAWSimO.Core;
using RAWSimO.Core.Configurations;
using RAWSimO.Core.Generator;
using RAWSimO.Core.Info;
using RAWSimO.Core.IO;
using RAWSimO.Visualization.Avalonia.Rendering;
using RAWSimO.Visualization.Avalonia.Simulation;

namespace RAWSimO.Visualization.Avalonia
{
    public partial class MainWindow : Window
    {
        private const int MaxLogLines = 2000;
        private static readonly DetailLevel[] DetailLevels = { DetailLevel.None, DetailLevel.Debug, DetailLevel.Full };

        private Instance _instance;
        private SimulationRunner _runner;
        private List<ITierInfo> _tiers = new List<ITierInfo>();
        private bool _busy;
        private bool _suppressEvents;
        private readonly ConcurrentQueue<string> _pendingLog = new ConcurrentQueue<string>();
        private readonly List<string> _logLines = new List<string>();
        private readonly DispatcherTimer _uiTimer;

        public MainWindow()
        {
            InitializeComponent();

            _suppressEvents = true;
            ComboDetail.ItemsSource = DetailLevels;
            ComboDetail.SelectedItem = DetailLevel.Debug;
            ComboBotColors.ItemsSource = Enum.GetValues<BotColorMode>();
            ComboBotColors.SelectedIndex = 0;
            _suppressEvents = false;

            View.SelectionChanged += _ => UpdateInfo();
            KeyDown += MainWindow_KeyDown;
            Closing += (_, _) => _runner?.Stop();

            // Periodically refresh the textual UI (time, fps, info panel, log)
            _uiTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, (_, _) => RefreshUi());
            _uiTimer.Start();

            UpdateButtons();
        }

        #region Instance handling

        /// <summary>
        /// Displays the given instance and prepares its execution.
        /// </summary>
        public void SetInstance(Instance instance)
        {
            _runner?.Stop();
            _instance = instance;
            _runner = new SimulationRunner(instance, (double)(NumericSpeed.Value ?? 1));
            _runner.SpeedReduced += speed => Dispatcher.UIThread.Post(() =>
            {
                Log($"Simulation cannot keep up, reducing speed to {speed}");
                _suppressEvents = true; NumericSpeed.Value = (decimal)speed; _suppressEvents = false;
            });
            _runner.Finished += () => Dispatcher.UIThread.Post(() => { Log("Simulation finished."); UpdateButtons(); });

            _suppressEvents = true;
            _tiers = instance.GetInfoTiers().ToList();
            ComboTier.ItemsSource = _tiers.Select(t => t.GetInfoID()).ToList();
            ComboTier.SelectedIndex = 0;
            _suppressEvents = false;

            BuildScene(keepView: false);
            TextStatus.Text =
                $"{instance.GetInfoName()}: {_tiers.Count} tier(s), {instance.GetInfoBots().Count()} bots, {instance.GetInfoPods().Count()} pods, " +
                $"{_tiers.Sum(t => t.GetInfoWaypoints().Count())} waypoints";
            UpdateButtons();
        }

        /// <summary>
        /// (Re-)creates the renderer for the currently selected tier and detail level.
        /// </summary>
        private void BuildScene(bool keepView)
        {
            if (_instance == null || ComboTier.SelectedIndex < 0)
                return;
            ITierInfo tier = _tiers[ComboTier.SelectedIndex];
            var detail = (DetailLevel)(ComboDetail.SelectedItem ?? DetailLevel.Debug);
            SimulationRunner runner = _runner;
            View.SetScene(new SceneRenderer(_instance, tier, detail), () => runner.Snapshot, keepView);
            ApplyRenderOptions();
        }

        private void ApplyRenderOptions()
        {
            View.Options.HeatMode = CheckHeat.IsChecked == true;
            View.Options.DrawGoal = CheckGoal.IsChecked == true;
            View.Options.DrawDestination = CheckDestination.IsChecked == true;
            View.Options.DrawPath = CheckPath.IsChecked == true;
            View.Options.BotColorMode = (BotColorMode)(ComboBotColors.SelectedItem ?? BotColorMode.DefaultBotDefaultState);
            if (_runner != null)
                _runner.CapturePaths = View.Options.DrawPath;
            View.Invalidate();
        }

        private async Task RunBusy(string status, Func<Instance> work)
        {
            _busy = true;
            UpdateButtons();
            TextStatus.Text = status;
            try
            {
                Instance instance = await Task.Run(work);
                SetInstance(instance);
            }
            catch (Exception ex)
            {
                Log("Error: " + ex.Message);
                TextStatus.Text = "Error: " + ex.Message;
            }
            finally
            {
                _busy = false;
                UpdateButtons();
            }
        }

        private async void ButtonGenerate_Click(object sender, RoutedEventArgs e)
        {
            _runner?.Stop();
            await RunBusy("Generating instance…", () =>
            {
                var setting = new SettingConfiguration { VisualizationAttached = true, LogAction = Log };
                var control = new ControlConfiguration();
                Instance instance = InstanceGenerator.GenerateLayout(new LayoutConfiguration(), setting, control, Log);
                instance.Name = instance.GetMetaInfoBasedInstanceName();
                return instance;
            });
        }

        private async void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            string instanceFile = await PickFile("Select instance or layout file", "Instance / layout files", "*.xinst", "*.xlayo");
            if (instanceFile == null) return;
            string settingFile = await PickFile("Select setting configuration", "Setting files", "*.xsett");
            if (settingFile == null) return;
            string configFile = await PickFile("Select control configuration", "Configuration files", "*.xconf");
            if (configFile == null) return;
            _runner?.Stop();
            await RunBusy("Loading instance…", () =>
            {
                Instance instance = InstanceIO.ReadInstance(instanceFile, settingFile, configFile, overrideVisualizationAttached: true, logAction: Log);
                instance.SettingConfig.LogAction = Log;
                return instance;
            });
        }

        private async Task<string> PickFile(string title, string filterName, params string[] patterns)
        {
            IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType(filterName) { Patterns = patterns }, FilePickerFileTypes.All },
            });
            return files.Count > 0 ? files[0].TryGetLocalPath() : null;
        }

        #endregion

        #region Simulation control

        private void StartOrTogglePause()
        {
            if (_runner == null || _busy) return;
            if (!_runner.IsRunning)
            {
                if (_instance.Controller.CurrentTime > 0)
                    return; // Already executed - a new instance has to be generated / loaded
                PrepareExecution();
                _runner.Start(_instance.SettingConfig.SimulationWarmupTime + _instance.SettingConfig.SimulationDuration);
                Log("Simulation started.");
            }
            else if (_runner.IsPaused)
                _runner.Resume();
            else
                _runner.Pause();
            UpdateButtons();
        }

        private void PrepareExecution()
        {
            // Find the specified resource files (just-in-case)
            var inventory = _instance.SettingConfig.InventoryConfiguration;
            if (inventory.ColoredWordConfiguration != null)
                inventory.ColoredWordConfiguration.WordFile = IOHelper.FindResourceFile(inventory.ColoredWordConfiguration.WordFile, ".");
            if (inventory.FixedInventoryConfiguration != null)
                inventory.FixedInventoryConfiguration.OrderFile = IOHelper.FindResourceFile(inventory.FixedInventoryConfiguration.OrderFile, ".");
            _instance.SettingConfig.LogAction = Log;
        }

        private void StopSimulation()
        {
            if (_runner == null || !_runner.IsRunning) return;
            _runner.Stop();
            UpdateButtons();
        }

        private void SetSpeed(double speed) => NumericSpeed.Value = (decimal)Math.Clamp(speed, 1, 400);

        private void UpdateButtons()
        {
            bool running = _runner?.IsRunning == true;
            bool executed = _instance != null && _instance.Controller.CurrentTime > 0;
            ButtonGenerate.IsEnabled = !_busy && !running;
            ButtonLoad.IsEnabled = !_busy && !running;
            ButtonPlayPause.IsEnabled = !_busy && _runner != null && (running || !executed);
            ButtonPlayPause.Content = running && !_runner.IsPaused ? "⏸ Pause" : running ? "▶ Resume" : "▶ Start";
            ButtonStop.IsEnabled = running;
        }

        #endregion

        #region UI events

        private void ButtonPlayPause_Click(object sender, RoutedEventArgs e) => StartOrTogglePause();
        private void ButtonStop_Click(object sender, RoutedEventArgs e) => StopSimulation();
        private void ButtonFit_Click(object sender, RoutedEventArgs e) => View.FitToView();

        private void NumericSpeed_ValueChanged(object sender, NumericUpDownValueChangedEventArgs e)
        {
            if (!_suppressEvents && _runner != null && e.NewValue.HasValue)
                _runner.Speed = (double)e.NewValue.Value;
        }

        private void ComboTier_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_suppressEvents) BuildScene(keepView: false);
        }

        private void ComboDetail_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_suppressEvents) BuildScene(keepView: true);
        }

        private void RenderOption_Changed(object sender, RoutedEventArgs e)
        {
            if (!_suppressEvents) ApplyRenderOptions();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Do not steal keys from text input controls
            if (e.Source is TextBox) return;
            switch (e.Key)
            {
                case Key.Space: StartOrTogglePause(); break;
                case Key.OemPlus: case Key.Add: SetSpeed((double)(NumericSpeed.Value ?? 1) + 1); break;
                case Key.OemMinus: case Key.Subtract: SetSpeed((double)(NumericSpeed.Value ?? 1) - 1); break;
                case Key.D0: case Key.NumPad0: SetSpeed(1); break;
                case Key.D1: case Key.NumPad1: SetSpeed(50); break;
                case Key.D2: case Key.NumPad2: SetSpeed(100); break;
                case Key.D3: case Key.NumPad3: SetSpeed(200); break;
                case Key.D4: case Key.NumPad4: SetSpeed(400); break;
                case Key.PageUp: if (ComboTier.SelectedIndex < _tiers.Count - 1) ComboTier.SelectedIndex++; break;
                case Key.PageDown: if (ComboTier.SelectedIndex > 0) ComboTier.SelectedIndex--; break;
                case Key.F: View.FitToView(); break;
                default: return;
            }
            e.Handled = true;
        }

        #endregion

        #region Status, info and log

        private void RefreshUi()
        {
            SceneSnapshot snapshot = _runner?.Snapshot;
            if (snapshot != null)
                TextTime.Text = TimeSpan.FromSeconds(snapshot.Time).ToString(@"hh\:mm\:ss");
            TextFps.Text = $"{View.MeasureFps(),5:0} fps";
            if (View.SelectedObject != null)
                UpdateInfo();
            FlushLog();
            if (_runner != null && !_runner.IsRunning && ButtonStop.IsEnabled)
                UpdateButtons();
        }

        private void UpdateInfo()
        {
            object selected = View.SelectedObject;
            TextInfo.Text = selected == null ? "Click on a bot, pod or station to see details." : DescribeObject(selected);
        }

        private static string DescribeObject(object obj)
        {
            // Note: this reads live simulation state from the UI thread (as the WPF visualization did) - values are informative only
            var sb = new StringBuilder();
            string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
            try
            {
                switch (obj)
                {
                    case IBotInfo bot:
                        sb.AppendLine($"Bot {bot.GetInfoID()}");
                        sb.AppendLine($"State:       {bot.GetInfoState()}");
                        sb.AppendLine($"Task:        {bot.GetInfoTask()}");
                        sb.AppendLine($"Position:    {F(bot.GetInfoCenterX())}, {F(bot.GetInfoCenterY())}");
                        sb.AppendLine($"Orientation: {F(bot.GetInfoOrientation() * 180 / Math.PI)}°");
                        sb.AppendLine($"Speed:       {F(bot.GetInfoSpeed())}");
                        sb.AppendLine($"Current WP:  {bot.GetInfoCurrentWaypoint()?.GetInfoID().ToString() ?? "-"}");
                        sb.AppendLine($"Goal WP:     {bot.GetInfoGoalWaypoint()?.GetInfoID().ToString() ?? "-"}");
                        sb.AppendLine($"Dest. WP:    {bot.GetInfoDestinationWaypoint()?.GetInfoID().ToString() ?? "-"}");
                        sb.AppendLine($"Blocked:     {bot.GetInfoBlocked()}");
                        sb.AppendLine($"Queueing:    {bot.GetInfoIsQueueing()}");
                        break;
                    case IPodInfo pod:
                        sb.AppendLine($"Pod {pod.GetInfoID()}");
                        sb.AppendLine($"Position:    {F(pod.GetInfoCenterX())}, {F(pod.GetInfoCenterY())}");
                        sb.AppendLine($"Capacity:    {F(pod.GetInfoCapacityUsed())} / {F(pod.GetInfoCapacity())}");
                        sb.AppendLine($"Reserved:    {F(pod.GetInfoCapacityReserved())}");
                        sb.AppendLine($"Heat:        {F(pod.GetInfoHeatValue())}");
                        sb.AppendLine($"Refill:      {pod.GetInfoReadyForRefill()}");
                        break;
                    case IInputStationInfo station:
                        sb.AppendLine($"Input station {station.GetInfoID()}");
                        sb.AppendLine($"Position:    {F(station.GetInfoCenterX())}, {F(station.GetInfoCenterY())}");
                        sb.AppendLine($"Active:      {station.GetInfoActive()}");
                        sb.AppendLine($"Capacity:    {F(station.GetInfoCapacityUsed())} / {F(station.GetInfoCapacity())}");
                        sb.AppendLine($"Requests:    {station.GetInfoOpenRequests()}");
                        sb.AppendLine($"Blocked:     {station.GetInfoBlocked()}");
                        sb.AppendLine($"Queue:       {station.GetInfoQueue()}");
                        break;
                    case IOutputStationInfo station:
                        sb.AppendLine($"Output station {station.GetInfoID()}");
                        sb.AppendLine($"Position:    {F(station.GetInfoCenterX())}, {F(station.GetInfoCenterY())}");
                        sb.AppendLine($"Active:      {station.GetInfoActive()}");
                        sb.AppendLine($"Capacity:    {F(station.GetInfoCapacityUsed())} / {F(station.GetInfoCapacity())}");
                        sb.AppendLine($"Requests:    {station.GetInfoOpenRequests()}");
                        sb.AppendLine($"Blocked:     {station.GetInfoBlocked()}");
                        sb.AppendLine($"Queue:       {station.GetInfoQueue()}");
                        break;
                    case IWaypointInfo wp:
                        sb.AppendLine($"Waypoint {wp.GetInfoID()}");
                        sb.AppendLine($"Position:    {F(wp.GetInfoCenterX())}, {F(wp.GetInfoCenterY())}");
                        sb.AppendLine($"Storage:     {wp.GetInfoStorageLocation()}");
                        sb.AppendLine($"Connections: {string.Join(", ", wp.GetInfoConnectedWaypoints().Select(o => o.GetInfoID()))}");
                        break;
                    default:
                        sb.AppendLine(obj.ToString());
                        break;
                }
            }
            catch (Exception ex)
            {
                // Concurrent modification by the simulation thread - simply show what we have, the next refresh will be fine
                sb.AppendLine($"({ex.GetType().Name} while reading)");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Logs a line (can be called from any thread).
        /// </summary>
        private void Log(string line) => _pendingLog.Enqueue($"{DateTime.Now:HH:mm:ss} {line}");

        private void FlushLog()
        {
            if (_pendingLog.IsEmpty) return;
            while (_pendingLog.TryDequeue(out string line))
                _logLines.Add(line);
            if (_logLines.Count > MaxLogLines)
                _logLines.RemoveRange(0, _logLines.Count - MaxLogLines);
            TextLog.Text = string.Join(Environment.NewLine, _logLines);
            TextLog.CaretIndex = TextLog.Text.Length;
        }

        #endregion
    }
}
