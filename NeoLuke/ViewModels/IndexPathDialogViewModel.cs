using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuceneDirectory = Lucene.Net.Store.Directory;

namespace NeoLuke.ViewModels;

public class DirectoryImplementationInfo
{
    public Type Type { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
}

public partial class IndexPathDialogViewModel : ViewModelBase
{
    private readonly Window _window;

    [ObservableProperty]
    private string _indexPath = string.Empty;

    [ObservableProperty]
    private bool _isReadOnly = true;

    [ObservableProperty]
    private ObservableCollection<DirectoryImplementationInfo> _directoryImplementations = [];

    [ObservableProperty]
    private DirectoryImplementationInfo? _selectedDirectoryImplementation;

    public IndexPathDialogViewModel(Window window)
    {
        _window = window;
        LoadDirectoryImplementations();
        SetDefaultPath();
    }

    private void SetDefaultPath()
    {
        // Try to find the source repo root and demo directory
        var repoRoot = FindRepoRoot();
        if (repoRoot != null)
        {
            var demoPath = System.IO.Path.Combine(repoRoot, "demo");
            if (System.IO.Directory.Exists(demoPath))
            {
                IndexPath = demoPath;
            }
        }
    }

    private static string? FindRepoRoot()
    {
        var currentDir = System.IO.Directory.GetCurrentDirectory();

        while (currentDir != null)
        {
            // Look for .git directory or NeoLuke.csproj as indicators of repo root
            if (System.IO.Directory.Exists(System.IO.Path.Combine(currentDir, ".git")) ||
                System.IO.File.Exists(System.IO.Path.Combine(currentDir, "NeoLuke", "NeoLuke.csproj")))
            {
                return currentDir;
            }

            currentDir = System.IO.Directory.GetParent(currentDir)?.FullName;
        }

        return null;
    }

    private void LoadDirectoryImplementations()
    {
        var directoryType = typeof(LuceneDirectory);
        var implementations = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    return [];
                }
            })
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                directoryType.IsAssignableFrom(type))
            .Select(type => new DirectoryImplementationInfo
            {
                Type = type,
                DisplayName = type.FullName ?? type.Name
            })
            .OrderBy(info => info.DisplayName)
            .ToList();

        DirectoryImplementations = new ObservableCollection<DirectoryImplementationInfo>(implementations);

        // Select SimpleFSDirectory by default if available
        SelectedDirectoryImplementation = DirectoryImplementations
            .FirstOrDefault(d => d.Type.Name == "SimpleFSDirectory")
            ?? DirectoryImplementations.FirstOrDefault();
    }

    [RelayCommand]
    private async Task Browse()
    {
        var storageProvider = _window.StorageProvider;

        var result = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Lucene Index Directory",
            AllowMultiple = false
        });

        if (result.Count > 0)
        {
            IndexPath = result[0].Path.LocalPath;
        }
    }

    [RelayCommand]
    private async Task Ok()
    {
        // Validate that the path exists
        if (string.IsNullOrWhiteSpace(IndexPath))
        {
            await ShowErrorMessage("Please enter a directory path.");
            return;
        }

        if (!System.IO.Directory.Exists(IndexPath))
        {
            await ShowErrorMessage($"The directory does not exist:\n{IndexPath}");
            return;
        }

        _window.Close((IndexPath, IsReadOnly, SelectedDirectoryImplementation?.Type));
    }

    private async Task ShowErrorMessage(string message)
    {
        var messageBox = new Window
        {
            Title = "Error",
            Width = 450,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var mainPanel = new StackPanel
        {
            Margin = new Avalonia.Thickness(0)
        };

        // Header with colored background
        var header = new Border
        {
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#D32F2F")),
            Padding = new Avalonia.Thickness(20, 15)
        };

        var headerContent = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 10
        };

        // Error icon (using a red circle with "!")
        var iconBorder = new Border
        {
            Width = 32,
            Height = 32,
            CornerRadius = new Avalonia.CornerRadius(16),
            Background = Avalonia.Media.Brushes.White,
            Child = new TextBlock
            {
                Text = "!",
                FontSize = 20,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#D32F2F")),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            }
        };

        var headerText = new TextBlock
        {
            Text = "Error",
            FontSize = 18,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            Foreground = Avalonia.Media.Brushes.White,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        headerContent.Children.Add(iconBorder);
        headerContent.Children.Add(headerText);
        header.Child = headerContent;

        // Message content
        var contentPanel = new StackPanel
        {
            Margin = new Avalonia.Thickness(25, 20),
            Spacing = 20
        };

        var messageText = new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            FontSize = 14
        };

        contentPanel.Children.Add(messageText);

        // Button panel
        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 10,
            Margin = new Avalonia.Thickness(25, 0, 25, 20)
        };

        var okButton = new Button
        {
            Content = "OK",
            Width = 100,
            Height = 32,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#D32F2F")),
            Foreground = Avalonia.Media.Brushes.White,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            CornerRadius = new Avalonia.CornerRadius(4)
        };

        okButton.Click += (_, _) => messageBox.Close();

        buttonPanel.Children.Add(okButton);

        mainPanel.Children.Add(header);
        mainPanel.Children.Add(contentPanel);
        mainPanel.Children.Add(buttonPanel);

        messageBox.Content = mainPanel;

        await messageBox.ShowDialog(_window);
    }

    [RelayCommand]
    private void Cancel()
    {
        _window.Close(null);
    }
}
