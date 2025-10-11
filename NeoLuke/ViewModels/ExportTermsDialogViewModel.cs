using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeoLuke.Models.Tools;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NeoLuke.ViewModels;

/// <summary>
/// ViewModel for the Export Terms dialog
/// </summary>
public partial class ExportTermsDialogViewModel : ViewModelBase
{
    private readonly IndexToolsModel _model;
    private readonly Window? _parentWindow;

    [ObservableProperty]
    private string _indexPath;

    [ObservableProperty]
    private ObservableCollection<string> _fieldNames = [];

    [ObservableProperty]
    private string? _selectedField;

    [ObservableProperty]
    private string _outputDirectory;

    [ObservableProperty]
    private ObservableCollection<DelimiterOption> _delimiterOptions = [];

    [ObservableProperty]
    private DelimiterOption? _selectedDelimiter;

    [ObservableProperty]
    private string _statusText = "Idle";

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private bool _showSuccessMessage;

    [ObservableProperty]
    private string _successMessage = string.Empty;

    [ObservableProperty]
    private bool _showErrorMessage;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public bool CanExport => !IsExporting && !string.IsNullOrEmpty(SelectedField) && !string.IsNullOrEmpty(OutputDirectory);

    public ExportTermsDialogViewModel()
    {
        // Design-time constructor
        _model = null!;
        _indexPath = "/path/to/index";
        _outputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        InitializeDelimiterOptions();

        FieldNames = ["field1", "field2", "field3"];
        SelectedField = FieldNames.FirstOrDefault();
    }

    public ExportTermsDialogViewModel(IndexToolsModel model, string indexPath, Window parentWindow)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _indexPath = indexPath ?? throw new ArgumentNullException(nameof(indexPath));
        _parentWindow = parentWindow;
        _outputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        InitializeDelimiterOptions();
        LoadFieldNames();
    }

    private void InitializeDelimiterOptions()
    {
        DelimiterOptions =
        [
            new DelimiterOption("Comma", ","),
            new DelimiterOption("Tab", "\t"),
            new DelimiterOption("Whitespace", " ")
        ];
        SelectedDelimiter = DelimiterOptions.First(); // Default to Comma
    }

    private void LoadFieldNames()
    {
        try
        {
            var fields = _model.GetFieldNames();
            FieldNames = new ObservableCollection<string>(fields);
            SelectedField = FieldNames.FirstOrDefault();
        }
        catch (Exception ex)
        {
            StatusText = "Error loading fields";
            ShowErrorMessage = true;
            ErrorMessage = $"Failed to load field names: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        if (_parentWindow == null)
            return;

        try
        {
            var storageProvider = _parentWindow.StorageProvider;
            var folder = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Output Directory",
                AllowMultiple = false
            });

            if (folder.Count > 0)
            {
                OutputDirectory = folder[0].Path.LocalPath;
            }
        }
        catch (Exception ex)
        {
            StatusText = "Error";
            ShowErrorMessage = true;
            ErrorMessage = $"Failed to open folder picker: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (IsExporting || string.IsNullOrEmpty(SelectedField) || string.IsNullOrEmpty(OutputDirectory))
            return;

        // Reset messages
        ShowSuccessMessage = false;
        ShowErrorMessage = false;

        IsExporting = true;
        StatusText = "Exporting...";

        try
        {
            var delimiter = SelectedDelimiter?.Separator ?? ",";

            // Run export in background
            var outputFile = await Task.Run(() => _model.ExportTerms(OutputDirectory, SelectedField, delimiter));

            // Show success
            StatusText = "Done";
            ShowSuccessMessage = true;
            SuccessMessage = $"Terms exported successfully to:\n{outputFile}\n\nFormat: [term]{delimiter}[doc frequency]";
        }
        catch (Exception ex)
        {
            StatusText = "Error";
            ShowErrorMessage = true;
            ErrorMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    partial void OnSelectedFieldChanged(string? value)
    {
        OnPropertyChanged(nameof(CanExport));
    }

    partial void OnOutputDirectoryChanged(string value)
    {
        OnPropertyChanged(nameof(CanExport));
    }

    partial void OnIsExportingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanExport));
    }
}

/// <summary>
/// Represents a delimiter option for the export
/// </summary>
public class DelimiterOption(string description, string separator)
{
    public string Description { get; } = description;
    public string Separator { get; } = separator;
}
