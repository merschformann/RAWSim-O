using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAWSimO.Core.Configurations;
using RAWSimO.Core.Bots;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.Items;
using RAWSimO.Core.Management;
using RAWSimO.Core.Randomization;
using RAWSimO.Core.Control;
using RAWSimO.Core.Waypoints;
using System.IO;
using RAWSimO.Core.IO;

namespace RAWSimO.Core.Generator
{
    class LayoutGenerator
    {

        #region member variables

        private SettingConfiguration baseConfiguration;
        private IRandomizer rand;
        private Dictionary<Tuple<double, double>, Elevator> elevatorPositions;
        private Dictionary<Elevator, List<Waypoint>> elevatorWaypoints;
        private Dictionary<Waypoint, QueueSemaphore> elevatorSemaphores;
        private double orientationPodDefault = 0;
        private Instance instance;
        private LayoutConfiguration lc;
        private bool _logInfo = true;
        private Action<string> _logAction;

        #endregion

        #region helper methods

        /// <summary>
        /// Helper method projecting boolean direction markers into a direction type.
        /// </summary>
        /// <param name="east">Indicates whether a east direction is desired.</param>
        /// <param name="west">Indicates whether a west direction is desired.</param>
        /// <param name="north">Indicates whether a north direction is desired.</param>
        /// <param name="south">Indicates whether a south direction is desired.</param>
        /// <returns>The direction.</returns>
        internal static directions GetDirectionType(bool east, bool west, bool south, bool north)
        {
            if (east)
            {
                // EAST
                if (west)
                {
                    // WEST
                    if (north)
                    {
                        // NORTH
                        if (south)
                            // SOUTH
                            return directions.EastNorthSouthWest;
                        else
                            // NO SOUTH
                            return directions.EastNorthWest;
                    }
                    else
                    {
                        // NO NORTH
                        if (south)
                            // SOUTH
                            return directions.EastSouthWest;
                        else
                            // NO SOUTH
                            return directions.EastWest;
                    }
                }
                else
                {
                    // NO WEST
                    if (north)
                    {
                        // NORTH
                        if (south)
                            // SOUTH
                            return directions.EastNorthSouth;
                        else
                            // NO SOUTH
                            return directions.EastNorth;
                    }
                    else
                    {
                        // NO NORTH
                        if (south)
                            // SOUTH
                            return directions.EastSouth;
                        else
                            // NO SOUTH
                            return directions.East;
                    }
                }
            }
            else
            {
                // NO EAST
                if (west)
                {
                    // WEST
                    if (north)
                    {
                        // NORTH
                        if (south)
                            // SOUTH
                            return directions.NorthSouthWest;
                        else
                            // NO SOUTH
                            return directions.NorthWest;
                    }
                    else
                    {
                        // NO NORTH
                        if (south)
                            // SOUTH
                            return directions.SouthWest;
                        else
                            // NO SOUTH
                            return directions.West;
                    }
                }
                else
                {
                    // NO WEST
                    if (north)
                    {
                        // NORTH
                        if (south)
                            // SOUTH
                            return directions.NorthSouth;
                        else
                            // NO SOUTH
                            return directions.North;
                    }
                    else
                    {
                        // NO NORTH
                        if (south)
                            // SOUTH
                            return directions.South;
                        else
                            // NO SOUTH
                            return directions.Invalid;
                    }
                }
            }
        }

        /// <summary>
        /// Prints the layout given by the 2D array to the console.
        /// </summary>
        /// <param name="tiles">The layout to print.</param>
        /// <param name="showCols">Shows the column indices instead of the type.</param>
        /// <param name="showRows">Shows the row indices instead of the type.</param>
        internal static void DebugPrintLayout(Tile[,] tiles, bool showRows = false, bool showCols = false)
        {
            int maxRowIndexLength = (tiles.GetLength(0) - 1).ToString().Length;
            int maxColIndexLength = (tiles.GetLength(1) - 1).ToString().Length;
            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                    Console.Write((
                        showRows ? i.ToString().PadLeft(maxRowIndexLength) :
                        showCols ? j.ToString().PadLeft(maxColIndexLength) :
                        (tiles[i, j] != null ? tiles[i, j].directionAsString() : " ")) + 
                        (j == tiles.GetLength(1) - 1 ? "" : " "));
                Console.WriteLine();
            }
        }

        #endregion

