using Avalonia.Headless.XUnit;
using Lucene.Net.Index;
using NeoLuke.Services;

namespace NeoLuke.Tests.Integration.Tests;

/// <summary>
/// Integration tests for index opening and management workflows
/// </summary>
public class IndexWorkflowTests : HeadlessTestBase
{
    [AvaloniaFact]
    public async Task OpenIndex_UpdatesIndexServiceState()
    {
        // Act
        await OpenTestIndexAsync();

        // Assert
        Assert.True(IndexService.IsOpen);
        Assert.NotNull(IndexService.CurrentReader);
        Assert.NotNull(IndexService.CurrentDirectory);
        Assert.Equal(TestIndexPath, IndexService.CurrentPath);
        Assert.True(IndexService.IsReadOnly);
    }

    [AvaloniaFact]
    public async Task OpenIndex_PublishesIndexOpenedEvent()
    {
        // Arrange
        IndexOpenedEvent? receivedEvent = null;
        IndexService.IndexOpened.Subscribe(e => receivedEvent = e);

        // Act
        await OpenTestIndexAsync();

        // Assert
        Assert.NotNull(receivedEvent);
        Assert.Equal(TestIndexPath, receivedEvent.Info.Path);
        Assert.True(receivedEvent.Info.IsReadOnly);
        Assert.NotNull(receivedEvent.Info.Reader);
        Assert.Equal(20, receivedEvent.Info.Reader.NumDocs); // Default test document count
    }

    [AvaloniaFact]
    public async Task OpenIndex_InReadWriteMode_SetsCorrectMode()
    {
        // Act
        await IndexService.OpenAsync(TestIndexPath, directoryType: null, readOnly: false);

        // Assert
        Assert.False(IndexService.IsReadOnly);
        Assert.True(IndexService.IsOpen);
    }

    [AvaloniaFact]
    public async Task ReopenIndex_RefreshesReader()
    {
        // Arrange
        await OpenTestIndexAsync();
        var firstReader = IndexService.CurrentReader;

        // Act
        await IndexService.ReopenAsync();

        // Assert
        Assert.True(IndexService.IsOpen);
        Assert.NotSame(firstReader, IndexService.CurrentReader);
        Assert.Equal(TestIndexPath, IndexService.CurrentPath);
    }

    [AvaloniaFact]
    public async Task ReopenWithToggledMode_SwitchesReadOnlyMode()
    {
        // Arrange
        await OpenTestIndexAsync(readOnly: true);
        Assert.True(IndexService.IsReadOnly);

        // Act
        await IndexService.ReopenWithToggledModeAsync();

        // Assert
        Assert.False(IndexService.IsReadOnly);
        Assert.True(IndexService.IsOpen);
    }

    [AvaloniaFact]
    public async Task CloseIndex_PublishesIndexClosedEvent()
    {
        // Arrange
        await OpenTestIndexAsync();
        var closedEventReceived = false;
        IndexService.IndexClosed.Subscribe(_ => closedEventReceived = true);

        // Act
        IndexService.Close();

        // Assert
        Assert.True(closedEventReceived);
        Assert.False(IndexService.IsOpen);
        Assert.Null(IndexService.CurrentReader);
        Assert.Null(IndexService.CurrentDirectory);
    }

    [AvaloniaFact]
    public async Task OpenIndex_ReadDocuments_ReturnsExpectedCount()
    {
        // Act
        await OpenTestIndexAsync();

        // Assert
        Assert.NotNull(IndexService.CurrentReader);
        Assert.Equal(20, IndexService.CurrentReader.NumDocs);
        Assert.Equal(20, IndexService.CurrentReader.MaxDoc);
    }

    [AvaloniaFact]
    public async Task OpenIndex_HasExpectedFields()
    {
        // Act
        await OpenTestIndexAsync();

        // Assert
        var reader = IndexService.CurrentReader;
        Assert.NotNull(reader);

        var fields = new HashSet<string>();
        var fieldInfos = MultiFields.GetMergedFieldInfos(reader);

        foreach (var fieldInfo in fieldInfos)
        {
            fields.Add(fieldInfo.Name);
        }

        // Verify expected fields exist
        Assert.Contains("id", fields);
        Assert.Contains("name", fields);
        Assert.Contains("description", fields);
        Assert.Contains("category", fields);
        Assert.Contains("price", fields);
        Assert.Contains("in_stock", fields);
    }
}
