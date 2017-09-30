using RAWSimO.Core.Control;
using RAWSimO.Core.Bots;
using RAWSimO.Core.Geometrics;
using RAWSimO.Core.Helper;
using RAWSimO.Core.Info;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.IO;
using RAWSimO.Core.Management;
using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using RAWSimO.MultiAgentPathFinding;
using RAWSimO.Toolbox;

namespace RAWSimO.Core.Elements
{
    /// <summary>
    /// The base class for a robot.
    /// </summary>
    public abstract class Bot : Circle, IBotInfo, IUpdateable, IBotEventListener, IExposeVolatileID
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance of the robot.
        /// </summary>
        /// <param name="instance">The instance this bot belongs to.</param>
        internal Bot(Instance instance) : base(instance) { CurrentTask = new DummyTask(Instance, this); }

        #endregion

        #region FixedAttributes

        /// <summary>
        /// Time necessary to pickup or set down a pod in seconds.
        /// </summary>
        public double PodTransferTime { get; internal set; }
        /// <summary>
        /// The maximal possible acceleration in m/s^2.
        /// </summary>
        public double MaxAcceleration { get; internal set; }
        /// <summary>
        /// The maximal possible deceleration in m/s^2.
        /// </summary>
        public double MaxDeceleration { get; internal set; }
        /// <summary>
        /// The maximal possible velocity in m/s.
        /// </summary>
        public double MaxVelocity { get; internal set; }
        /// <summary>
        /// The speed of the robot for turning on the spot.
        /// <remarks>The unit of measure is the simulation time it takes for a full 360° turn.</remarks>
        /// </summary>
        public double TurnSpeed { get; internal set; }
        /// <summary>
        /// Penalty time applied after a collision.
        /// </summary>
        public double CollisionPenaltyTime { get; internal set; }

        #endregion

        #region DynamicAttributes

        /// <summary>
        /// The pod (if this bot is carrying one, otherwise <code>null</code>)
        /// </summary>
        public Pod Pod { get; internal set; }
        /// <summary>
        /// The current task this bot is occupied by.
        /// </summary>
        public BotTask CurrentTask { get; internal set; }
        /// <summary>
        /// Last way point the bot passed.
        /// </summary>
        public abstract Waypoint CurrentWaypoint { get; set; }
        /// <summary>
        /// The current destination of the bot as useful information for other mechanisms.
        /// </summary>
        internal abstract Waypoint TargetWaypoint { get; }
        /// <summary>
        /// The bot's current velocity regarding the x-dimension.
        /// </summary>
        public double XVelocity { get; protected set; }
        /// <summary>
        /// The bot's current velocity regarding the y-dimension.
        /// </summary>
        public double YVelocity { get; protected set; }
        /// <summary>
        /// Moment when the bot finishes being blocked by a performing task.
        /// </summary>
        public double BlockedUntil = -1.0;
        /// <summary>
        /// A flag that indicates that the robot is currently queueing.
        /// </summary>
        public bool IsQueueing = false;

        #endregion

        #region Core
        /// <summary>
        /// Returns this bot's current velocity.
        /// </summary>
        /// <returns>The speed at which this bot is currently traveling.</returns>
        public double GetSpeed() { return Math.Sqrt(this.XVelocity * this.XVelocity + this.YVelocity * this.YVelocity); }

        /// <summary>
        /// Blocks the robot for the specified time.
        /// </summary>
        /// <param name="time">The time to be blocked for.</param>
        public abstract void WaitUntil(double time);

        /// <summary>
        /// Assigns a new task to the bot.
        /// </summary>
        /// <param name="t">The task to execute.</param>
        public abstract void AssignTask(BotTask t);

