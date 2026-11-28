using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RAWSimO.Core.Info;
using RAWSimO.Core.Items;
using SkiaSharp;

namespace RAWSimO.Visualization.Avalonia.Simulation
{
    /// <summary>
    /// An item (or order position) displayed as a small colored chip.
    /// </summary>
    /// <param name="Label">The text of the chip.</param>
    /// <param name="Color">The background color of the chip.</param>
    /// <param name="Done">Whether the position is complete (rendered with inverted text).</param>
    public readonly record struct ItemChip(string Label, SKColor Color, bool Done);

    /// <summary>
    /// An (immutable) view of an order.
    /// </summary>
    /// <param name="Tag">A short prefix (e.g. the station the order is assigned to), may be null.</param>
    public sealed record OrderView(string Tag, IReadOnlyList<ItemChip> Positions);

    /// <summary>
    /// An (immutable) view of a station.
    /// </summary>
    public sealed record StationView(string Name, bool IsOutput, bool Active, bool Blocked, int Assigned, string Requests, string Capacity, string Queue);

    /// <summary>
    /// Details of a selected object.
    /// </summary>
    /// <param name="Title">A title (e.g. "Pod 12").</param>
    /// <param name="Properties">Name / value pairs.</param>
    /// <param name="ChipsHeader">Header for <paramref name="Chips"/> (e.g. "Content"), null if there are none.</param>
    /// <param name="Chips">Items (pod content, station bundles).</param>
    /// <param name="Orders">Orders (output stations).</param>
    public sealed record SelectionDetail(string Title, IReadOnlyList<KeyValuePair<string, string>> Properties,
        string ChipsHeader, IReadOnlyList<ItemChip> Chips, IReadOnlyList<OrderView> OpenOrders, IReadOnlyList<OrderView> CompletedOrders);

    /// <summary>
    /// An immutable overview of the simulation state used by the info panel (statistics, stations, orders, selection).
    /// </summary>
    public sealed class Overview
    {
        public double Time { get; init; }
        public IReadOnlyList<KeyValuePair<string, string>> Statistics { get; init; }
        public IReadOnlyList<StationView> Stations { get; init; }
        public int PendingOrderCount { get; init; }
        public IReadOnlyList<OrderView> PendingOrders { get; init; }
        public int OpenOrderCount { get; init; }
        public IReadOnlyList<OrderView> OpenOrders { get; init; }
        public int CompletedOrderCount { get; init; }
        public IReadOnlyList<OrderView> CompletedOrders { get; init; }
        public SelectionDetail Selection { get; init; }
        /// <summary>
        /// The object the <see cref="Selection"/> describes.
        /// </summary>
        public object SelectedObject { get; init; }
    }

    /// <summary>
    /// Builds <see cref="Overview"/>s. Must be called on the thread executing the simulation (or while it is not running).
    /// </summary>
    public sealed class OverviewBuilder
    {
        /// <summary>
        /// Maximal number of orders per list (the counts are always complete).
        /// </summary>
        public const int MaxOrdersPerList = 200;

        private static readonly Dictionary<LetterColors, SKColor> LetterColorMap = new Dictionary<LetterColors, SKColor>
        {
            { LetterColors.Blue, new SKColor(100, 149, 237) }, // CornflowerBlue
            { LetterColors.Green, new SKColor(50, 205, 50) }, // LimeGreen
            { LetterColors.Red, new SKColor(205, 92, 92) }, // IndianRed
            { LetterColors.Yellow, new SKColor(255, 255, 0) },
        };

        private readonly IInstanceInfo _instance;
        private readonly Func<double> _time;
        private volatile object _selected;

        public OverviewBuilder(IInstanceInfo instance, Func<double> time)
        {
            _instance = instance;
            _time = time;
        }

        /// <summary>
        /// The object to provide details for (can be set from any thread).
        /// </summary>
        public object Selected { get => _selected; set => _selected = value; }

