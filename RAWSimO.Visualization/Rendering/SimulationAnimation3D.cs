using RAWSimO.Core.Info;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RAWSimO.Visualization.Rendering
{
    public class SimulationAnimation3D : SimulationAnimation
    {
        public SimulationAnimation3D(
            IInstanceInfo instance,
            Dispatcher uiDispatcher,
            SimulationAnimationConfig config,
            Func<bool> heatModeEnabled,
            Func<BotColorMode> botColorModeGetter,
            HelixViewport3D contentControl,
            SimulationInfoManager infoControl)
            : base(instance, uiDispatcher, config, botColorModeGetter, heatModeEnabled) { _contentControl = contentControl; _infoControl = infoControl; }

        protected HelixViewport3D _contentControl;
        protected SimulationInfoManager _infoControl;

        private Dictionary<IBotInfo, SimulationVisualBot3D> _botVisuals;
        private Dictionary<IPodInfo, SimulationVisualPod3D> _podVisuals;
        private Dictionary<IInputStationInfo, SimulationVisualInputStation3D> _iStationVisuals;
        private Dictionary<IOutputStationInfo, SimulationVisualOutputStation3D> _oStationVisuals;
        private Dictionary<ITierInfo, SimulationVisualTier3D> _tiers;
        private Dictionary<IWaypointInfo, SimulationVisualWaypoint3D> _waypoints;
        private Dictionary<IWaypointInfo, SimulationVisualElevatorEntrance3D> _elevatorEntrances;

        public override void Init()
        {
            // Remove old visuals (if any)
            foreach (var visual in _contentControl.Children.Where(v => v is SimulationVisualMovable3D || v is SimulationVisualImmovable3D).ToArray())
                _contentControl.Children.Remove(visual);
            // Init visuals
            _botVisuals = _instance.GetInfoBots().ToDictionary(k => k, v => { return new SimulationVisualBot3D(v, _config.DetailLevel); });
            _podVisuals = _instance.GetInfoPods().ToDictionary(k => k, v => { return new SimulationVisualPod3D(v, _config.DetailLevel, _heatModeEnabled); });
            _iStationVisuals = _instance.GetInfoTiers().SelectMany(t => t.GetInfoInputStations()).ToDictionary(k => k, v => { return new SimulationVisualInputStation3D(v, _config.DetailLevel); });
            _oStationVisuals = _instance.GetInfoTiers().SelectMany(t => t.GetInfoOutputStations()).ToDictionary(k => k, v => { return new SimulationVisualOutputStation3D(v, _config.DetailLevel); });
            _tiers = _instance.GetInfoTiers().ToDictionary(k => k, v => { return new SimulationVisualTier3D(v, _config.DetailLevel); });
            _elevatorEntrances = _instance.GetInfoElevators().SelectMany(e => e.GetInfoWaypoints()).ToDictionary(k => k, v => { return new SimulationVisualElevatorEntrance3D(v, _config.DetailLevel); });
            if (_config.DetailLevel >= DetailLevel.Debug)
                _waypoints = _instance.GetInfoTiers().SelectMany(t => t.GetInfoWaypoints()).ToDictionary(k => k, v => { return new SimulationVisualWaypoint3D(v, _config.DetailLevel); });
            // Add new bot visuals to the view
            foreach (var bot in _instance.GetInfoBots())
                _contentControl.Children.Add(_botVisuals[bot]);
            // Add new pod visuals to the view
            foreach (var pod in _instance.GetInfoPods())
                _contentControl.Children.Add(_podVisuals[pod]);
            // Add new iStation visuals to the view
            foreach (var iStation in _instance.GetInfoTiers().SelectMany(t => t.GetInfoInputStations()))
                _contentControl.Children.Add(_iStationVisuals[iStation]);
            // Add new oStation visuals to the view
            foreach (var oStation in _instance.GetInfoTiers().SelectMany(t => t.GetInfoOutputStations()))
                _contentControl.Children.Add(_oStationVisuals[oStation]);
            // Add new tier visuals to the view
            foreach (var tier in _instance.GetInfoTiers())
                _contentControl.Children.Add(_tiers[tier]);
            // Add new elevator entrances to the view
            foreach (var elevator in _instance.GetInfoElevators())
                foreach (var entrance in elevator.GetInfoWaypoints())
                    _contentControl.Children.Add(_elevatorEntrances[entrance]);
            // Add new waypoint visuals to the view
            if (_config.DetailLevel >= DetailLevel.Debug)
                foreach (var waypoint in _instance.GetInfoTiers().SelectMany(t => t.GetInfoWaypoints()))
                    _contentControl.Children.Add(_waypoints[waypoint]);
            // Update the view
            Update(true);
        }

        public override void Update(bool overrideUpdate)
        {
            // Update on change or override
            if (_instance.GetInfoChanged() || overrideUpdate)
            {
                _dispatcher.Invoke(() =>
                {
                    // Update movable visual objects
                    foreach (var botVisual in _botVisuals.Values)
                        botVisual.UpdateTransformation(overrideUpdate);
                    foreach (var podVisual in _podVisuals.Values)
                        podVisual.UpdateTransformation(overrideUpdate);
                });
            }
        }

        public override void StopAnimation()
        {
            // Nothing to see here
        }

        public override void TakeSnapshot(string snapshotDir, string snapshotFilename = null)
        {
            // Export the image to the given path and filename
            _contentControl.Export(System.IO.Path.Combine(snapshotDir,
                // If a filename is given, use it
                snapshotFilename != null ?
                    // If the filename does not already end with .png, append it
                    snapshotFilename.EndsWith(".png") ?
                        snapshotFilename :
                        snapshotFilename + ".png" :
                // No filename given, use a date timestamp instead
                _instance.GetInfoName() + "-3D-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".png"));
        }
    }
}
