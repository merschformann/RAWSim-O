using RAWSimO.Core.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Configurations
{
    /// <summary>
    /// Declares some basic aisle layout types.
    /// </summary>
    public enum AisleLayoutTypes
    {
        /// <summary>
        /// Layout of Tim Lamballais.
        /// </summary>
        Tim,
        /// <summary>
        /// Modified layout using a highway as a hallway enabling more possibilities for the bots to switch aisles.
        /// </summary>
        HighwayHallway,
    }

    /// <summary>
    /// Supplies all attributes of a layout for generating it.
    /// </summary>
    public class LayoutConfiguration
    {

        #region member variables 

        /// <summary>
        /// The number of tiers to generate.
        /// </summary>
        public int TierCount = 1;
        /// <summary>
        /// The height of a tier. (Only relevant for visual feedback)
        /// </summary>
        public double TierHeight = 4;
        /// <summary>
        /// The number of bots to generate.
        /// </summary>
        public int BotCount = 32;
        /// <summary>
        /// The radius of a bot in m.
        /// </summary>
        public double BotRadius = 0.35;
        /// <summary>
        /// The acceleration of a bot in m/s^2.
        /// </summary>
        public double MaxAcceleration = 1.0;
        /// <summary>
        /// The deceleration of a bot in m/s^2.
        /// </summary>
        public double MaxDeceleration = 1.0;
        /// <summary>
        /// The maximal velocity of a bot in m/s.
        /// </summary>
        public double MaxVelocity = 1.5;
        /// <summary>
        /// The time it takes for the bot to do a complete (360°) turn in s.
        /// </summary>
        public double TurnSpeed = 2.5;
        /// <summary>
        /// The penalty time for bots that collide.
        /// </summary>
        public double CollisionPenaltyTime = 0.5;
        /// <summary>
        /// The time it takes to pickup / setdown a pod.
        /// </summary>
        public double PodTransferTime = 2.2;
        /// <summary>
        /// The amount of pods generated relative to the number of available storage locations.
        /// </summary>
        public double PodAmount = 0.85;
        /// <summary>
        /// The radius of a pod in m.
        /// </summary>
        public double PodRadius = 0.45;
        /// <summary>
        /// The capacity of a pod.
        /// </summary>
        public double PodCapacity = 500;
        /// <summary>
        /// The radius of the I/O-stations in m.
        /// </summary>
        public double StationRadius = 0.45;
        /// <summary>
        /// The time it takes to handle an item at a pick station.
        /// </summary>
        public double ItemTransferTime = 10;
        /// <summary>
        /// The time it takes to pick an item from a pod (excluding further handling times like putting it in a tote).
        /// </summary>
        public double ItemPickTime = 3;
        /// <summary>
        /// The time it takes to place a bundle in a pod.
        /// </summary>
        public double ItemBundleTransferTime = 10;
        /// <summary>
        /// The capacity of the input stations in bundle weight.
        /// </summary>
        public double IStationCapacity = 1000;
        /// <summary>
        /// The maximal number of orders that can be assigned to an output-station.
        /// </summary>
        public int OStationCapacity = 12;
        /// <summary>
        /// The time it takes to transport a bot vertically with an elevator for one tier in s.
        /// </summary>
        public double ElevatorTransportationTimePerTier = 10;
        /// <summary>
        /// The type of the aisle layout to use.
        /// </summary>
        public AisleLayoutTypes AisleLayoutType = AisleLayoutTypes.Tim;
        /// <summary>
        /// Indicates whether aisles are generated in two directional mode or not.
        /// </summary>
        public bool AislesTwoDirectional = false;
        /// <summary>
        /// Indicates whether aisles will be generated as single lanes.
        /// </summary>
        public bool SingleLane = true;
        /// <summary>
        /// The name of the layout.
        /// </summary>
        public string NameLayout = "tiny";
        /// <summary>
        /// The number of horizontal aisles to generate.
        /// </summary>
        public int NrHorizontalAisles = 8;
        /// <summary>
        /// The number of vertical aisles to generate.
        /// </summary>
        public int NrVerticalAisles = 6;
        /// <summary>
        /// The horizontal length of a block, i.e. the number of pod columns placed in a block.
        /// </summary>
        public int HorizontalLengthBlock = 4;
        /// <summary>
        /// The vertical length of a block, i.e. the number of pod rows placed in a block.
        /// </summary>
        public readonly int VerticalLengthBlock = 2; //this parameter really should not be changed, just created it to make other code more readable
        /// <summary>
        /// The width of the ringway.
        /// </summary>
        public readonly int WidthRingway = 1; //this parameter really should not be changed, just created it to make other code more readable
        /// <summary>
        /// The width of the hall.
        /// </summary>
        public int WidthHall = 6;
        /// <summary>
        /// The width of the buffer.
        /// </summary>
        public int WidthBuffer = 4;
        /// <summary>
        /// The distance between entrance and exit of a station.
        /// </summary>
        public int DistanceEntryExitStation = 3;
        /// <summary>
        /// The direction of the ringway. It's either clockwise or counter-clockwise.
        /// </summary>
        public bool CounterClockwiseRingwayDirection = true;
        /// <summary>
        /// The number of pick stations placed at the west end of the system.
        /// </summary>
        public int NPickStationWest = 0;
        /// <summary>
        /// The number of pick stations placed at the east end of the system.
        /// </summary>
        public int NPickStationEast = 4;
        /// <summary>
        /// The number of pick stations placed at the south end of the system.
        /// </summary>
        public int NPickStationSouth = 0;
        /// <summary>
        /// The number of pick stations placed at the north end of the system.
        /// </summary>
        public int NPickStationNorth = 0;
        /// <summary>
        /// The number of replenishment stations placed at the west end of the system.
        /// </summary>
        public int NReplenishmentStationWest = 4;
        /// <summary>
        /// The number of replenishment stations placed at the east end of the system.
        /// </summary>
        public int NReplenishmentStationEast = 0;
        /// <summary>
        /// The number of replenishment stations placed at the south end of the system.
        /// </summary>
        public int NReplenishmentStationSouth = 0;
        /// <summary>
        /// The number of replenishment stations placed at the north end of the system.
        /// </summary>
        public int NReplenishmentStationNorth = 0;
        /// <summary>
        /// The number of elevators placed at the west end of the system.
        /// </summary>
        public int NElevatorsWest = 0;
        /// <summary>
        /// The number of elevators placed at the east end of the system.
        /// </summary>
        public int NElevatorsEast = 0;
        /// <summary>
        /// The number of elevators placed at the south end of the system.
        /// </summary>
        public int NElevatorsSouth = 0;
        /// <summary>
        /// The number of elevators placed at the north end of the system.
        /// </summary>
        public int NElevatorsNorth = 0;

        #endregion

        /// <summary>
        /// Applies values set by an override configuration.
        /// </summary>
        /// <param name="overrideConfig">The override config to apply.</param>
        public void ApplyOverrideConfig(OverrideConfiguration overrideConfig)
        {
            // Return on null config
            if (overrideConfig == null)
                return;
            // Apply values
            if (overrideConfig.OverrideInputStationCount)
                AdjustToOverrideValue((int)((NReplenishmentStationWest + NReplenishmentStationEast + NReplenishmentStationSouth + NReplenishmentStationNorth) * overrideConfig.OverrideInputStationCountValue),
                    ref NReplenishmentStationNorth, ref NReplenishmentStationSouth, ref NReplenishmentStationEast, ref NReplenishmentStationWest);
            if (overrideConfig.OverrideOutputStationCount)
                AdjustToOverrideValue((int)((NPickStationWest + NPickStationEast + NPickStationSouth + NPickStationNorth) * overrideConfig.OverrideOutputStationCountValue),
                    ref NPickStationSouth, ref NPickStationNorth, ref NPickStationWest, ref NPickStationEast);
            if (overrideConfig.OverrideBotCountPerOStation)
                BotCount = overrideConfig.OverrideBotCountPerOStationValue * (NPickStationWest + NPickStationEast + NPickStationSouth + NPickStationNorth);
            if (overrideConfig.OverrideBotPodTransferTime)
                PodTransferTime = overrideConfig.OverrideBotPodTransferTimeValue;
            if (overrideConfig.OverrideBotMaxAcceleration)
                MaxAcceleration = overrideConfig.OverrideBotMaxAccelerationValue;
            if (overrideConfig.OverrideBotMaxDeceleration)
                MaxDeceleration = overrideConfig.OverrideBotMaxDecelerationValue;
            if (overrideConfig.OverrideBotMaxVelocity)
                MaxVelocity = overrideConfig.OverrideBotMaxVelocityValue;
            if (overrideConfig.OverrideBotTurnSpeed)
                TurnSpeed = overrideConfig.OverrideBotTurnSpeedValue;
            if (overrideConfig.OverridePodCapacity)
                PodCapacity = overrideConfig.OverridePodCapacityValue;
            if (overrideConfig.OverrideInputStationCapacity)
                IStationCapacity = overrideConfig.OverrideInputStationCapacityValue;
            if (overrideConfig.OverrideOutputStationCapacity)
                OStationCapacity = overrideConfig.OverrideOutputStationCapacityValue;
            if (overrideConfig.OverrideInputStationItemBundleTransferTime)
                ItemBundleTransferTime = overrideConfig.OverrideInputStationItemBundleTransferTimeValue;
            if (overrideConfig.OverrideOutputStationItemTransferTime)
                ItemTransferTime = overrideConfig.OverrideOutputStationItemTransferTimeValue;
            if (overrideConfig.OverrideOutputStationItemPickTime)
                ItemPickTime = overrideConfig.OverrideOutputStationItemPickTimeValue;
        }

        /// <summary>
        /// Adjusts values to an override target some by equally increasing or decreasing them.
        /// </summary>
        /// <param name="targetValue">The target value.</param>
        /// <param name="firstValue">The first value (this will be modified first).</param>
        /// <param name="secondValue">The first value (this will be modified second).</param>
        /// <param name="thirdValue">The first value (this will be modified third).</param>
        /// <param name="fourthValue">The first value (this will be modified fourth).</param>
        private void AdjustToOverrideValue(int targetValue, ref int firstValue, ref int secondValue, ref int thirdValue, ref int fourthValue)
        {
            if (targetValue < 0)
                throw new ArgumentException("Cannot target a negative value!");
            int currentValue = firstValue + secondValue + thirdValue + fourthValue;
            int currentValueToModify = 0;
            while (currentValue != targetValue)
            {
                if (currentValue < targetValue)
                {
                    switch (currentValueToModify)
                    {
                        case 0: firstValue++; break;
                        case 1: secondValue++; break;
                        case 2: thirdValue++; break;
                        case 3: fourthValue++; break;
                        default: throw new InvalidOperationException("Unknown index!");
                    }
                    currentValue++;
                }
                else if (currentValue > targetValue)
                {
                    switch (currentValueToModify)
                    {
                        case 0: if (firstValue > 0) firstValue--; break;
                        case 1: if (secondValue > 0) secondValue--; break;
                        case 2: if (thirdValue > 0) thirdValue--; break;
                        case 3: if (fourthValue > 0) fourthValue--; break;
                        default: throw new InvalidOperationException("Unknown index!");
                    }
                    currentValue--;
                }
                else
                {
                    throw new InvalidOperationException("Something went wrong while adjusting to target value!");
                }
                // Move on to next value
                currentValueToModify = (currentValueToModify + 1) % 4;
            }
        }

        /// <summary>
        /// Returns a simple layout describing name.
        /// </summary>
        /// <returns>A string that can be used as an instance / layout name.</returns>
        public string GetMetaInfoBasedLayoutName()
        {
            string delimiter = "-";
            return
                TierCount + delimiter +
                (NReplenishmentStationEast + NReplenishmentStationWest + NReplenishmentStationSouth + NReplenishmentStationNorth) + delimiter +
                (NPickStationEast + NPickStationWest + NPickStationSouth + NPickStationNorth) + delimiter +
                BotCount + delimiter +
                PodAmount.ToString(IOConstants.EXPORT_FORMAT_SHORTER, IOConstants.FORMATTER);
        }

        /// <summary>
        /// Checks whether the layout can be generated.
        /// </summary>
        /// <param name="errorMessage">A message describing the error if the layout is not valid.</param>
        /// <returns>Indicates whether the layout is valid.</returns>
        public bool isValid(out String errorMessage)
        {
            if (TierCount <= 0)
            {
                errorMessage = "TierCount <= 0, TierCount: " + TierCount;
                return false;
            }
            if (TierHeight <= 0)
            {
                errorMessage = "TierHeight <= 0, TierHeight: " + TierHeight;
                return false;
            }
            if (BotCount <= 0 || BotCount > maxBots())
            {
                errorMessage = "BotCount <= 0  || BotCount > maxBots(), BotCount: " + BotCount;
                return false;
            }
            if (BotCount > maxUnusedStorageLocations())
            {
                errorMessage = "No sufficient number of free storage locations for resting robots, Storage Locations - Pods > BotCount";
                return false;
            }
            if (BotRadius <= 0 || BotRadius >= 0.5)
            {
                errorMessage = "BotRadius <= 0 || BotRadius >= 0.5, BotRadius: " + BotRadius;
                return false;
            }
            if (MaxAcceleration <= 0)
            {
                errorMessage = "MaxAcceleration <= 0, MaxAcceleration: " + MaxAcceleration;
                return false;
            }
            if (MaxDeceleration <= 0)
            {
                errorMessage = "MaxDeceleration <= 0, MaxDeceleration: " + MaxDeceleration;
                return false;
            }
            if (MaxVelocity <= 0)
            {
                errorMessage = "MaxVelocity <= 0, MaxVelocity: " + MaxVelocity;
                return false;
            }
            if (TurnSpeed <= 0)
            {
                errorMessage = "TurnSpeed <= 0, TurnSpeed: " + TurnSpeed;
                return false;
            }
            if (CollisionPenaltyTime <= 0)
            {
                errorMessage = "CollisionPenaltyTime <= 0, CollisionPenaltyTime: " + CollisionPenaltyTime;
                return false;
            }
            if (PodTransferTime <= 0)
            {
                errorMessage = "PodTransferTime <= 0, PodTransferTime: " + PodTransferTime;
                return false;
            }
            if (PodAmount <= 0 || PodAmount >= 1)
            {
                errorMessage = "PodAmount <= 0 || PodAmount >= 1, PodAmount: " + PodAmount;
                return false;
            }
            if (PodRadius <= 0 || PodRadius >= 0.5)
            {
                errorMessage = "PodRadius <= 0 || PodRadius >= 0.5, PodRadius: " + PodRadius;
                return false;
            }
            if (PodCapacity <= 0)
            {
                errorMessage = "PodCapacity <= 0, PodCapacity: " + PodCapacity;
                return false;
            }
            if (StationRadius <= 0 || StationRadius >= 0.5)
            {
                errorMessage = "StationRadius <= 0 || StationRadius >= 0.5, StationRadius: " + StationRadius;
                return false;
            }
            if (ItemTransferTime <= 0)
            {
                errorMessage = "ItemTransferTime <= 0, ItemTransferTime: " + ItemTransferTime;
                return false;
            }
            if (ItemBundleTransferTime <= 0)
            {
                errorMessage = "ItemBundleTransferTime <= 0, ItemBundleTransferTime: " + ItemBundleTransferTime;
                return false;
            }
            if (IStationCapacity <= 0)
            {
                errorMessage = "IStationCapacity <= 0, IStationCapacity: " + IStationCapacity;
                return false;
            }
            if (OStationCapacity <= 0)
            {
                errorMessage = "OStationCapacity <= 0, OStationCapacity: " + OStationCapacity;
                return false;
            }
            if (ElevatorTransportationTimePerTier <= 0)
            {
                errorMessage = "ElevatorTransportationTimePerTier <= 0, ElevatorTransportationTimePerTier: " + ElevatorTransportationTimePerTier;
                return false;
            }

            if (!AislesTwoDirectional && (NrVerticalAisles % 2 != 0 || NrHorizontalAisles % 2 != 0))
            {
                errorMessage = "!TwoDirectional && (NrVerticalAisles % 2 != 0 || NrHorizontalAisles % 2 != 0), NrVerticalAisles: " + NrVerticalAisles + ", NrHorizontalAisles: " + NrHorizontalAisles;
                return false; //if travel is uni-directional, the number of aisles needs to be even, as well as the number of cross-aisles
            }
            if (WidthBuffer < 1)
            {
                errorMessage = "WidthBuffer < 2, WidthBuffer: " + WidthBuffer;
                return false;
            }
            if (DistanceEntryExitStation < 1 || DistanceEntryExitStation > 2 * VerticalLengthBlock + widthAisles() || DistanceEntryExitStation % 2 != 1)
            {
                errorMessage = "DistanceEntryExitStation < 1 || DistanceEntryExitStation > DistanceEntryExitStation > 2 * VerticalLengthBlock + widthAisles() || DistanceEntryExitStation % 2 != 1, DistanceEntryExitStation: " + DistanceEntryExitStation + ", DistanceEntryExitStation has to be odd and cannot be longer than 5 to ensure it won't interfere with other stations. If you need more buffer / queue, then increase the bufferwidth";
                return false;
            }
            if (WidthHall < 2)
            {
                errorMessage = "WidthHall < 0, WidthHall: " + WidthHall;
                return false;
            }
            if (HorizontalLengthBlock < 1 || HorizontalLengthBlock % 2 != 0)
            {
                errorMessage = "HorizontalLengthBlock < 1 || HorizontalLengthBlock % 2 != 0, HorizontalLengthBlock: " + HorizontalLengthBlock;
                return false;
            }
            if (VerticalLengthBlock != 2)
            {
                errorMessage = "VerticalLengthBlock != 2, VerticalLengthBlock: " + VerticalLengthBlock;
                return false;
            }
            if (NrVerticalAisles < 2)
            {
                errorMessage = "NrVerticalAisles < 2, NrVerticalAisles: " + NrVerticalAisles;
                return false;
            }
            if (NrHorizontalAisles < 2)
            {
                errorMessage = "NrHorizontalAisles < 2, NrHorizontalAisles: " + NrHorizontalAisles;
                return false;
            }
            if (NPickStationWest < 0)
            {
                errorMessage = "NPickStationWest < 0, NPickStationWest: " + NPickStationWest;
                return false;
            }
            if (NPickStationEast < 0)
            {
                errorMessage = "NPickStationEast < 0, NPickStationEast: " + NPickStationEast;
                return false;
            }
            if (NPickStationSouth < 0)
            {
                errorMessage = "NPickStationSouth < 0, NPickStationSouth: " + NPickStationSouth;
                return false;
            }
            if (NPickStationNorth < 0)
            {
                errorMessage = "NPickStationNorth < 0, NPickStationNorth: " + NPickStationNorth;
                return false;
            }
            if (NReplenishmentStationWest < 0)
            {
                errorMessage = "NReplenishmentStationWest < 0, NReplenishmentStationWest: " + NReplenishmentStationWest;
                return false;
            }
            if (NReplenishmentStationEast < 0)
            {
                errorMessage = "NReplenishmentStationEast < 0, NReplenishmentStationEast: " + NReplenishmentStationEast;
                return false;
            }
            if (NReplenishmentStationSouth < 0)
            {
                errorMessage = "NReplenishmentStationSouth < 0, NReplenishmentStationSouth: " + NReplenishmentStationSouth;
                return false;
            }
            if (NReplenishmentStationNorth < 0)
            {
                errorMessage = "NReplenishmentStationNorth < 0, NReplenishmentStationNorth: " + NReplenishmentStationNorth;
                return false;
            }
            if (NElevatorsWest < 0)
            {
                errorMessage = "NElevatorsWest < 0, NElevatorsWest: " + NElevatorsWest;
                return false;
            }
            if (NElevatorsEast < 0)
            {
                errorMessage = "NElevatorsEast < 0, NElevatorsEast: " + NElevatorsEast;
                return false;
            }
            if (NElevatorsSouth < 0)
            {
                errorMessage = "NElevatorsSouth < 0, NElevatorsSouth: " + NElevatorsSouth;
                return false;
            }
            if (NElevatorsNorth < 0)
            {
                errorMessage = "NElevatorsNorth < 0, NElevatorsNorth: " + NElevatorsNorth;
                return false;
            }
            if (NPickStationWest + NPickStationEast + NPickStationSouth + NPickStationNorth < 1)
            {
                errorMessage = "NPickStationWest + NPickStationEast + NPickStationSouth + NPickStationNorth < 1, NPickStationWest + NPickStationEast + NPickStationSouth + NPickStationNorth = " + (NPickStationWest + NPickStationEast + NPickStationSouth + NPickStationNorth);
                return false; //there should always be at least one pick station
            }
            if (NReplenishmentStationWest + NReplenishmentStationEast + NReplenishmentStationSouth + NReplenishmentStationNorth < 1)
            {
                errorMessage = "NReplenishmentStationWest + NReplenishmentStationEast + NReplenishmentStationSouth + NReplenishmentStationNorth < 1, NReplenishmentStationWest + NReplenishmentStationEast + NReplenishmentStationSouth + NReplenishmentStationNorth = " + (NReplenishmentStationWest + NReplenishmentStationEast + NReplenishmentStationSouth + NReplenishmentStationNorth);
                return false; //there should always be at least one replenishment station
            }
            if (NPickStationSouth + NReplenishmentStationSouth + NElevatorsSouth > maxNrOfStationsNorthOrSouth())
            {
                errorMessage = "NPickStationSouth + NReplenishmentStationSouth > calculateMaxNumberOfStationsPossibleNorthOrSouth()";
                return false;
            }
            if (NPickStationNorth + NReplenishmentStationNorth + NElevatorsNorth > maxNrOfStationsNorthOrSouth())
            {
                errorMessage = "NPickStationNorth + NReplenishmentStationNorth > calculateMaxNumberOfStationsPossibleNorthOrSouth()";
                return false;
            }
            if (NPickStationWest + NReplenishmentStationWest + NElevatorsWest > maxNrOfStationsWestOrEast())
            {
                errorMessage = "NPickStationWest + NReplenishmentStationWest > calculateMaxNumberOfStationsPossibleEastOrWest()";
                return false;
            }
            if (NPickStationEast + NReplenishmentStationEast + NElevatorsEast > maxNrOfStationsWestOrEast())
            {
                errorMessage = "NPickStationEast + NReplenishmentStationEast > calculateMaxNumberOfStationsPossibleEastOrWest()";
                return false;
            }

            if (TierCount > 1 && (NElevatorsNorth + NElevatorsSouth + NElevatorsEast + NElevatorsWest == 0))
            {
                errorMessage = "TierCount > 1 but no elevators to transport pods between tiers";
                return false;
            }

            if (TierCount == 1 && (NElevatorsNorth + NElevatorsSouth + NElevatorsEast + NElevatorsWest > 0))
            {
                errorMessage = "TierCount == 1 so no elevators are needed, but NElevatorsNorth + NElevatorsSouth + NElevatorsEast + NElevatorsWest > 0";
                return false;
            }

            errorMessage = "";
            return true;
        }

        internal int maxNrOfStationsNorthOrSouth()
        {
            return NrVerticalAisles / 2;
        }

        internal int maxNrOfStationsWestOrEast()
        {
            return NrHorizontalAisles / 2;
        }

        internal bool hasStationsNorth()
        {
            return NPickStationNorth > 0 || NReplenishmentStationNorth > 0 || NElevatorsNorth > 0;
        }

        internal bool hasStationsSouth()
        {
            return NPickStationSouth > 0 || NReplenishmentStationSouth > 0 || NElevatorsSouth > 0;
        }

        internal bool hasStationsEast()
        {
            return NPickStationEast > 0 || NReplenishmentStationEast > 0 || NElevatorsEast > 0;
        }

        internal bool hasStationsWest()
        {
            return NPickStationWest > 0 || NReplenishmentStationWest > 0 || NElevatorsWest > 0;
        }

        internal int widthTier()
        {
            int width = widthStorageArea();
            if (hasStationsNorth())
            {
                width += WidthHall + WidthBuffer;
            }
            if (hasStationsSouth())
            {
                width += WidthHall + WidthBuffer;
            }
            return width;
        }

        internal int lengthTier()
        {
            int length = lengthStorageArea();
            if (hasStationsEast())
            {
                length += WidthHall + WidthBuffer;
            }
            if (hasStationsWest())
            {
                length += WidthHall + WidthBuffer;
            }
            return length;
        }

        internal int lengthStorageArea()
        {
            int lengthDueToStorageLocations = (NrVerticalAisles + 1) * HorizontalLengthBlock;
            int lengthDueToAisles = widthAisles() * NrVerticalAisles;
            int lengthDueToRingwayAroundStorageArea = 2 * WidthRingway;
            return lengthDueToStorageLocations + lengthDueToAisles + lengthDueToRingwayAroundStorageArea;
        }

        internal int widthStorageArea()
        {
            int lengthDueToStorageLocations = (NrHorizontalAisles + 1) * VerticalLengthBlock;
            int lengthDueToAisles = widthAisles() * NrHorizontalAisles;
            int lengthDueToRingwayAroundStorageArea = 2 * WidthRingway;
            return lengthDueToStorageLocations + lengthDueToAisles + lengthDueToRingwayAroundStorageArea;
        }

        internal int minDistanceExits()
        {
            return VerticalLengthBlock + widthAisles();
        }

        internal int widthAisles()
        {
            return SingleLane ? 1 : 2;
        }

        internal int distanceBetweenPossibleExitLocationsAtNorthOrSouthHall()
        {
            int distance = HorizontalLengthBlock + widthAisles();
            return 2 * distance;
        }

        internal int distanceBetweenPossibleExitLocationsAtWestOrEastHall()
        {
            int distance = VerticalLengthBlock + widthAisles();
            return 2 * distance;
        }

        internal int maxBots()
        {
            //in theory there is room for more robots, but this seems like a sensible maximum
            return lengthStorageArea() * widthStorageArea() - nStorageBlocks() * nStorageLocationsPerBlock();
        }

        internal int maxUnusedStorageLocations()
        {
            //this still underlies some random influence as 
            return (int)((1 - PodAmount) * nStorageLocationsPerBlock() * nStorageBlocks());
        }

        internal int nEntrancesPerStation()
        {
            return (DistanceEntryExitStation + 1) / 2;
        }

        internal int nEntrancesStationsWest()
        {
            return nStationsWest() * nEntrancesPerStation();
        }

        internal int nEntrancesStationsEast()
        {
            return nStationsEast() * nEntrancesPerStation();
        }

        internal int nEntrancesStationsNorth()
        {
            return nStationsNorth() * nEntrancesPerStation();
        }

        internal int nEntrancesStationsSouth()
        {
            return nStationsSouth() * nEntrancesPerStation();
        }

        internal int nStationsWest()
        {
            return NPickStationWest + NReplenishmentStationWest + NElevatorsWest;
        }

        internal int nStationsEast()
        {
            return NPickStationEast + NReplenishmentStationEast + NElevatorsEast;
        }

        internal int nStationsNorth()
        {
            return NPickStationNorth + NReplenishmentStationNorth + NElevatorsNorth;
        }

        internal int nStationsSouth()
        {
            return NPickStationSouth + NReplenishmentStationSouth + NElevatorsSouth;
        }

        internal int nStations()
        {
            return nStationsWest() + nStationsEast() + nStationsNorth() + nStationsSouth();
        }

        internal int nStorageBlocks()
        {
            return (NrHorizontalAisles + 1) * (NrVerticalAisles + 1);
        }

        internal int nStorageLocationsPerBlock()
        {
            return HorizontalLengthBlock * VerticalLengthBlock;
        }
    }
}
