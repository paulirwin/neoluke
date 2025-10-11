using Avalonia.Headless.XUnit;
using NeoLuke.Models.Overview;
using NeoLuke.ViewModels;
using NeoLuke.Views;

namespace NeoLuke.Tests.Integration.Tests;

/// <summary>
/// Integration tests for Overview tab with View and ViewModel
/// </summary>
public class OverviewTabTests : HeadlessTestBase
{
    [AvaloniaFact]
    public async Task OverviewViewModel_LoadIndexInfo_UpdatesProperties()
    {
        // Arrange
        await OpenTestIndexAsync();
        var viewModel = new OverviewViewModel();
        var model = new OverviewModel(
            IndexService.CurrentReader!,
            TestIndexPath,
            IndexService.CurrentDirectory!);

        // Act
        await viewModel.LoadIndexInfoAsync(model);

        // Assert
        Assert.Equal(TestIndexPath, viewModel.IndexPath);
        Assert.Equal("20", viewModel.NumDocuments);
        Assert.NotEmpty(viewModel.FieldStats);
        // TopTerms is populated when a field is selected
        Assert.Empty(viewModel.TopTerms);
    }

    [AvaloniaFact]
    public async Task OverviewViewModel_LoadIndexInfo_PopulatesFieldInformation()
    {
        // Arrange
        await OpenTestIndexAsync();
        var viewModel = new OverviewViewModel();
        var model = new OverviewModel(
            IndexService.CurrentReader!,
            TestIndexPath,
            IndexService.CurrentDirectory!);

        // Act
        await viewModel.LoadIndexInfoAsync(model);

        // Assert
        Assert.NotEmpty(viewModel.FieldStats);

        // Check that common fields exist
        var fieldNames = viewModel.FieldStats.Select(f => f.FieldName).ToList();
        Assert.Contains("name", fieldNames);
        Assert.Contains("description", fieldNames);
        Assert.Contains("category", fieldNames);
        Assert.Contains("price", fieldNames);
    }

    [AvaloniaFact]
    public async Task OverviewView_WithViewModel_RendersWithoutError()
    {
        // Arrange
        await OpenTestIndexAsync();
        var viewModel = new OverviewViewModel();
        var model = new OverviewModel(
            IndexService.CurrentReader!,
            TestIndexPath,
            IndexService.CurrentDirectory!);
        await viewModel.LoadIndexInfoAsync(model);

        // Act
        var view = new OverviewView
        {
            DataContext = viewModel
        };

        // Assert
        Assert.NotNull(view);
        Assert.Same(viewModel, view.DataContext);
    }

    [AvaloniaFact]
    public async Task OverviewViewModel_ClearIndexInfo_ResetsProperties()
    {
        // Arrange
        await OpenTestIndexAsync();
        var viewModel = new OverviewViewModel();
        var model = new OverviewModel(
            IndexService.CurrentReader!,
            TestIndexPath,
            IndexService.CurrentDirectory!);
        await viewModel.LoadIndexInfoAsync(model);

        // Verify data is loaded
        Assert.NotNull(viewModel.IndexPath);
        Assert.NotEmpty(viewModel.FieldStats);

        // Act
        viewModel.ClearIndexInfo();

        // Assert
        Assert.Empty(viewModel.IndexPath);
        Assert.Empty(viewModel.FieldStats);
        Assert.Empty(viewModel.NumDocuments);
    }

    [AvaloniaFact]
    public async Task OverviewViewModel_LoadIndexInfo_PopulatesTopTermsWhenFieldSelected()
    {
        // Arrange
        await OpenTestIndexAsync();
        var viewModel = new OverviewViewModel();
        var model = new OverviewModel(
            IndexService.CurrentReader!,
            TestIndexPath,
            IndexService.CurrentDirectory!);
        await viewModel.LoadIndexInfoAsync(model);

        // TopTerms should be empty initially
        Assert.Empty(viewModel.TopTerms);

        // Act - Select a field to trigger top terms loading
        viewModel.SelectedField = viewModel.FieldStats.FirstOrDefault(f => f.FieldName == "name");

        // Wait for async loading
        await Task.Delay(500);

        // Assert
        Assert.NotEmpty(viewModel.TopTerms);
    }

    [AvaloniaFact]
    public async Task OverviewViewModel_HasValidDirectoryImplementation()
    {
        // Arrange
        await OpenTestIndexAsync();
        var viewModel = new OverviewViewModel();
        var model = new OverviewModel(
            IndexService.CurrentReader!,
            TestIndexPath,
            IndexService.CurrentDirectory!);

        // Act
        await viewModel.LoadIndexInfoAsync(model);

        // Assert
        Assert.NotNull(viewModel.DirectoryImpl);
        Assert.NotEmpty(viewModel.DirectoryImpl);
        // Should be a fully qualified type name
        Assert.Contains("Lucene.Net.Store", viewModel.DirectoryImpl);
    }

    [AvaloniaFact]
    public async Task OverviewViewModel_HasValidIndexVersion()
    {
        // Arrange
        await OpenTestIndexAsync();
        var viewModel = new OverviewViewModel();
        var model = new OverviewModel(
            IndexService.CurrentReader!,
            TestIndexPath,
            IndexService.CurrentDirectory!);

        // Act
        await viewModel.LoadIndexInfoAsync(model);

        // Assert
        Assert.NotNull(viewModel.IndexVersion);
        Assert.NotEmpty(viewModel.IndexVersion);
    }
}
