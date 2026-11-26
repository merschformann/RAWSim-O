using System;
using Avalonia;

namespace RAWSimO.Visualization.Avalonia
{
    internal static class Program
    {
        [STAThread]
        public static int Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        /// <summary>
        /// Avalonia configuration (also used by the designer and headless tests).
        /// </summary>
        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
