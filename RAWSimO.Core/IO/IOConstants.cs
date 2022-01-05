using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.IO
{
    /// <summary>
    /// Defines certain constants used for serialization and deserialization.
    /// </summary>
    public class IOConstants
    {
        #region Delimiters and Formatting

        /// <summary>
        /// The basic formatter to use for string-representations of values.
        /// </summary>
        public static CultureInfo FORMATTER = CultureInfo.InvariantCulture;

        /// <summary>
        /// A format pattern to use when shortened string representations of values are desired.
        /// </summary>
        public const string EXPORT_FORMAT_SHORT = "F";

        /// <summary>
        /// A format pattern to use when even shorter string represenstations of values are desired.
        /// </summary>
        public const string EXPORT_FORMAT_SHORTER = "0.##";

        /// <summary>
        /// A format pattern to use when shortest string represenstations of values are desired.
        /// </summary>
        public const string EXPORT_FORMAT_SHORTEST_BY_ROUNDING = "F0";

        /// <summary>
        /// A format pattern to use when exporting a timespan as human readable that has to include days, e.g. the current simulation time after converting the seconds to <code>TimeSpan</code>.
        /// </summary>
        public const string TIMESPAN_FORMAT_HUMAN_READABLE_DAYS = "d\\.hh\\:mm\\:ss";

        /// <summary>
        /// A format pattern to use when exporting a timespan as human readable that has to include minutes, e.g. the current blocked time of a robot after converting the seconds to <code>TimeSpan</code>.
        /// </summary>
        public const string TIMESPAN_FORMAT_HUMAN_READABLE_MINUTES = "mm\\:ss\\.ff";

        /// <summary>
        /// String for indicating a following comment line.
        /// </summary>
        public const string COMMENT_LINE = "#";

        /// <summary>
        /// String for indicating a following block-start.
        /// </summary>
        public const string BLOCKNAME_START = "$";

        /// <summary>
        /// Character indicating the end of a blockname.
        /// </summary>
        public const char BLOCKNAME_END = ':';

        /// <summary>
        /// String that terminates each line when writing a CSV-file.
        /// </summary>
        public const string LINE_TERMINATOR = "\n";

        /// <summary>
        /// Delimiter for separating values.
        /// </summary>
        public const char DELIMITER_VALUE = ';';

        /// <summary>
        /// Delimiter for list-values.
        /// </summary>
        public const char DELIMITER_LIST = ',';

        /// <summary>
        /// Delimiter for tuples.
        /// </summary>
        public const char DELIMITER_TUPLE = '/';

        /// <summary>
        /// Delimiter for special purposes.
        /// </summary>
        public const char DELIMITER_SPECIAL = '-';

        /// <summary>
        /// Delimiter for custom performance footprint strings.
        /// </summary>
        public const char DELIMITER_CUSTOM_CONTROLLER_FOOTPRINT = '_';

        #endregion

        #region Gnuplot delimiters

        /// <summary>
        /// Delimiter for separating values to use in gnuplot.
        /// </summary>
        public const char GNU_PLOT_VALUE_SPLIT = ' ';

        /// <summary>
        /// Marker for a gnuplot comment line.
        /// </summary>
        public const char GNU_PLOT_COMMENT_LINE = '#';

        #endregion

        #region Heat info characters

        /// <summary>
        /// The character denoting the start of a tag identifying the heat info type.
        /// </summary>
        public const string STAT_HEAT_TAG_START = "<";
        /// <summary>
        /// The character denoting the end of a tag identifying the heat info type.
        /// </summary>
        public const string STAT_HEAT_TAG_END = ">:";

        #endregion

        #region Log filename

        /// <summary>
        /// The name of the log file to use (if desired).
        /// </summary>
        public const string LOG_FILE = "output.log";

        #endregion

        #region Simulation statistics filenames

        /// <summary>
        /// Enumerates all statistics files written out by the simulation (every file needs to be contained in here!).
        /// </summary>
        public enum StatFile
        {
            /// <summary>
            /// The file containing the instance name.
            /// </summary>
            InstanceName,
            /// <summary>
            /// The file containing the setting name.
            /// </summary>
            SettingName,
            /// <summary>
            /// The file containing the controller name.
            /// </summary>
            ControllerName,
            /// <summary>
            /// The item-progression file.
            /// </summary>
            ItemProgressionRaw,
            /// <summary>
            /// The bundle-progression file.
            /// </summary>
            BundleProgressionRaw,
            /// <summary>
            /// The order-progression file.
            /// </summary>
            OrderProgressionRaw,
            /// <summary>
            /// The bundle-placement-progression file.
            /// </summary>
            BundlePlacementProgressionRaw,
            /// <summary>
            /// The order-placement-progression file.
            /// </summary>
            OrderPlacementProgressionRaw,
            /// <summary>
            /// The collision-progression file.
            /// </summary>
            CollisionProgressionRaw,
            /// <summary>
            /// The completed trips progression file.
            /// </summary>
            TripsCompletedProgressionRaw,
            /// <summary>
            /// The traveled distance progression file.
            /// </summary>
            TraveledDistanceProgressionRaw,
            /// <summary>
            /// The file storing all inventory levels polled.
            /// </summary>
            InventoryLevelPollingRaw,
            /// <summary>
            /// The file storing all performance polls.
            /// </summary>
            PerformancePollingRaw,
            /// <summary>
            /// The file storing all backlog levels polled.
            /// </summary>
            BundleOrderSituationPollingRaw,
            /// <summary>
            /// The file storing all station statistics polled.
            /// </summary>
            StationPollingRaw,
            /// <summary>
            /// The file storing all bot info statistics polled.
            /// </summary>
            BotInfoPollingRaw,
            /// <summary>
            /// The file storing all well sortedness polled.
            /// </summary>
            WellSortednessPollingRaw,
            /// <summary>
            /// The readable statistics file.
            /// </summary>
            ReadableStatistics,
            /// <summary>
            /// The file containing statistics about the stations.
            /// </summary>
            StationStatistics,
            /// <summary>
            /// The file containing detailed statistics about the connections the bot used.
            /// </summary>
            ConnectionStatistics,
            /// <summary>
            /// The file containing detailed information about all SKUs and how they were ordered during simulation.
            /// </summary>
            ItemDescriptionStatistics,
            /// <summary>
            /// The file containing the main stat values of the corresponding run.
            /// </summary>
            Footprint,
            /// <summary>
            /// The pathfinding file.
            /// </summary>
            PathFinding,
            /// <summary>
            /// The file storing all the locations polled.
            /// </summary>
            HeatLocationPolling,
            /// <summary>
            /// The file storing the trip information.
            /// </summary>
            HeatTrips,
            /// <summary>
            /// The file storing all the pod info polled.
            /// </summary>
            HeatStorageLocationPolling,
            /// <summary>
            /// The file for storing the individual separate performance result for the async order manager, if present.
            /// </summary>
            IndividualPerformanceFileOrderManagerAsyncSeparate,
            /// <summary>
            /// The file for storing the individual consolidated performance result for the async order manager, if present.
            /// </summary>
            IndividualPerformanceFileOrderManagerAsyncConsolidated,
        }
        /// <summary>
        /// Contains all names to use for the statistic files defined.
        /// </summary>
        public static readonly Dictionary<StatFile, string> StatFileNames = new Dictionary<StatFile, string>()
        {
            { StatFile.InstanceName, "instance.txt" },
            { StatFile.SettingName, "setting.txt" },
            { StatFile.ControllerName, "controller.txt" },
            { StatFile.ItemProgressionRaw, "itemprogression.csv" },
            { StatFile.BundleProgressionRaw, "bundleprogression.csv" },
            { StatFile.OrderProgressionRaw, "orderprogression.csv" },
            { StatFile.BundlePlacementProgressionRaw, "bundleplacementprogression.csv" },
            { StatFile.OrderPlacementProgressionRaw, "orderplacementprogression.csv" },
            { StatFile.CollisionProgressionRaw, "collisionprogression.csv" },
            { StatFile.TripsCompletedProgressionRaw, "tripscompleted.csv" },
            { StatFile.TraveledDistanceProgressionRaw, "traveleddistanceprogression.csv" },
            { StatFile.InventoryLevelPollingRaw, "inventorylevel.csv" },
            { StatFile.PerformancePollingRaw, "performance.csv" },
            { StatFile.BundleOrderSituationPollingRaw, "bundleordersituations.csv" },
            { StatFile.StationPollingRaw, "stationprogression.csv" },
            { StatFile.BotInfoPollingRaw, "botinfoprogression.csv" },
            { StatFile.WellSortednessPollingRaw, "wellsortedness.csv" },
            { StatFile.ReadableStatistics, "statistics.txt" },
            { StatFile.StationStatistics, "stationstatistics.csv" },
            { StatFile.ConnectionStatistics, "connectionstatistics.csv" },
            { StatFile.ItemDescriptionStatistics, "itemdescriptionstatistics.csv" },
            { StatFile.Footprint, "footprint.csv" },
            { StatFile.PathFinding, "pathfinding.csv" },
            { StatFile.HeatLocationPolling, "locationspolled.heat" },
            { StatFile.HeatTrips, "trips.heat" },
            { StatFile.HeatStorageLocationPolling, "storagelocationinfopolled.heat" },
            { StatFile.IndividualPerformanceFileOrderManagerAsyncSeparate, "ordermanagerasyncseparate.csv" },
            { StatFile.IndividualPerformanceFileOrderManagerAsyncConsolidated, "ordermanagerasyncconsolidated.csv" },
        };

        #endregion

        #region Intermediate result files

        /// <summary>
        /// Name of the item-progression result files.
        /// </summary>
        public const string STAT_ITEM_PROGRESSION_RESULT_FILENAME = "itemprogressionresults";
        /// <summary>
        /// Name of the bundle-progression result files.
        /// </summary>
        public const string STAT_BUNDLE_PROGRESSION_RESULT_FILENAME = "bundleprogressionresults";
        /// <summary>
        /// Name of the order-progression result files.
        /// </summary>
        public const string STAT_ORDER_PROGRESSION_RESULT_FILENAME = "orderprogressionresults";
        /// <summary>
        /// Name of the collision-progression result files.
        /// </summary>
        public const string STAT_COLLISION_PROGRESSION_RESULT_FILENAME = "collisionprogressionresults";
        /// <summary>
        /// Name of the traveled distance progression result files.
        /// </summary>
        public const string STAT_TRAVELED_DISTANCE_PROGRESSION_RESULT_FILENAME = "traveleddistanceprogression";
        /// <summary>
        /// Name of the progression script files.
        /// </summary>
        public const string STAT_PROGRESSION_SCRIPT_FILENAME = "progressionscript";
        /// <summary>
        /// Name of the file containing all the KPI-values of the runs.
        /// </summary>
        public const string STAT_CONSOLIDATED_FOOTPRINTS_FILENAME = "footprints.csv";

        #endregion

        #region Default Paths

        /// <summary>
        /// The default paths to search for resource files.
        /// </summary>
        public static readonly List<string> DEFAULT_RESOURCE_DIRS = new List<string>
        {
            Path.Combine("Material", "Resources", "Wordlists"),
            Path.Combine("Material", "Resources"),
            Path.Combine("repo", "Material", "Resources", "Wordlists"),
            Path.Combine("repo", "Material", "Resources"),
            Path.Combine("..", "..", "..", "Material", "Resources", "Wordlists"),
            Path.Combine("..", "..", "..", "Material", "Resources"),
            Path.Combine("..", "..", "..", "..", "Material", "Resources", "Wordlists"),
            Path.Combine("..", "..", "..", "..", "Material", "Resources"),
            Path.Combine("Resources", "Wordlists"),
            Path.Combine("Resources"),
            Path.Combine("Wordlists"),
        };

        #endregion
    }
}
