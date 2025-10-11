using Microsoft.Extensions.Logging;
using NeoLuke.Models.Logging;

namespace NeoLuke.Tests;

public class LogEntryTests
{
    [Fact]
    public void SingleLineMessage_FirstLine_ReturnsSameMessage()
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = LogLevel.Information,
            Category = "Test",
            Message = "This is a single line message"
        };

        Assert.Equal("This is a single line message", logEntry.FirstLine);
    }

    [Fact]
    public void SingleLineMessage_HasMultipleLines_ReturnsFalse()
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = LogLevel.Information,
            Category = "Test",
            Message = "This is a single line message"
        };

        Assert.False(logEntry.HasMultipleLines);
    }

    [Fact]
    public void SingleLineMessage_AdditionalLineCount_ReturnsZero()
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = LogLevel.Information,
            Category = "Test",
            Message = "This is a single line message"
        };

        Assert.Equal(0, logEntry.AdditionalLineCount);
    }

    [Fact]
    public void SingleLineMessage_AdditionalLinesIndicator_ReturnsEmpty()
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = LogLevel.Information,
            Category = "Test",
            Message = "This is a single line message"
        };

        Assert.Equal(string.Empty, logEntry.AdditionalLinesIndicator);
    }

    [Fact]
    public void MultiLineMessage_LF_FirstLine_ReturnsFirstLineOnly()
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = LogLevel.Information,
            Category = "Test",
            Message = "First line\nSecond line\nThird line"
        };

        Assert.Equal("First line", logEntry.FirstLine);
    }

    [Fact]
    public void MultiLineMessage_CRLF_FirstLine_ReturnsFirstLineOnly()
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = LogLevel.Information,
            Category = "Test",
            Message = "First line\r\nSecond line\r\nThird line"
        };

        Assert.Equal("First line", logEntry.FirstLine);
    }

    [Fact]
    public void MultiLineMessage_CR_FirstLine_ReturnsFirstLineOnly()
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = LogLevel.Information,
            Category = "Test",
            Message = "First line\rSecond line\rThird line"
        };

        Assert.Equal("First line", logEntry.FirstLine);
    }

    [Fact]
    public void MultiLineMessage_HasMultipleLines_ReturnsTrue()
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = LogLevel.Information,
            Category = "Test",
            Message = "First line\nSecond line"
        };

        Assert.True(logEntry.HasMultipleLines);
    }

    [Fact]
    public void MultiLineMessage_TwoLines_AdditionalLineCount_ReturnsOne()
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = LogLevel.Information,
            Category = "Test",
            Message = "First line\nSecond line"
        };

        Assert.Equal(1, logEntry.AdditionalLineCount);
    }

    [Fact]
    public void MultiLineMessage_ThreeLines_AdditionalLineCount_ReturnsTwo()
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = LogLevel.Information,
            Category = "Test",
            Message = "First line\nSecond line\nThird line"
        };

        Assert.Equal(2, logEntry.AdditionalLineCount);
    }

    [Fact]
    public void MultiLineMessage_CRLF_AdditionalLineCount_CountsCorrectly()
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = LogLevel.Information,
            Category = "Test",
            Message = "Line 1\r\nLine 2\r\nLine 3"
        };

        Assert.Equal(2, logEntry.AdditionalLineCount);
    }

    [Fact]
    public void MultiLineMessage_MixedLineEndings_AdditionalLineCount_CountsCorrectly()
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = LogLevel.Information,
            Category = "Test",
            Message = "Line 1\nLine 2\r\nLine 3\rLine 4"
        };

        Assert.Equal(3, logEntry.AdditionalLineCount);
    }

    [Fact]
    public void MultiLineMessage_OneLine_AdditionalLinesIndicator_ReturnsSingular()
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = LogLevel.Information,
            Category = "Test",
            Message = "First line\nSecond line"
        };

        Assert.Equal("[+1 line]", logEntry.AdditionalLinesIndicator);
    }

    [Fact]
    public void MultiLineMessage_TwoLines_AdditionalLinesIndicator_ReturnsPlural()
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = LogLevel.Information,
            Category = "Test",
            Message = "First line\nSecond line\nThird line"
        };

        Assert.Equal("[+2 lines]", logEntry.AdditionalLinesIndicator);
    }

    [Fact]
    public void MultiLineMessage_SevenLines_AdditionalLinesIndicator_ReturnsCorrectCount()
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = LogLevel.Information,
            Category = "Test",
            Message = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\nLine 7\nLine 8"
        };

        Assert.Equal("[+7 lines]", logEntry.AdditionalLinesIndicator);
    }

    [Fact]
    public void EmptyMessage_PropertiesHandleGracefully()
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = LogLevel.Information,
            Category = "Test",
            Message = string.Empty
        };

        Assert.Equal(string.Empty, logEntry.FirstLine);
        Assert.False(logEntry.HasMultipleLines);
        Assert.Equal(0, logEntry.AdditionalLineCount);
        Assert.Equal(string.Empty, logEntry.AdditionalLinesIndicator);
    }

    [Fact]
    public void MessageEndingWithNewline_CountsCorrectly()
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = LogLevel.Information,
            Category = "Test",
            Message = "First line\nSecond line\n"
        };

        Assert.Equal("First line", logEntry.FirstLine);
        Assert.Equal(2, logEntry.AdditionalLineCount);
        Assert.True(logEntry.HasMultipleLines);
    }

    [Fact]
    public void FullMessage_WithoutException_ReturnsSameAsMessage()
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = LogLevel.Information,
            Category = "Test",
            Message = "Test message\nLine 2"
        };

        Assert.Equal("Test message\nLine 2", logEntry.FullMessage);
    }

    [Fact]
    public void FullMessage_WithException_IncludesExceptionText()
    {
        var exception = new InvalidOperationException("Test exception");
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = LogLevel.Error,
            Category = "Test",
            Message = "Error occurred",
            Exception = exception
        };

        Assert.Contains("Error occurred", logEntry.FullMessage);
        Assert.Contains("Test exception", logEntry.FullMessage);
    }
}
