using Avalonia;
using Avalonia.Markup.Xaml;

namespace NeoLuke.Tests.Integration;

/// <summary>
/// Test application for headless integration tests
/// </summary>
public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
