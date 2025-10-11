using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Queries.Mlt;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoLuke.Models.Search;

/// <summary>
/// Model for More Like This functionality
/// </summary>
public class MoreLikeThisModel(IndexReader reader)
{
    private readonly IndexReader _reader = reader ?? throw new ArgumentNullException(nameof(reader));

    /// <summary>
    /// Creates a MoreLikeThis query based on a document ID
    /// </summary>
    /// <param name="docId">The document ID to use as a template</param>
    /// <param name="config">Configuration parameters for MLT</param>
    /// <param name="analyzer">The analyzer to use for term analysis</param>
    /// <returns>A Query that finds documents similar to the specified document</returns>
    public Query CreateMoreLikeThisQuery(int docId, MoreLikeThisConfig config, Analyzer analyzer)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        if (analyzer == null)
            throw new ArgumentNullException(nameof(analyzer));

        try
        {
            var mlt = new MoreLikeThis(_reader)
            {
                Analyzer = analyzer,
                MinDocFreq = config.MinDocFreq,
                MaxDocFreq = config.MaxDocFreq,
                MinTermFreq = config.MinTermFreq,
                MinWordLen = config.MinWordLen,
                MaxWordLen = config.MaxWordLen,
                MaxQueryTerms = config.MaxQueryTerms,
                BoostFactor = config.BoostFactor
            };

            // Set field names - if not specified, use all available fields
            if (config.FieldNames.Count > 0)
            {
                mlt.FieldNames = config.FieldNames.ToArray();
            }
            else
            {
                // Get all field names from the index if none specified
                var fields = GetFieldNames();
                if (fields.Count > 0)
                {
                    mlt.FieldNames = fields.ToArray();
                }
            }

            // Generate query from document
            return mlt.Like(docId);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create More Like This query for document {docId}", ex);
        }
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
}

/// <summary>
/// Configuration for More Like This query generation
/// </summary>
public class MoreLikeThisConfig
{
    // Constants for default values
    public const int DEFAULT_MIN_DOC_FREQ = 5;
    public const int DEFAULT_MAX_DOC_FREQ = int.MaxValue;
    public const int DEFAULT_MIN_TERM_FREQ = 2;
    public const int DEFAULT_MIN_WORD_LENGTH = 0;
    public const int DEFAULT_MAX_WORD_LENGTH = 0;
    public const int DEFAULT_MAX_QUERY_TERMS = 25;
    public const bool DEFAULT_BOOST = false;

    /// <summary>
    /// Minimum document frequency - terms must appear in at least this many docs
    /// Default: 5
    /// </summary>
    public int MinDocFreq { get; set; } = DEFAULT_MIN_DOC_FREQ;

    /// <summary>
    /// Maximum document frequency - terms must appear in no more than this many docs
    /// Default: int.MaxValue (unlimited)
    /// </summary>
    public int MaxDocFreq { get; set; } = DEFAULT_MAX_DOC_FREQ;

    /// <summary>
    /// Minimum term frequency - terms must appear at least this many times in the source doc
    /// Default: 2
    /// </summary>
    public int MinTermFreq { get; set; } = DEFAULT_MIN_TERM_FREQ;

    /// <summary>
    /// Minimum word length - words must be at least this long
    /// Default: 0 (no minimum)
    /// </summary>
    public int MinWordLen { get; set; } = DEFAULT_MIN_WORD_LENGTH;

    /// <summary>
    /// Maximum word length - words must be no longer than this
    /// Default: 0 (no maximum)
    /// </summary>
    public int MaxWordLen { get; set; } = DEFAULT_MAX_WORD_LENGTH;

    /// <summary>
    /// Maximum query terms - include at most this many terms in the query
    /// Default: 25
    /// </summary>
    public int MaxQueryTerms { get; set; } = DEFAULT_MAX_QUERY_TERMS;

    /// <summary>
    /// Boost factor for terms
    /// Default: false
    /// </summary>
    public bool Boost { get; set; } = DEFAULT_BOOST;

    /// <summary>
    /// Boost factor multiplier
    /// Default: 1.0
    /// </summary>
    public float BoostFactor { get; set; } = 1.0f;

    /// <summary>
    /// Field names to analyze - if null/empty, all fields are analyzed
    /// </summary>
    public List<string> FieldNames { get; set; } = [];

    /// <summary>
    /// Creates a new configuration with default values
    /// </summary>
    public MoreLikeThisConfig()
    {
    }

    /// <summary>
    /// Creates a copy of this configuration
    /// </summary>
    public MoreLikeThisConfig Clone()
    {
        return new MoreLikeThisConfig
        {
            MinDocFreq = MinDocFreq,
            MaxDocFreq = MaxDocFreq,
            MinTermFreq = MinTermFreq,
            MinWordLen = MinWordLen,
            MaxWordLen = MaxWordLen,
            MaxQueryTerms = MaxQueryTerms,
            Boost = Boost,
            BoostFactor = BoostFactor,
            FieldNames = [..FieldNames]
        };
    }
}
