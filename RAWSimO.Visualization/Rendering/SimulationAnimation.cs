using RAWSimO.Core.Info;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace RAWSimO.Visualization.Rendering
{
    public abstract class SimulationAnimation
    {
        public SimulationAnimation(IInstanceInfo instance, Dispatcher uiDispatcher, SimulationAnimationConfig config, Func<BotColorMode> botColorModeGetter, Func<bool> heatModeEnabled)
        {
            _config = config;
            _dispatcher = uiDispatcher;
            _heatModeEnabled = heatModeEnabled;
            _botColorModeGetter = botColorModeGetter;
            _instance = instance;
            // Init colors for current bots
            List<IBotInfo> bots = instance.GetInfoBots().ToList();
            for (int i = 0; i < bots.Count; i++)
                _rainbowBotColors[bots[i]] = new SolidColorBrush(HeatVisualizer.GenerateBiChromaticHeatColor(Colors.Purple, Colors.Red, ((double)i / (bots.Count - 1))));
        }

        protected IInstanceInfo _instance;
        protected Dispatcher _dispatcher;
        protected SimulationAnimationConfig _config;
        protected Func<BotColorMode> _botColorModeGetter;
        protected Func<bool> _heatModeEnabled;

        Dictionary<IBotInfo, Brush> _rainbowBotColors = new Dictionary<IBotInfo, Brush>();
        public Brush GetBotColor(IBotInfo bot, string state)
        {
            // Check whether we already generated a color for the bot
            if (!_rainbowBotColors.ContainsKey(bot))
            {
                Random randomizer = new Random();
                _rainbowBotColors[bot] = new SolidColorBrush(HeatVisualizer.GenerateBiChromaticHeatColor(Colors.Purple, Colors.Red, randomizer.NextDouble()));
            }
            // Return color
            BotColorMode colorMode = _botColorModeGetter();
            switch (colorMode)
            {
                case BotColorMode.DefaultBotDefaultState: return VisualizationConstants.StateBrushes[state];
                case BotColorMode.RainbowBotSingleState: return state == "Move" ? _rainbowBotColors[bot] : VisualizationConstants.StateBrushHidden;
                case BotColorMode.RainbowBotDefaultState: return state == "Move" ? _rainbowBotColors[bot] : VisualizationConstants.StateBrushes[state];
                default: throw new ArgumentException("Unknown bot coloring mode: " + colorMode.ToString());
            }
        }

        public abstract void Init();
        public abstract void Update(bool overrideUpdate);
        public abstract void StopAnimation();
        public abstract void TakeSnapshot(string snapshotDir, string snapshotFilename = null);
    }
}
