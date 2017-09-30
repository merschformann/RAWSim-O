using RAWSimO.Core.Bots;
using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.Items;
using RAWSimO.Core.Management;
using RAWSimO.Core.Metrics;
using RAWSimO.Core.Waypoints;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RAWSimO.Core.Control.RepositioningManager;

namespace RAWSimO.Core.Control
{
    /// <summary>
    /// The base implementation for managing the tasks of the bots.
    /// </summary>
    public abstract partial class BotManager : IUpdateable, IOptimize, IStatTracker
    {
        /// <summary>
        /// The waypoint graph.
        /// </summary>
        protected WaypointGraph waypointGraph = null;
        /// <summary>
        /// The instance this manager belongs to.
        /// </summary>
        protected Instance Instance { get; set; }
        /// <summary>
        /// All task queues of the robots.
        /// </summary>
        private Dictionary<Bot, BotTask> _taskQueues = new Dictionary<Bot, BotTask>();
        /// <summary>
        /// The last task that was assigned to the bot.
        /// </summary>
        private Dictionary<Bot, BotTask> _lastTaskEnqueued = new Dictionary<Bot, BotTask>();
        /// <summary>
        /// Creates a new task allocation manager instance.
        /// </summary>
        /// <param name="instance">The instance the manager belongs to.</param>
        public BotManager(Instance instance)
        {
            // Init if not already done
            Instance = instance;
            waypointGraph = instance.WaypointGraph;
            foreach (var bot in instance.Bots)
                _taskQueues[bot] = new DummyTask(instance, bot);
            foreach (var bot in instance.Bots)
                _lastTaskEnqueued[bot] = null;
            // Subscribe to order assignment updates (in order to add more work on-the-fly)
            instance.OrderAllocated += OrderAllocated;
            instance.BundleAllocated += BundleAllocated;
            instance.PodPickup += PodPickup;
        }

        /// <summary>
        /// Enqueues an extraction task.
        /// </summary>
        /// <param name="bot">The bot that shall execute the task.</param>
        /// <param name="station">The station at which the task will be executed.</param>
        /// <param name="pod">The pod used for the task.</param>
        /// <param name="requests">The requests to handle when executing the task.</param>
        protected void EnqueueExtract(Bot bot, OutputStation station, Pod pod, List<ExtractRequest> requests)
        {
            ExtractTask task = new ExtractTask(Instance, bot, pod, station, requests);
            task.Prepare();
            if (_taskQueues[bot] != null)
                _taskQueues[bot].Cancel();
            _taskQueues[bot] = task;
            _lastTaskEnqueued[bot] = task;
        }
        /// <summary>
        /// Enqueues an insertion task.
        /// </summary>
        /// <param name="bot">The bot that shall execute the task.</param>
        /// <param name="station">The station at which the task will be executed.</param>
        /// <param name="pod">The pod to use for this task.</param>
        /// <param name="requests">The requests that will be handled by the task.</param>
        protected void EnqueueInsert(Bot bot, InputStation station, Pod pod, List<InsertRequest> requests)
        {
            InsertTask task = new InsertTask(Instance, bot, pod, station, requests);
            task.Prepare();
            if (_taskQueues[bot] != null)
                _taskQueues[bot].Cancel();
            _taskQueues[bot] = task;
            _lastTaskEnqueued[bot] = task;
        }
        /// <summary>
        /// Enqueues a pod parking operation.
        /// </summary>
        /// <param name="bot">The bot that shall execute the task.</param>
        /// <param name="pod">The pod to park.</param>
        /// <param name="storageLocation">The storage location to park the pod at.</param>
        protected void EnqueueParkPod(Bot bot, Pod pod, Waypoint storageLocation)
        {
            ParkPodTask task = new ParkPodTask(Instance, bot, pod, storageLocation);
            task.Prepare();
            if (_taskQueues[bot] != null)
                _taskQueues[bot].Cancel();
            _taskQueues[bot] = task;
            _lastTaskEnqueued[bot] = task;
        }
        /// <summary>
        /// Enqueus a pod repositioning operation.
        /// </summary>
        /// <param name="bot">The bot that shall execute the task.</param>
        /// <param name="pod">The pod to reposition.</param>
        /// <param name="storageLocation">The storage location the pod shall be brought to.</param>
        protected void EnqueueRepositionPod(Bot bot, Pod pod, Waypoint storageLocation)
        {
            RepositionPodTask task = new RepositionPodTask(Instance, bot, pod, storageLocation);
            task.Prepare();
            if (_taskQueues[bot] != null)
                _taskQueues[bot].Cancel();
            _taskQueues[bot] = task;
            _lastTaskEnqueued[bot] = task;
        }
        /// <summary>
        /// Enqueues a rest task.
        /// </summary>
        /// <param name="bot">The bot that shall idle.</param>
        /// <param name="restLocation">The location to use for idling.</param>
        protected void EnqueueRest(Bot bot, Waypoint restLocation)
        {
            RestTask task = new RestTask(Instance, bot, restLocation);
            task.Prepare();
            if (_taskQueues[bot] != null)
                _taskQueues[bot].Cancel();
            _taskQueues[bot] = task;
            _lastTaskEnqueued[bot] = task;
        }