        public LayoutGenerator(
            LayoutConfiguration layoutConfiguration,
            IRandomizer rand,
            SettingConfiguration baseConfiguration,
            ControlConfiguration controlConfiguration,
            Action<string> logAction = null)
        {
            _logAction = logAction;
            String errorMessage = "";
            if (!layoutConfiguration.isValid(out errorMessage))
            {
                throw new ArgumentException("LayoutConfiguration is not valid. " + errorMessage);
            }

            baseConfiguration.InventoryConfiguration.autogenerate();
            if (!baseConfiguration.InventoryConfiguration.isValid(layoutConfiguration.PodCapacity, out errorMessage))
            {
                throw new ArgumentException("InventoryConfiguration is not valid. " + errorMessage);
            }

            if (!controlConfiguration.IsValid(out errorMessage))
            {
                throw new ArgumentException("ControlConfiguration is not valid. " + errorMessage);
            }

            this.rand = rand;
            this.baseConfiguration = baseConfiguration;
            elevatorPositions = new Dictionary<Tuple<double, double>, Elevator>();
            elevatorWaypoints = new Dictionary<Elevator, List<Waypoint>>();
            elevatorSemaphores = new Dictionary<Waypoint, QueueSemaphore>();
            instance = Instance.CreateInstance(this.baseConfiguration, controlConfiguration);
            instance.Name = layoutConfiguration.NameLayout;
            this.lc = layoutConfiguration;

        }

        private void Write(string msg)
        {
            _logAction?.Invoke(msg);
            Console.Write(msg);
        }

        public void addTiersToCompound()
        {
            for (int whichTier = 0; whichTier < lc.TierCount; whichTier++)
            {
                double relativePositionX = 0;
                double relativePositionY = 0;
                double relativePositionZ = whichTier * lc.TierHeight;
                instance.CreateTier(instance.RegisterTierID(), lc.lengthTier(), lc.widthTier(), relativePositionX, relativePositionY, relativePositionZ);
            }
        }

        public void connectAllWayPoints(Tile[,] tiles)
        {
            for (int row = 0; row < tiles.GetLength(0); row++)
            {
                for (int column = 0; column < tiles.GetLength(1); column++)
                {
                    if (tiles[row, column] != null)
                    {
                        bool addWest = false;
                        bool addEast = false;
                        bool addNorth = false;
                        bool addSouth = false;
                        switch (tiles[row, column].direction)
                        {
                            case directions.EastNorthSouthWest: addEast = true; addNorth = true; addSouth = true; addWest = true; break;
                            case directions.NorthSouthWest: addNorth = true; addSouth = true; addWest = true; break;
                            case directions.EastNorthSouth: addEast = true; addNorth = true; addSouth = true; break;
                            case directions.EastNorthWest: addEast = true; addNorth = true; addWest = true; break;
                            case directions.EastSouthWest: addEast = true; addSouth = true; addWest = true; break;
                            case directions.NorthSouth: addNorth = true; addSouth = true; break;
                            case directions.NorthWest: addNorth = true; addWest = true; break;
                            case directions.EastNorth: addEast = true; addNorth = true; break;
                            case directions.SouthWest: addSouth = true; addWest = true; break;
                            case directions.EastSouth: addEast = true; addSouth = true; break;
                            case directions.EastWest: addEast = true; addWest = true; break;
                            case directions.East: addEast = true; break;
                            case directions.West: addWest = true; break;
                            case directions.South: addSouth = true; break;
                            case directions.North: addNorth = true; break;
                            case directions.Invalid: throw new ArgumentException("invalid direction encountered");
                            default: break;
                        }
                        Waypoint current = tiles[row, column].wp;
                        if (addWest)
                        {
                            Waypoint west = tiles[row, column - 1].wp;
                            current.AddPath(west);
                        }
                        if (addEast)
                        {
                            Waypoint east = tiles[row, column + 1].wp;
                            current.AddPath(east);
                        }
                        if (addNorth)
                        {
                            Waypoint north = tiles[row - 1, column].wp;
                            current.AddPath(north);
                        }
                        if (addSouth)
                        {
                            Waypoint south = tiles[row + 1, column].wp;
                            current.AddPath(south);
                        }
                    }
                }
            }
        }

