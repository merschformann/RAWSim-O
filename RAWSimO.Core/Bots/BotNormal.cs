using RAWSimO.Core.Control;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Waypoints;
using RAWSimO.MultiAgentPathFinding;
using System;
using System.Collections.Generic;
using System.Linq;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.Info;
using RAWSimO.Core.Items;
using RAWSimO.Core.Geometrics;
using RAWSimO.MultiAgentPathFinding.Physic;
using System.Diagnostics;
using System.Text;
using RAWSimO.Core.IO;
using RAWSimO.Core.Metrics;
using RAWSimO.Toolbox;

namespace RAWSimO.Core.Bots
{
    /// <summary>
    /// Bot Driver
    /// Optimization is complete delegated to the controller, because he knows all the bots
    /// </summary>
    public class BotNormal : Bot
    {

        #region Attributes

        /// <summary>
        /// The bots request a re-optimization after failing of next way point reservation
        /// </summary>
        public static bool RequestReoptimizationAfterFailingOfNextWaypointReservation = false;

        /// <summary>
        /// The current destination of the bot as useful information for other mechanisms. If not available, the current waypoint will be provided.
        /// </summary>
        internal override Waypoint TargetWaypoint { get { return _destinationWaypoint != null ? _destinationWaypoint : _currentWaypoint; } }

        /// <summary>
        /// destination way point
        /// </summary>
        public Waypoint DestinationWaypoint
        {
            get { return _destinationWaypoint; }
            set
            {
                if (_destinationWaypoint != value)
                {
                    _destinationWaypoint = value;
                    Instance.Controller.PathManager.notifyBotNewDestination(this);
                }
            }
        }
        private Waypoint _destinationWaypoint;

        /// <summary>
        /// next way point
        /// </summary>
        public Waypoint NextWaypoint
        {
            get
            {
                return _nextWaypoint;
            }
            private set
            {
                if (value != null) { _nextWaypointID = value.ID; }
                _nextWaypoint = value;
            }
        }
        private Waypoint _nextWaypoint;
        private int _nextWaypointID;

        /// <summary>
        /// next way point
        /// </summary>
        public bool RequestReoptimization;

        #region State handling

        /// <summary>
        /// The state queue
        /// </summary>
        private Queue<IBotState> _stateQueue = new Queue<IBotState>();
        /// <summary>
        /// Returns the next state in the state queue without removing it.
        /// </summary>
        /// <returns>The next state in the state queue.</returns>
        private IBotState StateQueuePeek() { return _stateQueue.Peek(); }
        /// <summary>
        /// Enqueues a state.
        /// </summary>
        /// <param name="state">The state to enqueue.</param>
        private void StateQueueEnqueue(IBotState state) { _stateQueue.Enqueue(state); _currentInfoStateName = _stateQueue.Peek().ToString(); }
        /// <summary>
        /// Dequeues the next state from the state queue.
        /// </summary>
        /// <returns>The state that was just dequeued.</returns>
        private IBotState StateQueueDequeue() { IBotState state = _stateQueue.Dequeue(); _currentInfoStateName = _stateQueue.Any() ? _stateQueue.Peek().ToString() : ""; return state; }
        /// <summary>
        /// Clears the complete state queue.
        /// </summary>
        private void StateQueueClear() { _stateQueue.Clear(); _currentInfoStateName = ""; }
        /// <summary>
        /// The number of states currently in the queue.
        /// </summary>
        private int StateQueueCount { get { return _stateQueue.Count; } }

        #endregion

        /// <summary>
        /// drive until
        /// </summary>
        private double _driveDuration = -1.0;

        /// <summary>
        /// rotate until
        /// </summary>
        private double _rotateDuration = -1.0;

        /// <summary>
        /// rotate until
        /// </summary>
        private double _waitUntil = -1.0;

        /// <summary>
        /// rotate until
        /// </summary>
        private double _startOrientation = 0;

        /// <summary>
        /// rotate until
        /// </summary>
        private double _endOrientation = 0;

        /// <summary>
        /// The agent reached the next way point
        /// </summary>
        private bool _eventReachedNextWaypoint = false;

        /// <summary>
        /// Indicates whether the first position info was received from the remote server.
        /// </summary>
        private bool _initialEventReceived = false;

        /// <summary>
        /// Indicates the last state of the robot. This is used to lower the communication with the robot.
        /// </summary>
        private BotStateType _lastExteriorState = BotStateType.Rest;

        /// <summary>
        /// The physics calculation object.
        /// </summary>
        public Physics Physics;

        /// <summary>
        /// The current path.
        /// </summary>
        private Path _path;
        /// <summary>
        /// The current path.
        /// </summary>
        public Path Path
        {
            get
            {
                // Just return it
                return _path;
            }
            internal set
            {
                // Set it
                _path = value;
                // Make path public if visualization is present
                if (Instance.SettingConfig.VisualizationAttached && _path != null)
                    _currentPath = _path.Actions.Select(a => Instance.Controller.PathManager.GetWaypointByNodeId(a.Node)).Cast<IWaypointInfo>().ToList();
            }
        }

        #endregion

        #region core

        /// <summary>
        /// Initializes a new instance of the <see cref="BotNormal"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="podTransferTime">The pod transfer time.</param>
        /// <param name="acceleration">The maximum acceleration.</param>
        /// <param name="deceleration">The maximum deceleration.</param>
        /// <param name="maxVelocity">The maximum velocity.</param>
        /// <param name="turnSpeed">The turn speed.</param>
        /// <param name="collisionPenaltyTime">The collision penalty time.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        public BotNormal(int id, Instance instance, double radius, double podTransferTime, double acceleration, double deceleration, double maxVelocity, double turnSpeed, double collisionPenaltyTime, double x = 0.0, double y = 0.0) : base(instance)
        {
            this.ID = id;
            this.Instance = instance;
            this.Radius = radius;
            this.X = x;
            this.Y = y;
            this.PodTransferTime = podTransferTime;
            this.MaxAcceleration = acceleration;
            this.MaxDeceleration = deceleration;
            this.MaxVelocity = maxVelocity;
            this.TurnSpeed = turnSpeed;
            this.CollisionPenaltyTime = collisionPenaltyTime;
            this.Physics = new Physics(acceleration, deceleration, maxVelocity, turnSpeed);
        }

        /// <summary>
        /// orientation the bot should look at
        /// </summary>
        /// <returns>orientation</returns>
        public double GetTargetOrientation()
        {
            return _endOrientation;
        }

        #endregion

        #region Bot Members

        /// <summary>
        /// last way point
        /// </summary>
        public override Waypoint CurrentWaypoint
        {
            get
            {
                // Get it
                return _currentWaypoint;
            }
            set
            {
                // Set it
                _currentWaypoint = value;
                // If the current waypoint belongs to a queue, notify the corresponding manager
                if (_currentWaypoint.QueueManager != null)
                    // Notify the manager about this bot joining the queue even if the bot accidentally joined it
                    _currentWaypoint.QueueManager.onBotJoinQueue(this);
            }
        }
        /// <summary>
        /// The last waypoint.
        /// </summary>
        private Waypoint _currentWaypoint;

