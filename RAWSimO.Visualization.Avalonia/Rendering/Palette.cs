using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace RAWSimO.Visualization.Avalonia.Rendering
{
    /// <summary>
    /// Colors used for rendering (ported from the WPF visualization to keep the familiar look).
    /// </summary>
    public static class Palette
    {
        public static readonly SKColor Background = new SKColor(211, 211, 211); // LightGray
        public static readonly SKColor Tier = SKColors.White;
        public static readonly SKColor Outline = SKColors.Black;
        public static readonly SKColor Waypoint = SKColors.Black;
        public static readonly SKColor Bot = new SKColor(86, 175, 54);
        public static readonly SKColor Pod = new SKColor(100, 149, 237); // CornflowerBlue
        public static readonly SKColor InputStation = new SKColor(255, 255, 0); // Yellow
        public static readonly SKColor OutputStation = new SKColor(205, 92, 92); // IndianRed
        public static readonly SKColor ElevatorEntrance = new SKColor(102, 205, 170); // MediumAquamarine
        public static readonly SKColor GoalMarker = new SKColor(255, 99, 71); // Tomato
        public static readonly SKColor DestinationMarker = new SKColor(255, 69, 0); // OrangeRed
        public static readonly SKColor SemaphoreBlocked = new SKColor(255, 0, 0);
        public static readonly SKColor SemaphoreEntry = new SKColor(50, 205, 50); // LimeGreen
        public static readonly SKColor SemaphoreGuard = new SKColor(100, 149, 237); // CornflowerBlue
        public static readonly SKColor Selection = new SKColor(0, 120, 215);

        /// <summary>
        /// Color used for all non-move states in <see cref="BotColorMode.RainbowBotSingleState"/>.
        /// </summary>
        public static readonly SKColor StateHidden = new SKColor(128, 128, 128);
        /// <summary>
        /// Fallback for bot states without a dedicated color.
        /// </summary>
        public static readonly SKColor StateUnknown = new SKColor(169, 169, 169); // DarkGray

        public static readonly Dictionary<string, SKColor> StateColors = new Dictionary<string, SKColor>()
        {
            { "", StateUnknown },
            { "Evade", new SKColor(255, 105, 180) }, // HotPink
            { "Move", Bot },
            { "PutItems", new SKColor(148, 0, 211) }, // DarkViolet
            { "GetItems", new SKColor(255, 105, 180) }, // HotPink
            { "PickupPod", new SKColor(139, 0, 0) }, // DarkRed
            { "SetdownPod", new SKColor(255, 255, 0) }, // Yellow
            { "Rest", new SKColor(0, 0, 139) }, // DarkBlue
            { "UseElevator", new SKColor(0, 128, 128) }, // Teal
            { "Debug", new SKColor(255, 0, 0) },
        };

        /// <summary>
        /// Returns the color for the given bot state.
        /// </summary>
        public static SKColor GetStateColor(string state) =>
            state != null && StateColors.TryGetValue(state, out SKColor color) ? color : StateUnknown;

        /// <summary>
        /// Generates a heat color for a value in [0,1] (blue = cold, red = hot).
        /// </summary>
        public static SKColor GetHeatColor(double value)
        {
            value = double.IsNaN(value) ? 0 : Math.Clamp(value, 0, 1);
            return SKColor.FromHsv((float)((1 - value) * 240), 80, 97);
        }

        /// <summary>
        /// Generates a color between the low (value 0) and high (value 1) color (port of the WPF version).
        /// If one of the colors is fully transparent, only the alpha channel of the other color is varied,
        /// otherwise the colors are interpolated in HSV space.
        /// </summary>
        public static SKColor GetBichromaticColor(SKColor low, SKColor high, double value)
        {
            value = double.IsNaN(value) ? 0 : Math.Clamp(value, 0, 1);
            if (low.Alpha == 0 || high.Alpha == 0)
            {
                if (low.Alpha == 0)
                    return high.WithAlpha(value == 1 ? (byte)255 : (byte)(value * 256));
                return low.WithAlpha(value == 0 ? (byte)255 : (byte)((1 - value) * 256));
            }
            low.ToHsv(out float hLow, out float sLow, out float vLow);
            high.ToHsv(out float hHigh, out float sHigh, out float vHigh);
            static float Lerp(float a, float b, double t) => a <= b ? a + (float)(Math.Abs(b - a) * t) : a - (float)(Math.Abs(b - a) * t);
            return SKColor.FromHsv(Lerp(hLow, hHigh, value), Lerp(sLow, sHigh, value), Lerp(vLow, vHigh, value));
        }

        /// <summary>
        /// Generates a color between purple (value 0) and red (value 1). Used to distinguish bots.
        /// </summary>
        public static SKColor GetRainbowColor(double value)
        {
            value = Math.Clamp(value, 0, 1);
            // Interpolation in HSV space between Purple (300°, 100%, 50.2%) and Red (0°, 100%, 100%)
            return SKColor.FromHsv((float)(300 * (1 - value)), 100, (float)((0.502 + 0.498 * value) * 100));
        }
    }

    /// <summary>
    /// Determines the colors of bots (stable per-bot colors for the rainbow modes).
    /// </summary>
    public sealed class BotColors
    {
        private readonly SKColor[] _rainbowColors;

        public BotColors(int botCount)
        {
            _rainbowColors = Enumerable.Range(0, botCount).Select(i => Palette.GetRainbowColor(botCount > 1 ? (double)i / (botCount - 1) : 0)).ToArray();
        }

        public SKColor Get(in BotSnapshot bot, BotColorMode mode)
        {
            SKColor rainbow = bot.Index < _rainbowColors.Length ? _rainbowColors[bot.Index] : Palette.Bot;
            return mode switch
            {
                BotColorMode.RainbowBotSingleState => bot.State == "Move" ? rainbow : Palette.StateHidden,
                BotColorMode.RainbowBotDefaultState => bot.State == "Move" ? rainbow : Palette.GetStateColor(bot.State),
                _ => Palette.GetStateColor(bot.State),
            };
        }
    }
}