        public void connectElevators()
        {
            foreach (var elevator in elevatorWaypoints.Keys)
            {
                // Add all waypoints to the elevator
                elevator.RegisterPoints(0, elevatorWaypoints[elevator]);

                // Set timings for transportation depending on the difference in tier-level
                foreach (var from in elevator.ConnectedPoints)
                    foreach (var to in elevator.ConnectedPoints.Where(wp => wp != from))
                        elevator.SetTiming(from, to, Math.Abs(from.Tier.ID - to.Tier.ID) * lc.ElevatorTransportationTimePerTier);

                // Connect the waypoints
                foreach (var from in elevator.ConnectedPoints)
                    foreach (var to in elevator.ConnectedPoints.Where(wp => wp != from))
                        from.AddPath(to);

                // Generate outgoing guards for the queues on each level
                foreach (var from in elevator.ConnectedPoints)
                    foreach (var to in elevator.ConnectedPoints.Where(wp => wp != from))
                        elevatorSemaphores[from].RegisterGuard(from, to, false, false);
            }
        }

        public void fillTiers()
        {
            int iStationActivationID = 0;
            int oStationActivationID = 0;
            Func<int> obtainIStationActivationID = () => { return iStationActivationID++; };
            Func<int> obtainOStationActivationID = () => { return oStationActivationID++; };
            foreach (var tier in instance.Compound.Tiers)
            {

                //the stations are generated first, so that generator that makes the storage area and halls knows where the entrances and exits are of the stations.
                //however, the semaphores of the stations can only be created after the halls, because the semaphores need the waypoints that from the halls

                Tile[,] tiles = new Tile[lc.widthTier(), lc.lengthTier()]; //this keeps track of all the waypoints, their directions and type. This is handy for construction of the layout but also to for example display information in the console during debugging
                SemaphoreGenerator semaphoreGenerator = new SemaphoreGenerator(tiles, lc, instance, elevatorSemaphores);
                StationGenerator stationGenerator = new StationGenerator(tier, instance, lc, tiles, elevatorPositions, elevatorWaypoints, semaphoreGenerator, obtainOStationActivationID, obtainIStationActivationID);
                stationGenerator.generateStations();
                StorageAreaAndHallsGenerator storageAreaAndHallsGenerator = new StorageAreaAndHallsGenerator(tier, instance, lc, rand, tiles, stationGenerator);
                storageAreaAndHallsGenerator.createStorageAreaAndHalls();

                if (_logInfo)
                {
                    WriteAllDirectionsInfo(tiles);
                    WriteAllTypesInfo(tiles);
                }

                generatePods(tier);
                semaphoreGenerator.generateAllSemaphores(); //depends on info from both stationGenerator and storageAreaAndHallsGenerator, therefore construction of semaphores is delayed until here
                connectAllWayPoints(tiles);
                connectElevators();
                generateRobots(tier, tiles);
                instance.Flush();
                locateResourceFiles();
            }
            // Re-order activation sequence (stations on the lowest floor go first, then the ones next to the tier's center and lastly the ones with the lowest ID - this should break all ties)
            int currentActivationID = 1;
            foreach (var station in instance.InputStations.OrderBy(s => s.Tier.ID).ThenBy(s => Metrics.Distances.CalculateEuclid(s.X, s.Y, s.Tier.Length / 2.0, s.Tier.Width / 2.0)).ThenBy(s => s.ID))
                station.ActivationOrderID = currentActivationID++;
            currentActivationID = 1;
            foreach (var station in instance.OutputStations.OrderBy(s => s.Tier.ID).ThenBy(s => Metrics.Distances.CalculateEuclid(s.X, s.Y, s.Tier.Length / 2.0, s.Tier.Width / 2.0)).ThenBy(s => s.ID))
                station.ActivationOrderID = currentActivationID++;
        }

        public Instance GenerateLayout()
        {
            // Init the tiers
            addTiersToCompound();
            // Fill the tiers
            fillTiers();
            // Return the instance
            return instance;
        }

        public void generatePods(Tier tier)
        {
            int podCount = (int)Math.Floor(lc.PodAmount * (lc.nStorageBlocks() * lc.nStorageLocationsPerBlock()));
            List<Waypoint> potentialWaypoints = tier.Waypoints.Where(wp => wp.PodStorageLocation).ToList();
            for (int i = 0; i < podCount; i++)
            {
                int waypointIndex = rand.NextInt(potentialWaypoints.Count);
                Waypoint chosenWaypoint = potentialWaypoints[waypointIndex];
                Pod pod = instance.CreatePod(instance.RegisterPodID(), tier, chosenWaypoint, lc.PodRadius, orientationPodDefault, lc.PodCapacity);
                potentialWaypoints.RemoveAt(waypointIndex);
            }
        }

