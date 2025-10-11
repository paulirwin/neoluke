using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using NeoLuke.ViewModels;
using NeoLuke.Views.Dialogs;

namespace NeoLuke.Views;

public partial class DocumentsView : UserControl
{
    private DocumentsViewModel? _viewModel;

    public DocumentsView()
    {
        InitializeComponent();
        _viewModel = new DocumentsViewModel();
        DataContext = _viewModel;

        // Set clipboard and dialog when the control is loaded
        Loaded += (s, e) =>
        {
            if (TopLevel.GetTopLevel(this) is Window window && _viewModel != null)
            {
                _viewModel.SetClipboard(window.Clipboard);
                _viewModel.SetConfirmDeleteDialog(async () => await ShowConfirmDeleteDialog(window));
                _viewModel.SetAddDocumentDialog(async () => await ShowAddDocumentDialog(window));

                // Set up callback to reload index after changes (e.g., deletions, additions)
                if (window is MainWindow mainWindow)
                {
                    _viewModel.SetIndexChangedCallback(async () => await mainWindow.ReopenCurrentIndexAsync());
                    _viewModel.SetMoreLikeThisCallback(docId => mainWindow.NavigateToMoreLikeThis(docId));
                }
            }
        };

        // Handle selection changes on the DataGrid
        FieldsDataGrid.SelectionChanged += (s, e) =>
        {
            if (_viewModel != null)
            {
                _viewModel.SelectedFields = FieldsDataGrid.SelectedItems;
            }
        };
    }

    private async Task<bool> ShowConfirmDeleteDialog(Window owner)
    {
        if (_viewModel == null)
            return false;

        var dialog = new ConfirmDialog(
            $"Are you sure you want to delete document {_viewModel.CurrentDocId}?\n\nThis action cannot be undone.",
            "Confirm Delete",
            "Delete",
            "Cancel"
        );

        var result = await dialog.ShowDialog<bool?>(owner);
        return result == true;
    }

    private async Task<bool> ShowAddDocumentDialog(Window owner)
    {
        if (_viewModel == null)
            return false;

        // Get field template from a non-deleted document to pre-populate the dialog
        var templateFields = _viewModel.GetFieldTemplateForNewDocument();

        // Create the dialog with the template fields
        var dialogViewModel = new AddDocumentDialogViewModel(templateFields);
        var dialog = new AddDocumentDialog(dialogViewModel);

        // Set close action
        dialogViewModel.SetCloseAction(() => dialog.Close());

        // Show the dialog
        await dialog.ShowDialog(owner);

        // If the user added the document, process it
        if (dialogViewModel.WasAdded)
        {
            var validFields = dialogViewModel.GetValidFields();

            // Need to get the DocumentsModel from somewhere to call AddDocument
            // We'll need to pass this through or access it differently
            // For now, let's get it from the MainWindow
            if (owner is MainWindow mainWindow)
            {
                // Get the DocumentsModel from the MainWindow
                // We need to add a method to expose this or call it directly
                try
                {
                    // Call AddDocument through the ViewModel which has access to the model
                    await Task.Run(() => _viewModel.AddDocumentToModel(validFields));
                    return true;
                }
                catch (System.Exception ex)
                {
                    // Show error dialog
                    var errorDialog = new Window
                    {
                        Title = "Error Adding Document",
                        Width = 400,
                        SizeToContent = SizeToContent.Height,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Content = new StackPanel
                        {
                            Margin = new Avalonia.Thickness(20),
                            Spacing = 10,
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = "Failed to add document:",
                                    FontWeight = Avalonia.Media.FontWeight.SemiBold
                                },
                                new TextBlock
                                {
                                    Text = ex.Message,
                                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                                }
                            }
                        }
                    };

                    await errorDialog.ShowDialog(owner);
                    return false;
                }
            }
        }

        return false;
    }
}
