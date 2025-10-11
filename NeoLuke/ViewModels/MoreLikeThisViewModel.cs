using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lucene.Net.Analysis;
using Microsoft.Extensions.Logging;
using NeoLuke.Models.Search;
using NeoLuke.Utilities;

namespace NeoLuke.ViewModels;

public partial class MoreLikeThisViewModel : ViewModelBase
{
    private readonly ILogger<MoreLikeThisViewModel> _logger;
    private MoreLikeThisModel? _mltModel;
    private SearchModel? _searchModel;
    private List<SearchResultRow> _allResults = [];
    private readonly List<AnalyzerInfo> _availableAnalyzers;

    [ObservableProperty]
    private bool _isIndexLoaded;

    [ObservableProperty]
    private int _documentId;

    [ObservableProperty]
    private int _minDocFreq = MoreLikeThisConfig.DEFAULT_MIN_DOC_FREQ;

    [ObservableProperty]
    private int _maxDocFreq = MoreLikeThisConfig.DEFAULT_MAX_DOC_FREQ;

    [ObservableProperty]
    private int _minTermFreq = MoreLikeThisConfig.DEFAULT_MIN_TERM_FREQ;

    [ObservableProperty]
    private int _minWordLen;

    [ObservableProperty]
    private int _maxWordLen;

    [ObservableProperty]
    private int _maxQueryTerms = MoreLikeThisConfig.DEFAULT_MAX_QUERY_TERMS;

    [ObservableProperty]
    private bool _boost;

    [ObservableProperty]
    private float _boostFactor = 1.0f;

    [ObservableProperty]
    private ObservableCollection<FieldSelectionItem> _fieldsList = [];

    [ObservableProperty]
    private bool _selectAllFields = true;

    [ObservableProperty]
    private ObservableCollection<string> _analyzerNames = [];

    [ObservableProperty]
    private string? _selectedAnalyzer;

    [ObservableProperty]
    private string _parsedQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SearchResultRow> _searchResults = [];

    [ObservableProperty]
    private SearchResultRow? _selectedResult;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _totalResults;

    [ObservableProperty]
    private string _pageInfoText = "Page 1 of 1 (0 results)";

    [ObservableProperty]
    private bool _isSearching;

    private const int PageSize = 10;

    public MoreLikeThisViewModel()
    {
        _logger = Program.LoggerFactory.CreateLogger<MoreLikeThisViewModel>();

        // Initialize analyzer list
        _availableAnalyzers = AnalyzerDiscovery.DiscoverAnalyzers();
        foreach (var analyzer in _availableAnalyzers)
        {
            AnalyzerNames.Add(analyzer.SimpleName);
        }

        // Select StandardAnalyzer as default
        SelectedAnalyzer = AnalyzerNames.FirstOrDefault(a => a == "StandardAnalyzer") ?? AnalyzerNames.FirstOrDefault();
    }

    partial void OnSelectAllFieldsChanged(bool value)
    {
        foreach (var field in FieldsList)
        {
            field.IsSelected = value;
        }
    }

    partial void OnDocumentIdChanged(int value)
    {
        UpdateParsedQuery();
        SearchCommand.NotifyCanExecuteChanged();
    }

    partial void OnMinDocFreqChanged(int value)
    {
        UpdateParsedQuery();
    }

    partial void OnMaxDocFreqChanged(int value)
    {
        UpdateParsedQuery();
    }

    partial void OnMinTermFreqChanged(int value)
    {
        UpdateParsedQuery();
    }

    partial void OnMinWordLenChanged(int value)
    {
        UpdateParsedQuery();
    }

    partial void OnMaxWordLenChanged(int value)
    {
        UpdateParsedQuery();
    }

    partial void OnMaxQueryTermsChanged(int value)
    {
        UpdateParsedQuery();
    }

    partial void OnBoostChanged(bool value)
    {
        UpdateParsedQuery();
    }

    partial void OnBoostFactorChanged(float value)
    {
        UpdateParsedQuery();
    }

    partial void OnSelectedAnalyzerChanged(string? value)
    {
        UpdateParsedQuery();
    }

