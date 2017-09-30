using RAWSimO.Core.Items;
using RAWSimO.Core.Management;
using RAWSimO.Core.Randomization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Configurations
{
    /// <summary>
    /// The configuration for inventory related stuff.
    /// </summary>
    public class InventoryConfiguration
    {
        /// <summary>
        /// The type of the items to use.
        /// </summary>
        public ItemType ItemType = ItemType.SimpleItem;

        /// <summary>
        /// The order mode to use when simulating.
        /// </summary>
        public OrderMode OrderMode = OrderMode.Fill;

        /// <summary>
        /// Indicates whether the batches defined by the batch-configuration will be submitted or not.
        /// </summary>
        public bool SubmitBatches = false;

        /// <summary>
        /// The relative amount of already filled inventory when starting the simulation. Inventory is filled randomly.
        /// </summary>
        public double InitialInventory = 0.7;

        /// <summary>
        /// Specifies whether new bundles are generated even if the system exceeded the load specified by the bundle buffer.
        /// </summary>
        public bool IgnoreCapacityForBundleGeneration = false;

        /// <summary>
        /// Specifies a maximal amount of bundles that are additionally generated (when the storage area is at its limit) in relation to the overall storage capacity.
        /// </summary>
        public double BufferBundlesUntilInventoryLoad = 1.1;

        /// <summary>
        /// The number of orders generated to warmup methods using these.
        /// </summary>
        public int WarmupOrderCount = 5000;

        /// <summary>
        /// The minimal weight of one item.
        /// </summary>
        public double ItemWeightMin = 5;
        /// <summary>
        /// The maximal weight of one item.
        /// </summary>
        public double ItemWeightMax = 5;

        /// <summary>
        /// The minimal number of items in one bundle.
        /// </summary>
        public int BundleSizeMin = 6;
        /// <summary>
        /// The maximal number of items in one bundle.
        /// </summary>
        public int BundleSizeMax = 6;

        /// <summary>
        /// The probability for a return order to happen, i.e. a bundle is generated with size 1.
        /// </summary>
        public double ReturnOrderProbability = 0;

        /// <summary>
        /// The mean to use for determining the number of items per positions of an order by using a normal distribution.
        /// </summary>
        public double PositionCountMean = 1;
        /// <summary>
        /// The standard deviation to use for determining the number of items per positions of an order by using a normal distribution.
        /// </summary>
        public double PositionCountStdDev = 0.3;
        /// <summary>
        /// The minimal number of items per line of an order.
        /// </summary>
        public int PositionCountMin = 1;
        /// <summary>
        /// The maximal number of items per line of an order.
        /// </summary>
        public int PositionCountMax = 3;

        /// <summary>
        /// The mean to use for determining the number of lines of an order by using a normal distribution.
        /// </summary>
        public double OrderPositionCountMean = 1;
        /// <summary>
        /// The standard deviation to use for determining the number of lines of an order by using a normal distribution.
        /// </summary>
        public double OrderPositionCountStdDev = 1;
        /// <summary>
        /// The minimal number of lines for an order (inclusive bound).
        /// </summary>
        public int OrderPositionCountMin = 1;
        /// <summary>
        /// The maximal number of lines for an order (inclusive bound).
        /// </summary>
        public int OrderPositionCountMax = 4;

        /// <summary>
        /// Indicates that due times shall be generated to emulate priority mode, i.e. a shorter due time is selected with the given probability.
        /// </summary>
        public bool DueTimePriorityMode = true;
        /// <summary>
        /// The probability for generating a priority order.
        /// </summary>
        public double DueTimePriorityOrderProbability = 0.2;
        /// <summary>
        /// The due time of a priority order.
        /// </summary>
        public double DueTimePriorityOrder = TimeSpan.FromMinutes(30).TotalSeconds;
        /// <summary>
        /// The due time of an ordinary order.
        /// </summary>
        public double DueTimeOrdinaryOrder = TimeSpan.FromHours(2).TotalSeconds;
        /// <summary>
        /// The mean time the due time is set seen from the generation timestamp of the order, e.g.: if the order is generated at 1 o'clock a mean of 3 hours due time will be at 4 o'clock.
        /// </summary>
        public double DueTimeOffsetMean = TimeSpan.FromHours(3).TotalSeconds;
        /// <summary>
        /// The standard deviation of the due time.
        /// </summary>
        public double DueTimeOffsetStdDev = TimeSpan.FromMinutes(15).TotalSeconds;
        /// <summary>
        /// The minimal time the due time is set seen from the generation timestamp of the order
        /// </summary>
        public double DueTimeOffsetMin = TimeSpan.FromHours(2).TotalSeconds;
        /// <summary>
        /// The maximal time the due time is set seen from the generation timestamp of the order
        /// </summary>
        public double DueTimeOffsetMax = TimeSpan.FromHours(4).TotalSeconds;

        /// <summary>
        /// The configuration for generating orders and inventory in a demand based mode.
        /// </summary>
        public DemandInventoryConfiguration DemandInventoryConfiguration = new DemandInventoryConfiguration();

        /// <summary>
        /// The configuration for reading orders and inventory from a given file.
        /// </summary>
        public FixedInventoryConfiguration FixedInventoryConfiguration;

        /// <summary>
        /// The configuration for generating orders and inventory using a poisson process.
        /// </summary>
        public PoissonInventoryConfiguration PoissonInventoryConfiguration;

        /// <summary>
        /// The configuration for generating orders and bundles as batches.
        /// </summary>
        public BatchInventoryConfiguration BatchInventoryConfiguration;

        /// <summary>
        /// The configuration to use when in colored word mode.
        /// </summary>
        public ColoredWordConfiguration ColoredWordConfiguration;

        /// <summary>
        /// The configuration to use when in simple item mode.
        /// </summary>
        public SimpleItemConfiguration SimpleItemConfiguration = new SimpleItemConfiguration();

        /// <summary>
        /// Checks whether the inventory configuration is valid.
        /// </summary>
        /// <param name="errorMessage">A message describing the error if the inventory is not valid.</param>
        /// <param name="podCapacity">The corresponding pod capacity. </param>
        /// <returns>Indicates whether the inventory configuration is valid.</returns>
        public bool isValid(double podCapacity, out String errorMessage)
        {
            double initialWeight = 0;

            if (ItemWeightMin > ItemWeightMax)
            {
                errorMessage = "ItemWeightMin has to be <= ItemWeightMax ";
                return false;
            }
            if (BundleSizeMin > BundleSizeMax)
            {
                errorMessage = "BundleSizeMin has to be <= BundleSizeMax";
                return false;
            }
            if (PositionCountMin > PositionCountMax)
            {
                errorMessage = "PositionCountMin has to be <= PositionCountMax";
                return false;
            }
            if (OrderPositionCountMin > OrderPositionCountMax)
            {
                errorMessage = "OrderPositionCountMin hat to be <= OrderPositionCountMax";
                return false;
            }
            if (ItemWeightMax <= 0 || ItemWeightMin <= 0)
            {
                errorMessage = "ItemWeightMax or ItemWeightMin is <= 0. They both need to be > 0";
                return false;
            }
            if (BundleSizeMax < 1 || BundleSizeMin < 1)
            {
                errorMessage = "BundleSizeMax or BundleSizeMin is <1. They both need to be > =1";
                return false;
            }
            if (OrderPositionCountMean < 1 || OrderPositionCountMax < 1)
            {
                errorMessage = "OdrderPositionCountMean and OrderPositionCountMax need to be >= 1";
                return false;
            }
            if (OrderPositionCountMin < 0)
            {
                errorMessage = "OrderPositionCountMin needs to be >= 0 ";
                return false;
            }
            if (PositionCountMean < 1 || PositionCountMax < 1)
            {
                errorMessage = "PositionCountMean and PositionCountMax need to be >= 1";
                return false;
            }
            if (PositionCountMin < 0)
            {
                errorMessage = "PositionCountMin needs to be >= 0 ";
                return false;
            }
            if (DemandInventoryConfiguration.InventoryLevelBundleRestartThreshold < 0)
            {
                errorMessage = "InventoryLevelRestartThreshold needs to be >= 0";
                return false;
            }
            if (DemandInventoryConfiguration.InventoryLevelBundleStopThreshold <= 0)
            {
                errorMessage = "InventoryLevelRestartThreshold needs to be >= 0";
                return false;
            }
            if (DemandInventoryConfiguration.InventoryLevelOrderRestartThreshold < 0)
            {
                errorMessage = "InventoryLevelOrderRestartThreshold needs to be >= 0";
                return false;
            }
            if (DemandInventoryConfiguration.InventoryLevelOrderStopThreshold <= 0)
            {
                errorMessage = "InventoryLevelOrderStopThreshold needs to be >= 0";
                return false;
            }

            if (ItemType == ItemType.Letter && ColoredWordConfiguration == null)
            {
                errorMessage = "If you want to use the ItemType Letter, please initialize ColoredWordConfiguration";
                return false;
            }
            if (OrderMode == OrderMode.Poisson && PoissonInventoryConfiguration == null)
            {
                errorMessage = "If you want to use the OrderMode Poisson, please initialize PoissonInventoryConfiguration";
                return false;
            }
            if (OrderMode == OrderMode.Fixed && FixedInventoryConfiguration == null)
            {
                errorMessage = "If you want to use the OrderMode Fixed, please initialize FixedInventoryConfiguration";
                return false;
            }
            if (SubmitBatches == true && BatchInventoryConfiguration == null)
            {
                errorMessage = "If SubmitBatches==true, BatcheInventoryConfiguration must be initialized";
                return false;
            }

            if (podCapacity <= ItemWeightMax * BundleSizeMax)
            {
                errorMessage = "Pod Capacity is too small for Bundles. Please increase PodCapacity or scale down ItemWeightMax / BundleSizeMax.";
                return false;
            }

            while (initialWeight < podCapacity * InitialInventory)
            {
                initialWeight += (ItemWeightMin * BundleSizeMax);
            }

            if (podCapacity < initialWeight)
            {
                errorMessage = "InitialInventory has been choosen too heigh. Please scale down ItemWeightMax, BundleSizeMax or InitalInventory.";
                return false;
            }


            errorMessage = "";
            return true;
        }

        /// <summary>
        /// Generates a ColoredWordConfiguration or PoissonInventoryConfiguration with default parameters if need be.
        /// </summary>
        public void autogenerate()
        {
            if (ItemType == ItemType.Letter && ColoredWordConfiguration == null)
            {
                DefaultConstructorIdentificationClass p = new DefaultConstructorIdentificationClass();
                ColoredWordConfiguration = new ColoredWordConfiguration(p);
            }
            if (OrderMode == OrderMode.Poisson && PoissonInventoryConfiguration == null)
            {
                DefaultConstructorIdentificationClass p = new DefaultConstructorIdentificationClass();
                PoissonInventoryConfiguration = new PoissonInventoryConfiguration(p);
            }
        }
    }


    /// <summary>
    /// The configuration for generating orders and inventory using a poisson process.
    /// </summary>
    public class PoissonInventoryConfiguration
    {
        /// <summary>
        /// Parameter-less constructor mainly used by the xml-serializer.
        /// </summary>
        public PoissonInventoryConfiguration() { }
        /// <summary>
        /// Constructor that generates default values for all fields.
        /// </summary>
        /// <param name="param">Not used.</param>
        public PoissonInventoryConfiguration(DefaultConstructorIdentificationClass param) : this()
        {
            TimeDependentOrderWeights = new List<Skvp<double, double>>() {
                new Skvp<double, double>() { Key = TimeSpan.FromHours(0).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 20) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(1).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 10) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(2).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 5) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(3).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 5) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(4).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 10) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(5).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 10) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(6).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 20) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(7).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 20) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(8).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 40) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(9).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 80) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(10).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 80) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(11).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 90) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(12).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 110) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(13).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 80) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(14).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 90) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(15).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 130) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(16).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 180) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(17).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 120) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(18).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 190) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(19).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 250) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(20).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 220) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(21).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 150) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(22).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 110) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(23).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 50) },
            };
            TimeDependentBundleWeights = new List<Skvp<double, double>>() {
                new Skvp<double, double>() { Key = TimeSpan.FromHours(0).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 20) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(1).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 40) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(2).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 80) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(3).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 80) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(4).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 90) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(5).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 110) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(6).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 80) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(7).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 90) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(8).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 130) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(9).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 180) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(10).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 120) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(11).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 190) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(12).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 250) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(13).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 220) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(14).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 150) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(15).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 110) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(16).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 50) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(17).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 20) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(18).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 10) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(19).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 5) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(20).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 5) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(21).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 10) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(22).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 10) },
                new Skvp<double, double>() { Key = TimeSpan.FromHours(23).TotalSeconds, Value = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromMinutes(60), 20) },
            };
        }

        /// <summary>
        /// The mode of the poisson generator if used.
        /// </summary>
        public PoissonMode PoissonMode = PoissonMode.TimeDependent;
        /// <summary>
        /// The initial number of orders ready to be allocated.
        /// </summary>
        public int InitialOrderCount = 30;
        /// <summary>
        /// The initial number of bundles ready to be allocated.
        /// </summary>
        public int InitialBundleCount = 30;
        /// <summary>
        /// Can be used to set the type of information used to distort the rate parameter during simulation execution.
        /// </summary>
        public PoissonDistortionType DistortOrderRateParameter = PoissonDistortionType.None;
        /// <summary>
        /// Can be used to set the type of information used to distort the rate parameter during simulation execution.
        /// </summary>
        public PoissonDistortionType DistortBundleRateParameter = PoissonDistortionType.None;
        /// <summary>
        /// The number of orders generated in one hour per station that the poisson process is aiming for. This number will be used to calculate an instance-wide rate in all poisson modes.
        /// </summary>
        public double AverageOrdersPerHourAndStation = 160;
        /// <summary>
        /// The number of bundles generated in one hour per station that the poisson process is aiming for. This number will be used to calculate an instance-wide rate in all poisson modes.
        /// </summary>
        public double AverageBundlesPerHourAndStation = 45;
        /// <summary>
        /// The number of orders generated in one hour per station that the poisson process is aiming for. This number will be used to calculate an instance-wide rate in for the high-low mode in high times.
        /// </summary>
        public double AverageOrdersPerHourAndStationHigh = 210;
        /// <summary>
        /// The number of bundles generated in one hour per station that the poisson process is aiming for. This number will be used to calculate an instance-wide rate in for the high-low mode in high times.
        /// </summary>
        public double AverageBundlesPerHourAndStationHigh = 75;
        /// <summary>
        /// The number of switches in average to go from low to high in one hour.
        /// This is used to determine the rate parameter, but seems more intuitive than immediately specifying the rate.
        /// In contrast to this description the rate of course immediately changes to the one to go from high to low, hence, no multiple events will be generated using this parameter but only the first one.
        /// </summary>
        public double LowToHighSwitchesPerHour = 0.5;
        /// <summary>
        /// The number of switches in average to go from high to low in one hour.
        /// This is used to determine the rate parameter, but seems more intuitive than immediately specifying the rate.
        /// In contrast to this description the rate of course immediately changes to the one to go from low to high, hence, no multiple events will be generated using this parameter but only the first one.
        /// </summary>
        public double HighToLowSwitchesPerHour = 2;
        /// <summary>
        /// Indicates whether the generation of bundles is affected during the high period, i.e. more bundles are generated during the high period.
        /// </summary>
        public bool BundleGenAffectedByHighPeriod = false;
        /// <summary>
        /// Indicates whether the generation of orders is affected during the high period, i.e. more orders are generated during the high period.
        /// </summary>
        public bool OrderGenAffectedByHighPeriod = true;
        /// <summary>
        /// The time after which the rate value of the poisson process for orders loops (in inhomogeneous poisson mode).
        /// </summary>
        public double MaxTimeForTimeDependentOrderRates = TimeSpan.FromDays(1).TotalSeconds;
        /// <summary>
        /// The time after which the rate value of the poisson process for bundles loops (in inhomogeneous poisson mode).
        /// </summary>
        public double MaxTimeForTimeDependentBundleRates = TimeSpan.FromDays(1).TotalSeconds;
        /// <summary>
        /// The weights of the rates for the orders depending on the current time. 
        /// One element specifies the weight of a rate as a value and the time in seconds (starting at 0). 
        /// The rate weight then is used starting from the time given as the key until another KVP specifies a new rate weight.
        /// </summary>
        public List<Skvp<double, double>> TimeDependentOrderWeights;
        /// <summary>
        /// The weights of the rates for the bundles depending on the current time. 
        /// One element specifies the weight of a rate as a value and the time in seconds (starting at 0). 
        /// The rate weight then is used starting from the time given as the key until another KVP specifies a new rate weight.
        /// </summary>
        public List<Skvp<double, double>> TimeDependentBundleWeights;
    }

    /// <summary>
    /// The configuration for reading orders and inventory from a given file.
    /// </summary>
    public class FixedInventoryConfiguration
    {
        /// <summary>
        /// The file containing the list of orders. This is not necessary in case randomly generated orders are used.
        /// </summary>
        public string OrderFile;
    }

    /// <summary>
    /// The configuration for generating orders and inventory in a demand based mode.
    /// </summary>
    public class DemandInventoryConfiguration
    {
        /// <summary>
        /// The order count to have available (in demand-mode).
        /// </summary>
        public int OrderCount = 200;
        /// <summary>
        /// The bundle count to have available (in demand-mode).
        /// </summary>
        public int BundleCount = 200;
        /// <summary>
        /// Indicates whether inventory level is tracked and bundles are only generated when below certain threshold.
        /// </summary>
        public bool InventoryLevelDrivenBundleGeneration = true;
        /// <summary>
        /// The inventory level threshold after exceeding which bundle generation will get deactivated.
        /// </summary>
        public double InventoryLevelBundleStopThreshold = 0.85;
        /// <summary>
        /// The inventory level threshold after dropping below which bundle generation will be activated again.
        /// </summary>
        public double InventoryLevelBundleRestartThreshold = 0.65;
        /// <summary>
        /// Indicates whether inventory level is tracked and order generation is paused as soon as inventory drops below a certain threshold.
        /// </summary>
        public bool InventoryLevelDrivenOrderGeneration = true;
        /// <summary>
        /// The inventory level threshold after dropping below which order generation will get deactivated.
        /// </summary>
        public double InventoryLevelOrderStopThreshold = 0.1;
        /// <summary>
        /// The inventory level threshold after exceeding which order generation will get activated again.
        /// </summary>
        public double InventoryLevelOrderRestartThreshold = 0.6;
        /// <summary>
        /// A sub configuration specifying down periods for order and bundle generation.
        /// </summary>
        public DemandDownPeriodConfiguration DownPeriodConfiguration = null;
    }

    /// <summary>
    /// A sub configuration specifying down periods for order and bundle generation.
    /// </summary>
    public class DemandDownPeriodConfiguration
    {
        /// <summary>
        /// Parameter-less constructor mainly used by the xml-serializer.
        /// </summary>
        public DemandDownPeriodConfiguration() { }
        /// <summary>
        /// Constructor that generates default values for all fields.
        /// </summary>
        /// <param name="param">Not used.</param>
        public DemandDownPeriodConfiguration(DefaultConstructorIdentificationClass param) : this()
        {
            // Default values
            OrderDownAndUpTimes = new List<Skvp<double, bool>>()
            {
                new Skvp<double, bool>() { Key = TimeSpan.FromHours(0).TotalSeconds, Value = true },
                new Skvp<double, bool>() { Key = TimeSpan.FromHours(6).TotalSeconds, Value = false },
                new Skvp<double, bool>() { Key = TimeSpan.FromHours(22).TotalSeconds, Value = true },
            };
            BundleDownAndUpTimes = new List<Skvp<double, bool>>()
            {
                new Skvp<double, bool>() { Key = TimeSpan.FromHours(0).TotalSeconds, Value = true },
                new Skvp<double, bool>() { Key = TimeSpan.FromHours(6).TotalSeconds, Value = false },
                new Skvp<double, bool>() { Key = TimeSpan.FromHours(22).TotalSeconds, Value = true },
            };
        }
        /// <summary>
        /// The time after which the timepoints for order down periods loop.
        /// </summary>
        public double MaxTimeForOrderDownAndUpPeriods = TimeSpan.FromDays(1).TotalSeconds;
        /// <summary>
        /// The time after which the timepoints for bundle down periods loop.
        /// </summary>
        public double MaxTimeForBundleDownAndUpPeriods = TimeSpan.FromDays(1).TotalSeconds;
        /// <summary>
        /// The timepoints at which order generation is paused or reactivated again. <code>true</code> indicates a starting down period, <code>false</code> a starting normal period.
        /// These have to be ordered by their timestamp.
        /// If no element with timepoint 0 is given, it is assumed that generation is activated at the begin of the period.
        /// </summary>
        public List<Skvp<double, bool>> OrderDownAndUpTimes;
        /// <summary>
        /// The timepoints at which bundle generation is paused or reactivated again. <code>true</code> indicates a starting down period, <code>false</code> a starting normal period.
        /// These have to be ordered by their timestamp.
        /// If no element with timepoint 0 is given, it is assumed that generation is activated at the begin of the period.
        /// </summary>
        public List<Skvp<double, bool>> BundleDownAndUpTimes;
    }

    /// <summary>
    /// The configuration for submitting big batches of orders or bundles at specific timepoints.
    /// </summary>
    public class BatchInventoryConfiguration
    {
        /// <summary>
        /// Parameter-less constructor mainly used by the xml-serializer.
        /// </summary>
        public BatchInventoryConfiguration() { }
        /// <summary>
        /// Constructor that generates default values for all fields.
        /// </summary>
        /// <param name="param">Not used.</param>
        public BatchInventoryConfiguration(DefaultConstructorIdentificationClass param) : this()
        {
            // Once per day completely fill the inventory
            BundleBatches = new List<Skvp<double, double>>() { new Skvp<double, double>() { Key = TimeSpan.FromHours(7).TotalSeconds, Value = 0.9 } };
            // Once per day have a whole bunch of orders
            OrderBatches = new List<Skvp<double, int>>() { new Skvp<double, int>() { Key = TimeSpan.FromHours(10).TotalSeconds, Value = 400 } };
        }
        /// <summary>
        /// The time after which the timepoints for order batch submissions loop.
        /// </summary>
        public double MaxTimeForOrderSubmissions = TimeSpan.FromDays(1).TotalSeconds;
        /// <summary>
        /// The time after which the timepoints for bundle batch submissions loop.
        /// </summary>
        public double MaxTimeForBundleSubmissions = TimeSpan.FromDays(1).TotalSeconds;
        /// <summary>
        /// The timepoints at which orders shall be submitted as a batch and also the number of orders to have in backlog per station after submitting the batch.
        /// These have to be ordered by their timestamp.
        /// Orders will be generated until the respective number of orders is available in the backlog times the station count of the instance.
        /// </summary>
        public List<Skvp<double, int>> OrderBatches;
        /// <summary>
        /// The timepoints at which bundles shall be submitted as a batch and also the aimed for inventory level after submitting the batch.
        /// These have to be ordered by their timestamp.
        /// Bundles will be generated until there are enough bundles in backlog and the system as well such that the respective inventory level is reached.
        /// </summary>
        public List<Skvp<double, double>> BundleBatches;
    }
}
