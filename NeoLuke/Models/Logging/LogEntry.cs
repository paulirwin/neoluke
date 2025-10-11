using Microsoft.Extensions.Logging;
using System;

namespace NeoLuke.Models.Logging;

/// <summary>
/// Represents a single log entry captured by the application.
/// </summary>
public class LogEntry
{
    /// <summary>
    /// The timestamp when the log entry was created.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// The log level (Trace, Debug, Information, Warning, Error, Critical).
    /// </summary>
    public LogLevel LogLevel { get; init; }

    /// <summary>
    /// The category name (typically the logger name or class name).
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// The log message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Optional exception information if the log entry is related to an error.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets a formatted timestamp string.
    /// </summary>
    public string FormattedTimestamp => Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");

    /// <summary>
    /// Gets a short log level name for display.
    /// </summary>
    public string LogLevelShort => LogLevel switch
    {
        LogLevel.Trace => "TRC",
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        LogLevel.Critical => "CRT",
        _ => "???"
    };

    /// <summary>
    /// Gets the full message including exception details if present.
    /// </summary>
    public string FullMessage => Exception != null
        ? $"{Message}\n{Exception}"
        : Message;

    /// <summary>
    /// Gets the first line of the message.
    /// </summary>
    public string FirstLine
    {
        get
        {
            // Find the first newline character (any style)
            var indexLF = Message.IndexOf('\n');
            var indexCR = Message.IndexOf('\r');

            // Determine the position of the first line break
            int breakIndex;
            if (indexLF == -1 && indexCR == -1)
                return Message; // No line breaks
            else if (indexLF == -1)
                breakIndex = indexCR;
            else if (indexCR == -1)
                breakIndex = indexLF;
            else
                breakIndex = Math.Min(indexLF, indexCR);

            return Message.Substring(0, breakIndex);
        }
    }

    /// <summary>
    /// Gets the number of additional lines beyond the first line.
    /// </summary>
    public int AdditionalLineCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < Message.Length; i++)
            {
                char c = Message[i];
                if (c == '\n')
                {
                    count++;
                }
                else if (c == '\r')
                {
                    count++;
                    // Skip \n if this is \r\n
                    if (i + 1 < Message.Length && Message[i + 1] == '\n')
                        i++;
                }
            }
            return count;
        }
    }

    /// <summary>
    /// Gets the indicator text for additional lines (e.g., "[+7 lines]").
    /// Returns empty string if there are no additional lines.
    /// </summary>
    public string AdditionalLinesIndicator
    {
        get
        {
            var count = AdditionalLineCount;
            return count > 0 ? $"[+{count} line{(count == 1 ? "" : "s")}]" : string.Empty;
        }
    }

    /// <summary>
    /// Gets whether the message has multiple lines.
    /// </summary>
    public bool HasMultipleLines => Message.IndexOf('\n') != -1 || Message.IndexOf('\r') != -1;
}
