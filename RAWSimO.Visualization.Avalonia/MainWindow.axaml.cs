using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
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
using RAWSimO.Core.Items;
using RAWSimO.Core.Management;
using RAWSimO.DataPreparation;
using RAWSimO.Visualization.Avalonia.Controls;
using RAWSimO.Visualization.Avalonia.Generation;
using RAWSimO.Visualization.Avalonia.Rendering;
using RAWSimO.Visualization.Avalonia.Simulation;

namespace RAWSimO.Visualization.Avalonia
{
    public partial class MainWindow : Window
    {
        private const int MaxLogLines = 2000;
        private static readonly DetailLevel[] DetailLevels = { DetailLevel.None, DetailLevel.Debug, DetailLevel.Full };

        private Instance _instance;
        /// <summary>
        /// Whether the current instance can be executed (instances loaded for heatmap rendering only cannot).
        /// </summary>
        private bool _instanceRunnable;
        private SimulationRunner _runner;
        private List<ITierInfo> _tiers = new List<ITierInfo>();
        private bool _busy;
        private bool _suppressEvents;
        private readonly ConcurrentQueue<string> _pendingLog = new ConcurrentQueue<string>();
        private readonly List<string> _logLines = new List<string>();
        private readonly DispatcherTimer _uiTimer;

        // Configurations edited in the side panel (copies are used for generation, see GenerateFromConfigs)
        private LayoutConfiguration _layoutConfig;
        private SettingConfiguration _settingConfig;
        private ControlConfiguration _controlConfig;
        private OrderGenerator.SimpleItemGeneratorPreConfiguration _simpleItemConfig;
        private AlphabetOrderParameters _alphabetOrderParameters;
        private GridLength _sidePanelWidth = new GridLength(440);

        // Statistics output requested for the current run (captured at start, used when the simulation finishes)
        private string _statisticsFolder;

