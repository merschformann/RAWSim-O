using RAWSimO.Core.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace RAWSimO.Visualization.Rendering
{
    public class SimulationAnimation2D : SimulationAnimation
    {
        public SimulationAnimation2D(
            IInstanceInfo instance,
            Dispatcher uiDispatcher,
            SimulationAnimationConfig config,
            Func<bool> heatModeEnabled,
            Func<BotColorMode> botColorModeGetter,
            Canvas contentControl,
            Grid contentHost,
            MouseButtonEventHandler elementClickAction,
            SimulationInfoManager infoControl,
            ITierInfo currentTier)
            : base(instance, uiDispatcher, config, botColorModeGetter, heatModeEnabled)
        {
            _contentControl = contentControl;
            _contentHost = contentHost;
            _currentTier = instance.GetInfoTiers().First();
            _elementClickAction = elementClickAction;
            _infoControl = infoControl;
            _currentTier = currentTier;
        }

        protected double DEFAULT_TRANSFORMATION_FACTOR = 40;

        protected Canvas _contentControl;
        protected Grid _contentHost;
        protected Transformation2D _transformer;
        protected MouseButtonEventHandler _elementClickAction;
        protected SimulationInfoManager _infoControl;

        private ITierInfo _currentTier;
        private Dictionary<IBotInfo, SimulationVisualBot2D> _botVisuals;
        private Dictionary<IBotInfo, SimulationVisualBot2D> _shadowBotVisuals;
        private Dictionary<IPodInfo, SimulationVisualPod2D> _podVisuals;
        private Dictionary<IPodInfo, SimulationVisualPod2D> _shadowPodVisuals;
        private Dictionary<IInputStationInfo, SimulationVisualInputStation2D> _iStationVisuals;
        private Dictionary<IOutputStationInfo, SimulationVisualOutputStation2D> _oStationVisuals;
        private Dictionary<IWaypointInfo, SimulationVisualWaypoint2D> _waypointVisuals;
        private Image _waypointGraphVisual;
        private Dictionary<IBotInfo, SimulationVisualGoalMarker2D> _botGoalMarkerVisuals;
        private Dictionary<IBotInfo, SimulationVisualGoalMarker2D> _shadowBotGoalMarkerVisuals;
        private Dictionary<IBotInfo, SimulationVisualDestinationMarker2D> _botDestinationMarkerVisuals;
        private Dictionary<IBotInfo, SimulationVisualDestinationMarker2D> _shadowBotDestinationMarkerVisuals;
        private Dictionary<IBotInfo, SimulationVisualPathMarker2D> _botPathVisuals;
        private Dictionary<IBotInfo, SimulationVisualPathMarker2D> _shadowBotPathVisuals;
        private Dictionary<IGuardInfo, SimulationVisualGuard2D> _guardVisuals;
        private Dictionary<IWaypointInfo, SimulationVisualElevatorEntrance2D> _elevatorEntranceVisuals;

        public void UpdateCurrentTier(ITierInfo newTier)
        {
            // Set new current tier
            _currentTier = newTier;
            // Reinitialize the visualization
            Init();
        }

        /// <summary>
        /// Adds an element to the active view.
        /// </summary>
        /// <param name="element">The element to add.</param>
        private void Add(UIElement element)
        {
            _contentControl.Children.Add(element);
            if (element is SimulationVisualGoalMarker2D)
                Canvas.SetZIndex(element, 10);
            else if (element is SimulationVisualBot2D)
                Canvas.SetZIndex(element, 9);
            else if (element is SimulationVisualPod2D)
                Canvas.SetZIndex(element, 8);
            else if (element is SimulationVisualGuard2D)
                Canvas.SetZIndex(element, 7);
            else if (element is SimulationVisualInputStation2D)
                Canvas.SetZIndex(element, 6);
            else if (element is SimulationVisualOutputStation2D)
                Canvas.SetZIndex(element, 5);
            else if (element is SimulationVisualElevatorEntrance2D)
                Canvas.SetZIndex(element, 4);
            else if (element is SimulationVisualDestinationMarker2D)
                Canvas.SetZIndex(element, 3);
            else if (element is SimulationVisualPathMarker2D)
                Canvas.SetZIndex(element, 2);
            else if (element is SimulationVisualWaypoint2D)
                Canvas.SetZIndex(element, 1);
            else if (element is Image)
                Canvas.SetZIndex(element, 0);
            else
                throw new ArgumentException("Unknown type of element: " + element.GetType().ToString());
        }
        /// <summary>
        /// Removes an element from the active view.
        /// </summary>
        /// <param name="element">The element to remove.</param>
        private void Remove(UIElement element)
        {
            _contentControl.Children.Remove(element);
        }

        public override void Init()
        {
            // Canvas size and transformation
            _contentHost.Width = _currentTier.GetInfoLength() * DEFAULT_TRANSFORMATION_FACTOR;
            _contentHost.Height = _currentTier.GetInfoWidth() * DEFAULT_TRANSFORMATION_FACTOR;
            _transformer = new Transformation2D(_currentTier.GetInfoLength(), _currentTier.GetInfoWidth(), _contentHost.Width, _contentHost.Height);
            // Remove old visuals (if any)
            foreach (var visual in _contentControl.Children.OfType<SimulationVisual2D>().Cast<UIElement>().ToArray())
                Remove(visual);
            foreach (var visual in _contentControl.Children.OfType<Image>().Cast<UIElement>().ToArray())
                Remove(visual);
            // --> Init visuals
            double waypointRadius = _instance.GetInfoTiers().First().GetInfoWaypoints().First().GetInfoLength() / 2.0;
            double strokeThickness = 0.2 * Math.Min(_transformer.ProjectXLength(waypointRadius), _transformer.ProjectYLength(waypointRadius));
            // Add bots
            _botVisuals = _currentTier.GetInfoBots().ToDictionary(k => k, v =>
            {
                SimulationVisualBot2D botVisual = new SimulationVisualBot2D(v, _config.DetailLevel, _transformer, strokeThickness, _elementClickAction, this);
                _infoControl.Register(v, botVisual);
                return botVisual;
            });
            // Add non-visual bots
            _shadowBotVisuals = _instance.GetInfoBots().Except(_botVisuals.Keys).ToDictionary(k => k, v =>
            {
                SimulationVisualBot2D botVisual = new SimulationVisualBot2D(v, _config.DetailLevel, _transformer, strokeThickness, _elementClickAction, this);
                _infoControl.Register(v, botVisual);
                return botVisual;
            });
            // Add pods
            _podVisuals = _currentTier.GetInfoPods().ToDictionary(k => k, v =>
            {
                SimulationVisualPod2D podVisual = new SimulationVisualPod2D(v, _config.DetailLevel, _transformer, strokeThickness, _heatModeEnabled, _elementClickAction, this);
                _infoControl.Register(v, podVisual);
                return podVisual;
            });
            // Add non-visual bots
            _shadowPodVisuals = _instance.GetInfoPods().Except(_podVisuals.Keys).ToDictionary(k => k, v =>
            {
                SimulationVisualPod2D podVisual = new SimulationVisualPod2D(v, _config.DetailLevel, _transformer, strokeThickness, _heatModeEnabled, _elementClickAction, this);
                _infoControl.Register(v, podVisual);
                return podVisual;
            });
            // Add input-stations
            _iStationVisuals = _currentTier.GetInfoInputStations().ToDictionary(k => k, v =>
            {
                SimulationVisualInputStation2D iStationVisual = new SimulationVisualInputStation2D(v, _config.DetailLevel, _transformer, strokeThickness, _elementClickAction, this);
                _infoControl.Register(v, iStationVisual);
                return iStationVisual;
            });
            // Add output-stations
            _oStationVisuals = _currentTier.GetInfoOutputStations().ToDictionary(k => k, v =>
            {
                SimulationVisualOutputStation2D oStationVisual = new SimulationVisualOutputStation2D(v, _config.DetailLevel, _transformer, strokeThickness, _elementClickAction, this);
                _infoControl.Register(v, oStationVisual);
                return oStationVisual;
            });
            // Add elevator entrances
            _elevatorEntranceVisuals = _instance.GetInfoElevators().SelectMany(e => e.GetInfoWaypoints()).Where(wp => wp.GetInfoCurrentTier() == _currentTier).ToDictionary(k => k, v =>
            {
                SimulationVisualElevatorEntrance2D elevatorEntranceVisual = new SimulationVisualElevatorEntrance2D(v, _config.DetailLevel, _transformer, strokeThickness, _elementClickAction, this);
                //_infoControl.Register(v, elevatorEntranceVisual); // TODO enable again
                return elevatorEntranceVisual;
            });
            if (_config.DetailLevel >= DetailLevel.Debug)
            {
                // Refine level of detail
                if (_config.DetailLevel >= DetailLevel.Full)
                {
                    // Add each waypoint
                    _waypointVisuals = _currentTier.GetInfoWaypoints().ToDictionary(k => k, v =>
                            {
                                SimulationVisualWaypoint2D waypointVisual = new SimulationVisualWaypoint2D(v, _config.DetailLevel, _transformer, strokeThickness, _elementClickAction, this);
                                _infoControl.Register(v, waypointVisual);
                                return waypointVisual;
                            });
                }
                else
                {
                    // Only add the complete waypoint graph without explicit information about each waypoint
                    _waypointGraphVisual = SimulationVisualWaypointGraph2D.GenerateWaypointGraphImage(_currentTier, _transformer, strokeThickness);
                }
                // Add guards
                _guardVisuals = _currentTier.GetInfoGuards().ToDictionary(k => k, v =>
                {
                    SimulationVisualGuard2D guardVisual = new SimulationVisualGuard2D(v, _config.DetailLevel, _transformer, strokeThickness, _elementClickAction, this);
                    _infoControl.Register(v, guardVisual);
                    return guardVisual;
                });
            }
            // Add if desired
            if (_config.DrawGoal)
            {
                // Add goal markers
                _botGoalMarkerVisuals = _currentTier.GetInfoBots().ToDictionary(k => k, v =>
                {
                    return new SimulationVisualGoalMarker2D(v, _config.DetailLevel, _transformer, strokeThickness, _elementClickAction, this);
                });
                // Add non-visual goal markers
                _shadowBotGoalMarkerVisuals = _instance.GetInfoBots().Except(_botGoalMarkerVisuals.Keys).ToDictionary(k => k, v =>
                {
                    return new SimulationVisualGoalMarker2D(v, _config.DetailLevel, _transformer, strokeThickness, _elementClickAction, this);
                });
            }
            // Add if desired
            if (_config.DrawDestination)
            {
                // Add destination markers
                _botDestinationMarkerVisuals = _currentTier.GetInfoBots().ToDictionary(k => k, v =>
                {
                    return new SimulationVisualDestinationMarker2D(v, _config.DetailLevel, _transformer, strokeThickness, _elementClickAction, this);
                });
                // Add non-visual destination markers
                _shadowBotDestinationMarkerVisuals = _instance.GetInfoBots().Except(_botDestinationMarkerVisuals.Keys).ToDictionary(k => k, v =>
                {
                    return new SimulationVisualDestinationMarker2D(v, _config.DetailLevel, _transformer, strokeThickness, _elementClickAction, this);
                });
            }
            // Add if desired
            if (_config.DrawPath)
            {
                // Add path markers
                _botPathVisuals = _currentTier.GetInfoBots().ToDictionary(k => k, v =>
                {
                    return new SimulationVisualPathMarker2D(v, _config.DetailLevel, _transformer, strokeThickness, _elementClickAction, this);
                });
                // Add non-visual path markers
                _shadowBotPathVisuals = _instance.GetInfoBots().Except(_botPathVisuals.Keys).ToDictionary(k => k, v =>
                {
                    return new SimulationVisualPathMarker2D(v, _config.DetailLevel, _transformer, strokeThickness, _elementClickAction, this);
                });
            }
            // --> Add the generated elements to the GUI
            // Add new waypoint visuals to the view
            if (_config.DetailLevel >= DetailLevel.Debug)
            {
                if (_config.DetailLevel >= DetailLevel.Full)
                {
                    foreach (var waypoint in _currentTier.GetInfoWaypoints())
                        Add(_waypointVisuals[waypoint]);
                }
                else
                {
                    Add(_waypointGraphVisual);
                }
            }
            // Add new iStation visuals to the view
            foreach (var iStation in _currentTier.GetInfoInputStations())
                Add(_iStationVisuals[iStation]);
            // Add new oStation visuals to the view
            foreach (var oStation in _currentTier.GetInfoOutputStations())
                Add(_oStationVisuals[oStation]);
            foreach (var elevatorEntrance in _elevatorEntranceVisuals.Keys)
                Add(_elevatorEntranceVisuals[elevatorEntrance]);
            // Add new path marker visuals to the view
            if (_config.DrawPath)
                foreach (var bot in _currentTier.GetInfoBots())
                    Add(_botPathVisuals[bot]);
            // Add new destination marker visuals to the view
            if (_config.DrawDestination)
                foreach (var bot in _currentTier.GetInfoBots())
                    Add(_botDestinationMarkerVisuals[bot]);
            // Add new pod visuals to the view
            foreach (var pod in _currentTier.GetInfoPods())
                Add(_podVisuals[pod]);
            // Add new bot visuals to the view
            foreach (var bot in _currentTier.GetInfoBots())
                Add(_botVisuals[bot]);
            // Add new goal marker visuals to the view
            if (_config.DrawGoal)
                foreach (var bot in _currentTier.GetInfoBots())
                    Add(_botGoalMarkerVisuals[bot]);
            // Add new guard visuals to the view
            if (_config.DetailLevel >= DetailLevel.Debug)
                foreach (var guard in _currentTier.GetInfoGuards())
                    Add(_guardVisuals[guard]);
            // Update the view
            Update(true);
        }

        public override void Update(bool overrideUpdate)
        {
            if (_instance.GetInfoChanged() || overrideUpdate)
            {
                _dispatcher.Invoke(() =>
                    {
                        // Get new elements and leaving elements
                        IBotInfo[] newBots = _currentTier.GetInfoBots().Except(_botVisuals.Keys).ToArray();
                        IPodInfo[] newPods = _currentTier.GetInfoPods().Except(_podVisuals.Keys).ToArray();
                        IBotInfo[] oldBots = _botVisuals.Keys.Where(b => b.GetInfoCurrentTier() != _currentTier).ToArray();
                        IPodInfo[] oldPods = _podVisuals.Keys.Where(b => b.GetInfoCurrentTier() != _currentTier).ToArray();
                        // ---> Add new visuals of the tier
                        foreach (var newBot in newBots)
                        {
                            if (_config.DrawPath)
                            {
                                // Add path marker of bot
                                _botPathVisuals.Add(newBot, _shadowBotPathVisuals[newBot]);
                                _shadowBotPathVisuals.Remove(newBot);
                                Add(_botPathVisuals[newBot]);
                            }
                            if (_config.DrawDestination)
                            {
                                // Add destination marker of bot
                                _botDestinationMarkerVisuals.Add(newBot, _shadowBotDestinationMarkerVisuals[newBot]);
                                _shadowBotDestinationMarkerVisuals.Remove(newBot);
                                Add(_botDestinationMarkerVisuals[newBot]);
                            }
                        }
                        foreach (var newPod in newPods)
                        {
                            // Add new pod
                            _podVisuals.Add(newPod, _shadowPodVisuals[newPod]);
                            _shadowPodVisuals.Remove(newPod);
                            Add(_podVisuals[newPod]);
                        }
                        foreach (var newBot in newBots)
                        {
                            // Add new bot
                            _botVisuals.Add(newBot, _shadowBotVisuals[newBot]);
                            _shadowBotVisuals.Remove(newBot);
                            Add(_botVisuals[newBot]);
                            if (_config.DrawGoal)
                            {
                                // Add goal marker of bot
                                _botGoalMarkerVisuals.Add(newBot, _shadowBotGoalMarkerVisuals[newBot]);
                                _shadowBotGoalMarkerVisuals.Remove(newBot);
                                Add(_botGoalMarkerVisuals[newBot]);
                            }
                        }
                        // ---> Remove visuals not part of this tier
                        foreach (var oldPod in oldPods)
                        {
                            // Remove pod
                            _shadowPodVisuals.Add(oldPod, _podVisuals[oldPod]);
                            _podVisuals.Remove(oldPod);
                            Remove(_shadowPodVisuals[oldPod]);
                        }
                        foreach (var oldBot in oldBots)
                        {
                            // Remove bot
                            _shadowBotVisuals.Add(oldBot, _botVisuals[oldBot]);
                            _botVisuals.Remove(oldBot);
                            Remove(_shadowBotVisuals[oldBot]);
                            if (_config.DrawGoal)
                            {
                                // Remove goal marker of bot
                                _shadowBotGoalMarkerVisuals.Add(oldBot, _botGoalMarkerVisuals[oldBot]);
                                _botGoalMarkerVisuals.Remove(oldBot);
                                Remove(_shadowBotGoalMarkerVisuals[oldBot]);
                            }
                            if (_config.DrawDestination)
                            {
                                // Remove destination marker of bot
                                _shadowBotDestinationMarkerVisuals.Add(oldBot, _botDestinationMarkerVisuals[oldBot]);
                                _botDestinationMarkerVisuals.Remove(oldBot);
                                Remove(_shadowBotDestinationMarkerVisuals[oldBot]);
                            }
                            if (_config.DrawPath)
                            {
                                // Remove path marker of bot
                                _shadowBotPathVisuals.Add(oldBot, _botPathVisuals[oldBot]);
                                _botPathVisuals.Remove(oldBot);
                                Remove(_shadowBotPathVisuals[oldBot]);
                            }
                        }

                        // ---> Update movable visual objects
                        foreach (var botVisual in _botVisuals.Values)
                            botVisual.UpdateTransformation(overrideUpdate);
                        foreach (var podVisual in _podVisuals.Values)
                            podVisual.UpdateTransformation(overrideUpdate);
                        // Update goal markers if desired
                        if (_config.DrawGoal)
                            foreach (var goalVisual in _botGoalMarkerVisuals.Values)
                                goalVisual.Update();
                        // Update destination markers if desired
                        if (_config.DrawDestination)
                            foreach (var destinationVisual in _botDestinationMarkerVisuals.Values)
                                destinationVisual.Update();
                        // Update path markers if desired
                        if (_config.DrawPath)
                            foreach (var pathVisual in _botPathVisuals.Values)
                                pathVisual.Update();
                        // Update guards if in debug level
                        if (_config.DetailLevel >= DetailLevel.Debug)
                            foreach (var guardVisual in _guardVisuals.Values)
                                guardVisual.Update();
                    });
            }
        }

        public override void StopAnimation()
        {
            // Nothing to see here
        }

        public override void TakeSnapshot(string snapshotDir, string snapshotFilename = null)
        {
            // Get the bounds of the stuff to render
            Rect bounds = System.Windows.Media.VisualTreeHelper.GetDescendantBounds(_contentControl);
            // Scale dimensions from 96 dpi to 300 dpi.
            double scale = 300 / 96;
            // Init the image
            RenderTargetBitmap bitmap = new RenderTargetBitmap((int)(scale * (bounds.Width + 1)), (int)(scale * (bounds.Height + 1)), scale * 96, scale * 96, System.Windows.Media.PixelFormats.Default);
            // Render the complete control
            bitmap.Render(_contentControl);
            // Init encoder
            BitmapEncoder pngEncoder = new PngBitmapEncoder();
            // Add the frame
            pngEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            // Write the image to the given export location using the instance name and a timestamp
            using (System.IO.FileStream stream = System.IO.File.Create(System.IO.Path.Combine(snapshotDir,
                // If a filename is given, use it
                snapshotFilename != null ?
                    // If the filename does not already end with .png, append it
                    snapshotFilename.EndsWith(".png") ?
                        snapshotFilename :
                        snapshotFilename + ".png" :
                // No filename given, use a date timestamp instead
                _instance.GetInfoName() + "-2D-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".png")))
            {
                // Actually save the picture
                pngEncoder.Save(stream);
            }
            // Copy the image to the clipboard
            //Clipboard.SetImage(rtb); // TODO remove unused clipboard
        }
    }
}
