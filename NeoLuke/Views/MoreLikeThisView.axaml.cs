using Avalonia.Controls;
using NeoLuke.ViewModels;

namespace NeoLuke.Views;

public partial class MoreLikeThisView : UserControl
{
    public MoreLikeThisView()
    {
        InitializeComponent();
        DataContext = new MoreLikeThisViewModel();
    }

    public MoreLikeThisViewModel ViewModel => (MoreLikeThisViewModel)DataContext!;
}
