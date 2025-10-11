using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeoLuke.Models.Documents;
using Microsoft.Extensions.Logging;

namespace NeoLuke.ViewModels;

public partial class DocumentsViewModel : ViewModelBase
{
    private readonly ILogger<DocumentsViewModel> _logger;
    private DocumentsModel? _documentsModel;
    private IClipboard? _clipboard;
    private Func<Task<bool>>? _showConfirmDeleteDialog;
    private Func<Task<bool>>? _showAddDocumentDialog;
    private Func<Task>? _onIndexChanged;
    private Action<int>? _navigateToMoreLikeThis;

    [ObservableProperty]
    private int _currentDocId;

    [ObservableProperty]
    private int _maxDocId;

    [ObservableProperty]
    private string _docCountText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DocumentField> _documentFields = [];

    [ObservableProperty]
    private IList? _selectedFields;

    [ObservableProperty]
    private bool _isIndexLoaded;

    [ObservableProperty]
    private bool _isCurrentDocumentDeleted;

    [ObservableProperty]
    private string _deleteButtonTooltip = "Delete the current document";

    [ObservableProperty]
    private string _addDocumentButtonTooltip = "Add a new document to the index";

    public DocumentsViewModel()
    {
        _logger = Program.LoggerFactory.CreateLogger<DocumentsViewModel>();
    }

    public void SetConfirmDeleteDialog(Func<Task<bool>> dialogFunc)
    {
        _showConfirmDeleteDialog = dialogFunc;
    }

    public void SetAddDocumentDialog(Func<Task<bool>> dialogFunc)
    {
        _showAddDocumentDialog = dialogFunc;
    }

    public void SetIndexChangedCallback(Func<Task> callback)
    {
        _onIndexChanged = callback;
    }

    public void SetClipboard(IClipboard? clipboard)
    {
        _clipboard = clipboard;
        _logger.LogDebug("Clipboard set for DocumentsViewModel");
    }

