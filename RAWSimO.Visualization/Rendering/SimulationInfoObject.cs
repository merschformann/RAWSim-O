using RAWSimO.Core.Info;
using RAWSimO.Core.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RAWSimO.Visualization.Rendering
{
    #region Base class

    public abstract class SimulationInfoObject
    {
        public SimulationInfoObject(TreeView infoHost) { _infoHost = infoHost; }

        protected readonly TreeView _infoHost;
        internal SimulationVisual2D ManagedVisual2D { get; set; }
        internal SimulationVisual3D ManagedVisual3D { get; set; }

        protected double StrokeThicknessFocused { get { return 5 * ManagedVisual2D.StrokeThicknessReference; } }

        public abstract void InfoPanelInit();
        public void InfoPanelLeave() { ManagedVisual2D.StrokeThickness = ManagedVisual2D.StrokeThicknessReference; _infoHost.Items.Clear(); }
        public abstract void InfoPanelUpdate();
    }

    #endregion

    #region Instance

    public class SimulationInfoInstance : SimulationInfoObject
    {
        private readonly IInstanceInfo _instance;
        private readonly int _infoPanelLeftColumnWidth = 110;
        private readonly int _infoPanelRightColumnWidth = 60;
        private readonly int _infoPanelSingleSmallElementWidth = 16;
        private Brush _colorStationActive = Brushes.LightGreen;
        private Brush _colorStationInactive = Brushes.MediumBlue;
        private Brush _colorStationBlocked = Brushes.Red;
        private Brush _colorStationFree = Brushes.LightGreen;
        private TreeViewItem _root;
        private TextBlock _blockPendingBundles;
        private TextBlock _blockPendingOrders;
        private TextBlock _blockStatItems;
        private TextBlock _blockStatBundles;
        private TextBlock _blockStatOrders;
        private TextBlock _blockStatOrdersLate;
        private TextBlock _blockStatRepositioningMoves;
        private TextBlock _blockStatCollisions;
        private TextBlock _blockStatStorageFillLevel;
        private Dictionary<IInputStationInfo, TextBlock> _blocksAssignedBundles = new Dictionary<IInputStationInfo, TextBlock>();
        private Dictionary<IOutputStationInfo, TextBlock> _blocksAssignedOrders = new Dictionary<IOutputStationInfo, TextBlock>();
        private Dictionary<IInputStationInfo, TextBlock> _blocksOpenInsertRequests = new Dictionary<IInputStationInfo, TextBlock>();
        private Dictionary<IOutputStationInfo, TextBlock> _blocksOpenExtractRequests = new Dictionary<IOutputStationInfo, TextBlock>();
        private Dictionary<IOutputStationInfo, TextBlock> _blocksOpenQueuedExtractRequests = new Dictionary<IOutputStationInfo, TextBlock>();
        private Dictionary<IOutputStationInfo, TextBlock> _blocksActiveOStations = new Dictionary<IOutputStationInfo, TextBlock>();
        private Dictionary<IInputStationInfo, TextBlock> _blocksActiveIStations = new Dictionary<IInputStationInfo, TextBlock>();
        private Dictionary<IOutputStationInfo, TextBlock> _blocksBlockedOStations = new Dictionary<IOutputStationInfo, TextBlock>();
        private Dictionary<IInputStationInfo, TextBlock> _blocksBlockedIStations = new Dictionary<IInputStationInfo, TextBlock>();
        private SimulationVisualOrderManager _orderManager;

        public SimulationInfoInstance(TreeView infoHost, IInstanceInfo instance) : base(infoHost) { _instance = instance; }

        public override void InfoPanelUpdate()
        {
            // Update all info
            _blockPendingBundles.Text = _instance.GetInfoItemManager().GetInfoPendingBundleCount().ToString();
            _blockPendingOrders.Text = _instance.GetInfoItemManager().GetInfoPendingOrderCount().ToString();
            _blockStatItems.Text = _instance.GetInfoStatItemsHandled().ToString();
            _blockStatBundles.Text = _instance.GetInfoStatBundlesHandled().ToString();
            _blockStatOrders.Text = _instance.GetInfoStatOrdersHandled().ToString();
            _blockStatOrdersLate.Text = _instance.GetInfoStatOrdersLate().ToString();
            _blockStatRepositioningMoves.Text = _instance.GetInfoStatRepositioningMoves().ToString();
            _blockStatCollisions.Text = _instance.GetInfoStatCollisions().ToString();
            _blockStatStorageFillLevel.Text =
                (_instance.GetInfoStatStorageFillLevel() * 100).ToString("F1", IOConstants.FORMATTER) + "% " +
                (_instance.GetInfoStatStorageFillAndReservedLevel() * 100).ToString("F1", IOConstants.FORMATTER) + "% " +
                (_instance.GetInfoStatStorageFillAndReservedAndBacklogLevel() * 100).ToString("F1", IOConstants.FORMATTER) + "%";
            foreach (var station in _instance.GetInfoTiers().SelectMany(t => t.GetInfoInputStations()))
            {
                _blocksAssignedBundles[station].Text = station.GetInfoAssignedBundles().ToString();
                _blocksOpenInsertRequests[station].Text = station.GetInfoOpenRequests().ToString() + "/" + station.GetInfoOpenBundles();
            }
            foreach (var station in _instance.GetInfoTiers().SelectMany(t => t.GetInfoOutputStations()))
            {
                _blocksAssignedOrders[station].Text = station.GetInfoAssignedOrders().ToString();
                _blocksOpenExtractRequests[station].Text = station.GetInfoOpenRequests().ToString() + "/" + station.GetInfoOpenItems();
                _blocksOpenQueuedExtractRequests[station].Text = station.GetInfoOpenQueuedRequests().ToString() + "/" + station.GetInfoOpenQueuedItems();
            }
            foreach (var station in _instance.GetInfoTiers().SelectMany(t => t.GetInfoInputStations()))
                if (station.GetInfoActive() && _blocksActiveIStations[station].Background != _colorStationActive)
                    _blocksActiveIStations[station].Background = _colorStationActive;
                else if (!station.GetInfoActive() && _blocksActiveIStations[station].Background != _colorStationInactive)
                    _blocksActiveIStations[station].Background = _colorStationInactive;
            foreach (var station in _instance.GetInfoTiers().SelectMany(t => t.GetInfoOutputStations()))
                if (station.GetInfoActive() && _blocksActiveOStations[station].Background != _colorStationActive)
                    _blocksActiveOStations[station].Background = _colorStationActive;
                else if (!station.GetInfoActive() && _blocksActiveOStations[station].Background != _colorStationInactive)
                    _blocksActiveOStations[station].Background = _colorStationInactive;
            foreach (var station in _instance.GetInfoTiers().SelectMany(t => t.GetInfoInputStations()))
                if (station.GetInfoBlocked() && _blocksBlockedIStations[station].Background != _colorStationBlocked)
                    _blocksBlockedIStations[station].Background = _colorStationBlocked;
                else if (!station.GetInfoBlocked() && _blocksBlockedIStations[station].Background != _colorStationFree)
                    _blocksBlockedIStations[station].Background = _colorStationFree;
            foreach (var station in _instance.GetInfoTiers().SelectMany(t => t.GetInfoOutputStations()))
                if (station.GetInfoBlocked() && _blocksBlockedOStations[station].Background != _colorStationBlocked)
                    _blocksBlockedOStations[station].Background = _colorStationBlocked;
                else if (!station.GetInfoBlocked() && _blocksBlockedOStations[station].Background != _colorStationFree)
                    _blocksBlockedOStations[station].Background = _colorStationFree;
            _orderManager.Update(_instance.GetInfoItemManager().GetInfoOpenOrders(), _instance.GetInfoItemManager().GetInfoCompletedOrders());
        }

        public override void InfoPanelInit()
        {
            // Prepare information controls
            _infoHost.Items.Clear();
            // Init if not already done
            if (_root == null)
            {
                // Init root node
                _root = new TreeViewItem { Header = "Instance" };
                // --> Add static information
                // Add bot count info
                WrapPanel botCountPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                botCountPanel.Children.Add(new TextBlock { Text = "Bots: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                botCountPanel.Children.Add(new TextBlock { Text = _instance.GetInfoBots().Count().ToString(), MinWidth = _infoPanelRightColumnWidth });
                _root.Items.Add(botCountPanel);
                // Add pod count info
                WrapPanel podCountPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                podCountPanel.Children.Add(new TextBlock { Text = "Pods: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                podCountPanel.Children.Add(new TextBlock { Text = _instance.GetInfoPods().Count().ToString(), MinWidth = _infoPanelRightColumnWidth });
                _root.Items.Add(podCountPanel);
                // Add input station count info
                WrapPanel iStationCountPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                iStationCountPanel.Children.Add(new TextBlock { Text = "Input-stations: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                iStationCountPanel.Children.Add(new TextBlock { Text = _instance.GetInfoTiers().Sum(t => t.GetInfoInputStations().Count()).ToString(), MinWidth = _infoPanelRightColumnWidth });
                _root.Items.Add(iStationCountPanel);
                // Add output station count info
                WrapPanel oStationCountPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                oStationCountPanel.Children.Add(new TextBlock { Text = "Output-stations: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                oStationCountPanel.Children.Add(new TextBlock { Text = _instance.GetInfoTiers().Sum(t => t.GetInfoOutputStations().Count()).ToString(), MinWidth = _infoPanelRightColumnWidth });
                _root.Items.Add(oStationCountPanel);
                // Add waypoint count info
                WrapPanel waypointCountPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                waypointCountPanel.Children.Add(new TextBlock { Text = "Waypoints: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                waypointCountPanel.Children.Add(new TextBlock { Text = _instance.GetInfoTiers().Sum(t => t.GetInfoWaypoints().Count()).ToString(), MinWidth = _infoPanelRightColumnWidth });
                _root.Items.Add(waypointCountPanel);
                // Add tier sizes info
                WrapPanel tierSizesPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                tierSizesPanel.Children.Add(new TextBlock { Text = "Tier sizes (LxW): ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                tierSizesPanel.Children.Add(new TextBlock { Text = string.Join(",", _instance.GetInfoTiers().Select(t => $"{t.GetInfoLength():F0}x{t.GetInfoWidth():F0}")), MinWidth = _infoPanelRightColumnWidth });
                _root.Items.Add(tierSizesPanel);
                // --> Add dynamic information
                // Add pending bundles info
                WrapPanel pendingBundlesPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                pendingBundlesPanel.Children.Add(new TextBlock { Text = "Bundles (pending): ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockPendingBundles = new TextBlock { Text = _instance.GetInfoItemManager().GetInfoPendingBundleCount().ToString(), MinWidth = _infoPanelRightColumnWidth };
                pendingBundlesPanel.Children.Add(_blockPendingBundles);
                _root.Items.Add(pendingBundlesPanel);
                // Add pending orders info
                WrapPanel pendingOrdersPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                pendingOrdersPanel.Children.Add(new TextBlock { Text = "Orders (pending): ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockPendingOrders = new TextBlock { Text = _instance.GetInfoItemManager().GetInfoPendingOrderCount().ToString(), MinWidth = _infoPanelRightColumnWidth };
                pendingOrdersPanel.Children.Add(_blockPendingOrders);
                _root.Items.Add(pendingOrdersPanel);
                // Add stats items handled
                WrapPanel itemsPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                itemsPanel.Children.Add(new TextBlock { Text = "Items: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockStatItems = new TextBlock { Text = _instance.GetInfoStatItemsHandled().ToString(), MinWidth = _infoPanelRightColumnWidth };
                itemsPanel.Children.Add(_blockStatItems);
                _root.Items.Add(itemsPanel);
                // Add stats bundles handled
                WrapPanel bundlesPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                bundlesPanel.Children.Add(new TextBlock { Text = "Bundles: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockStatBundles = new TextBlock { Text = _instance.GetInfoStatBundlesHandled().ToString(), MinWidth = _infoPanelRightColumnWidth };
                bundlesPanel.Children.Add(_blockStatBundles);
                _root.Items.Add(bundlesPanel);
                // Add stats orders handled
                WrapPanel ordersPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                ordersPanel.Children.Add(new TextBlock { Text = "Orders: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockStatOrders = new TextBlock { Text = _instance.GetInfoStatOrdersHandled().ToString(), MinWidth = _infoPanelRightColumnWidth };
                ordersPanel.Children.Add(_blockStatOrders);
                _root.Items.Add(ordersPanel);
                // Add stats orders handled
                WrapPanel ordersLatePanel = new WrapPanel { Orientation = Orientation.Horizontal };
                ordersLatePanel.Children.Add(new TextBlock { Text = "Orders (late): ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockStatOrdersLate = new TextBlock { Text = _instance.GetInfoStatOrdersLate().ToString(), MinWidth = _infoPanelRightColumnWidth };
                ordersLatePanel.Children.Add(_blockStatOrdersLate);
                _root.Items.Add(ordersLatePanel);
                // Add stats orders handled
                WrapPanel repositioningsPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                repositioningsPanel.Children.Add(new TextBlock { Text = "Repositionings: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockStatRepositioningMoves = new TextBlock { Text = _instance.GetInfoStatRepositioningMoves().ToString(), MinWidth = _infoPanelRightColumnWidth };
                repositioningsPanel.Children.Add(_blockStatRepositioningMoves);
                _root.Items.Add(repositioningsPanel);
                // Add stats collisions occurred
                WrapPanel collisionsPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                collisionsPanel.Children.Add(new TextBlock { Text = "Collisions: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockStatCollisions = new TextBlock { Text = _instance.GetInfoStatCollisions().ToString(), MinWidth = _infoPanelRightColumnWidth };
                collisionsPanel.Children.Add(_blockStatCollisions);
                _root.Items.Add(collisionsPanel);

                // Add stats storage fill level
                WrapPanel storageFillLevelPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                storageFillLevelPanel.Children.Add(new TextBlock { Text = "Storage fill level: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockStatStorageFillLevel = new TextBlock
                {
                    Text =
                        (_instance.GetInfoStatStorageFillLevel() * 100).ToString("F1", IOConstants.FORMATTER) + "% " +
                        (_instance.GetInfoStatStorageFillAndReservedLevel() * 100).ToString("F1", IOConstants.FORMATTER) + "% " +
                        (_instance.GetInfoStatStorageFillAndReservedAndBacklogLevel() * 100).ToString("F1", IOConstants.FORMATTER) + "%",
                    MinWidth = _infoPanelRightColumnWidth
                };
                storageFillLevelPanel.Children.Add(_blockStatStorageFillLevel);
                _root.Items.Add(storageFillLevelPanel);

                // Add request info
                StackPanel insertRequestsPanel = new StackPanel { Orientation = Orientation.Horizontal };
                insertRequestsPanel.Children.Add(new TextBlock { Text = "Ass. i-requests:", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                insertRequestsPanel.Children.Add(new TextBlock { MinWidth = 2 });
                foreach (var station in _instance.GetInfoTiers().SelectMany(t => t.GetInfoInputStations()))
                {
                    TextBlock block = new TextBlock
                    {
                        Text = station.GetInfoOpenRequests().ToString() + "/" + station.GetInfoOpenBundles(),
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(2, 0, 2, 0),
                    };
                    _blocksOpenInsertRequests[station] = block;
                    insertRequestsPanel.Children.Add(block);
                }
                _root.Items.Add(insertRequestsPanel);
                StackPanel extractRequestsPanel = new StackPanel { Orientation = Orientation.Horizontal };
                extractRequestsPanel.Children.Add(new TextBlock { Text = "Ass. e-requests:", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                extractRequestsPanel.Children.Add(new TextBlock { MinWidth = 2 });
                foreach (var station in _instance.GetInfoTiers().SelectMany(t => t.GetInfoOutputStations()))
                {
                    TextBlock block = new TextBlock
                    {
                        Text = station.GetInfoOpenRequests().ToString() + "/" + station.GetInfoOpenItems(),
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(2, 0, 2, 0),
                    };
                    _blocksOpenExtractRequests[station] = block;
                    extractRequestsPanel.Children.Add(block);
                }
                _root.Items.Add(extractRequestsPanel);
                StackPanel queuedExtractRequestsPanel = new StackPanel { Orientation = Orientation.Horizontal };
                queuedExtractRequestsPanel.Children.Add(new TextBlock { Text = "Queued e-requests:", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                queuedExtractRequestsPanel.Children.Add(new TextBlock { MinWidth = 2 });
                foreach (var station in _instance.GetInfoTiers().SelectMany(t => t.GetInfoOutputStations()))
                {
                    TextBlock block = new TextBlock
                    {
                        Text = station.GetInfoOpenQueuedRequests().ToString() + "/" + station.GetInfoOpenQueuedItems(),
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(2, 0, 2, 0),
                    };
                    _blocksOpenQueuedExtractRequests[station] = block;
                    queuedExtractRequestsPanel.Children.Add(block);
                }
                _root.Items.Add(queuedExtractRequestsPanel);

                // Add assigned bundles and orders info
                StackPanel assignedBundlesPanel = new StackPanel { Orientation = Orientation.Horizontal };
                assignedBundlesPanel.Children.Add(new TextBlock { Text = "Ass. bundles:", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                assignedBundlesPanel.Children.Add(new TextBlock { MinWidth = 2 });
                foreach (var station in _instance.GetInfoTiers().SelectMany(t => t.GetInfoInputStations()))
                {
                    TextBlock stationAssignedBlock = new TextBlock
                    {
                        Text = station.GetInfoAssignedBundles().ToString(),
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(2, 0, 2, 0),
                    };
                    _blocksAssignedBundles[station] = stationAssignedBlock;
                    assignedBundlesPanel.Children.Add(stationAssignedBlock);
                }
                _root.Items.Add(assignedBundlesPanel);
                StackPanel assignedOrdersPanel = new StackPanel { Orientation = Orientation.Horizontal };
                assignedOrdersPanel.Children.Add(new TextBlock { Text = "Ass. orders:", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                assignedOrdersPanel.Children.Add(new TextBlock { MinWidth = 2 });
                foreach (var station in _instance.GetInfoTiers().SelectMany(t => t.GetInfoOutputStations()))
                {
                    TextBlock stationAssignedBlock = new TextBlock
                    {
                        Text = station.GetInfoAssignedOrders().ToString(),
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(2, 0, 2, 0),
                    };
                    _blocksAssignedOrders[station] = stationAssignedBlock;
                    assignedOrdersPanel.Children.Add(stationAssignedBlock);
                }
                _root.Items.Add(assignedOrdersPanel);

                // Add active stations info
                StackPanel activeIStationsPanel = new StackPanel { Orientation = Orientation.Horizontal };
                activeIStationsPanel.Children.Add(new TextBlock { Text = "Input-stations:", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                activeIStationsPanel.Children.Add(new TextBlock { MinWidth = 2 });
                foreach (var station in _instance.GetInfoTiers().SelectMany(t => t.GetInfoInputStations()))
                {
                    TextBlock stationActiveBlock = new TextBlock
                    {
                        Text = station.GetInfoID().ToString(),
                        TextAlignment = TextAlignment.Center,
                        MinWidth = _infoPanelSingleSmallElementWidth,
                        Background = Brushes.Gray
                    };
                    _blocksActiveIStations[station] = stationActiveBlock;
                    activeIStationsPanel.Children.Add(stationActiveBlock);
                }
                _root.Items.Add(activeIStationsPanel);
                StackPanel activeOStationsPanel = new StackPanel { Orientation = Orientation.Horizontal };
                activeOStationsPanel.Children.Add(new TextBlock { Text = "Output-stations:", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                activeOStationsPanel.Children.Add(new TextBlock { MinWidth = 2 });
                foreach (var station in _instance.GetInfoTiers().SelectMany(t => t.GetInfoOutputStations()))
                {
                    TextBlock stationActiveBlock = new TextBlock
                    {
                        Text = station.GetInfoID().ToString(),
                        TextAlignment = TextAlignment.Center,
                        MinWidth = _infoPanelSingleSmallElementWidth,
                        Background = Brushes.Gray
                    };
                    _blocksActiveOStations[station] = stationActiveBlock;
                    activeOStationsPanel.Children.Add(stationActiveBlock);
                }
                _root.Items.Add(activeOStationsPanel);

                // Add blocked stations info
                StackPanel blockedIStationsPanel = new StackPanel { Orientation = Orientation.Horizontal };
                blockedIStationsPanel.Children.Add(new TextBlock { Text = "Input-stations:", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                blockedIStationsPanel.Children.Add(new TextBlock { MinWidth = 2 });
                foreach (var station in _instance.GetInfoTiers().SelectMany(t => t.GetInfoInputStations()))
                {
                    TextBlock stationBlockedBlock = new TextBlock
                    {
                        Text = station.GetInfoID().ToString(),
                        TextAlignment = TextAlignment.Center,
                        MinWidth = _infoPanelSingleSmallElementWidth,
                        Background = Brushes.Gray
                    };
                    _blocksBlockedIStations[station] = stationBlockedBlock;
                    blockedIStationsPanel.Children.Add(stationBlockedBlock);
                }
                _root.Items.Add(blockedIStationsPanel);
                StackPanel blockedOStationsPanel = new StackPanel { Orientation = Orientation.Horizontal };
                blockedOStationsPanel.Children.Add(new TextBlock { Text = "Output-stations:", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                blockedOStationsPanel.Children.Add(new TextBlock { MinWidth = 2 });
                foreach (var station in _instance.GetInfoTiers().SelectMany(t => t.GetInfoOutputStations()))
                {
                    TextBlock stationBlockedBlock = new TextBlock
                    {
                        Text = station.GetInfoID().ToString(),
                        TextAlignment = TextAlignment.Center,
                        MinWidth = _infoPanelSingleSmallElementWidth,
                        Background = Brushes.Gray
                    };
                    _blocksBlockedOStations[station] = stationBlockedBlock;
                    blockedOStationsPanel.Children.Add(stationBlockedBlock);
                }
                _root.Items.Add(blockedOStationsPanel);

                // Init order list root nodes
                TreeViewItem openOrderListItem = new TreeViewItem { };
                TreeViewItem completedOrderListItem = new TreeViewItem { IsExpanded = true };
                _root.Items.Add(openOrderListItem);
                _root.Items.Add(completedOrderListItem);
                // Add order list
                _orderManager = new SimulationVisualOrderManager(openOrderListItem, "OpenOrders", completedOrderListItem, "CompleteOrders", 40);
                _orderManager.Update(_instance.GetInfoItemManager().GetInfoOpenOrders(), _instance.GetInfoItemManager().GetInfoCompletedOrders());
            }
            // Expand root node
            _infoHost.Items.Add(_root);
            _root.IsExpanded = true;
        }
    }

    #endregion

    #region InputStation

    public class SimulationInfoInputStation : SimulationInfoObject
    {
        private readonly IInputStationInfo _iStation;
        private readonly int _infoPanelLeftColumnWidth = 105;
        private readonly int _infoPanelRightColumnWidth = 60;
        private TreeViewItem _root;
        private TreeViewItem _treeItemContent;
        private TextBlock _blockXY;
        private TextBlock _blockCapacity;
        private TextBlock _blockCapacityReserved;
        private TextBlock _blockActivationOrderID;
        private TextBlock _blockQueueCapacity;
        private TextBlock _blockBlocked;
        private TextBlock _blockBlockedLeft;
        private TextBlock _blockOpenRequests;
        private SimulationVisualBundleManager _bundleManager;

        public SimulationInfoInputStation(TreeView infoHost, IInputStationInfo iStation) : base(infoHost) { _iStation = iStation; }

        public override void InfoPanelUpdate()
        {
            // Update all info (no need to update x/y - it's not changeable)
            _blockCapacity.Text =
                _iStation.GetInfoCapacityUsed().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "/" +
                _iStation.GetInfoCapacity().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER);
            _blockCapacityReserved.Text =
                _iStation.GetInfoCapacityReserved().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "/" +
                _iStation.GetInfoCapacity().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER);
            double blockedUntil = _iStation.GetInfoBlockedLeft();
            _blockBlockedLeft.Text = double.IsNaN(blockedUntil) || double.IsPositiveInfinity(blockedUntil) || blockedUntil < 0 ? "n/a" : TimeSpan.FromSeconds(blockedUntil).ToString(IOConstants.TIMESPAN_FORMAT_HUMAN_READABLE_MINUTES);
            _blockOpenRequests.Text = _iStation.GetInfoOpenRequests().ToString() + " / " + _iStation.GetInfoOpenBundles().ToString();
            // Update content info
            _bundleManager.UpdateContentInfo(_iStation.GetInfoBundles().ToArray());
        }

        public override void InfoPanelInit()
        {
            // Visually emphasize focus on element
            ManagedVisual2D.StrokeThickness = StrokeThicknessFocused;
            // Prepare information controls
            _infoHost.Items.Clear();
            // Init if not already done
            if (_root == null)
            {
                // Init root node
                _root = new TreeViewItem { Header = "InputStation" + _iStation.GetInfoID() };
                // Add position
                WrapPanel xyPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                xyPanel.Children.Add(new TextBlock { Text = "X/Y: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockXY = new TextBlock
                {
                    Text =
                    _iStation.GetInfoCenterX().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "/" +
                    _iStation.GetInfoCenterY().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER),
                    MinWidth = _infoPanelRightColumnWidth
                };
                xyPanel.Children.Add(_blockXY);
                _root.Items.Add(xyPanel);
                // Add capacity
                WrapPanel capacityPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                capacityPanel.Children.Add(new TextBlock { Text = "Capacity: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockCapacity = new TextBlock
                {
                    Text = _iStation.GetInfoCapacityUsed().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "/" +
                    _iStation.GetInfoCapacity().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER),
                    MinWidth = _infoPanelRightColumnWidth,
                };
                capacityPanel.Children.Add(_blockCapacity);
                _root.Items.Add(capacityPanel);
                // Add reserved capacity
                WrapPanel capacityReservedPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                capacityReservedPanel.Children.Add(new TextBlock { Text = "Capacity Reserved: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockCapacityReserved = new TextBlock
                {
                    Text = _iStation.GetInfoCapacityReserved().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "/" +
                    _iStation.GetInfoCapacity().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER),
                    MinWidth = _infoPanelRightColumnWidth,
                };
                capacityReservedPanel.Children.Add(_blockCapacityReserved);
                _root.Items.Add(capacityReservedPanel);
                // Add activation ID
                WrapPanel activationPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                activationPanel.Children.Add(new TextBlock { Text = "Activation ID: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockActivationOrderID = new TextBlock
                {
                    Text = _iStation.GetInfoActivationOrderID().ToString(),
                    MinWidth = _infoPanelRightColumnWidth,
                };
                activationPanel.Children.Add(_blockActivationOrderID);
                _root.Items.Add(activationPanel);
                // Add Queue
                WrapPanel queuePanel = new WrapPanel { Orientation = Orientation.Horizontal };
                queuePanel.Children.Add(new TextBlock { Text = "Queue: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockQueueCapacity = new TextBlock
                {
                    Text = _iStation.GetInfoQueue(),
                    MinWidth = _infoPanelRightColumnWidth,
                };
                queuePanel.Children.Add(_blockQueueCapacity);
                _root.Items.Add(queuePanel);
                // Add blocked status
                WrapPanel blockedPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                blockedPanel.Children.Add(new TextBlock { Text = "Blocked: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockBlocked = new TextBlock
                {
                    Text = _iStation.GetInfoBlocked().ToString(),
                    MinWidth = _infoPanelRightColumnWidth
                };
                blockedPanel.Children.Add(_blockBlocked);
                _root.Items.Add(blockedPanel);
                // Add blocked time remaining
                WrapPanel blockedUntilPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                blockedUntilPanel.Children.Add(new TextBlock { Text = "Blocked until: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockBlockedLeft = new TextBlock
                {
                    Text = "n/a",
                    MinWidth = _infoPanelRightColumnWidth
                };
                blockedUntilPanel.Children.Add(_blockBlockedLeft);
                _root.Items.Add(blockedUntilPanel);
                // Add open requests
                WrapPanel openRequestsPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                openRequestsPanel.Children.Add(new TextBlock { Text = "Requests: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockOpenRequests = new TextBlock
                {
                    Text = _iStation.GetInfoOpenRequests().ToString() + " / " + _iStation.GetInfoOpenBundles().ToString(),
                    MinWidth = _infoPanelRightColumnWidth,
                };
                openRequestsPanel.Children.Add(_blockOpenRequests);
                _root.Items.Add(openRequestsPanel);
                // Add content
                if (_treeItemContent == null)
                {
                    _treeItemContent = new TreeViewItem { Header = "Content" };
                    _treeItemContent.IsExpanded = true;
                    _bundleManager = new SimulationVisualBundleManager(_treeItemContent);
                }
                _root.Items.Add(_treeItemContent);
            }
            // Update content info
            _bundleManager.UpdateContentInfo(_iStation.GetInfoBundles().ToArray());
            // Expand root node
            _infoHost.Items.Add(_root);
            _root.IsExpanded = true;
        }
    }

    #endregion

    #region OutputStation

    public class SimulationInfoOutputStation : SimulationInfoObject
    {
        private readonly IOutputStationInfo _oStation;
        private readonly int _infoPanelLeftColumnWidth = 80;
        private readonly int _infoPanelRightColumnWidth = 60;
        private TreeViewItem _root;
        private TextBlock _blockXY;
        private TextBlock _blockCapacity;
        private TextBlock _blockActivationOrderID;
        private TextBlock _blockQueueCapacity;
        private TextBlock _blockBlocked;
        private TextBlock _blockBlockedLeft;
        private TextBlock _blockOpenRequests;
        private TextBlock _blockInboundPods;
        private SimulationVisualOrderManager _orderManager;

        public SimulationInfoOutputStation(TreeView infoHost, IOutputStationInfo oStation) : base(infoHost) { _oStation = oStation; }

        public override void InfoPanelUpdate()
        {
            // Update all info (no need to update x/y - it's not changeable)
            _blockCapacity.Text =
                _oStation.GetInfoCapacityUsed().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "/" +
                _oStation.GetInfoCapacity().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER);
            _blockBlocked.Text = _oStation.GetInfoBlocked().ToString();
            double blockedUntil = _oStation.GetInfoBlockedLeft();
            _blockBlockedLeft.Text = double.IsNaN(blockedUntil) || double.IsPositiveInfinity(blockedUntil) || blockedUntil < 0 ? "n/a" : TimeSpan.FromSeconds(blockedUntil).ToString(IOConstants.TIMESPAN_FORMAT_HUMAN_READABLE_MINUTES);
            _blockOpenRequests.Text = _oStation.GetInfoOpenRequests().ToString() + " / " + _oStation.GetInfoOpenItems().ToString();
            _blockInboundPods.Text = _oStation.GetInfoInboundPods().ToString();
            // Update content info
            _orderManager.Update(_oStation.GetInfoOpenOrders(), _oStation.GetInfoCompletedOrders());
        }

        public override void InfoPanelInit()
        {
            // Visually emphasize focus on element
            ManagedVisual2D.StrokeThickness = StrokeThicknessFocused;
            // Prepare information controls
            _infoHost.Items.Clear();
            // Init if not already done
            if (_root == null)
            {
                // Init root node
                _root = new TreeViewItem { Header = "OutputStation" + _oStation.GetInfoID() };
                // Add position
                WrapPanel xyPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                xyPanel.Children.Add(new TextBlock { Text = "X/Y: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockXY = new TextBlock
                {
                    Text =
                        _oStation.GetInfoCenterX().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "/" +
                        _oStation.GetInfoCenterY().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER),
                    MinWidth = _infoPanelRightColumnWidth
                };
                xyPanel.Children.Add(_blockXY);
                _root.Items.Add(xyPanel);
                // Add capacity
                WrapPanel capacityPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                capacityPanel.Children.Add(new TextBlock { Text = "Capacity: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockCapacity = new TextBlock
                {
                    Text =
                        _oStation.GetInfoCapacityUsed().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "/" +
                        _oStation.GetInfoCapacity().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER),
                    MinWidth = _infoPanelRightColumnWidth
                };
                capacityPanel.Children.Add(_blockCapacity);
                _root.Items.Add(capacityPanel);
                // Add activation ID
                WrapPanel activationPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                activationPanel.Children.Add(new TextBlock { Text = "Activation ID: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockActivationOrderID = new TextBlock
                {
                    Text = _oStation.GetInfoActivationOrderID().ToString(),
                    MinWidth = _infoPanelRightColumnWidth,
                };
                activationPanel.Children.Add(_blockActivationOrderID);
                _root.Items.Add(activationPanel);
                // Add Queue
                WrapPanel queuePanel = new WrapPanel { Orientation = Orientation.Horizontal };
                queuePanel.Children.Add(new TextBlock { Text = "Queue: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockQueueCapacity = new TextBlock
                {
                    Text = _oStation.GetInfoQueue(),
                    MinWidth = _infoPanelRightColumnWidth,
                };
                queuePanel.Children.Add(_blockQueueCapacity);
                _root.Items.Add(queuePanel);
                // Add blocked status
                WrapPanel blockedPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                blockedPanel.Children.Add(new TextBlock { Text = "Blocked: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockBlocked = new TextBlock
                {
                    Text = _oStation.GetInfoBlocked().ToString(),
                    MinWidth = _infoPanelRightColumnWidth
                };
                blockedPanel.Children.Add(_blockBlocked);
                _root.Items.Add(blockedPanel);
                // Add blocked time remaining
                WrapPanel blockedUntilPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                blockedUntilPanel.Children.Add(new TextBlock { Text = "Blocked until: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockBlockedLeft = new TextBlock
                {
                    Text = "n/a",
                    MinWidth = _infoPanelRightColumnWidth
                };
                blockedUntilPanel.Children.Add(_blockBlockedLeft);
                _root.Items.Add(blockedUntilPanel);
                // Add open requests
                WrapPanel openRequestsPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                openRequestsPanel.Children.Add(new TextBlock { Text = "Requests: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockOpenRequests = new TextBlock
                {
                    Text = _oStation.GetInfoOpenRequests().ToString() + " / " + _oStation.GetInfoOpenItems().ToString(),
                    MinWidth = _infoPanelRightColumnWidth,
                };
                openRequestsPanel.Children.Add(_blockOpenRequests);
                _root.Items.Add(openRequestsPanel);
                // Add open requests
                WrapPanel inboundPodsPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                inboundPodsPanel.Children.Add(new TextBlock { Text = "Inbound pods: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockInboundPods = new TextBlock
                {
                    Text = "0",
                    MinWidth = _infoPanelRightColumnWidth,
                };
                inboundPodsPanel.Children.Add(_blockInboundPods);
                _root.Items.Add(inboundPodsPanel);
                // Init order list root nodes
                TreeViewItem openOrderListItem = new TreeViewItem { IsExpanded = true };
                TreeViewItem completedOrderListItem = new TreeViewItem { IsExpanded = true };
                _root.Items.Add(openOrderListItem);
                _root.Items.Add(completedOrderListItem);
                // Add order list
                _orderManager = new SimulationVisualOrderManager(openOrderListItem, "OpenOrders", completedOrderListItem, "CompleteOrders", 30);
                _orderManager.Update(_oStation.GetInfoOpenOrders(), _oStation.GetInfoCompletedOrders());
            }
            // Expand root node
            _infoHost.Items.Add(_root);
            _root.IsExpanded = true;
        }
    }

    #endregion

    #region ElevatorEntrance

    public class SimulationInfoElevatorEntrance : SimulationInfoObject
    {
        private readonly IElevatorInfo _elevatorInfo;
        private readonly int _infoPanelLeftColumnWidth = 80;
        private readonly int _infoPanelRightColumnWidth = 60;
        private TreeViewItem _root;
        private TextBlock _blockWaypoint;

        public SimulationInfoElevatorEntrance(TreeView infoHost, IElevatorInfo elevatorInfo) : base(infoHost) { _elevatorInfo = elevatorInfo; }

        public override void InfoPanelUpdate()
        {
            // Update all info (no need to update x/y - it's not changeable)
        }

        public override void InfoPanelInit()
        {
            // Visually emphasize focus on element
            ManagedVisual2D.StrokeThickness = StrokeThicknessFocused;
            // Prepare information controls
            _infoHost.Items.Clear();
            // Init if not already done
            if (_root == null)
            {
                // Init root node
                _root = new TreeViewItem { Header = "Elevator" + _elevatorInfo.GetInfoID() };
                // Add waypoint
                WrapPanel waypointPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                waypointPanel.Children.Add(new TextBlock { Text = "Waypoints: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockWaypoint = new TextBlock
                {
                    Text = string.Join(",", _elevatorInfo.GetInfoWaypoints().Select(wp => wp.GetInfoID())),
                    MinWidth = _infoPanelRightColumnWidth
                };
                waypointPanel.Children.Add(_blockWaypoint);
                _root.Items.Add(waypointPanel);
                // Add entrance // TODO show coordinates of selected entrance
                //WrapPanel entrancePanel = new WrapPanel { Orientation = Orientation.Horizontal };
                //entrancePanel.Children.Add(new TextBlock { Text = "Entrance: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                //_blockEntrance = new TextBlock
                //{
                //    Text =
                //        _entranceInfo.GetInfoCenterX().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "/" +
                //        _entranceInfo.GetInfoCenterY().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER),
                //    MinWidth = _infoPanelRightColumnWidth
                //};
                //entrancePanel.Children.Add(_blockEntrance);
                //_root.Items.Add(entrancePanel);
            }
            // Expand root node
            _infoHost.Items.Add(_root);
            _root.IsExpanded = true;
        }
    }

    #endregion

    #region Bot

    public class SimulationInfoBot : SimulationInfoObject
    {
        private readonly IBotInfo _bot;
        private readonly int _infoPanelLeftColumnWidth = 80;
        private readonly int _infoPanelRightColumnWidth = 60;
        private TextBlock _blockXY;
        private TextBlock _blockOrientation;
        private TextBlock _blockTargetOrientation;
        private TextBlock _blockCurrentWaypoint;
        private TextBlock _blockDestinationWaypoint;
        private TextBlock _blockGoalWaypoint;
        private TextBlock _blockSpeed;
        private TextBlock _blockBlocked;
        private TextBlock _blockBlockedUntil;
        private TextBlock _blockQueueing;
        private TextBlock _blockState;
        private TextBlock _blockPath;

        public SimulationInfoBot(TreeView infoHost, IBotInfo bot) : base(infoHost) { _bot = bot; }

        public override void InfoPanelUpdate()
        {
            // Update all info
            _blockXY.Text = _bot.GetInfoCenterX().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "/" + _bot.GetInfoCenterY().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER);
            _blockOrientation.Text = Transformation2D.ProjectOrientation(_bot.GetInfoOrientation()).ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "°";
            _blockTargetOrientation.Text = Transformation2D.ProjectOrientation(_bot.GetInfoTargetOrientation()).ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "°";
            IWaypointInfo currentWP = _bot.GetInfoCurrentWaypoint(); IWaypointInfo destinationWP = _bot.GetInfoDestinationWaypoint(); IWaypointInfo goalWP = _bot.GetInfoGoalWaypoint();
            _blockCurrentWaypoint.Text = currentWP == null ? "none" :
                currentWP.GetInfoID().ToString() + " (" +
                currentWP.GetInfoCenterX().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "," +
                currentWP.GetInfoCenterY().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + ")";
            _blockDestinationWaypoint.Text = destinationWP == null ? "none" :
                destinationWP.GetInfoID().ToString() + " (" +
                destinationWP.GetInfoCenterX().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "," +
                destinationWP.GetInfoCenterY().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + ")";
            _blockGoalWaypoint.Text = goalWP == null ? "none" :
                goalWP.GetInfoID().ToString() + " (" +
                goalWP.GetInfoCenterX().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "," +
                goalWP.GetInfoCenterY().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + ")";
            _blockSpeed.Text = _bot.GetInfoSpeed().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + " m/s";
            _blockBlocked.Text = _bot.GetInfoBlocked().ToString();
            double blockedUntil = _bot.GetInfoBlockedLeft();
            _blockBlockedUntil.Text = double.IsNaN(blockedUntil) || double.IsPositiveInfinity(blockedUntil) || blockedUntil < 0 ? "n/a" : TimeSpan.FromSeconds(blockedUntil).ToString(IOConstants.TIMESPAN_FORMAT_HUMAN_READABLE_MINUTES);
            _blockQueueing.Text = _bot.GetInfoIsQueueing().ToString();
            _blockState.Text = _bot.GetInfoState();
            List<IWaypointInfo> path = _bot.GetInfoPath();
            if (path != null && path.Any() && _bot.GetInfoState() == "Move")
                _blockPath.Text = string.Join(Environment.NewLine, path.Select(w =>
                    "Waypoint" + w.GetInfoID() + "-(" +
                        w.GetInfoCenterX().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "," +
                        w.GetInfoCenterY().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + ")"));
            else
                _blockPath.Text = "<empty>";
        }

        public override void InfoPanelInit()
        {
            // Visually emphasize focus on element
            ManagedVisual2D.StrokeThickness = StrokeThicknessFocused;
            // Prepare information controls
            _infoHost.Items.Clear();
            TreeViewItem root = new TreeViewItem { Header = "Bot" + _bot.GetInfoID() };
            _infoHost.Items.Add(root);
            // Add position
            WrapPanel xyPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            xyPanel.Children.Add(new TextBlock { Text = "X/Y: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
            _blockXY = new TextBlock
            {
                Text =
                _bot.GetInfoCenterX().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "/" +
                _bot.GetInfoCenterY().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER),
                MinWidth = _infoPanelRightColumnWidth
            };
            xyPanel.Children.Add(_blockXY);
            root.Items.Add(xyPanel);
            // Add orientation
            WrapPanel orientationPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            orientationPanel.Children.Add(new TextBlock { Text = "Orientation: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
            _blockOrientation = new TextBlock
            {
                Text = Transformation2D.ProjectOrientation(_bot.GetInfoOrientation()).ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "°",
                MinWidth = _infoPanelRightColumnWidth,
            };
            orientationPanel.Children.Add(_blockOrientation);
            root.Items.Add(orientationPanel);
            // Add orientation target
            WrapPanel orientationTargetPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            orientationTargetPanel.Children.Add(new TextBlock { Text = "Target Orientation: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
            _blockTargetOrientation = new TextBlock
            {
                Text = Transformation2D.ProjectOrientation(_bot.GetInfoTargetOrientation()).ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "°",
                MinWidth = _infoPanelRightColumnWidth,
            };
            orientationTargetPanel.Children.Add(_blockTargetOrientation);
            root.Items.Add(orientationTargetPanel);
            // Add target
            WrapPanel currentWaypointPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            currentWaypointPanel.Children.Add(new TextBlock { Text = "Current Waypoint: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
            _blockCurrentWaypoint = new TextBlock
            {
                Text = (_bot.GetInfoCurrentWaypoint() == null) ? "none" : _bot.GetInfoCurrentWaypoint().GetInfoID().ToString(),
                MinWidth = _infoPanelRightColumnWidth,
            };
            currentWaypointPanel.Children.Add(_blockCurrentWaypoint);
            root.Items.Add(currentWaypointPanel);
            // Add target
            WrapPanel destinationWaypointPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            destinationWaypointPanel.Children.Add(new TextBlock { Text = "Target Waypoint: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
            _blockDestinationWaypoint = new TextBlock
            {
                Text = (_bot.GetInfoDestinationWaypoint() == null) ? "none" : _bot.GetInfoDestinationWaypoint().GetInfoID().ToString(),
                MinWidth = _infoPanelRightColumnWidth,
            };
            destinationWaypointPanel.Children.Add(_blockDestinationWaypoint);
            root.Items.Add(destinationWaypointPanel);
            // Add goal
            WrapPanel goalWaypointPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            goalWaypointPanel.Children.Add(new TextBlock { Text = "Goal Waypoint: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
            _blockGoalWaypoint = new TextBlock
            {
                Text = (_bot.GetInfoGoalWaypoint() == null) ? "none" : _bot.GetInfoGoalWaypoint().GetInfoID().ToString(),
                MinWidth = _infoPanelRightColumnWidth,
            };
            goalWaypointPanel.Children.Add(_blockGoalWaypoint);
            root.Items.Add(goalWaypointPanel);
            // Add speed
            WrapPanel speedPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            speedPanel.Children.Add(new TextBlock { Text = "Speed: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
            _blockSpeed = new TextBlock
            {
                Text = _bot.GetInfoSpeed().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + " m/s",
                MinWidth = _infoPanelRightColumnWidth
            };
            speedPanel.Children.Add(_blockSpeed);
            root.Items.Add(speedPanel);
            // Add blocked status
            WrapPanel blockedPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            blockedPanel.Children.Add(new TextBlock { Text = "Blocked: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
            _blockBlocked = new TextBlock
            {
                Text = _bot.GetInfoBlocked().ToString(),
                MinWidth = _infoPanelRightColumnWidth
            };
            blockedPanel.Children.Add(_blockBlocked);
            root.Items.Add(blockedPanel);
            // Add blocked time remaining
            WrapPanel blockedUntilPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            blockedUntilPanel.Children.Add(new TextBlock { Text = "Blocked until: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
            _blockBlockedUntil = new TextBlock
            {
                Text = "n/a",
                MinWidth = _infoPanelRightColumnWidth
            };
            blockedUntilPanel.Children.Add(_blockBlockedUntil);
            root.Items.Add(blockedUntilPanel);
            // Add queueing status
            WrapPanel queueingPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            queueingPanel.Children.Add(new TextBlock { Text = "Queueing: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
            _blockQueueing = new TextBlock
            {
                Text = _bot.GetInfoIsQueueing().ToString(),
                MinWidth = _infoPanelRightColumnWidth
            };
            queueingPanel.Children.Add(_blockQueueing);
            root.Items.Add(queueingPanel);
            // Add state
            WrapPanel statePanel = new WrapPanel { Orientation = Orientation.Horizontal };
            statePanel.Children.Add(new TextBlock { Text = "State: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
            _blockState = new TextBlock
            {
                Text = _bot.GetInfoState(),
                MinWidth = _infoPanelRightColumnWidth
            };
            statePanel.Children.Add(_blockState);
            root.Items.Add(statePanel);
            // Add path
            WrapPanel pathPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            pathPanel.Children.Add(new TextBlock { Text = "Path: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
            _blockPath = new TextBlock
            {
                Text = "<empty>",
                MinWidth = _infoPanelRightColumnWidth
            };
            pathPanel.Children.Add(_blockPath);
            root.Items.Add(pathPanel);
            // Expand root node
            root.IsExpanded = true;
        }
    }

    #endregion

    #region Pod

    public class SimulationInfoPod : SimulationInfoObject
    {
        private readonly IPodInfo _pod;
        private readonly int _infoPanelLeftColumnWidth = 105;
        private readonly int _infoPanelRightColumnWidth = 60;
        private TextBlock _blockXY;
        private TextBlock _blockOrientation;
        private TextBlock _blockCapacity;
        private TextBlock _blockCapacityReserved;
        private TextBlock _blockStorageTag;
        private TreeViewItem _treeItemContent;
        private TreeViewItem _root;
        private SimulationVisualContentManager _contentManager;

        private TextBlock _blockReadyForRefill;

        public SimulationInfoPod(TreeView infoHost, IPodInfo pod) : base(infoHost) { _pod = pod; }

        public override void InfoPanelUpdate()
        {
            // Update meta info
            _blockXY.Text = _pod.GetInfoCenterX().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "/" + _pod.GetInfoCenterY().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER);
            _blockOrientation.Text = Transformation2D.ProjectOrientation(_pod.GetInfoOrientation()).ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "°";
            _blockCapacity.Text = _pod.GetInfoCapacityUsed().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "/" + _pod.GetInfoCapacity().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER);
            _blockCapacityReserved.Text = _pod.GetInfoCapacityReserved().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "/" + _pod.GetInfoCapacity().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER);
            _blockStorageTag.Text = _pod.InfoTagPodStorageInfo;
            _blockReadyForRefill.Text = _pod.GetInfoReadyForRefill().ToString();

            // Update content info
            if (_pod.GetInfoContentChanged())
                _contentManager.UpdateContentInfo();
        }

        public override void InfoPanelInit()
        {
            // Visually emphasize focus on element
            ManagedVisual2D.StrokeThickness = StrokeThicknessFocused;
            // Prepare information controls
            _infoHost.Items.Clear();
            if (_root == null)
            {
                _root = new TreeViewItem { Header = "Pod" + _pod.GetInfoID() };
                // Add position
                WrapPanel xyPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                xyPanel.Children.Add(new TextBlock { Text = "X/Y: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockXY = new TextBlock
                {
                    Text =
                    _pod.GetInfoCenterX().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "/" +
                    _pod.GetInfoCenterY().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER),
                    MinWidth = _infoPanelRightColumnWidth
                };
                xyPanel.Children.Add(_blockXY);
                _root.Items.Add(xyPanel);
                // Add orientation
                WrapPanel orientationPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                orientationPanel.Children.Add(new TextBlock { Text = "Orientation: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockOrientation = new TextBlock
                {
                    Text = Transformation2D.ProjectOrientation(_pod.GetInfoOrientation()).ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "°",
                    MinWidth = _infoPanelRightColumnWidth,
                };
                orientationPanel.Children.Add(_blockOrientation);
                _root.Items.Add(orientationPanel);
                // Add capacity
                WrapPanel capacityPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                capacityPanel.Children.Add(new TextBlock { Text = "Capacity: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockCapacity = new TextBlock
                {
                    Text = _pod.GetInfoCapacityUsed().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "/" +
                    _pod.GetInfoCapacity().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER),
                    MinWidth = _infoPanelRightColumnWidth,
                };
                capacityPanel.Children.Add(_blockCapacity);
                _root.Items.Add(capacityPanel);
                // Add capacity reserved
                WrapPanel capacityReservedPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                capacityReservedPanel.Children.Add(new TextBlock { Text = "Capacity reserved: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockCapacityReserved = new TextBlock
                {
                    Text = _pod.GetInfoCapacityReserved().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "/" +
                    _pod.GetInfoCapacity().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER),
                    MinWidth = _infoPanelRightColumnWidth,
                };
                capacityReservedPanel.Children.Add(_blockCapacityReserved);
                _root.Items.Add(capacityReservedPanel);
                // Add capacity reserved
                WrapPanel storageTagPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                storageTagPanel.Children.Add(new TextBlock { Text = "Storage tag: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockStorageTag = new TextBlock
                {
                    Text = _pod.InfoTagPodStorageInfo,
                    MinWidth = _infoPanelRightColumnWidth,
                };
                storageTagPanel.Children.Add(_blockStorageTag);
                _root.Items.Add(storageTagPanel);
                // Add ReadyForRefill
                WrapPanel readyForRefillPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                readyForRefillPanel.Children.Add(new TextBlock { Text = "Ready for refill: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockReadyForRefill = new TextBlock
                {
                    Text = _pod.GetInfoReadyForRefill().ToString(),
                    MinWidth = _infoPanelRightColumnWidth,
                };
                readyForRefillPanel.Children.Add(_blockReadyForRefill);
                _root.Items.Add(readyForRefillPanel);
                // Add content
                if (_treeItemContent == null)
                {
                    _treeItemContent = new TreeViewItem { Header = "Content" };
                    _treeItemContent.IsExpanded = true;
                    _contentManager = new SimulationVisualContentManager(_treeItemContent, _pod);
                }
                _root.Items.Add(_treeItemContent);
            }
            // Update content info
            _contentManager.UpdateContentInfo();
            // Add and expand root node
            _infoHost.Items.Add(_root);
            _root.IsExpanded = true;
        }
    }

    #endregion

    #region Guard

    public class SimulationInfoGuard : SimulationInfoObject
    {
        private readonly IGuardInfo _guard;
        private readonly int _infoPanelLeftColumnWidth = 80;
        private readonly int _infoPanelRightColumnWidth = 60;
        private TextBlock _blockConnection;
        private TextBlock _blockEntry;
        private TextBlock _blockBlock;
        private TextBlock _blockCapacity;
        private TreeViewItem _root;

        public SimulationInfoGuard(TreeView infoHost, IGuardInfo guard) : base(infoHost) { _guard = guard; }

        public override void InfoPanelUpdate()
        {
            // Update meta info
            _blockBlock.Text = _guard.GetInfoIsAccessible().ToString();
            _blockCapacity.Text = _guard.GetInfoRequests().ToString() + "/" + _guard.GetInfoCapacity().ToString();
        }

        public override void InfoPanelInit()
        {
            // Visually emphasize focus on element
            ManagedVisual2D.StrokeThickness = StrokeThicknessFocused;
            // Prepare information controls
            _infoHost.Items.Clear();
            if (_root == null)
            {
                _root = new TreeViewItem { Header = "Guard (S:" + _guard.GetInfoSemaphore().GetInfoID().ToString() + "-" + _guard.GetInfoSemaphore().GetInfoGuards().ToString() + "Gs)" };
                // Add connection
                WrapPanel connectionPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                connectionPanel.Children.Add(new TextBlock { Text = "Path: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockConnection = new TextBlock
                {
                    Text = _guard.GetInfoFrom().GetInfoID().ToString() + " -> " + _guard.GetInfoTo().GetInfoID().ToString(),
                    MinWidth = _infoPanelRightColumnWidth
                };
                connectionPanel.Children.Add(_blockConnection);
                _root.Items.Add(connectionPanel);
                // Add entry info
                WrapPanel entryPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                entryPanel.Children.Add(new TextBlock { Text = "Entry: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockEntry = new TextBlock
                {
                    Text = _guard.GetInfoIsEntry().ToString(),
                    MinWidth = _infoPanelRightColumnWidth
                };
                entryPanel.Children.Add(_blockEntry);
                _root.Items.Add(entryPanel);
                // Add block info
                WrapPanel blockPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                blockPanel.Children.Add(new TextBlock { Text = "Accessible: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockBlock = new TextBlock
                {
                    Text = _guard.GetInfoIsAccessible().ToString(),
                    MinWidth = _infoPanelRightColumnWidth,
                };
                blockPanel.Children.Add(_blockBlock);
                _root.Items.Add(blockPanel);
                // Add capacity
                WrapPanel capacityPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                capacityPanel.Children.Add(new TextBlock { Text = "Capacity: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockCapacity = new TextBlock
                {
                    Text = _guard.GetInfoRequests().ToString() + "/" + _guard.GetInfoCapacity().ToString(),
                    MinWidth = _infoPanelRightColumnWidth,
                };
                capacityPanel.Children.Add(_blockCapacity);
                _root.Items.Add(capacityPanel);
            }
            // Add and expand root node
            _infoHost.Items.Add(_root);
            _root.IsExpanded = true;
        }
    }

    #endregion

    #region Waypoint

    public class SimulationInfoWaypoint : SimulationInfoObject
    {
        private readonly IWaypointInfo _waypoint;
        private readonly int _infoPanelLeftColumnWidth = 80;
        private readonly int _infoPanelRightColumnWidth = 60;
        private TextBlock _blockPaths;
        private TextBlock _blockPosition;
        private TextBlock _blockStorageInfo;
        private TreeViewItem _root;

        public SimulationInfoWaypoint(TreeView infoHost, IWaypointInfo waypoint) : base(infoHost) { _waypoint = waypoint; }

        public override void InfoPanelUpdate()
        {
            // Update meta info
            // Nothing should be subject to change - so don't do anything
        }

        public override void InfoPanelInit()
        {
            // Visually emphasize focus on element
            ManagedVisual2D.StrokeThickness = StrokeThicknessFocused;
            // Prepare information controls
            _infoHost.Items.Clear();
            if (_root == null)
            {
                _root = new TreeViewItem { Header = "Waypoint" + _waypoint.GetInfoID() };
                // Add position
                WrapPanel positionPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                positionPanel.Children.Add(new TextBlock { Text = "Position: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockPosition = new TextBlock
                {
                    Text = _waypoint.GetInfoCenterX().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER) + "," + _waypoint.GetInfoCenterY().ToString(IOConstants.EXPORT_FORMAT_SHORT, IOConstants.FORMATTER),
                    MinWidth = _infoPanelRightColumnWidth,
                };
                positionPanel.Children.Add(_blockPosition);
                _root.Items.Add(positionPanel);
                // Add connections
                WrapPanel pathPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                pathPanel.Children.Add(new TextBlock { Text = "Paths: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockPaths = new TextBlock
                {
                    Text = string.Join(",", _waypoint.GetInfoConnectedWaypoints().Select(wp => wp.GetInfoID())),
                    MinWidth = _infoPanelRightColumnWidth
                };
                pathPanel.Children.Add(_blockPaths);
                _root.Items.Add(pathPanel);
                // Add queue info
                WrapPanel storageInfoPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                storageInfoPanel.Children.Add(new TextBlock { Text = "Storage: ", TextAlignment = TextAlignment.Right, MinWidth = _infoPanelLeftColumnWidth });
                _blockStorageInfo = new TextBlock
                {
                    Text = _waypoint.GetInfoStorageLocation().ToString(),
                    MinWidth = _infoPanelRightColumnWidth
                };
                storageInfoPanel.Children.Add(_blockStorageInfo);
                _root.Items.Add(storageInfoPanel);
            }
            // Add and expand root node
            _infoHost.Items.Add(_root);
            _root.IsExpanded = true;
        }
    }

    #endregion
}