        /// <summary>
        /// Returns the last task that was enqueued for the given bot or <code>null</code>, if none is available.
        /// </summary>
        /// <param name="bot">The bot to check the last enqueued task for.</param>
        /// <returns>The last task enqueued by an inheriting controller.</returns>
        protected BotTask GetLastEnqueuedTask(Bot bot) { return _lastTaskEnqueued[bot]; }

        /// <summary>
        /// Main decision routine that determines the next task the bot will do.
        /// </summary>
        /// <param name="bot">The bot to assign a task to.</param>
        protected abstract void GetNextTask(Bot bot);

        #region Miscellaneous task selectors

        /// <summary>
        /// Determines a value that can be used to order possible rest locations for a pod.
        /// </summary>
        /// <param name="orderType">The order metric to use.</param>
        /// <param name="bot">The bot looking for a job.</param>
        /// <param name="restLocation">The rest location to look at.</param>
        /// <returns>A value reflecting the given metric. The lowest value indicates the best option for the given metric.</returns>
        public double RestLocationToBotAllocationMetric(PrefRestLocationForBot orderType, Bot bot, Waypoint restLocation)
        {
            double value;
            switch (orderType)
            {
                case PrefRestLocationForBot.Random:
                    value = Instance.Randomizer.NextDouble();
                    break;
                case PrefRestLocationForBot.RandomSameTier:
                    value = restLocation.Tier == bot.Tier ?
                        -Instance.Randomizer.NextDouble() :
                        Instance.Randomizer.NextDouble();
                    break;
                case PrefRestLocationForBot.Middle:
                    value = bot.GetDistance(bot.Tier.Length / 2.0, bot.Tier.Width / 2.0);
                    break;
                case PrefRestLocationForBot.MiddleSameTier:
                    value = restLocation.Tier == bot.Tier ?
                        restLocation.GetDistance(restLocation.Tier.Length / 2.0, restLocation.Tier.Width / 2.0) :
                        restLocation.GetDistance(restLocation.Tier.Length / 2.0, restLocation.Tier.Width / 2.0) + Instance.WrongTierPenaltyDistance; ;
                    break;
                case PrefRestLocationForBot.Nearest:
                    value = restLocation.Tier == bot.Tier ?
                        bot.GetDistance(restLocation) :
                        bot.GetDistance(restLocation) + Instance.WrongTierPenaltyDistance;
                    break;
                default: throw new ArgumentException("Unknown order type: " + orderType);
            }
            return value;
        }

        /// <summary>
        /// Chooses a rest location from all available ones for the given bot.
        /// </summary>
        /// <param name="bot">The bot to choose a rest location for.</param>
        /// <param name="restLocationOrderType">The rest location preference.</param>
        private Waypoint ChooseRestLocation(Bot bot, PrefRestLocationForBot restLocationOrderType)
        {
            return
                Instance.ResourceManager.UnusedRestingLocations.Any() ? // Check whether there is another free pod storage location to use for resting
                Instance.ResourceManager.UnusedRestingLocations.ArgMin(w => RestLocationToBotAllocationMetric(restLocationOrderType, bot, w)) :
                Instance.WaypointGraph.GetClosestWaypoint(bot.Tier, bot.X, bot.Y); ;
        }

