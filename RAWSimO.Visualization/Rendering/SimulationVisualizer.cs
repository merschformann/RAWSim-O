using RAWSimO.Core;
using RAWSimO.Core.Control;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RAWSimO.Visualization.Rendering
{
    public class SimulationVisualizer
    {
        private bool _paused = false;
        private bool _exitRequested = false;
        private SimulationAnimation2D _animationControl2D;
        private SimulationAnimation3D _animationControl3D;
        private SimulationInfoManager _infoControl;
        private Func<bool> _getDrawMode3D;
        private Action<double> _setUpdateRate;
        private double _updateRate;
        private Action<double> _updateTime;
        private Action<string> _logger;
        private Action _finishCallback;
        private Instance _simulationWorld;
        private ManualResetEvent[] _pausedMutex = new ManualResetEvent[] { new ManualResetEvent(false) };
        private DateTime _lastTime = DateTime.MinValue;

        public SimulationVisualizer(
            Instance instance,
            SimulationAnimation2D animationControl2D,
            SimulationAnimation3D animationControl3D,
            SimulationInfoManager infoControl,
            Func<bool> getDrawMode3D,
            Action<double> setUpdateRate,
            double updateRate,
            Action<double> updateTime,
            Action<string> logger,
            Action finishCallback)
        {
            _animationControl2D = animationControl2D;
            _animationControl3D = animationControl3D;
            _infoControl = infoControl;
            _getDrawMode3D = getDrawMode3D;
            _simulationWorld = instance;
            _setUpdateRate = setUpdateRate;
            _updateRate = updateRate;
            _updateTime = updateTime;
            _logger = logger;
            _finishCallback = finishCallback;
        }

        public void PauseSimulation() { _pausedMutex[0].Reset(); _paused = true; }

        public void ResumeSimulation() { _paused = false; _lastTime = DateTime.Now; _pausedMutex[0].Set(); }

        public void StopSimulation()
        {
            if (_paused)
            {
                _paused = false;
                _lastTime = DateTime.Now;
                _pausedMutex[0].Set();
            }
            _exitRequested = true;
            _animationControl2D.StopAnimation();
            _animationControl3D.StopAnimation();
        }

        #region ISimulationRenderer Members

        public void VisualizeSimulation(double simulationTime)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(Simulate), simulationTime);
            ThreadPool.QueueUserWorkItem(new WaitCallback(VisualizeOnly), simulationTime);
        }

        public void SetUpdateRate(double rate) { _updateRate = rate; }

        private void Simulate(object context)
        {
            double simulationTime = (double)context;
            double simulationEndTime = _simulationWorld.Controller.CurrentTime + simulationTime;
            double simulationHorizonEnd = _simulationWorld.SettingConfig.SimulationWarmupTime + _simulationWorld.SettingConfig.SimulationDuration;
            _lastTime = DateTime.Now;
            double minDelayInSeconds = 10.0 / 1000.0;

            // Start prequels
            _simulationWorld.StartExecutionTiming();

            // Prepare stat dir
            if (!Directory.Exists(_simulationWorld.SettingConfig.StatisticsDirectory))
            {
                string statisticsFolder =
                    _simulationWorld.Name + "-" +
                    _simulationWorld.SettingConfig.Name + "-" +
                    _simulationWorld.ControllerConfig.Name + "-" +
                    _simulationWorld.SettingConfig.Seed.ToString();
                _simulationWorld.SettingConfig.StatisticsDirectory = Path.Combine(Environment.CurrentDirectory, statisticsFolder);
            }

            // Define stat writing quick function
            Action checkAndHandleStats = () =>
            {
                // Reset stats on reaching warmup period end
                if (!_simulationWorld.StatWarmupResetDone && _simulationWorld.Controller.CurrentTime >= _simulationWorld.SettingConfig.SimulationWarmupTime)
                    _simulationWorld.StatReset();

                // Write statistics on reaching simulation horizon end
                if (!_simulationWorld.StatResultsWritten && _simulationWorld.Controller.CurrentTime >= simulationHorizonEnd)
                    _simulationWorld.WriteStatistics();
            };

            // Loop until done or exit requested
            while (!_exitRequested && !(_simulationWorld.Controller.CurrentTime >= simulationEndTime))
            {
                // Calculate time since last frame
                DateTime newTime = DateTime.Now;
                double updateAmountInSeconds = (newTime - _lastTime).TotalSeconds;
                _lastTime = newTime;

                // If steps get too long to simulate lower the update rate
                if (updateAmountInSeconds > 2)
                {
                    _updateRate = Math.Ceiling(_updateRate * 0.75);
                    _setUpdateRate(_updateRate);
                }

                // Handle statistics
                checkAndHandleStats();

                // Check whether the simulation shall pause
                if (!_paused)
                {
                    double timeDelta = _updateRate * updateAmountInSeconds;
                    // If simulationTime is specified, don't let it go past that time 
                    if (simulationTime > 0.0)
                        timeDelta = Math.Min(simulationEndTime - _simulationWorld.Controller.CurrentTime, timeDelta);

                    // Sleep if time available
                    if (timeDelta < minDelayInSeconds)
                    {
                        // Simulate
                        _simulationWorld.Controller.Update(timeDelta);
                        Thread.Sleep((int)((minDelayInSeconds - timeDelta) * 1000));
                    }
                    else
                    {
                        // Simulate
                        _simulationWorld.Controller.Update(timeDelta);
                    }
                }
                else
                {
                    // Wait for the unpause event
                    WaitHandle.WaitAll(_pausedMutex);
                }
            }

            // Handle statistics
            checkAndHandleStats();

            // Finish prequels
            _simulationWorld.StopExecutionTiming();

            // Signal finished simulation
            _finishCallback();
        }

        private void VisualizeOnly(object context)
        {
            double minDelay = 20;
            DateTime lastTime = DateTime.Now;
            while (!_exitRequested)
            {
                // Calculate the time-difference to the last update and sleep if possible
                DateTime newTime = DateTime.Now;
                double delayInMillis = (newTime - lastTime).TotalMilliseconds;
                if (delayInMillis < minDelay)
                {
                    Thread.Sleep((int)(minDelay - delayInMillis));
                }
                lastTime = newTime;

                // If the simulation is paused wait for the mutex managing the resume event
                if (_paused)
                    WaitHandle.WaitAll(_pausedMutex);

                // Update the current simulation time
                _updateTime(_simulationWorld.Controller.CurrentTime);

                // Redraw
                if (_getDrawMode3D())
                    _animationControl3D.Update(false);
                else
                    _animationControl2D.Update(false);
                // Refresh info
                _infoControl.Update();
            }
        }

        #endregion
    }
}
