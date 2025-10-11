using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Microsoft.Extensions.Logging;
using LuceneDirectory = Lucene.Net.Store.Directory;

namespace NeoLuke.Services;

/// <summary>
/// Represents information about an opened index
/// </summary>
public record IndexInfo(
    string Path,
    bool IsReadOnly,
    Type? DirectoryType,
    IndexReader Reader,
    LuceneDirectory Directory);

/// <summary>
/// Event published when an index is successfully opened
/// </summary>
public record IndexOpenedEvent(IndexInfo Info);

/// <summary>
/// Event published when an index is closed
/// </summary>
public record IndexClosedEvent;

/// <summary>
/// Service for managing Lucene.NET index lifecycle
/// </summary>
public interface IIndexService : IDisposable
{
    /// <summary>
    /// Gets whether an index is currently open
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Gets whether the current index is opened in read-only mode
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Gets the current index path, or null if no index is open
    /// </summary>
    string? CurrentPath { get; }

    /// <summary>
    /// Gets the current IndexReader, or null if no index is open
    /// </summary>
    IndexReader? CurrentReader { get; }

    /// <summary>
    /// Gets the current Directory, or null if no index is open
    /// </summary>
    LuceneDirectory? CurrentDirectory { get; }

    /// <summary>
    /// Gets the current directory type, or null if no index is open or default type is used
    /// </summary>
    Type? CurrentDirectoryType { get; }

    /// <summary>
    /// Observable stream of IndexOpened events
    /// </summary>
    IObservable<IndexOpenedEvent> IndexOpened { get; }

    /// <summary>
    /// Observable stream of IndexClosed events
    /// </summary>
    IObservable<IndexClosedEvent> IndexClosed { get; }

    /// <summary>
    /// Opens a Lucene index at the specified path
    /// </summary>
    /// <param name="path">Path to the index directory</param>
    /// <param name="directoryType">Optional directory implementation type (e.g., SimpleFSDirectory, MMapDirectory)</param>
    /// <param name="readOnly">Whether to open the index in read-only mode</param>
    /// <returns>Information about the opened index</returns>
    Task<IndexInfo> OpenAsync(string path, Type? directoryType, bool readOnly);

    /// <summary>
    /// Closes the currently open index
    /// </summary>
    void Close();

    /// <summary>
    /// Reopens the current index (useful for seeing recent changes)
    /// </summary>
    /// <returns>Information about the reopened index</returns>
    Task<IndexInfo> ReopenAsync();

    /// <summary>
    /// Reopens the current index with toggled read-only mode
    /// </summary>
    /// <returns>Information about the reopened index</returns>
    Task<IndexInfo> ReopenWithToggledModeAsync();
}

/// <summary>
/// Default implementation of IIndexService
/// </summary>
public class IndexService : IIndexService
{
    private readonly ILogger<IndexService> _logger;
    private readonly Subject<IndexOpenedEvent> _indexOpenedSubject = new();
    private readonly Subject<IndexClosedEvent> _indexClosedSubject = new();

    private IndexReader? _indexReader;
    private LuceneDirectory? _directory;
    private string? _currentPath;
    private Type? _currentDirectoryType;
    private bool _isReadOnly = true;

    public IndexService(ILogger<IndexService> logger)
    {
        _logger = logger;
    }

    public bool IsOpen => _indexReader != null && _directory != null;

    public bool IsReadOnly => _isReadOnly;

    public string? CurrentPath => _currentPath;

    public IndexReader? CurrentReader => _indexReader;

    public LuceneDirectory? CurrentDirectory => _directory;

    public Type? CurrentDirectoryType => _currentDirectoryType;

    public IObservable<IndexOpenedEvent> IndexOpened => _indexOpenedSubject.AsObservable();

    public IObservable<IndexClosedEvent> IndexClosed => _indexClosedSubject.AsObservable();

    public async Task<IndexInfo> OpenAsync(string path, Type? directoryType, bool readOnly)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Index path cannot be null or empty", nameof(path));
        }

        _logger.LogInformation("Opening index at path: {IndexPath} (ReadOnly: {ReadOnly})", path, readOnly);
        _logger.LogDebug("Directory type: {DirectoryType}", directoryType?.FullName ?? "default");

        // Close existing index if any
        Close();

        // Open the index in a background task
        await Task.Run(() =>
        {
            _logger.LogDebug("Opening directory and index reader...");

            // Open directory
            _directory = directoryType != null
                ? (LuceneDirectory)Activator.CreateInstance(directoryType, path)!
                : FSDirectory.Open(path);

            // Open index reader
            _indexReader = DirectoryReader.Open(_directory);
        });

        if (_indexReader == null || _directory == null)
        {
            throw new InvalidOperationException("Failed to open index: reader or directory is null");
        }

        _logger.LogInformation("Index opened successfully. Documents: {DocCount}", _indexReader.NumDocs);

        // Store current index parameters
        _currentPath = path;
        _currentDirectoryType = directoryType;
        _isReadOnly = readOnly;

        // Create index info and notify subscribers
        var indexInfo = new IndexInfo(path, readOnly, directoryType, _indexReader, _directory);
        _indexOpenedSubject.OnNext(new IndexOpenedEvent(indexInfo));

        return indexInfo;
    }

    public void Close()
    {
        if (_indexReader != null || _directory != null)
        {
            _logger.LogInformation("Closing index");

            _indexReader?.Dispose();
            _indexReader = null;

            _directory?.Dispose();
            _directory = null;

            // Note: We don't clear _currentPath and _currentDirectoryType here
            // so that "Reopen current index" continues to work after closing

            _indexClosedSubject.OnNext(new IndexClosedEvent());

            _logger.LogInformation("Index closed successfully");
        }
    }

    public async Task<IndexInfo> ReopenAsync()
    {
        if (string.IsNullOrEmpty(_currentPath))
        {
            throw new InvalidOperationException("No index to reopen - no previous index path stored");
        }

        _logger.LogInformation("Reopening current index: {IndexPath}", _currentPath);
        return await OpenAsync(_currentPath, _currentDirectoryType, _isReadOnly);
    }

    public async Task<IndexInfo> ReopenWithToggledModeAsync()
    {
        if (string.IsNullOrEmpty(_currentPath))
        {
            throw new InvalidOperationException("No index to reopen - no previous index path stored");
        }

        // Toggle the read-only mode
        var newReadOnlyMode = !_isReadOnly;
        _logger.LogInformation("Reopening current index in {Mode} mode: {IndexPath}",
            newReadOnlyMode ? "read-only" : "read/write", _currentPath);

        return await OpenAsync(_currentPath, _currentDirectoryType, newReadOnlyMode);
    }

    public void Dispose()
    {
        Close();
        _indexOpenedSubject.Dispose();
        _indexClosedSubject.Dispose();
    }
}
