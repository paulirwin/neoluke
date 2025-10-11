using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.QueryParsers.Flexible.Standard;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Util;
using NeoLuke.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Explanation = Lucene.Net.Search.Explanation;
using LuceneDateResolution = Lucene.Net.Documents.DateResolution;

namespace NeoLuke.Models.Search;

/// <summary>
/// Model for the Search tab that handles query parsing and search execution
/// </summary>
public class SearchModel
{
    private readonly IndexReader _reader;
    private readonly IndexSearcher _searcher;
    private SearchSettings _settings = new();
    private Query? _currentQuery;

    public SearchModel(IndexReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _searcher = new IndexSearcher(_reader);

        // Apply default similarity
        UpdateSimilarity();
    }

    /// <summary>
    /// Gets the current query that was last executed
    /// </summary>
    public Query? CurrentQuery => _currentQuery;

    /// <summary>
    /// Updates the search settings
    /// </summary>
    public void UpdateSettings(SearchSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        // Update similarity when settings change
        UpdateSimilarity();
    }

    /// <summary>
    /// Gets a list of all field names in the index
    /// </summary>
    public List<string> GetFieldNames()
    {
        var fields = MultiFields.GetFields(_reader);
        if (fields == null)
            return [];

        return fields.ToList();
    }

    /// <summary>
    /// Parses a query string using the configured parser and analyzer
    /// Returns null if parsing fails
    /// </summary>
    public Query? ParseQuery(string queryString, string defaultField)
    {
        if (string.IsNullOrWhiteSpace(queryString))
            return null;

        try
        {
            var analyzer = CreateAnalyzer();

            return _settings.ParserType switch
            {
                QueryParserType.Classic => ParseWithClassicParser(queryString, defaultField, analyzer),
                QueryParserType.Standard => ParseWithStandardParser(queryString, defaultField, analyzer),
                _ => ParseWithClassicParser(queryString, defaultField, analyzer)
            };
        }
        catch
        {
            return null;
        }
    }

    private Query ParseWithClassicParser(string queryString, string defaultField, Analyzer analyzer)
    {
        var parser = new QueryParser(LuceneVersion.LUCENE_48, defaultField, analyzer)
        {
            // Apply query parser settings
            DefaultOperator = _settings.DefaultOperator == QueryOperator.AND
                ? Operator.AND
                : Operator.OR,
            AllowLeadingWildcard = _settings.AllowLeadingWildcard,
            PhraseSlop = _settings.PhraseSlop,
            FuzzyMinSim = _settings.FuzzyMinSim,
            FuzzyPrefixLength = _settings.FuzzyPrefixLength
        };

        // Date/time settings
        var dateResolution = ConvertDateResolution(_settings.DateResolution);
        parser.SetDateResolution(dateResolution);
        parser.Locale = _settings.GetCultureInfo();
        parser.TimeZone = _settings.GetTimeZoneInfo();

        // Classic parser specific settings
        parser.AutoGeneratePhraseQueries = _settings.AutoGeneratePhraseQueries;

        // Note: EnablePositionIncrements and SplitOnWhitespace are not directly exposed
        // in QueryParser API for Lucene.NET 4.8 - these are analyzer-level concerns

        return parser.Parse(queryString);
    }

    private Query ParseWithStandardParser(string queryString, string defaultField, Analyzer analyzer)
    {
        var parser = new StandardQueryParser(analyzer)
        {
            // Apply query parser settings
            DefaultOperator = _settings.DefaultOperator == QueryOperator.AND
                ? StandardQueryConfigHandler.Operator.AND
                : StandardQueryConfigHandler.Operator.OR,
            AllowLeadingWildcard = _settings.AllowLeadingWildcard,
            PhraseSlop = _settings.PhraseSlop,
            FuzzyMinSim = _settings.FuzzyMinSim,
            FuzzyPrefixLength = _settings.FuzzyPrefixLength
        };

        // Date/time settings
        var dateResolution = ConvertDateResolution(_settings.DateResolution);
        parser.SetDateResolution(dateResolution);
        parser.Locale = _settings.GetCultureInfo();
        parser.TimeZone = _settings.GetTimeZoneInfo();

        // TODO: Auto-generate multi-term synonyms phrase query - requires Lucene 6+
        // This feature is not available in Lucene.NET 4.8

        return parser.Parse(queryString, defaultField);
    }