        /// <summary>
        /// Picks up the specified pod.
        /// </summary>
        /// <param name="pod">The pod to pick up.</param>
        /// <param name="currentTime">The current simulation time.</param>
        /// <returns><code>true</code> if the pickup-operation was successful, <code>false</code> otherwise.</returns>
        public bool PickupPod(Pod pod, double currentTime)
        {
            // Cannot pick up no pod
            if (pod == null)
                return false;

            // Cannot pick up a pod if already carrying one
            if (this.Pod != null)
                return false;

            // Can't ask pick up while moving
            if (this.GetSpeed() > 0.0)
                return false;

            // Cannot pick up a pod that is on another tier
            if (Instance.Compound.BotCurrentTier[this] != Instance.Compound.PodCurrentTier[pod])
                return false;

            // If outside of tolerance range, can't pick it up
            if ((pod.X - this.X) * (pod.X - this.X) + (pod.Y - this.Y) * (pod.Y - this.Y) > Instance.SettingConfig.Tolerance * Instance.SettingConfig.Tolerance)
                return false;

            // See if pod bot can adjust position to pick it up
            if (!Instance.Compound.BotCurrentTier[this].MoveBot(this, pod.X, pod.Y))
                return false;

            //#RealWorldIntegration.start
            if (Instance.SettingConfig.RealWorldIntegrationCommandOutput)
            {
                // Log the pickup command
                var sb = new StringBuilder();
                sb.Append("#RealWorldIntegration => Bot ").Append(ID).Append(" PickUp Pod");
                Instance.SettingConfig.LogAction(sb.ToString());
                // Issue the pickup command
                Instance.RemoteController.RobotSubmitPickupCommand(ID);
            }
            //#RealWorldIntegration.end

            // Pick it up!
            pod.InUse = true;
            this.Pod = pod;
            BlockedUntil = currentTime + this.PodTransferTime;
            this.StatNumberOfPickups++;

            // Notify the instance about the pickup operation
            Instance.NotifyPodPickup(pod, this);

            // Return success
            return true;
        }

        /// <summary>
        /// Sets down the current pod.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns><code>true</code> if the set down-operation was successful, <code>false</code> otherwise.</returns>
        public bool SetdownPod(double currentTime)
        {
            // Cannot set down a pod, if not carrying one
            if (this.Pod == null)
                return false;

            // Remember pod
            Pod pod = this.Pod;

            // Can't set down while moving
            if (this.GetSpeed() > 0.0)
                return false;

            // Set pod to default orientation // TODO use this? actually this should be fine.
            this.Pod.Orientation = Pod.DEFAULT_BUCKET_ORIENTATION;

            //#RealWorldIntegration.start
            if (Instance.SettingConfig.RealWorldIntegrationCommandOutput)
            {
                // Log the setdown command
                var sb = new StringBuilder();
                sb.Append("#RealWorldIntegration => Bot ").Append(ID).Append(" SetDown Pod");
                Instance.SettingConfig.LogAction(sb.ToString());
                // Issue the setdown command
                Instance.RemoteController.RobotSubmitSetdownCommand(ID);
            }
            //#RealWorldIntegration.end

            // Block the bot for the time needed to set down the pod
            this.Pod.InUse = false;
            this.BlockedUntil = currentTime + this.PodTransferTime;
            this.StatNumberOfSetdowns++;
            this.Pod = null;

            // Notify the instance about the setdown operation
            Instance.NotifyPodSetdown(pod, this);

            // Return successfully
            return true;
        }

        #endregion

        #region Statistics

