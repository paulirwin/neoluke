using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using NeoLuke.Models.Analysis;

namespace NeoLuke.Tests.Models;

public class AnalysisModelTests
{
    [Fact]
    public void AnalyzeText_WithValidInput_ReturnsTokens()
    {
        // Arrange
        var model = new AnalysisModel();
        var analyzerType = typeof(StandardAnalyzer);
        var inputText = "The quick brown fox";

        // Act
        var tokens = model.AnalyzeText(analyzerType, inputText);

        // Assert
        Assert.NotEmpty(tokens);
        // StandardAnalyzer removes "the" as a stop word
        Assert.Equal(3, tokens.Count); // "quick", "brown", "fox"
    }

    [Fact]
    public void AnalyzeText_WithNullAnalyzerType_ThrowsArgumentNullException()
    {
        // Arrange
        var model = new AnalysisModel();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => model.AnalyzeText(null!, "test"));
    }

    [Fact]
    public void AnalyzeText_WithNullInput_ReturnsEmptyList()
    {
        // Arrange
        var model = new AnalysisModel();
        var analyzerType = typeof(StandardAnalyzer);

        // Act
        var tokens = model.AnalyzeText(analyzerType, null!);

        // Assert
        Assert.Empty(tokens);
    }

    [Fact]
    public void AnalyzeText_WithEmptyInput_ReturnsEmptyList()
    {
        // Arrange
        var model = new AnalysisModel();
        var analyzerType = typeof(StandardAnalyzer);

        // Act
        var tokens = model.AnalyzeText(analyzerType, "");

        // Assert
        Assert.Empty(tokens);
    }

    [Fact]
    public void AnalyzeText_WithWhitespaceInput_ReturnsEmptyList()
    {
        // Arrange
        var model = new AnalysisModel();
        var analyzerType = typeof(StandardAnalyzer);

        // Act
        var tokens = model.AnalyzeText(analyzerType, "   ");

        // Assert
        Assert.Empty(tokens);
    }

    [Fact]
    public void AnalyzeText_TokensHaveCorrectTerms()
    {
        // Arrange
        var model = new AnalysisModel();
        var analyzerType = typeof(StandardAnalyzer);
        var inputText = "The quick brown fox";

        // Act
        var tokens = model.AnalyzeText(analyzerType, inputText);

        // Assert
        Assert.Equal("quick", tokens[0].Term);
        Assert.Equal("brown", tokens[1].Term);
        Assert.Equal("fox", tokens[2].Term);
    }

    [Fact]
    public void AnalyzeText_TokensHaveCorrectPositions()
    {
        // Arrange
        var model = new AnalysisModel();
        var analyzerType = typeof(StandardAnalyzer);
        var inputText = "quick brown fox"; // No stop words

        // Act
        var tokens = model.AnalyzeText(analyzerType, inputText);

        // Assert
        // Position should increment for each token
        Assert.Equal(3, tokens.Count);
        for (int i = 0; i < tokens.Count; i++)
        {
            Assert.Equal(i, tokens[i].Position);
        }
    }

    [Fact]
    public void AnalyzeText_TokensHaveCorrectOffsets()
    {
        // Arrange
        var model = new AnalysisModel();
        var analyzerType = typeof(StandardAnalyzer);
        var inputText = "quick brown";

        // Act
        var tokens = model.AnalyzeText(analyzerType, inputText);

        // Assert
        Assert.Equal(0, tokens[0].StartOffset);
        Assert.Equal(5, tokens[0].EndOffset); // "quick"
        Assert.Equal(6, tokens[1].StartOffset);
        Assert.Equal(11, tokens[1].EndOffset); // "brown"
    }

    [Fact]
    public void AnalyzeText_TokensHaveAttributes()
    {
        // Arrange
        var model = new AnalysisModel();
        var analyzerType = typeof(StandardAnalyzer);
        var inputText = "test";

        // Act
        var tokens = model.AnalyzeText(analyzerType, inputText);

        // Assert
        Assert.NotEmpty(tokens);
        var token = tokens[0];
        Assert.NotNull(token.Attributes);
        Assert.NotEmpty(token.Attributes);
        Assert.Contains("term", token.Attributes.Keys);
        Assert.Contains("position", token.Attributes.Keys);
        Assert.Contains("startOffset", token.Attributes.Keys);
        Assert.Contains("endOffset", token.Attributes.Keys);
        Assert.Contains("type", token.Attributes.Keys);
    }

    [Fact]
    public void AnalyzeText_TokensHaveDetailedAttributes()
    {
        // Arrange
        var model = new AnalysisModel();
        var analyzerType = typeof(StandardAnalyzer);
        var inputText = "test";

        // Act
        var tokens = model.AnalyzeText(analyzerType, inputText);

        // Assert
        Assert.NotEmpty(tokens);
        var token = tokens[0];
        Assert.NotNull(token.DetailedAttributes);
        Assert.NotEmpty(token.DetailedAttributes);
        Assert.All(token.DetailedAttributes, attr =>
        {
            Assert.NotNull(attr.AttributeTypeName);
            Assert.NotNull(attr.PropertyName);
            Assert.NotNull(attr.Value);
        });
    }

    [Fact]
    public void AnalyzeText_WithStandardAnalyzer_LowercasesTokens()
    {
        // Arrange
        var model = new AnalysisModel();
        var analyzerType = typeof(StandardAnalyzer);
        var inputText = "UPPERCASE text";

        // Act
        var tokens = model.AnalyzeText(analyzerType, inputText);

        // Assert
        Assert.NotEmpty(tokens);
        Assert.Equal("uppercase", tokens[0].Term);
        Assert.Equal("text", tokens[1].Term);
    }

    [Fact]
    public void AnalyzeText_WithStandardAnalyzer_RemovesStopWords_VerifiesCase()
    {
        // Arrange
        var model = new AnalysisModel();
        var analyzerType = typeof(StandardAnalyzer);
        var inputText = "UPPERCASE lowercase";

        // Act
        var tokens = model.AnalyzeText(analyzerType, inputText);

        // Assert
        Assert.NotEmpty(tokens);
        // StandardAnalyzer lowercases
        Assert.Equal("uppercase", tokens[0].Term);
        Assert.Equal("lowercase", tokens[1].Term);
    }

    [Fact]
    public void AnalyzeText_WithStandardAnalyzer_RemovesStopWords()
    {
        // Arrange
        var model = new AnalysisModel();
        var analyzerType = typeof(StandardAnalyzer);
        var inputText = "the quick fox"; // "the" is a stop word

        // Act
        var tokens = model.AnalyzeText(analyzerType, inputText);

        // Assert
        // "the" should be removed by StandardAnalyzer
        Assert.Equal(2, tokens.Count);
        Assert.DoesNotContain(tokens, t => t.Term == "the");
        Assert.Contains(tokens, t => t.Term == "quick");
        Assert.Contains(tokens, t => t.Term == "fox");
    }

    [Fact]
    public void AnalyzeText_WithDifferentText_ProducesDifferentTokens()
    {
        // Arrange
        var model = new AnalysisModel();
        var analyzerType = typeof(StandardAnalyzer);
        var inputText1 = "apple banana";
        var inputText2 = "orange grape";

        // Act
        var tokens1 = model.AnalyzeText(analyzerType, inputText1);
        var tokens2 = model.AnalyzeText(analyzerType, inputText2);

        // Assert
        Assert.NotEmpty(tokens1);
        Assert.NotEmpty(tokens2);
        Assert.NotEqual(tokens1[0].Term, tokens2[0].Term);
    }

    [Fact]
    public void AnalyzedToken_AttributesDisplay_FormatsCorrectly()
    {
        // Arrange
        var token = new AnalyzedToken
        {
            Attributes = new Dictionary<string, string>
            {
                ["term"] = "test",
                ["position"] = "0",
                ["type"] = "word"
            }
        };

        // Act
        var display = token.AttributesDisplay;

        // Assert
        Assert.NotNull(display);
        Assert.Contains("term=test", display);
        Assert.Contains("position=0", display);
        Assert.Contains("type=word", display);
    }

    [Fact]
    public void AnalyzeText_WithComplexText_HandlesCorrectly()
    {
        // Arrange
        var model = new AnalysisModel();
        var analyzerType = typeof(StandardAnalyzer);
        var inputText = "Hello, World! This is a test. It has punctuation, numbers (123), and symbols @#$.";

        // Act
        var tokens = model.AnalyzeText(analyzerType, inputText);

        // Assert
        Assert.NotEmpty(tokens);
        // StandardAnalyzer should handle punctuation and extract meaningful tokens
        Assert.All(tokens, token =>
        {
            Assert.NotNull(token.Term);
            Assert.True(token.StartOffset >= 0);
            Assert.True(token.EndOffset > token.StartOffset);
        });
    }

    [Fact]
    public void AnalyzeText_WithUnicodeText_HandlesCorrectly()
    {
        // Arrange
        var model = new AnalysisModel();
        var analyzerType = typeof(StandardAnalyzer);
        var inputText = "Hello café naïve résumé";

        // Act
        var tokens = model.AnalyzeText(analyzerType, inputText);

        // Assert
        Assert.NotEmpty(tokens);
        Assert.All(tokens, token => Assert.NotNull(token.Term));
    }

    [Fact]
    public void AnalyzeText_TokensHaveValidTypes()
    {
        // Arrange
        var model = new AnalysisModel();
        var analyzerType = typeof(StandardAnalyzer);
        var inputText = "test 123";

        // Act
        var tokens = model.AnalyzeText(analyzerType, inputText);

        // Assert
        Assert.NotEmpty(tokens);
        Assert.All(tokens, token =>
        {
            Assert.NotNull(token.Type);
            Assert.NotEmpty(token.Type);
        });
    }
}