    public void SetMoreLikeThisCallback(Action<int> callback)
    {
        _navigateToMoreLikeThis = callback;
        // Notify command state change since the callback availability affects CanExecute
        MoreLikeThisCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedFieldsChanged(IList? value)
    {
        CopyValuesCommand.NotifyCanExecuteChanged();
    }

    partial void OnCurrentDocIdChanged(int value)
    {
        _logger.LogDebug("CurrentDocId changed to: {DocId}", value);
        UpdateDocCountText();
        _ = LoadDocumentAsync();
    }

    private void UpdateDocCountText()
    {
        if (_documentsModel != null)
        {
            var totalDocs = _documentsModel.GetMaxDoc();
            DocCountText = $"of {totalDocs}";
        }
        else
        {
            DocCountText = string.Empty;
        }
    }

    [RelayCommand(CanExecute = nameof(CanNavigatePrevious))]
    private void NavigatePrevious()
    {
        if (CanNavigatePrevious())
        {
            CurrentDocId--;
        }
    }

    private bool CanNavigatePrevious() => IsIndexLoaded && CurrentDocId > 0;

    [RelayCommand(CanExecute = nameof(CanNavigateNext))]
    private void NavigateNext()
    {
        if (CanNavigateNext())
        {
            CurrentDocId++;
        }
    }

    private bool CanNavigateNext() => IsIndexLoaded && CurrentDocId < MaxDocId;

    [RelayCommand(CanExecute = nameof(CanNavigateFirst))]
    private void NavigateFirst()
    {
        if (CanNavigateFirst())
        {
            CurrentDocId = 0;
        }
    }

    private bool CanNavigateFirst() => IsIndexLoaded && CurrentDocId > 0;

    [RelayCommand(CanExecute = nameof(CanNavigateLast))]
    private void NavigateLast()
    {
        if (CanNavigateLast())
        {
            CurrentDocId = MaxDocId;
        }
    }

    private bool CanNavigateLast() => IsIndexLoaded && CurrentDocId < MaxDocId;

    [RelayCommand(CanExecute = nameof(CanCopyValues))]
    private async Task CopyValues()
    {
        if (_clipboard == null || SelectedFields == null || SelectedFields.Count == 0)
        {
            _logger.LogWarning("Cannot copy values: clipboard or selection is null/empty");
            return;
        }

        var values = new List<string>();
        foreach (var item in SelectedFields)
        {
            if (item is DocumentField field)
            {
                values.Add(field.FieldValue);
            }
        }

        var textToCopy = string.Join("\n", values);
        await _clipboard.SetTextAsync(textToCopy);

        _logger.LogInformation("Copied {Count} field values to clipboard", values.Count);
    }

    private bool CanCopyValues() => _clipboard != null && SelectedFields != null && SelectedFields.Count > 0;

    [RelayCommand(CanExecute = nameof(CanDeleteDocument))]
    private async Task DeleteDocument()
    {
        if (_documentsModel == null || _showConfirmDeleteDialog == null)
        {
            _logger.LogWarning("Cannot delete document: model or dialog is null");
            return;
        }

        // Show confirmation dialog
        var confirmed = await _showConfirmDeleteDialog();
        if (!confirmed)
        {
            _logger.LogInformation("Document deletion cancelled by user");
            return;
        }

        try
        {
            _logger.LogInformation("Deleting document {DocId}", CurrentDocId);

            // Delete the document in a background task
            await Task.Run(() => _documentsModel.DeleteDocument(CurrentDocId));

            _logger.LogInformation("Document {DocId} deleted successfully", CurrentDocId);

            // Trigger index reload to make the changes visible
            if (_onIndexChanged != null)
            {
                _logger.LogInformation("Triggering index reload after deletion");
                await _onIndexChanged();
            }
            else
            {
                _logger.LogWarning("Index changed callback not set - changes may not be visible");
                // Fallback: just reload the current document
                await LoadDocumentAsync();
            }

            // Update navigation commands
            NavigatePreviousCommand.NotifyCanExecuteChanged();
            NavigateNextCommand.NotifyCanExecuteChanged();
            NavigateFirstCommand.NotifyCanExecuteChanged();
            NavigateLastCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document {DocId}", CurrentDocId);
            // TODO: Show error dialog to user
        }
    }

    private bool CanDeleteDocument() => IsIndexLoaded && _documentsModel != null && !_documentsModel.IsReadOnly && !IsCurrentDocumentDeleted;

    [RelayCommand(CanExecute = nameof(CanAddDocument))]
    private async Task AddDocument()
    {
        if (_documentsModel == null || _showAddDocumentDialog == null)
        {
            _logger.LogWarning("Cannot add document: model or dialog is null");
            return;
        }

        try
        {
            _logger.LogInformation("Opening add document dialog");

            // Show the add document dialog
            var wasAdded = await _showAddDocumentDialog();

            if (wasAdded)
            {
                _logger.LogInformation("Document was added, reloading index");

                // Trigger index reload to make the changes visible
                if (_onIndexChanged != null)
                {
                    await _onIndexChanged();
                }
                else
                {
                    _logger.LogWarning("Index changed callback not set - changes may not be visible");
                }

                // Navigate to the last document (the one we just added)
                CurrentDocId = MaxDocId;

                // Update navigation commands
                NavigatePreviousCommand.NotifyCanExecuteChanged();
                NavigateNextCommand.NotifyCanExecuteChanged();
                NavigateFirstCommand.NotifyCanExecuteChanged();
                NavigateLastCommand.NotifyCanExecuteChanged();
            }
            else
            {
                _logger.LogInformation("Add document cancelled by user");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add document");
            // TODO: Show error dialog to user
        }
    }

    private bool CanAddDocument() => IsIndexLoaded && _documentsModel != null && !_documentsModel.IsReadOnly;

    [RelayCommand(CanExecute = nameof(CanMoreLikeThis))]
    private void MoreLikeThis()
    {
        if (_navigateToMoreLikeThis == null)
        {
            _logger.LogWarning("Cannot navigate to More Like This: callback is null");
            return;
        }

        _logger.LogInformation("Navigating to More Like This for document {DocId}", CurrentDocId);
        _navigateToMoreLikeThis(CurrentDocId);
    }

    private bool CanMoreLikeThis() => IsIndexLoaded && !IsCurrentDocumentDeleted && _navigateToMoreLikeThis != null;

    /// <summary>
    /// Adds a document to the index (called from the view)
    /// </summary>
    public void AddDocumentToModel(List<NewField> fields)
    {
        if (_documentsModel == null)
        {
            throw new InvalidOperationException("Documents model is not initialized");
        }

        _documentsModel.AddDocument(fields);
    }

    /// <summary>
    /// Gets field names from a non-deleted document to use as a template for adding new documents
    /// </summary>
    /// <returns>List of field names from the first non-deleted document, or empty list if none found</returns>
    public List<DocumentField> GetFieldTemplateForNewDocument()
    {
        if (_documentsModel == null)
        {
            _logger.LogWarning("Cannot get field template: model is null");
            return [];
        }

        // If current document is not deleted, use it
        if (!IsCurrentDocumentDeleted)
        {
            return DocumentFields.ToList();
        }

        // Otherwise, find the first non-deleted document
        var firstNonDeletedDocId = _documentsModel.FindFirstNonDeletedDocument();
        if (firstNonDeletedDocId.HasValue)
        {
            _logger.LogInformation("Current document is deleted, using document {DocId} for field template", firstNonDeletedDocId.Value);
            var fields = _documentsModel.GetDocument(firstNonDeletedDocId.Value);
            return fields;
        }

        _logger.LogWarning("No non-deleted documents found for field template");
        return [];
    }

    private void UpdateTooltips()
    {
        // Update Delete button tooltip
        if (!IsIndexLoaded || _documentsModel == null)
        {
            DeleteButtonTooltip = "Open an index to delete documents";
        }
        else if (_documentsModel.IsReadOnly)
        {
            DeleteButtonTooltip = "Cannot delete: Index opened in read-only mode";
        }
        else if (IsCurrentDocumentDeleted)
        {
            DeleteButtonTooltip = "Cannot delete: Document is already deleted";
        }
        else
        {
            DeleteButtonTooltip = "Delete the current document";
        }

        // Update Add Document button tooltip
        if (!IsIndexLoaded || _documentsModel == null)
        {
            AddDocumentButtonTooltip = "Open an index to add documents";
        }
        else if (_documentsModel.IsReadOnly)
        {
            AddDocumentButtonTooltip = "Cannot add documents: Index opened in read-only mode";
        }
        else
        {
            AddDocumentButtonTooltip = "Add a new document to the index";
        }
    }

    private async Task LoadDocumentAsync()
    {
        _logger.LogDebug("Loading document at docId: {DocId}", CurrentDocId);

        if (_documentsModel == null)
        {
            _logger.LogDebug("LoadDocumentAsync early return: no model");
            return;
        }

        var fields = await Task.Run(() => _documentsModel.GetDocument(CurrentDocId));
        _logger.LogInformation("Loaded document {DocId} with {FieldCount} fields", CurrentDocId, fields.Count);

        // Check if this document is deleted
        IsCurrentDocumentDeleted = fields.Count > 0 && fields[0].IsDeleted;
        _logger.LogDebug("Document {DocId} deleted status: {IsDeleted}", CurrentDocId, IsCurrentDocumentDeleted);

        DocumentFields.Clear();
        foreach (var field in fields)
        {
            DocumentFields.Add(field);
        }

        // Update tooltips based on current state
        UpdateTooltips();

        // Update command states
        NavigatePreviousCommand.NotifyCanExecuteChanged();
        NavigateNextCommand.NotifyCanExecuteChanged();
        NavigateFirstCommand.NotifyCanExecuteChanged();
        NavigateLastCommand.NotifyCanExecuteChanged();
        DeleteDocumentCommand.NotifyCanExecuteChanged();
        MoreLikeThisCommand.NotifyCanExecuteChanged();
    }

    public async Task LoadIndexAsync(DocumentsModel model)
    {
        _logger.LogInformation("Loading index information for Documents tab");
        _documentsModel = model;

        MaxDocId = model.GetMaxDoc() - 1;
        CurrentDocId = 0;
        IsIndexLoaded = true;

        UpdateDocCountText();

        _logger.LogInformation("Documents tab initialized with {MaxDoc} documents", MaxDocId + 1);
        _logger.LogInformation("Delete functionality enabled: {Enabled}", !model.IsReadOnly);

        // Load the first document
        await LoadDocumentAsync();

        // Update tooltips based on loaded state
        UpdateTooltips();

        // Update command states
        DeleteDocumentCommand.NotifyCanExecuteChanged();
        AddDocumentCommand.NotifyCanExecuteChanged();
        MoreLikeThisCommand.NotifyCanExecuteChanged();
    }

    public void ClearIndex()
    {
        _documentsModel = null;
        CurrentDocId = 0;
        MaxDocId = 0;
        DocCountText = string.Empty;
        DocumentFields.Clear();
        IsIndexLoaded = false;
        IsCurrentDocumentDeleted = false;

        // Update tooltips for cleared state
        UpdateTooltips();

        // Update command states
        NavigatePreviousCommand.NotifyCanExecuteChanged();
        NavigateNextCommand.NotifyCanExecuteChanged();
        NavigateFirstCommand.NotifyCanExecuteChanged();
        NavigateLastCommand.NotifyCanExecuteChanged();
        DeleteDocumentCommand.NotifyCanExecuteChanged();
        AddDocumentCommand.NotifyCanExecuteChanged();
        MoreLikeThisCommand.NotifyCanExecuteChanged();
    }
}
