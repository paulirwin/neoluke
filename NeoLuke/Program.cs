using Avalonia;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NeoLuke.Services;

namespace NeoLuke;

public sealed class Program
{
    // Logging services accessible throughout the application
    private static InMemoryLoggerProvider InMemoryLoggerProvider { get; set; } = null!;
    public static LoggingService LoggingService { get; private set; } = null!;
    public static ILoggerFactory LoggerFactory { get; private set; } = NullLoggerFactory.Instance;

    // Services accessible throughout the application
    public static IndexService IndexService { get; private set; } = null!;
    public static MessageBus MessageBus { get; private set; } = null!;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Initialize logging before starting the app
        InitializeLogging();

        // Initialize services
        InitializeServices();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static void InitializeLogging()
    {
        // Create in-memory logger provider
        InMemoryLoggerProvider = new InMemoryLoggerProvider();

        // Create logger factory with console and in-memory providers
        LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Trace)
                .AddConsole()
                .AddProvider(InMemoryLoggerProvider);
        });

        // Create logging service
        LoggingService = new LoggingService(InMemoryLoggerProvider);

        // Log startup message
        var logger = LoggerFactory.CreateLogger<Program>();
        logger.LogInformation("NeoLuke starting up...");
    }

    private static void InitializeServices()
    {
        // Create message bus
        MessageBus = new MessageBus();

        // Create index service
        var indexServiceLogger = LoggerFactory.CreateLogger<IndexService>();
        IndexService = new IndexService(indexServiceLogger);

        var logger = LoggerFactory.CreateLogger<Program>();
        logger.LogInformation("Services initialized");
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
