using Avalonia.Controls;
using Avalonia.Interactivity;
using NeoLuke.ViewModels;

namespace NeoLuke.Views.Dialogs;

public partial class ExplainDialog : Window
{
    public ExplainDialog()
    {
        InitializeComponent();
    }

    private async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ExplainDialogViewModel viewModel && Clipboard != null)
        {
            await viewModel.CopyToClipboardAsync(Clipboard);
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
