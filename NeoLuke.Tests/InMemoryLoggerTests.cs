using Microsoft.Extensions.Logging;
using NeoLuke.Services;

namespace NeoLuke.Tests;

public class InMemoryLoggerTests
{
    [Fact]
    public void Logger_SingleLineMessage_CapturesCorrectly()
    {
        var provider = new InMemoryLoggerProvider();
        var logger = provider.CreateLogger("Test");

        logger.LogInformation("Single line message");

        var entries = provider.GetLogEntries();
        Assert.Single(entries);
        Assert.Equal("Single line message", entries[0].Message);
        Assert.False(entries[0].HasMultipleLines);
    }

    [Fact]
    public void Logger_MultiLineMessage_CapturesAllLines()
    {
        var provider = new InMemoryLoggerProvider();
        var logger = provider.CreateLogger("Test");

        var multiLineMessage = "Line 1\nLine 2\nLine 3";
        logger.LogInformation(multiLineMessage);

        var entries = provider.GetLogEntries();
        Assert.Single(entries);
        Assert.Equal(multiLineMessage, entries[0].Message);
        Assert.True(entries[0].HasMultipleLines);
        Assert.Equal("Line 1", entries[0].FirstLine);
        Assert.Equal(2, entries[0].AdditionalLineCount);
    }

    [Fact]
    public void Logger_MessageWithCRLF_CapturesCorrectly()
    {
        var provider = new InMemoryLoggerProvider();
        var logger = provider.CreateLogger("Test");

        var multiLineMessage = "Line 1\r\nLine 2\r\nLine 3";
        logger.LogInformation(multiLineMessage);

        var entries = provider.GetLogEntries();
        Assert.Single(entries);
        Assert.Equal(multiLineMessage, entries[0].Message);
        Assert.True(entries[0].HasMultipleLines);
        Assert.Equal("Line 1", entries[0].FirstLine);
        Assert.Equal(2, entries[0].AdditionalLineCount);
    }

    [Fact]
    public void Logger_ExceptionWithMessage_CapturesException()
    {
        var provider = new InMemoryLoggerProvider();
        var logger = provider.CreateLogger("Test");

        var exception = new InvalidOperationException("Test exception");
        logger.LogError(exception, "Error occurred");

        var entries = provider.GetLogEntries();
        Assert.Single(entries);
        Assert.Equal("Error occurred", entries[0].Message);
        Assert.NotNull(entries[0].Exception);
        Assert.Equal("Test exception", entries[0].Exception?.Message);
    }

    [Fact]
    public void Logger_LogEntryAddedEvent_Fires()
    {
        var provider = new InMemoryLoggerProvider();
        var logger = provider.CreateLogger("Test");

        var eventFired = false;
        provider.LogEntryAdded += (_, entry) =>
        {
            eventFired = true;
            Assert.Equal("Test message", entry.Message);
        };

        logger.LogInformation("Test message");

        Assert.True(eventFired);
    }

    [Fact]
    public void Logger_ClearLogEntries_RemovesAllLogs()
    {
        var provider = new InMemoryLoggerProvider();
        var logger = provider.CreateLogger("Test");

        logger.LogInformation("Message 1");
        logger.LogInformation("Message 2");
        logger.LogInformation("Message 3");

        Assert.Equal(3, provider.GetLogEntries().Count);

        provider.ClearLogEntries();

        Assert.Empty(provider.GetLogEntries());
    }

    [Fact]
    public void Logger_MaxLogEntries_LimitsSize()
    {
        var provider = new InMemoryLoggerProvider();
        var logger = provider.CreateLogger("Test");

        // MaxLogEntries is 10000, let's test that old entries are removed
        // We'll log 10001 messages
        for (int i = 0; i < 10001; i++)
        {
            logger.LogInformation($"Message {i}");
        }

        var entries = provider.GetLogEntries();
        Assert.Equal(10000, entries.Count);

        // The first message should have been removed, so the first entry should be "Message 1"
        Assert.Equal("Message 1", entries[0].Message);
    }

    [Fact]
    public void Logger_StructuredLogging_FormatsCorrectly()
    {
        var provider = new InMemoryLoggerProvider();
        var logger = provider.CreateLogger("Test");

        logger.LogInformation("User {UserId} logged in from {IpAddress}", 123, "192.168.1.1");

        var entries = provider.GetLogEntries();
        Assert.Single(entries);
        Assert.Contains("123", entries[0].Message);
        Assert.Contains("192.168.1.1", entries[0].Message);
    }

    [Fact]
    public void Logger_MultilineStructuredMessage_CapturesMultilineContent()
    {
        var provider = new InMemoryLoggerProvider();
        var logger = provider.CreateLogger("Test");

        var multilineContent = "Part 1\nPart 2\nPart 3";
        logger.LogInformation("Data: {Content}", multilineContent);

        var entries = provider.GetLogEntries();
        Assert.Single(entries);
        Assert.Contains("Part 1", entries[0].Message);
        Assert.Contains("Part 2", entries[0].Message);
        Assert.Contains("Part 3", entries[0].Message);
        Assert.True(entries[0].HasMultipleLines);
    }
}
