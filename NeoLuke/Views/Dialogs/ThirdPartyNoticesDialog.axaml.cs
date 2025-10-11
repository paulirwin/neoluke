using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NeoLuke.Views.Dialogs;

public partial class ThirdPartyNoticesDialog : Window
{
    public ThirdPartyNoticesDialog()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
