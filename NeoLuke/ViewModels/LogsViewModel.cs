using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeoLuke.Models.Logging;
using NeoLuke.Services;
using Microsoft.Extensions.Logging;
using Avalonia.Input.Platform;

namespace NeoLuke.ViewModels;

public partial class LogsViewModel : ViewModelBase
{
    private readonly LoggingService _loggingService;
    private IClipboard? _clipboard;

    [ObservableProperty]
    private ObservableCollection<LogEntry> _allLogs = [];

    [ObservableProperty]
    private ObservableCollection<LogEntry> _filteredLogs = [];

    [ObservableProperty]
    private LogEntry? _selectedLogEntry;

    [ObservableProperty]
    private LogLevel _minimumLogLevel = LogLevel.Information;

    [ObservableProperty]
    private bool _autoScroll = true;

    public ObservableCollection<LogLevel> AvailableLogLevels { get; } =
    [
        LogLevel.Trace,
        LogLevel.Debug,
        LogLevel.Information,
        LogLevel.Warning,
        LogLevel.Error,
        LogLevel.Critical
    ];

    public LogsViewModel(LoggingService loggingService)
    {
        _loggingService = loggingService;
        _loggingService.LogEntryAdded += OnLogEntryAdded;

        // Load existing logs
        LoadExistingLogs();
    }

    partial void OnMinimumLogLevelChanged(LogLevel value) => ApplyFilter();

    public void SetClipboard(IClipboard? clipboard)
    {
        _clipboard = clipboard;
    }

    private void LoadExistingLogs()
    {
        var existingLogs = _loggingService.GetLogEntries();
        foreach (var log in existingLogs)
        {
            AllLogs.Add(log);
        }
        ApplyFilter();
    }

    private void OnLogEntryAdded(object? sender, LogEntry logEntry)
    {
        // Update on UI thread
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            AllLogs.Add(logEntry);
            if (ShouldShowLogLevel(logEntry.LogLevel))
            {
                FilteredLogs.Add(logEntry);
            }
        });
    }

    private void ApplyFilter()
    {
        FilteredLogs.Clear();
        foreach (var log in AllLogs)
        {
            if (ShouldShowLogLevel(log.LogLevel))
            {
                FilteredLogs.Add(log);
            }
        }
    }

    private bool ShouldShowLogLevel(LogLevel logLevel)
    {
        // Show logs at or above the minimum level
        return logLevel >= MinimumLogLevel;
    }

    [RelayCommand]
    private async Task CopyLogs()
    {
        if (_clipboard == null)
            return;

        var sb = new StringBuilder();
        sb.AppendLine($"=== NeoLuke Logs (Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}) ===");
        sb.AppendLine($"Total Logs: {FilteredLogs.Count}");
        sb.AppendLine();

        foreach (var log in FilteredLogs)
        {
            sb.AppendLine($"[{log.FormattedTimestamp}] [{log.LogLevelShort}] {log.Category}");
            sb.AppendLine($"  {log.Message}");
            if (log.Exception != null)
            {
                sb.AppendLine($"  Exception: {log.Exception}");
            }
            sb.AppendLine();
        }

        await _clipboard.SetTextAsync(sb.ToString());
    }

    [RelayCommand]
    private void ClearLogs()
    {
        _loggingService.ClearLogEntries();
        AllLogs.Clear();
        FilteredLogs.Clear();
    }
}
