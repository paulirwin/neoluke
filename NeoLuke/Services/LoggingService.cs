using System;
using System.Collections.Generic;
using NeoLuke.Models.Logging;

namespace NeoLuke.Services;

/// <summary>
/// Service that provides access to application logs for ViewModels.
/// </summary>
public class LoggingService(InMemoryLoggerProvider loggerProvider)
{
    /// <summary>
    /// Event raised when a new log entry is added.
    /// </summary>
    public event EventHandler<LogEntry>? LogEntryAdded
    {
        add => loggerProvider.LogEntryAdded += value;
        remove => loggerProvider.LogEntryAdded -= value;
    }

    /// <summary>
    /// Gets all log entries currently in memory.
    /// </summary>
    public IReadOnlyList<LogEntry> GetLogEntries()
    {
        return loggerProvider.GetLogEntries();
    }

    /// <summary>
    /// Clears all log entries from memory.
    /// </summary>
    public void ClearLogEntries()
    {
        loggerProvider.ClearLogEntries();
    }
}
