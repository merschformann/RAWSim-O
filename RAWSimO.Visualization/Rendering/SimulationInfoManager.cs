using RAWSimO.Core.Info;
using RAWSimO.Toolbox;
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
    public class SimulationInfoManager
    {
        public SimulationInfoManager(TreeView infoHost, IInstanceInfo instance)
        {
            _instance = instance;
            _infoHost = infoHost;
            _instanceInfoObject = new SimulationInfoInstance(infoHost, instance);
        }

        IInstanceInfo _instance;
        SimulationInfoInstance _instanceInfoObject;

        Dictionary<IGeneralObjectInfo, SimulationInfoObject> _managedInfoObjects = new Dictionary<IGeneralObjectInfo, SimulationInfoObject>();
        Dictionary<SimulationVisual2D, SimulationInfoObject> _managed2DVisuals = new Dictionary<SimulationVisual2D, SimulationInfoObject>();
        Dictionary<SimulationVisual3D, SimulationInfoObject> _managed3DVisuals = new Dictionary<SimulationVisual3D, SimulationInfoObject>();

        SimulationInfoObject _currentInfoObject;

        TreeView _infoHost;

        public void Register(IBotInfo bot, SimulationVisualBot2D visual)
        {
            if (!_managedInfoObjects.ContainsKey(bot))
                _managedInfoObjects[bot] = new SimulationInfoBot(_infoHost, bot);
            _managed2DVisuals[visual] = _managedInfoObjects[bot];
            _managed2DVisuals[visual].ManagedVisual2D = visual;
        }

        public void Register(IPodInfo pod, SimulationVisualPod2D visual)
        {
            if (!_managedInfoObjects.ContainsKey(pod))
                _managedInfoObjects[pod] = new SimulationInfoPod(_infoHost, pod);
            _managed2DVisuals[visual] = _managedInfoObjects[pod];
            _managed2DVisuals[visual].ManagedVisual2D = visual;
        }

        public void Register(IInputStationInfo iStation, SimulationVisualInputStation2D visual)
        {
            if (!_managedInfoObjects.ContainsKey(iStation))
                _managedInfoObjects[iStation] = new SimulationInfoInputStation(_infoHost, iStation);
            _managed2DVisuals[visual] = _managedInfoObjects[iStation];
            _managed2DVisuals[visual].ManagedVisual2D = visual;
        }

        public void Register(IOutputStationInfo oStation, SimulationVisualOutputStation2D visual)
        {
            if (!_managedInfoObjects.ContainsKey(oStation))
                _managedInfoObjects[oStation] = new SimulationInfoOutputStation(_infoHost, oStation);
            _managed2DVisuals[visual] = _managedInfoObjects[oStation];
            _managed2DVisuals[visual].ManagedVisual2D = visual;
        }

        public void Register(IWaypointInfo waypoint, SimulationVisualWaypoint2D visual)
        {
            if (!_managedInfoObjects.ContainsKey(waypoint))
                _managedInfoObjects[waypoint] = new SimulationInfoWaypoint(_infoHost, waypoint);
            _managed2DVisuals[visual] = _managedInfoObjects[waypoint];
            _managed2DVisuals[visual].ManagedVisual2D = visual;
        }

        public void Register(IGuardInfo guard, SimulationVisualGuard2D visual)
        {
            if (!_managedInfoObjects.ContainsKey(guard))
                _managedInfoObjects[guard] = new SimulationInfoGuard(_infoHost, guard);
            _managed2DVisuals[visual] = _managedInfoObjects[guard];
            _managed2DVisuals[visual].ManagedVisual2D = visual;
        }

        public void Register(IElevatorInfo elevator, SimulationVisualElevatorEntrance2D visual)
        {
            if (!_managedInfoObjects.ContainsKey(elevator))
                _managedInfoObjects[elevator] = new SimulationInfoElevatorEntrance(_infoHost, elevator);
            _managed2DVisuals[visual] = _managedInfoObjects[elevator];
            _managed2DVisuals[visual].ManagedVisual2D = visual;
        }

        public void Register(IBotInfo bot, SimulationVisualBot3D visual)
        {
            if (!_managedInfoObjects.ContainsKey(bot))
                _managedInfoObjects[bot] = new SimulationInfoBot(_infoHost, bot);
            _managed3DVisuals[visual] = _managedInfoObjects[bot];
            _managed3DVisuals[visual].ManagedVisual3D = visual;
        }

        public void Register(IPodInfo pod, SimulationVisualPod3D visual)
        {
            if (!_managedInfoObjects.ContainsKey(pod))
                _managedInfoObjects[pod] = new SimulationInfoPod(_infoHost, pod);
            _managed3DVisuals[visual] = _managedInfoObjects[pod];
            _managed3DVisuals[visual].ManagedVisual3D = visual;
        }

        public void Register(IInputStationInfo iStation, SimulationVisualInputStation3D visual)
        {
            if (!_managedInfoObjects.ContainsKey(iStation))
                _managedInfoObjects[iStation] = new SimulationInfoInputStation(_infoHost, iStation);
            _managed3DVisuals[visual] = _managedInfoObjects[iStation];
            _managed3DVisuals[visual].ManagedVisual3D = visual;
        }

        public void Register(IOutputStationInfo oStation, SimulationVisualOutputStation3D visual)
        {
            if (!_managedInfoObjects.ContainsKey(oStation))
                _managedInfoObjects[oStation] = new SimulationInfoOutputStation(_infoHost, oStation);
            _managed3DVisuals[visual] = _managedInfoObjects[oStation];
            _managed3DVisuals[visual].ManagedVisual3D = visual;
        }

        public void Register(IWaypointInfo waypoint, SimulationVisualOutputStation3D visual)
        {
            if (!_managedInfoObjects.ContainsKey(waypoint))
                _managedInfoObjects[waypoint] = new SimulationInfoWaypoint(_infoHost, waypoint);
            _managed3DVisuals[visual] = _managedInfoObjects[waypoint];
            _managed3DVisuals[visual].ManagedVisual3D = visual;
        }

        public void Register(IElevatorInfo elevator, SimulationVisualElevatorEntrance3D visual)
        {
            if (!_managedInfoObjects.ContainsKey(elevator))
                _managedInfoObjects[elevator] = new SimulationInfoElevatorEntrance(_infoHost, elevator);
            _managed3DVisuals[visual] = _managedInfoObjects[elevator];
            _managed3DVisuals[visual].ManagedVisual3D = visual;
        }

        public void InitInfoObject(SimulationVisual2D visual)
        {
            SimulationInfoObject newInfoObject = _managed2DVisuals.ContainsKey(visual) ? _managed2DVisuals[visual] : null;
            if (newInfoObject != _currentInfoObject)
            {
                // Select the new object and unselect the old one
                if (_currentInfoObject != null)
                    _currentInfoObject.InfoPanelLeave();
                _currentInfoObject = newInfoObject;
                if (_currentInfoObject != null)
                    _currentInfoObject.InfoPanelInit();
            }
            else
            {
                // Unselect the already selected object
                if (_currentInfoObject != null)
                    _currentInfoObject.InfoPanelLeave();
                _currentInfoObject = null;
            }
        }

        public void InitInfoObject(SimulationVisual3D visual)
        {
            SimulationInfoObject newInfoObject = _managed3DVisuals.ContainsKey(visual) ? _managed3DVisuals[visual] : null;
            if (newInfoObject != _currentInfoObject)
            {
                // Select the new object and unselect the old one
                if (_currentInfoObject != null)
                    _currentInfoObject.InfoPanelLeave();
                _currentInfoObject = newInfoObject;
                if (_currentInfoObject != null)
                    _currentInfoObject.InfoPanelInit();
            }
            else
            {
                // Unselect the already selected object
                if (_currentInfoObject != null)
                    _currentInfoObject.InfoPanelLeave();
                _currentInfoObject = null;
            }
        }

        public void Update()
        {
            _infoHost.Dispatcher.Invoke(() =>
            {
                if (_infoHost.Visibility == Visibility.Visible)
                {
                    if (_currentInfoObject != null)
                    {
                        _currentInfoObject.InfoPanelUpdate();
                    }
                    else
                    {
                        HandleInfoPanelNoSelection();
                    }
                }
            });
        }

        public void Init()
        {
            // Clear info panel
            _infoHost.Items.Clear();
        }

        protected void HandleInfoPanelNoSelection()
        {
            // Add controls in case the info panel is empty
            if (!_infoHost.HasItems)
            {
                _instanceInfoObject.InfoPanelInit();
            }

            // Use info of the instance
            _instanceInfoObject.InfoPanelUpdate();
        }
    }

    #region Order manager

    public class SimulationVisualOrderManager
    {
        public SimulationVisualOrderManager(TreeViewItem rootOpenOrders, string headerOpenOrders, TreeViewItem rootCompletedOrders, string headerCompletedOrders, int orderCount)
        {
            RootOpenOrders = rootOpenOrders;
            RootCompletedOrders = rootCompletedOrders;
            _headerOpenOrders = headerOpenOrders;
            _headerCompletedOrders = headerCompletedOrders;
            _orderCount = orderCount;
        }

        public TreeViewItem RootOpenOrders { get; private set; }
        public TreeViewItem RootCompletedOrders { get; private set; }

        private int _orderCount;
        private List<IOrderInfo> _droppedOrders = new List<IOrderInfo>();
        private List<IOrderInfo> _completedOrders = new List<IOrderInfo>();
        private List<IOrderInfo> _openOrders = new List<IOrderInfo>();
        private int _openOrderCount;
        private int _completedOrderCount;
        private string _headerOpenOrders;
        private string _headerCompletedOrders;
        private Dictionary<IOrderInfo, bool> _orderStatusOpen = new Dictionary<IOrderInfo, bool>();
        private Dictionary<IOrderInfo, WrapPanel> _orderControls = new Dictionary<IOrderInfo, WrapPanel>();
        private Dictionary<IOrderInfo, Dictionary<IItemDescriptionInfo, TextBlock>> _itemControls = new Dictionary<IOrderInfo, Dictionary<IItemDescriptionInfo, TextBlock>>();

        public void Update(IEnumerable<IOrderInfo> openOrders, IEnumerable<IOrderInfo> completedOrders)
        {
            // Add new controls for the orders
            foreach (var order in openOrders.Concat(completedOrders))
            {
                // Check whether a control for this order is already available
                if (!_orderStatusOpen.ContainsKey(order))
                {
                    // Create a container for this order
                    _orderControls[order] = new WrapPanel() { Orientation = Orientation.Horizontal };
                    RootOpenOrders.Items.Add(_orderControls[order]);
                    // Create the controls for every element of the order
                    _itemControls[order] = new Dictionary<IItemDescriptionInfo, TextBlock>();
                    foreach (var position in order.GetInfoPositions())
                    {
                        TextBlock positionBlock = new TextBlock() { Padding = new Thickness(3), FontFamily = VisualizationConstants.ItemFont };
                        if (position is IColoredLetterDescriptionInfo)
                        {
                            IColoredLetterDescriptionInfo itemDescription = position as IColoredLetterDescriptionInfo;
                            positionBlock.Background = VisualizationConstants.LetterColorBackgroundBrushes[itemDescription.GetInfoColor()];
                            positionBlock.Foreground = VisualizationConstants.LetterColorIncomplete;
                            positionBlock.Text = itemDescription.GetInfoLetter() + "(0/" + order.GetInfoDemandCount(position) + ")";
                        }
                        else
                        {
                            if (position is ISimpleItemDescriptionInfo)
                            {
                                ISimpleItemDescriptionInfo itemDescription = position as ISimpleItemDescriptionInfo;
                                positionBlock.Background = ColorManager.GenerateHueBrush(itemDescription.GetInfoHue());
                                positionBlock.Foreground = VisualizationConstants.SimpleItemColorIncomplete;
                                positionBlock.Text = (itemDescription.GetInfoID().ToString() + "(0/" + order.GetInfoDemandCount(position) + ")").PadLeft(VisualizationConstants.SIMPLE_ITEM_ORDER_MIN_CHAR_COUNT);
                            }
                            else
                            {
                                positionBlock.Text = position.GetInfoDescription() + "(0/" + order.GetInfoDemandCount(position) + ")";
                            }
                        }
                        _itemControls[order][position] = positionBlock;
                        _orderControls[order].Children.Add(positionBlock);
                    }
                    // Add the order to the list and mark it as open
                    _openOrders.Add(order);
                    _openOrderCount++;
                    _orderStatusOpen[order] = true;
                }
            }

            // Update served quantities of existing orders
            foreach (var order in _openOrders)
            {
                // Refresh order status
                if (order.GetInfoIsCompleted())
                    _orderStatusOpen[order] = false;

                // Update all positions
                foreach (var position in order.GetInfoPositions())
                {
                    if (position is IColoredLetterDescriptionInfo)
                    {
                        // Update the position's text (use the coloring too)
                        IColoredLetterDescriptionInfo itemDescription = position as IColoredLetterDescriptionInfo;
                        _itemControls[order][position].Text = itemDescription.GetInfoLetter() + "(" + order.GetInfoServedCount(position).ToString() + "/" + order.GetInfoDemandCount(position).ToString() + ")";
                    }
                    else
                    {
                        if (position is ISimpleItemDescriptionInfo)
                        {
                            // Update the position's text (use the coloring too)
                            ISimpleItemDescriptionInfo itemDescription = position as ISimpleItemDescriptionInfo;
                            _itemControls[order][position].Text =
                                (itemDescription.GetInfoID().ToString() + "(" + order.GetInfoServedCount(position).ToString() + "/" + order.GetInfoDemandCount(position).ToString() + ")")
                                    .PadLeft(VisualizationConstants.SIMPLE_ITEM_ORDER_MIN_CHAR_COUNT);
                        }
                        else
                        {
                            // Update the position's text
                            _itemControls[order][position].Text = position.GetInfoDescription() + "(" + order.GetInfoServedCount(position).ToString() + "/" + order.GetInfoDemandCount(position).ToString() + ")";
                        }
                    }
                    // Set color according to completed position
                    _itemControls[order][position].Foreground =
                        order.GetInfoServedCount(position) == order.GetInfoDemandCount(position) ? // Check whether position is complete
                        VisualizationConstants.LetterColorComplete : // Completed the position
                        VisualizationConstants.LetterColorIncomplete; // Position is incomplete
                }
            }

            // Move newly completed orders from open to complete node
            foreach (var order in _openOrders.Where(o => !_orderStatusOpen[o]).ToList())
            {
                _openOrders.Remove(order);
                _completedOrders.Add(order);
                RootOpenOrders.Items.Remove(_orderControls[order]);
                RootCompletedOrders.Items.Insert(0, _orderControls[order]);
                _openOrderCount--;
                _completedOrderCount++;
            }

            // Remove completed orders if list gets too long
            if (_completedOrders.Count > _orderCount)
            {
                // Manage the list of the currently displayed orders
                List<IOrderInfo> removedOrders = _completedOrders.Take(_completedOrders.Count - _orderCount).ToList();
                _droppedOrders.AddRange(removedOrders);
                _completedOrders.RemoveRange(0, _completedOrders.Count - _orderCount);
                foreach (var removedOrder in removedOrders)
                    RootCompletedOrders.Items.Remove(_orderControls[removedOrder]);
                // Remove the controls
                foreach (var removedOrder in removedOrders)
                {
                    foreach (var position in removedOrder.GetInfoPositions())
                        _itemControls[removedOrder].Remove(position);
                    _itemControls.Remove(removedOrder);
                    _orderControls.Remove(removedOrder);
                }
            }

            // Update order count info
            RootOpenOrders.Header = _headerOpenOrders + " (" + _openOrderCount + ")";
            RootCompletedOrders.Header = _headerCompletedOrders + " (" + Math.Min(_orderCount, _completedOrderCount) + "/" + _completedOrderCount + ")";
        }
    }

    #endregion

    #region Bundle manager

    public class SimulationVisualBundleManager
    {
        public SimulationVisualBundleManager(TreeViewItem contentHost) { _contentHost = contentHost; }
        private TreeViewItem _contentHost;
        private Dictionary<IItemDescriptionInfo, TextBlock> _blocksContent = new Dictionary<IItemDescriptionInfo, TextBlock>();
        private Dictionary<IItemDescriptionInfo, int> _contentCount = new Dictionary<IItemDescriptionInfo, int>();

        public void UpdateContentInfo(IItemBundleInfo[] bundles)
        {
            // Update content info
            _contentCount.Clear();
            // Update item-count information
            foreach (var item in bundles)
            {
                if (!_contentCount.ContainsKey(item.GetInfoItemDescription()))
                    _contentCount[item.GetInfoItemDescription()] = item.GetInfoItemCount();
                else
                    _contentCount[item.GetInfoItemDescription()] += item.GetInfoItemCount();
            }
            // Update the visual controls
            foreach (var item in bundles)
            {
                // Check whether it is a new item
                if (!_blocksContent.ContainsKey(item.GetInfoItemDescription()))
                {
                    // New item - init visual control
                    TextBlock textBlock = new TextBlock() { Padding = new Thickness(3), FontFamily = VisualizationConstants.ItemFont };
                    if (item.GetInfoItemDescription() is IColoredLetterDescriptionInfo)
                    {
                        IColoredLetterDescriptionInfo itemDescription = item.GetInfoItemDescription() as IColoredLetterDescriptionInfo;
                        textBlock.Background = VisualizationConstants.LetterColorBackgroundBrushes[itemDescription.GetInfoColor()];
                        textBlock.Foreground = VisualizationConstants.LetterColorComplete;
                        textBlock.Text = itemDescription.GetInfoLetter() + "/" + _contentCount[itemDescription];
                    }
                    else
                    {
                        if (item.GetInfoItemDescription() is ISimpleItemDescriptionInfo)
                        {
                            ISimpleItemDescriptionInfo itemDescription = item.GetInfoItemDescription() as ISimpleItemDescriptionInfo;
                            textBlock.Background = ColorManager.GenerateHueBrush(itemDescription.GetInfoHue());
                            textBlock.Foreground = VisualizationConstants.SimpleItemColorComplete;
                            textBlock.Text =
                                (itemDescription.GetInfoID().ToString() + "(" + _contentCount[itemDescription] + ")").PadBoth(VisualizationConstants.SIMPLE_ITEM_BUNDLE_MIN_CHAR_COUNT);
                        }
                        else
                        {
                            textBlock.Text = item.GetInfoItemDescription().GetInfoDescription();
                        }
                    }
                    // Add the control
                    _blocksContent[item.GetInfoItemDescription()] = textBlock;
                    _contentHost.Items.Add(textBlock);
                }
                else
                {
                    // Visual control already exists - grab and update it
                    if (item.GetInfoItemDescription() is IColoredLetterDescriptionInfo)
                    {
                        IColoredLetterDescriptionInfo itemDescription = item.GetInfoItemDescription() as IColoredLetterDescriptionInfo;
                        _blocksContent[item.GetInfoItemDescription()].Text = itemDescription.GetInfoLetter() + "/" + _contentCount[itemDescription];
                    }
                    else
                    {
                        if (item.GetInfoItemDescription() is ISimpleItemDescriptionInfo)
                        {
                            ISimpleItemDescriptionInfo itemDescription = item.GetInfoItemDescription() as ISimpleItemDescriptionInfo;
                            _blocksContent[item.GetInfoItemDescription()].Text =
                                (itemDescription.GetInfoID().ToString() + "(" + _contentCount[itemDescription] + ")").PadBoth(VisualizationConstants.SIMPLE_ITEM_BUNDLE_MIN_CHAR_COUNT);
                        }
                        else
                        {
                            _blocksContent[item.GetInfoItemDescription()].Text = item.GetInfoItemDescription().GetInfoDescription();
                        }
                    }
                }
            }
            // Remove controls showing items not present in the pod anymore
            foreach (var itemDescription in _blocksContent.Keys.Except(bundles.Select(b => b.GetInfoItemDescription())).ToArray())
            {
                _contentHost.Items.Remove(_blocksContent[itemDescription]);
                _blocksContent.Remove(itemDescription);
            }
        }
    }

    #endregion

    #region Content manager

    public class SimulationVisualContentManager
    {
        public SimulationVisualContentManager(TreeViewItem contentHost, IPodInfo pod) { _contentHost = contentHost; _pod = pod; }
        private IPodInfo _pod;
        private TreeViewItem _contentHost;
        private Dictionary<IItemDescriptionInfo, TextBlock> _blocksContent = new Dictionary<IItemDescriptionInfo, TextBlock>();

        public void UpdateContentInfo()
        {
            // Update the visual controls
            foreach (var item in _pod.GetInfoInstance().GetInfoItemDescriptions())
            {
                // Check whether the item is contained in the pod
                if (_pod.GetInfoContent(item) > 0)
                {
                    // Check whether it is a new item
                    if (!_blocksContent.ContainsKey(item))
                    {
                        // New item - init visual control
                        TextBlock textBlock = new TextBlock() { Padding = new Thickness(3), FontFamily = VisualizationConstants.ItemFont };
                        if (item is IColoredLetterDescriptionInfo)
                        {
                            IColoredLetterDescriptionInfo itemDescription = item as IColoredLetterDescriptionInfo;
                            textBlock.Background = VisualizationConstants.LetterColorBackgroundBrushes[itemDescription.GetInfoColor()];
                            textBlock.Foreground = VisualizationConstants.LetterColorComplete;
                            textBlock.Text = itemDescription.GetInfoLetter() + "/" + _pod.GetInfoContent(itemDescription);
                        }
                        else
                        {
                            if (item is ISimpleItemDescriptionInfo)
                            {
                                ISimpleItemDescriptionInfo itemDescription = item as ISimpleItemDescriptionInfo;
                                textBlock.Background = ColorManager.GenerateHueBrush(itemDescription.GetInfoHue());
                                textBlock.Foreground = VisualizationConstants.SimpleItemColorComplete;
                                textBlock.Text =
                                    (itemDescription.GetInfoID().ToString() + "(" + _pod.GetInfoContent(itemDescription) + ")").PadBoth(VisualizationConstants.SIMPLE_ITEM_BUNDLE_MIN_CHAR_COUNT);
                            }
                            else
                            {
                                textBlock.Text = item.GetInfoDescription();
                            }
                        }
                        // Add the control
                        _blocksContent[item] = textBlock;
                        _contentHost.Items.Add(textBlock);
                    }
                    else
                    {
                        // Visual control already exists - grab and update it
                        if (item is IColoredLetterDescriptionInfo)
                        {
                            IColoredLetterDescriptionInfo itemDescription = item as IColoredLetterDescriptionInfo;
                            _blocksContent[item].Text = itemDescription.GetInfoLetter() + "/" + _pod.GetInfoContent(itemDescription);
                        }
                        else
                        {
                            if (item is ISimpleItemDescriptionInfo)
                            {
                                ISimpleItemDescriptionInfo itemDescription = item as ISimpleItemDescriptionInfo;
                                _blocksContent[item].Text =
                                    (itemDescription.GetInfoID().ToString() + "(" + _pod.GetInfoContent(itemDescription) + ")").PadBoth(VisualizationConstants.SIMPLE_ITEM_BUNDLE_MIN_CHAR_COUNT);
                            }
                            else
                            {
                                _blocksContent[item].Text = item.GetInfoDescription();
                            }
                        }
                    }
                }
            }
            // Remove controls showing items not present in the pod anymore
            foreach (var item in _pod.GetInfoInstance().GetInfoItemDescriptions())
            {
                if (_pod.GetInfoContent(item) <= 0 && _blocksContent.ContainsKey(item))
                {
                    _contentHost.Items.Remove(_blocksContent[item]);
                    _blocksContent.Remove(item);
                }
            }
        }
    }

    #endregion
}
