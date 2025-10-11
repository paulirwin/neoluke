using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NeoLuke.Models.Search;

namespace NeoLuke.Tests.Models;

public class SearchModelTests : IDisposable
{
    private readonly string _testIndexPath;
    private readonly IndexReader _reader;

    public SearchModelTests()
    {
        // Create a unique temporary directory for each test
        _testIndexPath = Path.Combine(Path.GetTempPath(), $"lucene_test_{Guid.NewGuid()}");
        System.IO.Directory.CreateDirectory(_testIndexPath);

        // Create a test index with various content
        CreateTestIndex(_testIndexPath);

        // Open reader for tests
        var dir = FSDirectory.Open(_testIndexPath);
        _reader = DirectoryReader.Open(dir);
    }

    private static void CreateTestIndex(string path)
    {
        using var dir = FSDirectory.Open(path);
        var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, new StandardAnalyzer(LuceneVersion.LUCENE_48));
        using var writer = new IndexWriter(dir, config);

        // Add test documents with various content
        writer.AddDocument(new Document
        {
            new StringField("id", "1", Field.Store.YES),
            new TextField("title", "Quick brown fox", Field.Store.YES),
            new TextField("content", "The quick brown fox jumps over the lazy dog", Field.Store.YES),
            new StringField("category", "animals", Field.Store.YES)
        });

        writer.AddDocument(new Document
        {
            new StringField("id", "2", Field.Store.YES),
            new TextField("title", "Lazy dog", Field.Store.YES),
            new TextField("content", "The lazy dog sleeps all day", Field.Store.YES),
            new StringField("category", "animals", Field.Store.YES)
        });

        writer.AddDocument(new Document
        {
            new StringField("id", "3", Field.Store.YES),
            new TextField("title", "Programming guide", Field.Store.YES),
            new TextField("content", "Learn to program in C# and build amazing applications", Field.Store.YES),
            new StringField("category", "technology", Field.Store.YES)
        });

        writer.AddDocument(new Document
        {
            new StringField("id", "4", Field.Store.YES),
            new TextField("title", "Another fox story", Field.Store.YES),
            new TextField("content", "A fox and a dog became friends", Field.Store.YES),
            new StringField("category", "stories", Field.Store.YES)
        });