        /// <summary>
        /// assign a task to a bot -&gt; delegate to controller
        /// </summary>
        /// <param name="t">task</param>
        /// <exception cref="System.ArgumentException">Unknown task-type:  + t.Type</exception>
        public override void AssignTask(BotTask t)
        {
            this.CurrentTask = t;
            // Warn when clearing incomplete tasks
            if (StateQueueCount > 0)
                Instance.LogDefault("WARNING! Aborting some incomplete task: " + string.Join(", ", _stateQueue.Select(s => s.Type)));
            // Forget old tasks
            StateQueueClear();

            switch (t.Type)
            {
                case BotTaskType.None:
                    return;
                case BotTaskType.ParkPod:

                    //re-optimize
                    RequestReoptimization = true;

                    ParkPodTask storePodTask = t as ParkPodTask;
                    // If we have another pod we cannot store the given one
                    if (storePodTask.Pod != Pod)
                    {
                        Instance.LogDefault("WARNING! Cannot park a pod that the bot is not carrying!");
                        Instance.Controller.BotManager.TaskAborted(this, storePodTask);
                        return;
                    }
                    // Add the move states for parking the pod
                    _appendMoveStates(CurrentWaypoint, storePodTask.StorageLocation);
                    // After bringing the pod to the storage location set it down
                    StateQueueEnqueue(new BotSetdownPod(storePodTask.StorageLocation));

                    break;
                case BotTaskType.RepositionPod:

                    //re-optimize
                    RequestReoptimization = true;

                    RepositionPodTask repositionPodTask = t as RepositionPodTask;
                    // If don't have pod requested to store, then go get it 
                    if (Pod == null)
                    {
                        // Add states for getting the pod
                        _appendMoveStates(CurrentWaypoint, repositionPodTask.Pod.Waypoint);
                        // Add state for picking up pod
                        StateQueueEnqueue(new BotPickupPod(repositionPodTask.Pod));
                        // Add states for repositioning the pod
                        _appendMoveStates(repositionPodTask.Pod.Waypoint, repositionPodTask.StorageLocation);
                        // After bringing the pod to the storage location set it down
                        StateQueueEnqueue(new BotSetdownPod(repositionPodTask.StorageLocation));
                        // Log a repositioning move
                        Instance.NotifyRepositioningStarted(this, repositionPodTask.Pod.Waypoint, repositionPodTask.StorageLocation, repositionPodTask.Pod);
                    }
                    // We are already carrying a pod: we cannot execute the task
                    else
                    {
                        Instance.LogDefault("WARNING! Cannot reposition a pod when the robot already is carrying one!");
                        Instance.Controller.BotManager.TaskAborted(this, repositionPodTask);
                        return;
                    }

                    break;
                case BotTaskType.Insert:

                    //re-optimize
                    RequestReoptimization = true;

                    InsertTask storeTask = t as InsertTask;
                    if (storeTask.ReservedPod != Pod)
                    {
                        var podWaypoint = storeTask.ReservedPod.Waypoint;
                        _appendMoveStates(CurrentWaypoint, podWaypoint);
                        StateQueueEnqueue(new BotPickupPod(storeTask.ReservedPod));
                        _appendMoveStates(podWaypoint, storeTask.InputStation.Waypoint);
                    }
                    else
                    {
                        _appendMoveStates(CurrentWaypoint, storeTask.InputStation.Waypoint);
                    }
                    StateQueueEnqueue(new BotGetItems(storeTask));

                    break;
                case BotTaskType.Extract:

                    //re-optimize
                    RequestReoptimization = true;

                    ExtractTask extractTask = t as ExtractTask;
                    if (extractTask.ReservedPod != Pod)
                    {
                        var podWaypoint = extractTask.ReservedPod.Waypoint;
                        _appendMoveStates(CurrentWaypoint, podWaypoint);
                        StateQueueEnqueue(new BotPickupPod(extractTask.ReservedPod));
                        _appendMoveStates(podWaypoint, extractTask.OutputStation.Waypoint);
                    }
                    else
                    {
                        _appendMoveStates(CurrentWaypoint, extractTask.OutputStation.Waypoint);
                    }
                    StateQueueEnqueue(new BotPutItems(extractTask));

                    break;
                case BotTaskType.Rest:
                    var restTask = t as RestTask;
                    // Only append move task to get to resting location, if we are not at it yet
                    if ((restTask.RestingLocation != null) && (CurrentWaypoint != restTask.RestingLocation || Moving))
                        _appendMoveStates(CurrentWaypoint, restTask.RestingLocation);
                    StateQueueEnqueue(new BotRest(restTask.RestingLocation, BotRest.DEFAULT_REST_TIME)); // TODO set paramterized wait time and adhere to it
                    break;
                default:
                    throw new ArgumentException("Unknown task-type: " + t.Type);
            }

            // Track task count
            StatAssignedTasks++;
            StatTotalTaskCounts[t.Type]++;
        }

        /// <summary>
        /// appends the move states with respect to tiers and elevators.
        /// </summary>
        /// <param name="waypointFrom">The from waypoint.</param>
        /// <param name="waypointTo">The destination waypoint.</param>
        private void _appendMoveStates(Waypoint waypointFrom, Waypoint waypointTo)
        {
            double distance;
            var checkPoints = Instance.Controller.PathManager.FindElevatorSequence(this, waypointFrom, waypointTo, out distance);
            StatDistanceEstimated += distance;

            foreach (var point in checkPoints)
            {
                StateQueueEnqueue(new BotMove(point.Item2));
                StateQueueEnqueue(new UseElevator(point.Item1, point.Item2, point.Item3));
            }

            StateQueueEnqueue(new BotMove(waypointTo));
        }

        /// <summary>
        /// Dequeues the state.
        /// </summary>
        /// <param name="lastTime">The last time.</param>
        /// <param name="currentTime">The current time.</param>
        private void DequeueState(double lastTime, double currentTime)
        {
            IBotState dequeuedState = StateQueueDequeue();

            /*
            if (_stateQueue.Count > 0)
            {
                //directly the next way point
                var moveBot = _stateQueue.Peek() as BotMove;
                if(moveBot != null && moveBot.DestinationWaypoint == CurrentWaypoint)
                {
                    DequeueState(currentTime);
                    return;
                }
            }
             * */

            Debug.Assert(StateQueueCount == 0 || !(StateQueuePeek() is BotMove) || this.CurrentWaypoint.Tier.ID == ((BotMove)StateQueuePeek()).DestinationWaypoint.Tier.ID);

            // Act on next task
            if (StateQueueCount > 0)
                StateQueuePeek().Act(this, lastTime, currentTime);
        }

