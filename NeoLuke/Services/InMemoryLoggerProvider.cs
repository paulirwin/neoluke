using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NeoLuke.Models.Logging;

namespace NeoLuke.Services;

/// <summary>
/// A custom logger provider that captures log entries in memory.
/// </summary>
public class InMemoryLoggerProvider : ILoggerProvider
{
    private const int MaxLogEntries = 10000;
    private readonly ConcurrentQueue<LogEntry> _logEntries = new();
    private readonly object _lock = new();

    /// <summary>
    /// Event raised when a new log entry is added.
    /// </summary>
    public event EventHandler<LogEntry>? LogEntryAdded;

    /// <summary>
    /// Gets all log entries currently in memory.
    /// </summary>
    public IReadOnlyList<LogEntry> GetLogEntries()
    {
        lock (_lock)
        {
            return _logEntries.ToArray();
        }
    }

    /// <summary>
    /// Clears all log entries from memory.
    /// </summary>
    public void ClearLogEntries()
    {
        lock (_lock)
        {
            _logEntries.Clear();
        }
    }

    /// <summary>
    /// Adds a log entry to the collection.
    /// </summary>
    internal void AddLogEntry(LogEntry logEntry)
    {
        lock (_lock)
        {
            _logEntries.Enqueue(logEntry);

            // Maintain maximum size by removing oldest entries
            while (_logEntries.Count > MaxLogEntries)
            {
                _logEntries.TryDequeue(out _);
            }
        }

        // Raise event on UI thread-safe context
        LogEntryAdded?.Invoke(this, logEntry);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new InMemoryLogger(categoryName, this);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
