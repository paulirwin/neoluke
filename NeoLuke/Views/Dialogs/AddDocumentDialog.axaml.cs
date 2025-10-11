using Avalonia;
using Avalonia.Controls;
using NeoLuke.ViewModels;

namespace NeoLuke.Views.Dialogs;

public partial class AddDocumentDialog : Window
{
    public AddDocumentDialog()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    public AddDocumentDialog(AddDocumentDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
