using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NeoLuke.Models.Analysis;
using NeoLuke.ViewModels;
using NeoLuke.Views.Dialogs;

namespace NeoLuke.Views;

public partial class AnalysisView : UserControl
{
    public AnalysisView()
    {
        InitializeComponent();
        // DataContext is set by MainWindow after analyzer assemblies are loaded
    }

    private void DataGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is DataGrid dataGrid && dataGrid.SelectedItem is AnalyzedToken token)
        {
            ShowTokenDetails(token);
        }
    }

    private void DetailsMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        // Get the DataGrid from the visual tree
        if (sender is MenuItem menuItem &&
            menuItem.Parent is ContextMenu contextMenu &&
            contextMenu.PlacementTarget is DataGrid dataGrid &&
            dataGrid.SelectedItem is AnalyzedToken token)
        {
            ShowTokenDetails(token);
        }
    }

    private async void ShowTokenDetails(AnalyzedToken token)
    {
        var viewModel = new TokenDetailsDialogViewModel(token);
        var dialog = new TokenDetailsDialog(viewModel);

        // Get the parent window
        var parentWindow = this.VisualRoot as Window;
        if (parentWindow != null)
        {
            await dialog.ShowDialog(parentWindow);
        }
    }
}
