using System;
using System.Collections.Generic;
using System.Linq;
using RAWSimO.Core.Info;
using SkiaSharp;

namespace RAWSimO.Visualization.Avalonia.Rendering
{
    /// <summary>
    /// The state of a bot at the time a snapshot was taken.
    /// </summary>
    public struct BotSnapshot
    {
        public IBotInfo Bot;
        public ITierInfo Tier;
        public float X, Y, Orientation, Radius;
        public string State;
        /// <summary>
        /// Index of the bot (used for rainbow coloring).
        /// </summary>
        public int Index;
        public bool HasGoal;
        public float GoalX, GoalY;
        public bool HasDestination;
        public float DestinationX, DestinationY;
        /// <summary>
        /// The current path of the bot (only filled, if path drawing is requested and the bot is moving).
        /// </summary>
        public SKPoint[] Path;
    }

    /// <summary>
    /// The state of a pod at the time a snapshot was taken.
    /// </summary>
    public struct PodSnapshot
    {
        public IPodInfo Pod;
        public ITierInfo Tier;
        public float X, Y, Orientation, Radius;
        public float Heat;
    }

    /// <summary>
    /// The state of a guard (semaphore) at the time a snapshot was taken.
    /// </summary>
    public struct GuardSnapshot
    {
        public ITierInfo Tier;
        public float X, Y, Radius;
        public bool IsBarrier, IsAccessible;
    }

    /// <summary>
    /// An immutable copy of all dynamic simulation state needed for rendering one frame.
    /// Snapshots are taken on the simulation thread (between two simulation steps), so they are consistent,
    /// and can safely be rendered on any other thread afterwards.
    /// </summary>
    public sealed class SceneSnapshot
    {
        public double Time { get; init; }
        public BotSnapshot[] Bots { get; init; }
        public PodSnapshot[] Pods { get; init; }
        public GuardSnapshot[] Guards { get; init; }
        /// <summary>
        /// Increases with every snapshot, allows to cheaply detect new snapshots.
        /// </summary>
        public long Version { get; init; }
    }

    /// <summary>
    /// Creates <see cref="SceneSnapshot"/>s of an instance. Must only be used by one thread at a time
    /// (the thread manipulating the instance).
    /// </summary>
    public sealed class SceneSnapshotBuilder
    {
        private readonly IInstanceInfo _instance;
        private readonly IBotInfo[] _bots;
        private readonly IPodInfo[] _pods;
        private readonly IGuardInfo[] _guards;
        private readonly Func<double> _timeGetter;
        // Path conversions are cached per bot, since paths change rarely compared to the frame rate
        private readonly Dictionary<IBotInfo, (List<IWaypointInfo> source, SKPoint[] points)> _pathCache = new();
        private long _version;

        public SceneSnapshotBuilder(IInstanceInfo instance, Func<double> timeGetter)
        {
            _instance = instance;
            _timeGetter = timeGetter;
            _bots = instance.GetInfoBots().ToArray();
            _pods = instance.GetInfoPods().ToArray();
            _guards = instance.GetInfoTiers().SelectMany(t => t.GetInfoGuards()).ToArray();
        }

        /// <summary>
        /// Indicates whether bot paths shall be captured (costs a bit of performance, hence, optional).
        /// </summary>
        public volatile bool CapturePaths;

        /// <summary>
        /// Takes a snapshot of the current state of the instance.
        /// </summary>
        public SceneSnapshot Build()
        {
            bool capturePaths = CapturePaths;
            var bots = new BotSnapshot[_bots.Length];
            for (int i = 0; i < _bots.Length; i++)
            {
                IBotInfo b = _bots[i];
                double goalX = b.GetInfoGoalX(), goalY = b.GetInfoGoalY();
                IWaypointInfo destination = b.GetInfoDestinationWaypoint();
                string state = b.GetInfoState();
                bots[i] = new BotSnapshot
                {
                    Bot = b,
                    Tier = b.GetInfoCurrentTier(),
                    X = (float)b.GetInfoCenterX(),
                    Y = (float)b.GetInfoCenterY(),
                    Orientation = (float)b.GetInfoOrientation(),
                    Radius = (float)b.GetInfoRadius(),
                    State = state,
                    Index = i,
                    HasGoal = !double.IsNaN(goalX),
                    GoalX = (float)goalX,
                    GoalY = (float)goalY,
                    HasDestination = destination != null,
                    DestinationX = destination != null ? (float)destination.GetInfoCenterX() : 0,
                    DestinationY = destination != null ? (float)destination.GetInfoCenterY() : 0,
                    Path = capturePaths && state == "Move" ? GetPath(b) : null,
                };
            }
            var pods = new PodSnapshot[_pods.Length];
            for (int i = 0; i < _pods.Length; i++)
            {
                IPodInfo p = _pods[i];
                pods[i] = new PodSnapshot
                {
                    Pod = p,
                    Tier = p.GetInfoCurrentTier(),
                    X = (float)p.GetInfoCenterX(),
                    Y = (float)p.GetInfoCenterY(),
                    Orientation = (float)p.GetInfoOrientation(),
                    Radius = (float)p.GetInfoRadius(),
                    Heat = (float)p.GetInfoHeatValue(),
                };
            }
            var guards = new GuardSnapshot[_guards.Length];
            for (int i = 0; i < _guards.Length; i++)
            {
                IGuardInfo g = _guards[i];
                guards[i] = new GuardSnapshot
                {
                    Tier = g.GetInfoCurrentTier(),
                    X = (float)g.GetInfoCenterX(),
                    Y = (float)g.GetInfoCenterY(),
                    Radius = (float)(g.GetInfoLength() / 2.0),
                    IsBarrier = g.GetInfoIsBarrier(),
                    IsAccessible = g.GetInfoIsAccessible(),
                };
            }
            return new SceneSnapshot { Time = _timeGetter(), Bots = bots, Pods = pods, Guards = guards, Version = ++_version };
        }

        private SKPoint[] GetPath(IBotInfo bot)
        {
            List<IWaypointInfo> path = bot.GetInfoPath();
            if (path == null || path.Count < 2)
                return null;
            // Paths are replaced (not modified) by the bots, hence, reference equality is sufficient here
            if (_pathCache.TryGetValue(bot, out var cached) && ReferenceEquals(cached.source, path))
                return cached.points;
            SKPoint[] points = path.Select(wp => new SKPoint((float)wp.GetInfoCenterX(), (float)wp.GetInfoCenterY())).ToArray();
            _pathCache[bot] = (path, points);
            return points;
        }
    }
}
