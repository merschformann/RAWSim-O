using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using RAWSimO.Core;
using RAWSimO.Visualization.Avalonia.Rendering;

namespace RAWSimO.Visualization.Avalonia.Simulation
{
    /// <summary>
    /// Executes a simulation in (scaled) real time on a background thread and publishes snapshots of its state for rendering.
    /// This is the UI-independent port of the WPF visualization's simulation loop.
    /// </summary>
    public sealed class SimulationRunner
    {
        /// <summary>
        /// Minimal wall-clock time between two simulation updates.
        /// </summary>
        private static readonly TimeSpan MinUpdateDelay = TimeSpan.FromMilliseconds(10);
        /// <summary>
        /// Minimal wall-clock time between two snapshots (no need to snapshot faster than we can display).
        /// </summary>
        private static readonly TimeSpan MinSnapshotDelay = TimeSpan.FromMilliseconds(1000.0 / 120.0);

        private readonly Instance _instance;
        private readonly SceneSnapshotBuilder _snapshotBuilder;
        private readonly ManualResetEventSlim _resumeEvent = new ManualResetEventSlim(true);
        private Thread _thread;
        private volatile bool _stopRequested;
        private volatile bool _paused;
        private double _speed;
        private SceneSnapshot _snapshot;

        public SimulationRunner(Instance instance, double speed)
        {
            _instance = instance;
            _speed = speed;
            _snapshotBuilder = new SceneSnapshotBuilder(instance, () => instance.Controller.CurrentTime);
            // Initial snapshot (simulation is not running yet, hence, it is safe to do this on the calling thread)
            _snapshot = _snapshotBuilder.Build();
        }

        /// <summary>
        /// The most recent snapshot of the simulation state. Can be read from any thread.
        /// </summary>
        public SceneSnapshot Snapshot => Volatile.Read(ref _snapshot);

        /// <summary>
        /// Simulated seconds per wall-clock second.
        /// </summary>
        public double Speed { get => Volatile.Read(ref _speed); set => Volatile.Write(ref _speed, Math.Max(0, value)); }

        /// <summary>
        /// Whether bot paths shall be included in the snapshots.
        /// </summary>
        public bool CapturePaths { get => _snapshotBuilder.CapturePaths; set => _snapshotBuilder.CapturePaths = value; }

        public bool IsRunning => _thread != null && _thread.IsAlive;
        public bool IsPaused => _paused;

        /// <summary>
        /// Raised (on the simulation thread) when the speed had to be reduced, because the simulation cannot keep up.
        /// </summary>
        public event Action<double> SpeedReduced;
        /// <summary>
        /// Raised (on the simulation thread) when the simulation finished or was stopped.
        /// </summary>
        public event Action Finished;

        /// <summary>
        /// Starts executing the simulation for the given amount of simulation time.
        /// </summary>
        public void Start(double simulationTime)
        {
            if (_thread != null)
                throw new InvalidOperationException("The simulation was already started.");
            _thread = new Thread(() => Simulate(simulationTime)) { IsBackground = true, Name = "RAWSimO simulation" };
            _thread.Start();
        }

        public void Pause() { _paused = true; _resumeEvent.Reset(); }

        public void Resume() { _paused = false; _resumeEvent.Set(); }

        /// <summary>
        /// Requests the simulation to stop and waits for it (up to the given timeout).
        /// </summary>
        public void Stop(TimeSpan? timeout = null)
        {
            _stopRequested = true;
            _resumeEvent.Set();
            if (_thread != null && Thread.CurrentThread != _thread)
                _thread.Join(timeout ?? TimeSpan.FromSeconds(5));
        }

        private void Simulate(double simulationTime)
        {
            double simulationEndTime = _instance.Controller.CurrentTime + simulationTime;
            double simulationHorizonEnd = _instance.SettingConfig.SimulationWarmupTime + _instance.SettingConfig.SimulationDuration;

            // Start prequels
            _instance.StartExecutionTiming();

            // Prepare statistics directory
            if (!Directory.Exists(_instance.SettingConfig.StatisticsDirectory))
            {
                string statisticsFolder =
                    _instance.Name + "-" +
                    _instance.SettingConfig.Name + "-" +
                    _instance.ControllerConfig.Name + "-" +
                    _instance.SettingConfig.Seed.ToString();
                _instance.SettingConfig.StatisticsDirectory = Path.Combine(Environment.CurrentDirectory, statisticsFolder);
            }

            void CheckAndHandleStats()
            {
                // Reset stats on reaching warmup period end
                if (!_instance.StatWarmupResetDone && _instance.Controller.CurrentTime >= _instance.SettingConfig.SimulationWarmupTime)
                    _instance.StatReset();
                // Write statistics on reaching simulation horizon end
                if (!_instance.StatResultsWritten && _instance.Controller.CurrentTime >= simulationHorizonEnd)
                    _instance.WriteStatistics();
            }

            var clock = Stopwatch.StartNew();
            TimeSpan lastUpdate = clock.Elapsed;
            TimeSpan lastSnapshot = TimeSpan.Zero;
            try
            {
                while (!_stopRequested && _instance.Controller.CurrentTime < simulationEndTime)
                {
                    // Wait while paused (and do not count the pause as elapsed time)
                    if (_paused)
                    {
                        PublishSnapshot();
                        _resumeEvent.Wait();
                        lastUpdate = clock.Elapsed;
                        continue;
                    }

                    // Calculate time since last update
                    TimeSpan now = clock.Elapsed;
                    double elapsedSeconds = (now - lastUpdate).TotalSeconds;
                    lastUpdate = now;

                    // If steps take too long to simulate, lower the speed
                    if (elapsedSeconds > 2)
                    {
                        Speed = Math.Ceiling(Speed * 0.75);
                        SpeedReduced?.Invoke(Speed);
                    }

                    // Handle statistics
                    CheckAndHandleStats();

                    // Simulate
                    double timeDelta = Math.Min(simulationEndTime - _instance.Controller.CurrentTime, Speed * elapsedSeconds);
                    _instance.Controller.Update(timeDelta);

                    // Publish state for rendering
                    if (clock.Elapsed - lastSnapshot >= MinSnapshotDelay)
                    {
                        PublishSnapshot();
                        lastSnapshot = clock.Elapsed;
                    }

                    // Sleep if time is left
                    TimeSpan remaining = MinUpdateDelay - (clock.Elapsed - now);
                    if (remaining > TimeSpan.Zero)
                        Thread.Sleep(remaining);
                }

                // Handle statistics
                CheckAndHandleStats();
            }
            finally
            {
                // Finish prequels
                _instance.StopExecutionTiming();
                PublishSnapshot();
                Finished?.Invoke();
            }
        }

        private void PublishSnapshot() => Volatile.Write(ref _snapshot, _snapshotBuilder.Build());
    }
}
