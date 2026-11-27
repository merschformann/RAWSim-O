using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using RAWSimO.Core.Info;
using RAWSimO.Visualization.Avalonia.Controls;
using RAWSimO.Visualization.Avalonia.Simulation;

namespace RAWSimO.Visualization.Avalonia
{
    /// <summary>
    /// The collapsible panes and the info panel (overview, orders, selection).
    /// </summary>
    public partial class MainWindow
    {
        private static readonly IBrush ActiveBrush = new SolidColorBrush(Color.FromRgb(50, 205, 50)).ToImmutable();
        private static readonly IBrush InactiveBrush = new SolidColorBrush(Color.FromRgb(205, 92, 92)).ToImmutable();
        private static readonly IBrush BlockedBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0)).ToImmutable();
        private static readonly string[] StationColumns = { "Station", "Active", "Busy", "Assigned", "Requests", "Fill" };

        private static readonly string[] StationHeaderTips =
        {
            "I = input station, O = output station (hover for the queue, click to select)",
            "Whether the station is active",
            "Whether the station is currently busy handling an item / bundle",
            "Bundles (input) / orders (output) assigned to the station",
            "Open requests / items",
            "Used capacity (see the selection for details)",
        };

        private GridLength _sidePanelWidth = new GridLength(440);
        private GridLength _infoPanelWidth = new GridLength(360);
        private GridLength _logHeight = new GridLength(140);
        private Overview _shownOverview;
        // Cells of the statistics / stations / selection tables (reused as long as the structure does not change)
        private readonly List<TextBlock> _statisticsCells = new List<TextBlock>();
        private readonly List<TextBlock[]> _stationCells = new List<TextBlock[]>();
        private readonly List<TextBlock> _selectionCells = new List<TextBlock>();
        private List<object> _stationObjects = new List<object>();

        #region Panes

        /// <summary>
        /// Shows or hides a pane by collapsing its grid column / row (remembering the size).
        /// </summary>
        private static void SetPaneVisible(DefinitionBase definition, Control pane, Control splitter, bool show, ref GridLength savedSize)
        {
            GridLength current = definition is ColumnDefinition c ? c.Width : ((RowDefinition)definition).Height;
            if (!show && current.Value > 0)
                savedSize = current;
            GridLength size = show ? savedSize : new GridLength(0);
            if (definition is ColumnDefinition column) column.Width = size; else ((RowDefinition)definition).Height = size;
            pane.IsVisible = show;
            splitter.IsVisible = show;
        }

        private void TogglePanes_Changed(object sender, RoutedEventArgs e)
        {
            if (MainGrid == null) return; // Not initialized yet
            SetPaneVisible(MainGrid.ColumnDefinitions[0], SidePanel, SidePanelSplitter, TogglePanel.IsChecked == true, ref _sidePanelWidth);
            SetPaneVisible(MainGrid.ColumnDefinitions[4], InfoPanel, InfoPanelSplitter, ToggleInfo.IsChecked == true, ref _infoPanelWidth);
            SetPaneVisible(CenterGrid.RowDefinitions[2], TextLog, LogSplitter, ToggleLog.IsChecked == true, ref _logHeight);
            if (ToggleInfo.IsChecked == true)
                UpdateInfoPanel();
        }

        #endregion

        #region Info panel

        private void OnSelectionChanged(object selected)
        {
            if (_runner == null) return;
            _runner.OverviewSelection = selected;
            if (selected != null)
            {
                InfoPanel.SelectedItem = TabSelection;
                if (ToggleInfo.IsChecked != true)
                    ToggleInfo.IsChecked = true;
            }
            _runner.RequestOverview();
            UpdateInfoPanel();
            Update3D(force: true); // Selection highlight
        }

        private void ResetInfoPanel()
        {
            _shownOverview = null;
            _stationCells.Clear();
            // Same order as in the overview (all input stations, then all output stations)
            _stationObjects = _tiers.SelectMany(t => t.GetInfoInputStations()).Cast<object>().Concat(_tiers.SelectMany(t => t.GetInfoOutputStations())).ToList();
            GridStations.Children.Clear();
            GridStations.RowDefinitions.Clear();
            GridStations.ColumnDefinitions.Clear();
            UpdateInfoPanel();
        }

        /// <summary>
        /// Requests a new overview and shows the latest one (called periodically).
        /// </summary>
        private void UpdateInfoPanel()
        {
            if (_runner == null || ToggleInfo.IsChecked != true)
                return;
            _runner.RequestOverview();
            Exception error = _runner.OverviewError;
            if (error != null)
            {
                _runner.OverviewError = null;
                Log("Error while updating the info panel: " + error);
            }
            Overview overview = _runner.Overview;
            if (overview == null || ReferenceEquals(overview, _shownOverview))
                return;
            _shownOverview = overview;
            try { ShowOverview(overview); }
            catch (Exception ex) { Log("Error while showing the info panel: " + ex); }
        }

        private void ShowOverview(Overview overview)
        {

            UpdateKeyValueGrid(GridStatistics, _statisticsCells, overview.Statistics);
            UpdateStations(overview.Stations);

            ExpanderOpenOrders.Header = $"Open orders ({overview.OpenOrderCount})";
            ExpanderPendingOrders.Header = $"Backlog ({overview.PendingOrderCount})";
            ExpanderCompletedOrders.Header = $"Completed orders ({overview.CompletedOrderCount})";
            // Only update the visible lists
            if (ExpanderOpenOrders.IsExpanded) ChipsOpenOrders.Rows = overview.OpenOrders;
            if (ExpanderPendingOrders.IsExpanded) ChipsPendingOrders.Rows = overview.PendingOrders;
            if (ExpanderCompletedOrders.IsExpanded) ChipsCompletedOrders.Rows = overview.CompletedOrders;

            // Only show details of the current selection (the overview may still describe the previous one)
            SelectionDetail detail = ReferenceEquals(overview.SelectedObject, View.SelectedObject) ? overview.Selection : null;
            TextSelectionTitle.Text = detail?.Title ?? "Nothing selected";
            TextSelectionHint.IsVisible = detail == null;
            UpdateKeyValueGrid(GridSelection, _selectionCells, detail?.Properties ?? new List<KeyValuePair<string, string>>());
            ShowChipSection(TextSelectionChips, ChipsSelection, detail?.ChipsHeader, detail?.Chips?.Count > 0 ? new[] { new OrderView(null, detail.Chips) } : null, detail?.Chips != null);
            ShowChipSection(TextSelectionOpenOrders, ChipsSelectionOpenOrders, $"Open orders ({detail?.OpenOrders?.Count})", detail?.OpenOrders, detail?.OpenOrders != null);
            ShowChipSection(TextSelectionCompletedOrders, ChipsSelectionCompletedOrders, "Completed orders (recent first)", detail?.CompletedOrders, detail?.CompletedOrders != null);
        }

        private static void ShowChipSection(TextBlock header, ChipListView chips, string title, IReadOnlyList<OrderView> rows, bool visible)
        {
            header.IsVisible = chips.IsVisible = visible;
            if (!visible) return;
            header.Text = title;
            chips.Rows = rows;
        }

        /// <summary>
        /// Shows name / value pairs in a two-column grid (cells are reused).
        /// </summary>
        private static void UpdateKeyValueGrid(Grid grid, List<TextBlock> cells, IReadOnlyList<KeyValuePair<string, string>> values)
        {
            if (cells.Count != values.Count * 2)
            {
                grid.Children.Clear();
                grid.RowDefinitions.Clear();
                cells.Clear();
                for (int i = 0; i < values.Count; i++)
                {
                    grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    var key = new TextBlock { Classes = { "key" } };
                    var value = new SelectableTextBlock { TextWrapping = TextWrapping.Wrap };
                    Grid.SetRow(key, i);
                    Grid.SetRow(value, i);
                    Grid.SetColumn(value, 1);
                    grid.Children.Add(key);
                    grid.Children.Add(value);
                    cells.Add(key);
                    cells.Add(value);
                }
            }
            for (int i = 0; i < values.Count; i++)
            {
                if (cells[2 * i].Text != values[i].Key) cells[2 * i].Text = values[i].Key;
                if (cells[2 * i + 1].Text != values[i].Value) cells[2 * i + 1].Text = values[i].Value;
            }
        }

        private void UpdateStations(IReadOnlyList<StationView> stations)
        {
            if (_stationCells.Count != stations.Count)
            {
                GridStations.Children.Clear();
                GridStations.RowDefinitions.Clear();
                GridStations.ColumnDefinitions.Clear();
                _stationCells.Clear();
                for (int c = 0; c < StationColumns.Length; c++)
                    GridStations.ColumnDefinitions.Add(new ColumnDefinition(c == StationColumns.Length - 1 ? GridLength.Star : GridLength.Auto));
                GridStations.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                for (int c = 0; c < StationColumns.Length; c++)
                {
                    var header = new TextBlock { Text = StationColumns[c], FontWeight = FontWeight.SemiBold, Foreground = Brushes.Gray };
                    ToolTip.SetTip(header, StationHeaderTips[c]);
                    Grid.SetColumn(header, c);
                    GridStations.Children.Add(header);
                }
                for (int r = 0; r < stations.Count; r++)
                {
                    GridStations.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    var row = new TextBlock[StationColumns.Length];
                    for (int c = 0; c < StationColumns.Length; c++)
                    {
                        row[c] = new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis };
                        Grid.SetRow(row[c], r + 1);
                        Grid.SetColumn(row[c], c);
                        GridStations.Children.Add(row[c]);
                    }
                    // Clicking the station name selects the station
                    int index = r;
                    row[0].Cursor = new Cursor(StandardCursorType.Hand);
                    row[0].TextDecorations = TextDecorations.Underline;
                    row[0].PointerPressed += (_, _) =>
                    {
                        if (index < _stationObjects.Count)
                            View.Select(_stationObjects[index]);
                    };
                    _stationCells.Add(row);
                }
            }
            for (int r = 0; r < stations.Count; r++)
            {
                StationView s = stations[r];
                TextBlock[] row = _stationCells[r];
                row[0].Text = s.Name;
                row[1].Text = s.Active ? "●" : "○";
                row[1].Foreground = s.Active ? ActiveBrush : InactiveBrush;
                row[2].Text = s.Blocked ? "●" : "";
                row[2].Foreground = BlockedBrush;
                row[3].Text = s.Assigned.ToString();
                row[4].Text = s.Requests;
                row[5].Text = s.Capacity;
                ToolTip.SetTip(row[0], string.IsNullOrEmpty(s.Queue) ? "Queue: (empty)" : "Queue: " + s.Queue);
            }
        }

        #endregion
    }
}
