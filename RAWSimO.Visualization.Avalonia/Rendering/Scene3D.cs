using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Aquarium;
using RAWSimO.Core.Info;
using SkiaSharp;

namespace RAWSimO.Visualization.Avalonia.Rendering
{
    /// <summary>
    /// Translates the state of an instance into a 3D scene (all tiers). Dynamic content is updated from <see cref="SceneSnapshot"/>s.
    /// </summary>
    public sealed class Scene3D
    {
        /// <summary>
        /// Height of the tier floors.
        /// </summary>
        private const float FloorHeight = 0.05f;
        private const float StationHeight = 0.1f;
        /// <summary>
        /// Height of the bots / pods relative to their radius (same as in the WPF visualization).
        /// </summary>
        private const float BotHeightFactor = 0.9f, PodHeightFactor = 4f;

        private readonly InstanceBatch _bots, _pods, _selection;
        private readonly BotColors _botColors;
        private Instance[] _botInstances = Array.Empty<Instance>(), _podInstances = Array.Empty<Instance>();
        // The objects corresponding to the instances (for picking)
        private object[] _botObjects = Array.Empty<object>(), _podObjects = Array.Empty<object>();
        private readonly InstanceBatch _stations;
        private readonly List<object> _stationObjects = new();
        private readonly HashSet<(int, int)> _botCells = new();

        public Scene3D(IInstanceInfo instance, DetailLevel detailLevel)
        {
            Scene = new Scene { Background = new Color4(211, 211, 211) };
            List<ITierInfo> tiers = instance.GetInfoTiers().ToList();
            _botColors = new BotColors(instance.GetInfoBots().Count());

            // Tier floors (semi-transparent for tiers above the first one, so that the tiers below remain visible)
            var floors = new List<Instance>();
            var upperFloors = new List<Instance>();
            foreach (ITierInfo tier in tiers)
            {
                var floor = new Instance(
                    new Vector3((float)tier.GetInfoLength() / 2, (float)tier.GetInfoWidth() / 2, (float)tier.GetInfoZ() - FloorHeight / 2),
                    new Vector3((float)tier.GetInfoLength(), (float)tier.GetInfoWidth(), FloorHeight), new Color4(255, 255, 255));
                if (tier == tiers[0]) floors.Add(floor); else upperFloors.Add(floor with { Color = new Color4(255, 255, 255, 90) });
            }
            Scene.Add(new InstanceBatch(Mesh.Box, "Tiers") { Pickable = false, ShowEdges = false }).Set(floors.ToArray());
            if (upperFloors.Count > 0)
                Scene.Add(new InstanceBatch(Mesh.Box, "Upper tiers") { Pickable = false, ShowEdges = false, Transparent = true }).Set(upperFloors.ToArray());

            // Waypoint graph
            if (detailLevel >= DetailLevel.Debug)
            {
                var lines = new List<Line>();
                var color = new Color4(120, 120, 120);
                foreach (ITierInfo tier in tiers)
                {
                    float z = (float)tier.GetInfoZ() + 0.005f;
                    foreach (IWaypointInfo wp in tier.GetInfoWaypoints())
                        foreach (IWaypointInfo other in wp.GetInfoConnectedWaypoints())
                            if (other.GetInfoCurrentTier() == tier)
                                lines.Add(new Line(new Vector3((float)wp.GetInfoCenterX(), (float)wp.GetInfoCenterY(), z),
                                    new Vector3((float)other.GetInfoCenterX(), (float)other.GetInfoCenterY(), z), color));
                }
                Scene.Add(new LineBatch("Waypoint graph")).Set(lines.ToArray());
            }

            // Stations and elevator entrances
            var stations = new List<Instance>();
            void AddStation(IGeneralObjectInfo station, SKColor color)
            {
                var immovable = (IImmovableObjectInfo)station;
                stations.Add(new Instance(
                    new Vector3((float)station.GetInfoCenterX(), (float)station.GetInfoCenterY(), (float)station.GetInfoCurrentTier().GetInfoZ() + StationHeight / 2),
                    new Vector3((float)immovable.GetInfoLength(), (float)immovable.GetInfoWidth(), StationHeight), ToColor(color)));
                _stationObjects.Add(station);
            }
            foreach (ITierInfo tier in tiers)
            {
                foreach (var station in tier.GetInfoInputStations()) AddStation(station, Palette.InputStation);
                foreach (var station in tier.GetInfoOutputStations()) AddStation(station, Palette.OutputStation);
            }
            foreach (IWaypointInfo wp in instance.GetInfoElevators().SelectMany(e => e.GetInfoWaypoints()))
            {
                float d = (float)wp.GetInfoLength() * 3;
                stations.Add(new Instance(new Vector3((float)wp.GetInfoCenterX(), (float)wp.GetInfoCenterY(), (float)wp.GetInfoCurrentTier().GetInfoZ() + StationHeight / 2),
                    new Vector3(d, d, StationHeight), ToColor(Palette.ElevatorEntrance)));
                _stationObjects.Add(wp);
            }
            _stations = Scene.Add(new InstanceBatch(Mesh.Box, "Stations"));
            _stations.Set(stations.ToArray());

            _pods = Scene.Add(new InstanceBatch(Mesh.Box, "Pods"));
            _bots = Scene.Add(new InstanceBatch(Mesh.Cylinder, "Bots"));
            _selection = Scene.Add(new InstanceBatch(Mesh.Box, "Selection") { Pickable = false, ShowEdges = false, Transparent = true });
        }