        /// <summary>
        /// Sets the next way point.
        /// </summary>
        /// <param name="waypoint">The way point.</param>
        /// <param name="currentTime">The current time.</param>
        /// <returns>A boolean value indicating whether the reservation was successful.</returns>
        public bool setNextWaypoint(Waypoint waypoint, double currentTime)
        {
            if (GetSpeed() > 0)
                return false;
            if (X == waypoint.X && Y == waypoint.Y)
                throw new ArgumentException("Already at the given waypoint!");

            _startOrientation = Orientation;
            _endOrientation = Circle.GetOrientation(X, Y, waypoint.X, waypoint.Y);
            var rotateDuration = Physics.getTimeNeededToTurn(_startOrientation, _endOrientation);
            var waitUntil = Math.Max(_waitUntil, currentTime);

            if (Instance.Controller.PathManager.RegisterNextWaypoint(this, currentTime, waitUntil, rotateDuration, CurrentWaypoint, waypoint))
            {
                //set way point
                NextWaypoint = waypoint;

                //set move times
                _waitUntil = waitUntil;
                _rotateDuration = rotateDuration;
                _driveDuration = Physics.getTimeNeededToMove(0, CurrentWaypoint.GetDistance(NextWaypoint));

                return true;

            }
            else
            {
                NextWaypoint = null;
                if (RequestReoptimizationAfterFailingOfNextWaypointReservation)
                    RequestReoptimization = true;

                // Log failed reservation
                Instance.StatOverallFailedReservations++;

                return false;
            }
        }

        /// <summary>
        /// Blocks the robot for the specified time.
        /// </summary>
        /// <param name="time">The time to be blocked for.</param>
        public override void WaitUntil(double time)
        {
            if (this.GetSpeed() > 0)
                throw new Exception("Can not wait while driving!");

            _waitUntil = time;
        }

        /// <summary>
        /// Determines whether this bot is fixed to a position.
        /// </summary>
        /// <returns>true, if it is fixed</returns>
        public bool hasFixedPosition()
        {
            return StateQueueCount == 0 || !(StateQueuePeek() is BotMove);
        }

        /// <summary>
        /// Determines whether this bot is currently resting.
        /// </summary>
        /// <returns><code>true</code> if the bot is resting, <code>false</code> otherwise.</returns>
        public bool IsResting()
        {
            return StateQueueCount == 0 || StateQueuePeek() is BotRest;
        }

        /// <summary>
        /// Logs the data of an unfinished trip.
        /// </summary>
        internal override void LogIncompleteTrip()
        {
            if (StateQueueCount > 0 && StateQueuePeek() is BotMove)
                (StateQueuePeek() as BotMove).LogUnfinishedTrip(this);
        }

        #endregion

        #region Queueing zone tracking

        /// <summary>
        /// Stores the last trip start time.
        /// </summary>
        private double _queueTripStartTime = double.NaN;
        /// <summary>
        /// Contains all output station queueing areas.
        /// </summary>
        private VolatileIDDictionary<OutputStation, SimpleRectangle> _queueZonesOStations;
        /// <summary>
        /// Contains all input station queueing areas.
        /// </summary>
        private VolatileIDDictionary<InputStation, SimpleRectangle> _queueZonesIStations;
        /// <summary>
        /// Checks whether the bot is currently within the stations queueing area.
        /// </summary>
        /// <param name="station">The station to check.</param>
        /// <returns><code>true</code> if the bot is within the stations queueing area, <code>false</code> otherwise.</returns>
        private bool IsInStationQueueZone(OutputStation station)
        {
            if (_queueZonesOStations == null)
                _queueZonesOStations = new VolatileIDDictionary<OutputStation, SimpleRectangle>(Instance.OutputStations.Select(s =>
                {
                    double lowX = s.Queues != null && s.Queues.Any() && s.Queues.First().Value.Any() ? s.Queues.Min(q => q.Value.Min(w => w.X)) : s.X - 0.5;
                    double highX = s.Queues != null && s.Queues.Any() && s.Queues.First().Value.Any() ? s.Queues.Max(q => q.Value.Max(w => w.X)) : s.X + 0.5;
                    double lowY = s.Queues != null && s.Queues.Any() && s.Queues.First().Value.Any() ? s.Queues.Min(q => q.Value.Min(w => w.Y)) : s.Y - 0.5;
                    double highY = s.Queues != null && s.Queues.Any() && s.Queues.First().Value.Any() ? s.Queues.Max(q => q.Value.Max(w => w.Y)) : s.Y + 0.5;
                    return new VolatileKeyValuePair<OutputStation, SimpleRectangle>(s, new SimpleRectangle(s.Tier, lowX, lowY, highX - lowX, highY - lowY));
                }).ToList());
            return _queueZonesOStations[station].IsContained(Tier, X, Y);
        }
        /// <summary>
        /// Checks whether the bot is currently within the stations queueing area.
        /// </summary>
        /// <param name="station">The station to check.</param>
        /// <returns><code>true</code> if the bot is within the stations queueing area, <code>false</code> otherwise.</returns>
        private bool IsInStationQueueZone(InputStation station)
        {
            if (_queueZonesIStations == null)
                _queueZonesIStations = new VolatileIDDictionary<InputStation, SimpleRectangle>(Instance.InputStations.Select(s =>
                {
                    double lowX = s.Queues != null && s.Queues.Any() && s.Queues.First().Value.Any() ? s.Queues.Min(q => q.Value.Min(w => w.X)) : s.X - 0.5;
                    double highX = s.Queues != null && s.Queues.Any() && s.Queues.First().Value.Any() ? s.Queues.Max(q => q.Value.Max(w => w.X)) : s.X + 0.5;
                    double lowY = s.Queues != null && s.Queues.Any() && s.Queues.First().Value.Any() ? s.Queues.Min(q => q.Value.Min(w => w.Y)) : s.Y - 0.5;
                    double highY = s.Queues != null && s.Queues.Any() && s.Queues.First().Value.Any() ? s.Queues.Max(q => q.Value.Max(w => w.Y)) : s.Y + 0.5;
                    return new VolatileKeyValuePair<InputStation, SimpleRectangle>(s, new SimpleRectangle(s.Tier, lowX, lowY, highX - lowX, highY - lowY));
                }).ToList());
            return _queueZonesIStations[station].IsContained(Tier, X, Y);
        }

        #endregion

        #region IUpdateable Members

        /// <summary>
        /// The next event when this element has to be updated.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns>The next time this element has to be updated.</returns>
        public override double GetNextEventTime(double currentTime)
        {
            // Return soonest event that has not happened yet
            var minUntil = Double.PositiveInfinity;
            if (_waitUntil >= currentTime) minUntil = Math.Min(_waitUntil, minUntil);
            if (_waitUntil + _rotateDuration >= currentTime) minUntil = Math.Min(_waitUntil + _rotateDuration, minUntil);
            if (_waitUntil + _rotateDuration + _driveDuration >= currentTime) minUntil = Math.Min(_waitUntil + _rotateDuration + _driveDuration, minUntil);
            return minUntil;
        }

