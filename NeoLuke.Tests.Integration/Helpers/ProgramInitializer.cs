using Microsoft.Extensions.Logging;
using NeoLuke;
using NeoLuke.Services;
using System.Reflection;

namespace NeoLuke.Tests.Integration.Helpers;

/// <summary>
/// Helper class to initialize Program's static services for testing
/// </summary>
public static class ProgramInitializer
{
    private static bool _isInitialized = false;

    /// <summary>
    /// Initializes Program's static services (LoggingService, IndexService, MessageBus)
    /// </summary>
    public static void InitializeServices()
    {
        if (_isInitialized)
        {
            return;
        }

        // Initialize logging
        var inMemoryLoggerProvider = new InMemoryLoggerProvider();
        var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Trace)
                .AddConsole()
                .AddProvider(inMemoryLoggerProvider);
        });

        // Create logging service
        var loggingService = new LoggingService(inMemoryLoggerProvider);

        // Set Program's static properties using reflection
        SetStaticProperty(typeof(Program), "LoggerFactory", loggerFactory);
        SetStaticProperty(typeof(Program), "LoggingService", loggingService);

        // Initialize MessageBus
        var messageBus = new MessageBus();
        SetStaticProperty(typeof(Program), "MessageBus", messageBus);

        // Initialize IndexService
        var indexServiceLogger = loggerFactory.CreateLogger<IndexService>();
        var indexService = new IndexService(indexServiceLogger);
        SetStaticProperty(typeof(Program), "IndexService", indexService);

        _isInitialized = true;
    }

    private static void SetStaticProperty(Type type, string propertyName, object value)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
        if (property != null && property.CanWrite)
        {
            property.SetValue(null, value);
        }
        else
        {
            // Property might be read-only with a backing field - try to set the backing field
            var backingField = type.GetField($"<{propertyName}>k__BackingField",
                BindingFlags.NonPublic | BindingFlags.Static);
            backingField?.SetValue(null, value);
        }
    }
}