    private Analyzer CreateAnalyzer()
    {
        var analyzerType = _settings.GetAnalyzerType();
        if (analyzerType == null)
        {
            // Fall back to StandardAnalyzer if type cannot be resolved
            var defaultAnalyzer = AnalyzerDiscovery.FindByName("StandardAnalyzer");
            analyzerType = defaultAnalyzer?.Type ?? typeof(Lucene.Net.Analysis.Standard.StandardAnalyzer);
        }

        return AnalyzerDiscovery.CreateAnalyzer(analyzerType);
    }

    /// <summary>
    /// Executes a search query and returns the results
    /// </summary>
    /// <param name="query">The parsed query to execute</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    public SearchResults ExecuteSearch(Query query, int maxResults = 1000)
    {
        // Store the current query for later use (e.g., explain)
        _currentQuery = query ?? throw new ArgumentNullException(nameof(query));

        var topDocs = _searcher.Search(query, maxResults);
        var results = new List<SearchResultRow>();

        foreach (var scoreDoc in topDocs.ScoreDocs)
        {
            var doc = _searcher.Doc(scoreDoc.Doc);

            // Collect field values for display
            var fieldValues = new List<string>();
            foreach (var field in doc.Fields)
            {
                var value = field.GetStringValue();
                if (!string.IsNullOrEmpty(value))
                {
                    // Truncate long values
                    var displayValue = value.Length > 100 ? value.Substring(0, 100) + "..." : value;
                    fieldValues.Add($"{field.Name}: {displayValue}");
                }
            }

            results.Add(new SearchResultRow
            {
                DocId = scoreDoc.Doc,
                Score = scoreDoc.Score,
                FieldValues = string.Join("; ", fieldValues)
            });
        }

        return new SearchResults
        {
            TotalHits = topDocs.TotalHits,
            Results = results
        };
    }

    /// <summary>
    /// Explains why a document matched the current query
    /// </summary>
    /// <param name="docId">The document ID to explain</param>
    /// <returns>The explanation object from Lucene</returns>
    public Explanation? Explain(int docId)
    {
        if (_currentQuery == null)
            return null;

        try
        {
            return _searcher.Explain(_currentQuery, docId);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Updates the similarity configuration on the searcher
    /// </summary>
    private void UpdateSimilarity()
    {
        Similarity similarity = _settings.SimilarityType switch
        {
            SimilarityType.BM25 => CreateBM25Similarity(),
            SimilarityType.Classic => CreateClassicSimilarity(),
            _ => CreateBM25Similarity()
        };

        _searcher.Similarity = similarity;
    }

    /// <summary>
    /// Creates a BM25 similarity with configured parameters
    /// </summary>
    private BM25Similarity CreateBM25Similarity()
    {
        var similarity = new BM25Similarity(_settings.BM25_K1, _settings.BM25_B)
        {
            DiscountOverlaps = _settings.DiscountOverlaps
        };
        return similarity;
    }

    /// <summary>
    /// Creates a classic TF-IDF similarity with configured parameters
    /// </summary>
    private DefaultSimilarity CreateClassicSimilarity()
    {
        var similarity = new DefaultSimilarity
        {
            DiscountOverlaps = _settings.DiscountOverlaps
        };
        return similarity;
    }

    /// <summary>
    /// Converts our DateResolution enum to Lucene's DateResolution enum
    /// </summary>
    private static LuceneDateResolution ConvertDateResolution(DateResolution resolution)
    {
        return resolution switch
        {
            DateResolution.YEAR => LuceneDateResolution.YEAR,
            DateResolution.MONTH => LuceneDateResolution.MONTH,
            DateResolution.DAY => LuceneDateResolution.DAY,
            DateResolution.HOUR => LuceneDateResolution.HOUR,
            DateResolution.MINUTE => LuceneDateResolution.MINUTE,
            DateResolution.SECOND => LuceneDateResolution.SECOND,
            _ => LuceneDateResolution.MILLISECOND
        };
    }
}

/// <summary>
/// Represents the results of a search operation
/// </summary>
public class SearchResults
{
    public int TotalHits { get; set; }
    public List<SearchResultRow> Results { get; set; } = [];
}
