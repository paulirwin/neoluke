using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NeoLuke.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public ConfirmDialog(string message, string title = "Confirm", string confirmButtonText = "Confirm", string cancelButtonText = "Cancel")
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmButtonText;
        CancelButton.Content = cancelButtonText;
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