    partial void OnCurrentPageChanged(int value)
    {
        UpdatePageInfo();
        LoadCurrentPage();
        FirstPageCommand.NotifyCanExecuteChanged();
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
        LastPageCommand.NotifyCanExecuteChanged();
    }

    partial void OnTotalResultsChanged(int value)
    {
        TotalPages = value > 0 ? (int)Math.Ceiling(value / (double)PageSize) : 1;
        UpdatePageInfo();

        FirstPageCommand.NotifyCanExecuteChanged();
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
        LastPageCommand.NotifyCanExecuteChanged();
    }

    private void UpdatePageInfo()
    {
        PageInfoText = $"Page {CurrentPage} of {TotalPages} ({TotalResults} results)";
    }

    private void LoadCurrentPage()
    {
        SearchResults.Clear();

        if (_allResults.Count == 0)
            return;

        var skip = (CurrentPage - 1) * PageSize;
        var pageResults = _allResults.Skip(skip).Take(PageSize);

        foreach (var result in pageResults)
        {
            SearchResults.Add(result);
        }
    }

    private void UpdateParsedQuery()
    {
        if (_mltModel == null || !IsIndexLoaded)
        {
            ParsedQuery = string.Empty;
            return;
        }

        try
        {
            var config = BuildConfig();
            var analyzer = CreateAnalyzer();
            var query = _mltModel.CreateMoreLikeThisQuery(DocumentId, config, analyzer);
            ParsedQuery = query.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate MLT query for document {DocId}", DocumentId);
            ParsedQuery = $"Error: {ex.Message}";
        }
    }

    private MoreLikeThisConfig BuildConfig()
    {
        var config = new MoreLikeThisConfig
        {
            MinDocFreq = MinDocFreq,
            MaxDocFreq = MaxDocFreq,
            MinTermFreq = MinTermFreq,
            MinWordLen = MinWordLen,
            MaxWordLen = MaxWordLen,
            MaxQueryTerms = MaxQueryTerms,
            Boost = Boost,
            BoostFactor = BoostFactor,
            FieldNames = FieldsList.Where(f => f.IsSelected).Select(f => f.FieldName).ToList()
        };

        return config;
    }

    private Analyzer CreateAnalyzer()
    {
        var analyzerInfo = _availableAnalyzers.FirstOrDefault(a => a.SimpleName == SelectedAnalyzer);
        if (analyzerInfo != null)
        {
            return AnalyzerDiscovery.CreateAnalyzer(analyzerInfo.Type);
        }

        // Fallback to StandardAnalyzer
        var defaultAnalyzer = _availableAnalyzers.FirstOrDefault(a => a.SimpleName == "StandardAnalyzer");
        return AnalyzerDiscovery.CreateAnalyzer(defaultAnalyzer?.Type ?? typeof(Lucene.Net.Analysis.Standard.StandardAnalyzer));
    }

    [RelayCommand(CanExecute = nameof(CanSearch))]
    private async Task Search()
    {
        if (_mltModel == null || _searchModel == null)
        {
            _logger.LogWarning("Cannot search: MLT model or search model is null");
            return;
        }

        IsSearching = true;

        try
        {
            _logger.LogInformation("Executing More Like This search for document {DocId}", DocumentId);

            // Generate query and execute search in background
            var results = await Task.Run(() =>
            {
                var config = BuildConfig();
                var analyzer = CreateAnalyzer();
                var query = _mltModel.CreateMoreLikeThisQuery(DocumentId, config, analyzer);

                return _searchModel.ExecuteSearch(query);
            });

            _logger.LogInformation("More Like This search completed: {TotalHits} results", results.TotalHits);

            // Store all results for paging
            _allResults = results.Results;
            TotalResults = results.TotalHits;

            // Reset to first page
            if (CurrentPage != 1)
            {
                CurrentPage = 1;
            }
            else
            {
                LoadCurrentPage();
                FirstPageCommand.NotifyCanExecuteChanged();
                PreviousPageCommand.NotifyCanExecuteChanged();
                NextPageCommand.NotifyCanExecuteChanged();
                LastPageCommand.NotifyCanExecuteChanged();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "More Like This search failed for document {DocId}", DocumentId);
            ParsedQuery = $"Search error: {ex.Message}";
            _allResults.Clear();
            TotalResults = 0;
            SearchResults.Clear();
        }
        finally
        {
            IsSearching = false;
        }
    }

