using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using RAWSimO.Core;
using RAWSimO.Core.Control;
using RAWSimO.Core.Info;
using RAWSimO.Core.IO;
using RAWSimO.Core.Statistics;
using RAWSimO.Visualization.Avalonia.Controls;
using RAWSimO.Visualization.Avalonia.Generation;
using RAWSimO.Visualization.Avalonia.Rendering;

namespace RAWSimO.Visualization.Avalonia
{
    /// <summary>
    /// Heatmap rendering (port of the WPF visualization's heatmap functionality).
    /// </summary>
    public partial class MainWindow
    {
        private string _heatInstanceFile;
        private string _heatDataFile;
        private HeatmapParameters _heatmapParameters = new HeatmapParameters();
        private Dictionary<BotTaskType, CheckBox> _heatBotTaskFilter;
        private Heatmap _heatmap;
        private HeatmapParameters _heatmapRenderedWith;

        private void InitHeatmapUi()
        {
            EditorHeatmap.Content = ConfigEditor.Create(_heatmapParameters);
            _heatBotTaskFilter = Enum.GetValues<BotTaskType>().ToDictionary(t => t, t =>
            {
                var check = new CheckBox { Content = t.ToString(), IsChecked = true, Margin = new global::Avalonia.Thickness(0, 0, 10, 0) };
                PanelHeatBotTasks.Children.Add(check);
                return check;
            });
        }

        #region File selection

        private async void ButtonHeatInstance_Click(object sender, RoutedEventArgs e)
        {
            string file = await PickFile("Select instance for heatmap rendering", "Instance files", "*.xinst");
            if (file == null) return;
            _heatInstanceFile = file;
            TextHeatInstance.Text = Path.GetFileName(file);
            ToolTip.SetTip(TextHeatInstance, file);
        }

        private async void ButtonHeatData_Click(object sender, RoutedEventArgs e)
        {
            string file = await PickFile("Select heat data", "Heat data files", "*.heat");
            if (file == null) return;
            try
            {
                HeatDataType type = Heatmap.ParseHeatDataType(file);
                string[] choices = Heatmap.GetSubDataChoices(type);
                ComboHeatValue.ItemsSource = choices;
                ComboHeatValue.SelectedIndex = 0;
                _heatDataFile = file;
                TextHeatData.Text = $"{Path.GetFileName(file)} ({type})";
                ToolTip.SetTip(TextHeatData, file);
            }
            catch (Exception ex)
            {
                await MessageDialog.Show(this, "Error", "Cannot read the heat data file:\n" + ex.Message);
            }
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Reads an instance for heatmap rendering and removes the elements that shall not be drawn.
        /// </summary>
        private static Instance ReadHeatInstance(string file, bool bots, bool pods, bool stations, bool semaphores, out List<(int tier, double x, double y)> botPositions)
        {
            Instance instance = InstanceIO.ReadInstance(file, null, null, overrideVisualizationAttached: true, visualizationOnly: true);
            botPositions = instance.GetInfoBots().Select(b => (b.GetInfoCurrentTier().GetInfoID(), b.GetInfoCenterX(), b.GetInfoCenterY())).ToList();
            if (!bots) instance.VisClearBots();
            if (!pods) instance.VisClearPods();
            if (!stations) instance.VisClearStations();
            if (!semaphores) instance.VisClearSemaphores();
            return instance;
        }

        /// <summary>
        /// Captures the current heatmap UI settings (must be called on the UI thread).
        /// </summary>
        private (bool bots, bool pods, bool stations, bool semaphores, HashSet<BotTaskType> taskFilter, float opacity, HeatmapParameters parameters) GetHeatSettings() =>
            (CheckHeatBots.IsChecked == true, CheckHeatPods.IsChecked == true, CheckHeatStations.IsChecked == true, CheckHeatSemaphores.IsChecked == true,
             _heatBotTaskFilter.Where(kv => kv.Value.IsChecked == true).Select(kv => kv.Key).ToHashSet(),
             (float)SliderHeatOpacity.Value,
             _heatmapParameters.Clone());

        private async void ButtonHeatShowInstance_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(_heatInstanceFile)) { await MessageDialog.Show(this, "Heatmaps", "Select an instance first."); return; }
            var settings = GetHeatSettings();
            string file = _heatInstanceFile;
            _heatmap = null;
            await RunBusy("Loading instance…", () =>
                (ReadHeatInstance(file, settings.bots, settings.pods, settings.stations, settings.semaphores, out _), null, settings.parameters.TransparentTier), runnable: false);
        }

