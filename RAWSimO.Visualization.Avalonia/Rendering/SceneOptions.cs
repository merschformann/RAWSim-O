namespace RAWSimO.Visualization.Avalonia.Rendering
{
    /// <summary>
    /// The level of detail used for rendering the static parts of a tier.
    /// </summary>
    public enum DetailLevel
    {
        /// <summary>
        /// No details are added. The design is as simple as possible.
        /// </summary>
        None,
        /// <summary>
        /// Rendering is done using additional aesthetic components.
        /// </summary>
        Aesthetics,
        /// <summary>
        /// Debug information (waypoint graph, guards) is rendered too.
        /// </summary>
        Debug,
        /// <summary>
        /// All available information (each waypoint and its connections) is rendered.
        /// </summary>
        Full,
    }

    /// <summary>
    /// Determines how bots are colored.
    /// </summary>
    public enum BotColorMode
    {
        /// <summary>
        /// Uses the default color for the bots and state dependent coloring.
        /// </summary>
        DefaultBotDefaultState,
        /// <summary>
        /// Uses rainbow colors to better distinguish the bots as well as one color for all states other than move.
        /// </summary>
        RainbowBotSingleState,
        /// <summary>
        /// Uses rainbow colors to better distinguish the bots and state dependent coloring for all states other than move.
        /// </summary>
        RainbowBotDefaultState,
    }

    /// <summary>
    /// Options that can be changed while rendering without rebuilding the static scene.
    /// </summary>
    public sealed class SceneOptions
    {
        public bool DrawGoal { get; set; }
        public bool DrawDestination { get; set; }
        public bool DrawPath { get; set; }
        public bool HeatMode { get; set; }
        public BotColorMode BotColorMode { get; set; } = BotColorMode.DefaultBotDefaultState;
        /// <summary>
        /// A heatmap to draw on top of (or behind) the scene, if any.
        /// </summary>
        public HeatmapOverlay Heatmap { get; set; }
        /// <summary>
        /// Whether to omit the (white) tier background.
        /// </summary>
        public bool TransparentTier { get; set; }

        /// <summary>
        /// Creates a copy of these options (handed to the render thread for a single frame).
        /// </summary>
        public SceneOptions Clone() => (SceneOptions)MemberwiseClone();
    }
}