    private bool CanSearch() => IsIndexLoaded && !IsSearching && DocumentId >= 0;

    [RelayCommand(CanExecute = nameof(CanNavigateToFirstPage))]
    private void FirstPage()
    {
        if (CanNavigateToFirstPage())
        {
            CurrentPage = 1;
        }
    }

    private bool CanNavigateToFirstPage() => IsIndexLoaded && CurrentPage > 1;

    [RelayCommand(CanExecute = nameof(CanNavigateToPreviousPage))]
    private void PreviousPage()
    {
        if (CanNavigateToPreviousPage())
        {
            CurrentPage--;
        }
    }

    private bool CanNavigateToPreviousPage() => IsIndexLoaded && CurrentPage > 1;

    [RelayCommand(CanExecute = nameof(CanNavigateToNextPage))]
    private void NextPage()
    {
        if (CanNavigateToNextPage())
        {
            CurrentPage++;
        }
    }

    private bool CanNavigateToNextPage() => IsIndexLoaded && CurrentPage < TotalPages;

    [RelayCommand(CanExecute = nameof(CanNavigateToLastPage))]
    private void LastPage()
    {
        if (CanNavigateToLastPage())
        {
            CurrentPage = TotalPages;
        }
    }

    private bool CanNavigateToLastPage() => IsIndexLoaded && CurrentPage < TotalPages;

    public void LoadIndex(MoreLikeThisModel mltModel, SearchModel searchModel)
    {
        _logger.LogInformation("Loading index information for More Like This tab");
        _mltModel = mltModel;
        _searchModel = searchModel;

        // Get field names
        var fields = mltModel.GetFieldNames();
        FieldsList.Clear();
        foreach (var field in fields)
        {
            FieldsList.Add(new FieldSelectionItem { FieldName = field, IsSelected = true });
        }

        _logger.LogInformation("Loaded {FieldCount} fields for More Like This", FieldsList.Count);

        IsIndexLoaded = true;
        SearchResults.Clear();
        _allResults.Clear();
        TotalResults = 0;
        CurrentPage = 1;
        DocumentId = 0;
        ParsedQuery = string.Empty;

        // Reset to defaults
        MinDocFreq = MoreLikeThisConfig.DEFAULT_MIN_DOC_FREQ;
        MaxDocFreq = MoreLikeThisConfig.DEFAULT_MAX_DOC_FREQ;
        MinTermFreq = MoreLikeThisConfig.DEFAULT_MIN_TERM_FREQ;
        MinWordLen = 0;
        MaxWordLen = 0;
        MaxQueryTerms = MoreLikeThisConfig.DEFAULT_MAX_QUERY_TERMS;
        Boost = false;
        BoostFactor = 1.0f;
        SelectAllFields = true;

        SearchCommand.NotifyCanExecuteChanged();
    }

    public void ClearIndex()
    {
        _logger.LogInformation("Clearing index information for More Like This tab");
        _mltModel = null;
        _searchModel = null;
        SearchResults.Clear();
        _allResults.Clear();
        FieldsList.Clear();
        IsIndexLoaded = false;
        TotalResults = 0;
        CurrentPage = 1;
        DocumentId = 0;
        ParsedQuery = string.Empty;

        SearchCommand.NotifyCanExecuteChanged();
        FirstPageCommand.NotifyCanExecuteChanged();
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
        LastPageCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Sets the document ID for More Like This search (called from Documents tab)
    /// </summary>
    public void SetDocumentId(int docId)
    {
        DocumentId = docId;
        _logger.LogInformation("More Like This document ID set to: {DocId}", docId);
    }
}

/// <summary>
/// Represents a field with selection state for the fields list
/// </summary>
public partial class FieldSelectionItem : ObservableObject
{
    [ObservableProperty]
    private string _fieldName = string.Empty;

    [ObservableProperty]
    private bool _isSelected = true;
}
