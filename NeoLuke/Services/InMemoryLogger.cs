using Microsoft.Extensions.Logging;
using System;
using NeoLuke.Models.Logging;

namespace NeoLuke.Services;

/// <summary>
/// A custom logger implementation that captures log entries in memory.
/// </summary>
internal class InMemoryLogger(string categoryName, InMemoryLoggerProvider provider) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = logLevel,
            Category = categoryName,
            Message = message,
            Exception = exception
        };

        provider.AddLogEntry(logEntry);
    }
}
