using System;
using System.Globalization;

namespace NeoLuke.Models.Search;

/// <summary>
/// Settings for search query parsing and analysis
/// </summary>
public class SearchSettings
{
    /// <summary>
    /// The type of query parser to use
    /// </summary>
    public QueryParserType ParserType { get; set; } = QueryParserType.Standard;

    /// <summary>
    /// The type of analyzer to use (full type name for serialization)
    /// </summary>
    public string AnalyzerTypeName { get; private set; } = "Lucene.Net.Analysis.Standard.StandardAnalyzer";

    // ========== Query Parser Settings ==========

    /// <summary>
    /// The default operator for combining query terms (OR or AND)
    /// </summary>
    public QueryOperator DefaultOperator { get; set; } = QueryOperator.OR;

    /// <summary>
    /// Enable position increments in phrase queries
    /// </summary>
    public bool EnablePositionIncrements { get; set; } = true;

    /// <summary>
    /// Allow leading wildcard characters (* or ?) in queries
    /// </summary>
    public bool AllowLeadingWildcard { get; set; }

    /// <summary>
    /// Split on whitespace (Classic parser only)
    /// </summary>
    public bool SplitOnWhitespace { get; set; } = true;

    /// <summary>
    /// Auto-generate phrase queries (Classic parser only)
    /// </summary>
    public bool AutoGeneratePhraseQueries { get; set; }

    /// <summary>
    /// Default slop for phrase queries (0 = exact phrase match)
    /// </summary>
    public int PhraseSlop { get; set; }

    /// <summary>
    /// Minimum similarity for fuzzy queries (0.0-1.0 or edit distance if >= 1)
    /// </summary>
    public float FuzzyMinSim { get; set; } = 2f;

    /// <summary>
    /// Prefix length for fuzzy queries (characters that must match exactly)
    /// </summary>
    public int FuzzyPrefixLength { get; set; }

    /// <summary>
    /// Date resolution for range queries
    /// </summary>
    public DateResolution DateResolution { get; set; } = DateResolution.MILLISECOND;

    /// <summary>
    /// Locale for date parsing and text operations (culture name)
    /// </summary>
    public string LocaleName { get; set; } = CultureInfo.CurrentCulture.Name;

    /// <summary>
    /// Time zone ID for date/time parsing
    /// </summary>
    public string TimeZoneId { get; set; } = TimeZoneInfo.Local.Id;

    // ========== Similarity Settings ==========

    /// <summary>
    /// The type of similarity scoring to use
    /// </summary>
    public SimilarityType SimilarityType { get; set; } = SimilarityType.BM25;

    /// <summary>
    /// BM25 k1 parameter (controls non-linear term frequency normalization)
    /// </summary>
    public float BM25_K1 { get; set; } = 1.2f;

    /// <summary>
    /// BM25 b parameter (controls document length normalization)
    /// </summary>
    public float BM25_B { get; set; } = 0.75f;

    /// <summary>
    /// Whether to discount overlap tokens when computing norms
    /// </summary>
    public bool DiscountOverlaps { get; set; } = true;

    // ========== Helper Methods ==========

    /// <summary>
    /// Gets the analyzer Type from the type name
    /// </summary>
    public Type? GetAnalyzerType()
    {
        if (string.IsNullOrWhiteSpace(AnalyzerTypeName))
            return null;

        return Type.GetType(AnalyzerTypeName);
    }

    /// <summary>
    /// Sets the analyzer type name from a Type
    /// </summary>
    public void SetAnalyzerType(Type analyzerType)
    {
        if (analyzerType == null)
            throw new ArgumentNullException(nameof(analyzerType));

        AnalyzerTypeName = analyzerType.AssemblyQualifiedName ?? analyzerType.FullName ?? analyzerType.Name;
    }

    /// <summary>
    /// Gets a CultureInfo from the LocaleName property
    /// </summary>
    public CultureInfo GetCultureInfo()
    {
        try
        {
            return CultureInfo.GetCultureInfo(LocaleName);
        }
        catch
        {
            return CultureInfo.CurrentCulture;
        }
    }

    /// <summary>
    /// Gets a TimeZoneInfo from the TimeZoneId property
    /// </summary>
    public TimeZoneInfo GetTimeZoneInfo()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        }
        catch
        {
            return TimeZoneInfo.Local;
        }
    }
}

/// <summary>
/// Available query parser types
/// </summary>
public enum QueryParserType
{
    Classic,
    Standard
}

/// <summary>
/// Default operator for combining query terms
/// </summary>
public enum QueryOperator
{
    OR,
    AND
}

/// <summary>
/// Similarity scoring algorithms
/// </summary>
public enum SimilarityType
{
    /// <summary>
    /// BM25 similarity (default in modern Lucene)
    /// </summary>
    BM25,

    /// <summary>
    /// Classic TF-IDF similarity (DefaultSimilarity)
    /// </summary>
    Classic
}

/// <summary>
/// Date resolution for range queries
/// </summary>
public enum DateResolution
{
    YEAR,
    MONTH,
    DAY,
    HOUR,
    MINUTE,
    SECOND,
    MILLISECOND
}
