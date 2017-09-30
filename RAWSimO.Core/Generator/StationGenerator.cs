using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.Waypoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Generator
{
    class StationGenerator
    {
        private Tier tier;
        private Instance instance;
        private LayoutConfiguration lc;
        private Tile[,] tiles;
        private Dictionary<Tuple<double, double>, Elevator> elevatorPositions;
        private Dictionary<Elevator, List<Waypoint>> elevatorWaypoints;
        private SemaphoreGenerator semaphoreGenerator;
        private Func<int> obtainNextOStationActivationID;
        private Func<int> obtainNextIStationActivationID;

        public HashSet<Coordinate> coordinatesStationEntrances { get; } = new HashSet<Coordinate>();

        public StationGenerator(Tier tier, Instance instance, LayoutConfiguration layoutConfiguration, Tile[,] tiles, Dictionary<Tuple<double, double>, Elevator> elevatorPositions, Dictionary<Elevator, List<Waypoint>> elevatorWaypoints, SemaphoreGenerator semaphoreGenerator, Func<int> obtainNextOStationActivationID, Func<int> obtainNextIStationActivationID)
        {
            this.tier = tier;
            this.instance = instance;
            this.lc = layoutConfiguration;
            this.tiles = tiles;
            this.elevatorPositions = elevatorPositions;
            this.elevatorWaypoints = elevatorWaypoints;
            this.semaphoreGenerator = semaphoreGenerator;
            this.obtainNextOStationActivationID = obtainNextOStationActivationID;
            this.obtainNextIStationActivationID = obtainNextIStationActivationID;
        }

        public void addToBufferPath(bool stationWasBuilt, Tile tile, List<Waypoint> bufferPath)
        {
            //the buffer path is a from the station way point to the entrance of the buffer
            //this has two consequences:
            //1) waypoints between the station waypoint and the exit of the buffer are not included
            //2) waypoints between the station waypoint and the entrance of the buffer should be included in the reverse order in which they were created
            //note that the station itself also has to be added
            if (!stationWasBuilt || tile.isStation())
            {
                bufferPath.Insert(0, tile.wp);
            }
        }

        public void buildElevator(int row, int column, directions d, List<Waypoint> bufferPaths)
        {
            //have to determine x and y here to see whether an elevator for that position already exists
            double y = row + 0.5;
            double x = column + 0.5;

            Tuple<double, double> elevatorPosition = new Tuple<double, double>(x, y);
            bool elevatorAtThisPositionAlreadyExists = elevatorPositions.ContainsKey(elevatorPosition);
            Elevator elevator = elevatorAtThisPositionAlreadyExists ? elevatorPositions[elevatorPosition] : instance.CreateElevator(instance.RegisterElevatorID());

            createTile_Elevator(row, column, d, elevator);
            Waypoint elevatorWaypoint = tiles[row, column].wp;

            if (elevatorWaypoint.X != x || elevatorWaypoint.Y != y)
            {
                throw new ArgumentException("something went wrong here while building the elevator, elevatorWaypoint.X != x || elevatorWaypoint.Y != y");
            }

            if (!elevatorAtThisPositionAlreadyExists)
            {
                elevatorPositions.Add(elevatorPosition, elevator);
                elevatorWaypoints[elevator] = new List<Waypoint>();
            }
            elevatorWaypoints[elevator].Add(elevatorWaypoint);
            elevator.Queues[elevatorWaypoint] = bufferPaths;
        }

        public void buildPickStation(int row, int column, directions d, List<Waypoint> bufferPaths, int activationOrderID)
        {
            OutputStation oStation = instance.CreateOutputStation(
                instance.RegisterOutputStationID(), tier, column + 0.5, row + 0.5, lc.StationRadius, lc.OStationCapacity, lc.ItemTransferTime, lc.ItemPickTime, activationOrderID);
            createTile_PickStation(row, column, d, oStation);
            Waypoint wp = tiles[row, column].wp;
            oStation.Queues[wp] = bufferPaths;
        }

        public void buildReplenishmentStation(int row, int column, directions d, List<Waypoint> bufferPaths, int activationOrderID)
        {
            InputStation iStation = instance.CreateInputStation(
                instance.RegisterInputStationID(), tier, column + 0.5, row + 0.5, lc.StationRadius, lc.IStationCapacity, lc.ItemBundleTransferTime, activationOrderID);
            createTile_ReplenishmentStation(row, column, d, iStation);
            Waypoint wp = tiles[row, column].wp;
            iStation.Queues[wp] = bufferPaths;
        }

        public void buildStation(waypointTypes typeOfStation, int row, int column, directions d, List<Waypoint> bufferPaths, int activationOrderID)
        {
            switch (typeOfStation)
            {
                case waypointTypes.PickStation:
                    buildPickStation(row, column, d, bufferPaths, activationOrderID);
                    break;
                case waypointTypes.ReplenishmentStation:
                    buildReplenishmentStation(row, column, d, bufferPaths, activationOrderID);
                    break;
                case waypointTypes.Elevator:
                    buildElevator(row, column, d, bufferPaths);
                    break;
                default:
                    throw new ArgumentException("should built station, but typeOfStations is: " + typeOfStation);
            }
        }

        public void buildStationAndBuffer(int exitRow, int exitColumn, int entryRow, int entryColumn, int stationRow, int stationColumn, waypointTypes typeOfStation, HashSet<Coordinate> coordinatesStationEntrances, int entranceCounter, int activationOrderID)
        {
            checkValidityInputArgumentsBuildStationAndBuffer(exitRow, exitColumn, entryRow, entryColumn, stationRow, stationColumn, typeOfStation);

            int lengthSegment = Math.Max(Math.Abs(exitRow - stationRow), Math.Abs(exitColumn - stationColumn)) + 1;
            int nSegments = Math.Max(Math.Abs(exitRow - entryRow), Math.Abs(exitColumn - entryColumn)) + 1;

            int rowDiffToNextTile = exitRow > stationRow ? -1 : exitRow < stationRow ? 1 : 0;
            int columnDiffToNextTile = exitColumn > stationColumn ? -1 : exitColumn < stationColumn ? 1 : 0; ;
            int rowDiffToNextSegment = entryRow > exitRow ? -1 : entryRow < exitRow ? 1 : 0; ;
            int columnDiffToNextSegment = entryColumn > exitColumn ? -1 : entryColumn < exitColumn ? 1 : 0;

            if (lengthSegment <= 0 || lengthSegment != lc.WidthBuffer)
            {
                throw new ArgumentException("something went wrong while constructing the station, lengthSegment <= 0 || lengthSegment != lc.WidthBuffer, lengthSegment: " + lengthSegment);
            }
            if (nSegments <= 0 || nSegments % 2 != 0)
            {
                throw new ArgumentException("something went wrong while constructing the station, nSegments <= 0 || nSegments % 2 != 0, nSegments: " + nSegments);
            }

            List<Waypoint> bufferPath = new List<Waypoint>();
            int row = entryRow;
            int column = entryColumn;
            bool stationWasBuilt = false;
            for (int segment = 0; segment < nSegments; segment++)
            {
                if (segment % 2 == 0)
                {
                    coordinatesStationEntrances.Add(new Coordinate(row - rowDiffToNextTile, column - columnDiffToNextTile));
                }
                for (int place = 0; place < lengthSegment; place++)
                {
                    directions d = determineDirection(place, segment, lengthSegment, nSegments, rowDiffToNextTile, columnDiffToNextTile, rowDiffToNextSegment, columnDiffToNextSegment);
                    if (row == stationRow && column == stationColumn)
                    {
                        buildStation(typeOfStation, row, column, d, bufferPath, activationOrderID);
                        stationWasBuilt = true;
                    }
                    else
                    {
                        createTile_Buffer(row, column, d);
                    }
                    Waypoint location = tiles[row, column].wp;
                    semaphoreGenerator.update(row, column, segment, place, lengthSegment, nSegments, tiles[row, column], rowDiffToNextTile, columnDiffToNextTile, rowDiffToNextSegment, columnDiffToNextSegment);
                    addToBufferPath(stationWasBuilt, tiles[row, column], bufferPath);
                    row += place == lengthSegment - 1 ? 0 : rowDiffToNextTile;
                    column += place == lengthSegment - 1 ? 0 : columnDiffToNextTile;
                }
                rowDiffToNextTile = rowDiffToNextTile != 0 ? -rowDiffToNextTile : rowDiffToNextTile;
                columnDiffToNextTile = columnDiffToNextTile != 0 ? -columnDiffToNextTile : columnDiffToNextTile;
                row += rowDiffToNextSegment;
                column += columnDiffToNextSegment;
            }
            if (!stationWasBuilt)
            {
                throw new ArgumentException("!stationWasBuilt");
            }
        }

        public directions buildStationAndBuffer_normalDirection(int rowDiff, int columnDiff)
        {
            return rowDiff == -1 ? directions.North : rowDiff == 1 ? directions.South : columnDiff == -1 ? directions.West : columnDiff == 1 ? directions.East : directions.Invalid;
        }

        public directions buildStationAndBuffer_shortcutDirection(int rowDiffToNextTile, int columnDiffToNextTile, int rowDiffToNextSegment, int columnDiffToNextSegment)
        {
            if ((rowDiffToNextTile == -1 && columnDiffToNextSegment == 1) || (rowDiffToNextSegment == -1 && columnDiffToNextTile == 1))
            {
                return directions.EastNorth;
            }
            else if ((rowDiffToNextTile == 1 && columnDiffToNextSegment == 1) || (rowDiffToNextSegment == 1 && columnDiffToNextTile == 1))
            {
                return directions.EastSouth;
            }
            else if ((rowDiffToNextTile == -1 && columnDiffToNextSegment == -1) || (rowDiffToNextSegment == -1 && columnDiffToNextTile == -1))
            {
                return directions.NorthWest;
            }
            else if ((rowDiffToNextTile == 1 && columnDiffToNextSegment == -1) || (rowDiffToNextSegment == 1 && columnDiffToNextTile == -1))
            {
                return directions.SouthWest;
            }
            else
            {
                return directions.Invalid;
            }
        }

        public int calculateNrPossibleStations(int nAisles)
        {
            return lc.AislesTwoDirectional && lc.DistanceEntryExitStation < lc.minDistanceExits() ? nAisles : nAisles / 2;
        }

        public void checkValidityInputArgumentsBuildStationAndBuffer(int exitRow, int exitColumn, int entryRow, int entryColumn, int stationRow, int stationColumn, waypointTypes typeOfStation)
        {
            //check validity input arguments
            if (exitRow < 0 || stationRow < 0 || entryRow < 0 || exitColumn < 0 || stationColumn < 0 || entryColumn < 0)
            {
                throw new ArgumentException("something went wrong while constructing the station, one or more are negative, exitRow: " + exitRow + ", stationRow: " + stationRow + ", entryRow: " + entryRow + ", exitColumn: " + exitColumn + ", stationColumn: " + stationColumn + ", entryColumn: " + entryColumn);
            }
            if (exitRow != stationRow && exitColumn != stationColumn)
            {
                throw new ArgumentException("something went wrong while constructing the station, exitRow != stationRow && exitColumn != stationColumn, exitRow: " + exitRow + ", stationRow: " + stationRow + ", exitColumn: " + exitColumn + ", stationColumn: " + stationColumn);
            }
            if (exitRow != entryRow && exitColumn != entryColumn)
            {
                throw new ArgumentException("something went wrong while constructing the station, exitRow != entryRow && exitColumn != entryColumn, exitRow: " + exitRow + ", entryRow: " + entryRow + ", exitColumn: " + exitColumn + ", entryColumn: " + entryColumn);
            }
            if (exitRow == stationRow && exitColumn == stationColumn)
            {
                throw new ArgumentException("something went wrong while constructing the station, exitRow == stationRow && exitColumn == stationColumn, exitRow: " + exitRow + ", exitColumn: " + exitColumn);
            }
            if (exitRow == entryRow && exitColumn == entryColumn)
            {
                throw new ArgumentException("something went wrong while constructing the station, exitRow == entryRow && exitColumn == entryColumn, exitRow: " + exitRow + ", exitColumn: " + exitColumn);
            }
            if (stationColumn == entryRow && stationColumn == entryColumn)
            {
                throw new ArgumentException("something went wrong while constructing the station, stationRow == entryRow && stationColumn == entryColumn, stationRow: " + stationRow + ", stationColumn: " + stationColumn);
            }
            if (!typeOfStation.Equals(waypointTypes.PickStation) && !typeOfStation.Equals(waypointTypes.ReplenishmentStation) && !typeOfStation.Equals(waypointTypes.Elevator))
            {
                throw new ArgumentException("!typeOfStation.Equals(waypointTypes.PickStation) && !typeOfStation.Equals(waypointTypes.ReplenishmentStation) && !typeOfStation.Equals(waypointTypes.Elevator)");
            }
        }

        public void createTile_Buffer(int row, int column, directions d)
        {
            Waypoint wp = instance.CreateWaypoint(instance.RegisterWaypointID(), tier, column + 0.5, row + 0.5, false, true);
            if (tiles[row, column] != null)
            {
                throw new ArgumentException("trying to overwrite an existing waypoint!! At createTile_Buffer: tiles[row, column] != null");
            }
            tiles[row, column] = new Tile(d, wp, waypointTypes.Buffer);
        }

        public void createTile_Elevator(int row, int column, directions d, Elevator elevator)
        {
            Waypoint wp = instance.CreateWaypoint(instance.RegisterWaypointID(), tier, elevator, column + 0.5, row + 0.5, true);
            if (tiles[row, column] != null)
            {
                throw new ArgumentException("trying to overwrite an existing waypoint!! At createTile_Elevator: tiles[row, column] != null");
            }
            tiles[row, column] = new Tile(d, wp, waypointTypes.Elevator);
        }

        public void createTile_PickStation(int row, int column, directions d, OutputStation oStation)
        {
            Waypoint wp = instance.CreateWaypoint(instance.RegisterWaypointID(), tier, oStation, true);
            if (tiles[row, column] != null)
            {
                throw new ArgumentException("trying to overwrite an existing waypoint!! At createTile_PickStation: tiles[row, column] != null");
            }
            tiles[row, column] = new Tile(d, wp, waypointTypes.PickStation);
        }

        public void createTile_ReplenishmentStation(int row, int column, directions d, InputStation iStation)
        {
            Waypoint wp = instance.CreateWaypoint(instance.RegisterWaypointID(), tier, iStation, true);
            if (tiles[row, column] != null)
            {
                throw new ArgumentException("trying to overwrite an existing waypoint!! At createTile_ReplenishmentStation: tiles[row, column] != null");
            }
            tiles[row, column] = new Tile(d, wp, waypointTypes.ReplenishmentStation);
        }

        public directions determineDirection(int place, int segment, int lengthSegment, int nSegments, int rowDiffToNextTile, int columnDiffToNextTile, int rowDiffToNextSegment, int columnDiffToNextSegment)
        {
            directions shortcutDirection = buildStationAndBuffer_shortcutDirection(rowDiffToNextTile, columnDiffToNextTile, rowDiffToNextSegment, columnDiffToNextSegment);
            directions toNewSegment = buildStationAndBuffer_normalDirection(rowDiffToNextSegment, columnDiffToNextSegment);
            directions toNextPlace = buildStationAndBuffer_normalDirection(rowDiffToNextTile, columnDiffToNextTile);
            bool conditionForShortcut = (segment % 2 == 1 && place == 0 && segment != nSegments - 1);
            bool conditionToNexSegment = (place == lengthSegment - 1 && segment != nSegments - 1);
            return conditionForShortcut ? shortcutDirection : conditionToNexSegment ? toNewSegment : toNextPlace;
        }

        public int distanceFirstExitOfStationFromSideOfWarehouse(bool otherHallPresentThatIncreasesDistance, int blockSize, bool eastOrNorthHall)
        {
            //for the north and south hall this is the distance to the west side of the warehouse
            //for the west and east hall this is the distance to the north side of the warehouse

            int distanceDueToOtherSideHall = otherHallPresentThatIncreasesDistance ? lc.WidthBuffer + lc.WidthHall : 0;
            int distanceDueToRingway = 1;
            int distanceDueToFirstStorageBlock = blockSize;
            int distanceBetweenFirstAndSecondAisle = blockSize + lc.widthAisles();
            bool includeDistanceBetweenFirstAndSecondAisle = lc.AislesTwoDirectional ? false : ((eastOrNorthHall && !lc.CounterClockwiseRingwayDirection) || (!eastOrNorthHall && lc.CounterClockwiseRingwayDirection));
            int distanceDueToAisleWidth = !lc.SingleLane && !lc.AislesTwoDirectional && ((eastOrNorthHall && !lc.CounterClockwiseRingwayDirection) || (!eastOrNorthHall && lc.CounterClockwiseRingwayDirection)) ? 1 : 0;
            return distanceDueToOtherSideHall + distanceDueToRingway + distanceDueToFirstStorageBlock + (includeDistanceBetweenFirstAndSecondAisle ? distanceBetweenFirstAndSecondAisle : 0) + distanceDueToAisleWidth;
        }

        public void generateStations()
        {
            if (lc.hasStationsEast())
            {
                generateStationsEast();
            }
            if (lc.hasStationsWest())
            {
                generateStationsWest();
            }
            if (lc.hasStationsNorth())
            {
                generateStationsNorth();
            }
            if (lc.hasStationsSouth())
            {
                generateStationsSouth();
            }
        }

        public void generateStationsNorth()
        {
            int rowDiffEntry = 0;
            int columnDiffEntry = lc.CounterClockwiseRingwayDirection ? lc.DistanceEntryExitStation : -lc.DistanceEntryExitStation;
            int rowDiffStation = -(lc.WidthBuffer - 1);
            int columnDiffStation = 0;
            int rowFirstExit = lc.WidthBuffer - 1;
            int columnFirstExit = distanceFirstExitOfStationFromSideOfWarehouse(lc.hasStationsWest(), lc.HorizontalLengthBlock, true);
            int rowDiffExits = 0;
            int columnDiffExits = lc.distanceBetweenPossibleExitLocationsAtNorthOrSouthHall();
            int nPossibleStations = calculateNrPossibleStations(lc.NrVerticalAisles);
            int[,] possibleLocationsForExits = possibleLocationsForExitsStations(rowFirstExit, columnFirstExit, rowDiffExits, columnDiffExits, nPossibleStations);
            generateStationsOnOneSide(possibleLocationsForExits, lc.NPickStationNorth, lc.NReplenishmentStationNorth, lc.NElevatorsNorth, rowDiffEntry, columnDiffEntry, rowDiffStation, columnDiffStation, coordinatesStationEntrances);
        }

        public void generateStationsSouth()
        {
            int rowDiffEntry = 0;
            int columnDiffEntry = lc.CounterClockwiseRingwayDirection ? -lc.DistanceEntryExitStation : lc.DistanceEntryExitStation;
            int rowDiffStation = lc.WidthBuffer - 1;
            int columnDiffStation = 0;
            int rowFirstExit = lc.widthTier() - lc.WidthBuffer;
            int columnFirstExit = distanceFirstExitOfStationFromSideOfWarehouse(lc.hasStationsWest(), lc.HorizontalLengthBlock, false);
            int rowDiffExits = 0;
            int columnDiffExits = lc.distanceBetweenPossibleExitLocationsAtNorthOrSouthHall();
            int nPossibleStations = calculateNrPossibleStations(lc.NrVerticalAisles);
            int[,] possibleLocationsForExits = possibleLocationsForExitsStations(rowFirstExit, columnFirstExit, rowDiffExits, columnDiffExits, nPossibleStations);
            generateStationsOnOneSide(possibleLocationsForExits, lc.NPickStationSouth, lc.NReplenishmentStationSouth, lc.NElevatorsSouth, rowDiffEntry, columnDiffEntry, rowDiffStation, columnDiffStation, coordinatesStationEntrances);
        }

        public void generateStationsWest()
        {
            int rowDiffEntry = lc.CounterClockwiseRingwayDirection ? -lc.DistanceEntryExitStation : lc.DistanceEntryExitStation;
            int columnDiffEntry = 0;
            int rowDiffStation = 0;
            int columnDiffStation = -(lc.WidthBuffer - 1);
            int rowFirstExit = distanceFirstExitOfStationFromSideOfWarehouse(lc.hasStationsNorth(), lc.VerticalLengthBlock, false);
            int columnFirstExit = lc.WidthBuffer - 1;
            int rowDiffExits = lc.distanceBetweenPossibleExitLocationsAtWestOrEastHall();
            int columnDiffExits = 0;
            int nPossibleStations = calculateNrPossibleStations(lc.NrHorizontalAisles);
            int[,] possibleLocationsForExits = possibleLocationsForExitsStations(rowFirstExit, columnFirstExit, rowDiffExits, columnDiffExits, nPossibleStations);
            generateStationsOnOneSide(possibleLocationsForExits, lc.NPickStationWest, lc.NReplenishmentStationWest, lc.NElevatorsWest, rowDiffEntry, columnDiffEntry, rowDiffStation, columnDiffStation, coordinatesStationEntrances);
        }

        public void generateStationsEast()
        {
            int rowDiffEntry = lc.CounterClockwiseRingwayDirection ? lc.DistanceEntryExitStation : -lc.DistanceEntryExitStation;
            int columnDiffEntry = 0;
            int rowDiffStation = 0;
            int columnDiffStation = lc.WidthBuffer - 1;
            int rowFirstExit = distanceFirstExitOfStationFromSideOfWarehouse(lc.hasStationsNorth(), lc.VerticalLengthBlock, true);
            int columnFirstExit = lc.lengthTier() - lc.WidthBuffer;
            int rowDiffExits = lc.distanceBetweenPossibleExitLocationsAtWestOrEastHall();
            int columnDiffExits = 0;
            int nPossibleStations = calculateNrPossibleStations(lc.NrHorizontalAisles);
            int[,] possibleLocationsForExits = possibleLocationsForExitsStations(rowFirstExit, columnFirstExit, rowDiffExits, columnDiffExits, nPossibleStations);
            generateStationsOnOneSide(possibleLocationsForExits, lc.NPickStationEast, lc.NReplenishmentStationEast, lc.NElevatorsEast, rowDiffEntry, columnDiffEntry, rowDiffStation, columnDiffStation, coordinatesStationEntrances);
        }

        public void generateStationsOnOneSide(int[,] possibleLocationsForExits, int nPickStation, int nReplenishmentStations, int nElevators, int rowDiffEntry, int columnDiffEntry, int rowDiffStation, int columnDiffStation, HashSet<Coordinate> coordinatesStationEntrances)
        {
            int[,] exits = selectLocations(possibleLocationsForExits, nPickStation + nReplenishmentStations + nElevators);
            int entranceCounter = 0;
            for (int whichStation = 0; whichStation < exits.GetLength(0); whichStation++)
            {
                int exitRow = exits[whichStation, 0];
                int exitColumn = exits[whichStation, 1];
                int entryRow = exits[whichStation, 0] + rowDiffEntry;
                int entryColumn = exits[whichStation, 1] + columnDiffEntry;
                int stationRow = exits[whichStation, 0] + rowDiffStation;
                int stationColumn = exits[whichStation, 1] + columnDiffStation;
                waypointTypes typeOfStation = whichStation < nPickStation ? waypointTypes.PickStation : whichStation < nPickStation + nReplenishmentStations ? waypointTypes.ReplenishmentStation : waypointTypes.Elevator;
                int activationID = 0;
                switch (typeOfStation)
                {
                    case waypointTypes.PickStation: activationID = obtainNextOStationActivationID(); break;
                    case waypointTypes.ReplenishmentStation: activationID = obtainNextIStationActivationID(); break;
                    default: break;
                }
                buildStationAndBuffer(exitRow, exitColumn, entryRow, entryColumn, stationRow, stationColumn, typeOfStation, coordinatesStationEntrances, entranceCounter, activationID);
                entranceCounter += lc.nEntrancesPerStation();
            }
        }

        public int[,] possibleLocationsForExitsStations(int rowFirstExit, int columnFirstExit, int rowDiffExits, int columnDiffExits, int nPossibleStations)
        {
            int[,] possibleLocationsForExits = new int[nPossibleStations, 2];
            possibleLocationsForExits[0, 0] = rowFirstExit;
            possibleLocationsForExits[0, 1] = columnFirstExit;
            for (int whichExit = 1; whichExit < nPossibleStations; whichExit++)
            {
                possibleLocationsForExits[whichExit, 0] = possibleLocationsForExits[whichExit - 1, 0] + rowDiffExits;
                possibleLocationsForExits[whichExit, 1] = possibleLocationsForExits[whichExit - 1, 1] + columnDiffExits;
            }
            return possibleLocationsForExits;
        }

        public int[,] selectLocations(int[,] possibleLocations, int nStationsInTotal)
        {
            if (nStationsInTotal > possibleLocations.GetLength(0))
            {
                throw new ArgumentException("nStationsInTotal > possibleLocations.GetLength(0)");
            }
            if (possibleLocations.GetLength(1) != 2)
            {
                throw new ArgumentException("possibleLocations.GetLength(1) != 2");
            }
            int[,] locations = new int[nStationsInTotal, 2];

            int start = (possibleLocations.GetLength(0) - nStationsInTotal) / 2;
            for (int i = 0; i < nStationsInTotal; i++)
            {
                locations[i, 0] = possibleLocations[start + i, 0];
                locations[i, 1] = possibleLocations[start + i, 1];
            }
            return locations;
        }
    }
}
