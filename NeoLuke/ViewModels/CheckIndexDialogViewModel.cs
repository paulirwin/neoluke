using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lucene.Net.Index;
using NeoLuke.Models.Tools;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NeoLuke.ViewModels;

/// <summary>
/// ViewModel for the Check Index dialog
/// </summary>
public partial class CheckIndexDialogViewModel : ViewModelBase
{
    private readonly IndexToolsModel _model;
    private readonly Action? _onRepairCompleted;

    [ObservableProperty]
    private string _indexPath;

    [ObservableProperty]
    private string _logText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Idle";

    [ObservableProperty]
    private string _resultText;

    [ObservableProperty]
    private bool _isChecking;

    [ObservableProperty]
    private bool _isRepairing;

    [ObservableProperty]
    private bool _canRepair;

    [ObservableProperty]
    private bool _isReadOnly;

    private CheckIndex.Status? _checkStatus;

    public CheckIndexDialogViewModel()
    {
        // Design-time constructor
        _model = null!;
        _indexPath = "/path/to/index";
        _resultText = "Not checked yet";
    }

    public CheckIndexDialogViewModel(IndexToolsModel model, string indexPath, Action? onRepairCompleted = null)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _indexPath = indexPath ?? throw new ArgumentNullException(nameof(indexPath));
        _onRepairCompleted = onRepairCompleted;
        _isReadOnly = model.IsReadOnly;
        _resultText = "Click 'Check Index' to begin";
    }

    [RelayCommand]
    private async Task CheckIndexAsync()
    {
        if (IsChecking)
            return;

        IsChecking = true;
        StatusText = "Running...";
        LogText = string.Empty;
        ResultText = "Checking...";
        CanRepair = false;
        _checkStatus = null;

        try
        {
            // Create a StringWriter to capture log output
            var logBuilder = new StringBuilder();
            var logWriter = new StringWriter(logBuilder);

            // Run check in background
            var status = await Task.Run(() =>
            {
                logWriter.WriteLine("=== Check Index Started ===");
                logWriter.WriteLine($"Index Path: {IndexPath}");
                logWriter.WriteLine();

                var result = _model.CheckIndex(logWriter);

                logWriter.WriteLine();
                logWriter.WriteLine("=== Check Index Completed ===");
                logWriter.Flush();

                return result;
            });

            // Update log text
            LogText = logBuilder.ToString();

            // Store status for potential repair
            _checkStatus = status;

            // Format result message
            ResultText = FormatResultMessage(status);

            // Enable repair button if index is not clean and not in read-only mode
            if (!status.Clean && !IsReadOnly)
            {
                CanRepair = true;
            }

            StatusText = "Done";
        }
        catch (Exception ex)
        {
            StatusText = "Error";
            ResultText = "Error during check";
            LogText += $"\n\nError during check:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
        }
        finally
        {
            IsChecking = false;
        }
    }

    [RelayCommand]
    private async Task RepairIndexAsync()
    {
        if (_checkStatus == null || IsRepairing || IsReadOnly)
            return;

        IsRepairing = true;
        StatusText = "Repairing...";
        LogText = string.Empty;
        CanRepair = false;

        try
        {
            // Create a StringWriter to capture log output
            var logBuilder = new StringBuilder();
            var logWriter = new StringWriter(logBuilder);

            // Run repair in background
            await Task.Run(() =>
            {
                logWriter.WriteLine("=== Repair Index Started ===");
                logWriter.WriteLine($"Index Path: {IndexPath}");
                logWriter.WriteLine();

                _model.RepairIndex(_checkStatus, logWriter);

                logWriter.WriteLine();
                logWriter.WriteLine("=== Repair Index Completed ===");
                logWriter.Flush();
            });

            // Update log text
            LogText = logBuilder.ToString();
            ResultText = "Index repaired successfully";
            StatusText = "Done";

            // Reset check status since index has been repaired
            _checkStatus = null;

            // Notify that repair completed (so index can be reopened)
            _onRepairCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            StatusText = "Error";
            ResultText = "Error during repair";
            LogText += $"\n\nError during repair:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
        }
        finally
        {
            IsRepairing = false;
        }
    }

    private static string FormatResultMessage(CheckIndex.Status? status)
    {
        if (status == null)
        {
            return "Unknown";
        }

        if (status.Clean)
        {
            return "OK - Index is healthy";
        }

        if (status.ToolOutOfDate)
        {
            return "ERROR: Can't check - tool out-of-date";
        }

        var sb = new StringBuilder("BAD - Index has issues:");

        if (status.MissingSegments)
        {
            sb.AppendLine();
            sb.Append("  • Missing segments");
        }

        if (status.NumBadSegments > 0)
        {
            sb.AppendLine();
            sb.Append($"  • Bad segments: {status.NumBadSegments}");
        }

        if (status.TotLoseDocCount > 0)
        {
            sb.AppendLine();
            sb.Append($"  • Documents that would be lost: {status.TotLoseDocCount}");
        }

        return sb.ToString();
    }
}
