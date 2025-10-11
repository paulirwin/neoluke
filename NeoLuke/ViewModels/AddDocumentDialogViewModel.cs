using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NeoLuke.Models.Documents;

namespace NeoLuke.ViewModels;

public partial class AddDocumentDialogViewModel : ViewModelBase
{
    private readonly ILogger<AddDocumentDialogViewModel> _logger;
    private Action? _closeDialog;

    [ObservableProperty]
    private ObservableCollection<NewField> _fields = [];

    /// <summary>
    /// Available field types for the ComboBox
    /// </summary>
    public List<FieldType> AvailableFieldTypes { get; } = Enum.GetValues(typeof(FieldType)).Cast<FieldType>().ToList();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private IBrush _statusColor = Brushes.Gray;

    [ObservableProperty]
    private bool _hasStatusMessage;

    /// <summary>
    /// Indicates whether the document was successfully added
    /// </summary>
    public bool WasAdded { get; private set; }

    public AddDocumentDialogViewModel()
    {
        _logger = Program.LoggerFactory.CreateLogger<AddDocumentDialogViewModel>();
        InitializeEmptyFields();
    }

    public AddDocumentDialogViewModel(List<DocumentField> existingFields) : this()
    {
        InitializeFromExistingDocument(existingFields);
    }

    public void SetCloseAction(Action closeAction)
    {
        _closeDialog = closeAction;
    }

    private void InitializeEmptyFields()
    {
        // Add 5 empty rows for adding new fields
        for (int i = 0; i < 5; i++)
        {
            Fields.Add(NewField.CreateDefault());
        }
    }

    private void InitializeFromExistingDocument(List<DocumentField> existingFields)
    {
        Fields.Clear();

        // Add field names from the existing document, but leave values blank for new document creation
        foreach (var field in existingFields.Where(f => !f.IsDeleted))
        {
            Fields.Add(NewField.CreateDefault(field.FieldName, string.Empty));
        }

        // Add 3 empty rows at the end for additional fields
        for (int i = 0; i < 3; i++)
        {
            Fields.Add(NewField.CreateDefault());
        }

        _logger.LogInformation("Initialized add document dialog with {Count} field names from existing document", existingFields.Count);
    }

    [RelayCommand]
    private void AddDocument()
    {
        try
        {
            // Filter out empty fields
            var validFields = Fields.Where(f =>
                !string.IsNullOrWhiteSpace(f.Name) &&
                !string.IsNullOrWhiteSpace(f.Value)).ToList();

            if (validFields.Count == 0)
            {
                SetStatusMessage("Please add at least one field with both name and value.", Brushes.Orange);
                _logger.LogWarning("Add document cancelled: no valid fields");
                return;
            }

            _logger.LogInformation("Adding document with {Count} fields", validFields.Count);

            // Indicate success and close
            WasAdded = true;
            _closeDialog?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating document fields");
            SetStatusMessage($"Error: {ex.Message}", Brushes.Red);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _logger.LogInformation("Add document cancelled by user");
        WasAdded = false;
        _closeDialog?.Invoke();
    }

    private void SetStatusMessage(string message, IBrush color)
    {
        StatusMessage = message;
        StatusColor = color;
        HasStatusMessage = !string.IsNullOrEmpty(message);
    }

    /// <summary>
    /// Gets the list of valid fields to be added to the document
    /// </summary>
    public List<NewField> GetValidFields()
    {
        return Fields.Where(f =>
            !string.IsNullOrWhiteSpace(f.Name) &&
            !string.IsNullOrWhiteSpace(f.Value)).ToList();
    }
}