        /// <summary>
        /// The 3D scene.
        /// </summary>
        public Scene Scene { get; }

        /// <summary>
        /// The snapshot the scene currently shows.
        /// </summary>
        public long Version { get; private set; } = -1;

        private static Color4 ToColor(SKColor color) => new(color.Red, color.Green, color.Blue, color.Alpha);

        /// <summary>
        /// Updates the dynamic content.
        /// </summary>
        public void Update(SceneSnapshot snapshot, SceneOptions options, object selected)
        {
            Version = snapshot.Version;
            // Bots
            if (_botInstances.Length != snapshot.Bots.Length)
            {
                _botInstances = new Instance[snapshot.Bots.Length];
                _botObjects = new object[snapshot.Bots.Length];
            }
            _botCells.Clear();
            for (int i = 0; i < snapshot.Bots.Length; i++)
            {
                ref readonly BotSnapshot bot = ref snapshot.Bots[i];
                float height = bot.Radius * BotHeightFactor;
                _botInstances[i] = Instance.WithYaw(new Vector3(bot.X, bot.Y, (float)bot.Tier.GetInfoZ() + height / 2), new Vector3(2 * bot.Radius, 2 * bot.Radius, height),
                    bot.Orientation, ToColor(_botColors.Get(bot, options.BotColorMode)));
                _botObjects[i] = bot.Bot;
                _botCells.Add(Cell(bot.X, bot.Y));
            }
            _bots.Set(_botInstances);

            // Pods (carried pods are lifted onto the bot)
            if (_podInstances.Length != snapshot.Pods.Length)
            {
                _podInstances = new Instance[snapshot.Pods.Length];
                _podObjects = new object[snapshot.Pods.Length];
            }
            float botHeight = snapshot.Bots.Length > 0 ? snapshot.Bots[0].Radius * BotHeightFactor : 0;
            for (int i = 0; i < snapshot.Pods.Length; i++)
            {
                ref readonly PodSnapshot pod = ref snapshot.Pods[i];
                float height = pod.Radius * PodHeightFactor;
                float lift = _botCells.Contains(Cell(pod.X, pod.Y)) ? botHeight : 0;
                SKColor color = options.HeatMode ? Palette.GetHeatColor(pod.Heat) : Palette.Pod;
                _podInstances[i] = Instance.WithYaw(new Vector3(pod.X, pod.Y, (float)pod.Tier.GetInfoZ() + lift + height / 2),
                    new Vector3(2 * pod.Radius * 0.95f, 2 * pod.Radius * 0.95f, height), pod.Orientation, ToColor(color));
                _podObjects[i] = pod.Pod;
            }
            _pods.Set(_podInstances);

            // Selection: a transparent box around the selected object
            Instance? highlight = null;
            if (selected != null)
            {
                Instance? target = Find(_bots, _botObjects, selected) ?? Find(_pods, _podObjects, selected) ?? Find(_stations, _stationObjects, selected);
                if (target is Instance t)
                    highlight = new Instance(t.Position, t.Size * 1.15f + new Vector3(0.1f), t.Rotation, ToColor(Palette.Selection).WithAlpha(110));
            }
            _selection.Set(highlight is Instance h ? new[] { h } : Array.Empty<Instance>());
        }

        private static (int, int) Cell(float x, float y) => ((int)MathF.Round(x * 100), (int)MathF.Round(y * 100));

        private static Instance? Find(InstanceBatch batch, IReadOnlyList<object> objects, object selected)
        {
            for (int i = 0; i < objects.Count && i < batch.Count; i++)
                if (ReferenceEquals(objects[i], selected))
                    return batch.Get(i);
            return null;
        }

        /// <summary>
        /// Returns the simulation object of a picked instance (or null).
        /// </summary>
        public object GetObject(PickResult? hit)
        {
            if (hit is not PickResult h) return null;
            IReadOnlyList<object> objects = h.Batch == _bots ? _botObjects : h.Batch == _pods ? _podObjects : h.Batch == _stations ? _stationObjects : null;
            return objects != null && h.Index < objects.Count ? objects[h.Index] : null;
        }
    }
}
