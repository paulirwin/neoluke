using Microsoft.Extensions.Logging;
using NeoLuke.Services;
using NeoLuke.Tests.Integration.Helpers;

namespace NeoLuke.Tests.Integration;

/// <summary>
/// Base class for headless integration tests that provides common setup and teardown
/// </summary>
public abstract class HeadlessTestBase : IDisposable
{
    protected readonly string TestIndexPath;
    protected readonly ILogger Logger;
    protected readonly ILoggerFactory LoggerFactory;
    protected readonly IIndexService IndexService;

    protected HeadlessTestBase(int documentCount = 20)
    {
        // Create logger factory for tests
        LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddConsole();
        });

        Logger = LoggerFactory.CreateLogger(GetType());
        Logger.LogInformation("Setting up test: {TestName}", GetType().Name);

        // Create test index
        TestIndexPath = TestIndexHelper.CreateTempIndexDirectory();
        Logger.LogDebug("Creating test index at: {Path}", TestIndexPath);
        TestIndexHelper.CreateTestIndex(TestIndexPath, documentCount);
        Logger.LogDebug("Test index created successfully");

        // Create IndexService
        var indexServiceLogger = LoggerFactory.CreateLogger<IndexService>();
        IndexService = new IndexService(indexServiceLogger);
    }

    /// <summary>
    /// Opens the test index
    /// </summary>
    protected async Task OpenTestIndexAsync(bool readOnly = true)
    {
        Logger.LogDebug("Opening test index");
        await IndexService.OpenAsync(TestIndexPath, directoryType: null, readOnly);
        Logger.LogDebug("Test index opened successfully");
    }

    /// <summary>
    /// Cleanup test resources
    /// </summary>
    public virtual void Dispose()
    {
        Logger.LogInformation("Cleaning up test: {TestName}", GetType().Name);

        // Close index if open
        if (IndexService.IsOpen)
        {
            IndexService.Close();
        }

        // Dispose IndexService
        IndexService.Dispose();

        // Delete test index
        TestIndexHelper.DeleteTestIndex(TestIndexPath);
        Logger.LogDebug("Test index deleted");

        // Dispose logger factory
        LoggerFactory.Dispose();

        GC.SuppressFinalize(this);
    }
}