        writer.Commit();
    }

    [Fact]
    public void Constructor_InitializesWithDefaultSimilarity()
    {
        // Arrange & Act
        var model = new SearchModel(_reader);

        // Assert
        Assert.NotNull(model);
        Assert.Null(model.CurrentQuery);
    }

    [Fact]
    public void Constructor_WithNullReader_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SearchModel(null!));
    }

    [Fact]
    public void GetFieldNames_ReturnsAllFieldsInIndex()
    {
        // Arrange
        var model = new SearchModel(_reader);

        // Act
        var fields = model.GetFieldNames();

        // Assert
        Assert.NotEmpty(fields);
        Assert.Contains("title", fields);
        Assert.Contains("content", fields);
        Assert.Contains("category", fields);
    }

    [Fact]
    public void ParseQuery_WithValidQuery_ReturnsQuery()
    {
        // Arrange
        var model = new SearchModel(_reader);

        // Act
        var query = model.ParseQuery("fox", "content");

        // Assert
        Assert.NotNull(query);
        Assert.IsType<TermQuery>(query);
    }

    [Fact]
    public void ParseQuery_WithNullOrEmptyString_ReturnsNull()
    {
        // Arrange
        var model = new SearchModel(_reader);

        // Act
        var query1 = model.ParseQuery(null!, "content");
        var query2 = model.ParseQuery("", "content");
        var query3 = model.ParseQuery("   ", "content");

        // Assert
        Assert.Null(query1);
        Assert.Null(query2);
        Assert.Null(query3);
    }

    [Fact]
    public void ParseQuery_WithClassicParser_ParsesSuccessfully()
    {
        // Arrange
        var model = new SearchModel(_reader);
        var settings = new SearchSettings
        {
            ParserType = QueryParserType.Classic
        };
        model.UpdateSettings(settings);

        // Act
        var query = model.ParseQuery("fox AND dog", "content");

        // Assert
        Assert.NotNull(query);
        Assert.IsType<BooleanQuery>(query);
    }

    [Fact]
    public void ParseQuery_WithStandardParser_ParsesSuccessfully()
    {
        // Arrange
        var model = new SearchModel(_reader);
        var settings = new SearchSettings
        {
            ParserType = QueryParserType.Standard
        };
        model.UpdateSettings(settings);

        // Act
        var query = model.ParseQuery("fox dog", "content");

        // Assert
        Assert.NotNull(query);
    }

    [Fact]
    public void ParseQuery_WithWildcard_ParsesSuccessfully()
    {
        // Arrange
        var model = new SearchModel(_reader);
        var settings = new SearchSettings
        {
            AllowLeadingWildcard = true
        };
        model.UpdateSettings(settings);

        // Act - Use a wildcard in the middle to ensure WildcardQuery
        var query = model.ParseQuery("f*x", "content");

        // Assert
        Assert.NotNull(query);
        Assert.IsType<WildcardQuery>(query);
    }

    [Fact]
    public void ParseQuery_WithPhraseQuery_ParsesSuccessfully()
    {
        // Arrange
        var model = new SearchModel(_reader);

        // Act
        var query = model.ParseQuery("\"quick brown\"", "content");

        // Assert
        Assert.NotNull(query);
        Assert.IsType<PhraseQuery>(query);
    }

    [Fact]
    public void ParseQuery_WithFuzzyQuery_ParsesSuccessfully()
    {
        // Arrange
        var model = new SearchModel(_reader);

        // Act
        var query = model.ParseQuery("quik~", "content");

        // Assert
        Assert.NotNull(query);
        Assert.IsType<FuzzyQuery>(query);
    }

    [Fact]
    public void ParseQuery_WithDefaultOperatorAND_UsesAND()
    {
        // Arrange
        var model = new SearchModel(_reader);
        var settings = new SearchSettings
        {
            DefaultOperator = QueryOperator.AND
        };
        model.UpdateSettings(settings);

        // Act
        var query = model.ParseQuery("fox dog", "content");

        // Assert
        Assert.NotNull(query);
        var booleanQuery = Assert.IsType<BooleanQuery>(query);
        // With AND operator, both terms should be required
        Assert.Contains(booleanQuery.Clauses, c => c.Occur == Occur.MUST);
    }

    [Fact]
    public void ParseQuery_WithDefaultOperatorOR_UsesOR()
    {
        // Arrange
        var model = new SearchModel(_reader);
        var settings = new SearchSettings
        {
            DefaultOperator = QueryOperator.OR
        };
        model.UpdateSettings(settings);

        // Act
        var query = model.ParseQuery("fox dog", "content");

        // Assert
        Assert.NotNull(query);
        var booleanQuery = Assert.IsType<BooleanQuery>(query);
        // With OR operator, terms should be optional (SHOULD)
        Assert.Contains(booleanQuery.Clauses, c => c.Occur == Occur.SHOULD);
    }

    [Fact]
    public void ParseQuery_WithInvalidQuery_ReturnsNull()
    {
        // Arrange
        var model = new SearchModel(_reader);

        // Act - Use an invalid query syntax
        var query = model.ParseQuery("content:[invalid TO", "content");

        // Assert
        Assert.Null(query);
    }

    [Fact]
    public void ExecuteSearch_WithValidQuery_ReturnsResults()
    {
        // Arrange
        var model = new SearchModel(_reader);
        var query = model.ParseQuery("fox", "content");

        // Act
        var results = model.ExecuteSearch(query!, 10);

        // Assert
        Assert.NotNull(results);
        Assert.True(results.TotalHits > 0);
        Assert.NotEmpty(results.Results);
        Assert.All(results.Results, r => Assert.True(r.Score > 0));
    }

    [Fact]
    public void ExecuteSearch_StoresCurrentQuery()
    {
        // Arrange
        var model = new SearchModel(_reader);
        var query = model.ParseQuery("fox", "content");

        // Act
        model.ExecuteSearch(query!, 10);

        // Assert
        Assert.NotNull(model.CurrentQuery);
        Assert.Same(query, model.CurrentQuery);
    }

    [Fact]
    public void ExecuteSearch_WithMaxResults_LimitsResults()
    {
        // Arrange
        var model = new SearchModel(_reader);
        var query = model.ParseQuery("*:*", "content"); // Match all query

        // Act
        var results = model.ExecuteSearch(query!, maxResults: 2);

        // Assert
        Assert.NotNull(results);
        Assert.True(results.Results.Count <= 2);
    }

    [Fact]
    public void ExecuteSearch_WithNullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var model = new SearchModel(_reader);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => model.ExecuteSearch(null!, 10));
    }

    [Fact]
    public void ExecuteSearch_IncludesFieldValuesInResults()
    {
        // Arrange
        var model = new SearchModel(_reader);
        var query = model.ParseQuery("fox", "content");

        // Act
        var results = model.ExecuteSearch(query!, 10);

        // Assert
        Assert.NotEmpty(results.Results);
        Assert.All(results.Results, r => Assert.False(string.IsNullOrEmpty(r.FieldValues)));
    }

    [Fact]
    public void ExecuteSearch_TruncatesLongFieldValues()
    {
        // Arrange - Create an index with a very long field value
        var tempPath = Path.Combine(Path.GetTempPath(), $"lucene_test_long_{Guid.NewGuid()}");
        System.IO.Directory.CreateDirectory(tempPath);

        try
        {
            using var dir = FSDirectory.Open(tempPath);
            var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, new StandardAnalyzer(LuceneVersion.LUCENE_48));
            using (var writer = new IndexWriter(dir, config))
            {
                writer.AddDocument(new Document
                {
                    new StringField("id", "1", Field.Store.YES),
                    new TextField("content", new string('a', 200) + " test", Field.Store.YES)
                });
                writer.Commit();
            }

            using var reader = DirectoryReader.Open(dir);
            var model = new SearchModel(reader);
            var query = model.ParseQuery("test", "content");

            // Act
            var results = model.ExecuteSearch(query!, 10);

            // Assert
            Assert.NotEmpty(results.Results);
            var fieldValues = results.Results[0].FieldValues;
            // Should contain "..." indicating truncation
            Assert.Contains("...", fieldValues);
        }
        finally
        {
            System.IO.Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public void Explain_WithValidDocId_ReturnsExplanation()
    {
        // Arrange
        var model = new SearchModel(_reader);
        var query = model.ParseQuery("fox", "content");
        var results = model.ExecuteSearch(query!, 10);
        var firstDocId = results.Results[0].DocId;

        // Act
        var explanation = model.Explain(firstDocId);

        // Assert
        Assert.NotNull(explanation);
        Assert.True(explanation.Value > 0);
        Assert.NotNull(explanation.Description);
    }

    [Fact]
    public void Explain_WithoutCurrentQuery_ReturnsNull()
    {
        // Arrange
        var model = new SearchModel(_reader);

        // Act
        var explanation = model.Explain(0);

        // Assert
        Assert.Null(explanation);
    }

    [Fact]
    public void Explain_WithInvalidDocId_ReturnsExplanation()
    {
        // Arrange
        var model = new SearchModel(_reader);
        var query = model.ParseQuery("fox", "content");
        model.ExecuteSearch(query!, 10);

        // Act
        var explanation = model.Explain(999999); // Invalid doc ID

        // Assert
        // Lucene returns an explanation even for invalid doc IDs
        // It just says "NON-MATCH" or similar
        Assert.NotNull(explanation);
    }

    [Fact]
    public void UpdateSettings_WithBM25Similarity_UpdatesSimilarity()
    {
        // Arrange
        var model = new SearchModel(_reader);
        var settings = new SearchSettings
        {
            SimilarityType = SimilarityType.BM25,
            BM25_K1 = 1.5f,
            BM25_B = 0.75f
        };

        // Act
        model.UpdateSettings(settings);
        var query = model.ParseQuery("fox", "content");
        var results1 = model.ExecuteSearch(query!, 10);

        // Assert - Just verify it executes without error
        Assert.NotNull(results1);
        Assert.True(results1.TotalHits > 0);
    }

    [Fact]
    public void UpdateSettings_WithClassicSimilarity_UpdatesSimilarity()
    {
        // Arrange
        var model = new SearchModel(_reader);
        var settings = new SearchSettings
        {
            SimilarityType = SimilarityType.Classic
        };

        // Act
        model.UpdateSettings(settings);
        var query = model.ParseQuery("fox", "content");
        var results = model.ExecuteSearch(query!, 10);

        // Assert - Just verify it executes without error
        Assert.NotNull(results);
        Assert.True(results.TotalHits > 0);
    }

    [Fact]
    public void UpdateSettings_WithNullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var model = new SearchModel(_reader);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => model.UpdateSettings(null!));
    }

    [Fact]
    public void UpdateSettings_WithDifferentSimilarities_ProducesDifferentScores()
    {
        // Arrange
        var model = new SearchModel(_reader);
        var query = model.ParseQuery("fox dog", "content");

        // Execute with BM25
        var bm25Settings = new SearchSettings { SimilarityType = SimilarityType.BM25 };
        model.UpdateSettings(bm25Settings);
        var bm25Results = model.ExecuteSearch(query!, 10);

        // Execute with Classic
        var classicSettings = new SearchSettings { SimilarityType = SimilarityType.Classic };
        model.UpdateSettings(classicSettings);
        var classicResults = model.ExecuteSearch(query!, 10);

        // Assert - Scores should be different between BM25 and Classic
        Assert.NotEqual(bm25Results.Results[0].Score, classicResults.Results[0].Score);
    }

    [Fact]
    public void UpdateSettings_WithCustomBM25Parameters_AffectsScoring()
    {
        // Arrange
        var model = new SearchModel(_reader);
        var query = model.ParseQuery("fox", "content");

        // Execute with default BM25 parameters
        var defaultSettings = new SearchSettings
        {
            SimilarityType = SimilarityType.BM25,
            BM25_K1 = 1.2f,
            BM25_B = 0.75f
        };
        model.UpdateSettings(defaultSettings);
        var defaultResults = model.ExecuteSearch(query!, 10);

        // Execute with custom BM25 parameters
        var customSettings = new SearchSettings
        {
            SimilarityType = SimilarityType.BM25,
            BM25_K1 = 2.0f,
            BM25_B = 0.5f
        };
        model.UpdateSettings(customSettings);
        var customResults = model.ExecuteSearch(query!, 10);

        // Assert - Different parameters should produce different scores
        Assert.NotEqual(defaultResults.Results[0].Score, customResults.Results[0].Score);
    }

    [Fact]
    public void ParseQuery_WithPhraseSlop_ParsesSuccessfully()
    {
        // Arrange
        var model = new SearchModel(_reader);
        var settings = new SearchSettings
        {
            PhraseSlop = 2
        };
        model.UpdateSettings(settings);

        // Act
        var query = model.ParseQuery("\"brown dog\"~2", "content");

        // Assert
        Assert.NotNull(query);
        Assert.IsType<PhraseQuery>(query);
        var phraseQuery = (PhraseQuery)query;
        Assert.Equal(2, phraseQuery.Slop);
    }

    [Fact]
    public void ParseQuery_WithFuzzySettings_AppliesCorrectly()
    {
        // Arrange
        var model = new SearchModel(_reader);
        var settings = new SearchSettings
        {
            FuzzyMinSim = 0.7f,
            FuzzyPrefixLength = 2
        };
        model.UpdateSettings(settings);

        // Act
        var query = model.ParseQuery("quik~", "content");

        // Assert
        Assert.NotNull(query);
        Assert.IsType<FuzzyQuery>(query);
    }

    public void Dispose()
    {
        // Cleanup
        _reader?.Dispose();
        if (System.IO.Directory.Exists(_testIndexPath))
        {
            System.IO.Directory.Delete(_testIndexPath, true);
        }
    }
}