        private static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
        private static string Fill(double used, double capacity) => capacity > 0 ? Percent(used / capacity) : "-";
        private static string Percent(double v) => (v * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%";

        /// <summary>
        /// Returns the color of the given item description.
        /// </summary>
        public static SKColor GetItemColor(IItemDescriptionInfo item) => item switch
        {
            IColoredLetterDescriptionInfo letter => LetterColorMap.TryGetValue(letter.GetInfoColor(), out SKColor c) ? c : SKColors.LightGray,
            ISimpleItemDescriptionInfo simple => SKColor.FromHsv((float)Math.Clamp(simple.GetInfoHue(), 0, 360), 80, 97),
            _ => SKColors.LightGray,
        };

        private static string GetItemName(IItemDescriptionInfo item) => item switch
        {
            IColoredLetterDescriptionInfo letter => letter.GetInfoLetter().ToString(),
            ISimpleItemDescriptionInfo simple => simple.GetInfoID().ToString(CultureInfo.InvariantCulture),
            _ => item.GetInfoDescription(),
        };

        private static OrderView CreateOrderView(IOrderInfo order, string tag) =>
            new OrderView(tag, order.GetInfoPositions().Select(p =>
            {
                int served = order.GetInfoServedCount(p), demand = order.GetInfoDemandCount(p);
                return new ItemChip($"{GetItemName(p)} {served}/{demand}", GetItemColor(p), served >= demand);
            }).ToList());

        public Overview Build()
        {
            var outputStations = _instance.GetInfoTiers().SelectMany(t => t.GetInfoOutputStations()).ToList();
            var inputStations = _instance.GetInfoTiers().SelectMany(t => t.GetInfoInputStations()).ToList();
            // Remember the station of every open order (for tagging)
            var orderStations = new Dictionary<IOrderInfo, string>();
            foreach (var station in outputStations)
                foreach (var order in station.GetInfoOpenOrders())
                    orderStations[order] = "O" + station.GetInfoID();

            var stations = new List<StationView>();
            foreach (var s in inputStations)
                stations.Add(new StationView("I" + s.GetInfoID(), false, s.GetInfoActive(), s.GetInfoBlocked(), s.GetInfoAssignedBundles(),
                    $"{s.GetInfoOpenRequests()}/{s.GetInfoOpenBundles()}", Fill(s.GetInfoCapacityUsed(), s.GetInfoCapacity()), s.GetInfoQueue()));
            foreach (var s in outputStations)
                stations.Add(new StationView("O" + s.GetInfoID(), true, s.GetInfoActive(), s.GetInfoBlocked(), s.GetInfoAssignedOrders(),
                    $"{s.GetInfoOpenRequests()}/{s.GetInfoOpenItems()}", Fill(s.GetInfoCapacityUsed(), s.GetInfoCapacity()), s.GetInfoQueue()));

            IItemManagerInfo items = _instance.GetInfoItemManager();
            List<IOrderInfo> pending = items?.GetInfoPendingOrders().ToList() ?? new List<IOrderInfo>();
            List<IOrderInfo> open = items?.GetInfoOpenOrders().ToList() ?? new List<IOrderInfo>();
            List<IOrderInfo> completed = items?.GetInfoCompletedOrders().ToList() ?? new List<IOrderInfo>();

            var statistics = new List<KeyValuePair<string, string>>
            {
                new("Items handled", _instance.GetInfoStatItemsHandled().ToString(CultureInfo.InvariantCulture)),
                new("Bundles handled", _instance.GetInfoStatBundlesHandled().ToString(CultureInfo.InvariantCulture)),
                new("Orders handled", _instance.GetInfoStatOrdersHandled().ToString(CultureInfo.InvariantCulture)),
                new("Orders late", _instance.GetInfoStatOrdersLate().ToString(CultureInfo.InvariantCulture)),
                new("Pending orders", (items?.GetInfoPendingOrderCount() ?? 0).ToString(CultureInfo.InvariantCulture)),
                new("Pending bundles", (items?.GetInfoPendingBundleCount() ?? 0).ToString(CultureInfo.InvariantCulture)),
                new("Repositioning moves", _instance.GetInfoStatRepositioningMoves().ToString(CultureInfo.InvariantCulture)),
                new("Collisions", _instance.GetInfoStatCollisions().ToString(CultureInfo.InvariantCulture)),
                new("Storage fill level", Percent(_instance.GetInfoStatStorageFillLevel())),
                new("  + reserved", Percent(_instance.GetInfoStatStorageFillAndReservedLevel())),
                new("  + backlog", Percent(_instance.GetInfoStatStorageFillAndReservedAndBacklogLevel())),
            };

            object selected = _selected;
            return new Overview
            {
                Time = _time(),
                Statistics = statistics,
                Stations = stations,
                PendingOrderCount = pending.Count,
                PendingOrders = pending.Take(MaxOrdersPerList).Select(o => CreateOrderView(o, null)).ToList(),
                OpenOrderCount = open.Count,
                OpenOrders = open.Take(MaxOrdersPerList).Select(o => CreateOrderView(o, orderStations.TryGetValue(o, out string tag) ? tag : null)).ToList(),
                CompletedOrderCount = completed.Count,
                // Most recently completed first
                CompletedOrders = Enumerable.Range(0, Math.Min(MaxOrdersPerList, completed.Count)).Select(i => CreateOrderView(completed[completed.Count - 1 - i], null)).ToList(),
                Selection = selected == null ? null : SafeDescribe(selected),
                SelectedObject = selected,
            };
        }

        private SelectionDetail SafeDescribe(object obj)
        {
            try { return Describe(obj); }
            catch (Exception ex)
            {
                return new SelectionDetail(obj.GetType().Name, new[] { new KeyValuePair<string, string>("Error", ex.GetType().Name + ": " + ex.Message) }, null, null, null, null);
            }
        }

        private SelectionDetail Describe(object obj)
        {
            var p = new List<KeyValuePair<string, string>>();
            void Add(string name, object value) => p.Add(new KeyValuePair<string, string>(name, Convert.ToString(value, CultureInfo.InvariantCulture)));
            string Wp(IWaypointInfo wp) => wp?.GetInfoID().ToString(CultureInfo.InvariantCulture) ?? "-";
            void AddPosition(IGeneralObjectInfo o)
            {
                Add("Position", $"{F(o.GetInfoCenterX())}, {F(o.GetInfoCenterY())}");
                Add("Tier", o.GetInfoCurrentTier()?.GetInfoID());
            }

            switch (obj)
            {
                case IBotInfo bot:
                    Add("State", bot.GetInfoState());
                    Add("Task", bot.GetInfoTask());
                    AddPosition(bot);
                    Add("Orientation", F(bot.GetInfoOrientation() * 180 / Math.PI) + "°");
                    Add("Speed", F(bot.GetInfoSpeed()));
                    Add("Current waypoint", Wp(bot.GetInfoCurrentWaypoint()));
                    Add("Goal waypoint", Wp(bot.GetInfoGoalWaypoint()));
                    Add("Destination waypoint", Wp(bot.GetInfoDestinationWaypoint()));
                    Add("Path length", bot.GetInfoPath()?.Count ?? 0);
                    Add("Blocked", bot.GetInfoBlocked() ? $"yes ({F(bot.GetInfoBlockedLeft())} s left)" : "no");
                    Add("Queueing", bot.GetInfoIsQueueing() ? "yes" : "no");
                    return new SelectionDetail("Bot " + bot.GetInfoID(), p, null, null, null, null);
                case IPodInfo pod:
                    AddPosition(pod);
                    Add("Capacity", $"{F(pod.GetInfoCapacityUsed())} / {F(pod.GetInfoCapacity())}");
                    Add("Reserved", F(pod.GetInfoCapacityReserved()));
                    Add("Heat", F(pod.GetInfoHeatValue()));
                    Add("Ready for refill", pod.GetInfoReadyForRefill() ? "yes" : "no");
                    var content = new List<ItemChip>();
                    foreach (var item in _instance.GetInfoItemDescriptions())
                    {
                        int count;
                        try { count = pod.GetInfoContent(item); }
                        catch (Exception) { count = 0; }
                        if (count > 0)
                            content.Add(new ItemChip($"{GetItemName(item)} ×{count}", GetItemColor(item), false));
                    }
                    return new SelectionDetail("Pod " + pod.GetInfoID(), p, $"Content ({content.Count} SKUs)", content, null, null);
                case IInputStationInfo station:
                    AddPosition(station);
                    Add("Active", station.GetInfoActive() ? "yes" : "no");
                    Add("Blocked", station.GetInfoBlocked() ? $"yes ({F(station.GetInfoBlockedLeft())} s left)" : "no");
                    Add("Capacity", $"{F(station.GetInfoCapacityUsed())} / {F(station.GetInfoCapacity())} (reserved {F(station.GetInfoCapacityReserved())})");
                    Add("Assigned bundles", station.GetInfoAssignedBundles());
                    Add("Open requests / bundles", $"{station.GetInfoOpenRequests()} / {station.GetInfoOpenBundles()}");
                    Add("Activation order", station.GetInfoActivationOrderID());
                    Add("Queue", station.GetInfoQueue());
                    var bundles = station.GetInfoBundles()
                        .GroupBy(b => b.GetInfoItemDescription())
                        .Select(g => new ItemChip($"{GetItemName(g.Key)} ×{g.Sum(b => b.GetInfoItemCount())}", GetItemColor(g.Key), false))
                        .ToList();
                    return new SelectionDetail("Input station " + station.GetInfoID(), p, $"Bundles ({bundles.Count})", bundles, null, null);
                case IOutputStationInfo station:
                    AddPosition(station);
                    Add("Active", station.GetInfoActive() ? "yes" : "no");
                    Add("Blocked", station.GetInfoBlocked() ? $"yes ({F(station.GetInfoBlockedLeft())} s left)" : "no");
                    Add("Capacity", $"{F(station.GetInfoCapacityUsed())} / {F(station.GetInfoCapacity())}");
                    Add("Assigned orders", station.GetInfoAssignedOrders());
                    Add("Open requests / items", $"{station.GetInfoOpenRequests()} / {station.GetInfoOpenItems()}");
                    Add("Queued requests / items", $"{station.GetInfoOpenQueuedRequests()} / {station.GetInfoOpenQueuedItems()}");
                    Add("Inbound pods", station.GetInfoInboundPods());
                    Add("Activation order", station.GetInfoActivationOrderID());
                    Add("Queue", station.GetInfoQueue());
                    var stationCompleted = station.GetInfoCompletedOrders().ToList();
                    return new SelectionDetail("Output station " + station.GetInfoID(), p, null, null,
                        station.GetInfoOpenOrders().Select(o => CreateOrderView(o, null)).ToList(),
                        Enumerable.Range(0, Math.Min(MaxOrdersPerList, stationCompleted.Count)).Select(i => CreateOrderView(stationCompleted[stationCompleted.Count - 1 - i], null)).ToList());
                case IWaypointInfo wp:
                    AddPosition(wp);
                    Add("Storage location", wp.GetInfoStorageLocation() ? "yes" : "no");
                    Add("Connections", string.Join(", ", wp.GetInfoConnectedWaypoints().Select(o => o.GetInfoID())));
                    return new SelectionDetail("Waypoint " + wp.GetInfoID(), p, null, null, null, null);
                default:
                    return new SelectionDetail(obj.ToString(), p, null, null, null, null);
            }
        }
    }
}
