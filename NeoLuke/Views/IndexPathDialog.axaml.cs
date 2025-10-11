using Avalonia.Controls;
using NeoLuke.ViewModels;

namespace NeoLuke.Views;

public partial class IndexPathDialog : Window
{
    public IndexPathDialog()
    {
        InitializeComponent();
        DataContext = new IndexPathDialogViewModel(this);
    }
}
