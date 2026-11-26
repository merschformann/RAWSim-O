using Avalonia;
using Avalonia.Headless;
using RAWSimO.Visualization.Avalonia.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace RAWSimO.Visualization.Avalonia.Tests
{
    /// <summary>
    /// Runs the app headless, but with real Skia rendering (allows capturing and checking rendered frames).
    /// </summary>
    public class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                .UseSkia()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }
}