        /// <summary>
        /// The number of pickups done so far.
        /// </summary>
        public int StatNumberOfPickups;
        /// <summary>
        /// The number of set downs done so far.
        /// </summary>
        public int StatNumberOfSetdowns;
        /// <summary>
        /// The number of collisions that happened so far.
        /// </summary>
        public int StatNumCollisions;
        /// <summary>
        /// The distance traveled by this bot so far.
        /// </summary>
        public double StatDistanceTraveled;
        /// <summary>
        /// The assigned task for this bot so far.
        /// </summary>
        public int StatAssignedTasks;
        /// <summary>
        /// The distance of the direct path.
        /// </summary>
        public double StatDistanceEstimated;
        /// <summary>
        /// The optimal overall distance for completion of all requested trips.
        /// </summary>
        public double StatDistanceRequestedOptimal;
        /// <summary>
        /// The time the robot was moving.
        /// </summary>
        public double StatTotalTimeMoving;
        /// <summary>
        /// The time the robot was queueing.
        /// </summary>
        public double StatTotalTimeQueueing;
        /// <summary>
        /// The last task the bot was executing.
        /// </summary>
        public BotTaskType StatLastTask = BotTaskType.None;
        /// <summary>
        /// The last waypoint this bot was located at.
        /// </summary>
        public Waypoint StatLastWaypoint = null;
        /// <summary>
        /// The last time the robot went on a new trip.
        /// </summary>
        public double StatLastTripStartTime = 0;
        /// <summary>
        /// The time spent in each task.
        /// </summary>
        public Dictionary<BotTaskType, double> StatTotalTaskTimes = new Dictionary<BotTaskType, double>(Enum.GetValues(typeof(BotTaskType)).Cast<BotTaskType>().ToDictionary(k => k, v => 0.0));
        /// <summary>
        /// The number of tasks done per type.
        /// </summary>
        public Dictionary<BotTaskType, int> StatTotalTaskCounts = new Dictionary<BotTaskType, int>(Enum.GetValues(typeof(BotTaskType)).Cast<BotTaskType>().ToDictionary(k => k, v => 0));
        /// <summary>
        /// The last state the bot was in.
        /// </summary>
        public BotStateType StatLastState = BotStateType.Rest;
        /// <summary>
        /// The time spent in each state.
        /// </summary>
        public Dictionary<BotStateType, double> StatTotalStateTimes = new Dictionary<BotStateType, double>(Enum.GetValues(typeof(BotStateType)).Cast<BotStateType>().ToDictionary(k => k, v => 0.0));
        /// <summary>
        /// The number of times the bot was in each state type.
        /// </summary>
        public Dictionary<BotStateType, int> StatTotalStateCounts = new Dictionary<BotStateType, int>(Enum.GetValues(typeof(BotStateType)).Cast<BotStateType>().ToDictionary(k => k, v => 0));

        /// <summary>
        /// Logs the data of an unfinished trip.
        /// </summary>
        internal abstract void LogIncompleteTrip();

        /// <summary>
        /// Resets all statistics.
        /// </summary>
        public void ResetStatistics()
        {
            StatNumberOfPickups = 0;
            StatNumberOfSetdowns = 0;
            StatNumCollisions = 0;
            StatDistanceTraveled = 0;
            StatDistanceEstimated = 0;
            StatDistanceRequestedOptimal = 0;
            StatAssignedTasks = 0;
            StatTotalTimeMoving = 0;
            StatTotalTimeQueueing = 0;
            StatLastTask = BotTaskType.None;
            StatLastState = BotStateType.Rest;
            StatLastWaypoint = null;
            StatTotalTaskTimes = new Dictionary<BotTaskType, double>(Enum.GetValues(typeof(BotTaskType)).Cast<BotTaskType>().ToDictionary(k => k, v => 0.0));
            StatTotalStateTimes = new Dictionary<BotStateType, double>(Enum.GetValues(typeof(BotStateType)).Cast<BotStateType>().ToDictionary(k => k, v => 0.0));
            StatTotalTaskCounts = new Dictionary<BotTaskType, int>(Enum.GetValues(typeof(BotTaskType)).Cast<BotTaskType>().ToDictionary(k => k, v => 0));
            StatTotalStateCounts = new Dictionary<BotStateType, int>(Enum.GetValues(typeof(BotStateType)).Cast<BotStateType>().ToDictionary(k => k, v => 0));
        }

        #endregion

        #region Object Members

        /// <summary>
        /// Returns a string that can be used to identify the object.
        /// </summary>
        /// <returns>A string representing the object.</returns>
        public override string GetIdentfierString() { return "Bot" + this.ID; }
        /// <summary>
        /// Returns a string that can be used to identify the object.
        /// </summary>
        /// <returns>A string representing the object.</returns>
        public override string ToString() { return "Bot" + this.ID; }

        #endregion

        #region IUpdateable Members

        /// <summary>
        /// The next event when this element has to be updated.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns>The next time this element has to be updated.</returns>
        public abstract double GetNextEventTime(double currentTime);
        /// <summary>
        /// Do update.
        /// </summary>
        /// <param name="lastTime">last time update call</param>
        /// <param name="currentTime">current time</param>
        public abstract void Update(double lastTime, double currentTime);

        #endregion

        #region IMovableObject Members

