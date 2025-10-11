using Avalonia.Controls;
using NeoLuke.ViewModels;

namespace NeoLuke.Views;

public partial class OverviewView : UserControl
{
    private OverviewViewModel? _viewModel;

    public OverviewView()
    {
        InitializeComponent();
        _viewModel = new OverviewViewModel();
        DataContext = _viewModel;

        // Set up callback when the control is loaded
        Loaded += (s, e) =>
        {
            if (TopLevel.GetTopLevel(this) is Window window && _viewModel != null)
            {
                // Set up callback to navigate to Search tab with field:term
                if (window is MainWindow mainWindow)
                {
                    _viewModel.SetSearchByTermCallback((fieldName, term) =>
                        mainWindow.SearchByTerm(fieldName, term));
                }
            }
        };
    }
}