        private async void ButtonHeatRender_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(_heatInstanceFile) || !File.Exists(_heatDataFile)) { await MessageDialog.Show(this, "Heatmaps", "Select an instance and a data file first."); return; }
            var settings = GetHeatSettings();
            string instanceFile = _heatInstanceFile;
            var data = new HeatmapDataSelection { DataFile = _heatDataFile, DataIndex = Math.Max(0, ComboHeatValue.SelectedIndex), BotTaskFilter = settings.taskFilter };
            Heatmap heatmap = null;
            await RunBusy("Rendering heatmap…", () =>
            {
                Instance instance = ReadHeatInstance(instanceFile, settings.bots, settings.pods, settings.stations, settings.semaphores, out _);
                ITierInfo tier = instance.GetInfoTiers().First(); // Heat data is rendered for the first tier (as in the WPF version)
                heatmap = Heatmap.Calculate(tier.GetInfoLength(), tier.GetInfoWidth(), tier.GetInfoID(), data, settings.parameters, Log);
                if (heatmap == null)
                    Log("No data found for the heatmap.");
                return (instance, heatmap?.CreateOverlay(settings.parameters, settings.opacity), settings.parameters.TransparentTier);
            }, runnable: false);
            _heatmap = heatmap;
            _heatmapRenderedWith = settings.parameters;
            if (heatmap != null)
                TextHeatInfo.Text = string.Format(CultureInfo.InvariantCulture, "Tile size: {0:0.###} x {1:0.###}, radius: {2:0.###}", heatmap.TileLengthX, heatmap.TileLengthY, heatmap.Radius);
        }

        private void ButtonHeatClear_Click(object sender, RoutedEventArgs e)
        {
            _heatmap = null;
            View.Options.Heatmap = null;
            View.Options.TransparentTier = false;
            TextHeatInfo.Text = "";
            View.Invalidate();
        }

        private void SliderHeatOpacity_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (View?.Options.Heatmap == null) return;
            View.Options.Heatmap = View.Options.Heatmap.WithOpacity((float)e.NewValue);
            View.Invalidate();
        }

        #endregion

        #region Batch rendering

        private async void ButtonHeatAll_Click(object sender, RoutedEventArgs e)
        {
            var instanceDirs = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select the folder containing all instance files" });
            string instanceDir = instanceDirs.Count > 0 ? instanceDirs[0].TryGetLocalPath() : null;
            if (instanceDir == null) return;
            var resultRoots = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select the root folder containing all result folders" });
            string resultRoot = resultRoots.Count > 0 ? resultRoots[0].TryGetLocalPath() : null;
            if (resultRoot == null) return;

            var settings = GetHeatSettings();
            var detail = (DetailLevel)(ComboDetail.SelectedItem ?? DetailLevel.Debug);
            string[] heatFiles = { IOConstants.StatFileNames[IOConstants.StatFile.HeatLocationPolling], IOConstants.StatFileNames[IOConstants.StatFile.HeatTrips] };
            await RunBatch("Rendering heatmaps for all result folders…", () =>
            {
                string[] resultDirs = Directory.EnumerateDirectories(resultRoot).OrderBy(d => d).ToArray();
                Instance instance = null; string instanceName = null; int rendered = 0;
                for (int i = 0; i < resultDirs.Length; i++)
                {
                    string namePath = Path.Combine(resultDirs[i], IOConstants.StatFileNames[IOConstants.StatFile.InstanceName]);
                    if (!File.Exists(namePath))
                        continue;
                    string name = File.ReadAllText(namePath).Trim();
                    Log($"Generating heatmap(s) for instance {name} - directory {i + 1} / {resultDirs.Length}");
                    if (name != instanceName)
                    {
                        instance = ReadHeatInstance(Path.Combine(instanceDir, name + ".xinst"), settings.bots, settings.pods, settings.stations, settings.semaphores, out _);
                        instanceName = name;
                    }
                    foreach (string heatFile in heatFiles)
                    {
                        string dataFile = Path.Combine(resultDirs[i], heatFile);
                        if (!File.Exists(dataFile))
                            continue;
                        var data = new HeatmapDataSelection { DataFile = dataFile, DataIndex = 0, BotTaskFilter = settings.taskFilter };
                        if (RenderHeatmapToFile(instance, data, settings.parameters, settings.opacity, detail, Path.Combine(resultDirs[i], Path.GetFileNameWithoutExtension(heatFile) + ".png")))
                            rendered++;
                    }
                }
                Log($"Rendered {rendered} heatmap(s).");
            });
        }

        private async void ButtonHeatTimeWindows_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(_heatInstanceFile) || !File.Exists(_heatDataFile)) { await MessageDialog.Show(this, "Heatmaps", "Select an instance and a data file first."); return; }
            var outputDirs = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select the output folder for the heatmaps" });
            string outputDir = outputDirs.Count > 0 ? outputDirs[0].TryGetLocalPath() : null;
            if (outputDir == null) return;

            var settings = GetHeatSettings();
            var detail = (DetailLevel)(ComboDetail.SelectedItem ?? DetailLevel.Debug);
            string instanceFile = _heatInstanceFile, dataFile = _heatDataFile;
            int dataIndex = Math.Max(0, ComboHeatValue.SelectedIndex);
            double step = Math.Max(SimulationObserver.STEP_LENGTH_POSITION_POLL * 5, settings.parameters.TimeWindowLength);
            await RunBatch("Rendering time-window heatmaps…", () =>
            {
                Instance instance = ReadHeatInstance(instanceFile, settings.bots, settings.pods, settings.stations, settings.semaphores, out var botPositions);
                // The first window (before the start) shows the initial bot positions, then one window after another until no data is left
                for (double low = -step, high = 0; ; low += step, high += step)
                {
                    Log($"Generating heatmap for time-window: {low.ToString(IOConstants.FORMATTER)} -> {high.ToString(IOConstants.FORMATTER)}");
                    var data = new HeatmapDataSelection
                    {
                        DataFile = dataFile,
                        DataIndex = dataIndex,
                        BotTaskFilter = settings.taskFilter,
                        TimeFilterLow = low,
                        TimeFilterHigh = high,
                        InitialBotPositions = low < 0 ? botPositions : null,
                    };
                    TimeSpan start = TimeSpan.FromSeconds(low);
                    string file = Path.Combine(outputDir, $"{instance.Name}-D-{start.Days}-H-{start.Hours}-M-{start.Minutes}-W-{Math.Floor(TimeSpan.FromSeconds(step).TotalMinutes)}.png");
                    if (!RenderHeatmapToFile(instance, data, settings.parameters, settings.opacity, detail, file))
                        break;
                }
            });
        }

        /// <summary>
        /// Calculates a heatmap and exports it together with the instance as PNG. Returns false, if there was no data.
        /// </summary>
        private bool RenderHeatmapToFile(Instance instance, HeatmapDataSelection data, HeatmapParameters parameters, float opacity, DetailLevel detail, string file)
        {
            ITierInfo tier = instance.GetInfoTiers().First();
            Heatmap heatmap = Heatmap.Calculate(tier.GetInfoLength(), tier.GetInfoWidth(), tier.GetInfoID(), data, parameters, Log);
            if (heatmap == null)
                return false;
            var renderer = new SceneRenderer(instance, tier, detail);
            SceneSnapshot snapshot = new SceneSnapshotBuilder(instance, () => 0).Build();
            var options = new SceneOptions { Heatmap = heatmap.CreateOverlay(parameters, opacity), TransparentTier = parameters.TransparentTier };
            SceneExporter.Export(file, renderer, snapshot, options);
            Log("Heatmap written to " + file);
            return true;
        }

        /// <summary>
        /// Runs a batch job in the background while the UI is disabled.
        /// </summary>
        private async Task RunBatch(string status, Action work)
        {
            _busy = true;
            UpdateButtons();
            TextStatus.Text = status;
            try
            {
                await Task.Run(work);
                TextStatus.Text = "Done: " + status.TrimEnd('…');
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

        #endregion
    }
}
