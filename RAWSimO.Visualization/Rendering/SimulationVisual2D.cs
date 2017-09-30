using RAWSimO.Core.Info;
using RAWSimO.Core.IO;
using RAWSimO.Toolbox;
using RAWSimO.VisualToolbox.Arrows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RAWSimO.Visualization.Rendering
{
    #region Point and length transformations

    /// <summary>
    /// Exposes different projection functions to transform values of the original simulation area onto the drawing canvas.
    /// </summary>
    public class Transformation2D
    {
        public Transformation2D(double tierLength, double tierWidth, double canvasWidth, double canvasHeight)
        {
            OriginalXLength = tierLength;
            OriginalYLength = tierWidth;
            ProjectionXLength = canvasWidth;
            ProjectionYLength = canvasHeight;
        }

        /// <summary>
        /// The length of the original area.
        /// </summary>
        public double OriginalXLength { get; private set; }
        /// <summary>
        /// The width of the original area.
        /// </summary>
        public double OriginalYLength { get; private set; }
        /// <summary>
        /// The length of the projection area.
        /// </summary>
        public double ProjectionXLength { get; private set; }
        /// <summary>
        /// The width of the projection area.
        /// </summary>
        public double ProjectionYLength { get; private set; }

        /// <summary>
        /// Projects a given x-coordinate onto the managed canvas.
        /// </summary>
        /// <param name="simulationX">The x-coordinate to project.</param>
        /// <returns>The projected value.</returns>
        public double ProjectX(double simulationX) { return simulationX / OriginalXLength * ProjectionXLength; }
        /// <summary>
        /// Projects a given x-coordinate of the UI into the managed instance coordinate system.
        /// </summary>
        /// <param name="simulationX">The x-coordinate to project.</param>
        /// <returns>The projected value.</returns>
        public double RevertX(double uiX) { return uiX / ProjectionXLength * OriginalXLength; }

        /// <summary>
        /// Projects a given y-coordinate onto the managed canvas.
        /// </summary>
        /// <param name="simulationY">The y-coordinate to project.</param>
        /// <returns>The projected value.</returns>
        public double ProjectY(double simulationY) { return (1.0 - simulationY / OriginalYLength) * ProjectionYLength; }
        /// <summary>
        /// Projects a given y-coordinate of the UI into the managed instance coordinate system.
        /// </summary>
        /// <param name="uiY">The y-coordinate to project.</param>
        /// <returns>The projected value.</returns>
        public double RevertY(double uiY) { return (1.0 - uiY / ProjectionYLength) * OriginalYLength; }

        /// <summary>
        /// Projects a given x-length onto the managed canvas.
        /// </summary>
        /// <param name="simulationXLength">The x-length to project.</param>
        /// <returns>The projected value.</returns>
        public double ProjectXLength(double simulationXLength) { return simulationXLength / OriginalXLength * ProjectionXLength; }
        /// <summary>
        /// Projects a given x-length of the UI into the managed instance coordinate system.
        /// </summary>
        /// <param name="uiXLength">The x-length to project.</param>
        /// <returns>The projected value.</returns>
        public double RevertXLength(double uiXLength) { return uiXLength / ProjectionXLength * OriginalXLength; }

        /// <summary>
        /// Projects a given y-length onto the managed canvas.
        /// </summary>
        /// <param name="simulationYLength">The y-length to project.</param>
        /// <returns>The projected value.</returns>
        public double ProjectYLength(double simulationYLength) { return simulationYLength / OriginalYLength * ProjectionYLength; }
        /// <summary>
        /// Projects a given y-length of the UI into the managed instance coordinate system.
        /// </summary>
        /// <param name="uiYLength">The y-length to project.</param>
        /// <returns>The projected value.</returns>
        public double RevertYLength(double uiYLength) { return uiYLength / ProjectionYLength * OriginalYLength; }

        /// <summary>
        /// Projects a given radian into the degree representation.
        /// </summary>
        /// <param name="radianOrientation">The radian to project.</param>
        /// <returns>The projected value.</returns>
        public static double ProjectOrientation(double radianOrientation) { return ((2.0 * Math.PI - radianOrientation) / (2.0 * Math.PI)) * 360.0; }
    }

    #endregion

    #region Base classes

    public abstract class SimulationVisual2D : Shape
    {
        public SimulationVisual2D(DetailLevel detailLevel, Transformation2D transformer, double strokeThickness, MouseButtonEventHandler elementClickAction, SimulationAnimation2D controller)
        {
            _detailLevel = detailLevel;
            _transformer = transformer;
            StrokeThicknessReference = strokeThickness;
            _elementClickAction = elementClickAction;
            _animationController = controller;
        }

        protected readonly DetailLevel _detailLevel;
        protected readonly Transformation2D _transformer;
        protected readonly MouseButtonEventHandler _elementClickAction;
        protected readonly SimulationAnimation2D _animationController;
        internal double StrokeThicknessReference { get; private set; }
    }

    public abstract class SimulationVisualImmovable2D : SimulationVisual2D
    {
        public SimulationVisualImmovable2D(
            IImmovableObjectInfo immoveableObject,
            DetailLevel detailLevel,
            Transformation2D transformer,
            double strokeThickness,
            MouseButtonEventHandler elementClickAction,
            SimulationAnimation2D controller)
            : base(detailLevel, transformer, strokeThickness, elementClickAction, controller)
        {
            _immoveableObject = immoveableObject;
        }

        protected readonly IImmovableObjectInfo _immoveableObject;
    }

    public abstract class SimulationVisualMovable2D : SimulationVisual2D
    {
        public SimulationVisualMovable2D(
            IMovableObjectInfo moveableObject,
            DetailLevel detailLevel,
            Transformation2D transformer,
            double strokeThickness,
            MouseButtonEventHandler elementClickAction,
            SimulationAnimation2D controller)
            : base(detailLevel, transformer, strokeThickness, elementClickAction, controller)
        {
            _moveableObject = moveableObject;
        }

        protected readonly IMovableObjectInfo _moveableObject;

        protected void Init()
        {
            // Init position
            Canvas.SetLeft(this, _transformer.ProjectX(_moveableObject.GetInfoCenterX()));
            Canvas.SetTop(this, _transformer.ProjectY(_moveableObject.GetInfoCenterY()));
            // Update meta info
            UpdateMetaInfo();
        }

        public void UpdateTransformation(bool overrideUpdate)
        {
            if (_moveableObject.GetInfoChanged() || overrideUpdate)
            {
                // Set new position
                Canvas.SetLeft(this, _transformer.ProjectX(_moveableObject.GetInfoCenterX()));
                Canvas.SetTop(this, _transformer.ProjectY(_moveableObject.GetInfoCenterY()));
                // Set new orientation
                this.RenderTransform = new RotateTransform(Transformation2D.ProjectOrientation(_moveableObject.GetInfoOrientation()));
            }
            // Update meta info
            UpdateMetaInfo();
        }

        public abstract void UpdateMetaInfo();
    }

    #endregion

    #region Immovable objects

    public class SimulationVisualInputStation2D : SimulationVisualImmovable2D
    {
        private readonly IInputStationInfo _iStation;
        private readonly RectangleGeometry _geometry;

        public SimulationVisualInputStation2D(
            IInputStationInfo iStation,
            DetailLevel detailLevel,
            Transformation2D transformer,
            double strokeThickness,
            MouseButtonEventHandler elementClickAction,
            SimulationAnimation2D controller)
            : base(iStation, detailLevel, transformer, strokeThickness, elementClickAction, controller)
        {
            _iStation = iStation;
            // Build geometry
            _geometry =
                new RectangleGeometry(
                    new Rect(
                        new Point(_transformer.ProjectX(_iStation.GetInfoTLX()), _transformer.ProjectY(_iStation.GetInfoTLY())),
                        new Size(_transformer.ProjectXLength(_iStation.GetInfoLength()), _transformer.ProjectYLength(_iStation.GetInfoWidth()))));
            // Paint it
            Fill = VisualizationConstants.BrushInputStationVisual;
            Cursor = System.Windows.Input.Cursors.Hand;
            MouseDown += _elementClickAction;
            Stroke = VisualizationConstants.BrushOutline;
            StrokeThickness = StrokeThicknessReference;
        }

        protected override Geometry DefiningGeometry { get { return _geometry; } }
    }

    public class SimulationVisualOutputStation2D : SimulationVisualImmovable2D
    {
        private readonly IOutputStationInfo _oStation;
        private readonly RectangleGeometry _geometry;

        public SimulationVisualOutputStation2D(
            IOutputStationInfo oStation,
            DetailLevel detailLevel,
            Transformation2D transformer,
            double strokeThickness,
            MouseButtonEventHandler elementClickAction,
            SimulationAnimation2D controller)
            : base(oStation, detailLevel, transformer, strokeThickness, elementClickAction, controller)
        {
            _oStation = oStation;
            // Build geometry
            _geometry =
                new RectangleGeometry(
                    new Rect(
                        new Point(_transformer.ProjectX(_oStation.GetInfoTLX()), _transformer.ProjectY(_oStation.GetInfoTLY())),
                        new Size(_transformer.ProjectXLength(_oStation.GetInfoLength()), _transformer.ProjectYLength(_oStation.GetInfoWidth()))));
            // Paint it
            Fill = VisualizationConstants.BrushOutputStationVisual;
            Cursor = System.Windows.Input.Cursors.Hand;
            MouseDown += _elementClickAction;
            Stroke = VisualizationConstants.BrushOutline;
            StrokeThickness = StrokeThicknessReference;
        }

        protected override Geometry DefiningGeometry { get { return _geometry; } }
    }

    public class SimulationVisualWaypoint2D : SimulationVisualImmovable2D
    {
        private readonly IWaypointInfo _waypoint;
        private readonly GeometryGroup _geometry = new GeometryGroup();

        public SimulationVisualWaypoint2D(
            IWaypointInfo waypoint,
            DetailLevel detailLevel,
            Transformation2D transformer,
            double strokeThickness,
            MouseButtonEventHandler elementClickAction,
            SimulationAnimation2D controller)
            : base(waypoint, detailLevel, transformer, strokeThickness, elementClickAction, controller)
        {
            _waypoint = waypoint;
            // Create waypoint dot
            EllipseGeometry ellipse =
                new EllipseGeometry(
                    new Point(_transformer.ProjectX(waypoint.GetInfoCenterX()), _transformer.ProjectY(waypoint.GetInfoCenterY())),
                    _transformer.ProjectXLength(waypoint.GetInfoLength() / 2.0), _transformer.ProjectYLength(waypoint.GetInfoLength() / 2.0));
            _geometry.Children.Add(ellipse);
            // Add connections
            foreach (var otherWP in waypoint.GetInfoConnectedWaypoints())
            {
                Point start = new Point(_transformer.ProjectX(waypoint.GetInfoCenterX()), _transformer.ProjectY(waypoint.GetInfoCenterY()));
                Point end = new Point(_transformer.ProjectX(otherWP.GetInfoCenterX()), _transformer.ProjectY(otherWP.GetInfoCenterY()));
                double length = ArrowLineGeometryGenerator.GetDistanceBetweenPoints(start, end);
                var geom = ArrowLineGeometryGenerator.GenerateArrowGeometry(ArrowEnds.End, start, end, 45, 0.2 * length);
                _geometry.Children.Add(geom);
            }
            // Paint it
            Fill = VisualizationConstants.BrushWaypointVisual;
            Cursor = System.Windows.Input.Cursors.Hand;
            MouseDown += _elementClickAction;
            Stroke = VisualizationConstants.BrushOutline;
            StrokeThickness = StrokeThicknessReference;
        }

        protected override Geometry DefiningGeometry { get { return _geometry; } }
    }

    public class SimulationVisualElevatorEntrance2D : SimulationVisualImmovable2D
    {
        private readonly IWaypointInfo _waypoint;
        private readonly GeometryGroup _geometry = new GeometryGroup();
        public const double SIZE_FACTOR = 3;

        public SimulationVisualElevatorEntrance2D(
            IWaypointInfo waypoint,
            DetailLevel detailLevel,
            Transformation2D transformer,
            double strokeThickness,
            MouseButtonEventHandler elementClickAction,
            SimulationAnimation2D controller)
            : base(waypoint, detailLevel, transformer, strokeThickness, elementClickAction, controller)
        {
            _waypoint = waypoint;
            // Create waypoint dot
            EllipseGeometry ellipse =
                new EllipseGeometry(
                    new Point(_transformer.ProjectX(waypoint.GetInfoCenterX()), _transformer.ProjectY(waypoint.GetInfoCenterY())),
                    _transformer.ProjectXLength(waypoint.GetInfoLength() / 2.0 * SIZE_FACTOR), _transformer.ProjectYLength(waypoint.GetInfoLength() / 2.0 * SIZE_FACTOR));
            _geometry.Children.Add(ellipse);
            // Paint it
            Fill = VisualizationConstants.BrushElevatorEntranceVisual;
            Cursor = System.Windows.Input.Cursors.Hand;
            MouseDown += _elementClickAction;
            Stroke = VisualizationConstants.BrushOutline;
            StrokeThickness = StrokeThicknessReference;
        }

        protected override Geometry DefiningGeometry { get { return _geometry; } }
    }

    public class SimulationVisualGuard2D : SimulationVisual2D
    {
        private readonly IGuardInfo _guard;
        private readonly EllipseGeometry _geometry;

        public SimulationVisualGuard2D(
            IGuardInfo guard,
            DetailLevel detailLevel,
            Transformation2D transformer,
            double strokeThickness,
            MouseButtonEventHandler elementClickAction,
            SimulationAnimation2D controller)
            : base(detailLevel, transformer, strokeThickness, elementClickAction, controller)
        {
            _guard = guard;
            // Create waypoint dot
            _geometry =
                new EllipseGeometry(
                    new Point(_transformer.ProjectX(guard.GetInfoCenterX()), _transformer.ProjectY(guard.GetInfoCenterY())),
                    _transformer.ProjectXLength(guard.GetInfoLength() / 2.0), _transformer.ProjectYLength(guard.GetInfoLength() / 2.0));
            // Paint it
            Fill = _guard.GetInfoIsBarrier() ? VisualizationConstants.BrushSemaphoreEntry : VisualizationConstants.BrushSemaphoreGuard;
            Cursor = System.Windows.Input.Cursors.Hand;
            MouseDown += _elementClickAction;
            Stroke = VisualizationConstants.BrushOutline;
            StrokeThickness = StrokeThicknessReference;
        }

        public void Update()
        {
            // Draw current goal of the bot when in debug mode
            if (_detailLevel >= DetailLevel.Debug)
            {
                Fill = _guard.GetInfoIsAccessible() ?
                    (_guard.GetInfoIsBarrier() ? VisualizationConstants.BrushSemaphoreEntry : VisualizationConstants.BrushSemaphoreGuard) :
                    VisualizationConstants.BrushSemaphoreBlocked;
            }
        }

        protected override Geometry DefiningGeometry { get { return _geometry; } }
    }

    #endregion

    #region Movable objects

    public class SimulationVisualPathMarker2D : SimulationVisual2D
    {
        private readonly IBotInfo _bot;
        private readonly GeometryGroup _geometry = new GeometryGroup();
        private List<IWaypointInfo> _currentPath;
        private List<IWaypointInfo> _remainingPath;

        public SimulationVisualPathMarker2D(
            IBotInfo bot,
            DetailLevel detailLevel,
            Transformation2D transformer,
            double strokeThickness,
            MouseButtonEventHandler elementClickAction,
            SimulationAnimation2D controller)
            : base(detailLevel, transformer, strokeThickness, elementClickAction, controller)
        {
            _bot = bot;
            // Init geometry
            Stroke = VisualizationConstants.BrushGoalMarker;
            StrokeThickness = StrokeThicknessReference * VisualizationConstants.PATH_MARKER_STROKE_THICKNESS_FACTOR;
        }

        public void Update()
        {
            // Get path
            List<IWaypointInfo> _botPath = _bot.GetInfoPath();
            IWaypointInfo _botCurrentWaypoint = _bot.GetInfoCurrentWaypoint();
            string state = _bot.GetInfoState();
            // Update color
            Brush currentColor = _animationController.GetBotColor(_bot, state);
            if (currentColor != Stroke)
                Stroke = currentColor;
            // If we do not have a path, cleanup and quit
            if (_botPath == null || !_botPath.Any() || state != "Move")
            {
                if (_geometry.Children.Count > 0)
                    _geometry.Children.Clear();
                return;
            }
            // If the path changed, update it
            if (_currentPath != _botPath)
            {
                // Store it
                _currentPath = _remainingPath = _botPath;
                // Remove old path
                _geometry.Children.Clear();
                // Add all connections
                IWaypointInfo previousNode = null;
                foreach (var currentNode in _currentPath)
                {
                    // Check whether it's the first node of the path
                    if (previousNode == null)
                    {
                        // Only store first node
                        previousNode = currentNode;
                    }
                    else
                    {
                        // Draw path
                        _geometry.Children.Add(
                            new LineGeometry(
                                // From start
                                new Point(_transformer.ProjectX(previousNode.GetInfoCenterX()), _transformer.ProjectY(previousNode.GetInfoCenterY())),
                                // To destination
                                new Point(_transformer.ProjectX(currentNode.GetInfoCenterX()), _transformer.ProjectY(currentNode.GetInfoCenterY()))));
                        // Update previous node
                        previousNode = currentNode;
                    }
                }
            }
        }

        protected override Geometry DefiningGeometry { get { return _geometry; } }
    }

    public class SimulationVisualGoalMarker2D : SimulationVisual2D
    {
        private readonly IBotInfo _bot;
        private readonly LineGeometry _geometry;

        public SimulationVisualGoalMarker2D(
            IBotInfo bot,
            DetailLevel detailLevel,
            Transformation2D transformer,
            double strokeThickness,
            MouseButtonEventHandler elementClickAction,
            SimulationAnimation2D controller)
            : base(detailLevel, transformer, strokeThickness, elementClickAction, controller)
        {
            _bot = bot;
            // Init geometry
            _geometry = new LineGeometry(new Point(0, 0), new Point(0, 0));
            Stroke = VisualizationConstants.BrushGoalMarker;
            StrokeThickness = StrokeThicknessReference * VisualizationConstants.GOAL_MARKER_STROKE_THICKNESS_FACTOR;
        }

        public void Update()
        {
            // Update color
            Brush currentColor = _animationController.GetBotColor(_bot, _bot.GetInfoState());
            if (currentColor != Stroke)
                Stroke = currentColor;
            // Draw current goal of the bot
            _geometry.StartPoint = new Point(_transformer.ProjectX(_bot.GetInfoCenterX()), _transformer.ProjectY(_bot.GetInfoCenterY()));
            if (!double.IsNaN(_bot.GetInfoGoalX()))
                _geometry.EndPoint = new Point(_transformer.ProjectX(_bot.GetInfoGoalX()), _transformer.ProjectY(_bot.GetInfoGoalY()));
            else
                _geometry.EndPoint = new Point(_transformer.ProjectX(_bot.GetInfoCenterX()), _transformer.ProjectY(_bot.GetInfoCenterY()));
        }

        protected override Geometry DefiningGeometry { get { return _geometry; } }
    }

    public class SimulationVisualDestinationMarker2D : SimulationVisual2D
    {
        private readonly IBotInfo _bot;
        private readonly LineGeometry _geometry;

        public SimulationVisualDestinationMarker2D(
            IBotInfo bot,
            DetailLevel detailLevel,
            Transformation2D transformer,
            double strokeThickness,
            MouseButtonEventHandler elementClickAction,
            SimulationAnimation2D controller)
            : base(detailLevel, transformer, strokeThickness, elementClickAction, controller)
        {
            _bot = bot;
            // Init geometry
            _geometry = new LineGeometry(new Point(0, 0), new Point(0, 0));
            Stroke = VisualizationConstants.BrushDestinationMarker;
            StrokeThickness = StrokeThicknessReference * VisualizationConstants.DESTINATION_MARKER_STROKE_THICKNESS_FACTOR;
        }

        public void Update()
        {
            // Update color
            Brush currentColor = _animationController.GetBotColor(_bot, _bot.GetInfoState());
            if (currentColor != Stroke)
                Stroke = currentColor;
            // Draw current goal of the bot
            _geometry.StartPoint = new Point(_transformer.ProjectX(_bot.GetInfoCenterX()), _transformer.ProjectY(_bot.GetInfoCenterY()));
            IWaypointInfo destination = _bot.GetInfoDestinationWaypoint();
            if (destination != null)
                _geometry.EndPoint = new Point(_transformer.ProjectX(destination.GetInfoCenterX()), _transformer.ProjectY(destination.GetInfoCenterY()));
            else
                _geometry.EndPoint = new Point(_transformer.ProjectX(_bot.GetInfoCenterX()), _transformer.ProjectY(_bot.GetInfoCenterY()));
        }

        protected override Geometry DefiningGeometry { get { return _geometry; } }
    }

    public class SimulationVisualBot2D : SimulationVisualMovable2D
    {
        private readonly IBotInfo _bot;
        private readonly GeometryGroup _geometry = new GeometryGroup();

        public SimulationVisualBot2D(
            IBotInfo bot,
            DetailLevel detailLevel,
            Transformation2D transformer,
            double strokeThickness,
            MouseButtonEventHandler elementClickAction,
            SimulationAnimation2D controller)
            : base(bot, detailLevel, transformer, strokeThickness, elementClickAction, controller)
        {
            _bot = bot;
            // Build geometry
            _geometry.Children.Add(new EllipseGeometry(
                    new Point(0, 0),
                    _transformer.ProjectXLength(_bot.GetInfoRadius()), _transformer.ProjectYLength(_bot.GetInfoRadius())));
            // Add orientation marker
            double orientationMarkerX = _transformer.ProjectXLength(_bot.GetInfoRadius()) * Math.Cos(0);
            double orientationMarkerY = _transformer.ProjectXLength(_bot.GetInfoRadius()) * Math.Sin(0);
            _geometry.Children.Add(new LineGeometry(
                new Point(0, 0),
                new Point(orientationMarkerX, orientationMarkerY)));
            // Paint it
            Fill = VisualizationConstants.BrushBotVisual;
            Cursor = System.Windows.Input.Cursors.Hand;
            MouseDown += _elementClickAction;
            Stroke = VisualizationConstants.BrushOutline;
            StrokeThickness = StrokeThicknessReference;
            // Initialize
            Init();
        }

        public override void UpdateMetaInfo()
        {
            // Update color
            string state = _bot.GetInfoState();
            Brush currentColor = _animationController.GetBotColor(_bot, state);
            if (currentColor != Fill)
                Fill = currentColor;
        }

        protected override Geometry DefiningGeometry { get { return _geometry; } }
    }

    public class SimulationVisualPod2D : SimulationVisualMovable2D
    {
        private readonly IPodInfo _pod;
        private readonly Func<bool> _heatModeEnabled;
        private readonly RectangleGeometry _geometry;

        public SimulationVisualPod2D(
            IPodInfo pod,
            DetailLevel detailLevel,
            Transformation2D transformer,
            double strokeThickness,
            Func<bool> heatModeEnabled,
            MouseButtonEventHandler elementClickAction,
            SimulationAnimation2D controller)
            : base(pod, detailLevel, transformer, strokeThickness, elementClickAction, controller)
        {
            _pod = pod;
            _heatModeEnabled = heatModeEnabled;
            // Build geometry
            _geometry =
                new RectangleGeometry(
                    new Rect(
                        new Point(-_transformer.ProjectXLength(_pod.GetInfoRadius()), -_transformer.ProjectYLength(_pod.GetInfoRadius())),
                        new Size(_transformer.ProjectXLength(_pod.GetInfoRadius() * 2), _transformer.ProjectYLength(_pod.GetInfoRadius() * 2))));
            // Paint it
            Fill = VisualizationConstants.BrushPodVisual;
            Cursor = System.Windows.Input.Cursors.Hand;
            MouseDown += _elementClickAction;
            Stroke = VisualizationConstants.BrushOutline;
            StrokeThickness = StrokeThicknessReference;
            // Initialize
            Init();
        }

        public override void UpdateMetaInfo()
        {
            if (_heatModeEnabled())
            {
                Brush heatBrush = HeatVisualizer.GenerateHeatBrush(_pod.GetInfoHeatValue());
                if (Fill != heatBrush)
                    Fill = heatBrush;
            }
            else
            {
                if (Fill != VisualizationConstants.BrushPodVisual)
                    Fill = VisualizationConstants.BrushPodVisual;
            }
        }

        protected override Geometry DefiningGeometry { get { return _geometry; } }
    }

    #endregion

    #region Image based objects

    public class SimulationVisualWaypointGraph2D
    {
        public static Image GenerateWaypointGraphImage(ITierInfo tier, Transformation2D transformer, double strokeThickness)
        {
            // Init image
            Image image = new Image();
            image.Stretch = Stretch.Fill;
            image.SnapsToDevicePixels = true;
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            WriteableBitmap writeableBitmap = BitmapFactory.New((int)transformer.ProjectionXLength, (int)transformer.ProjectionYLength);
            // Create complete waypoint graph
            SymmetricKeyDictionary<IWaypointInfo, bool> connections = new SymmetricKeyDictionary<IWaypointInfo, bool>();
            foreach (var waypoint in tier.GetInfoWaypoints())
            {
                // Create and remember connections (while doing so bidirectional connections are implicitly combined to one)
                foreach (var otherWP in waypoint.GetInfoConnectedWaypoints()) { connections[waypoint, otherWP] = true; }
            }
            // Create connections
            foreach (var connection in connections.KeysCombined)
            {
                writeableBitmap.DrawLineAa(
                    (int)transformer.ProjectX(connection.Item1.GetInfoCenterX()),
                    (int)transformer.ProjectY(connection.Item1.GetInfoCenterY()),
                    (int)transformer.ProjectX(connection.Item2.GetInfoCenterX()),
                    (int)transformer.ProjectY(connection.Item2.GetInfoCenterY()),
                    Colors.Black,
                    (int)Math.Ceiling(strokeThickness * 3));
            }
            // Return it
            image.Source = writeableBitmap;
            return image;
        }
    }

    #endregion
}
