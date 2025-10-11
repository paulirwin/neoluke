using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LuceneDirectory = Lucene.Net.Store.Directory;

namespace NeoLuke.Models.Tools;

/// <summary>
/// Model for index optimization and maintenance operations
/// </summary>
public class IndexToolsModel : IDisposable
{
    private readonly LuceneDirectory _directory;
    private readonly bool _isReadOnly;
    private readonly IndexReader? _reader;

    public IndexToolsModel(LuceneDirectory directory, bool isReadOnly)
    {
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        _isReadOnly = isReadOnly;
    }

    public IndexToolsModel(IndexReader reader, LuceneDirectory directory, bool isReadOnly)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        _isReadOnly = isReadOnly;
    }

    /// <summary>
    /// Gets whether the index is opened in read-only mode
    /// </summary>
    public bool IsReadOnly => _isReadOnly;

    /// <summary>
    /// Optimizes the index by performing force merge operations.
    /// This reduces the number of segments in the index.
    /// </summary>
    /// <param name="expunge">If true, only segments having deleted documents are merged</param>
    /// <param name="maxNumSegments">Maximum number of segments (ignored if expunge is true)</param>
    /// <param name="logWriter">TextWriter for logging progress</param>
    /// <exception cref="InvalidOperationException">Thrown when index is opened in read-only mode</exception>
    public void Optimize(bool expunge, int maxNumSegments, TextWriter? logWriter)
    {
        if (_isReadOnly)
        {
            throw new InvalidOperationException("Cannot optimize index when opened in read-only mode.");
        }

        if (maxNumSegments < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxNumSegments), "Maximum number of segments must be at least 1.");
        }

        // Create an IndexWriter with InfoStream for logging
        var config = new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, null)
        {
            OpenMode = OpenMode.APPEND
        };

        // Set up InfoStream for logging
        if (logWriter != null)
        {
            config.SetInfoStream(new TextWriterInfoStream(logWriter));
        }

        using var writer = new IndexWriter(_directory, config);
        if (expunge)
        {
            // Force merge deletes only - removes deleted documents
            logWriter?.WriteLine("Starting force merge deletes...");
            writer.ForceMergeDeletes();
            logWriter?.WriteLine("Force merge deletes completed.");
        }
        else
        {
            // Force merge to maxNumSegments
            logWriter?.WriteLine($"Starting force merge to {maxNumSegments} segment(s)...");
            writer.ForceMerge(maxNumSegments);
            logWriter?.WriteLine($"Force merge to {maxNumSegments} segment(s) completed.");
        }

        writer.Commit();
        logWriter?.WriteLine("Changes committed successfully.");
    }

    /// <summary>
    /// Checks the current index for corruption or issues.
    /// </summary>
    /// <param name="logWriter">TextWriter for logging progress</param>
    /// <returns>CheckIndex.Status containing the results</returns>
    public CheckIndex.Status CheckIndex(TextWriter? logWriter)
    {
        var checkIndex = new CheckIndex(_directory);

        if (logWriter != null)
        {
            checkIndex.InfoStream = logWriter;
        }

        return checkIndex.DoCheckIndex();
    }

    /// <summary>
    /// Attempts to repair a corrupted index using a previously obtained CheckIndex.Status.
    /// This method should only be called after CheckIndex has been run and found issues.
    /// WARNING: This will remove bad segments and may result in document loss!
    /// </summary>
    /// <param name="status">The CheckIndex.Status from a previous check</param>
    /// <param name="logWriter">TextWriter for logging progress</param>
    /// <exception cref="InvalidOperationException">Thrown when index is opened in read-only mode</exception>
    public void RepairIndex(CheckIndex.Status status, TextWriter? logWriter)
    {
        if (_isReadOnly)
        {
            throw new InvalidOperationException("Cannot repair index when opened in read-only mode.");
        }

        if (status == null)
        {
            throw new ArgumentNullException(nameof(status), "Status cannot be null.");
        }

        var checkIndex = new CheckIndex(_directory);

        if (logWriter != null)
        {
            checkIndex.InfoStream = logWriter;
        }

        // FixIndex repairs the index by removing bad segments
        checkIndex.FixIndex(status);
    }

    /// <summary>
    /// Exports all terms from the specified field to a file.
    /// Output format: term{delimiter}docFrequency per line
    /// </summary>
    /// <param name="destDir">Destination directory for the export file</param>
    /// <param name="field">Field name to export terms from</param>
    /// <param name="delimiter">Delimiter to separate term and doc frequency</param>
    /// <returns>Full path to the created export file</returns>
    /// <exception cref="ArgumentNullException">Thrown when reader is not available</exception>
    /// <exception cref="InvalidOperationException">Thrown when field has no terms</exception>
    public string ExportTerms(string destDir, string field, string delimiter)
    {
        if (_reader == null)
        {
            throw new InvalidOperationException("IndexReader is required for exporting terms. Use the constructor that accepts an IndexReader.");
        }

        if (string.IsNullOrEmpty(destDir))
        {
            throw new ArgumentNullException(nameof(destDir));
        }

        if (string.IsNullOrEmpty(field))
        {
            throw new ArgumentNullException(nameof(field));
        }

        if (!Directory.Exists(destDir))
        {
            throw new DirectoryNotFoundException($"Destination directory does not exist: {destDir}");
        }

        // Generate filename with timestamp
        string filename = $"terms_{field}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.out";
        string fullPath = Path.Combine(destDir, filename);

        // Get terms for the field
        var terms = MultiFields.GetTerms(_reader, field);
        if (terms == null)
        {
            throw new InvalidOperationException($"Field '{field}' does not contain any terms to be exported");
        }

        // Write terms to file
        using var writer = new StreamWriter(fullPath, false, Encoding.UTF8);
        // ReSharper disable once GenericEnumeratorNotDisposed
        var termsEnum = terms.GetEnumerator();
        while (termsEnum.MoveNext())
        {
            var term = termsEnum.Term.Utf8ToString();
            var docFreq = termsEnum.DocFreq;
            writer.WriteLine($"{term}{delimiter}{docFreq}");
        }

        return fullPath;
    }

    /// <summary>
    /// Gets all field names from the index
    /// </summary>
    /// <returns>List of field names</returns>
    public List<string> GetFieldNames()
    {
        if (_reader == null)
        {
            throw new InvalidOperationException("IndexReader is required. Use the constructor that accepts an IndexReader.");
        }

        var fields = new List<string>();
        var fieldInfos = MultiFields.GetIndexedFields(_reader);

        foreach (var field in fieldInfos)
        {
            fields.Add(field);
        }

        fields.Sort();
        return fields;
    }

    public void Dispose()
    {
        // Nothing to dispose - directory and reader are managed externally
    }
}

/// <summary>
/// Custom InfoStream implementation that writes to a TextWriter
/// </summary>
internal class TextWriterInfoStream(TextWriter writer) : Lucene.Net.Util.InfoStream
{
    private readonly TextWriter _writer = writer ?? throw new ArgumentNullException(nameof(writer));

    public override void Message(string component, string message)
    {
        _writer.WriteLine($"[{component}] {message}");
        _writer.Flush();
    }

    public override bool IsEnabled(string component)
    {
        // Enable all components
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _writer.Flush();
        }
        base.Dispose(disposing);
    }
}
