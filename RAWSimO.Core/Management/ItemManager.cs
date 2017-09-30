using RAWSimO.Core.Configurations;
using RAWSimO.Core.Control;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Info;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.IO;
using RAWSimO.Core.Items;
using RAWSimO.Core.Randomization;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Management
{
    /// <summary>
    /// The manager class that handles all bundle, order and item generation during simulation.
    /// </summary>
    public class ItemManager : IItemManagerInfo
    {
        #region Constructors

        /// <summary>
        /// Constructs a new item manager object for the given instance.
        /// </summary>
        /// <param name="instance">The instance this item manager belongs to.</param>
        public ItemManager(Instance instance)
        {
            Instance = instance;
            // Setup color probabilities for fast access (if available)
            if (instance.SettingConfig.InventoryConfiguration.ItemType == ItemType.Letter)
                _colorProbabilities = Instance.SettingConfig.InventoryConfiguration.ColoredWordConfiguration.ColorProbabilities.ToDictionary(k => k.Key, v => v.Value);
            // Initialize the chosen mode
            InitializeMode();
            // Warmup item frequencies for methods using the information
            WarmupItemFrequencies();
            // Fill inventory and generate initial lists of orders and bundles
            InitializeInventoryAndBundlesAndOrders();
        }

        #endregion

        #region Core fields

        /// <summary>
        /// All possible item description. Hence, this is the complete list of all possible simulated products that can be generated.
        /// </summary>
        private List<ItemDescription> _itemDescriptions = new List<ItemDescription>();
        /// <summary>
        /// The probabilities for all item-descriptions.
        /// </summary>
        private VolatileIDDictionary<ItemDescription, double> _itemDescriptionProbabilities;
        /// <summary>
        /// The maximal probability of all item-descriptions.
        /// </summary>
        private double _itemDescriptionProbabilityMax;
        /// <summary>
        /// The poisson generator used to emulate realistic order generation frequencies.
        /// </summary>
        private PoissonGenerator OrderPoissonGenerator;
        /// <summary>
        /// The poisson generator used to emulate realistic bundle generation frequencies.
        /// </summary>
        private PoissonGenerator BundlePoissonGenerator;
        /// <summary>
        /// The next timestamp at which an order is generated when in poisson mode.
        /// </summary>
        private double _nextPoissonOrderGenerationTime;
        /// <summary>
        /// The next timestamp at which a bundle is generated when in poisson mode.
        /// </summary>
        private double _nextPoissonBundleGenerationTime;
        /// <summary>
        /// The instance this manager belongs to.
        /// </summary>
        Instance Instance { get; set; }
        /// <summary>
        /// A list containing all future orders that aren't placed yet. (Only available in fixed order mode.)
        /// </summary>
        private List<Order> _futureOrders = new List<Order>();
        /// <summary>
        /// The set of all pending orders.
        /// </summary>
        private HashSet<Order> _availableOrders = new HashSet<Order>();
        /// <summary>
        /// Gets the current number of orders in backlog.
        /// </summary>
        internal int BacklogOrderCount { get { return _availableOrders.Count; } }
        /// <summary>
        /// The list of all currently assigned orders.
        /// </summary>
        private List<Order> _openOrders = new List<Order>();
        /// <summary>
        /// The list of all already completed orders.
        /// </summary>
        private List<Order> _completedOrders = new List<Order>();
        /// <summary>
        /// A list containing all future bundles that aren't placed yet. (Only available in fixed bundle mode.)
        /// </summary>
        private List<ItemBundle> _futureBundles = new List<ItemBundle>();
        /// <summary>
        /// The set of all pending bundles.
        /// </summary>
        private HashSet<ItemBundle> _availableBundles = new HashSet<ItemBundle>();
        /// <summary>
        /// Gets the current number of bundles in backlog.
        /// </summary>
        internal int BacklogBundleCount { get { return _availableBundles.Count; } }
        /// <summary>
        /// The list of all currently assigned bundles.
        /// </summary>
        private List<ItemBundle> _openBundles = new List<ItemBundle>();
        /// <summary>
        /// The list of all already completed bundles.
        /// </summary>
        private List<ItemBundle> _completedBundles = new List<ItemBundle>();

        #endregion

        #region Fill mode specific fields

        /// <summary>
        /// When failing to generate suitable orders the order generation is blocked for this time.
        /// </summary>
        private const double ORDER_GENERATION_TIMEOUT = 60;
        /// <summary>
        /// When failing to generate suitable bundles the bundle generation is blocked for this time.
        /// </summary>
        private const double BUNDLE_GENERATION_TIMEOUT = 60;
        /// <summary>
        /// The timestamp in simulation time up until which the generation of orders is temporarily blocked.
        /// </summary>
        private double _orderGenerationBlockedUntil = 0;
        /// <summary>
        /// The timestamp in simulation time up until which the generation of bundles is temporarily blocked.
        /// </summary>
        private double _bundleGenerationBlockedUntil = 0;
        /// <summary>
        /// Indicates whether order generation is currently blocked by too much inventory.
        /// </summary>
        private bool _orderGenerationBlockedByInventoryLevel = false;
        /// <summary>
        /// Indicates whether bundle generation is currently blocked by too much inventory.
        /// </summary>
        private bool _bundleGenerationBlockedByInventoryLevel = false;
        /// <summary>
        /// Information about the demand for the different items. This is used when generating item-bundles according to the recently submitted orders.
        /// </summary>
        private Dictionary<ItemDescription, int> _itemDemandInformation = new Dictionary<ItemDescription, int>();

        #endregion

        #region Batch submission specific fields

        /// <summary>
        /// The next time the bundle batch timepoints cycle.
        /// </summary>
        private double _batchNextBundleCycle;
        /// <summary>
        /// The next time the order batch timepoints cycle.
        /// </summary>
        private double _batchNextOrderCycle;
        /// <summary>
        /// All times at which bundle batches have to be generated with their respective amount information.
        /// </summary>
        private List<Tuple<double, double>> _batchTimepointsBundles;
        /// <summary>
        /// All times at which order batches have to be generated with their respective amount information.
        /// </summary>
        private List<Tuple<double, int>> _batchTimepointsOrders;

        #endregion

        #region Down period specific fields

        /// <summary>
        /// Indicates whether down periods must be handled.
        /// </summary>
        private bool _downPeriodHandling = false;
        /// <summary>
        /// The next time the down period timepoints for bundles cycle.
        /// </summary>
        private double _downPeriodNextBundleCycle;
        /// <summary>
        /// The next time the down period timepoints for orders cycle.
        /// </summary>
        private double _downPeriodNextOrderCycle;
        /// <summary>
        /// All times at which the down period for bundle generation changes.
        /// </summary>
        private List<Tuple<double, bool>> _downPeriodTimepointsBundles;
        /// <summary>
        /// All times at which the down period for order generation changes.
        /// </summary>
        private List<Tuple<double, bool>> _downPeriodTimepointsOrders;
        /// <summary>
        /// Indicates whether there is an active down period for bundle generation.
        /// </summary>
        private bool _downPeriodActiveForBundles = false;
        /// <summary>
        /// Indicates whether there is an active down period for order generation.
        /// </summary>
        private bool _downPeriodActiveForOrders = false;

        #endregion

        #region Colored word specific fields

        /// <summary>
        /// The list of possible orders to generate when in random order mode. (The letters have to be colored by the specified probabilities)
        /// </summary>
        private string[] _baseWords;

        /// <summary>
        /// Contains all item-description objects by their characteristics.
        /// </summary>
        private MultiKeyDictionary<char, LetterColors, ItemDescription> _itemDescriptionsByValue = new MultiKeyDictionary<char, LetterColors, ItemDescription>();

        /// <summary>
        /// The probabilities of the different colors for fast access.
        /// </summary>
        private Dictionary<LetterColors, double> _colorProbabilities;

        #endregion

        #region Simple item specific fields

        /// <summary>
        /// The config for the simple item generator.
        /// </summary>
        private SimpleItemGeneratorConfiguration _simpleItemGeneratorConfig;
        /// <summary>
        /// The default conditional probability.
        /// </summary>
        private Dictionary<ItemDescription, double> _simpleItemCoProbabilityDefault = new Dictionary<ItemDescription, double>();
        /// <summary>
        /// Contains the probability that is used to determine additional items for an order based on one item already contained in that order.
        /// </summary>
        private MultiKeyDictionary<ItemDescription, ItemDescription, double> _simpleItemCoProbabilities = new MultiKeyDictionary<ItemDescription, ItemDescription, double>();
        /// <summary>
        /// Gets the conditional probability of selecting the second item in case the first one is already chosen.
        /// </summary>
        /// <param name="item1">The first item (already chosen).</param>
        /// <param name="item2">The second item.</param>
        /// <returns>The conditional probability.</returns>
        private double GetCombinedProbability(ItemDescription item1, ItemDescription item2)
        {
            if (_simpleItemCoProbabilities.ContainsKey(item1, item2))
                return _simpleItemCoProbabilities[item1, item2];
            else
                return _simpleItemCoProbabilityDefault[item1];
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the basic item-descriptions according to the given wordlist-file.
        /// </summary>
        private void InitializeMode()
        {
            // Prepare probabilities and other information about the items
            if (Instance.SettingConfig.InventoryConfiguration.OrderMode != OrderMode.Fixed)
            {
                switch (Instance.SettingConfig.InventoryConfiguration.ItemType)
                {
                    case ItemType.Letter:
                        {
                            #region Initialization for colored letters

                            // Open file to get words
                            List<string> bufferedWords = new List<string>();
                            using (StreamReader sr = new StreamReader(Instance.SettingConfig.InventoryConfiguration.ColoredWordConfiguration.WordFile))
                            {
                                string line;
                                while ((line = sr.ReadLine()) != null)
                                {
                                    bufferedWords.Add(line.Trim());
                                }
                            }
                            List<char> bufferedChars = bufferedWords.SelectMany(w => w).Distinct().ToList();

                            // Build all item-descriptions
                            foreach (var bufferedChar in bufferedChars)
                            {
                                foreach (var color in _colorProbabilities.Keys)
                                {
                                    ColoredLetterDescription description = Instance.CreateItemDescription(Instance.RegisterItemDescriptionID(), ItemType.Letter) as ColoredLetterDescription;
                                    description.Letter = bufferedChar;
                                    description.Color = color;
                                    description.Weight = Instance.Randomizer.NextDouble(
                                        Instance.SettingConfig.InventoryConfiguration.ItemWeightMin,
                                        Instance.SettingConfig.InventoryConfiguration.ItemWeightMax);
                                    _itemDescriptions.Add(description);
                                }
                            }
                            foreach (var description in _itemDescriptions.Cast<ColoredLetterDescription>())
                                _itemDescriptionsByValue[description.Letter, description.Color] = description;

                            // Store info to build more new words
                            _baseWords = bufferedWords.ToArray();

                            // Compute letter probabilities based on word set
                            Dictionary<char, double> letterProbabilities = new Dictionary<char, double>();
                            int numberOfLetters = 0;
                            foreach (string s in _baseWords)
                            {
                                foreach (char c in s.ToCharArray())
                                {
                                    numberOfLetters++;
                                    if (letterProbabilities.ContainsKey(c))
                                        letterProbabilities[c] += 1.0;
                                    else
                                        letterProbabilities[c] = 1.0;
                                }
                            }

                            // Normalize probabilities
                            foreach (var c in letterProbabilities.Keys.ToList())
                                letterProbabilities[c] /= numberOfLetters;

                            // Calculate probabilities of items
                            _itemDescriptionProbabilities = new VolatileIDDictionary<ItemDescription, double>(_itemDescriptions.Select(i => new VolatileKeyValuePair<ItemDescription, double>(i, 0)).ToList());
                            foreach (var description in _itemDescriptions.Cast<ColoredLetterDescription>())
                                _itemDescriptionProbabilities[description] = letterProbabilities[description.Letter] * _colorProbabilities[description.Color];
                            // Order item descriptions accordingly
                            _itemDescriptions = _itemDescriptions.OrderByDescending(d => _itemDescriptionProbabilities[d]).ToList();
                            // Set maximal probability for information supply
                            _itemDescriptionProbabilityMax = _itemDescriptionProbabilities.Max(kvp => kvp.Value);

                            #endregion
                        }
                        break;
                    case ItemType.SimpleItem:
                        {
                            #region Initialization for simple items

                            // Read resource config file
                            _simpleItemGeneratorConfig = InstanceIO.ReadSimpleItemGeneratorConfig(Instance.SettingConfig.InventoryConfiguration.SimpleItemConfiguration.GeneratorConfigFile);

                            // Read all item-descriptions
                            Dictionary<int, ItemDescription> itemDescriptionsByID = new Dictionary<int, ItemDescription>();
                            foreach (var serializedDescription in _simpleItemGeneratorConfig.ItemDescriptions.OrderBy(d => d.Key))
                            {
                                SimpleItemDescription description = Instance.CreateItemDescription(serializedDescription.Key, ItemType.SimpleItem) as SimpleItemDescription;
                                // Check whether weights are specified by the config or have to be generated
                                description.Weight = _simpleItemGeneratorConfig.ItemDescriptionWeights != null && _simpleItemGeneratorConfig.ItemDescriptionWeights.Count > 0 ?
                                    // Use the weight given by the config
                                    _simpleItemGeneratorConfig.ItemDescriptionWeights.Single(w => w.Key == serializedDescription.Key).Value :
                                    // Generate a new weight to use
                                    Instance.Randomizer.NextDouble(Instance.SettingConfig.InventoryConfiguration.ItemWeightMin, Instance.SettingConfig.InventoryConfiguration.ItemWeightMax);
                                // Check whether weights are specified by the config or have to be generated
                                description.BundleSize = _simpleItemGeneratorConfig.ItemDescriptionBundleSizes != null && _simpleItemGeneratorConfig.ItemDescriptionBundleSizes.Count > 0 ?
                                    // Use the weight given by the config
                                    _simpleItemGeneratorConfig.ItemDescriptionBundleSizes.Single(w => w.Key == serializedDescription.Key).Value :
                                    // Generate a new weight to use
                                    Instance.Randomizer.NextInt(Instance.SettingConfig.InventoryConfiguration.BundleSizeMin, Instance.SettingConfig.InventoryConfiguration.BundleSizeMax);
                                description.Hue = serializedDescription.Value;
                                _itemDescriptions.Add(description);
                                itemDescriptionsByID[description.ID] = description;
                            }

                            // Set item probabilities
                            Dictionary<ItemDescription, double> itemDescriptionProbabilities = _itemDescriptions.ToDictionary(k => k, v => _simpleItemGeneratorConfig.DefaultWeight);
                            foreach (var weight in _simpleItemGeneratorConfig.ItemWeights)
                                itemDescriptionProbabilities[itemDescriptionsByID[weight.Key]] = weight.Value;
                            _itemDescriptionProbabilities = new VolatileIDDictionary<ItemDescription, double>(itemDescriptionProbabilities.Select(kvp => new VolatileKeyValuePair<ItemDescription, double>(kvp.Key, kvp.Value)).ToList());
                            // Calculate weights by normalizing
                            double overallWeightSingle =
                                // Given weights
                                _simpleItemGeneratorConfig.ItemWeights.Sum(w => w.Value) +
                                // Default weights
                                _itemDescriptions.Select(d => d.ID).Except(_simpleItemGeneratorConfig.ItemWeights.Select(w => w.Key)).Count() * _simpleItemGeneratorConfig.DefaultWeight;
                            foreach (var itemDescription in _itemDescriptionProbabilities.Keys.ToList())
                                _itemDescriptionProbabilities[itemDescription] /= overallWeightSingle;
                            // Order item descriptions accordingly
                            _itemDescriptions = _itemDescriptions.OrderByDescending(d => _itemDescriptionProbabilities[d]).ToList();
                            // Set maximal probability for information supply
                            _itemDescriptionProbabilityMax = _itemDescriptionProbabilities.Max(kvp => kvp.Value);

                            // --> Set co-probabilities
                            // Determine overall weights
                            Dictionary<ItemDescription, double> overallCoWeights = _simpleItemGeneratorConfig.ItemCoWeights
                                .GroupBy(w => w.Key1)
                                .ToDictionary(
                                    k => itemDescriptionsByID[k.Key],
                                    v => v.Sum(e => e.Value) + (_simpleItemGeneratorConfig.DefaultCoWeight * (_itemDescriptions.Count - v.Count())));
                            Dictionary<ItemDescription, Tuple<int, double>> givenCoWeights = _simpleItemGeneratorConfig.ItemCoWeights
                                .GroupBy(w => w.Key1)
                                .ToDictionary(
                                    k => itemDescriptionsByID[k.Key],
                                    v => new Tuple<int, double>(v.Count(), v.Sum(e => e.Value)));
                            // Determine item specific default probabilities
                            _simpleItemCoProbabilityDefault = _simpleItemGeneratorConfig.ItemCoWeights.GroupBy(w => w.Key1)
                                .ToDictionary(
                                    k => itemDescriptionsByID[k.Key],
                                    v => _itemDescriptions.Count - givenCoWeights[itemDescriptionsByID[v.Key]].Item1 != 0 ?
                                        // Calculate the remaining weight and determine the probability for one single item
                                        (overallCoWeights[itemDescriptionsByID[v.Key]] - givenCoWeights[itemDescriptionsByID[v.Key]].Item2) /
                                        (_itemDescriptions.Count - givenCoWeights[itemDescriptionsByID[v.Key]].Item1) :
                                        // In case all weights are given set a default not used weight
                                        0
                                );
                            // Set default for items where there is no weight given at all - equal probability for all items
                            foreach (var description in _itemDescriptions)
                                if (!_simpleItemCoProbabilityDefault.ContainsKey(description))
                                    _simpleItemCoProbabilityDefault[description] = 1.0 / _itemDescriptions.Count;
                            // Set given conditional probabilities
                            foreach (var coprob in _simpleItemGeneratorConfig.ItemCoWeights)
                                _simpleItemCoProbabilities[itemDescriptionsByID[coprob.Key1], itemDescriptionsByID[coprob.Key2]] = coprob.Value;

                            #endregion
                        }
                        break;
                    default:
                        break;
                }
            }

            // Prepare batch submission info
            if (Instance.SettingConfig.InventoryConfiguration.SubmitBatches)
            {
                _batchNextBundleCycle = Instance.SettingConfig.InventoryConfiguration.BatchInventoryConfiguration.MaxTimeForBundleSubmissions;
                _batchNextOrderCycle = Instance.SettingConfig.InventoryConfiguration.BatchInventoryConfiguration.MaxTimeForOrderSubmissions;
                _batchTimepointsBundles = Instance.SettingConfig.InventoryConfiguration.BatchInventoryConfiguration.BundleBatches.Select(e => new Tuple<double, double>(e.Key, e.Value)).OrderBy(e => e.Item1).ToList();
                _batchTimepointsOrders = Instance.SettingConfig.InventoryConfiguration.BatchInventoryConfiguration.OrderBatches.Select(e => new Tuple<double, int>(e.Key, e.Value)).OrderBy(e => e.Item1).ToList();
            }

            // Prepare down time management
            if (Instance.SettingConfig.InventoryConfiguration.OrderMode == OrderMode.Fill &&
                Instance.SettingConfig.InventoryConfiguration.DemandInventoryConfiguration.DownPeriodConfiguration != null)
            {
                _downPeriodHandling = true;
                _downPeriodNextBundleCycle = Instance.SettingConfig.InventoryConfiguration.DemandInventoryConfiguration.DownPeriodConfiguration.MaxTimeForBundleDownAndUpPeriods;
                _downPeriodNextOrderCycle = Instance.SettingConfig.InventoryConfiguration.DemandInventoryConfiguration.DownPeriodConfiguration.MaxTimeForOrderDownAndUpPeriods;
                _downPeriodTimepointsBundles = Instance.SettingConfig.InventoryConfiguration.DemandInventoryConfiguration.DownPeriodConfiguration.BundleDownAndUpTimes
                    .Select(e => new Tuple<double, bool>(e.Key, e.Value)).OrderBy(e => e.Item1).ToList();
                _downPeriodTimepointsOrders = Instance.SettingConfig.InventoryConfiguration.DemandInventoryConfiguration.DownPeriodConfiguration.OrderDownAndUpTimes
                    .Select(e => new Tuple<double, bool>(e.Key, e.Value)).OrderBy(e => e.Item1).ToList();
                _downPeriodActiveForBundles = _downPeriodTimepointsBundles.Any(p => p.Item1 == 0) ? _downPeriodTimepointsBundles.First(p => p.Item1 == 0).Item2 : false;
                _downPeriodActiveForOrders = _downPeriodTimepointsOrders.Any(p => p.Item1 == 0) ? _downPeriodTimepointsOrders.First(p => p.Item1 == 0).Item2 : false;
            }
        }

        /// <summary>
        /// Initializes the inventory content, the available bundle buffer and the available order buffer.
        /// </summary>
        private void InitializeInventoryAndBundlesAndOrders()
        {
            // Initialize with respect to the current mode
            switch (Instance.SettingConfig.InventoryConfiguration.OrderMode)
            {
                case OrderMode.Fill:
                    {
                        #region Fill mode initialization

                        switch (Instance.SettingConfig.InventoryConfiguration.ItemType)
                        {
                            case ItemType.Letter:
                                {
                                    #region Colored word initialization

                                    // Generate initial order and bundle list
                                    InitializeBundlesAndOrdersRandomly(
                                        Instance.SettingConfig.InventoryConfiguration.DemandInventoryConfiguration.BundleCount,
                                        Instance.SettingConfig.InventoryConfiguration.DemandInventoryConfiguration.OrderCount);

                                    // Generate random pod content
                                    InitializePodContentsRandomly(Instance.SettingConfig.InventoryConfiguration.InitialInventory);

                                    #endregion
                                }
                                break;
                            case ItemType.SimpleItem:
                                {
                                    #region Simple item initialization

                                    // Generate random pod content
                                    InitializePodContentsRandomly(Instance.SettingConfig.InventoryConfiguration.InitialInventory);

                                    // Generate initial order and bundle list
                                    InitializeBundlesAndOrdersRandomly(
                                        _downPeriodActiveForBundles ? 0 : Instance.SettingConfig.InventoryConfiguration.DemandInventoryConfiguration.BundleCount,
                                        _downPeriodActiveForOrders ? 0 : Instance.SettingConfig.InventoryConfiguration.DemandInventoryConfiguration.OrderCount);

                                    #endregion
                                }
                                break;
                            default:
                                break;
                        }

                        #endregion
                    }
                    break;
                case OrderMode.Poisson:
                    {
                        #region Poisson mode initialization

                        // --> Prepare poisson generators
                        switch (Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.PoissonMode)
                        {
                            case PoissonMode.Simple:
                                {
                                    // Initiate order poisson generator
                                    double orderRate = PoissonGenerator.TranslateIntoRateParameter(
                                        TimeSpan.FromHours(1),
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.AverageOrdersPerHourAndStation * Instance.OutputStations.Count);
                                    OrderPoissonGenerator = new PoissonGenerator(Instance.Randomizer, orderRate);
                                    // Initiate bundle poisson generator
                                    double bundleRate = PoissonGenerator.TranslateIntoRateParameter(
                                        TimeSpan.FromHours(1),
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.AverageBundlesPerHourAndStation * Instance.InputStations.Count);
                                    BundlePoissonGenerator = new PoissonGenerator(Instance.Randomizer, bundleRate);
                                }
                                break;
                            case PoissonMode.TimeDependent:
                                {
                                    // --> Instantiate poisson generator for orders
                                    // Calculate instance-specific factor to adapt the rates
                                    List<KeyValuePair<double, double>> relativeOrderWeights = new List<KeyValuePair<double, double>>();
                                    for (int i = 0; i < Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentOrderWeights.Count; i++)
                                    {
                                        relativeOrderWeights.Add(new KeyValuePair<double, double>(
                                            i > 0 ?
                                            Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentOrderWeights[i].Key -
                                                Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentOrderWeights[i - 1].Key :
                                            Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentOrderWeights[i].Key,
                                            Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentOrderWeights[i].Value));
                                    }
                                    double unadjustedAverageOrderFrequency =
                                        relativeOrderWeights.Sum(w => w.Key * w.Value) /
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.MaxTimeForTimeDependentOrderRates;
                                    double aimedAverageOrderFrequency =
                                        TimeSpan.FromSeconds(Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.MaxTimeForTimeDependentOrderRates).TotalHours *
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.AverageOrdersPerHourAndStation *
                                        Instance.OutputStations.Count /
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.MaxTimeForTimeDependentOrderRates;
                                    double orderSteerFactor = aimedAverageOrderFrequency / unadjustedAverageOrderFrequency;
                                    // Initiate order poisson generator
                                    OrderPoissonGenerator = new PoissonGenerator(
                                        Instance.Randomizer,
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.MaxTimeForTimeDependentOrderRates,
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentOrderWeights.Select(w =>
                                            new KeyValuePair<double, double>(w.Key, orderSteerFactor * w.Value)));
                                    // --> Instantiate poisson generator for bundles
                                    // Calculate instance-specific factor to adapt the rates
                                    List<KeyValuePair<double, double>> relativeBundleWeights = new List<KeyValuePair<double, double>>();
                                    for (int i = 0; i < Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentBundleWeights.Count; i++)
                                    {
                                        relativeBundleWeights.Add(new KeyValuePair<double, double>(
                                            i > 0 ?
                                            Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentBundleWeights[i].Key -
                                                Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentBundleWeights[i - 1].Key :
                                            Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentBundleWeights[i].Key,
                                            Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentBundleWeights[i].Value));
                                    }
                                    double unadjustedAverageBundleFrequency =
                                        relativeBundleWeights.Sum(w => w.Key * w.Value) /
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.MaxTimeForTimeDependentBundleRates;
                                    double aimedAverageBundleFrequency =
                                        TimeSpan.FromSeconds(Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.MaxTimeForTimeDependentBundleRates).TotalHours *
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.AverageBundlesPerHourAndStation *
                                        Instance.InputStations.Count /
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.MaxTimeForTimeDependentBundleRates;
                                    double bundleSteerFactor = aimedAverageBundleFrequency / unadjustedAverageBundleFrequency;
                                    // Initiate bundle poisson generator
                                    BundlePoissonGenerator = new PoissonGenerator(
                                          Instance.Randomizer,
                                          Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.MaxTimeForTimeDependentBundleRates,
                                          Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.TimeDependentBundleWeights.Select(w =>
                                              new KeyValuePair<double, double>(w.Key, bundleSteerFactor * w.Value)));
                                }
                                break;
                            case PoissonMode.HighLow:
                                {
                                    // Obtain switch rates
                                    double rateSwitchLowHigh = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromHours(1),
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.LowToHighSwitchesPerHour);
                                    double rateSwitchHighLow = PoissonGenerator.TranslateIntoRateParameter(TimeSpan.FromHours(1),
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.HighToLowSwitchesPerHour);
                                    // Initiate order poisson generator (calculate low an high rates first)
                                    double orderRateLow = PoissonGenerator.TranslateIntoRateParameter(
                                        TimeSpan.FromHours(1),
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.AverageOrdersPerHourAndStation * Instance.OutputStations.Count);
                                    double orderRateHigh = PoissonGenerator.TranslateIntoRateParameter(
                                        TimeSpan.FromHours(1),
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.AverageOrdersPerHourAndStationHigh * Instance.OutputStations.Count);
                                    OrderPoissonGenerator = new PoissonGenerator(Instance.Randomizer,
                                        // Submit the rates for low and high
                                        orderRateLow, orderRateHigh,
                                        // Submit the rates for switching only if order generation shall be affected (otherwise provide 0 to suppress switching)
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.OrderGenAffectedByHighPeriod ? rateSwitchLowHigh : 0,
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.OrderGenAffectedByHighPeriod ? rateSwitchHighLow : 0,
                                        // Add a possibility for logging and affiliation information
                                        Instance.LogInfo, "Orders");
                                    // Initiate bundle poisson generator (calculate low an high rates first)
                                    double bundleRateLow = PoissonGenerator.TranslateIntoRateParameter(
                                        TimeSpan.FromHours(1),
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.AverageBundlesPerHourAndStation * Instance.InputStations.Count);
                                    double bundleRateHigh = PoissonGenerator.TranslateIntoRateParameter(
                                        TimeSpan.FromHours(1),
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.AverageBundlesPerHourAndStationHigh * Instance.InputStations.Count);
                                    BundlePoissonGenerator = new PoissonGenerator(Instance.Randomizer,
                                        // Submit the rates for low and high
                                        bundleRateLow, bundleRateHigh,
                                        // Submit the rates for switching only if bundle generation shall be affected (otherwise provide 0 to suppress switching)
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.BundleGenAffectedByHighPeriod ? rateSwitchLowHigh : 0,
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.BundleGenAffectedByHighPeriod ? rateSwitchHighLow : 0,
                                        // Add a possibility for logging and affiliation information
                                        Instance.LogInfo, "Bundles");
                                }
                                break;
                            default: throw new ArgumentException("Unknown poisson-mode: " + Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.PoissonMode.ToString());
                        }

                        switch (Instance.SettingConfig.InventoryConfiguration.ItemType)
                        {
                            case ItemType.Letter:
                                {
                                    #region Initialization for colored letters

                                    // Generate initial order and bundle list
                                    InitializeBundlesAndOrdersRandomly(
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.InitialBundleCount,
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.InitialOrderCount);

                                    // Generate random pod content
                                    InitializePodContentsRandomly(Instance.SettingConfig.InventoryConfiguration.InitialInventory);

                                    #endregion
                                }
                                break;
                            case ItemType.SimpleItem:
                                {
                                    #region Initialization for simple items

                                    // Generate random pod content
                                    InitializePodContentsRandomly(Instance.SettingConfig.InventoryConfiguration.InitialInventory);

                                    // Generate initial order and bundle list
                                    InitializeBundlesAndOrdersRandomly(
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.InitialBundleCount,
                                        Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.InitialOrderCount);

                                    #endregion
                                }
                                break;
                            default:
                                break;
                        }

                        // Calculate first order and bundle generation times
                        _nextPoissonOrderGenerationTime = OrderPoissonGenerator.Next(0);
                        _nextPoissonBundleGenerationTime = BundlePoissonGenerator.Next(0);

                        #endregion
                    }
                    break;
                case OrderMode.Fixed:
                    {
                        #region Fixed mode initialization

                        // --> Parse the list of orders
                        InstanceIO.ReadOrders(Instance.SettingConfig.InventoryConfiguration.FixedInventoryConfiguration.OrderFile, Instance);

                        // --> Init the fixed order mode
                        // Add item descriptions
                        _itemDescriptions.AddRange(Instance.OrderList.ItemDescriptions);
                        // Copy over order list
                        _futureOrders.AddRange(Instance.OrderList.Orders.OrderBy(o => o.TimeStamp));
                        // Copy over bundle list
                        _futureBundles.AddRange(Instance.OrderList.Bundles.OrderBy(b => b.TimeStamp));
                        // Set probabilites for random bundle generation
                        _itemDescriptionProbabilities = new VolatileIDDictionary<ItemDescription, double>(_itemDescriptions.Select(i => new VolatileKeyValuePair<ItemDescription, double>(i, 0)).ToList());
                        int itemsOrderedOverall = _futureOrders.Sum(o => o.Positions.Sum(p => p.Value));
                        foreach (var itemDescription in _itemDescriptions)
                            _itemDescriptionProbabilities[itemDescription] = _futureOrders.SelectMany(o => o.Positions).Where(p => p.Key == itemDescription).Sum(p => p.Value) / (double)itemsOrderedOverall;
                        // Set requirements so that all future orders can be fulfilled (when using Letter-items)
                        foreach (var itemDescription in _itemDescriptions)
                            _itemDemandInformation[itemDescription] = _futureOrders.SelectMany(o => o.Positions).Where(p => p.Key == itemDescription).Sum(p => p.Value);
                        // Generate random pod content
                        InitializePodContentsRandomly(Instance.SettingConfig.InventoryConfiguration.InitialInventory);

                        #endregion
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Initializes a random inventory content.
        /// </summary>
        /// <param name="initialInventory">The fractional amount of desired inventory.</param>
        private void InitializePodContentsRandomly(double initialInventory)
        {
            // Add stuff to pods
            while (Instance.Pods.Sum(b => b.CapacityInUse) / Instance.Pods.Sum(b => b.Capacity) < initialInventory)
            {
                // Create bundle
                ItemBundle bundle = GenerateBundle();
                // Ask the current item storage manager for the pod to use, then assign it
                Pod pod = Instance.Controller.StorageManager.SelectPodForInititalInventory(Instance, bundle);
                if (!pod.Add(bundle))
                    throw new InvalidOperationException("Could not assign bundle to the selected pod!");
                // Notify the instance about the new bundle
                Instance.NotifyInitialBundleStored(bundle, pod);
            }
        }

        /// <summary>
        /// Randomly generates a set of bundles and orders that are ready for allocation.
        /// </summary>
        /// <param name="bundles">The number of bundles to generate.</param>
        /// <param name="orders">The number of orders to generate.</param>
        private void InitializeBundlesAndOrdersRandomly(int bundles, int orders)
        {
            lock (_syncRoot)
            {
                // Fill list of orders to the given count
                for (int i = 0; i < orders; i++)
                {
                    // Check for generation pause
                    if (Instance.SettingConfig.InventoryConfiguration.ItemType != ItemType.Letter && CheckForOrderGenerationPause())
                        break;

                    // --> Generate order
                    Order order = GenerateRandomOrder();
                    // Only submit order if we successfully generated one
                    if (order != null)
                    {
                        _availableOrders.Add(order);
                        // Keep personalized list up-to-date
                        foreach (var retriever in _personalizedAvailableOrders.Keys)
                            _personalizedAvailableOrders[retriever].Add(order);
                        // Notify the instance about the new order
                        Instance.NotifyOrderPlaced(order);
                    }
                    else
                    {
                        // There is no order we can generate right now - quit trying
                        Instance.LogInfo("Cannot generate further orders - quitting");
                        break;
                    }
                }
                // Fill list of bundles to the given count
                for (int i = 0; i < bundles; i++)
                {
                    // Check for generation pause
                    if (Instance.SettingConfig.InventoryConfiguration.ItemType != ItemType.Letter && CheckForBundleGenerationPause())
                        break;

                    // --> Generate bundle
                    ItemBundle b = GenerateRandomBundle();
                    // Only add bundle, if we successfully generated one
                    if (b != null)
                    {
                        // Add it to the main list
                        _availableBundles.Add(b);
                        // Keep personalized list up-to-date
                        foreach (var retriever in _personalizedAvailableBundles.Keys)
                            _personalizedAvailableBundles[retriever].Add(b);
                        // Notify the instance about the new bundle
                        Instance.NotifyBundlePlaced(b);
                    }
                    else
                    {
                        // There is no bundle we can generate right now - quit trying
                        Instance.LogInfo("Cannot generate further bundles - quitting");
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Generates a random orders that are not submitted to the instance. These order can be used to warmup item-frequency based mechanisms.
        /// </summary>
        private void WarmupItemFrequencies()
        {
            if (Instance.SettingConfig.InventoryConfiguration.OrderMode == OrderMode.Fixed)
            {
                // TODO implement warmup for fixed order mode
            }
            else
            {
                switch (Instance.SettingConfig.InventoryConfiguration.ItemType)
                {
                    case ItemType.Letter:
                        {
                            #region Warmup colored letter based frequencies

                            // Generate random orders
                            for (int j = 0; j < Instance.SettingConfig.InventoryConfiguration.WarmupOrderCount; j++)
                            {
                                // Generate next random order
                                Order order = new Order();

                                // Choose a random word from the list
                                LetterColors[] chosenColors = null;
                                int[] chosenPositionCounts = null;
                                string word = _baseWords[Instance.Randomizer.NextInt(_baseWords.Length)];
                                chosenColors = new LetterColors[word.Length];
                                chosenPositionCounts = new int[word.Length];

                                // Add each letter
                                for (int i = 0; i < word.Length; i++)
                                {
                                    // Get color based on distribution
                                    double r = Instance.Randomizer.NextDouble();

                                    // Choose a default one just incase
                                    LetterColors chosenColor = _colorProbabilities.Keys.First();

                                    // Go through and check the range of each color, pulling random number down to the current range
                                    foreach (var c in _colorProbabilities.Keys)
                                    {
                                        if (_colorProbabilities[c] > r)
                                        {
                                            chosenColor = c;
                                            break;
                                        }
                                        r -= _colorProbabilities[c];
                                    }

                                    // Store decision
                                    chosenColors[i] = chosenColor;
                                    chosenPositionCounts[i] = Instance.SettingConfig.InventoryConfiguration.PositionCountMin == Instance.SettingConfig.InventoryConfiguration.PositionCountMax ?
                                        Instance.SettingConfig.InventoryConfiguration.PositionCountMin :
                                        Instance.Randomizer.NextNormalInt(
                                            Instance.SettingConfig.InventoryConfiguration.PositionCountMean,
                                            Instance.SettingConfig.InventoryConfiguration.PositionCountStdDev,
                                            Instance.SettingConfig.InventoryConfiguration.PositionCountMin,
                                            Instance.SettingConfig.InventoryConfiguration.PositionCountMax);

                                    // Add letter to order
                                    order.AddPosition(_itemDescriptionsByValue[word[i], chosenColors[i]], chosenPositionCounts[i]);
                                }

                                // Submit the shadow order
                                Instance.FrequencyTracker.NewOrderCallback(order);
                            }

                            #endregion
                        }
                        break;
                    case ItemType.SimpleItem:
                        {
                            #region Warmup simple item based frequencies

                            // Determine overall probability
                            double availableInventoryOverallProbability = _itemDescriptions.Sum(d => _itemDescriptionProbabilities[d]);
                            // Generate random orders
                            for (int j = 0; j < Instance.SettingConfig.InventoryConfiguration.WarmupOrderCount; j++)
                            {
                                // Init order
                                Order order = new Order();
                                // Decide position count
                                int orderPositionCount = Instance.SettingConfig.InventoryConfiguration.OrderPositionCountMin == Instance.SettingConfig.InventoryConfiguration.OrderPositionCountMax ?
                                    // If min == max return it
                                    Instance.SettingConfig.InventoryConfiguration.OrderPositionCountMin :
                                    // Elsewise get a normally distributed number
                                    Instance.Randomizer.NextNormalInt(
                                    Instance.SettingConfig.InventoryConfiguration.OrderPositionCountMean,
                                    Instance.SettingConfig.InventoryConfiguration.OrderPositionCountStdDev,
                                    Instance.SettingConfig.InventoryConfiguration.OrderPositionCountMin,
                                    Instance.SettingConfig.InventoryConfiguration.OrderPositionCountMax);

                                // Add remaining positions
                                ItemDescription chosenDescription; ItemDescription lastChosenDescription = null;
                                for (int i = 0; i < orderPositionCount; i++)
                                {
                                    // Determine position count
                                    int positionCount = Instance.SettingConfig.InventoryConfiguration.PositionCountMin == Instance.SettingConfig.InventoryConfiguration.PositionCountMax ?
                                        // If min == max return it
                                        Instance.SettingConfig.InventoryConfiguration.PositionCountMin :
                                        // Elsewise get a normally distributed number
                                        Instance.Randomizer.NextNormalInt(
                                        Instance.SettingConfig.InventoryConfiguration.PositionCountMean,
                                        Instance.SettingConfig.InventoryConfiguration.PositionCountStdDev,
                                        Instance.SettingConfig.InventoryConfiguration.PositionCountMin,
                                        Instance.SettingConfig.InventoryConfiguration.PositionCountMax);
                                    // Decide item based on available inventory
                                    // Distinguish between the first position (generation is based on a simple probability for the item)
                                    // and the other positions (probability is based on the preceding item)
                                    if (i == 0 || Instance.Randomizer.NextDouble() >= _simpleItemGeneratorConfig.ProbToUseCoWeight)
                                    {
                                        // Choose first item-description based on the probabilities
                                        double r = Instance.Randomizer.NextDouble();
                                        chosenDescription = _itemDescriptions.First();
                                        foreach (var description in _itemDescriptions)
                                        {
                                            r -= _itemDescriptionProbabilities[description] / availableInventoryOverallProbability;
                                            if (r <= 0) { chosenDescription = description; break; }
                                        }
                                        lastChosenDescription = chosenDescription;
                                    }
                                    else
                                    {
                                        // Choose other item-descriptions based on the conditional probabilities
                                        double availableInventoryCombinedOverallProbability = _itemDescriptions.Sum(d => GetCombinedProbability(lastChosenDescription, d));
                                        double r = Instance.Randomizer.NextDouble();
                                        chosenDescription = _itemDescriptions.First();
                                        foreach (var description in _itemDescriptions)
                                        {
                                            r -= GetCombinedProbability(lastChosenDescription, description) / availableInventoryCombinedOverallProbability;
                                            if (r <= 0) { chosenDescription = description; break; }
                                        }
                                        lastChosenDescription = chosenDescription;
                                    }

                                    // Add the position
                                    order.AddPosition(chosenDescription, positionCount);
                                }

                                // Submit the shadow order
                                Instance.FrequencyTracker.NewOrderCallback(order);
                            }

                            #endregion
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion

        #region Generation pause handling

        /// <summary>
        /// Checks whether order generation is currently suspended.
        /// </summary>
        /// <returns><code>true</code> if order generation is blocked, <code>false</code> otherwise.</returns>
        private bool CheckForOrderGenerationPause()
        {
            // See whether we have to pause order generation for a while
            if (// If we are above the restart threshold, ensure order generation activity
                Instance.StatStorageFillLevel > Instance.SettingConfig.InventoryConfiguration.DemandInventoryConfiguration.InventoryLevelOrderRestartThreshold &&
                // Paused at all?
                _orderGenerationBlockedByInventoryLevel)
            {
                // Unblock order generation due to sufficient inventory
                _orderGenerationBlockedByInventoryLevel = false;
                Instance.LogInfo("Reactivating order generation paused by inventory level (currently at: " + Instance.StatStorageFillLevel.ToString(IOConstants.FORMATTER) + ")");
            }
            if (// See whether order generation has to be deactivated, if it rises above a certain threshold
                Instance.SettingConfig.InventoryConfiguration.DemandInventoryConfiguration.InventoryLevelDrivenOrderGeneration &&
                // See whether we are below the pause threshold for order generation
                Instance.StatStorageFillLevel < Instance.SettingConfig.InventoryConfiguration.DemandInventoryConfiguration.InventoryLevelOrderStopThreshold)
            {
                // Entering pause?
                if (!_orderGenerationBlockedByInventoryLevel)
                {
                    Instance.LogInfo("Pausing order generation due to inventory level (currently at: " + Instance.StatStorageFillLevel.ToString(IOConstants.FORMATTER) + ")");
                    // Notify instance
                    Instance.NotifyOrderGenerationPaused();
                }
                // Block order generation due to low inventory
                _orderGenerationBlockedByInventoryLevel = true;
            }
            // If order generation is blocked, break
            return _orderGenerationBlockedByInventoryLevel;
        }

        /// <summary>
        /// Checks whether bundle generation is currently suspended.
        /// </summary>
        /// <returns><code>true</code> if bundle generation is blocked, <code>false</code> otherwise.</returns>
        private bool CheckForBundleGenerationPause()
        {
            // See whether we have to pause bundle generation for a while
            if (// If we are below the restart threshold, ensure bundle generation activity
                Instance.StatStorageFillAndReservedAndBacklogLevel < Instance.SettingConfig.InventoryConfiguration.DemandInventoryConfiguration.InventoryLevelBundleRestartThreshold &&
                // Paused at all?
                _bundleGenerationBlockedByInventoryLevel)
            {
                // Unblock bundle generation due to low inventory
                _bundleGenerationBlockedByInventoryLevel = false;
                Instance.LogInfo("Reactivating bundle generation paused by inventory+reserved+backlog level (currently at: " + Instance.StatStorageFillAndReservedAndBacklogLevel.ToString(IOConstants.FORMATTER) + ")");
            }
            if (// See whether bundle generation has to be deactivated, if it rises above a certain threshold
                Instance.SettingConfig.InventoryConfiguration.DemandInventoryConfiguration.InventoryLevelDrivenBundleGeneration &&
                // See whether we are above the pause threshold for bundle generation
                Instance.StatStorageFillAndReservedAndBacklogLevel > Instance.SettingConfig.InventoryConfiguration.DemandInventoryConfiguration.InventoryLevelBundleStopThreshold)
            {
                // Entering pause?
                if (!_bundleGenerationBlockedByInventoryLevel)
                {
                    Instance.LogInfo("Pausing bundle generation due to inventory+reserved+backlog level (currently at: " + Instance.StatStorageFillAndReservedAndBacklogLevel.ToString(IOConstants.FORMATTER) + ")");
                    // Notify instance
                    Instance.NotifyBundleGenerationPaused();
                }
                // Block bundle generation due to overfilled inventory
                _bundleGenerationBlockedByInventoryLevel = true;
            }
            // If bundle generation is blocked, break
            return _bundleGenerationBlockedByInventoryLevel;
        }

        #endregion

        #region Bundle and order generation

        private ItemBundle GenerateBundle()
        {
            // Generate a bundle depending on the type
            ItemBundle bundle;
            switch (Instance.SettingConfig.InventoryConfiguration.ItemType)
            {
                case ItemType.Letter:
                    {
                        // Generate a bundle required by the orders, if not in fixed mode
                        // (this is necessary, because the order generation does not respect the inventory)
                        bundle = GenerateRequiredBundle();
                    }
                    break;
                case ItemType.SimpleItem:
                    {
                        // Simply randomly generate bundles
                        bundle = GenerateRandomBundle();
                    }
                    break;
                default: throw new ArgumentException("Unknown item-type: " + Instance.SettingConfig.InventoryConfiguration.ItemType.ToString());
            }
            // Set the time as if the bundle was placed right now
            if (bundle != null)
                bundle.TimeStamp = Instance.Controller == null ? 0.0 : Instance.Controller.CurrentTime;
            // Return it
            return bundle;
        }

        /// <summary>
        /// Generates a random item.
        /// </summary>
        /// <returns>The generated item.</returns>
        private ItemBundle GenerateRandomBundle()
        {
            // Get a random value first
            double r = Instance.Randomizer.NextDouble();

            // Choose item-description based on the probabilities
            ItemDescription chosenDescription = _itemDescriptions.First();
            foreach (var description in _itemDescriptions)
            {
                r -= _itemDescriptionProbabilities[description];
                if (r <= 0) { chosenDescription = description; break; }
            }

            int bundleSize = 0;
            if (Instance.Randomizer.NextDouble() < Instance.SettingConfig.InventoryConfiguration.ReturnOrderProbability)
                // Emulate a return order
                bundleSize = 1;
            else if (chosenDescription.BundleSize > 0)
                // Use given bundle size, if possible
                bundleSize = chosenDescription.BundleSize;
            else
                // Generate a random bundle size
                bundleSize = Instance.Randomizer.NextInt(Instance.SettingConfig.InventoryConfiguration.BundleSizeMin, Instance.SettingConfig.InventoryConfiguration.BundleSizeMax);
            // If the bundle does not fit the system ignore it (in case of simple items)
            if (!Instance.SettingConfig.InventoryConfiguration.IgnoreCapacityForBundleGeneration && // Only respect the capacity utilization if desired
                Instance.SettingConfig.InventoryConfiguration.ItemType == ItemType.SimpleItem && // Only respect the capacity utilization for simple items
                Instance.StockInfo.CurrentReservedOverallLoad + bundleSize > Instance.StockInfo.OverallLoadCapacity * Instance.SettingConfig.InventoryConfiguration.BufferBundlesUntilInventoryLoad) // See whether there is enough potential capacity for the bundle
                return null;

            // Create a new bundle with the chosen description and return it
            ItemBundle bundle = Instance.CreateItemBundle(chosenDescription, bundleSize);
            return bundle;
        }

        /// <summary>
        /// Generates a bundle that is needed by the allocated orders. (This feels like cheating - that is why I integrated simple items)
        /// </summary>
        /// <returns>The newly generated bundle or <code>null</code> if no bundle is necessary.</returns>
        private ItemBundle GenerateRequiredBundle()
        {
            // Check for needed items
            IEnumerable<ItemDescription> neededItems = _itemDemandInformation
                .Where(i => i.Value > 0) // Item where there is actually need for
                .OrderByDescending(i => i.Value) // Generate an item with a high demand
                .Select(i => i.Key); // Select the actual SKU info
            // Check whether an item is needed
            if (neededItems.Any())
            {
                // Get item to generate
                ItemDescription itemToGenerate = neededItems.First();
                int bundleSize = 0;
                if (Instance.Randomizer.NextDouble() < Instance.SettingConfig.InventoryConfiguration.ReturnOrderProbability)
                    // Emulate a return order
                    bundleSize = 1;
                else if (itemToGenerate.BundleSize > 0)
                    // Use given bundle size
                    bundleSize = itemToGenerate.BundleSize;
                else
                    // Generate a random bundle size
                    bundleSize = Instance.Randomizer.NextInt(Instance.SettingConfig.InventoryConfiguration.BundleSizeMin, Instance.SettingConfig.InventoryConfiguration.BundleSizeMax);
                // Create the needed bundle
                ItemBundle bundle = Instance.CreateItemBundle(itemToGenerate, bundleSize);
                // Update item requirements
                if (_itemDemandInformation.ContainsKey(bundle.ItemDescription))
                    _itemDemandInformation[itemToGenerate] -= bundleSize;
                else
                    _itemDemandInformation[itemToGenerate] = -bundleSize;
                return bundle;
            }
            else
            {
                // Create a random item
                int highestRequirement = _itemDemandInformation.Max(r => r.Value);
                ItemDescription[] itemDescriptionsToChoose = _itemDemandInformation.Where(r => r.Value == highestRequirement).Select(r => r.Key).ToArray();
                ItemDescription itemToGenerate = itemDescriptionsToChoose[Instance.Randomizer.NextInt(itemDescriptionsToChoose.Length)];
                int bundleSize = 0;
                if (Instance.Randomizer.NextDouble() < Instance.SettingConfig.InventoryConfiguration.ReturnOrderProbability)
                    // Emulate a return order
                    bundleSize = 1;
                else if (itemToGenerate.BundleSize > 0)
                    // Use given bundle size
                    bundleSize = itemToGenerate.BundleSize;
                else
                    // Generate a random bundle size
                    bundleSize = Instance.Randomizer.NextInt(Instance.SettingConfig.InventoryConfiguration.BundleSizeMin, Instance.SettingConfig.InventoryConfiguration.BundleSizeMax);
                ItemBundle bundle = Instance.CreateItemBundle(itemToGenerate, bundleSize);
                // Update item requirements
                if (_itemDemandInformation.ContainsKey(itemToGenerate))
                    _itemDemandInformation[itemToGenerate] -= bundleSize;
                else
                    _itemDemandInformation[itemToGenerate] = -bundleSize;
                return bundle;
            }
        }

        /// <summary>
        /// Generates a random order.
        /// </summary>
        /// <returns>The newly generated order.</returns>
        private Order GenerateRandomOrder()
        {
            // Init
            IRandomizer rand = Instance.Randomizer;
            Order order = new Order();
            // Set the time as if the order was placed right now
            order.TimeStamp = Instance.Controller == null ? 0.0 : Instance.Controller.CurrentTime;
            // Set a random due time as an offset off the time at which the order is placed, hence: now + offset
            if (Instance.SettingConfig.InventoryConfiguration.DueTimePriorityMode)
                // Emulate priority orders
                order.DueTime = order.TimeStamp +
                    (Instance.Randomizer.NextDouble() < Instance.SettingConfig.InventoryConfiguration.DueTimePriorityOrderProbability ?
                        Instance.SettingConfig.InventoryConfiguration.DueTimePriorityOrder :
                        Instance.SettingConfig.InventoryConfiguration.DueTimeOrdinaryOrder);
            else
                // Use a normal distribution
                order.DueTime = order.TimeStamp + Instance.Randomizer.NextNormalDouble(
                    Instance.SettingConfig.InventoryConfiguration.DueTimeOffsetMean,
                    Instance.SettingConfig.InventoryConfiguration.DueTimeOffsetStdDev,
                    Instance.SettingConfig.InventoryConfiguration.DueTimeOffsetMin,
                    Instance.SettingConfig.InventoryConfiguration.DueTimeOffsetMax);

            // Switch on the current item type
            switch (Instance.SettingConfig.InventoryConfiguration.ItemType)
            {
                case ItemType.Letter:
                    {
                        #region Order generation for colored letter items

                        // Choose a random word from the list
                        LetterColors[] chosenColors = null;
                        int[] chosenPositionCounts = null;
                        string word = null;
                        word = _baseWords[rand.NextInt(_baseWords.Length)];
                        chosenColors = new LetterColors[word.Length];
                        chosenPositionCounts = new int[word.Length];

                        // Add each letter
                        for (int i = 0; i < word.Length; i++)
                        {
                            // Get color based on distribution
                            double r = rand.NextDouble();

                            // Choose a default one just incase
                            LetterColors chosenColor = _colorProbabilities.Keys.First();

                            // Go through and check the range of each color, pulling random number down to the current range
                            foreach (var c in _colorProbabilities.Keys)
                            {
                                if (_colorProbabilities[c] > r) { chosenColor = c; break; }
                                r -= _colorProbabilities[c];
                            }

                            // Store decision
                            chosenColors[i] = chosenColor;
                            chosenPositionCounts[i] = Instance.SettingConfig.InventoryConfiguration.PositionCountMin == Instance.SettingConfig.InventoryConfiguration.PositionCountMax ?
                                Instance.SettingConfig.InventoryConfiguration.PositionCountMin :
                                Instance.Randomizer.NextNormalInt(
                                    Instance.SettingConfig.InventoryConfiguration.PositionCountMean,
                                    Instance.SettingConfig.InventoryConfiguration.PositionCountStdDev,
                                    Instance.SettingConfig.InventoryConfiguration.PositionCountMin,
                                    Instance.SettingConfig.InventoryConfiguration.PositionCountMax);
                        }
                        for (int i = 0; i < word.Length; i++)
                        {
                            // Add letter to order
                            order.AddPosition(_itemDescriptionsByValue[word[i], chosenColors[i]], chosenPositionCounts[i]);
                        }

                        // Update item requirements
                        foreach (var position in order.Positions)
                            if (_itemDemandInformation.ContainsKey(position.Key))
                                _itemDemandInformation[position.Key] += position.Value;
                            else
                                _itemDemandInformation[position.Key] = position.Value;

                        #endregion
                    }
                    break;
                case ItemType.SimpleItem:
                    {
                        #region Order generation for simple items

                        // Decide position count
                        int orderPositionCount = Instance.SettingConfig.InventoryConfiguration.OrderPositionCountMin == Instance.SettingConfig.InventoryConfiguration.OrderPositionCountMax ?
                            // If min == max return it
                            Instance.SettingConfig.InventoryConfiguration.OrderPositionCountMin :
                            // Elsewise get a normally distributed number
                            Instance.Randomizer.NextNormalInt(
                                Instance.SettingConfig.InventoryConfiguration.OrderPositionCountMean,
                                Instance.SettingConfig.InventoryConfiguration.OrderPositionCountStdDev,
                                Instance.SettingConfig.InventoryConfiguration.OrderPositionCountMin,
                                Instance.SettingConfig.InventoryConfiguration.OrderPositionCountMax);
                        // Add remaining positions
                        ItemDescription chosenDescription; ItemDescription lastChosenDescription = null;
                        for (int i = 0; i < orderPositionCount; i++)
                        {
                            // Determine position count
                            int positionCount = Instance.SettingConfig.InventoryConfiguration.PositionCountMin == Instance.SettingConfig.InventoryConfiguration.PositionCountMax ?
                                // If min == max return it
                                Instance.SettingConfig.InventoryConfiguration.PositionCountMin :
                                // Elsewise get a normally distributed number
                                Instance.Randomizer.NextNormalInt(
                                    Instance.SettingConfig.InventoryConfiguration.PositionCountMean,
                                    Instance.SettingConfig.InventoryConfiguration.PositionCountStdDev,
                                    Instance.SettingConfig.InventoryConfiguration.PositionCountMin,
                                    Instance.SettingConfig.InventoryConfiguration.PositionCountMax);
                            // Decide item based on available inventory
                            ItemDescription[] availableInventory = _itemDescriptions
                                .Except(order.Positions.Select(p => p.Key)) // Do not use items already in the order
                                .Where(d => Instance.StockInfo.GetAvailableStock(d) >= positionCount) // Do not use inventory that is out-of-stock
                                .ToArray(); // Put it in an array for fast access
                            // If there is not enough inventory at all - return nothing
                            if (!availableInventory.Any())
                                return null;
                            // Distinguish between the first position (generation is based on a simple probability for the item)
                            // and the other positions (probability is based on the preceding item)
                            if (i == 0 || Instance.Randomizer.NextDouble() >= _simpleItemGeneratorConfig.ProbToUseCoWeight)
                            {
                                // Choose first item-description based on the probabilities
                                double availableInventoryOverallProbability = availableInventory.Sum(d => _itemDescriptionProbabilities[d]);
                                double r = Instance.Randomizer.NextDouble();
                                chosenDescription = availableInventory.First();
                                foreach (var description in availableInventory)
                                {
                                    r -= _itemDescriptionProbabilities[description] / availableInventoryOverallProbability;
                                    if (r <= 0) { chosenDescription = description; break; }
                                }
                                lastChosenDescription = chosenDescription;
                            }
                            else
                            {
                                // Choose other item-descriptions based on the conditional probabilities
                                double availableInventoryOverallProbability = availableInventory.Sum(d => GetCombinedProbability(lastChosenDescription, d));
                                double r = Instance.Randomizer.NextDouble();
                                chosenDescription = availableInventory.First();
                                foreach (var description in availableInventory)
                                {
                                    r -= GetCombinedProbability(lastChosenDescription, description) / availableInventoryOverallProbability;
                                    if (r <= 0) { chosenDescription = description; break; }
                                }
                                lastChosenDescription = chosenDescription;
                            }

                            // Add the position
                            order.AddPosition(chosenDescription, positionCount);
                        }

                        #endregion
                    }
                    break;
                default:
                    break;
            }

            // Return
            return order;
        }

        #endregion

        #region Actions

        /// <summary>
        /// Returns the first available order and removes it from the list.
        /// </summary>
        /// <returns>The order.</returns>
        public void TakeAvailableOrder(Order order)
        {
            // Synchronize with visualization
            lock (_syncRoot)
            {
                // Take selected order out of the list
                _availableOrders.Remove(order);
                // Mark the order as currently assigned
                _openOrders.Add(order);
            }
        }

        /// <summary>
        /// Returns the next available bundle and removes it from the list.
        /// </summary>
        /// <param name="bundle">The bundle that is being allocated.</param>
        public void TakeAvailableBundle(ItemBundle bundle)
        {
            // Synchronize with visualization
            lock (_syncRoot)
            {
                // Take selected bundle out of the list
                _availableBundles.Remove(bundle);
                // Mark the bundle as currently assigned
                _openBundles.Add(bundle);
            }
        }

        #endregion

        #region Order and bundle retrievers

        /// <summary>
        /// Exposes all currently available orders.
        /// </summary>
        public IEnumerable<Order> AvailableOrders { get { return _availableOrders; } }

        /// <summary>
        /// Retrieves the next order from a dynamic list of available orders for the retriever. Every retrieved item will be consumed and removed from the personalized list of the retriever.
        /// </summary>
        /// <param name="retriever">The object retrieving the next order.</param>
        /// <returns>The next order on the list or <code>null</code> if no order is left.</returns>
        public Order RetrieveOrder(object retriever)
        {
            // Init list if not present
            if (!_personalizedAvailableOrders.ContainsKey(retriever))
                _personalizedAvailableOrders[retriever] = _availableOrders.ToList();
            // Retrieve order
            Order order = _personalizedAvailableOrders[retriever].FirstOrDefault();
            // Remove bundle if there was one
            if (order != null)
                _personalizedAvailableOrders[retriever].RemoveAt(0);
            // Return it
            return order;
        }
        /// <summary>
        /// Contains all bundles personalized per retrieving object.
        /// </summary>
        private Dictionary<object, List<Order>> _personalizedAvailableOrders = new Dictionary<object, List<Order>>();

        /// <summary>
        /// Called by the system when a new order has been assigned to an output-station. It is useful as it indicates what new items need to be filled in the system (based on the order).
        /// </summary>
        /// <param name="oStation">The station the order was assigned to.</param>
        /// <param name="order">The assigned order.</param>
        public void NewOrderAssignedToStation(OutputStation oStation, Order order) { /* Not used right now */ }

        /// <summary>
        /// Marks the given order as complete.
        /// </summary>
        /// <param name="order">The completed order.</param>
        public void CompleteOrder(Order order)
        {
            // Store it
            lock (_syncRoot)
            {
                _completedOrders.Add(order);
                _openOrders.Remove(order);
            }
        }

        /// <summary>
        /// Exposes all currently available bundles.
        /// </summary>
        public IEnumerable<ItemBundle> AvailableBundles { get { return _availableBundles; } }

        /// <summary>
        /// Retrieves the next item-bundle from a dynamic list of available bundles for the retriever. Every retrieved item will be consumed and removed from the personalized list of the retriever.
        /// </summary>
        /// <param name="retriever">The object retrieving the next bundle.</param>
        /// <returns>The next bundle on the list or <code>null</code> if no bundle is left.</returns>
        public ItemBundle RetrieveBundle(object retriever)
        {
            // Init list if not present
            if (!_personalizedAvailableBundles.ContainsKey(retriever))
                _personalizedAvailableBundles[retriever] = _availableBundles.ToList();
            // Retrieve bundle
            ItemBundle bundle = _personalizedAvailableBundles[retriever].FirstOrDefault();
            // Remove bundle if there was one
            if (bundle != null)
                _personalizedAvailableBundles[retriever].RemoveAt(0);
            // Return it
            return bundle;
        }
        /// <summary>
        /// Contains all bundles personalized per retrieving object.
        /// </summary>
        private Dictionary<object, List<ItemBundle>> _personalizedAvailableBundles = new Dictionary<object, List<ItemBundle>>();

        /// <summary>
        /// Called by the system when a new bundle has been assigned to an input-station.
        /// </summary>
        /// <param name="iStation">The station the bundle was assigned to.</param>
        /// <param name="bundle">The assigned bundle.</param>
        public void NewBundleAssignedToStation(InputStation iStation, ItemBundle bundle) { /* Not used right now */ }

        /// <summary>
        /// Marks the given bundle as stored.
        /// </summary>
        /// <param name="bundle">The stored bundle.</param>
        public void CompleteBundle(ItemBundle bundle)
        {
            // Store it
            lock (_syncRoot)
            {
                _completedBundles.Add(bundle);
                _openBundles.Remove(bundle);
            }
        }

        /// <summary>
        /// Resets the statistics.
        /// </summary>
        public void ResetStatistics() { lock (_syncRoot) { _completedOrders = new List<Order>(); _completedBundles = new List<ItemBundle>(); } }

        #endregion

        #region IItemManagerInfo Members

        /// <summary>
        /// The object syncing is done with.
        /// </summary>
        private object _syncRoot = new object();
        /// <summary>
        /// Gets the number of currently available orders that are not yet allocated.
        /// </summary>
        /// <returns>The number of currently available orders that are not yet allocated.</returns>
        public int GetInfoPendingOrderCount() { return BacklogOrderCount; }
        /// <summary>
        /// Gets the number of currently available bundles that are not yet allocated.
        /// </summary>
        /// <returns>The number of currently available bundles that are not yet allocated.</returns>
        public int GetInfoPendingBundleCount() { return BacklogBundleCount; }
        /// <summary>
        /// Gets an enumeration of the currently pending orders. Hence, all orders not yet assigned to any station.
        /// </summary>
        /// <returns>The orders currently pending.</returns>
        public IEnumerable<IOrderInfo> GetInfoPendingOrders() { lock (_syncRoot) { return _availableOrders.ToList(); } }
        /// <summary>
        /// Gets an enumeration of the currently open orders. This are all orders currently assigned to a station.
        /// </summary>
        /// <returns>The orders currently open.</returns>
        public IEnumerable<IOrderInfo> GetInfoOpenOrders() { lock (_syncRoot) { return _openOrders.ToList(); } }
        /// <summary>
        /// Gets an enumeration of the already completed orders.
        /// </summary>
        /// <returns>The orders already completed.</returns>
        public IEnumerable<IOrderInfo> GetInfoCompletedOrders() { lock (_syncRoot) { return _completedOrders.ToList(); } }

        #endregion

        #region IUpdateable Members

        /// <summary>
        /// The next event when this element has to be updated.
        /// </summary>
        /// <param name="currentTime">The current time of the simulation.</param>
        /// <returns>The next time this element has to be updated.</returns>
        public double GetNextEventTime(double currentTime)
        {
            // Determine next event
            double nextEvent = double.PositiveInfinity;
            // Init event according to mode
            if (Instance.SettingConfig.InventoryConfiguration.OrderMode == OrderMode.Fixed)
            {
                // Use event of next order or bundle to submit
                nextEvent = Math.Min(_futureBundles.Any() ? _futureBundles.First().TimeStamp : double.PositiveInfinity, _futureOrders.Any() ? _futureOrders.First().TimeStamp : double.PositiveInfinity);
            }
            else if (Instance.SettingConfig.InventoryConfiguration.OrderMode == OrderMode.Poisson)
            {
                // Use event of next order or bundle to generate
                nextEvent = Math.Min(_nextPoissonBundleGenerationTime, _nextPoissonOrderGenerationTime);
                // If in high/low mode also check for switch event
                if (Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.PoissonMode == PoissonMode.HighLow)
                    nextEvent = MathHelpers.Min(nextEvent, OrderPoissonGenerator.NextHighLowSwitch, BundlePoissonGenerator.NextHighLowSwitch);
            }
            if (Instance.SettingConfig.InventoryConfiguration.SubmitBatches)
            {
                // Use next batch generation event, if it comes before the other ones
                nextEvent = MathHelpers.Min(
                    nextEvent,
                    _batchNextBundleCycle,
                    _batchNextOrderCycle,
                    _batchTimepointsBundles.Any() ? _batchTimepointsBundles.First().Item1 : double.PositiveInfinity,
                    _batchTimepointsOrders.Any() ? _batchTimepointsOrders.First().Item1 : double.PositiveInfinity);
            }
            if (_downPeriodHandling)
            {
                // Use next down period / up period change, if it happens before all other events
                nextEvent = MathHelpers.Min(
                    nextEvent,
                    _downPeriodNextBundleCycle,
                    _downPeriodNextOrderCycle,
                    _downPeriodTimepointsBundles.Any() ? _downPeriodTimepointsBundles.First().Item1 : double.PositiveInfinity,
                    _downPeriodTimepointsOrders.Any() ? _downPeriodTimepointsOrders.First().Item1 : double.PositiveInfinity);
            }
            return nextEvent;
        }
        /// <summary>
        /// Updates the element to the specified time.
        /// </summary>
        /// <param name="lastTime">The time before the update.</param>
        /// <param name="currentTime">The time to update to.</param>
        public void Update(double lastTime, double currentTime)
        {
            // --> Generate batches of orders and bundles
            if (Instance.SettingConfig.InventoryConfiguration.SubmitBatches)
            {
                #region Batch submission update

                // Track order batch cycles
                if (_batchNextOrderCycle <= currentTime)
                {
                    _batchTimepointsOrders = Instance.SettingConfig.InventoryConfiguration.BatchInventoryConfiguration.OrderBatches
                        .Select(e => new Tuple<double, int>(_batchNextOrderCycle + e.Key, e.Value))
                        .OrderBy(e => e.Item1)
                        .ToList();
                    _batchNextOrderCycle += Instance.SettingConfig.InventoryConfiguration.BatchInventoryConfiguration.MaxTimeForOrderSubmissions;
                }
                // Track bundle batch cycles
                if (_batchNextBundleCycle <= currentTime)
                {
                    _batchTimepointsBundles = Instance.SettingConfig.InventoryConfiguration.BatchInventoryConfiguration.BundleBatches
                        .Select(e => new Tuple<double, double>(_batchNextBundleCycle + e.Key, e.Value))
                        .OrderBy(e => e.Item1)
                        .ToList();
                    _batchNextBundleCycle += Instance.SettingConfig.InventoryConfiguration.BatchInventoryConfiguration.MaxTimeForBundleSubmissions;
                }
                // See whether we approached the next order batch generation
                if (_batchTimepointsOrders.Any() && _batchTimepointsOrders.First().Item1 <= currentTime)
                {
                    // Add orders until we reached the goal of the batch
                    while (_availableOrders.Count < Instance.OutputStations.Count * _batchTimepointsOrders.First().Item2)
                    {
                        // Generate a new order
                        Order order = GenerateRandomOrder();
                        // Only submit order if we successfully generated one
                        if (order != null)
                        {
                            // Synchronize with visualization
                            lock (_syncRoot)
                            {
                                // Add it to the end
                                _availableOrders.Add(order);
                                // Keep personalized list up-to-date
                                foreach (var retriever in _personalizedAvailableOrders.Keys)
                                    _personalizedAvailableOrders[retriever].Add(order);
                                // Notify instance about new order
                                Instance.NotifyOrderPlaced(order);
                            }
                        }
                        else
                        {
                            // There is no order we can generate right now - quit trying for a while
                            Instance.LogInfo("Cannot generate further orders - suspending generation for now");
                            _orderGenerationBlockedUntil = currentTime + ORDER_GENERATION_TIMEOUT;
                            break;
                        }
                    }
                    // Remove this timepoint
                    _batchTimepointsOrders.RemoveAt(0);
                }
                // See whether we approached the next bundle batch generation
                if (_batchTimepointsBundles.Any() && _batchTimepointsBundles.First().Item1 <= currentTime)
                {
                    // Fill the list of available bundles
                    while (Instance.StatStorageFillAndReservedAndBacklogLevel < _batchTimepointsBundles.First().Item2)
                    {
                        // Generate a new bundle
                        ItemBundle newBundle = GenerateBundle();
                        // Only submit bundle if we successfully generated one
                        if (newBundle != null)
                        {
                            // Synchronize with visualization
                            lock (_syncRoot)
                            {
                                // Add the new bundle
                                _availableBundles.Add(newBundle);
                                // Keep personalized list up-to-date
                                foreach (var retriever in _personalizedAvailableBundles.Keys)
                                    _personalizedAvailableBundles[retriever].Add(newBundle);
                                // Notify the instance about the new bundle
                                Instance.NotifyBundlePlaced(newBundle);
                            }
                        }
                        else
                        {
                            // There is no bundle we can generate right now - quit trying for a while
                            Instance.LogInfo("Cannot generate further bundles - suspending generation for now");
                            _bundleGenerationBlockedUntil = currentTime + BUNDLE_GENERATION_TIMEOUT;
                            break;
                        }
                    }
                    // Remove this timepoint
                    _batchTimepointsBundles.RemoveAt(0);
                }

                #endregion
            }
            // --> Handle down times, if active
            if (_downPeriodHandling)
            {
                #region Down period handling

                // Track bundle down time cycles
                if (_downPeriodNextBundleCycle <= currentTime)
                {
                    _downPeriodTimepointsBundles = Instance.SettingConfig.InventoryConfiguration.DemandInventoryConfiguration.DownPeriodConfiguration.BundleDownAndUpTimes
                        .Select(e => new Tuple<double, bool>(_downPeriodNextBundleCycle + e.Key, e.Value))
                        .OrderBy(e => e.Item1)
                        .ToList();
                    _downPeriodNextBundleCycle += Instance.SettingConfig.InventoryConfiguration.DemandInventoryConfiguration.DownPeriodConfiguration.MaxTimeForBundleDownAndUpPeriods;
                }
                // Track order down time cycles
                if (_downPeriodNextOrderCycle <= currentTime)
                {
                    _downPeriodTimepointsOrders = Instance.SettingConfig.InventoryConfiguration.DemandInventoryConfiguration.DownPeriodConfiguration.OrderDownAndUpTimes
                        .Select(e => new Tuple<double, bool>(_downPeriodNextOrderCycle + e.Key, e.Value))
                        .OrderBy(e => e.Item1)
                        .ToList();
                    _downPeriodNextOrderCycle += Instance.SettingConfig.InventoryConfiguration.DemandInventoryConfiguration.DownPeriodConfiguration.MaxTimeForOrderDownAndUpPeriods;
                }
                // See whether we approached the next change in down / up period for bundles
                if (_downPeriodTimepointsBundles.Any() && _downPeriodTimepointsBundles.First().Item1 <= currentTime)
                {
                    // Set down / up time
                    _downPeriodActiveForBundles = _downPeriodTimepointsBundles.First().Item2;
                    // Remove this timepoint
                    _downPeriodTimepointsBundles.RemoveAt(0);
                }
                // See whether we approached the next change in down / up period for orders
                if (_downPeriodTimepointsOrders.Any() && _downPeriodTimepointsOrders.First().Item1 <= currentTime)
                {
                    // Set down / up time
                    _downPeriodActiveForOrders = _downPeriodTimepointsOrders.First().Item2;
                    // Remove this timepoint
                    _downPeriodTimepointsOrders.RemoveAt(0);
                }

                #endregion
            }
            // --> Generate bundles and orders according to mode
            switch (Instance.SettingConfig.InventoryConfiguration.OrderMode)
            {
                case OrderMode.Fill:
                    {
                        #region Fill mode update

                        // Order generation allowed?
                        if (// Check whether order generation currently fails and is blocked temporarily
                            _orderGenerationBlockedUntil < currentTime &&
                            // Check whether there is an ongoing down period
                            !_downPeriodActiveForOrders)
                        {
                            // Fill the list of available orders
                            while (_availableOrders.Count < Instance.SettingConfig.InventoryConfiguration.DemandInventoryConfiguration.OrderCount)
                            {
                                // Check for generation pause
                                if (CheckForOrderGenerationPause())
                                    break;

                                // Generate a new order
                                Order order = GenerateRandomOrder();
                                // Only submit order if we successfully generated one
                                if (order != null)
                                {
                                    // Synchronize with visualization
                                    lock (_syncRoot)
                                    {
                                        // Add it to the end
                                        _availableOrders.Add(order);
                                        // Keep personalized list up-to-date
                                        foreach (var retriever in _personalizedAvailableOrders.Keys)
                                            _personalizedAvailableOrders[retriever].Add(order);
                                        // Notify instance about new order
                                        Instance.NotifyOrderPlaced(order);
                                    }
                                }
                                else
                                {
                                    // There is no order we can generate right now - quit trying for a while
                                    Instance.LogInfo("Cannot generate further orders - suspending generation for now");
                                    _orderGenerationBlockedUntil = currentTime + ORDER_GENERATION_TIMEOUT;
                                    break;
                                }
                            }
                        }
                        // Bundle generation allowed?
                        if (// Check whether bundle generation currently fails and is blocked temporarily
                            _bundleGenerationBlockedUntil < currentTime &&
                            // Check whether there is an ongoing down period
                            !_downPeriodActiveForBundles)
                        {
                            // Get amount of real available bundles
                            int count = _availableBundles.Count;

                            // Fill the list of available bundles
                            while (count < Instance.SettingConfig.InventoryConfiguration.DemandInventoryConfiguration.BundleCount)
                            {
                                // Check for generation pause
                                if (CheckForBundleGenerationPause())
                                    break;

                                // Generate a new bundle
                                ItemBundle newBundle = GenerateBundle();
                                // Only submit bundle if we successfully generated one
                                if (newBundle != null)
                                {
                                    // Synchronize with visualization
                                    lock (_syncRoot)
                                    {
                                        // Add the new bundle
                                        _availableBundles.Add(newBundle);
                                        // Keep personalized list up-to-date
                                        foreach (var retriever in _personalizedAvailableBundles.Keys)
                                            _personalizedAvailableBundles[retriever].Add(newBundle);
                                        // Notify the instance about the new bundle
                                        Instance.NotifyBundlePlaced(newBundle);
                                    }
                                }
                                else
                                {
                                    // There is no bundle we can generate right now - quit trying for a while
                                    Instance.LogInfo("Cannot generate further bundles - suspending generation for now");
                                    _bundleGenerationBlockedUntil = currentTime + BUNDLE_GENERATION_TIMEOUT;
                                    break;
                                }

                                // Update real count
                                count = _availableBundles.Count;
                            }
                        }

                        #endregion
                    }
                    break;
                case OrderMode.Poisson:
                    {
                        #region Poisson mode update

                        // Determine distortion factors
                        double orderDistortionFactor = 1;
                        double bundleDistortionFactor = 1;
                        switch (Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.DistortOrderRateParameter)
                        {
                            case PoissonDistortionType.PickStationsActivated: orderDistortionFactor = Math.Max(Instance.OutputStations.Count(s => s.Active), 1.0) / Instance.OutputStations.Count; break;
                            case PoissonDistortionType.ReplenishmentStationsActivated: orderDistortionFactor = Math.Max(Instance.InputStations.Count(s => s.Active), 1.0) / Instance.InputStations.Count; break;
                            default: break;
                        }
                        switch (Instance.SettingConfig.InventoryConfiguration.PoissonInventoryConfiguration.DistortBundleRateParameter)
                        {
                            case PoissonDistortionType.PickStationsActivated: bundleDistortionFactor = Math.Max(Instance.OutputStations.Count(s => s.Active), 1.0) / Instance.OutputStations.Count; break;
                            case PoissonDistortionType.ReplenishmentStationsActivated: bundleDistortionFactor = Math.Max(Instance.InputStations.Count(s => s.Active), 1.0) / Instance.InputStations.Count; break;
                            default: break;
                        }

                        // See whether we first have to conduct the high low switch (performed by the poisson generator, but artificially breaking for the event here)
                        if (OrderPoissonGenerator.NextHighLowSwitch <= currentTime)
                            OrderPoissonGenerator.Next(currentTime, orderDistortionFactor);
                        if (BundlePoissonGenerator.NextHighLowSwitch <= currentTime)
                            BundlePoissonGenerator.Next(currentTime, bundleDistortionFactor);
                        // See whether we approached the next order generation
                        if (_nextPoissonOrderGenerationTime <= currentTime)
                        {
                            // Generate a random order
                            Order order = GenerateRandomOrder();
                            // Only submit order if we successfully generated one
                            if (order != null)
                            {
                                // Synchronize with visualization
                                lock (_syncRoot)
                                {
                                    _availableOrders.Add(order);
                                    // Keep personalized list up-to-date
                                    foreach (var retriever in _personalizedAvailableOrders.Keys)
                                        _personalizedAvailableOrders[retriever].Add(order);
                                    // Notify the instance about the new order
                                    Instance.NotifyOrderPlaced(order);
                                }
                            }
                            else
                            {
                                // Log the fail
                                Instance.LogInfo("Order generation failed - no stock available!?");
                                // Notify the instance
                                Instance.NotifyOrderRejected();
                            }
                            // Determine next order generation timestamp
                            _nextPoissonOrderGenerationTime = currentTime + OrderPoissonGenerator.Next(currentTime, orderDistortionFactor);
                        }
                        // See whether we approached the next bundle generation
                        if (_nextPoissonBundleGenerationTime <= currentTime)
                        {
                            // Generate a random bundle
                            ItemBundle bundle = GenerateBundle();
                            // Only submit bundle if we successfully generated one
                            if (bundle != null)
                            {
                                // Synchronize with visualization
                                lock (_syncRoot)
                                {
                                    _availableBundles.Add(bundle);
                                    // Keep personalized list up-to-date
                                    foreach (var retriever in _personalizedAvailableBundles.Keys)
                                        _personalizedAvailableBundles[retriever].Add(bundle);
                                    // Notify the instance about the new bundle
                                    Instance.NotifyBundlePlaced(bundle);
                                }
                            }
                            else
                            {
                                // Log the fail
                                Instance.LogInfo("Bundle generation failed - no capacity available!?");
                                // Notify the instance
                                Instance.NotifyBundleRejected();
                            }
                            // Determine next bundle generation timestamp
                            _nextPoissonBundleGenerationTime = currentTime + BundlePoissonGenerator.Next(currentTime, bundleDistortionFactor);
                        }

                        #endregion
                    }
                    break;
                case OrderMode.Fixed:
                    {
                        #region Fixed mode update

                        // Only place new orders if there are any
                        if (_futureOrders.Any() && _futureOrders.First().TimeStamp <= currentTime)
                        {
                            // Place all new orders
                            List<Order> newOrders = _futureOrders.TakeWhile(o => o.TimeStamp <= currentTime).ToList();
                            foreach (var order in newOrders)
                                _availableOrders.Add(order);
                            _futureOrders.RemoveRange(0, newOrders.Count);
                            // Keep personalized list up-to-date
                            foreach (var retriever in _personalizedAvailableOrders.Keys)
                                _personalizedAvailableOrders[retriever].AddRange(newOrders);
                            // Notify the instance about all new orders placed
                            foreach (var newOrder in newOrders)
                                Instance.NotifyOrderPlaced(newOrder);
                        }
                        // Only place new bundles if there are any
                        if (_futureBundles.Any() && _futureBundles.First().TimeStamp <= currentTime)
                        {
                            // Place all new bundles
                            List<ItemBundle> newBundles = _futureBundles.TakeWhile(o => o.TimeStamp <= currentTime).ToList();
                            foreach (var bundle in newBundles)
                                _availableBundles.Add(bundle);
                            _futureBundles.RemoveRange(0, newBundles.Count);
                            // Keep personalized list up-to-date
                            foreach (var retriever in _personalizedAvailableBundles.Keys)
                                _personalizedAvailableBundles[retriever].AddRange(newBundles);
                            // Notify the instance about all new bundles placed
                            foreach (var newBundle in newBundles)
                                Instance.NotifyBundlePlaced(newBundle);
                        }

                        #endregion
                    }
                    break;
                default: throw new ArgumentException("Unknown order-mode: " + Instance.SettingConfig.InventoryConfiguration.OrderMode.ToString());
            }
        }

        #endregion

        #region Information supply

        /// <summary>
        /// Returns the static frequency of the given item type. This is the value currently used for generating new orders and bundles (depending on the current item type).
        /// </summary>
        /// <param name="item">The item to return the frequency for.</param>
        /// <returns>The frequency of the item. This value is equal to the probability for generating the item (when ignoring combined probabilities).</returns>
        internal double GetItemProbability(ItemDescription item) { return _itemDescriptionProbabilities.ContainsKey(item) ? _itemDescriptionProbabilities[item] : 0; }
        /// <summary>
        /// Returns the current maximal probability over all item descriptions.
        /// </summary>
        /// <returns>The current maximal probability.</returns>
        internal double GetItemProbabilityMax() { return _itemDescriptionProbabilityMax; }

        #endregion

        #region Debug stuff

        // TODO remove debug again
        private void OrderAnalyzer()
        {
            Console.WriteLine("StockInfo:");
            Dictionary<ItemDescription, int> actualStock = Instance.ItemDescriptions.ToDictionary(k => k, v => Instance.Pods.Sum(p => p.CountContained(v)));
            Dictionary<ItemDescription, int> availableOrdersDemand = _availableOrders.SelectMany(o => o.Positions).GroupBy(p => p.Key).ToDictionary(g => g.Key, v => v.Sum(p => p.Value));
            Dictionary<ItemDescription, int> allocatedOrdersDemand = Instance.OutputStations.SelectMany(o => o.AssignedOrders).SelectMany(o => o.Positions).GroupBy(p => p.Key).ToDictionary(g => g.Key, v => v.Sum(p => p.Value));
            Console.WriteLine(
                        "ItemDescription: " +
                        "availOrdersDemand/" +
                        "allocatedOrdersDemand/" +
                        "actualStock/" +
                        "availStock/" +
                        "measuredStock");
            foreach (var description in availableOrdersDemand.Keys.OrderBy(k => k.ID))
            {
                Console.WriteLine(
                        description.ToDescriptiveString() + ": " +
                        (availableOrdersDemand.ContainsKey(description) ? availableOrdersDemand[description].ToString() : "na") + "/" +
                        (allocatedOrdersDemand.ContainsKey(description) ? allocatedOrdersDemand[description].ToString() : "na") + "/" +
                        Instance.StockInfo.GetActualStock(description) + "/" +
                        Instance.StockInfo.GetAvailableStock(description) + "/" +
                        actualStock[description]);
            }
        }

        #endregion
    }
}