        /// <summary>
        /// Gets the current tier this object is placed on. Can't change in case of an immovable object.
        /// </summary>
        /// <returns>The current tier.</returns>
        public ITierInfo GetInfoCurrentTier() { return Tier; }
        /// <summary>
        /// Indicates whether the underlying object changed since the last call of <code>GetChanged()</code>.
        /// </summary>
        /// <returns><code>true</code> if the object changed since the last call of this method, <code>false</code> otherwise.</returns>
        public bool GetInfoChanged() { bool changed = _changed; _changed = false; return changed; }
        /// <summary>
        /// Returns the active instance belonging to this element.
        /// </summary>
        /// <returns>The active instance.</returns>
        public IInstanceInfo GetInfoInstance() { return Instance; }

        #endregion

        #region IBotInfo Members

        /// <summary>
        /// Returns the current task the bot is executing.
        /// </summary>
        /// <returns>The active task.</returns>
        public string GetInfoTask() { return CurrentTask.ToString(); }
        /// <summary>
        /// Gets the current speed of the bot.
        /// </summary>
        /// <returns>The speed in m/s.</returns>
        public double GetInfoSpeed() { return GetSpeed(); }
        /// <summary>
        /// Returns the current state the bot is in.
        /// </summary>
        /// <returns>The active state.</returns>
        public abstract string GetInfoState();
        /// <summary>
        /// Gets the current waypoint that is considered by planning.
        /// </summary>
        /// <returns>The current waypoint.</returns>
        public abstract IWaypointInfo GetInfoCurrentWaypoint();
        /// <summary>
        /// Gets the destination of the bot.
        /// </summary>
        /// <returns>The destination.</returns>
        public abstract IWaypointInfo GetInfoDestinationWaypoint();
        /// <summary>
        /// Gets the goal of the bot.
        /// </summary>
        /// <returns>The goal.</returns>
        public abstract IWaypointInfo GetInfoGoalWaypoint();
        /// <summary>
        /// Gets the current path of the bot.
        /// </summary>
        /// <returns>The current path.</returns>
        public abstract List<IWaypointInfo> GetInfoPath();
        /// <summary>
        /// Gets the x-position of the goal of the bot.
        /// </summary>
        /// <returns>The x-position.</returns>
        public abstract double GetInfoGoalX();
        /// <summary>
        /// Gets the y-position of the goal of the bot.
        /// </summary>
        /// <returns>The y-position.</returns>
        public abstract double GetInfoGoalY();
        /// <summary>
        /// Gets the target orientation in radians. (An element facing east is defined with orientation 0 or equally 2*pi.)
        /// </summary>
        /// <returns>The orientation.</returns>
        public abstract double GetInfoTargetOrientation();
        /// <summary>
        /// Indicates whether the robot is currently blocked.
        /// </summary>
        /// <returns><code>true</code> if the robot is blocked, <code>false</code> otherwise.</returns>
        public abstract bool GetInfoBlocked();
        /// <summary>
        /// The time until the bot is blocked.
        /// </summary>
        /// <returns>The time until the bot is blocked.</returns>
        public abstract double GetInfoBlockedLeft();
        /// <summary>
        /// Indicates whether the bot is currently queueing in a managed area.
        /// </summary>
        /// <returns><code>true</code> if the robot is within a queue area, <code>false</code> otherwise.</returns>
        public abstract bool GetInfoIsQueueing();

        #endregion

        #region IExposeVolatileID

        /// <summary>
        /// An ID that is useful as an index for listing this item.
        /// This ID is unique among all <code>ItemDescription</code>s while being as low as possible.
        /// Note: For now the volatile ID matches the actual ID.
        /// </summary>
        int IExposeVolatileID.VolatileID { get { return VolatileID; } }

        #endregion

        #region Events
        /// <summary>
        /// Called when [bot reached way point].
        /// </summary>
        /// <param name="waypoint">The way point.</param>
        public abstract void OnReachedWaypoint(Waypoint waypoint);

        /// <summary>
        /// Called when [bot picked up the pod].
        /// </summary>
        public abstract void OnPickedUpPod();

        /// <summary>
        /// Called when [bot set down pod].
        /// </summary>
        public abstract void OnSetDownPod();

        #endregion
    }
}