        public MainWindow()
        {
            InitializeComponent();

            _suppressEvents = true;
            ComboDetail.ItemsSource = DetailLevels;
            ComboDetail.SelectedItem = DetailLevel.Debug;
            ComboBotColors.ItemsSource = Enum.GetValues<BotColorMode>();
            ComboBotColors.SelectedIndex = 0;
            ComboHeatMode.ItemsSource = Enum.GetValues<HeatMode>();
            ComboHeatMode.SelectedItem = HeatMode.None;
            _suppressEvents = false;

            ResetConfigs();
            InitHeatmapUi();
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
        /// <param name="instance">The instance to display.</param>
        /// <param name="runnable">Whether the instance can be executed.</param>
        /// <param name="heatmap">A heatmap to show on top of the instance, if any.</param>
        public void SetInstance(Instance instance, bool runnable = true, HeatmapOverlay heatmap = null, bool transparentTier = false)
        {
            _runner?.Stop();
            _instance = instance;
            _instanceRunnable = runnable;
            View.Options.Heatmap = heatmap;
            View.Options.TransparentTier = transparentTier;
            _runner = new SimulationRunner(instance, (double)(NumericSpeed.Value ?? 1));
            _runner.SpeedReduced += speed => Dispatcher.UIThread.Post(() =>
            {
                Log($"Simulation cannot keep up, reducing speed to {speed}");
                _suppressEvents = true; NumericSpeed.Value = (decimal)speed; _suppressEvents = false;
            });
            Instance runnerInstance = instance;
            _runner.Finished += () =>
            {
                // Called on the simulation thread after the simulation stopped
                string folder = _statisticsFolder;
                if (folder != null)
                {
                    try
                    {
                        runnerInstance.SettingConfig.StatisticsDirectory = folder;
                        runnerInstance.WriteStatistics();
                        Log("Statistics written to " + folder);
                    }
                    catch (Exception ex) { Log("Error while writing statistics: " + ex.Message); }
                }
                Dispatcher.UIThread.Post(() => { Log("Simulation finished."); UpdateButtons(); });
            };

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
            var heatMode = (HeatMode)(ComboHeatMode.SelectedItem ?? HeatMode.None);
            View.Options.HeatMode = heatMode != HeatMode.None;
            // The pods calculate their heat value according to the instance's heat mode
            if (_instance != null && heatMode != HeatMode.None)
                _instance.SettingConfig.HeatMode = heatMode;
            View.Options.DrawGoal = CheckGoal.IsChecked == true;
            View.Options.DrawDestination = CheckDestination.IsChecked == true;
            View.Options.DrawPath = CheckPath.IsChecked == true;
            View.Options.BotColorMode = (BotColorMode)(ComboBotColors.SelectedItem ?? BotColorMode.DefaultBotDefaultState);
            if (_runner != null)
                _runner.CapturePaths = View.Options.DrawPath;
            View.Invalidate();
        }

        private Task RunBusy(string status, Func<Instance> work) => RunBusy(status, () => (work(), (HeatmapOverlay)null, false), runnable: true);

        /// <summary>
        /// Executes the given work in the background (UI disabled meanwhile) and shows the resulting instance.
        /// </summary>
        private async Task RunBusy(string status, Func<(Instance instance, HeatmapOverlay heatmap, bool transparentTier)> work, bool runnable)
        {
            _busy = true;
            UpdateButtons();
            TextStatus.Text = status;
            try
            {
                var result = await Task.Run(work);
                if (result.instance != null)
                    SetInstance(result.instance, runnable, result.heatmap, result.transparentTier);
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
            if (_busy || _runner?.IsRunning == true) return;
            _runner?.Stop();
            // Work on copies, so that editing the parameters later does not affect the generated instance
            LayoutConfiguration layout = Clone(_layoutConfig);
            SettingConfiguration setting = Clone(_settingConfig);
            ControlConfiguration control = Clone(_controlConfig);
            await RunBusy("Generating instance…", () => GenerateFromConfigs(layout, setting, control, Log));
        }

        /// <summary>
        /// Generates an instance from the given configurations (same preparations as the WPF visualization).
        /// </summary>
        public static Instance GenerateFromConfigs(LayoutConfiguration layout, SettingConfiguration setting, ControlConfiguration control, Action<string> log)
        {
            setting.Name = setting.GetMetaInfoBasedConfigName();
            control.Name = control.GetMetaInfoBasedConfigName();
            // Find the specified resource files
            var inventory = setting.InventoryConfiguration;
            if (inventory.ColoredWordConfiguration != null)
                inventory.ColoredWordConfiguration.WordFile = IOHelper.FindResourceFile(inventory.ColoredWordConfiguration.WordFile, ".");
            if (inventory.FixedInventoryConfiguration != null)
                inventory.FixedInventoryConfiguration.OrderFile = IOHelper.FindResourceFile(inventory.FixedInventoryConfiguration.OrderFile, ".");
            if (inventory.SimpleItemConfiguration != null)
                inventory.SimpleItemConfiguration.GeneratorConfigFile = IOHelper.FindResourceFile(inventory.SimpleItemConfiguration.GeneratorConfigFile, ".");
            setting.VisualizationAttached = true;
            setting.LogAction = log;
            Instance instance = InstanceGenerator.GenerateLayout(layout, setting, control, log);
            instance.Name = instance.GetMetaInfoBasedInstanceName();
            return instance;
        }

        /// <summary>
        /// Deep copies a configuration via XML serialization (the same mechanism used for writing configuration files).
        /// </summary>
        private static T Clone<T>(T config)
        {
            var serializer = new XmlSerializer(typeof(T));
            using var writer = new StringWriter();
            serializer.Serialize(writer, config);
            using var reader = new StringReader(writer.ToString());
            return (T)serializer.Deserialize(reader);
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

        #region Configuration panel

        private void ResetConfigs()
        {
            _layoutConfig = new LayoutConfiguration();
            _settingConfig = new SettingConfiguration();
            _controlConfig = new ControlConfiguration();
            _simpleItemConfig = new OrderGenerator.SimpleItemGeneratorPreConfiguration();
            _alphabetOrderParameters = new AlphabetOrderParameters();
            EditorLayout.Content = ConfigEditor.Create(_layoutConfig);
            EditorSetting.Content = ConfigEditor.Create(_settingConfig);
            EditorControl.Content = ConfigEditor.Create(_controlConfig);
            EditorSimpleItems.Content = ConfigEditor.Create(_simpleItemConfig);
            EditorAlphabetOrders.Content = ConfigEditor.Create(_alphabetOrderParameters);
        }

        private void ButtonResetConfigs_Click(object sender, RoutedEventArgs e) => ResetConfigs();

        private void TogglePanel_Changed(object sender, RoutedEventArgs e)
        {
            bool show = TogglePanel.IsChecked == true;
            ColumnDefinition column = MainGrid.ColumnDefinitions[0];
            if (!show && column.Width.Value > 0)
                _sidePanelWidth = column.Width;
            column.Width = show ? _sidePanelWidth : new GridLength(0);
            SidePanel.IsVisible = show;
            SidePanelSplitter.IsVisible = show;
        }

        private async void ButtonSanityCheck_Click(object sender, RoutedEventArgs e)
        {
            if (_instance == null) { await MessageDialog.Show(this, "Sanity check", "Generate or load an instance first."); return; }
            string message;
            try
            {
                var errors = _instance.SanityCheck();
                if (!errors.Any())
                    message = "No errors encountered!";
                else
                {
                    var sb = new StringBuilder("Sanity checking the instance returned negative!\nThe following errors were found:\n");
                    foreach (var errorType in errors.Select(err => err.Item1).Distinct())
                        sb.AppendLine(errorType + ": " + _instance.SanityGetDescription(errorType));
                    sb.AppendLine("\nThe detailed error list follows:");
                    foreach (var error in errors)
                        sb.AppendLine(error.Item1 + ": " + error.Item2);
                    message = sb.ToString();
                }
            }
            catch (Exception ex)
            {
                message = "An exception occurred while sanity-checking:\n" + ex;
            }
            await MessageDialog.Show(this, "Sanity check", message);
        }

        private async void ButtonSaveInstance_Click(object sender, RoutedEventArgs e)
        {
            if (_instance == null) { await MessageDialog.Show(this, "Save instance", "Generate or load an instance first."); return; }
            string file = await PickSaveFile("Save instance", _instance.GetMetaInfoBasedInstanceName(), "xinst", "Instance files");
            if (file == null) return;
            await RunSafe(() =>
            {
                _instance.Name = Path.GetFileNameWithoutExtension(file);
                InstanceIO.WriteInstance(file, _instance);
                Log("Saved instance to " + file);
            });
        }

        private async void ButtonSaveLayout_Click(object sender, RoutedEventArgs e)
        {
            string file = await PickSaveFile("Save layout configuration", _layoutConfig.GetMetaInfoBasedLayoutName(), "xlayo", "Layout files");
            if (file == null) return;
            await RunSafe(() =>
            {
                LayoutConfiguration layout = Clone(_layoutConfig);
                layout.NameLayout = Path.GetFileNameWithoutExtension(file);
                InstanceIO.WriteLayout(file, layout);
                Log("Saved layout configuration to " + file);
            });
        }

        private async void ButtonSaveSetting_Click(object sender, RoutedEventArgs e)
        {
            SettingConfiguration setting = Clone(_settingConfig);
            string file = await PickSaveFile("Save setting configuration", setting.GetMetaInfoBasedConfigName(), "xsett", "Setting files");
            if (file == null) return;
            await RunSafe(() =>
            {
                // Only store the file names of resource files (they are searched for when loading)
                var inventory = setting.InventoryConfiguration;
                if (inventory.ColoredWordConfiguration != null)
                    inventory.ColoredWordConfiguration.WordFile = Path.GetFileName(inventory.ColoredWordConfiguration.WordFile);
                if (inventory.FixedInventoryConfiguration != null)
                    inventory.FixedInventoryConfiguration.OrderFile = Path.GetFileName(inventory.FixedInventoryConfiguration.OrderFile);
                if (inventory.SimpleItemConfiguration != null)
                    inventory.SimpleItemConfiguration.GeneratorConfigFile = Path.GetFileName(inventory.SimpleItemConfiguration.GeneratorConfigFile);
                setting.Name = Path.GetFileNameWithoutExtension(file);
                InstanceIO.WriteSetting(file, setting);
                Log("Saved setting configuration to " + file);
            });
        }

        private async void ButtonSaveControl_Click(object sender, RoutedEventArgs e)
        {
            ControlConfiguration control = Clone(_controlConfig);
            string file = await PickSaveFile("Save controller configuration", control.GetMetaInfoBasedConfigName(), "xconf", "Configuration files");
            if (file == null) return;
            await RunSafe(() =>
            {
                control.Name = Path.GetFileNameWithoutExtension(file);
                InstanceIO.WriteConfiguration(file, control);
                Log("Saved controller configuration to " + file);
            });
        }

        private async void ButtonSaveSimpleItemConfig_Click(object sender, RoutedEventArgs e)
        {
            SimpleItemGeneratorConfiguration config;
            try { config = OrderGenerator.GenerateSimpleItemConfiguration(_simpleItemConfig); }
            catch (Exception ex) { await MessageDialog.Show(this, "Error", "Failure while generating the config:\n" + ex.Message); return; }
            string file = await PickSaveFile("Save simple item generator configuration", config.GetMetaInfoBasedName(), "xgenc", "Item generator configurations");
            if (file == null) return;
            await RunSafe(() =>
            {
                config.Name = Path.GetFileNameWithoutExtension(file);
                InstanceIO.WriteSimpleItemGeneratorConfigFile(file, config);
                Log("Saved item generator configuration to " + file);
                try
                {
                    InventoryInfoProcessor.PlotSimpleInventoryFrequencies(file);
                    Log("Generated a frequency diagram for the item configuration.");
                }
                catch (Exception ex)
                {
                    Log("Could not plot the item frequencies (is gnuplot installed and on the PATH?): " + ex.Message);
                }
            });
        }

        private async void ButtonSaveAlphabetOrders_Click(object sender, RoutedEventArgs e)
        {
            OrderList list;
            try { list = _alphabetOrderParameters.Generate(); }
            catch (Exception ex) { await MessageDialog.Show(this, "Error", "Failure while generating the order list:\n" + ex.Message); return; }
            string file = await PickSaveFile("Save order list", list.GetMetaInfoBasedOrderListName(), "xitem", "Order lists");
            if (file == null) return;
            await RunSafe(() =>
            {
                InstanceIO.WriteOrders(file, list);
                Log("Saved order list to " + file);
            });
        }

        private async Task<string> PickSaveFile(string title, string suggestedName, string extension, string description)
        {
            IStorageFile file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = suggestedName + "." + extension,
                DefaultExtension = extension,
                ShowOverwritePrompt = true,
                FileTypeChoices = new[] { new FilePickerFileType(description) { Patterns = new[] { "*." + extension } } },
            });
            return file?.TryGetLocalPath();
        }

        /// <summary>
        /// Executes the given action and shows errors in a dialog.
        /// </summary>
        private async Task RunSafe(Action action)
        {
            try { action(); }
            catch (Exception ex)
            {
                Log("Error: " + ex.Message);
                await MessageDialog.Show(this, "Error", ex.Message);
            }
        }

        #endregion

        #region Simulation control

        private void StartOrTogglePause()
        {
            if (_runner == null || _busy) return;
            if (!_runner.IsRunning)
            {
                if (!_instanceRunnable || _instance.Controller.CurrentTime > 0)
                    return; // Already executed - a new instance has to be generated / loaded
                PrepareExecution();
                _statisticsFolder = CheckWriteStatistics.IsChecked == true && Directory.Exists(TextStatisticsFolder.Text) ? TextStatisticsFolder.Text : null;
                if (CheckWriteStatistics.IsChecked == true && _statisticsFolder == null)
                    Log("Statistics folder does not exist - statistics will only be written at the end of the simulation horizon.");
                if (_statisticsFolder != null)
                    _instance.SettingConfig.StatisticsDirectory = _statisticsFolder;
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
            ButtonPlayPause.IsEnabled = !_busy && _runner != null && (running || (!executed && _instanceRunnable));
            ButtonHeatRender.IsEnabled = ButtonHeatAll.IsEnabled = ButtonHeatTimeWindows.IsEnabled = !_busy && !running;
            ButtonPlayPause.Content = running && !_runner.IsPaused ? "⏸ Pause" : running ? "▶ Resume" : "▶ Start";
            ButtonStop.IsEnabled = running;
        }

        #endregion

        #region UI events

        private void ButtonPlayPause_Click(object sender, RoutedEventArgs e) => StartOrTogglePause();
        private void ButtonStop_Click(object sender, RoutedEventArgs e) => StopSimulation();
        private void ButtonFit_Click(object sender, RoutedEventArgs e) => View.FitToView();

        private async void ButtonSnapshot_Click(object sender, RoutedEventArgs e)
        {
            if (_runner == null || View.Renderer == null) { await MessageDialog.Show(this, "Snapshot", "Generate or load an instance first."); return; }
            IStorageFile target = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export snapshot",
                SuggestedFileName = $"{_instance.GetInfoName()}-{DateTime.Now:yyyyMMddHHmmss}.png",
                DefaultExtension = "png",
                ShowOverwritePrompt = true,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("PNG image") { Patterns = new[] { "*.png" } },
                    new FilePickerFileType("SVG vector graphics") { Patterns = new[] { "*.svg" } },
                },
            });
            string file = target?.TryGetLocalPath();
            if (file == null) return;
            SceneRenderer renderer = View.Renderer;
            SceneSnapshot snapshot = _runner.Snapshot;
            SceneOptions options = View.Options.Clone();
            await RunSafe(() =>
            {
                SceneExporter.Export(file, renderer, snapshot, options);
                Log("Snapshot written to " + file);
            });
        }

        private async void ButtonBrowseStatistics_Click(object sender, RoutedEventArgs e)
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Statistics output folder", AllowMultiple = false });
            string path = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
            if (path == null) return;
            TextStatisticsFolder.Text = path;
            CheckWriteStatistics.IsChecked = true;
        }

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
