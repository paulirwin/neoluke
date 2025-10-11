using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Util;
using NeoLuke.Models.Documents;
using LuceneDirectory = Lucene.Net.Store.Directory;
using LuceneField = Lucene.Net.Documents.Field;
using LuceneDocument = Lucene.Net.Documents.Document;

namespace NeoLuke.Tests.Models;

public class DocumentsModelTests : IDisposable
{
    private readonly string _testIndexPath;
    private readonly LuceneDirectory _directory;
    private IndexReader _reader;

    public DocumentsModelTests()
    {
        // Create a unique temporary directory for each test
        _testIndexPath = Path.Combine(Path.GetTempPath(), $"lucene_test_{Guid.NewGuid()}");
        System.IO.Directory.CreateDirectory(_testIndexPath);

        // Create a test index
        _directory = Lucene.Net.Store.FSDirectory.Open(_testIndexPath);
        CreateTestIndex(_directory);

        // Open reader for tests
        _reader = DirectoryReader.Open(_directory);
    }

    private static void CreateTestIndex(LuceneDirectory directory)
    {
        var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, new StandardAnalyzer(LuceneVersion.LUCENE_48));
        using var writer = new IndexWriter(directory, config);

        // Add test documents with various field types
        writer.AddDocument(new LuceneDocument
        {
            new Lucene.Net.Documents.StringField("id", "1", LuceneField.Store.YES),
            new Lucene.Net.Documents.TextField("title", "First document", LuceneField.Store.YES),
            new Lucene.Net.Documents.TextField("content", "This is the first test document", LuceneField.Store.YES),
            new Lucene.Net.Documents.Int32Field("count", 42, LuceneField.Store.YES),
            new Lucene.Net.Documents.SingleField("score", 3.14f, LuceneField.Store.YES)
        });

        writer.AddDocument(new LuceneDocument
        {
            new Lucene.Net.Documents.StringField("id", "2", LuceneField.Store.YES),
            new Lucene.Net.Documents.TextField("title", "Second document", LuceneField.Store.YES),
            new Lucene.Net.Documents.TextField("content", "This is the second test document", LuceneField.Store.YES),
            new Lucene.Net.Documents.Int64Field("bigcount", 1000000L, LuceneField.Store.YES),
            new Lucene.Net.Documents.DoubleField("bigscore", 2.718281828, LuceneField.Store.YES)
        });

        writer.AddDocument(new LuceneDocument
        {
            new Lucene.Net.Documents.StringField("id", "3", LuceneField.Store.YES),
            new Lucene.Net.Documents.TextField("title", "Third document", LuceneField.Store.YES),
            new Lucene.Net.Documents.TextField("content", "This is the third test document", LuceneField.Store.YES)
        });

