using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Util;
using NeoLuke.Models.Overview;
using LuceneDirectory = Lucene.Net.Store.Directory;
using LuceneField = Lucene.Net.Documents.Field;
using LuceneDocument = Lucene.Net.Documents.Document;

namespace NeoLuke.Tests.Models;

public class OverviewModelTests : IDisposable
{
    private readonly string _testIndexPath;
    private readonly LuceneDirectory _directory;
    private readonly IndexReader _reader;

    public OverviewModelTests()
    {
        // Create a unique temporary directory for each test
        _testIndexPath = Path.Combine(Path.GetTempPath(), $"lucene_test_{Guid.NewGuid()}");
        System.IO.Directory.CreateDirectory(_testIndexPath);

        // Create a test index with various content
        _directory = Lucene.Net.Store.FSDirectory.Open(_testIndexPath);
        CreateTestIndex(_directory);

        // Open reader for tests
        _reader = DirectoryReader.Open(_directory);
    }

    private static void CreateTestIndex(LuceneDirectory directory)
    {
        var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, new StandardAnalyzer(LuceneVersion.LUCENE_48));
        using var writer = new IndexWriter(directory, config);

        // Add test documents with various fields
        for (int i = 0; i < 10; i++)
        {
            writer.AddDocument(new LuceneDocument
            {
                new Lucene.Net.Documents.StringField("id", i.ToString(), LuceneField.Store.YES),
                new Lucene.Net.Documents.TextField("title", $"Document {i}", LuceneField.Store.YES),
                new Lucene.Net.Documents.TextField("content", $"This is test content for document {i}. The quick brown fox jumps over the lazy dog.", LuceneField.Store.YES),
                new Lucene.Net.Documents.StringField("category", i % 3 == 0 ? "tech" : "general", LuceneField.Store.YES),
                new Lucene.Net.Documents.Int32Field("count", i * 10, LuceneField.Store.YES)
            });
        }

