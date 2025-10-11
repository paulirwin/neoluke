using Avalonia.Controls;

namespace NeoLuke.Views.Dialogs;

public partial class ExportTermsDialog : Window
{
    public ExportTermsDialog()
    {
        InitializeComponent();

        // Wire up the Close button
        var closeButton = this.FindControl<Button>("CloseButton");
        if (closeButton != null)
        {
            closeButton.Click += (s, e) => Close();
        }
    }
}
