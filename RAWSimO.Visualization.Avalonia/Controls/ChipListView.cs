using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using RAWSimO.Visualization.Avalonia.Simulation;
using SkiaSharp;

namespace RAWSimO.Visualization.Avalonia.Controls
{
    /// <summary>
    /// Displays rows of colored item chips (orders, pod contents, ...). The chips are drawn directly instead of being individual controls,
    /// hence, long lists stay cheap to update.
    /// </summary>
    public sealed class ChipListView : Control
    {
        private const double ChipPaddingX = 4, ChipHeight = 18, ChipSpacing = 3, RowSpacing = 5, TagWidth = 34;
        private static readonly Typeface Typeface = new Typeface(FontFamily.Default);
        private const double FontSize = 11;

        private readonly Dictionary<string, FormattedText> _textCache = new Dictionary<string, FormattedText>();
        private readonly Dictionary<(SKColor, bool), IBrush> _brushCache = new Dictionary<(SKColor, bool), IBrush>();
        private IReadOnlyList<OrderView> _rows = Array.Empty<OrderView>();
        private readonly List<(Rect rect, string text, IBrush background, IBrush foreground)> _layout = new();
        private string _emptyText = "(none)";

        /// <summary>
        /// The rows to display (each row starts on a new line, its chips wrap).
        /// </summary>
        public IReadOnlyList<OrderView> Rows
        {
            get => _rows;
            set
            {
                value ??= Array.Empty<OrderView>();
                if (RowsEqual(_rows, value)) return;
                _rows = value;
                InvalidateMeasure();
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Text shown when there are no rows.
        /// </summary>
        public string EmptyText { get => _emptyText; set { _emptyText = value; InvalidateVisual(); } }

        /// <summary>
        /// Sets a single row of chips.
        /// </summary>
        public void SetChips(IReadOnlyList<ItemChip> chips) =>
            Rows = chips == null || chips.Count == 0 ? Array.Empty<OrderView>() : new[] { new OrderView(null, chips) };

        private static bool RowsEqual(IReadOnlyList<OrderView> a, IReadOnlyList<OrderView> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].Tag != b[i].Tag || a[i].Positions.Count != b[i].Positions.Count) return false;
                for (int j = 0; j < a[i].Positions.Count; j++)
                    if (a[i].Positions[j] != b[i].Positions[j]) return false;
            }
            return true;
        }

        private FormattedText GetText(string text, IBrush foreground)
        {
            string key = text + (ReferenceEquals(foreground, Brushes.White) ? "\u0001" : "\u0000");
            if (!_textCache.TryGetValue(key, out FormattedText formatted))
            {
                if (_textCache.Count > 10000)
                    _textCache.Clear();
                formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface, FontSize, foreground);
                _textCache[key] = formatted;
            }
            return formatted;
        }

        private IBrush GetBrush(SKColor color, bool done)
        {
            if (!_brushCache.TryGetValue((color, done), out IBrush brush))
            {
                // Completed positions are darkened (and get white text)
                double f = done ? 0.55 : 1;
                brush = new SolidColorBrush(Color.FromArgb(255, (byte)(color.Red * f), (byte)(color.Green * f), (byte)(color.Blue * f))).ToImmutable();
                _brushCache[(color, done)] = brush;
            }
            return brush;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            double width = double.IsInfinity(availableSize.Width) ? 400 : availableSize.Width;
            _layout.Clear();
            if (_rows.Count == 0)
                return new Size(0, ChipHeight);
            bool hasTags = false;
            foreach (var row in _rows)
                hasTags |= row.Tag != null;
            double y = 0, maxX = 0;
            foreach (var row in _rows)
            {
                double x = 0;
                if (hasTags)
                {
                    if (row.Tag != null)
                        _layout.Add((new Rect(0, y, TagWidth, ChipHeight), row.Tag, null, Brushes.Gray));
                    x = TagWidth;
                }
                double rowStart = x;
                foreach (var chip in row.Positions)
                {
                    IBrush foreground = chip.Done ? Brushes.White : Brushes.Black;
                    double chipWidth = GetText(chip.Label, foreground).Width + 2 * ChipPaddingX;
                    if (x > rowStart && x + chipWidth > width)
                    {
                        x = rowStart;
                        y += ChipHeight + ChipSpacing;
                    }
                    _layout.Add((new Rect(x, y, chipWidth, ChipHeight), chip.Label, GetBrush(chip.Color, chip.Done), foreground));
                    x += chipWidth + ChipSpacing;
                    maxX = Math.Max(maxX, x);
                }
                y += ChipHeight + RowSpacing;
            }
            return new Size(Math.Min(maxX, width), y - RowSpacing);
        }

        public override void Render(DrawingContext context)
        {
            if (_rows.Count == 0)
            {
                context.DrawText(GetText(_emptyText, Brushes.Gray), new Point(0, 2));
                return;
            }
            Rect visible = new Rect(Bounds.Size);
            foreach (var (rect, text, background, foreground) in _layout)
            {
                if (!rect.Intersects(visible)) continue;
                if (background != null)
                    context.DrawRectangle(background, null, rect, 3, 3);
                FormattedText formatted = GetText(text, foreground);
                double offsetX = background != null ? ChipPaddingX : 0;
                context.DrawText(formatted, new Point(rect.X + offsetX, rect.Y + (ChipHeight - formatted.Height) / 2));
            }
        }
    }
}