        writer.Commit();
    }

    [Fact]
    public void Constructor_WithValidParameters_InitializesSuccessfully()
    {
        // Arrange & Act
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Assert
        Assert.NotNull(model);
        Assert.Equal(_testIndexPath, model.GetIndexPath());
    }

    [Fact]
    public void Constructor_WithNullReader_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OverviewModel(null!, _testIndexPath));
    }

    [Fact]
    public void Constructor_WithNullIndexPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OverviewModel(_reader, null!));
    }

    [Fact]
    public void GetIndexPath_ReturnsCorrectPath()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Act
        var path = model.GetIndexPath();

        // Assert
        Assert.Equal(_testIndexPath, path);
    }

    [Fact]
    public void GetNumFields_ReturnsCorrectCount()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Act
        var numFields = model.GetNumFields();

        // Assert
        Assert.True(numFields > 0);
        Assert.True(numFields >= 5); // id, title, content, category, count
    }

    [Fact]
    public void GetNumDocuments_ReturnsCorrectCount()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Act
        var numDocs = model.GetNumDocuments();

        // Assert
        Assert.Equal(10, numDocs);
    }

    [Fact]
    public void GetNumTerms_ReturnsPositiveValue()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Act
        var numTerms = model.GetNumTerms();

        // Assert
        Assert.True(numTerms > 0);
    }

    [Fact]
    public void HasDeletions_WithNoDeletedDocs_ReturnsFalse()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Act
        var hasDeletions = model.HasDeletions();

        // Assert
        Assert.False(hasDeletions);
    }

    [Fact]
    public void GetNumDeletedDocs_WithNoDeletedDocs_ReturnsZero()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Act
        var numDeleted = model.GetNumDeletedDocs();

        // Assert
        Assert.Equal(0, numDeleted);
    }

    [Fact]
    public void IsOptimized_WithSingleSegment_ReturnsTrue()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Act
        var isOptimized = model.IsOptimized();

        // Assert
        Assert.NotNull(isOptimized);
        Assert.True(isOptimized.Value); // Single segment after commit
    }

    [Fact]
    public void GetIndexVersion_ReturnsValue()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Act
        var version = model.GetIndexVersion();

        // Assert
        Assert.NotNull(version);
        Assert.True(version.Value > 0);
    }

    [Fact]
    public void GetIndexFormat_ReturnsValidFormat()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Act
        var format = model.GetIndexFormat();

        // Assert
        Assert.NotNull(format);
        Assert.NotEmpty(format);
        // Should be "Lucene 4.8" or similar
        Assert.Contains("Lucene", format);
    }

    [Fact]
    public void GetDirImpl_ReturnsDirectoryType()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Act
        var dirImpl = model.GetDirImpl();

        // Assert
        Assert.NotNull(dirImpl);
        Assert.Contains("Directory", dirImpl);
    }

    [Fact]
    public void GetDirImpl_WithNullDirectory_ReturnsNull()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, null);

        // Act
        var dirImpl = model.GetDirImpl();

        // Assert
        Assert.Null(dirImpl);
    }

    [Fact]
    public void GetCommitDescription_ReturnsValidDescription()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Act
        var commit = model.GetCommitDescription();

        // Assert
        Assert.NotNull(commit);
        Assert.Contains("generation=", commit);
        Assert.Contains("segs=", commit);
    }

    [Fact]
    public void GetDeletionsOptimizedString_WithNoDeletedDocs_ReturnsCorrectFormat()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Act
        var status = model.GetDeletionsOptimizedString();

        // Assert
        Assert.NotNull(status);
        Assert.Contains("No", status); // No deletions
        Assert.Contains("/", status);
        Assert.Contains("Yes", status); // Is optimized (single segment)
    }

    [Fact]
    public void GetFieldTermCounts_ReturnsFieldStatistics()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Act
        var fieldStats = model.GetFieldTermCounts();

        // Assert
        Assert.NotEmpty(fieldStats);
        Assert.All(fieldStats, stat =>
        {
            Assert.NotNull(stat.FieldName);
            Assert.True(stat.TermCount > 0);
            Assert.True(stat.Percentage >= 0 && stat.Percentage <= 100);
        });
    }

    [Fact]
    public void GetFieldTermCounts_ReturnsSortedByTermCountDescending()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Act
        var fieldStats = model.GetFieldTermCounts();

        // Assert
        Assert.NotEmpty(fieldStats);
        // Verify sorted descending
        for (int i = 0; i < fieldStats.Count - 1; i++)
        {
            Assert.True(fieldStats[i].TermCount >= fieldStats[i + 1].TermCount,
                $"Field stats not sorted: {fieldStats[i].FieldName}({fieldStats[i].TermCount}) < {fieldStats[i + 1].FieldName}({fieldStats[i + 1].TermCount})");
        }
    }

    [Fact]
    public void GetFieldTermCounts_PercentagesAddUpToApproximately100()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Act
        var fieldStats = model.GetFieldTermCounts();

        // Assert
        Assert.NotEmpty(fieldStats);
        var totalPercentage = fieldStats.Sum(f => f.Percentage);
        // Allow small rounding errors
        Assert.True(totalPercentage >= 99.9 && totalPercentage <= 100.1,
            $"Percentages don't add up to 100: {totalPercentage}");
    }

    [Fact]
    public void GetTopTerms_WithValidField_ReturnsTerms()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Act
        var topTerms = model.GetTopTerms("content", 10);

        // Assert
        Assert.NotEmpty(topTerms);
        Assert.All(topTerms, term =>
        {
            Assert.NotNull(term.Term);
            Assert.True(term.Frequency > 0);
        });
    }

    [Fact]
    public void GetTopTerms_ReturnsSortedByFrequencyDescending()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Act
        var topTerms = model.GetTopTerms("content", 10);

        // Assert
        Assert.NotEmpty(topTerms);
        // Verify sorted descending by frequency
        for (int i = 0; i < topTerms.Count - 1; i++)
        {
            Assert.True(topTerms[i].Frequency >= topTerms[i + 1].Frequency,
                $"Terms not sorted: {topTerms[i].Term}({topTerms[i].Frequency}) < {topTerms[i + 1].Term}({topTerms[i + 1].Frequency})");
        }
    }

    [Fact]
    public void GetTopTerms_RespectsMaxTermsLimit()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Act
        var topTerms = model.GetTopTerms("content", 5);

        // Assert
        Assert.True(topTerms.Count <= 5, $"Returned {topTerms.Count} terms, expected max 5");
    }

    [Fact]
    public void GetTopTerms_WithNonExistentField_ReturnsEmptyList()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Act
        var topTerms = model.GetTopTerms("nonexistent_field", 10);

        // Assert
        Assert.Empty(topTerms);
    }

    [Fact]
    public void GetTopTerms_WithZeroMaxTerms_ReturnsEmptyList()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Act
        var topTerms = model.GetTopTerms("content", 0);

        // Assert
        Assert.Empty(topTerms);
    }

    [Fact]
    public void GetFieldTermCounts_WithEmptyIndex_ReturnsEmptyList()
    {
        // Arrange - Create empty index
        var emptyIndexPath = Path.Combine(Path.GetTempPath(), $"lucene_test_empty_{Guid.NewGuid()}");
        System.IO.Directory.CreateDirectory(emptyIndexPath);

        try
        {
            var emptyDir = Lucene.Net.Store.FSDirectory.Open(emptyIndexPath);
            var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, new StandardAnalyzer(LuceneVersion.LUCENE_48));
            using (var writer = new IndexWriter(emptyDir, config))
            {
                writer.Commit(); // Empty commit
            }

            using var emptyReader = DirectoryReader.Open(emptyDir);
            var model = new OverviewModel(emptyReader, emptyIndexPath, emptyDir);

            // Act
            var fieldStats = model.GetFieldTermCounts();

            // Assert
            Assert.Empty(fieldStats);

            emptyDir.Dispose();
        }
        finally
        {
            System.IO.Directory.Delete(emptyIndexPath, true);
        }
    }

    [Fact]
    public void GetNumTerms_WithEmptyIndex_ReturnsZero()
    {
        // Arrange - Create empty index
        var emptyIndexPath = Path.Combine(Path.GetTempPath(), $"lucene_test_empty_{Guid.NewGuid()}");
        System.IO.Directory.CreateDirectory(emptyIndexPath);

        try
        {
            var emptyDir = Lucene.Net.Store.FSDirectory.Open(emptyIndexPath);
            var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, new StandardAnalyzer(LuceneVersion.LUCENE_48));
            using (var writer = new IndexWriter(emptyDir, config))
            {
                writer.Commit(); // Empty commit
            }

            using var emptyReader = DirectoryReader.Open(emptyDir);
            var model = new OverviewModel(emptyReader, emptyIndexPath, emptyDir);

            // Act
            var numTerms = model.GetNumTerms();

            // Assert
            Assert.Equal(0, numTerms);

            emptyDir.Dispose();
        }
        finally
        {
            System.IO.Directory.Delete(emptyIndexPath, true);
        }
    }

    [Fact]
    public void GetCommitUserData_WithNoUserData_ReturnsNull()
    {
        // Arrange
        var model = new OverviewModel(_reader, _testIndexPath, _directory);

        // Act
        var userData = model.GetCommitUserData();

        // Assert
        // Typically null for a simple test index with no user data
        Assert.Null(userData);
    }

    public void Dispose()
    {
        // Cleanup
        _reader?.Dispose();
        _directory?.Dispose();
        if (System.IO.Directory.Exists(_testIndexPath))
        {
            System.IO.Directory.Delete(_testIndexPath, true);
        }
    }
}