        /// <summary>
        /// update bot
        /// </summary>
        /// <param name="lastTime">time stamp: last update</param>
        /// <param name="currentTime">time stamp: now</param>
        public override void Update(double lastTime, double currentTime)
        {
            //wait short start time
            if (currentTime < 0.2)
                return;
            //bot is blocked
            if (this._waitUntil >= currentTime)
            {
                // We still want to update the statistics of the bot
                _updateStatistics(currentTime - lastTime, X, Y);
                return;
            }

            var delta = currentTime - lastTime;
            var xOld = X;
            var yOld = Y;

            //get a task
            if (StateQueueCount == 0)
            {
                if (CurrentTask != null)
                    Instance.Controller.BotManager.TaskComplete(this, CurrentTask);
                Instance.Controller.BotManager.RequestNewTask(this);
            }

            //do state dependent action
            if (StateQueueCount > 0)
                StateQueuePeek().Act(this, lastTime, currentTime);

            //bot is blocked
            if (this._waitUntil >= currentTime)
                return;

            // Indicate change
            _changed = true;
            Instance.Changed = true;

            //get target orientation
            _updateDrive(lastTime, currentTime);

            //do state dependent action
            if (StateQueueCount > 0)
                StateQueuePeek().Act(this, lastTime, currentTime);

            //save statistics
            _updateStatistics(delta, xOld, yOld);
        }

        /// <summary>
        /// Drive the bot.
        /// </summary>
        /// <param name="lastTime">The last time.</param>
        /// <param name="currentTime">The current time.</param>
        private void _updateDrive(double lastTime, double currentTime)
        {
            // Is there a rotation still going on?
            if (_waitUntil + _rotateDuration >= currentTime)
            {
                // --> First rotate
                _updateRotation(currentTime);
            }
            else
            {
                // Complete any started rotation
                if (_endOrientation != Orientation)
                {
                    //_rotateDuration = 0;
                    Orientation = _endOrientation;
                    _startOrientation = _endOrientation;
                    if (this.Pod != null && Instance.SettingConfig.RotatePods)
                        this.Pod.Orientation = _endOrientation;
                }

                // --> Then move (if we have a target)
                if (NextWaypoint != null)
                {
                    _updateMove(currentTime);
                }

                //_updatePassedWaypoints();
            }
        }

        /*/// <summary>
        /// _updates the passed way points.
        /// </summary>
        private void _updatePassedWaypoints()
        {
            //delete passed way points in skipped way points
            for (int i = 0; i < skippendWaypoints.Count; i++)
            {
                if (Instance.Controller.PathManager.GetWaypointByNodeId(skippendWaypoints[i].Node).GetSquaredDistance(NextWaypoint) > GetSquaredDistance(NextWaypoint))
                    skippendWaypoints.RemoveAt(i--);
                else
                    break;
            }
        }*/

        /// <summary>
        /// update the rotation to the target orientation
        /// </summary>
        /// <param name="currentTime">time stamp: now</param>
        private void _updateRotation(double currentTime)
        {
            //stop the pod (this should already be 0)
            XVelocity = YVelocity = 0;

            Orientation = Physics.getOrientationAfterTimeStep(_startOrientation, _endOrientation, currentTime - _waitUntil);

            //set the pod orientation
            if (this.Pod != null && Instance.SettingConfig.RotatePods)
                this.Pod.Orientation = Orientation;
        }

        /// <summary>
        /// move the bot towards the next way point
        /// </summary>
        /// <param name="currentTime">time stamp: now</param>
        private void _updateMove(double currentTime)
        {
            //get distance traveled
            double distanceTraveled;
            double speed;
            Physics.getTimeNeededToMove(0, CurrentWaypoint.GetDistance(NextWaypoint));
            Physics.GetDistanceTraveledAfterTimeStep(0, Math.Min(_driveDuration, currentTime - _waitUntil - _rotateDuration), out distanceTraveled, out speed);

            //set speed
            XVelocity = Math.Cos(Orientation) * speed;
            YVelocity = Math.Sin(Orientation) * speed;

            //travel in percentage
            var travelPercentage = distanceTraveled / NextWaypoint.GetDistance(CurrentWaypoint);

            //initiate new positions and reset it during this method
            var xNew = CurrentWaypoint.X * (1 - travelPercentage) + NextWaypoint.X * travelPercentage;
            var yNew = CurrentWaypoint.Y * (1 - travelPercentage) + NextWaypoint.Y * travelPercentage;

            if (currentTime >= _waitUntil + _rotateDuration + _driveDuration)
            {
                //reached goal
                XVelocity = YVelocity = 0;
                xNew = NextWaypoint.X;
                yNew = NextWaypoint.Y;
                CurrentWaypoint = NextWaypoint;
                NextWaypoint = null;
            }

            // Try to make move. If can't ask move due to a collision, then stop
            if (!Instance.Compound.BotCurrentTier[this].MoveBotOverride(this, xNew, yNew))
            {
                // Log the potential collision
                Instance.LogInfo("Potential collision (" + GetIdentfierString() + ") - adding check for crashhandler ...");
                // Mark the bot for collision investigation
                Instance.BotCrashHandler.AddPotentialCrashBot(this);
            }

            // Check whether bot is now in destination's queueing area
            if (!double.IsNaN(_queueTripStartTime))
            {
                // Check whether the destination is an output-station and we reached it
                if (DestinationWaypoint.OutputStation != null)
                    if (IsInStationQueueZone(DestinationWaypoint.OutputStation))
                    {
                        Instance.NotifyTripCompleted(this, Statistics.StationTripDatapoint.StationTripType.O, Instance.Controller.CurrentTime - _queueTripStartTime);
                        _queueTripStartTime = double.NaN;
                    }
                // Check whether the destination is an input-station and we reached it
                if (DestinationWaypoint.InputStation != null)
                    if (IsInStationQueueZone(DestinationWaypoint.InputStation))
                    {
                        Instance.NotifyTripCompleted(this, Statistics.StationTripDatapoint.StationTripType.I, Instance.Controller.CurrentTime - _queueTripStartTime);
                        _queueTripStartTime = double.NaN;
                    }
            }
        }

        /// <summary>
        /// update statistical data
        /// </summary>
        /// <param name="delta">time passed since last update</param>
        /// <param name="xOld">Position x before update</param>
        /// <param name="yOld">Position y before update</param>
        private void _updateStatistics(double delta, double xOld, double yOld)
        {
            // Measure moving time
            if (Moving)
                StatTotalTimeMoving += delta;
            // Measure queueing time
            if (IsQueueing)
                StatTotalTimeQueueing += delta;

            // Set moving flag bot
            if (XVelocity == 0.0 && YVelocity == 0.0)
                this.Moving = false;
            else
                this.Moving = true;

            // Set moving flag pod
            if (this.Pod != null)
                this.Pod.Moving = this.Moving;

            // Count distanceTraveled
            this.StatDistanceTraveled += Math.Sqrt((X - xOld) * (X - xOld) + (Y - yOld) * (Y - yOld));

            // Compute time in previous task
            this.StatTotalTaskTimes[StatLastTask] += delta;
            StatLastTask = CurrentTask != null ? CurrentTask.Type : BotTaskType.None;

            // Measure time spent in state
            StatTotalStateTimes[StatLastState] += delta;
            StatLastState = StateQueueCount > 0 ? StateQueuePeek().Type : BotStateType.Rest;

            // Compute time in previous state
            if (StateQueueCount > 0)
            {
                // TODO maybe add the time spent in the states in another stat dictionary
                //string s = _stateQueue.Peek().ToString();
                //if (this.StatTotalTimes.ContainsKey(s))
                //    this.StatTotalTimes[s] += delta;
                //else
                //    this.StatTotalTimes[s] = delta;
            }
        }

