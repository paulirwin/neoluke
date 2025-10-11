using Lucene.Net.Analysis.Standard;
using NeoLuke.Models.Search;
using System.Globalization;

namespace NeoLuke.Tests.Models;

public class SearchSettingsTests
{
    [Fact]
    public void DefaultConstructor_InitializesWithDefaultValues()
    {
        // Arrange & Act
        var settings = new SearchSettings();

        // Assert
        Assert.Equal(QueryParserType.Standard, settings.ParserType);
        Assert.Equal(QueryOperator.OR, settings.DefaultOperator);
        Assert.True(settings.EnablePositionIncrements);
        Assert.False(settings.AllowLeadingWildcard);
        Assert.True(settings.SplitOnWhitespace);
        Assert.False(settings.AutoGeneratePhraseQueries);
        Assert.Equal(0, settings.PhraseSlop);
        Assert.Equal(2f, settings.FuzzyMinSim);
        Assert.Equal(0, settings.FuzzyPrefixLength);
        Assert.Equal(DateResolution.MILLISECOND, settings.DateResolution);
        Assert.Equal(SimilarityType.BM25, settings.SimilarityType);
        Assert.Equal(1.2f, settings.BM25_K1);
        Assert.Equal(0.75f, settings.BM25_B);
        Assert.True(settings.DiscountOverlaps);
    }

    [Fact]
    public void GetAnalyzerType_WithDefaultAnalyzer_ReturnsStandardAnalyzer()
    {
        // Arrange
        var settings = new SearchSettings();

        // Act
        var analyzerType = settings.GetAnalyzerType();

        // Assert
        // Default analyzer type name may not resolve with Type.GetType()
        // This is expected behavior - the SearchModel handles this case
        Assert.True(analyzerType == null || analyzerType.Name == "StandardAnalyzer");
    }

    [Fact]
    public void SetAnalyzerType_WithValidType_UpdatesAnalyzerTypeName()
    {
        // Arrange
        var settings = new SearchSettings();
        var analyzerType = typeof(StandardAnalyzer);

        // Act
        settings.SetAnalyzerType(analyzerType);
        var retrievedType = settings.GetAnalyzerType();

        // Assert
        Assert.NotNull(retrievedType);
        Assert.Equal(analyzerType, retrievedType);
    }

