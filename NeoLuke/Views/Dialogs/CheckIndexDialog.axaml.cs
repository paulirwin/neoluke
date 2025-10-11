using Avalonia.Controls;

namespace NeoLuke.Views.Dialogs;

public partial class CheckIndexDialog : Window
{
    public CheckIndexDialog()
    {
        InitializeComponent();

        // Wire up the close button
        var closeButton = this.FindControl<Button>("CloseButton");
        if (closeButton != null)
        {
            closeButton.Click += (s, e) => Close();
        }
    }
}
