using NeoLuke.Utilities;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;

namespace NeoLuke.Tests;

public class AnalyzerDiscoveryTests
{
    // Static constructor to ensure analyzer assemblies are loaded
    static AnalyzerDiscoveryTests()
    {
        // Force load the analyzer types so their assemblies are available for reflection
        _ = typeof(StandardAnalyzer);
        _ = typeof(WhitespaceAnalyzer);
        _ = typeof(SimpleAnalyzer);
        _ = typeof(KeywordAnalyzer);
        _ = typeof(StopAnalyzer);
    }

    [Fact]
    public void DiscoverAnalyzers_ShouldFindCommonAnalyzers()
    {
        // Act - ignoreCache to ensure fresh discovery after static constructor
        var analyzers = AnalyzerDiscovery.DiscoverAnalyzers(ignoreCache: true);

        // Assert
        Assert.NotEmpty(analyzers);

        // Check for common analyzer types
        Assert.Contains(analyzers, a => a.SimpleName == "StandardAnalyzer");
        Assert.Contains(analyzers, a => a.SimpleName == "WhitespaceAnalyzer");
        Assert.Contains(analyzers, a => a.SimpleName == "SimpleAnalyzer");
        Assert.Contains(analyzers, a => a.SimpleName == "KeywordAnalyzer");
        Assert.Contains(analyzers, a => a.SimpleName == "StopAnalyzer");
    }

    [Fact]
    public void DiscoverAnalyzers_ShouldOnlyReturnConcreteClasses()
    {
        // Act - ignoreCache to ensure fresh discovery
        var analyzers = AnalyzerDiscovery.DiscoverAnalyzers(ignoreCache: true);

        // Assert
        foreach (var analyzer in analyzers)
        {
            Assert.True(analyzer.Type.IsClass, $"{analyzer.SimpleName} should be a class");
            Assert.False(analyzer.Type.IsAbstract, $"{analyzer.SimpleName} should not be abstract");
            Assert.True(analyzer.Type.IsPublic, $"{analyzer.SimpleName} should be public");
            Assert.True(typeof(Analyzer).IsAssignableFrom(analyzer.Type),
                $"{analyzer.SimpleName} should inherit from Analyzer");
        }
    }

    [Fact]
    public void DiscoverAnalyzers_ShouldBeCached()
    {
        // Act - first call with ignoreCache, then subsequent calls should be cached
        var first = AnalyzerDiscovery.DiscoverAnalyzers(ignoreCache: true);
        var second = AnalyzerDiscovery.DiscoverAnalyzers();

        // Assert - should return the same instance
        Assert.Same(first, second);
    }

    [Fact]
    public void CreateAnalyzer_StandardAnalyzer_ShouldCreateInstance()
    {
        // Arrange
        var analyzerType = typeof(StandardAnalyzer);

        // Act
        using var analyzer = AnalyzerDiscovery.CreateAnalyzer(analyzerType);

        // Assert
        Assert.NotNull(analyzer);
        Assert.IsType<StandardAnalyzer>(analyzer);
    }

    [Fact]
    public void CreateAnalyzer_WhitespaceAnalyzer_ShouldCreateInstance()
    {
        // Arrange
        var analyzerType = typeof(WhitespaceAnalyzer);

        // Act
        using var analyzer = AnalyzerDiscovery.CreateAnalyzer(analyzerType);

        // Assert
        Assert.NotNull(analyzer);
        Assert.IsType<WhitespaceAnalyzer>(analyzer);
    }

    [Fact]
    public void CreateAnalyzer_KeywordAnalyzer_ShouldCreateInstance()
    {
        // Arrange
        var analyzerType = typeof(KeywordAnalyzer);

        // Act
        using var analyzer = AnalyzerDiscovery.CreateAnalyzer(analyzerType);

        // Assert
        Assert.NotNull(analyzer);
        Assert.IsType<KeywordAnalyzer>(analyzer);
    }

    [Fact]
    public void CreateAnalyzer_NullType_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => AnalyzerDiscovery.CreateAnalyzer(null!));
    }

    [Fact]
    public void CreateAnalyzer_NonAnalyzerType_ShouldThrowArgumentException()
    {
        // Arrange
        var nonAnalyzerType = typeof(string);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => AnalyzerDiscovery.CreateAnalyzer(nonAnalyzerType));
    }

    [Fact]
    public void GetDisplayName_StandardAnalyzer_ShouldReturnSimpleName()
    {
        // Arrange
        var analyzerType = typeof(StandardAnalyzer);

        // Act
        var displayName = AnalyzerDiscovery.GetDisplayName(analyzerType);

        // Assert
        Assert.Equal("StandardAnalyzer", displayName);
    }

    [Fact]
    public void GetDisplayName_NullType_ShouldReturnUnknown()
    {
        // Act
        var displayName = AnalyzerDiscovery.GetDisplayName(null!);

        // Assert
        Assert.Equal("Unknown", displayName);
    }

    [Theory]
    [InlineData("StandardAnalyzer")]
    [InlineData("standardanalyzer")]
    [InlineData("STANDARDANALYZER")]
    public void FindByName_ShouldBeCaseInsensitive(string searchName)
    {
        // Act - ignoreCache to ensure fresh discovery
        var analyzerInfo = AnalyzerDiscovery.FindByName(searchName, ignoreCache: true);

        // Assert
        Assert.NotNull(analyzerInfo);
        Assert.Equal("StandardAnalyzer", analyzerInfo.SimpleName);
        Assert.Equal(typeof(StandardAnalyzer), analyzerInfo.Type);
    }

    [Fact]
    public void FindByName_NonExistentAnalyzer_ShouldReturnNull()
    {
        // Act - ignoreCache to ensure fresh discovery
        var analyzerInfo = AnalyzerDiscovery.FindByName("NonExistentAnalyzer", ignoreCache: true);

        // Assert
        Assert.Null(analyzerInfo);
    }

    [Fact]
    public void AnalyzerInfo_ShouldHaveDisplayNameAndSimpleName()
    {
        // Act - ignoreCache to ensure fresh discovery
        var analyzers = AnalyzerDiscovery.DiscoverAnalyzers(ignoreCache: true);

        // Assert
        foreach (var analyzer in analyzers)
        {
            Assert.NotNull(analyzer.Type);
            Assert.False(string.IsNullOrWhiteSpace(analyzer.SimpleName));
            Assert.False(string.IsNullOrWhiteSpace(analyzer.DisplayName));
        }
    }
}
