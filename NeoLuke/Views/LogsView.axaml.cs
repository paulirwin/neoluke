using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NeoLuke.Models.Logging;

namespace NeoLuke.Views;

public partial class LogsView : UserControl
{
    public LogsView()
    {
        InitializeComponent();
        LogsDataGrid.DoubleTapped += OnLogEntryDoubleTapped;
    }

    private async void OnLogEntryDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (LogsDataGrid.SelectedItem is LogEntry logEntry && this.VisualRoot is Window owner)
        {
            var dialog = new LogDetailsDialog(logEntry);
            await dialog.ShowDialog(owner);
        }
    }
}
