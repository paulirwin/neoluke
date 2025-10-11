using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Microsoft.Extensions.Logging;
using NeoLuke.Services;

namespace NeoLuke.Tests.Services;

public class IndexServiceTests : IDisposable
{
    private readonly string _testIndexPath;
    private readonly ILogger<IndexService> _logger;

    public IndexServiceTests()
    {
        // Create a unique temporary directory for each test
        _testIndexPath = Path.Combine(Path.GetTempPath(), $"lucene_test_{Guid.NewGuid()}");
        System.IO.Directory.CreateDirectory(_testIndexPath);

        // Create a simple logger factory for tests
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<IndexService>();

        // Create a test index
        CreateTestIndex(_testIndexPath);
    }

    private static void CreateTestIndex(string path)
    {
        using var dir = FSDirectory.Open(path);
        var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, new StandardAnalyzer(LuceneVersion.LUCENE_48));
        using var writer = new IndexWriter(dir, config);

        // Add a few test documents
        for (var i = 0; i < 5; i++)
        {
            var doc = new Document
            {
                new StringField("id", i.ToString(), Field.Store.YES),
                new TextField("content", $"test content {i}", Field.Store.YES)
            };
            writer.AddDocument(doc);
        }

        writer.Commit();
    }

    [Fact]
    public void Constructor_InitializesWithDefaultState()
    {
        // Arrange & Act
        using var service = new IndexService(_logger);

        // Assert
        Assert.False(service.IsOpen);
        Assert.True(service.IsReadOnly);
        Assert.Null(service.CurrentPath);
        Assert.Null(service.CurrentReader);
        Assert.Null(service.CurrentDirectory);
        Assert.Null(service.CurrentDirectoryType);
    }

    [Fact]
    public async Task OpenAsync_OpensIndexSuccessfully()
    {
        // Arrange
        using var service = new IndexService(_logger);

        // Act
        var indexInfo = await service.OpenAsync(_testIndexPath, null, true);

        // Assert
        Assert.True(service.IsOpen);
        Assert.Equal(_testIndexPath, service.CurrentPath);
        Assert.NotNull(service.CurrentReader);
        Assert.NotNull(service.CurrentDirectory);
        Assert.True(service.IsReadOnly);
        Assert.Equal(5, service.CurrentReader.NumDocs);

        // Verify IndexInfo
        Assert.Equal(_testIndexPath, indexInfo.Path);
        Assert.True(indexInfo.IsReadOnly);
        Assert.NotNull(indexInfo.Reader);
        Assert.NotNull(indexInfo.Directory);
    }

    [Fact]
    public async Task OpenAsync_WithDirectoryType_UsesSpecifiedType()
    {
        // Arrange
        using var service = new IndexService(_logger);

        // Act
        await service.OpenAsync(_testIndexPath, typeof(SimpleFSDirectory), false);

        // Assert
        Assert.True(service.IsOpen);
        Assert.Equal(typeof(SimpleFSDirectory), service.CurrentDirectoryType);
        Assert.False(service.IsReadOnly);
        Assert.IsType<SimpleFSDirectory>(service.CurrentDirectory);
    }

    [Fact]
    public async Task OpenAsync_WithNullPath_ThrowsArgumentException()
    {
        // Arrange
        using var service = new IndexService(_logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.OpenAsync(null!, null, true));
    }

    [Fact]
    public async Task OpenAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Arrange
        using var service = new IndexService(_logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.OpenAsync(string.Empty, null, true));
    }

    [Fact]
    public async Task OpenAsync_WithInvalidPath_ThrowsException()
    {
        // Arrange
        using var service = new IndexService(_logger);
        var invalidPath = Path.Combine(Path.GetTempPath(), "nonexistent_index_" + Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => service.OpenAsync(invalidPath, null, true));
    }

    [Fact]
    public async Task OpenAsync_PublishesIndexOpenedEvent()
    {
        // Arrange
        using var service = new IndexService(_logger);
        IndexOpenedEvent? receivedEvent = null;
        service.IndexOpened.Subscribe(e => receivedEvent = e);

        // Act
        await service.OpenAsync(_testIndexPath, null, true);

        // Assert
        Assert.NotNull(receivedEvent);
        Assert.Equal(_testIndexPath, receivedEvent.Info.Path);
        Assert.True(receivedEvent.Info.IsReadOnly);
        Assert.NotNull(receivedEvent.Info.Reader);
        Assert.NotNull(receivedEvent.Info.Directory);
    }

    [Fact]
    public async Task OpenAsync_ClosesExistingIndexBeforeOpeningNew()
    {
        // Arrange
        using var service = new IndexService(_logger);
        await service.OpenAsync(_testIndexPath, null, true);
        var firstReader = service.CurrentReader;

        // Create a second test index
        var secondIndexPath = Path.Combine(Path.GetTempPath(), $"lucene_test_{Guid.NewGuid()}");
        System.IO.Directory.CreateDirectory(secondIndexPath);
        CreateTestIndex(secondIndexPath);

        try
        {
            var closedEventReceived = false;
            service.IndexClosed.Subscribe(_ => closedEventReceived = true);

            // Act
            await service.OpenAsync(secondIndexPath, null, false);

            // Assert
            Assert.True(closedEventReceived);
            Assert.Equal(secondIndexPath, service.CurrentPath);
            Assert.False(service.IsReadOnly);
            Assert.NotSame(firstReader, service.CurrentReader);
        }
        finally
        {
            // Cleanup second index
            System.IO.Directory.Delete(secondIndexPath, true);
        }
    }

    [Fact]
    public async Task Close_ClosesIndexAndClearsReader()
    {
        // Arrange
        using var service = new IndexService(_logger);
        await service.OpenAsync(_testIndexPath, null, true);

        // Act
        service.Close();

        // Assert
        Assert.False(service.IsOpen);
        Assert.Null(service.CurrentReader);
        Assert.Null(service.CurrentDirectory);
        // Path and directory type should be preserved for reopening
        Assert.NotNull(service.CurrentPath);
    }

    [Fact]
    public async Task Close_PublishesIndexClosedEvent()
    {
        // Arrange
        using var service = new IndexService(_logger);
        await service.OpenAsync(_testIndexPath, null, true);

        var closedEventReceived = false;
        service.IndexClosed.Subscribe(_ => closedEventReceived = true);

        // Act
        service.Close();

        // Assert
        Assert.True(closedEventReceived);
    }

    [Fact]
    public void Close_WhenNoIndexOpen_DoesNotThrow()
    {
        // Arrange
        using var service = new IndexService(_logger);

        // Act & Assert (should not throw)
        service.Close();
    }

    [Fact]
    public async Task ReopenAsync_ReopensWithSameParameters()
    {
        // Arrange
        using var service = new IndexService(_logger);
        await service.OpenAsync(_testIndexPath, typeof(SimpleFSDirectory), true);
        var firstReader = service.CurrentReader;

        // Act
        await service.ReopenAsync();

        // Assert
        Assert.True(service.IsOpen);
        Assert.Equal(_testIndexPath, service.CurrentPath);
        Assert.Equal(typeof(SimpleFSDirectory), service.CurrentDirectoryType);
        Assert.True(service.IsReadOnly);
        Assert.NotSame(firstReader, service.CurrentReader); // Should be a new reader instance
        Assert.IsType<SimpleFSDirectory>(service.CurrentDirectory);
    }

    [Fact]
    public async Task ReopenAsync_WithoutPreviousOpen_ThrowsInvalidOperationException()
    {
        // Arrange
        using var service = new IndexService(_logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReopenAsync());
    }

    [Fact]
    public async Task ReopenAsync_AfterClose_ReopensSuccessfully()
    {
        // Arrange
        using var service = new IndexService(_logger);
        await service.OpenAsync(_testIndexPath, null, true);
        service.Close();

        // Act
        await service.ReopenAsync();

        // Assert
        Assert.True(service.IsOpen);
        Assert.Equal(_testIndexPath, service.CurrentPath);
        Assert.NotNull(service.CurrentReader);
    }

    [Fact]
    public async Task ReopenWithToggledModeAsync_TogglesReadOnlyMode()
    {
        // Arrange
        using var service = new IndexService(_logger);
        await service.OpenAsync(_testIndexPath, null, true);
        Assert.True(service.IsReadOnly);

        // Act
        await service.ReopenWithToggledModeAsync();

        // Assert
        Assert.False(service.IsReadOnly);
        Assert.Equal(_testIndexPath, service.CurrentPath);
        Assert.NotNull(service.CurrentReader);
    }

    [Fact]
    public async Task ReopenWithToggledModeAsync_TogglesBackAndForth()
    {
        // Arrange
        using var service = new IndexService(_logger);
        await service.OpenAsync(_testIndexPath, null, true);

        // Act & Assert - Toggle to read/write
        await service.ReopenWithToggledModeAsync();
        Assert.False(service.IsReadOnly);

        // Act & Assert - Toggle back to read-only
        await service.ReopenWithToggledModeAsync();
        Assert.True(service.IsReadOnly);
    }

    [Fact]
    public async Task ReopenWithToggledModeAsync_WithoutPreviousOpen_ThrowsInvalidOperationException()
    {
        // Arrange
        using var service = new IndexService(_logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReopenWithToggledModeAsync());
    }

    [Fact]
    public async Task Dispose_ClosesIndexAndDisposesObservables()
    {
        // Arrange
        var service = new IndexService(_logger);
        await service.OpenAsync(_testIndexPath, null, true);

        var closedEventReceived = false;
        service.IndexClosed.Subscribe(_ => closedEventReceived = true);

        // Act
        service.Dispose();

        // Assert
        Assert.False(service.IsOpen);
        Assert.True(closedEventReceived);
    }

    [Fact]
    public async Task MultipleSubscribers_AllReceiveEvents()
    {
        // Arrange
        using var service = new IndexService(_logger);
        var subscriber1Received = false;
        var subscriber2Received = false;

        service.IndexOpened.Subscribe(_ => subscriber1Received = true);
        service.IndexOpened.Subscribe(_ => subscriber2Received = true);

        // Act
        await service.OpenAsync(_testIndexPath, null, true);

        // Assert
        Assert.True(subscriber1Received);
        Assert.True(subscriber2Received);
    }

    public void Dispose()
    {
        // Cleanup test index
        if (System.IO.Directory.Exists(_testIndexPath))
        {
            System.IO.Directory.Delete(_testIndexPath, true);
        }
    }
}