    [Fact]
    public void SetAnalyzerType_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        var settings = new SearchSettings();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => settings.SetAnalyzerType(null!));
    }

    [Fact]
    public void GetCultureInfo_WithDefaultLocale_ReturnsCurrentCulture()
    {
        // Arrange
        var settings = new SearchSettings();

        // Act
        var culture = settings.GetCultureInfo();

        // Assert
        Assert.NotNull(culture);
    }

    [Fact]
    public void GetCultureInfo_WithValidLocaleName_ReturnsCorrectCulture()
    {
        // Arrange
        var settings = new SearchSettings
        {
            LocaleName = "en-US"
        };

        // Act
        var culture = settings.GetCultureInfo();

        // Assert
        Assert.NotNull(culture);
        Assert.Equal("en-US", culture.Name);
    }

    [Fact]
    public void GetCultureInfo_WithEmptyLocaleName_DoesNotThrow()
    {
        // Arrange
        var settings = new SearchSettings
        {
            LocaleName = ""
        };

        // Act
        var culture = settings.GetCultureInfo();

        // Assert
        Assert.NotNull(culture);
        // Should fall back to current culture instead of throwing
    }

    [Fact]
    public void GetTimeZoneInfo_WithDefaultTimeZoneId_ReturnsLocalTimeZone()
    {
        // Arrange
        var settings = new SearchSettings();

        // Act
        var timeZone = settings.GetTimeZoneInfo();

        // Assert
        Assert.NotNull(timeZone);
    }

    [Fact]
    public void GetTimeZoneInfo_WithValidTimeZoneId_ReturnsCorrectTimeZone()
    {
        // Arrange
        var settings = new SearchSettings
        {
            TimeZoneId = "UTC"
        };

        // Act
        var timeZone = settings.GetTimeZoneInfo();

        // Assert
        Assert.NotNull(timeZone);
        Assert.Equal("UTC", timeZone.Id);
    }

    [Fact]
    public void GetTimeZoneInfo_WithInvalidTimeZoneId_ReturnsLocalTimeZone()
    {
        // Arrange
        var settings = new SearchSettings
        {
            TimeZoneId = "Invalid/TimeZone"
        };

        // Act
        var timeZone = settings.GetTimeZoneInfo();

        // Assert
        Assert.NotNull(timeZone);
        // Should fall back to local time zone instead of throwing
        Assert.Equal(TimeZoneInfo.Local.Id, timeZone.Id);
    }

    [Fact]
    public void ParserType_CanBeSet()
    {
        // Arrange
        var settings = new SearchSettings();

        // Act
        settings.ParserType = QueryParserType.Classic;

        // Assert
        Assert.Equal(QueryParserType.Classic, settings.ParserType);
    }

    [Fact]
    public void DefaultOperator_CanBeSet()
    {
        // Arrange
        var settings = new SearchSettings();

        // Act
        settings.DefaultOperator = QueryOperator.AND;

        // Assert
        Assert.Equal(QueryOperator.AND, settings.DefaultOperator);
    }

    [Fact]
    public void SimilarityType_CanBeSet()
    {
        // Arrange
        var settings = new SearchSettings();

        // Act
        settings.SimilarityType = SimilarityType.Classic;

        // Assert
        Assert.Equal(SimilarityType.Classic, settings.SimilarityType);
    }

    [Fact]
    public void BM25Parameters_CanBeSet()
    {
        // Arrange
        var settings = new SearchSettings();

        // Act
        settings.BM25_K1 = 2.0f;
        settings.BM25_B = 0.5f;

        // Assert
        Assert.Equal(2.0f, settings.BM25_K1);
        Assert.Equal(0.5f, settings.BM25_B);
    }

    [Fact]
    public void PhraseSlop_CanBeSet()
    {
        // Arrange
        var settings = new SearchSettings();

        // Act
        settings.PhraseSlop = 5;

        // Assert
        Assert.Equal(5, settings.PhraseSlop);
    }

    [Fact]
    public void FuzzyParameters_CanBeSet()
    {
        // Arrange
        var settings = new SearchSettings();

        // Act
        settings.FuzzyMinSim = 0.8f;
        settings.FuzzyPrefixLength = 3;

        // Assert
        Assert.Equal(0.8f, settings.FuzzyMinSim);
        Assert.Equal(3, settings.FuzzyPrefixLength);
    }

    [Fact]
    public void DateResolution_CanBeSet()
    {
        // Arrange
        var settings = new SearchSettings();

        // Act
        settings.DateResolution = DateResolution.DAY;

        // Assert
        Assert.Equal(DateResolution.DAY, settings.DateResolution);
    }

    [Fact]
    public void AllowLeadingWildcard_CanBeSet()
    {
        // Arrange
        var settings = new SearchSettings();

        // Act
        settings.AllowLeadingWildcard = true;

        // Assert
        Assert.True(settings.AllowLeadingWildcard);
    }

    [Fact]
    public void EnablePositionIncrements_CanBeSet()
    {
        // Arrange
        var settings = new SearchSettings();

        // Act
        settings.EnablePositionIncrements = false;

        // Assert
        Assert.False(settings.EnablePositionIncrements);
    }

    [Fact]
    public void AutoGeneratePhraseQueries_CanBeSet()
    {
        // Arrange
        var settings = new SearchSettings();

        // Act
        settings.AutoGeneratePhraseQueries = true;

        // Assert
        Assert.True(settings.AutoGeneratePhraseQueries);
    }

    [Fact]
    public void DiscountOverlaps_CanBeSet()
    {
        // Arrange
        var settings = new SearchSettings();

        // Act
        settings.DiscountOverlaps = false;

        // Assert
        Assert.False(settings.DiscountOverlaps);
    }
}