        public void generateRobots(Tier tier, Tile[,] tiles)
        {
            List<List<Waypoint>> waypoints = getWaypointsForInitialRobotPositions(tiles);
            List<Waypoint> potentialBotLocations = waypoints.SelectMany(w => w).Where(w => w.Pod == null && w.InputStation == null && w.OutputStation == null).ToList();
            for (int i = 0; i < lc.BotCount; i++)
            {
                int randomWaypointIndex = rand.NextInt(potentialBotLocations.Count);
                Waypoint botWaypoint = potentialBotLocations[randomWaypointIndex];
                int orientation = 0;
                Bot bot = instance.CreateBot(instance.RegisterBotID(), tier, botWaypoint.X, botWaypoint.Y, lc.BotRadius, orientation, lc.PodTransferTime, lc.MaxAcceleration, lc.MaxDeceleration, lc.MaxVelocity, lc.TurnSpeed, lc.CollisionPenaltyTime);
                botWaypoint.AddBotApproaching(bot);
                bot.CurrentWaypoint = botWaypoint;
                potentialBotLocations.RemoveAt(randomWaypointIndex);
            }
        }

        public void locateResourceFiles()
        {
            if (baseConfiguration.InventoryConfiguration.ColoredWordConfiguration != null && !string.IsNullOrWhiteSpace(baseConfiguration.InventoryConfiguration.ColoredWordConfiguration.WordFile))
                baseConfiguration.InventoryConfiguration.ColoredWordConfiguration.WordFile = IOHelper.FindResourceFile(baseConfiguration.InventoryConfiguration.ColoredWordConfiguration.WordFile, Directory.GetCurrentDirectory());
            if (baseConfiguration.InventoryConfiguration.FixedInventoryConfiguration != null && !string.IsNullOrWhiteSpace(baseConfiguration.InventoryConfiguration.FixedInventoryConfiguration.OrderFile))
                baseConfiguration.InventoryConfiguration.FixedInventoryConfiguration.OrderFile = IOHelper.FindResourceFile(baseConfiguration.InventoryConfiguration.FixedInventoryConfiguration.OrderFile, Directory.GetCurrentDirectory());
        }

        public List<List<Waypoint>> getWaypointsForInitialRobotPositions(Tile[,] tiles)
        {
            List<List<Waypoint>> waypoints = new List<List<Waypoint>>();
            for (int row = 0; row < tiles.GetLength(0); row++)
            {
                waypoints.Add(new List<Waypoint>());
                for (int column = 0; column < tiles.GetLength(1); column++)
                {
                    Tile tile = tiles[row, column];
                    if (tile != null && tile.type.Equals(waypointTypes.Road))
                    {
                        Waypoint waypoint = tiles[row, column].wp;
                        waypoints.Last().Add(waypoint);
                    }
                }
            }
            return waypoints;
        }