        #endregion

        #region IBotInfo Members

        /// <summary>
        /// x position of the goal for the info panel
        /// </summary>
        /// <returns>x position</returns>
        public override double GetInfoGoalX()
        {
            Waypoint destinationWP = DestinationWaypoint;
            if (destinationWP != null)
                return destinationWP.X;
            else
                return X;
        }
        /// <summary>
        /// y position of the goal for the info panel
        /// </summary>
        /// <returns>y position</returns>
        public override double GetInfoGoalY()
        {
            Waypoint destinationWP = DestinationWaypoint;
            if (destinationWP != null)
                return destinationWP.Y;
            else
                return Y;
        }
        /// <summary>
        /// target for the info panel
        /// </summary>
        /// <returns>orientation</returns>
        public override double GetInfoTargetOrientation() { return GetTargetOrientation(); }
        /// <summary>
        /// The current state the bot is in (for async access).
        /// </summary>
        public string _currentInfoStateName = "";
        /// <summary>
        /// state for the info panel
        /// </summary>
        /// <returns>state</returns>
        public override string GetInfoState() { return _currentInfoStateName; }
        /// <summary>
        /// Gets the current waypoint that is considered by planning.
        /// </summary>
        /// <returns>The current waypoint.</returns>
        public override IWaypointInfo GetInfoCurrentWaypoint() { return CurrentWaypoint; }
        /// <summary>
        /// Destination way point in the info panel
        /// </summary>
        /// <returns>destination</returns>
        public override IWaypointInfo GetInfoDestinationWaypoint() { return NextWaypoint; }
        /// <summary>
        /// Destination way point in the info panel
        /// </summary>
        /// <returns>destination</returns>
        public override IWaypointInfo GetInfoGoalWaypoint() { return DestinationWaypoint; }
        /// <summary>
        /// The current path the bot is following.
        /// </summary>
        private List<IWaypointInfo> _currentPath = new List<IWaypointInfo>();
        /// <summary>
        /// Gets the current path of the bot.
        /// </summary>
        /// <returns>The current path.</returns>
        public override List<IWaypointInfo> GetInfoPath() { return _currentPath; }
        /// <summary>
        /// Indicates whether the robot is currently blocked.
        /// </summary>
        /// <returns><code>true</code> if the robot is blocked, <code>false</code> otherwise.</returns>
        public override bool GetInfoBlocked() { return Instance.Controller.CurrentTime < _waitUntil || Instance.Controller.CurrentTime < BlockedUntil; }
        /// <summary>
        /// The time until the bot is blocked.
        /// </summary>
        /// <returns>The time until the bot is blocked.</returns>
        public override double GetInfoBlockedLeft()
        {
            double currentTime = Instance.Controller.CurrentTime; double waitUntil = _waitUntil; double blockedUntil = BlockedUntil;
            // Return next block release time that lies in the future
            return
                currentTime < waitUntil && currentTime < blockedUntil ? Math.Min(_waitUntil, blockedUntil) - currentTime :
                currentTime < waitUntil ? waitUntil - currentTime :
                currentTime < blockedUntil ? blockedUntil - currentTime :
                double.NaN;
        }
        /// <summary>
        /// Indicates whether the bot is currently queueing in a managed area.
        /// </summary>
        /// <returns><code>true</code> if the robot is within a queue area, <code>false</code> otherwise.</returns>
        public override bool GetInfoIsQueueing() { return IsQueueing; }

        #endregion

        #region States

        #region Move state

        /// <summary>
        /// The state defining the operation of moving.
        /// </summary>
        internal class BotMove : IBotState
        {
            /// <summary>
            /// next node to reach
            /// </summary>
            public Waypoint DestinationWaypoint { get; private set; }

            /// <summary>
            /// constructor
            /// </summary>
            /// <param name="w">way point</param>
            public BotMove(Waypoint w) { DestinationWaypoint = w; }

            /// <summary>
            /// Indicates whether we just entered the state.
            /// </summary>
            private bool _initialized;

            /// <summary>
            /// Logs an unfinished trip.
            /// </summary>
            /// <param name="bot">The bot that is logging the trip.</param>
            internal void LogUnfinishedTrip(BotNormal bot)
            {
                // Manage connectivity statistics
                if (bot.DestinationWaypoint != null)
                    bot.DestinationWaypoint.StatLogUnfinishedTrip(bot);
            }

