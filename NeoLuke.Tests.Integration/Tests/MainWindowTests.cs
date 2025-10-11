using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using NeoLuke.Tests.Integration.Helpers;
using NeoLuke.Views;

namespace NeoLuke.Tests.Integration.Tests;

/// <summary>
/// Integration tests for MainWindow initialization and basic functionality
/// </summary>
public class MainWindowTests : HeadlessTestBase
{
    public MainWindowTests()
    {
        // Initialize Program's static services before creating MainWindow
        ProgramInitializer.InitializeServices();
    }

    [AvaloniaFact]
    public void MainWindow_Constructor_CreatesWindowSuccessfully()
    {
        // Act - Create MainWindow (note: this will trigger IndexPathDialog in real app)
        // For testing, we just verify the window can be constructed
        var window = new MainWindow();

        // Assert
        Assert.NotNull(window);
        Assert.IsAssignableFrom<Window>(window);
    }

    [AvaloniaFact]
    public void MainWindow_Title_ContainsApplicationName()
    {
        // Arrange & Act
        var window = new MainWindow();

        // Assert
        Assert.NotNull(window.Title);
        Assert.Contains("NeoLuke", window.Title);
        Assert.Contains("Lucene.NET Toolbox", window.Title);
    }

    [AvaloniaFact]
    public void MainWindow_Title_ContainsVersion()
    {
        // Arrange & Act
        var window = new MainWindow();

        // Assert
        Assert.NotNull(window.Title);
        // Version should be in format "v0.1.0" or similar
        Assert.Matches(@"v\d+\.\d+\.\d+", window.Title);
    }

    [AvaloniaFact]
    public void MainWindow_HasMenuBar()
    {
        // Arrange & Act
        var window = new MainWindow();

        // Assert
        var nativeMenu = NativeMenu.GetMenu(window);
        Assert.NotNull(nativeMenu);
        Assert.NotEmpty(nativeMenu.Items);
    }

    [AvaloniaFact]
    public void MainWindow_CanBeConstructed_WithoutShowing()
    {
        // Arrange & Act
        var window = new MainWindow();

        // Assert - Window is created but not shown (to avoid IndexPathDialog)
        Assert.NotNull(window);
        Assert.False(window.IsVisible);
    }

    [AvaloniaFact]
    public void MainWindow_Close_WithoutShowing_DoesNotThrow()
    {
        // Arrange
        var window = new MainWindow();

        // Act & Assert - Closing without showing should not throw
        window.Close();
        Assert.False(window.IsVisible);
    }
}
