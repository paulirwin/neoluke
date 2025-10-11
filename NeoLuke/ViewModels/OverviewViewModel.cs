using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeoLuke.Models.Overview;
using Microsoft.Extensions.Logging;

namespace NeoLuke.ViewModels;

public partial class OverviewViewModel : ViewModelBase
{
    private readonly ILogger<OverviewViewModel> _logger;
    private OverviewModel? _overviewModel;
    private Action<string, string>? _searchByTermCallback;

    [ObservableProperty]
    private string _indexPath = string.Empty;

    [ObservableProperty]
    private string _numFields = string.Empty;

    [ObservableProperty]
    private string _numDocuments = string.Empty;

    [ObservableProperty]
    private string _numTerms = string.Empty;

    [ObservableProperty]
    private string _deletionsOptimized = string.Empty;

    [ObservableProperty]
    private string _indexVersion = string.Empty;

    [ObservableProperty]
    private string _indexFormat = string.Empty;

    [ObservableProperty]
    private string _directoryImpl = string.Empty;

    [ObservableProperty]
    private string _commitPoint = string.Empty;

    [ObservableProperty]
    private string _commitUserData = string.Empty;

    [ObservableProperty]
    private ObservableCollection<FieldStats> _fieldStats = [];

    [ObservableProperty]
    private FieldStats? _selectedField;

    [ObservableProperty]
    private ObservableCollection<TermStats> _topTerms = [];

    [ObservableProperty]
    private TermStats? _selectedTerm;

    [ObservableProperty]
    private int _maxTerms = 50;

    public OverviewViewModel()
    {
        _logger = Program.LoggerFactory.CreateLogger<OverviewViewModel>();
    }

    public void SetSearchByTermCallback(Action<string, string> callback)
    {
        _searchByTermCallback = callback;
        SearchByTermCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedTermChanged(TermStats? value)
    {
        SearchByTermCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedFieldChanged(FieldStats? value)
    {
        _logger.LogDebug("Field selection changed to: {FieldName}", value?.FieldName ?? "null");
        if (value != null)
        {
            _ = LoadTopTermsAsync();
        }
        else
        {
            TopTerms.Clear();
        }
    }

    partial void OnMaxTermsChanged(int value)
    {
        _logger.LogDebug("MaxTerms changed to: {MaxTerms}", value);
        if (SelectedField != null)
        {
            _ = LoadTopTermsAsync();
        }
    }

    private async Task LoadTopTermsAsync()
    {
        _logger.LogDebug("Loading top terms for field: {FieldName}, MaxTerms: {MaxTerms}",
            SelectedField?.FieldName ?? "null", MaxTerms);

        if (_overviewModel == null || SelectedField == null)
        {
            _logger.LogDebug("LoadTopTermsAsync early return: no model or field selected");
            return;
        }

        var terms = await Task.Run(() => _overviewModel.GetTopTerms(SelectedField.FieldName, MaxTerms));
        _logger.LogInformation("Loaded {Count} top terms for field: {FieldName}", terms.Count, SelectedField.FieldName);

        TopTerms.Clear();
        foreach (var term in terms)
        {
            TopTerms.Add(term);
        }
    }

    public async Task LoadIndexInfoAsync(OverviewModel model)
    {
        _logger.LogInformation("Loading index information for Overview tab");
        _overviewModel = model;

        IndexPath = model.GetIndexPath();
        NumFields = model.GetNumFields().ToString();
        NumDocuments = model.GetNumDocuments().ToString();
        NumTerms = model.GetNumTerms().ToString();
        DeletionsOptimized = model.GetDeletionsOptimizedString();
        IndexVersion = model.GetIndexVersion()?.ToString() ?? "?";
        IndexFormat = model.GetIndexFormat() ?? string.Empty;
        DirectoryImpl = model.GetDirImpl() ?? string.Empty;
        CommitPoint = model.GetCommitDescription() ?? "---";
        CommitUserData = model.GetCommitUserData() ?? "---";

        _logger.LogInformation("Index info: {NumDocs} documents, {NumFields} fields, {NumTerms} terms",
            NumDocuments, NumFields, NumTerms);

        // Load field statistics asynchronously
        var fieldStats = await Task.Run(() => model.GetFieldTermCounts());
        _logger.LogInformation("Loaded field statistics for {FieldCount} fields", fieldStats.Count);

        FieldStats.Clear();
        foreach (var stat in fieldStats)
        {
            FieldStats.Add(stat);
        }
    }

    [RelayCommand(CanExecute = nameof(CanSearchByTerm))]
    private void SearchByTerm()
    {
        if (SelectedField == null || SelectedTerm == null || _searchByTermCallback == null)
        {
            _logger.LogWarning("Cannot search by term: field, term, or callback is null");
            return;
        }

        var fieldName = SelectedField.FieldName;
        var term = SelectedTerm.Term;

        _logger.LogInformation("Searching for {Field}:{Term}", fieldName, term);
        _searchByTermCallback(fieldName, term);
    }

    private bool CanSearchByTerm() =>
        SelectedField != null &&
        SelectedTerm != null &&
        _searchByTermCallback != null;

    public void ClearIndexInfo()
    {
        _overviewModel = null;

        IndexPath = string.Empty;
        NumFields = string.Empty;
        NumDocuments = string.Empty;
        NumTerms = string.Empty;
        DeletionsOptimized = string.Empty;
        IndexVersion = string.Empty;
        IndexFormat = string.Empty;
        DirectoryImpl = string.Empty;
        CommitPoint = string.Empty;
        CommitUserData = string.Empty;
        FieldStats.Clear();
        SelectedField = null;
        TopTerms.Clear();
        SelectedTerm = null;
    }
}