        /// <summary>
        /// Allocates a rest task.
        /// </summary>
        /// <param name="bot">The bot that shall rest.</param>
        /// <param name="restLocationOrder">The order in which the next free resting location is chosen (if the bot already rested before, the same location is used).</param>
        protected void DoRestTask(Bot bot, PrefRestLocationForBot restLocationOrder)
        {
            // Get the last task that was assigned to the bot
            BotTask lastTask = GetLastEnqueuedTask(bot);
            // Choose resting location
            Waypoint restLocation;
            // Check whether the last task was resting too
            if (lastTask != null && lastTask.Type == BotTaskType.Rest && Instance.ResourceManager.IsRestingLocationAvailable((lastTask as RestTask).RestingLocation))
                restLocation = (lastTask as RestTask).RestingLocation;
            else
                restLocation = ChooseRestLocation(bot, restLocationOrder);
            // Submit the task
            EnqueueRest(bot, restLocation);
        }

        /// <summary>
        /// Allocates a repositioning task, if available.
        /// </summary>
        /// <param name="bot">The bot that shall perform a repositioning task.</param>
        /// <returns><code>true</code> if the bot was assigned a repositioning task, <code>false</code> otherwise (no task was assigned).</returns>
        protected bool DoRepositioningTask(Bot bot)
        {
            // Reposition a pod, if a move is available
            RepositioningMove move = Instance.Controller.RepositioningManager.GetNextMove(bot);
            // Check whether a suitable move was available
            if (move != null)
            {
                // We found a repositioning move - skip resting and proceed with repositioning instead
                EnqueueRepositionPod(bot, move.Pod, move.StorageLocation);
                return true;
            }
            else
            {
                // Indicate no repositioning available
                return false;
            }
        }

        /// <summary>
        /// Attempts to do a repositioning task. If no such task is available a resting task is done instead.
        /// </summary>
        /// <param name="bot">The bot.</param>
        /// <param name="restLocationOrder">The order in which the next free resting location is chosen.</param>
        /// <returns><code>true</code> if a repositioning move was found, <code>false</code> otherwise.</returns>
        protected bool DoRepositioningTaskOrRest(Bot bot, PrefRestLocationForBot restLocationOrder)
        {
            // Reposition a pod, if a move is available
            RepositioningMove move = Instance.Controller.RepositioningManager.GetNextMove(bot);
            // Check whether a suitable move was available
            if (move != null)
            {
                // We found a repositioning move - skip resting and proceed with repositioning instead
                EnqueueRepositionPod(bot, move.Pod, move.StorageLocation);
                return true;
            }
            // No repositioning move - just rest
            DoRestTask(bot, restLocationOrder);
            return false;
        }

        /// <summary>
        /// Attempts to park the currently carried pod.
        /// </summary>
        /// <param name="bot">The bot carrying a pod.</param>
        /// <returns><code>true</code> if a park pod task was enqueued, <code>false</code> otherwise.</returns>
        protected bool DoParkPodTask(Bot bot)
        {
            if (bot.Pod != null)
            {
                EnqueueParkPod(bot, bot.Pod, Instance.Controller.PodStorageManager.GetStorageLocation(bot.Pod));
                return true;
            }
            else { return false; }
        }

        #endregion

        #region IBotManager Members

        /// <summary>
        /// Requests a new task for the given robot.
        /// </summary>
        /// <param name="bot">The bot.</param>
        public void RequestNewTask(Bot bot)
        {
            // Check for further tasks to do
            if (_taskQueues[bot] == null || _taskQueues[bot].Type == BotTaskType.None)
            {
                // Measure time for fetching the task
                DateTime before = DateTime.Now;

                // No tasks available - get the next task
                GetNextTask(bot);

                // Calculate time it took to decide the next task to do
                Instance.Observer.TimeTaskAllocation((DateTime.Now - before).TotalSeconds);
            }

            // Assign the next task
            if (_taskQueues[bot] != null)
            {
                // Assign the task
                bot.AssignTask(_taskQueues[bot]);
            }
            else
            {
                // No tasks available - assign dummy task
                bot.AssignTask(new DummyTask(Instance, bot));
            }
        }

