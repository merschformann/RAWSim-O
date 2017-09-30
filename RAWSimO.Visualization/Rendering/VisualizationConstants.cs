using RAWSimO.Core.Items;
using RAWSimO.Core.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace RAWSimO.Visualization.Rendering
{
    public class VisualizationConstants
    {
        public static readonly Brush BrushBotVisual = new SolidColorBrush(Color.FromRgb(86, 175, 54));
        public static readonly Brush BrushWaypointVisual = Brushes.Black;
        public static readonly Brush BrushTierVisual = Brushes.LightGray;
        public static readonly Brush BrushPodVisual = Brushes.CornflowerBlue;
        public static readonly Brush BrushInputStationVisual = Brushes.Yellow;
        public static readonly Brush BrushOutputStationVisual = Brushes.IndianRed;
        public static readonly Brush BrushElevatorEntranceVisual = Brushes.MediumAquamarine;
        public static readonly Brush BrushOutline = Brushes.Black;
        public static readonly Brush BrushGoalMarker = Brushes.Tomato;
        public static readonly Brush BrushDestinationMarker = Brushes.OrangeRed;
        public static readonly Brush BrushSemaphoreBlocked = Brushes.Red;
        public static readonly Brush BrushSemaphoreEntry = Brushes.LimeGreen;
        public static readonly Brush BrushSemaphoreGuard = Brushes.CornflowerBlue;

        public static readonly FontFamily ItemFont = new FontFamily("Consolas");

        public const int SIMPLE_ITEM_BUNDLE_MIN_CHAR_COUNT = 9;
        public const int SIMPLE_ITEM_ORDER_MIN_CHAR_COUNT = 9;

        public const double GOAL_MARKER_STROKE_THICKNESS_FACTOR = 5;
        public const double DESTINATION_MARKER_STROKE_THICKNESS_FACTOR = 10;
        public const double PATH_MARKER_STROKE_THICKNESS_FACTOR = 10;

        public const int HEAT_BRUSHES = 20;

        public static readonly Brush StateBrushHidden = Brushes.Gray;
        public static readonly Dictionary<string, Brush> StateBrushes = new Dictionary<string, Brush>() {
            { "", new SolidColorBrush(Colors.DarkGray) },
            { "Evade", new SolidColorBrush(Colors.HotPink) },
            { "Move", BrushBotVisual },
            { "PutItems", new SolidColorBrush(Colors.DarkViolet) },
            { "GetItems", new SolidColorBrush(Colors.HotPink) },
            { "PickupPod", new SolidColorBrush(Colors.DarkRed) },
            { "SetdownPod", new SolidColorBrush(Colors.Yellow) },
            { "Rest", new SolidColorBrush(Colors.DarkBlue) },
            { "UseElevator", new SolidColorBrush(Colors.Teal) },
            { "Debug", new SolidColorBrush(Colors.Red) }
        };

        public static readonly Dictionary<LetterColors, Brush> LetterColorBackgroundBrushes = new Dictionary<LetterColors, Brush>() {
            { LetterColors.Blue, new SolidColorBrush(Colors.CornflowerBlue) },
            { LetterColors.Green, new SolidColorBrush(Colors.LimeGreen) },
            { LetterColors.Red, new SolidColorBrush(Colors.IndianRed) },
            { LetterColors.Yellow, new SolidColorBrush(Colors.Yellow) },
        };

        public static readonly SolidColorBrush LetterColorIncomplete = Brushes.Black;
        public static readonly SolidColorBrush LetterColorComplete = Brushes.White;

        public static readonly SolidColorBrush SimpleItemColorIncomplete = Brushes.Black;
        public static readonly SolidColorBrush SimpleItemColorComplete = Brushes.White;
    }
}
