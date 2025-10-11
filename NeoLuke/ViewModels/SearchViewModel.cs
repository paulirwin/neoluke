using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeoLuke.Models.Search;
using NeoLuke.Views.Dialogs;
using NeoLuke.Utilities;
using Microsoft.Extensions.Logging;
using ExplainDialog = NeoLuke.Views.Dialogs.ExplainDialog;

namespace NeoLuke.ViewModels;

public partial class SearchViewModel : ViewModelBase
{
    private readonly ILogger<SearchViewModel> _logger;
    private SearchModel? _searchModel;
    private List<SearchResultRow> _allResults = [];
    private Window? _parentWindow;
    private SearchSettings _settings = new();
    private Action<int>? _navigateToDocument;

    [ObservableProperty]
    private ObservableCollection<SearchResultRow> _searchResults = [];

    [ObservableProperty]
    private SearchResultRow? _selectedResult;

    [ObservableProperty]
    private bool _isIndexLoaded;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _totalResults;

    [ObservableProperty]
    private string _pageInfoText = "Page 1 of 1 (0 results)";

    [ObservableProperty]
    private string _queryExpression = string.Empty;

    [ObservableProperty]
    private string _parsedQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _fieldNames = [];

    [ObservableProperty]
    private string? _selectedField;

    [ObservableProperty]
    private ObservableCollection<string> _analyzerNames = [];

    [ObservableProperty]
    private string? _selectedAnalyzer;

    [ObservableProperty]
    private ObservableCollection<string> _parserTypes = [];

    [ObservableProperty]
    private string? _selectedParserType;

    [ObservableProperty]
    private bool _isSearching;

    private readonly List<AnalyzerInfo> _availableAnalyzers;

    private const int PageSize = 10;

    public SearchViewModel()
    {
        _logger = Program.LoggerFactory.CreateLogger<SearchViewModel>();

        // Initialize analyzer list
        _availableAnalyzers = AnalyzerDiscovery.DiscoverAnalyzers();
        foreach (var analyzer in _availableAnalyzers)
        {
            AnalyzerNames.Add(analyzer.SimpleName);
        }

        // Select StandardAnalyzer as default
        SelectedAnalyzer = AnalyzerNames.FirstOrDefault(a => a == "StandardAnalyzer") ?? AnalyzerNames.FirstOrDefault();

        // Initialize parser types
        ParserTypes.Add("Classic");
        ParserTypes.Add("Standard");
        SelectedParserType = "Standard";
    }

    public void SetParentWindow(Window window)
    {
        _parentWindow = window;
    }

    public void SetNavigateToDocumentCallback(Action<int> callback)
    {
        _navigateToDocument = callback;
    }

