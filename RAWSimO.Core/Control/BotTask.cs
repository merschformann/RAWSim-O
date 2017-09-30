using RAWSimO.Core.Elements;
using RAWSimO.Core.Items;
using RAWSimO.Core.Management;
using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control
{
    /// <summary>
    /// Defines the task a robot can execute.
    /// </summary>
    public abstract class BotTask
    {
        /// <summary>
        /// Creates a new task.
        /// </summary>
        /// <param name="instance">The instance in which the task is executed.</param>
        /// <param name="bot">The bot that shall execute the task.</param>
        public BotTask(Instance instance, Bot bot) { Instance = instance; Bot = bot; }
        /// <summary>
        /// The instance this task belongs to.
        /// </summary>
        public Instance Instance { get; private set; }
        /// <summary>
        /// The type of the task.
        /// </summary>
        public abstract BotTaskType Type { get; }
        /// <summary>
        /// The bot that executes the task.
        /// </summary>
        public Bot Bot { get; private set; }
        /// <summary>
        /// Prepares everything for executing the task (claiming resources and similar).
        /// </summary>
        public abstract void Prepare();
        /// <summary>
        /// Cleans up a cancelled task.
        /// </summary>
        public abstract void Cancel();
        /// <summary>
        /// Cleans up after a task was successfully executed.
        /// </summary>
        public abstract void Finish();
    }

    /// <summary>
    /// This class represents a park pod task.
    /// </summary>
    public class ParkPodTask : BotTask
    {
        /// <summary>
        /// Creates a new task.
        /// </summary>
        /// <param name="instance">The instance this task belongs to.</param>
        /// <param name="bot">The bot that shall execute the task.</param>
        /// <param name="pod">The pod that the robot shall park.</param>
        /// <param name="storageLocation">The location at which the pod shall be parked.</param>
        public ParkPodTask(Instance instance, Bot bot, Pod pod, Waypoint storageLocation)
            : base(instance, bot)
        {
            Pod = pod;
            StorageLocation = storageLocation;
        }
        /// <summary>
        /// The type of the task.
        /// </summary>
        public override BotTaskType Type { get { return BotTaskType.ParkPod; } }
        /// <summary>
        /// The storage location to use for the pod.
        /// </summary>
        public Waypoint StorageLocation { get; private set; }
        /// <summary>
        /// The pod to store.
        /// </summary>
        public Pod Pod { get; private set; }
        /// <summary>
        /// Prepares everything for executing the task (claiming resources and similar).
        /// </summary>
        public override void Prepare()
        {
            Instance.ResourceManager.ClaimPod(Pod, Bot, BotTaskType.ParkPod);
            Instance.ResourceManager.ClaimStorageLocation(StorageLocation);
        }
        /// <summary>
        /// Cleans up after a task was successfully executed.
        /// </summary>
        public override void Finish()
        {
        }
        /// <summary>
        /// Cleans up a cancelled task.
        /// </summary>
        public override void Cancel()
        {
            Instance.ResourceManager.ReleaseStorageLocation(StorageLocation);
        }
    }
    /// <summary>
    /// This class represents a park pod task.
    /// </summary>
    public class RepositionPodTask : BotTask
    {
        /// <summary>
        /// Creates a new task.
        /// </summary>
        /// <param name="instance">The instance this task belongs to.</param>
        /// <param name="bot">The bot that shall execute the task.</param>
        /// <param name="pod">The pod that the robot shall park.</param>
        /// <param name="storageLocation">The location to bring the pod to.</param>
        public RepositionPodTask(Instance instance, Bot bot, Pod pod, Waypoint storageLocation)
            : base(instance, bot)
        {
            Pod = pod;
            StorageLocation = storageLocation;
        }
        /// <summary>
        /// The type of the task.
        /// </summary>
        public override BotTaskType Type { get { return BotTaskType.RepositionPod; } }
        /// <summary>
        /// The location to bring the pod to.
        /// </summary>
        public Waypoint StorageLocation { get; private set; }
        /// <summary>
        /// The pod to store.
        /// </summary>
        public Pod Pod { get; private set; }
        /// <summary>
        /// Prepares everything for executing the task (claiming resources and similar).
        /// </summary>
        public override void Prepare()
        {
            Instance.ResourceManager.ClaimPod(Pod, Bot, BotTaskType.RepositionPod);
            Instance.ResourceManager.ClaimStorageLocation(StorageLocation);
        }
        /// <summary>
        /// Cleans up after a task was successfully executed.
        /// </summary>
        public override void Finish()
        {
        }
        /// <summary>
        /// Cleans up a cancelled task.
        /// </summary>
        public override void Cancel()
        {
            Instance.ResourceManager.ReleaseStorageLocation(StorageLocation);
        }
    }
    /// <summary>
    /// Defines an extraction task.
    /// </summary>
    public class ExtractTask : BotTask
    {
        /// <summary>
        /// Creates a new task.
        /// </summary>
        /// <param name="instance">The instance this task belongs to.</param>
        /// <param name="bot">The robot that shall execute the task.</param>
        /// <param name="reservedPod">The pod to use for executing the task.</param>
        /// <param name="outputStation">The output station to bring the pod to.</param>
        /// <param name="requests">The requests to handle with this task.</param>
        public ExtractTask(Instance instance, Bot bot, Pod reservedPod, OutputStation outputStation, List<ExtractRequest> requests)
            : base(instance, bot)
        {
            ReservedPod = reservedPod;
            OutputStation = outputStation;
            Requests = requests;
            foreach (var request in requests)
                request.StatInjected = false;
        }
        /// <summary>
        /// The type of the task.
        /// </summary>
        public override BotTaskType Type { get { return BotTaskType.Extract; } }
        /// <summary>
        /// The output station to bring the pod to.
        /// </summary>
        public OutputStation OutputStation { get; private set; }
        /// <summary>
        /// The requests to finish by executing this task.
        /// </summary>
        public List<ExtractRequest> Requests { get; private set; }
        /// <summary>
        /// The pod to use for this task.
        /// </summary>
        public Pod ReservedPod { get; private set; }
        /// <summary>
        /// Marks the first request handled.
        /// </summary>
        public void FirstPicked() { Requests[0].Finish(); Requests.RemoveAt(0); }
        /// <summary>
        /// Marks the first request aborted and re-inserts it into the pool of available requests.
        /// </summary>
        public void FirstAborted() { Requests[0].Abort(); Instance.ResourceManager.ReInsertExtractRequest(Requests[0]); Requests.RemoveAt(0); }
        /// <summary>
        /// Adds another request to this task on-the-fly.
        /// </summary>
        /// <param name="request">The request that shall also be completed by this task.</param>
        public void AddRequest(ExtractRequest request)
        {
            Instance.ResourceManager.RemoveExtractRequest(request);
            if (!ReservedPod.IsContained(request.Item))
                throw new InvalidOperationException("Cannot add a request for an item that is not available!");
            ReservedPod.RegisterItem(request.Item, request);
            Requests.Add(request);
            request.StatInjected = true;
        }
        /// <summary>
        /// Prepares everything for executing the task (claiming resources and similar).
        /// </summary>
        public override void Prepare()
        {
            Instance.ResourceManager.ClaimPod(ReservedPod, Bot, BotTaskType.Extract);
            OutputStation.RegisterInboundPod(ReservedPod);
            OutputStation.RegisterExtractTask(this);
            for (int i = 0; i < Requests.Count; i++)
            {
                Instance.ResourceManager.RemoveExtractRequest(Requests[i]);
                ReservedPod.RegisterItem(Requests[i].Item, Requests[i]);
            }
        }
        /// <summary>
        /// Cleans up a cancelled task.
        /// </summary>
        public override void Cancel()
        {
            if (Bot.Pod == null)
                Instance.ResourceManager.ReleasePod(ReservedPod);
            OutputStation.UnregisterInboundPod(ReservedPod);
            OutputStation.UnregisterExtractTask(this);
            for (int i = 0; i < Requests.Count; i++)
            {
                Instance.ResourceManager.ReInsertExtractRequest(Requests[i]);
                ReservedPod.UnregisterItem(Requests[i].Item, Requests[i]);
            }
        }
        /// <summary>
        /// Cleans up after a task was successfully executed.
        /// </summary>
        public override void Finish()
        {
            if (Requests.Any())
                throw new InvalidOperationException("An unfinished request cannot be marked as finished!");
            OutputStation.UnregisterInboundPod(ReservedPod);
            OutputStation.UnregisterExtractTask(this);
        }
    }
    /// <summary>
    /// Defines a store task.
    /// </summary>
    public class InsertTask : BotTask
    {
        /// <summary>
        /// Creates a new task.
        /// </summary>
        /// <param name="instance">The instance the task belongs to.</param>
        /// <param name="bot">The bot that shall carry out the task.</param>
        /// <param name="reservedPod">The pod to use for the task.</param>
        /// <param name="inputStation">The input station to bring the pod to.</param>
        /// <param name="requests">The requests that shall be finished after successful execution of the task.</param>
        public InsertTask(Instance instance, Bot bot, Pod reservedPod, InputStation inputStation, List<InsertRequest> requests)
            : base(instance, bot)
        {
            InputStation = inputStation;
            Requests = requests;
            ReservedPod = reservedPod;
            foreach (var request in requests)
                request.StatInjected = false;
        }
        /// <summary>
        /// The type of the task.
        /// </summary>
        public override BotTaskType Type { get { return BotTaskType.Insert; } }
        /// <summary>
        /// The input station at which the task is carried out.
        /// </summary>
        public InputStation InputStation { get; private set; }
        /// <summary>
        /// The requests to finish by executing this task.
        /// </summary>
        public List<InsertRequest> Requests { get; private set; }
        /// <summary>
        /// The pod used for storing the bundles.
        /// </summary>
        public Pod ReservedPod { get; private set; }
        /// <summary>
        /// Marks the first request handled.
        /// </summary>
        public void FirstStored() { Requests[0].Finish(); Requests.RemoveAt(0); }
        /// <summary>
        /// Marks the first request aborted and re-inserts it into the pool of available requests.
        /// </summary>
        public void FirstAborted() { Requests[0].Abort(); Instance.ResourceManager.ReInsertStoreRequest(Requests[0]); Requests.RemoveAt(0); }
        /// <summary>
        /// Adds another request to this task on-the-fly.
        /// </summary>
        /// <param name="request">The request that shall also be completed by this task.</param>
        public void AddRequest(InsertRequest request)
        {
            Instance.ResourceManager.RemoveStoreRequest(request);
            if (!ReservedPod.Fits(request.Bundle))
                throw new InvalidOperationException("Cannot add a request for a bundle not fitting the pod!");
            Requests.Add(request);
            request.StatInjected = true;
        }
        /// <summary>
        /// Prepares everything for executing the task (claiming resources and similar).
        /// </summary>
        public override void Prepare()
        {
            Instance.ResourceManager.ClaimPod(ReservedPod, Bot, BotTaskType.Insert);
            for (int i = 0; i < Requests.Count; i++)
                Instance.ResourceManager.RemoveStoreRequest(Requests[i]);
        }
        /// <summary>
        /// Cleans up a cancelled task.
        /// </summary>
        public override void Cancel()
        {
            if (Bot.Pod == null)
                Instance.ResourceManager.ReleasePod(ReservedPod);
            for (int i = 0; i < Requests.Count; i++)
                Instance.ResourceManager.ReInsertStoreRequest(Requests[i]);
        }
        /// <summary>
        /// Cleans up after a task was successfully executed.
        /// </summary>
        public override void Finish() { }
    }
    /// <summary>
    /// Defines a rest task.
    /// </summary>
    public class RestTask : BotTask
    {
        /// <summary>
        /// Creates a new task.
        /// </summary>
        /// <param name="instance">The instance this task belongs to.</param>
        /// <param name="bot">The bot that shall rest.</param>
        /// <param name="restingLocation">The location at which the robot shall rest.</param>
        public RestTask(Instance instance, Bot bot, Waypoint restingLocation)
            : base(instance, bot)
        {
            RestingLocation = restingLocation;
        }
        /// <summary>
        /// The type of the task.
        /// </summary>
        public override BotTaskType Type { get { return BotTaskType.Rest; } }
        /// <summary>
        /// The location at which the robot shall rest.
        /// </summary>
        public Waypoint RestingLocation { get; private set; }
        /// <summary>
        /// Prepares everything for executing the task (claiming resources and similar).
        /// </summary>
        public override void Prepare()
        {
            // TODO check for resting location?
            //if (RestingLocation.PodStorageLocation)
            Instance.ResourceManager.ClaimRestingLocation(RestingLocation);
        }
        /// <summary>
        /// Cleans up after a task was successfully executed.
        /// </summary>
        public override void Finish()
        {
            // TODO check for resting location?
            //if (RestingLocation.PodStorageLocation)
            Instance.ResourceManager.ReleaseRestingLocation(RestingLocation);
        }
        /// <summary>
        /// Cleans up a cancelled task.
        /// </summary>
        public override void Cancel()
        {
            // TODO check for resting location?
            //if (RestingLocation.PodStorageLocation)
            Instance.ResourceManager.ReleaseRestingLocation(RestingLocation);
        }
    }
    /// <summary>
    /// Defines a dummy task that basically does nothing.
    /// </summary>
    public class DummyTask : BotTask
    {
        /// <summary>
        /// Creates a new placeholder task.
        /// </summary>
        /// <param name="instance">The instance the task belongs to.</param>
        /// <param name="bot">The bot for which the placeholder task shall be generated.</param>
        public DummyTask(Instance instance, Bot bot) : base(instance, bot) { }
        /// <summary>
        /// The type of the task.
        /// </summary>
        public override BotTaskType Type { get { return BotTaskType.None; } }
        /// <summary>
        /// Prepares everything for executing the task (claiming resources and similar).
        /// </summary>
        public override void Prepare() { /* Nothing to do */ }
        /// <summary>
        /// Cleans up a cancelled task.
        /// </summary>
        public override void Cancel() { /* Nothing to do */ }
        /// <summary>
        /// Cleans up after a task was successfully executed.
        /// </summary>
        public override void Finish() { /* Nothing to do */ }
    }
}
