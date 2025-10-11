using Avalonia.Controls;
using Avalonia.Interactivity;
using NeoLuke.ViewModels;

namespace NeoLuke.Views.Dialogs;

public partial class TokenDetailsDialog : Window
{
    public TokenDetailsDialog()
    {
        InitializeComponent();
    }

    public TokenDetailsDialog(TokenDetailsDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