            /// <summary>
            /// act
            /// </summary>
            /// <param name="self">driver</param>
            /// <param name="lastTime">The time before this update.</param>
            /// <param name="currentTime">The current time.</param>
            public void Act(Bot self, double lastTime, double currentTime)
            {
                var bot = self as BotNormal;

                // If it's the first time executing this, log the start time of the trip
                if (!_initialized)
                {
                    bot.Path = new Path();
                    // Track path statistics
                    self.StatLastTripStartTime = currentTime;
                    self.StatTotalStateCounts[Type]++;
                    self.StatDistanceRequestedOptimal += self.Pod != null ?
                        Distances.CalculateShortestPathPodSafe(bot.CurrentWaypoint, DestinationWaypoint, bot.Instance) :
                        Distances.CalculateShortestPath(bot.CurrentWaypoint, DestinationWaypoint, bot.Instance);
                    // Track last mile statistics
                    if (DestinationWaypoint.OutputStation != null)
                    {
                        if (!bot.IsInStationQueueZone(DestinationWaypoint.OutputStation))
                            // Start the trip now
                            bot._queueTripStartTime = bot.Instance.Controller.CurrentTime;
                        else
                            // Already at the location - no trip to do
                            bot._queueTripStartTime = double.NaN;
                    }
                    else if (DestinationWaypoint.InputStation != null)
                    {
                        if (!bot.IsInStationQueueZone(DestinationWaypoint.InputStation))
                            // Start the trip now
                            bot._queueTripStartTime = bot.Instance.Controller.CurrentTime;
                        else
                            // Already at the location - no trip to do
                            bot._queueTripStartTime = double.NaN;
                    }
                    else
                    {
                        // No station trip - do not track
                        bot._queueTripStartTime = double.NaN;
                    }

                    // Mark initialized
                    _initialized = true;
                }

                //not while driving
                if (bot.GetSpeed() > 0)
                    return;

                //set destination way point
                bot.DestinationWaypoint = DestinationWaypoint;

                //we are at the destination && RealWorldIntegrationEventDriven
                if (bot.CurrentWaypoint == bot.DestinationWaypoint)
                {
                    //#RealWorldIntegration.start
                    if (bot.Instance.SettingConfig.RealWorldIntegrationEventDriven)
                    {
                        lock (bot)
                        {
                            if (!bot._eventReachedNextWaypoint)
                            {
                                // Logging info message
                                bot.Instance.LogInfo("Setting Bot" + bot.ID + " to sleep");

                                bot.BlockedUntil = bot._waitUntil = double.PositiveInfinity;
                                return;
                            }
                        }
                    }
                    //#RealWorldIntegration.end

                    // Manage connectivity statistics
                    if (bot.DestinationWaypoint != null)
                        bot.CurrentWaypoint.StatReachedDestination(bot);

                    // Remove this task
                    bot.NextWaypoint = null;
                    bot.DestinationWaypoint = null;
                    bot.DequeueState(lastTime, currentTime);
                    return;
                }

                // Only mark the move state if we weren't already at the destination waypoint
                bot._lastExteriorState = Type;

                //the bot has already something to do
                if (bot.NextWaypoint != null)
                    return;

                //Has the bot a path?
                if (bot.Path == null || bot.Path.Count == 0)
                {
                    bot.RequestReoptimization = true;
                    return;
                }

                //#RealWorldIntegration.start
                if (bot.Instance.SettingConfig.RealWorldIntegrationEventDriven)
                {
                    lock (bot)
                    {
                        if (bot.Instance.Controller.PathManager.GetWaypointByNodeId(bot.Path.NextAction.Node) == bot.CurrentWaypoint && !bot._eventReachedNextWaypoint)
                        {
                            // Logging info message
                            bot.Instance.LogInfo("Setting Bot" + bot.ID + " to sleep");

                            bot.BlockedUntil = bot._waitUntil = double.PositiveInfinity;
                            return;
                        }
                    }
                }
                //#RealWorldIntegration.end

                //Bot reached the next way point?
                if (bot.Instance.Controller.PathManager.GetWaypointByNodeId(bot.Path.NextAction.Node) != bot.CurrentWaypoint)
                {
                    // --> Not reached yet
                    bool successfulRegistration = bot.setNextWaypoint(bot.Instance.Controller.PathManager.GetWaypointByNodeId(bot.Path.NextAction.Node), currentTime);
                    if (successfulRegistration)
                        bot._eventReachedNextWaypoint = false;
                    else
                        bot._waitUntil = currentTime + 1;
                }
                else
                {
                    // --> Bot reached the next way point
                    // See whether turning to prepare for next move is necessary
                    if (bot.Path.NextAction == bot.Path.LastAction && bot.Path.NextNodeToPrepareFor >= 0)
                    {
                        // Calculate turn times
                        bot._startOrientation = bot.Orientation;
                        Waypoint turnTowards = bot.Instance.Controller.PathManager.GetWaypointByNodeId(bot.Path.NextNodeToPrepareFor);
                        bot._endOrientation = GetOrientation(bot.X, bot.Y, turnTowards.X, turnTowards.Y);
                        double rotateDuration = bot.Physics.getTimeNeededToTurn(bot._startOrientation, bot._endOrientation);
                        // Forget about the node
                        bot.Path.NextNodeToPrepareFor = -1;
                        // See whether we need to turn at all
                        if (rotateDuration > 0)
                        {
                            // Proceed with turn then come back here to get rid of the action
                            bot._waitUntil = Math.Max(bot._waitUntil, currentTime);
                            bot._rotateDuration = rotateDuration;
                            return;
                        }
                    }

                    // Set wait until time
                    if (bot.Path.NextAction.StopAtNode && bot.Path.NextAction.WaitTimeAfterStop > 0)
                        bot._waitUntil = currentTime + bot.Path.NextAction.WaitTimeAfterStop;

                    //pop the node
                    bot.Path.RemoveFirstAction();

                    //skip all non-stopping nodes
                    while (bot.Path.Count > 0 && bot.Path.NextAction.StopAtNode == false)
                        bot.Path.RemoveFirstAction();

                    if (bot.Path == null || bot.Path.Count == 0)
                    {
                        if (bot._waitUntil <= currentTime)
                            bot.RequestReoptimization = true;
                        return;
                    }

                    //set next destination
                    bool successfulRegistration = bot.setNextWaypoint(bot.Instance.Controller.PathManager.GetWaypointByNodeId(bot.Path.NextAction.Node), currentTime);
                    if (successfulRegistration)
                        bot._eventReachedNextWaypoint = false;
                    else
                        bot._waitUntil = currentTime + 1;
                }
            }

            /// <summary>
            /// Notifies the move state, that a collision occurred.
            /// </summary>
            /// <param name="bot">The bot.</param>
            /// <param name="currentTime">The current simulation time.</param>
            internal void NotifyCollision(BotNormal bot, double currentTime)
            {
                if (bot.Path == null)
                    bot.Path = new Path();

                //drive back to passed way point
                bot.setNextWaypoint(bot.CurrentWaypoint, currentTime);

                bot.Path.AddFirst(bot.Instance.Controller.PathManager.GetNodeIdByWaypoint(bot.CurrentWaypoint), true, 0);
            }

            /// <summary>
            /// Returns the current way point this bot wants to drive to
            /// </summary>
            public Waypoint getDestinationWaypoint()
            {
                return DestinationWaypoint;
            }

            /// <summary>
            /// state name
            /// </summary>
            /// <returns>name</returns>
            public override string ToString() { return "Move"; }

            /// <summary>
            /// State type.
            /// </summary>
            public BotStateType Type { get { return BotStateType.Move; } }

        }
        #endregion

        #region Pickup and Set down states

        /// <summary>
        /// The state defining the operation of picking up a pod at the current location.
        /// </summary>
        internal class BotPickupPod : IBotState
        {
            private Pod _pod;
            private Waypoint _waypoint;
            private bool _initialized = false;
            private bool _executed = false;
            public BotPickupPod(Pod b) { _pod = b; _waypoint = _pod.Waypoint; }
            public Waypoint DestinationWaypoint { get { return _waypoint; } }
            public void Act(Bot self, double lastTime, double currentTime)
            {
                var bot = self as BotNormal;

                // Remember the last state we were in
                bot._lastExteriorState = Type;

                // Initialize
                if (!_initialized) { self.StatTotalStateCounts[Type]++; _initialized = true; }

                // Dequeue the state as soon as it is finished
                if (_executed)
                {
                    bot.DequeueState(lastTime, currentTime);
                    return;
                }
                // Act based on whether pod was picked up
                if (bot.PickupPod(_pod, currentTime))
                {
                    _executed = true;
                    bot.WaitUntil(bot.BlockedUntil);
                    bot.Instance.WaypointGraph.PodPickup(_pod);
                    bot.Instance.Controller.BotManager.PodPickedUp(bot, _pod, _waypoint);

                    //#RealWorldIntegraton.Start
                    //Trigger comes from outside => stay blocked
                    if (bot.Instance.SettingConfig.RealWorldIntegrationEventDriven)
                        bot.BlockedUntil = bot._waitUntil = double.PositiveInfinity;
                    //#RealWorldIntegraton.End
                }
                else
                {
                    // Failed to pick up pod
                    bot.StateQueueClear();
                    bot.Instance.Controller.BotManager.TaskAborted(bot, bot.CurrentTask);
                }
            }

            /// <summary>
            /// state name
            /// </summary>
            /// <returns>name</returns>
            public override string ToString() { return "PickupPod"; }

            /// <summary>
            /// State type.
            /// </summary>
            public BotStateType Type { get { return BotStateType.PickupPod; } }
        }