        /// <summary>
        /// This has to be called when the robot picked up the given pod.
        /// </summary>
        /// <param name="r">The bot that picked up the pod.</param>
        /// <param name="b">The pod that was picked up.</param>
        /// <param name="w">The waypoint at which the robot executed the operation.</param>
        public void PodPickedUp(Bot r, Pod b, Waypoint w)
        {
            // Free storage location
            b.Waypoint = null;
            b.Bot = r;
            Instance.ResourceManager.ReleaseStorageLocation(w);
        }
        /// <summary>
        /// This has to be called when the robot set down the given pod.
        /// </summary>
        /// <param name="r">The bot that set down the pod.</param>
        /// <param name="b">The pod that was set down.</param>
        /// <param name="w">The waypoint at which the robot executed the operation.</param>
        public void PodSetDown(Bot r, Pod b, Waypoint w)
        {
            // Free the pod
            b.Waypoint = w;
            b.Bot = null;
            Instance.ResourceManager.ReleasePod(b);
        }
        /// <summary>
        /// This has to be called when the robot finished its task.
        /// </summary>
        /// <param name="r">The robot that finished its task.</param>
        /// <param name="t">The task that the robot finished.</param>
        public void TaskComplete(Bot r, BotTask t)
        {
            if (t == null)
                return;
            if (t.Type != BotTaskType.None && t != _taskQueues[r])
                throw new ArgumentException("Wrong task to complete - bot was executing another task!");

            // Remove the finished task and free the resources
            _taskQueues[r].Finish();
            _taskQueues[r] = new DummyTask(Instance, r);

            // Change to dummy task
            r.AssignTask(_taskQueues[r]);
        }
        /// <summary>
        /// This has to be called when the robot aborts its task.
        /// </summary>
        /// <param name="r">The robot that aborted its task.</param>
        /// <param name="t">The task that the robot aborted.</param>
        public void TaskAborted(Bot r, BotTask t)
        {
            if (t == null)
                throw new ArgumentException("No task to abort given!");
            if (_taskQueues[r] == null)
                throw new InvalidOperationException("No task to abort - referenced task was: " + t.ToString());
            if (t.Type != BotTaskType.None && t != _taskQueues[r])
                throw new ArgumentException("Wrong task to cancel - bot was executing another task!");

            // Log warning
            Instance.LogDefault("WARNING! Task aborted: " + t.Type);

            // Cancel the task and remove it
            _taskQueues[r].Cancel();
            _taskQueues[r] = new DummyTask(Instance, r);

            // Change to dummy task
            r.AssignTask(_taskQueues[r]);
        }

        #endregion

        #region IUpdateable Members

        /// <summary>
        /// The next event when this element has to be updated.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns>The next time this element has to be updated.</returns>
        public abstract double GetNextEventTime(double currentTime);
        /// <summary>
        /// Updates the element to the specified time.
        /// </summary>
        /// <param name="lastTime">The time before the update.</param>
        /// <param name="currentTime">The time to update to.</param>
        public abstract void Update(double lastTime, double currentTime);

        #endregion

        #region IOptimize Members

        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        public abstract void SignalCurrentTime(double currentTime);

        #endregion

        #region IStatTracker Members

        /// <summary>
        /// The callback that indicates that the simulation is finished and statistics have to submitted to the instance.
        /// </summary>
        public virtual void StatFinish()
        {
            // Call PC component too
            StatFinishPC();
        }

        /// <summary>
        /// The callback indicates a reset of the statistics.
        /// </summary>
        public virtual void StatReset()
        {
            // Call PC component too
            StatResetPC();
        }

        #endregion
    }
}