        writer.Commit();
    }

    private void ReopenReader()
    {
        var oldReader = _reader;
        _reader = DirectoryReader.Open(_directory);
        oldReader.Dispose();
    }

    [Fact]
    public void Constructor_WithValidParameters_InitializesSuccessfully()
    {
        // Arrange & Act
        using var model = new DocumentsModel(_reader, _directory, true);

        // Assert
        Assert.True(model.IsReadOnly);
        Assert.Equal(3, model.GetNumDocs());
    }

    [Fact]
    public void Constructor_WithNullReader_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DocumentsModel(null!, _directory, true));
    }

    [Fact]
    public void Constructor_WithNullDirectory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DocumentsModel(_reader, null!, true));
    }

    [Fact]
    public void GetMaxDoc_ReturnsCorrectValue()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, true);

        // Act
        var maxDoc = model.GetMaxDoc();

        // Assert
        Assert.Equal(3, maxDoc);
    }

    [Fact]
    public void GetNumDocs_ReturnsCorrectValue()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, true);

        // Act
        var numDocs = model.GetNumDocs();

        // Assert
        Assert.Equal(3, numDocs);
    }

    [Fact]
    public void GetDocument_WithValidDocId_ReturnsDocumentFields()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, true);

        // Act
        var fields = model.GetDocument(0);

        // Assert
        Assert.NotEmpty(fields);
        Assert.Contains(fields, f => f.FieldName == "id");
        Assert.Contains(fields, f => f.FieldName == "title");
        Assert.Contains(fields, f => f.FieldName == "content");
        Assert.False(fields[0].IsDeleted);
    }

    [Fact]
    public void GetDocument_ReturnsFieldsInAlphabeticalOrder()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, true);

        // Act
        var fields = model.GetDocument(0);

        // Assert
        var fieldNames = fields.Select(f => f.FieldName).ToList();
        var sortedNames = fieldNames.OrderBy(n => n).ToList();
        Assert.Equal(sortedNames, fieldNames);
    }

    [Fact]
    public void GetDocument_WithNegativeDocId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, true);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => model.GetDocument(-1));
    }

    [Fact]
    public void GetDocument_WithDocIdTooHigh_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, true);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => model.GetDocument(999));
    }

    [Fact]
    public void IsValidDocId_WithValidId_ReturnsTrue()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, true);

        // Act & Assert
        Assert.True(model.IsValidDocId(0));
        Assert.True(model.IsValidDocId(1));
        Assert.True(model.IsValidDocId(2));
    }

    [Fact]
    public void IsValidDocId_WithNegativeId_ReturnsFalse()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, true);

        // Act & Assert
        Assert.False(model.IsValidDocId(-1));
    }

    [Fact]
    public void IsValidDocId_WithIdTooHigh_ReturnsFalse()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, true);

        // Act & Assert
        Assert.False(model.IsValidDocId(999));
    }

    [Fact]
    public void FindFirstNonDeletedDocument_WithAllLiveDocs_ReturnsZero()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, true);

        // Act
        var firstDoc = model.FindFirstNonDeletedDocument();

        // Assert
        Assert.NotNull(firstDoc);
        Assert.Equal(0, firstDoc.Value);
    }

    [Fact]
    public void AddDocument_InReadOnlyMode_ThrowsInvalidOperationException()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, true);
        var fields = new List<NewField>
        {
            new() { Name = "title", Value = "Test", FieldType = FieldType.TextField, IsStored = true }
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => model.AddDocument(fields));
    }

    [Fact]
    public void AddDocument_WithNullFields_ThrowsArgumentException()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => model.AddDocument(null!));
    }

    [Fact]
    public void AddDocument_WithEmptyFields_ThrowsArgumentException()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => model.AddDocument(new List<NewField>()));
    }

    [Fact]
    public void AddDocument_WithOnlyEmptyFieldValues_ThrowsArgumentException()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);
        var fields = new List<NewField>
        {
            new() { Name = "", Value = "", FieldType = FieldType.TextField, IsStored = true }
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => model.AddDocument(fields));
    }

    [Fact]
    public void AddDocument_WithValidTextFields_AddsSuccessfully()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);
        var fields = new List<NewField>
        {
            new() { Name = "id", Value = "4", FieldType = FieldType.StringField, IsStored = true },
            new() { Name = "title", Value = "New document", FieldType = FieldType.TextField, IsStored = true }
        };

        // Act
        model.AddDocument(fields);
        ReopenReader();
        using var modelAfter = new DocumentsModel(_reader, _directory, true);

        // Assert
        Assert.Equal(4, modelAfter.GetNumDocs());
    }

    [Fact]
    public void AddDocument_WithInt32Field_AddsSuccessfully()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);
        var fields = new List<NewField>
        {
            new() { Name = "id", Value = "5", FieldType = FieldType.StringField, IsStored = true },
            new() { Name = "count", Value = "123", FieldType = FieldType.Int32Field, IsStored = true }
        };

        // Act
        model.AddDocument(fields);
        ReopenReader();
        using var modelAfter = new DocumentsModel(_reader, _directory, true);

        // Assert
        Assert.Equal(4, modelAfter.GetNumDocs());
    }

    [Fact]
    public void AddDocument_WithInt64Field_AddsSuccessfully()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);
        var fields = new List<NewField>
        {
            new() { Name = "id", Value = "6", FieldType = FieldType.StringField, IsStored = true },
            new() { Name = "bigcount", Value = "9999999999", FieldType = FieldType.Int64Field, IsStored = true }
        };

        // Act
        model.AddDocument(fields);
        ReopenReader();
        using var modelAfter = new DocumentsModel(_reader, _directory, true);

        // Assert
        Assert.Equal(4, modelAfter.GetNumDocs());
    }

    [Fact]
    public void AddDocument_WithSingleField_AddsSuccessfully()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);
        var fields = new List<NewField>
        {
            new() { Name = "id", Value = "7", FieldType = FieldType.StringField, IsStored = true },
            new() { Name = "score", Value = "3.14", FieldType = FieldType.SingleField, IsStored = true }
        };

        // Act
        model.AddDocument(fields);
        ReopenReader();
        using var modelAfter = new DocumentsModel(_reader, _directory, true);

        // Assert
        Assert.Equal(4, modelAfter.GetNumDocs());
    }

    [Fact]
    public void AddDocument_WithDoubleField_AddsSuccessfully()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);
        var fields = new List<NewField>
        {
            new() { Name = "id", Value = "8", FieldType = FieldType.StringField, IsStored = true },
            new() { Name = "bigscore", Value = "2.718281828", FieldType = FieldType.DoubleField, IsStored = true }
        };

        // Act
        model.AddDocument(fields);
        ReopenReader();
        using var modelAfter = new DocumentsModel(_reader, _directory, true);

        // Assert
        Assert.Equal(4, modelAfter.GetNumDocs());
    }

    [Fact]
    public void AddDocument_WithInvalidInt32Value_ThrowsArgumentException()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);
        var fields = new List<NewField>
        {
            new() { Name = "count", Value = "not_a_number", FieldType = FieldType.Int32Field, IsStored = true }
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => model.AddDocument(fields));
    }

    [Fact]
    public void AddDocument_WithInvalidInt64Value_ThrowsArgumentException()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);
        var fields = new List<NewField>
        {
            new() { Name = "bigcount", Value = "not_a_number", FieldType = FieldType.Int64Field, IsStored = true }
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => model.AddDocument(fields));
    }

    [Fact]
    public void AddDocument_WithInvalidFloatValue_ThrowsArgumentException()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);
        var fields = new List<NewField>
        {
            new() { Name = "score", Value = "not_a_number", FieldType = FieldType.SingleField, IsStored = true }
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => model.AddDocument(fields));
    }

    [Fact]
    public void AddDocument_WithInvalidDoubleValue_ThrowsArgumentException()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);
        var fields = new List<NewField>
        {
            new() { Name = "bigscore", Value = "not_a_number", FieldType = FieldType.DoubleField, IsStored = true }
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => model.AddDocument(fields));
    }

    [Fact]
    public void AddDocument_FiltersOutEmptyFields()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);
        var fields = new List<NewField>
        {
            new() { Name = "id", Value = "9", FieldType = FieldType.StringField, IsStored = true },
            new() { Name = "", Value = "empty name", FieldType = FieldType.TextField, IsStored = true },
            new() { Name = "title", Value = "", FieldType = FieldType.TextField, IsStored = true },
            new() { Name = "valid", Value = "value", FieldType = FieldType.TextField, IsStored = true }
        };

        // Act - Should not throw, empty fields should be filtered out
        model.AddDocument(fields);
        ReopenReader();
        using var modelAfter = new DocumentsModel(_reader, _directory, true);

        // Assert
        Assert.Equal(4, modelAfter.GetNumDocs());
    }

    [Fact]
    public void DeleteDocument_InReadOnlyMode_ThrowsInvalidOperationException()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, true);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => model.DeleteDocument(0));
    }

    [Fact]
    public void DeleteDocument_WithNegativeDocId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => model.DeleteDocument(-1));
    }

    [Fact]
    public void DeleteDocument_WithDocIdTooHigh_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => model.DeleteDocument(999));
    }

    [Fact]
    public void DeleteDocument_WithValidDocId_DeletesSuccessfully()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);
        var numDocsBefore = model.GetNumDocs();

        // Act
        model.DeleteDocument(0);
        ReopenReader();
        using var modelAfter = new DocumentsModel(_reader, _directory, true);

        // Assert
        Assert.Equal(numDocsBefore - 1, modelAfter.GetNumDocs());
    }

    [Fact]
    public void DeleteDocument_ThenGetDocument_ReturnsDeletedMarker()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);
        model.DeleteDocument(0);
        ReopenReader();
        using var modelAfter = new DocumentsModel(_reader, _directory, true);

        // Act
        var fields = modelAfter.GetDocument(0);

        // Assert
        Assert.Single(fields);
        Assert.Equal("(deleted)", fields[0].FieldName);
        Assert.True(fields[0].IsDeleted);
    }

    [Fact]
    public void DeleteDocument_AlreadyDeleted_ThrowsInvalidOperationException()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);
        model.DeleteDocument(0);
        ReopenReader();
        using var modelAfter = new DocumentsModel(_reader, _directory, false);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => modelAfter.DeleteDocument(0));
    }

    [Fact]
    public void IsValidDocId_WithDeletedDoc_ReturnsFalse()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);
        model.DeleteDocument(0);
        ReopenReader();
        using var modelAfter = new DocumentsModel(_reader, _directory, true);

        // Act & Assert
        Assert.False(modelAfter.IsValidDocId(0));
    }

    [Fact]
    public void FindFirstNonDeletedDocument_AfterDeletingFirst_ReturnsSecondDoc()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);
        model.DeleteDocument(0);
        ReopenReader();
        using var modelAfter = new DocumentsModel(_reader, _directory, true);

        // Act
        var firstDoc = modelAfter.FindFirstNonDeletedDocument();

        // Assert
        Assert.NotNull(firstDoc);
        Assert.Equal(1, firstDoc.Value);
    }

    [Fact]
    public void AddDocument_WithStoredFieldOnly_AddsSuccessfully()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);
        var fields = new List<NewField>
        {
            new() { Name = "id", Value = "10", FieldType = FieldType.StringField, IsStored = true },
            new() { Name = "metadata", Value = "stored only", FieldType = FieldType.StoredField, IsStored = true }
        };

        // Act
        model.AddDocument(fields);
        ReopenReader();
        using var modelAfter = new DocumentsModel(_reader, _directory, true);

        // Assert
        Assert.Equal(4, modelAfter.GetNumDocs());
    }

    [Fact]
    public void AddDocument_WithNotStoredField_AddsSuccessfully()
    {
        // Arrange
        using var model = new DocumentsModel(_reader, _directory, false);
        var fields = new List<NewField>
        {
            new() { Name = "id", Value = "11", FieldType = FieldType.StringField, IsStored = true },
            new() { Name = "indexed", Value = "indexed but not stored", FieldType = FieldType.TextField, IsStored = false }
        };

        // Act
        model.AddDocument(fields);
        ReopenReader();
        using var modelAfter = new DocumentsModel(_reader, _directory, true);

        // Assert
        Assert.Equal(4, modelAfter.GetNumDocs());
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
