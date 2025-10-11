using Avalonia.Controls;
using Avalonia.Interactivity;
using NeoLuke.Models.Search;
using NeoLuke.ViewModels;
using System;
using System.Globalization;

namespace NeoLuke.Views.Dialogs;

public partial class SearchSettingsDialog : Window
{
    public SearchSettingsDialog()
    {
        InitializeComponent();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SearchSettingsDialogViewModel viewModel)
        {
            Close(viewModel.GetSettings());
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void OnResetLocaleClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SearchSettingsDialogViewModel viewModel)
        {
            viewModel.LocaleName = CultureInfo.CurrentCulture.Name;
        }
    }

    private void OnResetTimeZoneClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SearchSettingsDialogViewModel viewModel)
        {
            viewModel.TimeZoneId = TimeZoneInfo.Local.Id;
        }
    }
}
