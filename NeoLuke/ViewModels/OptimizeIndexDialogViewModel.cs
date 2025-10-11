using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeoLuke.Models.Tools;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NeoLuke.ViewModels;

/// <summary>
/// ViewModel for the Optimize Index dialog
/// </summary>
public partial class OptimizeIndexDialogViewModel : ViewModelBase
{
    private readonly IndexToolsModel _model;
    private readonly Action? _onOptimizeCompleted;

    [ObservableProperty]
    private string _indexPath;

    [ObservableProperty]
    private bool _expungeDeletes;

    [ObservableProperty]
    private int _maxSegments = 1;

    [ObservableProperty]
    private string _logText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Idle";

    [ObservableProperty]
    private bool _isOptimizing;

    [ObservableProperty]
    private bool _isReadOnly;

    public OptimizeIndexDialogViewModel()
    {
        // Design-time constructor
        _model = null!;
        _indexPath = "/path/to/index";
    }

    public OptimizeIndexDialogViewModel(IndexToolsModel model, string indexPath, Action? onOptimizeCompleted = null)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _indexPath = indexPath ?? throw new ArgumentNullException(nameof(indexPath));
        _onOptimizeCompleted = onOptimizeCompleted;
        IsReadOnly = model.IsReadOnly;
    }

    [RelayCommand]
    private async Task OptimizeAsync()
    {
        if (IsOptimizing)
            return;

        if (IsReadOnly)
        {
            StatusText = "Error: Index is opened in read-only mode";
            LogText += "\nError: Cannot optimize index when opened in read-only mode.\n";
            return;
        }

        IsOptimizing = true;
        StatusText = "Running...";
        LogText = string.Empty;

        try
        {
            // Create a StringWriter to capture log output
            var logBuilder = new StringBuilder();
            var logWriter = new StringWriter(logBuilder);

            // Run optimization in background
            await Task.Run(() =>
            {
                logWriter.WriteLine("=== Optimize Index Started ===");
                logWriter.WriteLine($"Index Path: {IndexPath}");
                logWriter.WriteLine($"Expunge Deletes Only: {ExpungeDeletes}");
                if (!ExpungeDeletes)
                {
                    logWriter.WriteLine($"Max Segments: {MaxSegments}");
                }
                logWriter.WriteLine();

                _model.Optimize(ExpungeDeletes, MaxSegments, logWriter);

                logWriter.WriteLine();
                logWriter.WriteLine("=== Optimize Index Completed Successfully ===");
                logWriter.Flush();
            });

            // Update log text
            LogText = logBuilder.ToString();
            StatusText = "Done";

            // Notify that optimization completed
            _onOptimizeCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            StatusText = "Error";
            LogText += $"\n\nError during optimization:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
        }
        finally
        {
            IsOptimizing = false;
        }
    }
}
