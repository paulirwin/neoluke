using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LuceneDirectory = Lucene.Net.Store.Directory;

namespace NeoLuke.Models.Overview;

/// <summary>
/// Model for the Overview tab that provides index statistics and information
/// </summary>
public class OverviewModel(IndexReader reader, string indexPath, LuceneDirectory? directory = null)
{
    private readonly IndexReader _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    private readonly string _indexPath = indexPath ?? throw new ArgumentNullException(nameof(indexPath));

    /// <summary>
    /// Gets the index directory path
    /// </summary>
    public string GetIndexPath() => _indexPath;

    /// <summary>
    /// Gets the number of fields in the index
    /// </summary>
    public int GetNumFields()
    {
        var fieldInfos = MultiFields.GetMergedFieldInfos(_reader);
        return fieldInfos.Count;
    }

    /// <summary>
    /// Gets the number of documents in the index (excluding deleted docs)
    /// </summary>
    public int GetNumDocuments() => _reader.NumDocs;

    /// <summary>
    /// Gets the total number of terms across all fields
    /// </summary>
    public long GetNumTerms()
    {
        long totalTerms = 0;
        var fields = MultiFields.GetFields(_reader);

        if (fields == null)
            return 0;

        foreach (var field in fields)
        {
            var terms = fields.GetTerms(field);
            if (terms != null)
            {
                totalTerms += terms.Count;
            }
        }

        return totalTerms;
    }

    /// <summary>
    /// Returns true if the index has deletions
    /// </summary>
    public bool HasDeletions() => _reader.HasDeletions;

    /// <summary>
    /// Gets the number of deleted documents
    /// </summary>
    public int GetNumDeletedDocs() => _reader.NumDeletedDocs;

    /// <summary>
    /// Returns true if the index is optimized (has only one segment)
    /// </summary>
    public bool? IsOptimized()
    {
        if (_reader is DirectoryReader dirReader)
        {
            var indexCommit = dirReader.IndexCommit;
            return indexCommit?.SegmentCount == 1;
        }
        return null;
    }

    /// <summary>
    /// Gets the index version
    /// </summary>
    public long? GetIndexVersion()
    {
        if (_reader is DirectoryReader dirReader)
        {
            return dirReader.Version;
        }
        return null;
    }

    /// <summary>
    /// Gets the index format (Lucene version)
    /// </summary>
    public string GetIndexFormat()
    {
        try
        {
            // Get segments from the reader
            var leaves = _reader.Leaves;
            if (leaves is { Count: > 0 })
            {
                // Check the first segment for version information
                var firstSegment = leaves[0].Reader;
                if (firstSegment is SegmentReader segmentReader)
                {
                    var segmentInfo = segmentReader.SegmentInfo;
                    if (segmentInfo is { Info: not null })
                    {
                        var version = segmentInfo.Info.Version;
                        if (!string.IsNullOrEmpty(version))
                        {
                            // Version is a string like "4.8.0" - return it with "Lucene" prefix
                            return $"Lucene {version}";
                        }
                    }
                }
            }

            // Default fallback - version couldn't be determined
            return "Unknown";
        }
        catch
        {
            // If we can't read the version, return unknown
            return "Unknown";
        }
    }

    /// <summary>
    /// Gets the Directory implementation class name
    /// </summary>
    public string? GetDirImpl()
    {
        return directory?.GetType().FullName;
    }

    /// <summary>
    /// Gets the commit point description
    /// </summary>
    public string? GetCommitDescription()
    {
        if (_reader is DirectoryReader dirReader)
        {
            var commit = dirReader.IndexCommit;
            if (commit != null)
            {
                return $"{commit.SegmentsFileName} (generation={commit.Generation}, segs={commit.SegmentCount})";
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the commit user data
    /// </summary>
    public string? GetCommitUserData()
    {
        if (_reader is DirectoryReader dirReader)
        {
            var commit = dirReader.IndexCommit;
            if (commit != null)
            {
                var userData = commit.UserData;
                if (userData != null && userData.Count > 0)
                {
                    return string.Join(", ", userData.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Gets a formatted string for deletions/optimized status
    /// </summary>
    public string GetDeletionsOptimizedString()
    {
        string deletions = HasDeletions()
            ? string.Format(CultureInfo.InvariantCulture, "Yes ({0})", GetNumDeletedDocs())
            : "No";

        string optimized = IsOptimized() switch
        {
            true => "Yes",
            false => "No",
            null => "?"
        };

        return $"{deletions} / {optimized}";
    }

    /// <summary>
    /// Gets field statistics sorted by term count descending
    /// </summary>
    public List<FieldStats> GetFieldTermCounts()
    {
        var fields = MultiFields.GetFields(_reader);
        if (fields == null)
        {
            System.Diagnostics.Debug.WriteLine("GetFieldTermCounts: fields is null");
            return [];
        }

        long totalTerms = GetNumTerms();
        var fieldStats = new List<FieldStats>();

        System.Diagnostics.Debug.WriteLine($"GetFieldTermCounts: totalTerms={totalTerms}");

        foreach (var field in fields)
        {
            var terms = fields.GetTerms(field);
            if (terms != null)
            {
                long termCount = terms.Count;

                // Terms.Count can be -1 if unknown, skip if so
                if (termCount >= 0)
                {
                    double percentage = totalTerms > 0
                        ? (termCount * 100.0) / totalTerms
                        : 0.0;

                    System.Diagnostics.Debug.WriteLine($"  Field: {field}, TermCount: {termCount}, Percentage: {percentage:F2}%");
                    fieldStats.Add(new FieldStats(field, termCount, percentage));
                }
            }
        }

        System.Diagnostics.Debug.WriteLine($"GetFieldTermCounts: returning {fieldStats.Count} field stats");

        // Sort by term count descending
        return fieldStats.OrderByDescending(f => f.TermCount).ToList();
    }

    /// <summary>
    /// Gets the top terms for a specific field sorted by document frequency descending
    /// </summary>
    /// <param name="fieldName">The field name to get terms for</param>
    /// <param name="maxTerms">Maximum number of terms to return</param>
    public List<TermStats> GetTopTerms(string fieldName, int maxTerms)
    {
        var fields = MultiFields.GetFields(_reader);
        if (fields == null)
        {
            System.Diagnostics.Debug.WriteLine($"GetTopTerms: fields is null");
            return [];
        }

        var terms = fields.GetTerms(fieldName);
        if (terms == null)
        {
            System.Diagnostics.Debug.WriteLine($"GetTopTerms: no terms for field '{fieldName}'");
            return [];
        }

        var termStats = new List<TermStats>();
        // ReSharper disable once GenericEnumeratorNotDisposed
        var termsEnum = terms.GetEnumerator();

        System.Diagnostics.Debug.WriteLine($"GetTopTerms: reading terms for field '{fieldName}', maxTerms={maxTerms}");

        while (termsEnum.MoveNext())
        {
            var term = termsEnum.Term.Utf8ToString();
            var docFreq = termsEnum.DocFreq;

            termStats.Add(new TermStats(term, docFreq));
        }

        System.Diagnostics.Debug.WriteLine($"GetTopTerms: found {termStats.Count} terms, returning top {maxTerms}");

        // Sort by frequency descending and take top N
        return termStats.OrderByDescending(t => t.Frequency)
                       .Take(maxTerms)
                       .ToList();
    }
}
