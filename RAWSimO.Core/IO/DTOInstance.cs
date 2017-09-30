using RAWSimO.Core.Management;
using RAWSimO.Core.Randomization;
using RAWSimO.Core.Control;
using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RAWSimO.Core.IO
{
    /// <summary>
    /// A simplified representation of the original object used for serialization.
    /// </summary>
    [XmlRootAttribute("Instance")]
    public class DTOInstance : IDataTransferObject<Instance, DTOInstance>
    {
        /// <summary>
        /// The name of this instance.
        /// </summary>
        [XmlAttribute]
        public string Name;
        /// <summary>
        /// All robots of this instance.
        /// </summary>
        [XmlArrayItem("Bot")]
        public List<DTOBot> Bots;
        /// <summary>
        /// All pods of this instance.
        /// </summary>
        [XmlArrayItem("Pod")]
        public List<DTOPod> Pods;
        /// <summary>
        /// All elevators of this instance.
        /// </summary>
        [XmlArrayItem("Elevator")]
        public List<DTOElevator> Elevators;
        /// <summary>
        /// All input-stations of this instance.
        /// </summary>
        [XmlArrayItem("InputStation")]
        public List<DTOInputStation> InputStations;
        /// <summary>
        /// All output-stations of this instance.
        /// </summary>
        [XmlArrayItem("OutputStation")]
        public List<DTOOutputStation> OutputStations;
        /// <summary>
        /// All tiers of this instance.
        /// </summary>
        [XmlArrayItem("Tier")]
        public List<DTOTier> Tiers;
        /// <summary>
        /// All waypoints of this instance.
        /// </summary>
        [XmlArrayItem("Waypoint")]
        public List<DTOWaypoint> Waypoints;
        /// <summary>
        /// All semaphores of this instance.
        /// </summary>
        [XmlArrayItem("Semaphore")]
        public List<DTOQueueSemaphore> Semaphores;
        /// <summary>
        /// Creates a DTO representation of the original object.
        /// </summary>
        /// <param name="value">The original object.</param>
        public static implicit operator DTOInstance(Instance value)
        {
            if (value == null)
                return null;

            DTOInstance dtoInstance = new DTOInstance { Name = value.Name };
            dtoInstance.Bots = new List<DTOBot>();
            foreach (var bot in value.Bots)
                dtoInstance.Bots.Add(bot);
            dtoInstance.Pods = new List<DTOPod>();
            foreach (var pod in value.Pods)
                dtoInstance.Pods.Add(pod);
            dtoInstance.Elevators = new List<DTOElevator>();
            foreach (var elevator in value.Elevators)
                dtoInstance.Elevators.Add(elevator);
            dtoInstance.InputStations = new List<DTOInputStation>();
            foreach (var iStation in value.InputStations)
                dtoInstance.InputStations.Add(iStation);
            dtoInstance.OutputStations = new List<DTOOutputStation>();
            foreach (var oStation in value.OutputStations)
                dtoInstance.OutputStations.Add(oStation);
            dtoInstance.Tiers = new List<DTOTier>();
            if (value.Compound != null)
            {
                foreach (var tier in value.Compound.Tiers)
                    dtoInstance.Tiers.Add(tier);
            }
            dtoInstance.Waypoints = new List<DTOWaypoint>();
            foreach (var wp in value.Waypoints)
                dtoInstance.Waypoints.Add(wp);
            dtoInstance.Semaphores = new List<DTOQueueSemaphore>();
            foreach (var semaphore in value.Semaphores)
                dtoInstance.Semaphores.Add(semaphore);
            return dtoInstance;
        }

        #region IDataTransferObject<Instance,DTOInstance> Members

        /// <summary>
        /// Creates a clone out of the original.
        /// </summary>
        /// <param name="original">The original object.</param>
        /// <returns>The cloned object.</returns>
        public DTOInstance FromOrig(Instance original) { return original; }
        /// <summary>
        /// Submits all this object to the instance.
        /// </summary>
        /// <param name="instance">The instance to submit to.</param>
        /// <returns>The original object created at the instance.</returns>
        public Instance Submit(Instance instance)
        {
            // Check configuration - config has to be present at this point
            if (instance.SettingConfig == null)
                throw new InvalidOperationException("Cannot submit values to instance without a configuration present in the object!");
            // Set name
            instance.Name = Name;
            // --> HANDLE OVERRIDES
            // If an override is specified, remove the specified amount of input stations
            if (instance.SettingConfig.OverrideConfig != null && instance.SettingConfig.OverrideConfig.OverrideInputStationCount)
            {
                // Determine stations to remove, but at least keep one
                int stationsToKeep = Math.Max(1, (int)Math.Floor(instance.SettingConfig.OverrideConfig.OverrideInputStationCountValue * InputStations.Count));
                IEnumerable<DTOInputStation> stationsToIgnore = InputStations.OrderBy(s => s.ActivationOrderID).Skip(stationsToKeep).ToList();
                foreach (var station in stationsToIgnore)
                {
                    // Remove the station before it gets submitted
                    InputStations.Remove(station);
                    // Change the station reference at the corresponding waypoint
                    Waypoints.Single(w => w.InputStation == station.ID).InputStation = -1;
                }
            }
            // If an override is specified, remove the specified amount of output stations
            if (instance.SettingConfig.OverrideConfig != null && instance.SettingConfig.OverrideConfig.OverrideOutputStationCount)
            {
                // Determine stations to remove, but at least keep one
                int stationsToKeep = Math.Max(1, (int)Math.Floor(instance.SettingConfig.OverrideConfig.OverrideOutputStationCountValue * OutputStations.Count));
                IEnumerable<DTOOutputStation> stationsToIgnore = OutputStations.OrderBy(s => s.ActivationOrderID).Skip(stationsToKeep).ToList();
                foreach (var station in stationsToIgnore)
                {
                    // Remove the station before it gets submitted
                    OutputStations.Remove(station);
                    // Change the station reference at the corresponding waypoint
                    Waypoints.Single(w => w.OutputStation == station.ID).OutputStation = -1;
                }
            }
            // Check whether there is an override for the bot count
            if (instance.SettingConfig.OverrideConfig != null && instance.SettingConfig.OverrideConfig.OverrideBotCountPerOStation)
            {
                // Get some randomizer
                Random rand = new Random(0);
                // Generate a number of bots depending on the override value by cloning the first given bot
                DTOBot referenceBot = Bots.First();
                Bots.Clear();
                HashSet<DTOWaypoint> _usedBotPositions = new HashSet<DTOWaypoint>();
                for (int i = 1; i <= instance.SettingConfig.OverrideConfig.OverrideBotCountPerOStationValue * OutputStations.Count; i++)
                {
                    // Choose random waypoint to set bot position to
                    DTOWaypoint botPosition = Waypoints.Where(w => !w.IsQueueWaypoint && !w.PodStorageLocation && !_usedBotPositions.Contains(w)).OrderBy(w => rand.NextDouble()).First();
                    _usedBotPositions.Add(botPosition);
                    // Create bot by copying the characteristics of the first given bot
                    Bots.Add(new DTOBot()
                    {
                        ID = i,
                        Tier = botPosition.Tier,
                        X = botPosition.X,
                        Y = botPosition.Y,
                        Radius = referenceBot.Radius,
                        Orientation = referenceBot.Orientation,
                        PodTransferTime = referenceBot.PodTransferTime,
                        MaxAcceleration = referenceBot.MaxAcceleration,
                        MaxDeceleration = referenceBot.MaxDeceleration,
                        MaxVelocity = referenceBot.MaxVelocity,
                        TurnSpeed = referenceBot.TurnSpeed,
                        CollisionPenaltyTime = referenceBot.CollisionPenaltyTime
                    });
                }
            }
            // First submit the tiers to build the basic structure
            foreach (var tier in Tiers)
                tier.Submit(instance);
            // --> Now submit the basics - IDs of the tiers should match
            // --> BOTS
            foreach (var bot in Bots)
                bot.Submit(instance);
            // --> PODS
            foreach (var pod in Pods)
                pod.Submit(instance);
            // --> INPUT STATIONS
            foreach (var iStation in InputStations)
                iStation.Submit(instance);
            // --> OUTPUT STATIONS
            foreach (var oStation in OutputStations)
                oStation.Submit(instance);
            // --> ELEVATORS
            foreach (var elevator in Elevators)
                elevator.Submit(instance);
            // --> WAYPOINTS
            foreach (var waypoint in Waypoints)
                waypoint.Submit(instance);
            // --> SEMAPHORES
            foreach (var semaphore in Semaphores)
                semaphore.Submit(instance);
            // Connect the waypoints
            DTOWaypoint.SetConnections(instance, Waypoints);
            // Connect the elevators
            DTOElevator.SetConnections(instance, Elevators);
            // Flush it
            foreach (var iStation in InputStations)
                iStation.Flush(instance);
            foreach (var oStation in OutputStations)
                oStation.Flush(instance);
            foreach (var elevator in Elevators)
                elevator.Flush(instance);

            instance.Flush();
            // Do not add managers if only a visualization is required
            if (!instance.SettingConfig.VisualizationOnly)
            {
                // Add managers
                instance.Randomizer = new RandomizerSimple(instance.SettingConfig.Seed);
                instance.Controller = new Controller(instance);
                instance.ResourceManager = new ResourceManager(instance);
                instance.ItemManager = new ItemManager(instance);
            }
            // Notify instance about completed initializiation (time to initialize all stuff that relies on all managers being in place)
            instance.LateInitialize();
            // Return it
            return instance;
        }

        #endregion
    }
}
