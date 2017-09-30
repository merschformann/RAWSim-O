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
    class SemaphoreGenerator
    {
        private SemaphoreBlueprint[,] semaphoreBlueprints;
        private Instance instance;
        private int currentStation = 0;
        private int currentSemaphore = 0;
        private Dictionary<Waypoint, QueueSemaphore> elevatorSemaphores;
        private Tile[,] tile;

        public SemaphoreGenerator(Tile[,] tile, LayoutConfiguration lc, Instance instance, Dictionary<Waypoint, QueueSemaphore> elevatorSemaphores)
        {
            this.tile = tile;
            this.instance = instance;
            this.elevatorSemaphores = elevatorSemaphores;
            int nStations = lc.nStations();
            int nBlueprintsPerStation = (lc.DistanceEntryExitStation + 1) / 2;
            semaphoreBlueprints = new SemaphoreBlueprint[nStations, nBlueprintsPerStation];
        }

        public void generateAllSemaphores()
        {
            if (currentStation != semaphoreBlueprints.GetLength(0) - 1 && currentSemaphore != semaphoreBlueprints.GetLength(1))
            {
                throw new ArgumentException("not all stations will be covered with semaphores");
            }
            for (int whichStation = 0; whichStation < semaphoreBlueprints.GetLength(0); whichStation++)
            {
                for (int whichSemaphore = 0; whichSemaphore < semaphoreBlueprints.GetLength(1); whichSemaphore++)
                {
                    semaphoreBlueprints[whichStation, whichSemaphore].generate();
                }
            }
        }

        public bool inputValid(int segment, int place)
        {
            if (segment == 0 && place == 0)
            {
                if (!(currentStation == 0 && currentSemaphore == 0) && currentSemaphore != semaphoreBlueprints.GetLength(1))
                {
                    return false;
                }
            }
            return true;
        }

        private void initializeNewSemaphore(int segment, int place, int lengthSegment, bool containsStationWP)
        {
            bool isSemaphoreFurthestFromStationWaypoint = false;
            if (segment == 0 && place == 0)
            {
                currentStation = (currentStation == 0 && currentSemaphore == 0) ? 0 : currentStation + 1;
                currentSemaphore = 0;
                isSemaphoreFurthestFromStationWaypoint = true;
            }
            int nBotsAllowedInSemaphore = containsStationWP ? lengthSegment + 1 : 2 * lengthSegment;
            semaphoreBlueprints[currentStation, currentSemaphore] = new SemaphoreBlueprint(tile, instance, isSemaphoreFurthestFromStationWaypoint, nBotsAllowedInSemaphore, elevatorSemaphores);
        }

        public void update(int currentRow, int currentColumn, int segment, int place, int lengthSegment, int nSegments, Tile tile, int rowDiffToNextTile, int columnDiffToNextTile, int rowDiffToNextSegment, int columnDiffToNextSegment)
        {
            if (!inputValid(segment, place))
            {
                throw new ArgumentException("input not valid for update SemaphoreGenerator");
            }
            if (segment % 2 == 0 && place == 0)
            {
                bool containsStationWP = (segment >= nSegments - 2);
                initializeNewSemaphore(segment, place, lengthSegment, containsStationWP);
                int rowFrom = currentRow - rowDiffToNextTile;
                int columnFrom = currentColumn - columnDiffToNextTile;
                int rowTo = currentRow;
                int columnTo = currentColumn;
                semaphoreBlueprints[currentStation, currentSemaphore].setEntryFromHall(rowFrom, columnFrom, rowTo, columnTo);
                if (currentSemaphore != 0)
                {
                    rowFrom = currentRow - rowDiffToNextSegment;
                    columnFrom = currentColumn - columnDiffToNextSegment;
                    rowTo = currentRow;
                    columnTo = currentColumn;
                    semaphoreBlueprints[currentStation, currentSemaphore].setEntryFromBuffer(rowFrom, columnFrom, rowTo, columnTo);
                }
            }
            if (tile.isStation())
            {
                semaphoreBlueprints[currentStation, currentSemaphore].setStationTile(tile);
            }
            if (segment % 2 == 0 && place == lengthSegment - 1 && currentSemaphore != 0)
            {
                int rowFrom = currentRow - rowDiffToNextSegment;
                int columnFrom = currentColumn - columnDiffToNextSegment;
                int rowTo = currentRow;
                int columnTo = currentColumn;
                semaphoreBlueprints[currentStation, currentSemaphore].setEntryShortcutWithinBuffer(rowFrom, columnFrom, rowTo, columnTo);
            }
            if (segment % 2 == 1 && place == 0 && currentSemaphore != semaphoreBlueprints.GetLength(1) - 1)
            {
                int rowFrom = currentRow;
                int columnFrom = currentColumn;
                int rowTo = currentRow + rowDiffToNextSegment;
                int columnTo = currentColumn + columnDiffToNextSegment;
                semaphoreBlueprints[currentStation, currentSemaphore].setExitShortcutWithinBuffer(rowFrom, columnFrom, rowTo, columnTo);
            }
            if (segment % 2 == 1 && place == lengthSegment - 1)
            {
                int rowFrom = currentRow;
                int columnFrom = currentColumn;
                int rowTo = currentSemaphore == semaphoreBlueprints.GetLength(1) - 1 ? currentRow + rowDiffToNextTile : currentRow + rowDiffToNextSegment;
                int columnTo = currentSemaphore == semaphoreBlueprints.GetLength(1) - 1 ? currentColumn + columnDiffToNextTile : currentColumn + columnDiffToNextSegment;
                semaphoreBlueprints[currentStation, currentSemaphore].setNormalExit(rowFrom, columnFrom, rowTo, columnTo);
                currentSemaphore++;
            }
        }
    }

    class SemaphoreBlueprint
    {
        //one semaphore is two segments within the buffer
        //a normal semaphore has an entry from the hall, a normal entry from the previous semaphore in the buffer, and a shortcut entry from the previous semaphore in the buffer
        //the semaphore furthest from the station waypoint does not have the two latter ones because there is no previous semaphore
        //semaphores have a normal exit and a shortcut exit into the next semaphore
        //the semaphore containing the station waypoint is an exception since there is no next semaphore, the normal exit for this semaphore leads out into the hall again

        private Tile[,] tiles;
        private Instance instance;
        private bool isSemaphoreFurthestFromStationWaypoint;
        private int nBotsAllowedInSemaphore;
        private QueueSemaphore semaphore;
        private bool alreadyGenerated = false;
        private Tile stationTile;
        private Dictionary<Waypoint, QueueSemaphore> elevatorSemaphores;

        private int[,] entryFromHall = new int[2, 2];
        private int[,] entryShortcutWithinBuffer = new int[2, 2];
        private int[,] entryFromBuffer = new int[2, 2];
        private int[,] exitShortcutWithinBuffer = new int[2, 2];
        private int[,] normalExit = new int[2, 2];

        private bool entryFromHallReady = false;
        private bool entryShortcutWithinBufferReady = false;
        private bool entryFromBufferReady = false;
        private bool normalExitReady = false;
        private bool exitShortcutWithinBufferReady = false;

        public SemaphoreBlueprint(Tile[,] tile, Instance instance, bool isSemaphoreFurthestFromStationWaypoint, int nBotsAllowedInSemaphore, Dictionary<Waypoint, QueueSemaphore> elevatorSemaphores)
        {
            this.tiles = tile;
            this.instance = instance;
            this.isSemaphoreFurthestFromStationWaypoint = isSemaphoreFurthestFromStationWaypoint;
            this.nBotsAllowedInSemaphore = nBotsAllowedInSemaphore;
            this.elevatorSemaphores = elevatorSemaphores;
        }

        public bool containsStationWaypoint()
        {
            return stationTile != null;
        }

        public void generate()
        {
            if (!isReady())
            {
                throw new ArgumentException("Cannot generate the Semaphore, because it is not ready yet");
            }
            if (alreadyGenerated || semaphore != null)
            {
                throw new ArgumentException("already generated the semaphore, now attempting to generate it a second time!");
            }
            semaphore = instance.CreateSemaphore(instance.RegisterSemaphoreID(), nBotsAllowedInSemaphore);
            generateEntryFromHall();
            generateNormalExit();
            if (!containsStationWaypoint())
            {
                generateExitShortcutWithinBuffer();
            }
            if (!isSemaphoreFurthestFromStationWaypoint)
            {
                generateEntryFromBuffer();
                generateEntryShortcutWithinBuffer();
            }
            alreadyGenerated = true;

            updateElevatorSemaphores();
        }

        private void generateEntryFromHall()
        {
            bool entry = true;
            bool barrier = true;
            generateGuard(entryFromHall, entry, barrier);
        }

        private void generateEntryShortcutWithinBuffer()
        {
            bool entry = true;
            bool barrier = true;
            generateGuard(entryShortcutWithinBuffer, entry, barrier);
        }

        private void generateEntryFromBuffer()
        {
            bool entry = true;
            bool barrier = false;
            generateGuard(entryFromBuffer, entry, barrier);
        }

        private void generateExitShortcutWithinBuffer()
        {
            bool entry = false;
            bool barrier = false;
            generateGuard(exitShortcutWithinBuffer, entry, barrier);
        }

        private void generateNormalExit()
        {
            bool entry = false;
            bool barrier = false;
            generateGuard(normalExit, entry, barrier);
        }

        private void generateGuard(int[,] info, bool entry, bool barrier)
        {
            int rowFrom = info[0, 0];
            int columnFrom = info[0, 1];
            int rowTo = info[1, 0];
            int columnTo = info[1, 1];
            Waypoint from = tiles[rowFrom, columnFrom].wp;
            Waypoint to = tiles[rowTo, columnTo].wp;
            semaphore.RegisterGuard(from, to, entry, barrier);
        }

        public bool isReady()
        {
            bool universalConditionMet = nBotsAllowedInSemaphore >= 1 && entryFromHallReady && normalExitReady;
            bool additionalConditionStationWaypoint = containsStationWaypoint() ? !exitShortcutWithinBufferReady : exitShortcutWithinBufferReady; //from the semaphore with the station waypoint there is not shortcut to a next part of the buffer

            //the semaphore furthest away from the station does not have entries from the shortcut within the buffer or normal entry from previous parts of the buffer
            bool additionalConditionSemaphoreFurthestFromStationWaypoint = isSemaphoreFurthestFromStationWaypoint ? !entryShortcutWithinBufferReady && !entryFromBufferReady : entryShortcutWithinBufferReady && entryFromBufferReady;

            return universalConditionMet && additionalConditionStationWaypoint && additionalConditionSemaphoreFurthestFromStationWaypoint;
        }

        public void setEntryFromHall(int rowFrom, int columnFrom, int rowTo, int columnTo)
        {
            setInfo(entryFromHall, rowFrom, columnFrom, rowTo, columnTo, "entryFromHall", ref entryFromHallReady);
        }

        public void setEntryShortcutWithinBuffer(int rowFrom, int columnFrom, int rowTo, int columnTo)
        {
            setInfo(entryShortcutWithinBuffer, rowFrom, columnFrom, rowTo, columnTo, "entryShortcutWithinBuffer", ref entryShortcutWithinBufferReady);
        }

        public void setEntryFromBuffer(int rowFrom, int columnFrom, int rowTo, int columnTo)
        {
            setInfo(entryFromBuffer, rowFrom, columnFrom, rowTo, columnTo, "entryFromBuffer", ref entryFromBufferReady);
        }

        public void setExitShortcutWithinBuffer(int rowFrom, int columnFrom, int rowTo, int columnTo)
        {
            setInfo(exitShortcutWithinBuffer, rowFrom, columnFrom, rowTo, columnTo, "exitShortcutWithinBuffer", ref exitShortcutWithinBufferReady);
        }

        public void setNormalExit(int rowFrom, int columnFrom, int rowTo, int columnTo)
        {
            setInfo(normalExit, rowFrom, columnFrom, rowTo, columnTo, "normalExitReady", ref normalExitReady);
        }

        private void setInfo(int[,] info, int rowFrom, int columnFrom, int rowTo, int columnTo, String name, ref bool alreadySetInfo)
        {
            if (alreadySetInfo)
            {
                throw new ArgumentException("Problems at BlueprintNormalSemaphore." + name + ", was already true, so you are setting this twice now");
            }
            info[0, 0] = rowFrom;
            info[0, 1] = columnFrom;
            info[1, 0] = rowTo;
            info[1, 1] = columnTo;
            alreadySetInfo = true;
        }

        public void setStationTile(Tile tile)
        {
            if (stationTile != null)
            {
                throw new ArgumentException("stationTile was already set, at SemaphoreGenerator.SemaphoreBlueprint");
            }
            this.stationTile = tile;
        }

        private void updateElevatorSemaphores()
        {
            if (containsStationWaypoint())
            {
                if (stationTile.type.Equals(waypointTypes.Elevator))
                {
                    elevatorSemaphores[stationTile.wp] = semaphore; // Store the semaphore in case it is an elevator
                }
            }
        }
    }
}