        /// <summary>
        /// The state defining the operation of setting down a pod at the current location.
        /// </summary>
        internal class BotSetdownPod : IBotState
        {
            private Waypoint _waypoint;
            private bool _initialized = false;
            private bool _executed = false;
            public BotSetdownPod(Waypoint w) { _waypoint = w; }
            public Waypoint DestinationWaypoint { get { return _waypoint; } }
            public void Act(Bot self, double lastTime, double currentTime)
            {
                var bot = self as BotNormal;

                // Remember the last state we were in
                bot._lastExteriorState = Type;

                // Initialize
                if (!_initialized) { self.StatTotalStateCounts[Type]++; _initialized = true; }

                // Dequeue the state as soon as it is finished
                if (_executed)
                {
                    bot.DequeueState(lastTime, currentTime);
                    return;
                }

                //remember Pod
                Pod pod = bot.Pod;

                // Act based on whether pod was set down
                if (bot.SetdownPod(currentTime))
                {
                    _executed = true;
                    bot.WaitUntil(bot.BlockedUntil);
                    bot.Instance.WaypointGraph.PodSetdown(pod, _waypoint);
                    bot.Instance.Controller.BotManager.PodSetDown(bot, pod, _waypoint);

                    //#RealWorldIntegraton.Start
                    //Trigger comes from outside => stay blocked
                    if (bot.Instance.SettingConfig.RealWorldIntegrationEventDriven)
                        bot.BlockedUntil = bot._waitUntil = double.PositiveInfinity;
                    //#RealWorldIntegraton.End
                }
                else
                {
                    // Failed to set down pod
                    bot.StateQueueClear();
                    bot.Instance.Controller.BotManager.TaskAborted(bot, bot.CurrentTask);
                }
            }

            /// <summary>
            /// state name
            /// </summary>
            /// <returns>name</returns>
            public override string ToString() { return "SetdownPod"; }

            /// <summary>
            /// State type.
            /// </summary>
            public BotStateType Type { get { return BotStateType.SetdownPod; } }
        }

        #endregion

        #region Get and Put states

        /// <summary>
        /// The state defining the operation of storing an item-bundle in the pod at an input-station.
        /// </summary>
        internal class BotGetItems : IBotState
        {
            private InsertTask _storeTask;
            private Waypoint _waypoint;
            private bool _initialized = false;
            private bool alreadyRequested = false;
            public BotGetItems(InsertTask storeTask) { _storeTask = storeTask; _waypoint = _storeTask.InputStation.Waypoint; }
            public Waypoint DestinationWaypoint { get { return _waypoint; } }
            public void Act(Bot self, double lastTime, double currentTime)
            {
                var bot = self as BotNormal;

                // Initialize
                if (!_initialized) { self.StatTotalStateCounts[Type]++; _initialized = true; }

                //#RealWorldIntegration.start
                if (bot.Instance.SettingConfig.RealWorldIntegrationCommandOutput && bot._lastExteriorState != Type)
                {
                    // Log the pickup command
                    var sb = new StringBuilder();
                    sb.Append("#RealWorldIntegration => Bot ").Append(bot.ID).Append(" Get");
                    bot.Instance.SettingConfig.LogAction(sb.ToString());
                    // Issue the pickup command
                    bot.Instance.RemoteController.RobotSubmitGetItemCommand(bot.ID);
                }
                //#RealWorldIntegration.end

                // If this is the first put action at a station, register - we need to notify it
                if (bot._lastExteriorState != Type)
                    _storeTask.InputStation.RegisterBot(bot);

                // Remember the last state we were in
                bot._lastExteriorState = Type;

                // If it's the first time, request the bundles
                if (!alreadyRequested)
                {
                    _storeTask.InputStation.RequestBundle(bot, _storeTask.Requests.First());
                    alreadyRequested = true;
                }

                if (bot.Pod == null)
                {
                    // Something wrong happened... don't have a pod!
                    bot.Instance.Controller.BotManager.TaskAborted(bot, bot.CurrentTask);
                    bot.DequeueState(lastTime, currentTime);
                    return;
                }

                // See if bundle has been deposited in the pod
                switch (_storeTask.Requests.First().State)
                {
                    case Management.RequestState.Unfinished: /* Ignore */ break;
                    case Management.RequestState.Aborted: // Request was aborted for some reason - give it back to the manager for re-insertion
                        {
                            // Remove the request that was just aborted
                            _storeTask.FirstAborted();
                            // See whether there are more bundles to store
                            if (_storeTask.Requests.Any())
                            {
                                // Store another one
                                alreadyRequested = false;
                            }
                            else
                            {
                                // We are done here
                                bot.DequeueState(lastTime, currentTime);
                                return;
                            }
                        }
                        break;
                    case Management.RequestState.Finished: // Request was finished - we can go on
                        {
                            // Remove the request that was just completed
                            _storeTask.FirstStored();
                            // See whether there are more bundles to store
                            if (_storeTask.Requests.Any())
                            {
                                // Store another one
                                alreadyRequested = false;
                            }
                            else
                            {
                                // We are done here
                                bot.DequeueState(lastTime, currentTime);
                                return;
                            }
                        }
                        break;
                    default: throw new ArgumentException("Unknown request state: " + _storeTask.Requests.First().State);
                }
            }

            /// <summary>
            /// state name
            /// </summary>
            /// <returns>name</returns>
            public override string ToString() { return "GetItems"; }

            /// <summary>
            /// State type.
            /// </summary>
            public BotStateType Type { get { return BotStateType.GetItems; } }
        }

        /// <summary>
        /// The state defining the operation of picking an item from the pod at an output-station.
        /// </summary>
        internal class BotPutItems : IBotState
        {
            ExtractTask _extractTask;
            Waypoint _waypoint;
            private bool _initialized = false;
            bool alreadyRequested = false;
            public BotPutItems(ExtractTask extractTask)
            { _extractTask = extractTask; _waypoint = extractTask.OutputStation.Waypoint; }
            public Waypoint DestinationWaypoint { get { return _waypoint; } }
            public void Act(Bot self, double lastTime, double currentTime)
            {
                var bot = self as BotNormal;

                // Initialize
                if (!_initialized) { self.StatTotalStateCounts[Type]++; _initialized = true; }

                //#RealWorldIntegration.start
                if (bot.Instance.SettingConfig.RealWorldIntegrationCommandOutput && bot._lastExteriorState != Type)
                {
                    // Log the pickup command
                    var sb = new StringBuilder();
                    sb.Append("#RealWorldIntegration => Bot ").Append(bot.ID).Append(" Put");
                    bot.Instance.SettingConfig.LogAction(sb.ToString());
                    // Issue the pickup command
                    bot.Instance.RemoteController.RobotSubmitPutItemCommand(bot.ID);
                }
                //#RealWorldIntegration.end

                // If this is the first put action at a station, register - we need to notify it
                if (bot._lastExteriorState != Type)
                    _extractTask.OutputStation.RegisterBot(bot);

                // Remember the last state we were in
                bot._lastExteriorState = Type;

                // If it's the first time, request the items be taken
                if (!alreadyRequested)
                {
                    _extractTask.OutputStation.RequestItemTake(bot, _extractTask.Requests.First());
                    alreadyRequested = true;
                }

                if (bot.Pod == null)
                {
                    // Something wrong happened... don't have a pod!
                    bot.Instance.Controller.BotManager.TaskAborted(bot, bot.CurrentTask);
                    bot.StateQueueClear();
                    return;
                }

                // See if item has been picked from the pod
                switch (_extractTask.Requests.First().State)
                {
                    case Management.RequestState.Unfinished: /* Ignore */ break;
                    case Management.RequestState.Aborted: // Request was aborted for some reason - give it back to the manager for re-insertion
                        {
                            // Remove the request that was just aborted
                            _extractTask.FirstAborted();
                            // See whether there are more items to pick
                            if (_extractTask.Requests.Any())
                            {
                                // Pick another one
                                alreadyRequested = false;
                            }
                            else
                            {
                                // We are done here
                                bot.DequeueState(lastTime, currentTime);
                                return;
                            }
                        }
                        break;
                    case Management.RequestState.Finished: // Request was finished - we can go on
                        {
                            // Remove the request that was just completed
                            _extractTask.FirstPicked();
                            // See whether there are more items to pick
                            if (_extractTask.Requests.Any())
                            {
                                // Pick another one
                                alreadyRequested = false;
                            }
                            else
                            {
                                // We are done here
                                bot.DequeueState(lastTime, currentTime);
                                return;
                            }
                        }
                        break;
                    default: throw new ArgumentException("Unknown request state: " + _extractTask.Requests.First().State);
                }
            }

