using Avalonia.Controls;

namespace NeoLuke.Views.Dialogs;

public partial class OptimizeIndexDialog : Window
{
    public OptimizeIndexDialog()
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
