using System;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using NeoLuke.Models.Search;

namespace NeoLuke.ViewModels;

public partial class SearchSettingsDialogViewModel : ViewModelBase
{
    // Query Parser Settings
    [ObservableProperty]
    private QueryOperator _defaultOperator;

    [ObservableProperty]
    private bool _enablePositionIncrements;

    [ObservableProperty]
    private bool _allowLeadingWildcard;

    [ObservableProperty]
    private bool _splitOnWhitespace;

    [ObservableProperty]
    private bool _autoGeneratePhraseQueries;

    [ObservableProperty]
    private int _phraseSlop;

    [ObservableProperty]
    private float _fuzzyMinSim;

    [ObservableProperty]
    private int _fuzzyPrefixLength;

    [ObservableProperty]
    private ObservableCollection<DateResolution> _availableDateResolutions = [];

    [ObservableProperty]
    private DateResolution _selectedDateResolution;

    [ObservableProperty]
    private string _localeName = CultureInfo.CurrentCulture.Name;

    [ObservableProperty]
    private string _timeZoneId = TimeZoneInfo.Local.Id;

    // Similarity Settings
    [ObservableProperty]
    private SimilarityType _selectedSimilarityType;

    [ObservableProperty]
    private float _bm25K1;

    [ObservableProperty]
    private float _bm25B;

    [ObservableProperty]
    private bool _discountOverlaps;

    public SearchSettingsDialogViewModel()
    {
        // Initialize with default settings
        InitializeDateResolutions();
        InitializeDefaults();
    }

    public SearchSettingsDialogViewModel(SearchSettings settings) : this()
    {
        LoadFromSettings(settings);
    }

    private void InitializeDefaults()
    {
        // Query Parser defaults
        DefaultOperator = QueryOperator.OR;
        EnablePositionIncrements = true;
        AllowLeadingWildcard = false;
        SplitOnWhitespace = true;
        AutoGeneratePhraseQueries = false;
        PhraseSlop = 0;
        FuzzyMinSim = 2f;
        FuzzyPrefixLength = 0;
        SelectedDateResolution = DateResolution.MILLISECOND;
        LocaleName = CultureInfo.CurrentCulture.Name;
        TimeZoneId = TimeZoneInfo.Local.Id;

        // Similarity defaults
        SelectedSimilarityType = SimilarityType.BM25;
        Bm25K1 = 1.2f;
        Bm25B = 0.75f;
        DiscountOverlaps = true;
    }

    private void InitializeDateResolutions()
    {
        AvailableDateResolutions.Clear();
        foreach (DateResolution resolution in Enum.GetValues(typeof(DateResolution)))
        {
            AvailableDateResolutions.Add(resolution);
        }
    }

    private void LoadFromSettings(SearchSettings settings)
    {
        // Query parser settings
        DefaultOperator = settings.DefaultOperator;
        EnablePositionIncrements = settings.EnablePositionIncrements;
        AllowLeadingWildcard = settings.AllowLeadingWildcard;
        SplitOnWhitespace = settings.SplitOnWhitespace;
        AutoGeneratePhraseQueries = settings.AutoGeneratePhraseQueries;
        PhraseSlop = settings.PhraseSlop;
        FuzzyMinSim = settings.FuzzyMinSim;
        FuzzyPrefixLength = settings.FuzzyPrefixLength;
        SelectedDateResolution = settings.DateResolution;
        LocaleName = settings.LocaleName;
        TimeZoneId = settings.TimeZoneId;

        // Similarity settings
        SelectedSimilarityType = settings.SimilarityType;
        Bm25K1 = settings.BM25_K1;
        Bm25B = settings.BM25_B;
        DiscountOverlaps = settings.DiscountOverlaps;
    }

    public SearchSettings GetSettings()
    {
        var settings = new SearchSettings
        {
            // Query parser settings
            DefaultOperator = DefaultOperator,
            EnablePositionIncrements = EnablePositionIncrements,
            AllowLeadingWildcard = AllowLeadingWildcard,
            SplitOnWhitespace = SplitOnWhitespace,
            AutoGeneratePhraseQueries = AutoGeneratePhraseQueries,
            PhraseSlop = PhraseSlop,
            FuzzyMinSim = FuzzyMinSim,
            FuzzyPrefixLength = FuzzyPrefixLength,
            DateResolution = SelectedDateResolution,
            LocaleName = LocaleName,
            TimeZoneId = TimeZoneId,

            // Similarity settings
            SimilarityType = SelectedSimilarityType,
            BM25_K1 = Bm25K1,
            BM25_B = Bm25B,
            DiscountOverlaps = DiscountOverlaps
        };

        return settings;
    }
}
