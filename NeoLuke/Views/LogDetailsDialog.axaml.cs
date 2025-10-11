using Avalonia.Controls;
using Avalonia.Interactivity;
using NeoLuke.Models.Logging;
using System;
using System.Text;

namespace NeoLuke.Views;

public partial class LogDetailsDialog : Window
{
    private readonly LogEntry _logEntry;

    // Parameterless constructor for XAML designer
    public LogDetailsDialog() : this(new LogEntry
    {
        Timestamp = DateTime.Now,
        LogLevel = Microsoft.Extensions.Logging.LogLevel.Information,
        Category = "Design",
        Message = "Design-time log entry"
    })
    {
    }

    public LogDetailsDialog(LogEntry logEntry)
    {
        _logEntry = logEntry;
        InitializeComponent();
        PopulateDetails();
    }

    private void PopulateDetails()
    {
        TimestampText.Text = _logEntry.FormattedTimestamp;
        LogLevelText.Text = _logEntry.LogLevel.ToString();
        CategoryText.Text = _logEntry.Category;
        MessageText.Text = _logEntry.Message;

        if (_logEntry.Exception != null)
        {
            ExceptionBorder.IsVisible = true;
            ExceptionText.Text = _logEntry.Exception.ToString();
        }
    }

    private string GetDetailsAsText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Log Entry Details ===");
        sb.AppendLine();
        sb.AppendLine($"Timestamp: {_logEntry.FormattedTimestamp}");
        sb.AppendLine($"Log Level: {_logEntry.LogLevel}");
        sb.AppendLine($"Category: {_logEntry.Category}");
        sb.AppendLine();
        sb.AppendLine("Message:");
        sb.AppendLine(_logEntry.Message);

        if (_logEntry.Exception != null)
        {
            sb.AppendLine();
            sb.AppendLine("Exception:");
            sb.AppendLine(_logEntry.Exception.ToString());
        }

        return sb.ToString();
    }

    private async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        var clipboard = Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(GetDetailsAsText());
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