            /// <summary>
            /// state name
            /// </summary>
            /// <returns>name</returns>
            public override string ToString() { return "PutItems"; }

            /// <summary>
            /// State type.
            /// </summary>
            public BotStateType Type { get { return BotStateType.PutItems; } }
        }

        #endregion

        #region Use elevator state

        /// <summary>
        /// State: Bot uses an elevator to get to a different tier
        /// </summary>
        internal class UseElevator : IBotState
        {
            private Elevator _elevator;
            private Waypoint _waypointFrom;
            private Waypoint _waypointTo;
            private bool _initialized = false;
            private bool inUse;
            private double travelUntil;
            public Waypoint DestinationWaypoint { get { return _waypointTo; } }
            public UseElevator(Elevator elevator, Waypoint waypointFrom, Waypoint waypointTo) { _elevator = elevator; _waypointFrom = waypointFrom; _waypointTo = waypointTo; inUse = false; }
            public void Act(Bot self, double lastTime, double currentTime)
            {
                var bot = self as BotNormal;

                // Initialize
                if (!_initialized) { self.StatTotalStateCounts[Type]++; _initialized = true; }

                // Remember the last state we were in
                bot._lastExteriorState = Type;

                // Check if i already using the elevator
                if (!inUse)
                {
                    //consistency
                    if (!_elevator.ConnectedPoints.Contains(_waypointFrom) || !_elevator.ConnectedPoints.Contains(_waypointTo))
                        throw new NotSupportedException("Way point is not managed by Elevator!");

                    inUse = true;
                    travelUntil = currentTime + _elevator.GetTiming(_waypointFrom, _waypointTo);
                    bot._waitUntil = travelUntil;
                }


                if (currentTime >= travelUntil)
                {
                    //do the transportation
                    _elevator.Transport(bot, _waypointFrom, _waypointTo);
                    bot.CurrentWaypoint = _waypointTo;
                    bot.DequeueState(lastTime, currentTime);
                    return;
                }

            }

            /// <summary>
            /// state name
            /// </summary>
            /// <returns>name</returns>
            public override string ToString() { return "UseElevator"; }

            /// <summary>
            /// State type.
            /// </summary>
            public BotStateType Type { get { return BotStateType.UseElevator; } }
        }
        #endregion

        #region Rest state

        internal class BotRest : IBotState
        {
            // TODO make rest time randomized and parameterized
            public const double DEFAULT_REST_TIME = 5;

            private Waypoint _waypoint;
            private double _timeSpan;
            private bool _initialized = false;
            private bool alreadyRested = false;
            public BotRest(Waypoint waypoint, double timeSpan) { _waypoint = waypoint; _timeSpan = timeSpan; }
            public Waypoint DestinationWaypoint { get { return _waypoint; } }

            public void Act(Bot self, double lastTime, double currentTime)
            {
                var bot = self as BotNormal;

                // Initialize
                if (!_initialized) { self.StatTotalStateCounts[Type]++; _initialized = true; }

                //#RealWorldIntegration.start
                if (bot.Instance.SettingConfig.RealWorldIntegrationCommandOutput && bot._lastExteriorState != Type)
                {
                    // Log the pickup command
                    var sb = new StringBuilder();
                    sb.Append("#RealWorldIntegration => Bot ").Append(bot.ID).Append(" Rest");
                    bot.Instance.SettingConfig.LogAction(sb.ToString());
                    // Issue the pickup command
                    bot.Instance.RemoteController.RobotSubmitRestCommand(bot.ID);
                }
                //#RealWorldIntegration.end

                // Remember the last state we were in
                bot._lastExteriorState = Type;

                // Randomly rest or exit resting
                if (!alreadyRested)
                {
                    // Rest for a predefined period
                    bot.BlockedUntil = currentTime + _timeSpan;
                    bot.WaitUntil(bot.BlockedUntil);
                    alreadyRested = true;
                    return;
                }
                else
                {
                    // exit the resting
                    bot.DequeueState(lastTime, currentTime);
                }
            }

            /// <summary>
            /// state name
            /// </summary>
            /// <returns>name</returns>
            public override string ToString() { return "Rest"; }

            /// <summary>
            /// State type.
            /// </summary>
            public BotStateType Type { get { return BotStateType.Rest; } }

        }
        #endregion

        #endregion

        #region Events
        /// <summary>
        /// Called when [bot reached way point].
        /// </summary>
        /// <param name="waypoint">The way point.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void OnReachedWaypoint(Waypoint waypoint)
        {
            //not necessary 
            if (!Instance.SettingConfig.RealWorldIntegrationEventDriven)
                return;

            // Logging info message
            Instance.LogInfo("Bot" + this.ID + " is at: " + waypoint.ID);

            //we are not interested in intermediate points
            if (waypoint.ID == _nextWaypointID || !_initialEventReceived)
                lock (this)
                {
                    _nextWaypointID = -1;
                    _eventReachedNextWaypoint = true; _initialEventReceived = true;
                    XVelocity = YVelocity = BlockedUntil = _waitUntil = _rotateDuration = _driveDuration = 0;
                }
        }

        /// <summary>
        /// Called when [bot picked up the pod].
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void OnPickedUpPod()
        {
            //not necessary 
            if (!Instance.SettingConfig.RealWorldIntegrationEventDriven)
                return;

            //stop blocking
            BlockedUntil = _waitUntil = 0;
        }

        /// <summary>
        /// Called when [bot set down pod].
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void OnSetDownPod()
        {
            //not necessary 
            if (!Instance.SettingConfig.RealWorldIntegrationEventDriven)
                return;

            //stop blocking
            BlockedUntil = _waitUntil = 0;
        }
        #endregion
    }

}