    partial void OnQueryExpressionChanged(string value)
    {
        UpdateParsedQuery();
        SearchCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedFieldChanged(string? value)
    {
        UpdateParsedQuery();
    }

    partial void OnSelectedAnalyzerChanged(string? value)
    {
        if (value != null)
        {
            var analyzerInfo = _availableAnalyzers.FirstOrDefault(a => a.SimpleName == value);
            if (analyzerInfo != null)
            {
                _settings.SetAnalyzerType(analyzerInfo.Type);
                _logger.LogInformation("Analyzer changed to: {Analyzer}", value);

                // Update the SearchModel with new settings
                if (_searchModel != null)
                {
                    _searchModel.UpdateSettings(_settings);
                }

                // Re-parse the current query with new settings
                UpdateParsedQuery();
            }
        }
    }

    partial void OnSelectedParserTypeChanged(string? value)
    {
        if (value != null)
        {
            _settings.ParserType = value == "Standard" ? QueryParserType.Standard : QueryParserType.Classic;
            _logger.LogInformation("Parser type changed to: {ParserType}", value);

            // Update the SearchModel with new settings
            if (_searchModel != null)
            {
                _searchModel.UpdateSettings(_settings);
            }

            // Re-parse the current query with new settings
            UpdateParsedQuery();
        }
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

        // Notify paging commands since TotalPages changed
        FirstPageCommand.NotifyCanExecuteChanged();
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
        LastPageCommand.NotifyCanExecuteChanged();
    }

    private void UpdatePageInfo()
    {
        PageInfoText = $"Page {CurrentPage} of {TotalPages} ({TotalResults} results)";
    }

    [RelayCommand]
    private async Task OpenSettings()
    {
        if (_parentWindow == null)
        {
            _logger.LogWarning("Cannot open settings: parent window not set");
            return;
        }

        var dialog = new SearchSettingsDialog
        {
            DataContext = new SearchSettingsDialogViewModel(_settings)
        };

        var result = await dialog.ShowDialog<SearchSettings?>(_parentWindow);

        if (result != null)
        {
            _settings = result;
            _logger.LogInformation("Search settings updated: Parser={Parser}, Analyzer={Analyzer}",
                _settings.ParserType, _settings.AnalyzerTypeName);

            // Update the SearchModel with new settings
            if (_searchModel != null)
            {
                _searchModel.UpdateSettings(_settings);
            }

            // Re-parse the current query with new settings
            UpdateParsedQuery();
        }
    }

    private void UpdateParsedQuery()
    {
        if (_searchModel == null || string.IsNullOrWhiteSpace(QueryExpression) || string.IsNullOrWhiteSpace(SelectedField))
        {
            ParsedQuery = string.Empty;
            return;
        }

        try
        {
            var query = _searchModel.ParseQuery(QueryExpression, SelectedField);
            ParsedQuery = query?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse query: {Query}", QueryExpression);
            ParsedQuery = $"Parse error: {ex.Message}";
        }
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

    [RelayCommand(CanExecute = nameof(CanSearch))]
    private async Task Search()
    {
        if (_searchModel == null || string.IsNullOrWhiteSpace(QueryExpression) || string.IsNullOrWhiteSpace(SelectedField))
        {
            _logger.LogWarning("Cannot search: model, query, or field is null/empty");
            return;
        }

        IsSearching = true;

        try
        {
            _logger.LogInformation("Executing search: {Query} on field {Field}", QueryExpression, SelectedField);

            // Parse and execute search in background
            var results = await Task.Run(() =>
            {
                var query = _searchModel.ParseQuery(QueryExpression, SelectedField);
                if (query == null)
                {
                    _logger.LogWarning("Query parsing returned null");
                    return new SearchResults { TotalHits = 0, Results = [] };
                }

                return _searchModel.ExecuteSearch(query);
            });

            _logger.LogInformation("Search completed: {TotalHits} results", results.TotalHits);

            // Store all results for paging
            _allResults = results.Results;
            TotalResults = results.TotalHits;

            // Reset to first page (if already on page 1, property change won't fire)
            if (CurrentPage != 1)
            {
                CurrentPage = 1; // This will trigger OnCurrentPageChanged which notifies commands
            }
            else
            {
                // Already on page 1, so manually load and notify
                LoadCurrentPage();
                FirstPageCommand.NotifyCanExecuteChanged();
                PreviousPageCommand.NotifyCanExecuteChanged();
                NextPageCommand.NotifyCanExecuteChanged();
                LastPageCommand.NotifyCanExecuteChanged();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for query: {Query}", QueryExpression);
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

    private bool CanSearch() => IsIndexLoaded && !IsSearching && !string.IsNullOrWhiteSpace(QueryExpression) && !string.IsNullOrWhiteSpace(SelectedField);

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

    [RelayCommand(CanExecute = nameof(CanExplain))]
    private async Task Explain()
    {
        if (_searchModel == null || _parentWindow == null || SelectedResult == null)
        {
            _logger.LogWarning("Cannot explain: model, window, or selected result is null");
            return;
        }

        _logger.LogInformation("Explaining document {DocId}", SelectedResult.DocId);

        try
        {
            // Get explanation from model
            var explanation = await Task.Run(() => _searchModel.Explain(SelectedResult.DocId));

            if (explanation == null)
            {
                _logger.LogWarning("Explanation returned null for document {DocId}", SelectedResult.DocId);
                return;
            }

            // Create and show dialog
            var dialog = new ExplainDialog
            {
                DataContext = new ExplainDialogViewModel(SelectedResult.DocId, explanation)
            };

            await dialog.ShowDialog(_parentWindow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to explain document {DocId}", SelectedResult.DocId);
        }
    }

    private bool CanExplain() => IsIndexLoaded && SelectedResult != null && _searchModel?.CurrentQuery != null;

    partial void OnSelectedResultChanged(SearchResultRow? value)
    {
        ExplainCommand.NotifyCanExecuteChanged();
        ShowAllFieldsCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanShowAllFields))]
    private void ShowAllFields()
    {
        if (SelectedResult == null || _navigateToDocument == null)
        {
            _logger.LogWarning("Cannot show all fields: selected result or navigation callback is null");
            return;
        }

        _logger.LogInformation("Showing all fields for document {DocId}", SelectedResult.DocId);
        _navigateToDocument(SelectedResult.DocId);
    }

    private bool CanShowAllFields() => IsIndexLoaded && SelectedResult != null && _navigateToDocument != null;

    public void LoadIndex(SearchModel model)
    {
        _logger.LogInformation("Loading index information for Search tab");
        _searchModel = model;

        // Initialize model with current settings
        _searchModel.UpdateSettings(_settings);

        // Get field names
        var fields = model.GetFieldNames();
        FieldNames.Clear();
        foreach (var field in fields)
        {
            FieldNames.Add(field);
        }

        // Select first field as default
        if (FieldNames.Count > 0)
        {
            SelectedField = FieldNames[0];
        }

        _logger.LogInformation("Loaded {FieldCount} fields for search", FieldNames.Count);

        IsIndexLoaded = true;
        SearchResults.Clear();
        _allResults.Clear();
        TotalResults = 0;
        CurrentPage = 1;
        QueryExpression = string.Empty;
        ParsedQuery = string.Empty;

        SearchCommand.NotifyCanExecuteChanged();
    }

    public void ClearIndex()
    {
        _logger.LogInformation("Clearing index information for Search tab");
        _searchModel = null;
        SearchResults.Clear();
        _allResults.Clear();
        FieldNames.Clear();
        SelectedField = null;
        IsIndexLoaded = false;
        TotalResults = 0;
        CurrentPage = 1;
        QueryExpression = string.Empty;
        ParsedQuery = string.Empty;

        SearchCommand.NotifyCanExecuteChanged();
        FirstPageCommand.NotifyCanExecuteChanged();
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
        LastPageCommand.NotifyCanExecuteChanged();
    }
}
