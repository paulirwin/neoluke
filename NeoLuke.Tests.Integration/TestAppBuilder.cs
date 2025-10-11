using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(NeoLuke.Tests.Integration.TestAppBuilder))]

namespace NeoLuke.Tests.Integration;

/// <summary>
/// Configures the Avalonia test application for headless xUnit tests
/// </summary>
public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
}
