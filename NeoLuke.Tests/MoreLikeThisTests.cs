using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NeoLuke.Models.Search;

namespace NeoLuke.Tests;

public class MoreLikeThisTests : IDisposable
{
    private readonly RAMDirectory _directory;
    private readonly IndexReader _reader;
    private readonly MoreLikeThisModel _model;

    public MoreLikeThisTests()
    {
        // Create a test index with sample documents
        _directory = TestIndexGenerator.CreateSampleIndex();
        _reader = DirectoryReader.Open(_directory);
        _model = new MoreLikeThisModel(_reader);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _reader.Dispose();
        _directory.Dispose();
    }

    [Fact]
    public void CreateMoreLikeThisQuery_WithValidDocId_ReturnsQuery()
    {
        // Arrange
        // Use lenient settings for small test index
        var config = new MoreLikeThisConfig
        {
            MinDocFreq = 1,
            MinTermFreq = 1
        };
        var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

        // Act
        var query = _model.CreateMoreLikeThisQuery(0, config, analyzer);

        // Assert
        Assert.NotNull(query);
        Assert.NotEmpty(query.ToString());
    }

    [Fact]
    public void CreateMoreLikeThisQuery_WithCustomConfig_UsesConfigValues()
    {
        // Arrange
        var config = new MoreLikeThisConfig
        {
            MinTermFreq = 1,
            MinDocFreq = 1,
            MaxQueryTerms = 10
        };
        var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

        // Act
        var query = _model.CreateMoreLikeThisQuery(0, config, analyzer);

        // Assert
        Assert.NotNull(query);
        // Query should contain terms from the document
        var queryString = query.ToString();
        Assert.NotEmpty(queryString);
    }

    [Fact]
    public void CreateMoreLikeThisQuery_WithFieldNames_UsesSpecifiedFields()
    {
        // Arrange
        var config = new MoreLikeThisConfig
        {
            FieldNames = ["content"],
            MinTermFreq = 1,
            MinDocFreq = 1
        };
        var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

        // Act
        var query = _model.CreateMoreLikeThisQuery(0, config, analyzer);

        // Assert
        Assert.NotNull(query);
        var queryString = query.ToString();
        Assert.Contains("content:", queryString);
    }

    [Fact]
    public void CreateMoreLikeThisQuery_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _model.CreateMoreLikeThisQuery(0, null!, analyzer));
    }

    [Fact]
    public void CreateMoreLikeThisQuery_WithNullAnalyzer_ThrowsArgumentNullException()
    {
        // Arrange
        var config = new MoreLikeThisConfig();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _model.CreateMoreLikeThisQuery(0, config, null!));
    }

    [Fact]
    public void GetFieldNames_ReturnsAllFields()
    {
        // Act
        var fields = _model.GetFieldNames();

        // Assert
        Assert.NotNull(fields);
        Assert.NotEmpty(fields);
        Assert.Contains("id", fields);
        Assert.Contains("title", fields);
        Assert.Contains("content", fields);
        Assert.Contains("category", fields);
        Assert.Contains("author", fields);
    }

    [Fact]
    public void MoreLikeThisConfig_DefaultValues_AreSet()
    {
        // Arrange & Act
        var config = new MoreLikeThisConfig();

        // Assert
        Assert.Equal(5, config.MinDocFreq);
        Assert.Equal(int.MaxValue, config.MaxDocFreq);
        Assert.Equal(2, config.MinTermFreq);
        Assert.Equal(0, config.MinWordLen);
        Assert.Equal(0, config.MaxWordLen);
        Assert.Equal(25, config.MaxQueryTerms);
        Assert.False(config.Boost);
        Assert.Equal(1.0f, config.BoostFactor);
        Assert.NotNull(config.FieldNames);
        Assert.Empty(config.FieldNames);
    }

    [Fact]
    public void MoreLikeThisConfig_Clone_CreatesDeepCopy()
    {
        // Arrange
        var original = new MoreLikeThisConfig
        {
            MinDocFreq = 10,
            MaxDocFreq = 100,
            MinTermFreq = 3,
            MinWordLen = 2,
            MaxWordLen = 50,
            MaxQueryTerms = 15,
            Boost = true,
            BoostFactor = 2.0f,
            FieldNames = ["field1", "field2"]
        };

        // Act
        var clone = original.Clone();

        // Assert
        Assert.NotSame(original, clone);
        Assert.Equal(original.MinDocFreq, clone.MinDocFreq);
        Assert.Equal(original.MaxDocFreq, clone.MaxDocFreq);
        Assert.Equal(original.MinTermFreq, clone.MinTermFreq);
        Assert.Equal(original.MinWordLen, clone.MinWordLen);
        Assert.Equal(original.MaxWordLen, clone.MaxWordLen);
        Assert.Equal(original.MaxQueryTerms, clone.MaxQueryTerms);
        Assert.Equal(original.Boost, clone.Boost);
        Assert.Equal(original.BoostFactor, clone.BoostFactor);
        Assert.NotSame(original.FieldNames, clone.FieldNames);
        Assert.Equal(original.FieldNames.Count, clone.FieldNames.Count);
    }

    [Fact]
    public void MoreLikeThis_FindsSimilarDocuments()
    {
        // Arrange
        var searchModel = new SearchModel(_reader);
        var config = new MoreLikeThisConfig
        {
            MinTermFreq = 1,
            MinDocFreq = 1,
            MaxQueryTerms = 50
        };
        var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

        // Act - Find documents similar to doc 0 (AI article)
        var query = _model.CreateMoreLikeThisQuery(0, config, analyzer);
        var results = searchModel.ExecuteSearch(query, 10);

        // Assert
        Assert.NotNull(results);
        Assert.True(results.TotalHits > 0, "Should find at least one similar document");

        // Document 1 (Machine Learning) should be in the results as it's similar to doc 0 (AI)
        var resultDocIds = results.Results.Select(r => r.DocId).ToList();

        // The original document might be in results, and doc 1 should be similar
        Assert.Contains(resultDocIds, id => id == 1 || id == 2 || id == 4);
    }

    [Fact]
    public void MoreLikeThis_WithFieldSelection_UsesOnlySelectedFields()
    {
        // Arrange
        var config = new MoreLikeThisConfig
        {
            FieldNames = ["title"],
            MinTermFreq = 1,
            MinDocFreq = 1,
            MaxQueryTerms = 50
        };
        var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

        // Act
        var query = _model.CreateMoreLikeThisQuery(0, config, analyzer);
        var queryString = query.ToString();

        // Assert
        Assert.NotEmpty(queryString);
        Assert.Contains("title:", queryString);
    }
}
