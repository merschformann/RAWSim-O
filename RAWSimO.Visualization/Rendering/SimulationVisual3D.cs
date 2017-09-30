using RAWSimO.Core.Info;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace RAWSimO.Visualization.Rendering
{
    #region Base classes

    public abstract class SimulationVisual3D : ModelVisual3D
    {
        protected readonly IGeneralObjectInfo _managedObject;

        public SimulationVisual3D(IGeneralObjectInfo managedObject)
        {
            _managedObject = managedObject;
        }
    }

    public abstract class SimulationVisualImmovable3D : SimulationVisual3D
    {
        protected readonly IImmovableObjectInfo _immoveableObject;

        public SimulationVisualImmovable3D(IImmovableObjectInfo immoveableObject)
            : base(immoveableObject)
        {
            _immoveableObject = immoveableObject;
        }
    }

    public abstract class SimulationVisualMovable3D : SimulationVisual3D
    {
        protected readonly IMovableObjectInfo _moveableObject;
        protected readonly double _height;
        internal ProjectionCamera _camera;
        private bool _onboardCameraAttached;

        public SimulationVisualMovable3D(IMovableObjectInfo moveableObject)
            : base(moveableObject)
        {
            // Store object
            _moveableObject = moveableObject;
            // Calculate height
            if (this is SimulationVisualPod3D)
                _height = _moveableObject.GetInfoRadius() * 4;
            else
                if (this is SimulationVisualBot3D)
                _height = _moveableObject.GetInfoRadius() * 0.9;
            else
                throw new ArgumentException("Unknown visual: " + this.GetType().ToString());
            // Add transforms
            Transform3DGroup tg = new Transform3DGroup();
            tg.Children.Add(new TranslateTransform3D(
                _moveableObject.GetInfoCurrentTier().GetInfoTLX() + _moveableObject.GetInfoCenterX(),
                _moveableObject.GetInfoCurrentTier().GetInfoTLY() + _moveableObject.GetInfoCenterY(),
                _moveableObject.GetInfoCurrentTier().GetInfoZ() + GetZ()));
            tg.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), moveableObject.GetInfoOrientation() / (2 * Math.PI) * 360)));
            Transform = tg;
        }

        public void StartOnboardCamera(ProjectionCamera camera)
        {
            _camera = camera;
            _onboardCameraAttached = true;
        }

        public void StopOnboardCamera()
        {
            _onboardCameraAttached = false;
        }

        public abstract double GetZ();

        public void UpdateTransformation(bool overrideUpdate)
        {
            if (_moveableObject.GetInfoChanged() || overrideUpdate)
            {
                // Fetch necessary information
                ITierInfo tier = _moveableObject.GetInfoCurrentTier();
                if (tier != null)
                {
                    // Get position
                    double xOffset = tier.GetInfoTLX() + _moveableObject.GetInfoCenterX();
                    double yOffset = tier.GetInfoTLY() + _moveableObject.GetInfoCenterY();
                    double zOffset = tier.GetInfoZ() + GetZ();
                    double orientationInDegrees = _moveableObject.GetInfoOrientation() / (2 * Math.PI) * 360;
                    //// Fetch transformers // TODO is it really necessary to "new" the transformer objects?
                    //Transform3DGroup tg = (Transform3DGroup)Transform;
                    //TranslateTransform3D positionTransform = (TranslateTransform3D)tg.Children.First();
                    //RotateTransform3D rotationTransform = (RotateTransform3D)tg.Children.Last();
                    //// Set new information
                    //positionTransform.OffsetX = xOffset;
                    //positionTransform.OffsetY = yOffset;
                    //positionTransform.OffsetZ = zOffset;
                    // Update visual object itself
                    Transform3DGroup tg = new Transform3DGroup();
                    tg.Children.Add(new TranslateTransform3D(xOffset, yOffset, zOffset));
                    tg.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), _moveableObject.GetInfoOrientation() / (2 * Math.PI) * 360), new Point3D(xOffset, yOffset, zOffset)));
                    Transform = tg;
                    // Update onboard camera, if attached
                    if (_onboardCameraAttached)
                    {
                        double angle = _moveableObject.GetInfoOrientation() + 0.5 * Math.PI;
                        CameraHelper.AnimateTo(
                            _camera, // The camera to adjust
                            new Point3D(xOffset, yOffset, tier.GetInfoZ() + _height + 0.05), // The position of the camera
                            new Vector3D(Math.Sin(angle), -Math.Cos(angle), 0), // The direction the camera is facing
                            new Vector3D(0, 0, 1), // The roll of the camera - just keep it upright
                            0); // The animation time
                    }
                }
            }
            // Update meta info
            UpdateMetaInfo();
        }

        public abstract void UpdateMetaInfo();
    }

    #endregion

    #region Immoveable objects

    public class SimulationVisualTier3D : SimulationVisualImmovable3D
    {
        public const double TIER_HEIGHT = 0.05;
        private readonly ITierInfo _tier;

        public SimulationVisualTier3D(ITierInfo tier, DetailLevel detailLevel)
            : base(tier)
        {
            _tier = tier;
            var visual = new BoxVisual3D
            {
                Fill = VisualizationConstants.BrushTierVisual,
                Center = new Point3D(
                    (_tier.GetInfoTLX() + _tier.GetInfoLength()) / 2.0,
                    (_tier.GetInfoTLY() + _tier.GetInfoWidth()) / 2.0,
                    _tier.GetInfoZ() - TIER_HEIGHT / 2.0),
                Length = _tier.GetInfoLength(),
                Width = _tier.GetInfoWidth(),
                Height = TIER_HEIGHT
            };
            Children.Add(visual);
        }
    }

    public class SimulationVisualElevatorEntrance3D : SimulationVisualImmovable3D
    {
        public const double ELEVATOR_HEIGHT = 0.05;
        public const double SIZE_FACTOR = 3;
        private readonly IWaypointInfo _elevator;

        public SimulationVisualElevatorEntrance3D(IWaypointInfo elevator, DetailLevel detailLevel)
            : base(elevator)
        {
            _elevator = elevator;
            var visual = new BoxVisual3D
            {
                Fill = VisualizationConstants.BrushElevatorEntranceVisual,
                Center = new Point3D(
                    _elevator.GetInfoCurrentTier().GetInfoTLX() + _elevator.GetInfoCenterX(),
                    _elevator.GetInfoCurrentTier().GetInfoTLY() + _elevator.GetInfoCenterY(),
                    _elevator.GetInfoCurrentTier().GetInfoZ() + ELEVATOR_HEIGHT / 2.0
                    ),
                Length = _elevator.GetInfoLength() * SIZE_FACTOR,
                Width = _elevator.GetInfoWidth() * SIZE_FACTOR,
                Height = ELEVATOR_HEIGHT
            };
            Children.Add(visual);
        }
    }

    public class SimulationVisualInputStation3D : SimulationVisualImmovable3D
    {
        public const double STATION_HEIGHT = 0.05;
        private readonly IInputStationInfo _iStation;

        public SimulationVisualInputStation3D(IInputStationInfo iStation, DetailLevel detailLevel)
            : base(iStation)
        {
            _iStation = iStation;
            var visual = new BoxVisual3D
            {
                Fill = VisualizationConstants.BrushInputStationVisual,
                Center = new Point3D(
                    _iStation.GetInfoCurrentTier().GetInfoTLX() + _iStation.GetInfoCenterX(),
                    _iStation.GetInfoCurrentTier().GetInfoTLY() + _iStation.GetInfoCenterY(),
                    _iStation.GetInfoCurrentTier().GetInfoZ() + STATION_HEIGHT / 2.0
                    ),
                Length = _iStation.GetInfoLength(),
                Width = _iStation.GetInfoWidth(),
                Height = STATION_HEIGHT
            };
            Children.Add(visual);
        }
    }

    public class SimulationVisualOutputStation3D : SimulationVisualImmovable3D
    {
        public const double STATION_HEIGHT = 0.05;
        private readonly IOutputStationInfo _oStation;

        public SimulationVisualOutputStation3D(IOutputStationInfo oStation, DetailLevel detailLevel)
            : base(oStation)
        {
            _oStation = oStation;
            var visual = new BoxVisual3D
            {
                Fill = VisualizationConstants.BrushOutputStationVisual,
                Center = new Point3D(
                    _oStation.GetInfoCurrentTier().GetInfoTLX() + _oStation.GetInfoCenterX(),
                    _oStation.GetInfoCurrentTier().GetInfoTLY() + _oStation.GetInfoCenterY(),
                    _oStation.GetInfoCurrentTier().GetInfoZ() + STATION_HEIGHT / 2.0
                    ),
                Length = _oStation.GetInfoLength(),
                Width = _oStation.GetInfoWidth(),
                Height = STATION_HEIGHT
            };
            Children.Add(visual);
        }
    }

    public class SimulationVisualWaypoint3D : SimulationVisualImmovable3D
    {
        public const double WAYPOINT_LIFT = 0.05;
        private readonly IWaypointInfo _waypoint;

        public SimulationVisualWaypoint3D(IWaypointInfo waypoint, DetailLevel detailLevel)
            : base(waypoint)
        {
            _waypoint = waypoint;
            var visual = new SphereVisual3D
            {
                Fill = VisualizationConstants.BrushWaypointVisual,
                Center = new Point3D(waypoint.GetInfoCenterX(), waypoint.GetInfoCenterY(), waypoint.GetInfoCurrentTier().GetInfoZ() + WAYPOINT_LIFT),
                Radius = waypoint.GetInfoLength() / 2.0
            };
            Children.Add(visual);
            // Add connections to other ones (if detailed drawing mode)
            //foreach (var otherWP in waypoint.GetInfoConnectedWaypoints())
            //{
            //    // TODO draw connection to other wp
            //}
        }
    }

    #endregion

    #region Moveable objects

    public class SimulationVisualBot3D : SimulationVisualMovable3D
    {
        public readonly IBotInfo Bot;

        public SimulationVisualBot3D(IBotInfo bot, DetailLevel detailLevel)
            : base(bot)
        {
            Bot = bot;
            var visual = new BoxVisual3D
            {
                Fill = VisualizationConstants.BrushBotVisual,
                Center = new Point3D(0, 0, 0),
                Length = Bot.GetInfoRadius() * 2,
                Width = Bot.GetInfoRadius() * 2,
                Height = _height,
            };
            Children.Add(visual);
        }

        public override double GetZ() { return _height / 2.0; }

        public override void UpdateMetaInfo()
        {
            foreach (var child in this.Children.OfType<BoxVisual3D>().ToArray())
                if (child.Fill != VisualizationConstants.StateBrushes[Bot.GetInfoState()])
                    child.Fill = VisualizationConstants.StateBrushes[Bot.GetInfoState()];
        }
    }

    public class SimulationVisualPod3D : SimulationVisualMovable3D
    {
        internal readonly IPodInfo Pod;

        private Func<bool> _heatModeEnabled;

        public SimulationVisualPod3D(IPodInfo pod, DetailLevel detailLevel, Func<bool> heatModeEnabled)
            : base(pod)
        {
            Pod = pod;
            _heatModeEnabled = heatModeEnabled;

            Brush coloredBrush = VisualizationConstants.BrushPodVisual;
            // Add main part
            var visual = new BoxVisual3D
            {
                Fill = coloredBrush,
                Center = new Point3D(0, 0, _height / 8.0),
                Length = Pod.GetInfoRadius() * 2,
                Width = Pod.GetInfoRadius() * 2,
                Height = _height / 4.0 * 3.0
            };
            Children.Add(visual);
            // Add feet (if detailed drawing mode)
            if (detailLevel == DetailLevel.Aesthetics)
            {
                double feetLength = Pod.GetInfoRadius() / 10;
                double feetHeight = _height / 4.0;
                visual = new BoxVisual3D
                {
                    Fill = coloredBrush,
                    Center = new Point3D(-Pod.GetInfoRadius() + feetLength / 2.0, -Pod.GetInfoRadius() + feetLength / 2.0, -_height / 2.0 + feetHeight / 2.0),
                    Length = feetLength,
                    Width = feetLength,
                    Height = feetHeight
                };
                Children.Add(visual);
                visual = new BoxVisual3D
                {
                    Fill = coloredBrush,
                    Center = new Point3D(Pod.GetInfoRadius() - feetLength / 2.0, -Pod.GetInfoRadius() + feetLength / 2.0, -_height / 2.0 + feetHeight / 2.0),
                    Length = feetLength,
                    Width = feetLength,
                    Height = feetHeight
                };
                Children.Add(visual);
                visual = new BoxVisual3D
                {
                    Fill = coloredBrush,
                    Center = new Point3D(-Pod.GetInfoRadius() + feetLength / 2.0, Pod.GetInfoRadius() - feetLength / 2.0, -_height / 2.0 + feetHeight / 2.0),
                    Length = feetLength,
                    Width = feetLength,
                    Height = feetHeight
                };
                Children.Add(visual);
                visual = new BoxVisual3D
                {
                    Fill = coloredBrush,
                    Center = new Point3D(Pod.GetInfoRadius() - feetLength / 2.0, Pod.GetInfoRadius() - feetLength / 2.0, -_height / 2.0 + feetHeight / 2.0),
                    Length = feetLength,
                    Width = feetLength,
                    Height = feetHeight
                };
                Children.Add(visual);
            }
        }

        public override double GetZ() { return _height / 2.0; }

        public override void UpdateMetaInfo()
        {
            if (_heatModeEnabled())
            {
                Brush heatBrush = HeatVisualizer.GenerateHeatBrush(Pod.GetInfoHeatValue());
                foreach (var child in this.Children.OfType<BoxVisual3D>().ToArray())
                    if (child.Fill != heatBrush)
                        child.Fill = heatBrush;
            }
            else
            {
                foreach (var child in this.Children.OfType<BoxVisual3D>().ToArray())
                    if (child.Fill != VisualizationConstants.BrushPodVisual)
                        child.Fill = VisualizationConstants.BrushPodVisual;
            }
        }
    }

    #endregion
}