        public void WriteAllDirectionsInfo(Tile[,] tiles)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Info about all directions within the generated instance:");
            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                {
                    if (tiles[i, j] == null)
                    {
                        sb.Append("# ");
                    }
                    else
                    {
                        sb.Append(tiles[i, j].directionAsString() + " ");
                    }
                }
                sb.AppendLine();
            }
            sb.AppendLine();
            Write(sb.ToString());
        }

        public void WriteAllTypesInfo(Tile[,] tiles)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Info about all types within the generated instance:");
            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                {
                    if (tiles[i, j] == null)
                    {
                        sb.Append("# ");
                    }
                    else
                    {
                        sb.Append(tiles[i, j].typeAsString());
                    }
                }
                sb.AppendLine();
            }
            sb.AppendLine();
            Write(sb.ToString());
        }
    }

    internal enum directions
    {
        //this enum indicates to which other waypoints a waypoint is connected. 
        //So for example, East means that a waypoint is only connected to the waypoint directly east of it, 
        //whereas EastNorth would indicate that the waypoint is connected to both the one directly to the east and directly to the north

        //used alphabetical order for ordering directions when there are multiple (i.e. EastNorth instead of NorthEast)

        //single directional:
        East, North, South, West,

        //two directions:
        EastNorth, EastSouth, EastWest,
        NorthSouth, NorthWest,
        SouthWest,

        //three directions:
        EastNorthSouth, EastNorthWest, EastSouthWest, NorthSouthWest,

        //four directions:
        EastNorthSouthWest,

        //invalid value
        Invalid,
    }
    /// <summary>
    /// A subset of the possible directions that is limited to single directions.
    /// </summary>
    internal enum UniDirections
    {
        /// <summary>
        /// An invalid direction.
        /// </summary>
        Invalid,
        /// <summary>
        /// A connection to the east.
        /// </summary>
        East,
        /// <summary>
        /// A connection to the north.
        /// </summary>
        North,
        /// <summary>
        /// A connection to the south.
        /// </summary>
        South,
        /// <summary>
        /// A connection to the west.
        /// </summary>
        West,
    }
    /// <summary>
    /// Distinguishes the different hallways that can be generated.
    /// </summary>
    public enum HallwayField
    {
        /// <summary>
        /// Indicates the eastern hallway field.
        /// </summary>
        East,
        /// <summary>
        /// Indicates the western hallway field.
        /// </summary>
        West,
        /// <summary>
        /// Indicates the southern hallway field.
        /// </summary>
        South,
        /// <summary>
        /// Indicates the northern hallway field.
        /// </summary>
        North,
    }

    internal enum waypointTypes
    {
        Elevator, Road, StorageLocation, PickStation, ReplenishmentStation, Buffer, Invalid
    }

    /// <summary>
    /// Comprises a coordinate.
    /// </summary>
    internal struct Coordinate
    {
        /// <summary>
        /// Creates a new coordinate.
        /// </summary>
        /// <param name="row">The row of the coordinate.</param>
        /// <param name="column">The column of the coordinate.</param>
        public Coordinate(int row, int column) { Row = row; Column = column; }
        /// <summary>
        /// The row.
        /// </summary>
        int Row;
        /// <summary>
        /// The column.
        /// </summary>
        int Column;
        /// <summary>
        /// Returns a string representation of the coordinate.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString() { return $"{Row},{Column}"; }
    }

    class Tile
    {
        public Waypoint wp { get; }
        public directions direction { get; }
        public waypointTypes type { get; }

        public Tile(directions d, Waypoint wp, waypointTypes type)
        {
            this.direction = d;
            this.wp = wp;
            this.type = type;

            if (type.Equals(waypointTypes.StorageLocation) && !d.Equals(directions.EastNorthSouthWest))
            {
                throw new ArgumentException("something went wrong with storage locations");
            }
            if (d.Equals(directions.Invalid))
            {
                throw new ArgumentException("direction invalid");
            }
            if (wp == null)
            {
                throw new ArgumentException("wp is null");
            }
        }

        public String directionAsString()
        {
            switch (direction)
            {
                case directions.EastNorthSouthWest: return "+";
                case directions.NorthSouthWest: return "<";
                case directions.EastNorthSouth: return ">";
                case directions.EastNorthWest: return "^";
                case directions.EastSouthWest: return "v";
                case directions.NorthSouth: return "|";
                case directions.NorthWest: return "d";
                case directions.EastNorth: return "b";
                case directions.SouthWest: return "q";
                case directions.EastSouth: return "p";
                case directions.EastWest: return "-";
                case directions.East: return "e";
                case directions.West: return "w";
                case directions.South: return "s";
                case directions.North: return "n";
                case directions.Invalid: return "INVALID DIRECTION!";
                default: return "SOMETHING WENT WRONG";
            }
        }

        public bool isStation()
        {
            return type.Equals(waypointTypes.PickStation) || type.Equals(waypointTypes.ReplenishmentStation) || type.Equals(waypointTypes.Elevator);
        }

        public String typeAsString()
        {
            switch (type)
            {
                case waypointTypes.Elevator: return "e ";
                case waypointTypes.Road: return "r ";
                case waypointTypes.StorageLocation: return "s ";
                case waypointTypes.Buffer: return "b ";
                case waypointTypes.PickStation: return "o ";
                case waypointTypes.ReplenishmentStation: return "i ";
                case waypointTypes.Invalid: return "INVALID DIRECTION!";
                default: return "SOMETHING WENT WRONG";
            }
        }
    }
}
