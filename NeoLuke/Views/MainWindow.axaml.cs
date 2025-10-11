using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using NeoLuke.Models.Overview;
using NeoLuke.Models.Documents;
using NeoLuke.Models.Search;
using NeoLuke.Models.Commits;
using NeoLuke.Models.Tools;
using NeoLuke.ViewModels;
using NeoLuke.Utilities;
using NeoLuke.Views.Dialogs;
using NeoLuke.Services;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Core;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LuceneDirectory = Lucene.Net.Store.Directory;

namespace NeoLuke.Views;

public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private readonly IIndexService _indexService;
    private NativeMenuItem? _reopenToggleModeMenuItem;
    private IDisposable? _indexOpenedSubscription;
    private IDisposable? _indexClosedSubscription;

    public MainWindow()
    {
        _logger = Program.LoggerFactory.CreateLogger<MainWindow>();
        _logger.LogInformation("MainWindow initializing");

        // Use the application-wide IndexService
        _indexService = Program.IndexService;

        // Force load analyzer assemblies for reflection-based discovery
        // This ensures analyzer types are available when AnalyzerDiscovery runs
        ForceLoadAnalyzerAssemblies();

        InitializeComponent();

        // Find and store reference to the toggle mode menu item
        // Navigate through the menu structure: NativeMenu.Menu -> File submenu -> item at index 2
        if (NativeMenu.GetMenu(this) is NativeMenu topMenu &&
            topMenu.Items.Count > 0 &&
            topMenu.Items[0] is NativeMenuItem fileMenu &&
            fileMenu.Menu != null &&
            fileMenu.Menu.Items.Count > 2 &&
            fileMenu.Menu.Items[2] is NativeMenuItem toggleMenuItem)
        {
            _reopenToggleModeMenuItem = toggleMenuItem;
        }

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var versionString = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v0.0.0";
        Title = $"NeoLuke: Lucene.NET Toolbox Project - {versionString}";

        // Log application startup information with dynamically retrieved versions
        var avaloniaVersion = typeof(Application).GetInformationalVersion();
        var dotnetVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        var luceneVersion = typeof(IndexReader).GetInformationalVersion();

        _logger.LogInformation(
            "Application Startup:\n" +
            "  Version: {Version}\n" +
            "  Framework: Avalonia UI {AvaloniaVersion}\n" +
            "  Target: {DotNetVersion}\n" +
            "  Lucene.NET: {LuceneVersion}\n" +
            "  Platform: {Platform}\n" +
            "  OS: {OS}\n" +
            "  Architecture: {Arch}\n" +
            "  Working Directory: {WorkingDir}",
            versionString,
            avaloniaVersion,
            dotnetVersion,
            luceneVersion,
            Environment.OSVersion.Platform,
            Environment.OSVersion.VersionString,
            System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture,
            Environment.CurrentDirectory);

        // Initialize LogsView with LoggingService
        var logsViewModel = new LogsViewModel(Program.LoggingService);
        LogsView.DataContext = logsViewModel;

        // Initialize SearchView with SearchViewModel
        var searchViewModel = new SearchViewModel();
        searchViewModel.SetParentWindow(this);
        searchViewModel.SetNavigateToDocumentCallback(NavigateToDocument);
        SearchView.DataContext = searchViewModel;

        // Initialize CommitsView with CommitsViewModel
        var commitsViewModel = new CommitsViewModel();
        CommitsView.DataContext = commitsViewModel;

        // Initialize AnalysisView with AnalysisViewModel
        var analysisViewModel = new AnalysisViewModel();
        AnalysisView.DataContext = analysisViewModel;

        // Initialize MoreLikeThisView with MoreLikeThisViewModel
        var moreLikeThisViewModel = new MoreLikeThisViewModel();
        MoreLikeThisView.DataContext = moreLikeThisViewModel;

        // Subscribe to IndexService events
        _indexOpenedSubscription = _indexService.IndexOpened.Subscribe(OnIndexOpened);
        _indexClosedSubscription = _indexService.IndexClosed.Subscribe(OnIndexClosed);

        // Set clipboard when window is opened
        Opened += async (s, e) =>
        {
            logsViewModel.SetClipboard(Clipboard);
            _logger.LogInformation("MainWindow opened");
            await ShowIndexPathDialogAsync();
        };

        // Cleanup subscriptions when window is closed
        Closed += (s, e) =>
        {
            _indexOpenedSubscription?.Dispose();
            _indexClosedSubscription?.Dispose();
        };
    }

    /// <summary>
    /// Forces the CLR to load analyzer assemblies by referencing their types.
    /// This is necessary for reflection-based discovery to work.
    /// </summary>
    private void ForceLoadAnalyzerAssemblies()
    {
        _logger.LogDebug("Force loading analyzer assemblies for reflection discovery");

        // Reference common analyzer types to ensure their assemblies are loaded
        _ = typeof(StandardAnalyzer);
        _ = typeof(WhitespaceAnalyzer);
        _ = typeof(SimpleAnalyzer);
        _ = typeof(KeywordAnalyzer);
        _ = typeof(StopAnalyzer);

        _logger.LogDebug("Analyzer assemblies loaded");
    }

    public async void OnOpenIndexClick(object? sender, EventArgs e)
    {
        _logger.LogInformation("User requested to open index via File menu");
        await ShowIndexPathDialogAsync();
    }

    public async void OnCloseIndexClick(object? sender, EventArgs e)
    {
        _logger.LogInformation("User requested to close index via File menu");
        _indexService.Close();
        await ShowIndexPathDialogAsync();
    }

    public async void OnReopenIndexClick(object? sender, EventArgs e)
    {
        await ReopenCurrentIndexAsync();
    }

    public async void OnReopenToggleModeClick(object? sender, EventArgs e)
    {
        await ReopenCurrentIndexWithToggleModeAsync();
    }

    public async Task ReopenCurrentIndexAsync()
    {
        try
        {
            await _indexService.ReopenAsync();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot reopen index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reopen index");
            await ShowErrorDialogAsync("Error Reopening Index", ex.Message);
        }
    }

    public async Task ReopenCurrentIndexWithToggleModeAsync()
    {
        try
        {
            await _indexService.ReopenWithToggledModeAsync();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot reopen index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reopen index with toggled mode");
            await ShowErrorDialogAsync("Error Reopening Index", ex.Message);
        }
    }

    public void OnLightModeClick(object? sender, EventArgs e)
    {
        _logger.LogInformation("User switched to Light Mode");
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
    }

    public void OnDarkModeClick(object? sender, EventArgs e)
    {
        _logger.LogInformation("User switched to Dark Mode");
        Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
    }

    public void OnSystemThemeClick(object? sender, EventArgs e)
    {
        _logger.LogInformation("User switched to System Theme");
        Application.Current!.RequestedThemeVariant = ThemeVariant.Default;
    }

    public async void OnOptimizeIndexClick(object? sender, EventArgs e)
    {
        _logger.LogInformation("User requested to optimize index via Tools menu");
        await ShowOptimizeIndexDialogAsync();
    }

    public async void OnCheckIndexClick(object? sender, EventArgs e)
    {
        _logger.LogInformation("User requested to check index via Tools menu");
        await ShowCheckIndexDialogAsync();
    }

    public async void OnExportTermsClick(object? sender, EventArgs e)
    {
        _logger.LogInformation("User requested to export terms via Tools menu");
        await ShowExportTermsDialogAsync();
    }

    public async void OnThirdPartyNoticesClick(object? sender, EventArgs e)
    {
        _logger.LogInformation("User requested Third-Party Notices dialog via Help menu");
        await ShowThirdPartyNoticesDialogAsync();
    }

    public async void OnAboutClick(object? sender, EventArgs e)
    {
        _logger.LogInformation("User requested About dialog via Help menu");
        await ShowAboutDialogAsync();
    }

    private void UpdateReopenToggleModeMenuItem()
    {
        if (_reopenToggleModeMenuItem != null)
        {
            _reopenToggleModeMenuItem.Header = _indexService.IsReadOnly
                ? "Reopen current index as read/write"
                : "Reopen current index as read-only";

            _logger.LogDebug("Updated reopen toggle menu item to: {Header}", _reopenToggleModeMenuItem.Header);
        }
    }

    private async Task ShowIndexPathDialogAsync()
    {
        var dialog = new IndexPathDialog();
        var result = await dialog.ShowDialog<(string Path, bool IsReadOnly, System.Type? DirectoryType)?>(this);

        if (result.HasValue && !string.IsNullOrEmpty(result.Value.Path))
        {
            try
            {
                await _indexService.OpenAsync(result.Value.Path, result.Value.DirectoryType, result.Value.IsReadOnly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open index at path: {IndexPath}", result.Value.Path);
                await ShowErrorDialogAsync("Error Opening Index", ex.Message);
            }
        }
    }

    private async Task ShowOptimizeIndexDialogAsync()
    {
        // Check if index is open
        if (!_indexService.IsOpen || string.IsNullOrEmpty(_indexService.CurrentPath))
        {
            _logger.LogWarning("Cannot optimize: no index is currently open");

            var errorDialog = new Window
            {
                Title = "No Index Open",
                Width = 400,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 10,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Please open an index before using the optimize function.",
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
                        }
                    }
                }
            };

            var okButton = (Button)((StackPanel)errorDialog.Content).Children[1];
            okButton.Click += (s, e) => errorDialog.Close();

            await errorDialog.ShowDialog(this);
            return;
        }

        // Check if index is in read-only mode
        if (_indexService.IsReadOnly)
        {
            _logger.LogWarning("Cannot optimize: index is opened in read-only mode");

            var errorDialog = new Window
            {
                Title = "Read-Only Mode",
                Width = 400,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 10,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Cannot optimize index when opened in read-only mode.",
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        },
                        new TextBlock
                        {
                            Text = "Please reopen the index in read/write mode using File > Reopen current index as read/write.",
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
                        }
                    }
                }
            };

            var okButton = (Button)((StackPanel)errorDialog.Content).Children[2];
            okButton.Click += (s, e) => errorDialog.Close();

            await errorDialog.ShowDialog(this);
            return;
        }

        try
        {
            // Create the model
            var model = new IndexToolsModel(_indexService.CurrentDirectory!, _indexService.IsReadOnly);

            // Create the ViewModel with a callback to reopen the index after optimization
            var viewModel = new OptimizeIndexDialogViewModel(
                model,
                _indexService.CurrentPath!,
                async () => await ReopenCurrentIndexAsync()
            );

            // Create and show the dialog
            var dialog = new OptimizeIndexDialog
            {
                DataContext = viewModel
            };

            await dialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show optimize index dialog");

            var errorDialog = new Window
            {
                Title = "Error",
                Width = 400,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 10,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Failed to open optimize dialog:",
                            FontWeight = Avalonia.Media.FontWeight.SemiBold
                        },
                        new TextBlock
                        {
                            Text = ex.Message,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
                        }
                    }
                }
            };

            var okButton = (Button)((StackPanel)errorDialog.Content).Children[2];
            okButton.Click += (s, e) => errorDialog.Close();

            await errorDialog.ShowDialog(this);
        }
    }

    private async Task ShowCheckIndexDialogAsync()
    {
        // Check if index is open
        if (!_indexService.IsOpen || string.IsNullOrEmpty(_indexService.CurrentPath))
        {
            _logger.LogWarning("Cannot check index: no index is currently open");

            var errorDialog = new Window
            {
                Title = "No Index Open",
                Width = 400,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 10,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Please open an index before using the check index function.",
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
                        }
                    }
                }
            };

            var okButton = (Button)((StackPanel)errorDialog.Content).Children[1];
            okButton.Click += (s, e) => errorDialog.Close();

            await errorDialog.ShowDialog(this);
            return;
        }

        try
        {
            // Create the model
            var model = new IndexToolsModel(_indexService.CurrentDirectory!, _indexService.IsReadOnly);

            // Create the ViewModel with a callback to reopen the index after repair
            var viewModel = new CheckIndexDialogViewModel(
                model,
                _indexService.CurrentPath!,
                async () => await ReopenCurrentIndexAsync()
            );

            // Create and show the dialog
            var dialog = new CheckIndexDialog
            {
                DataContext = viewModel
            };

            await dialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show check index dialog");

            var errorDialog = new Window
            {
                Title = "Error",
                Width = 400,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 10,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Failed to open check index dialog:",
                            FontWeight = Avalonia.Media.FontWeight.SemiBold
                        },
                        new TextBlock
                        {
                            Text = ex.Message,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
                        }
                    }
                }
            };

            var okButton = (Button)((StackPanel)errorDialog.Content).Children[2];
            okButton.Click += (s, e) => errorDialog.Close();

            await errorDialog.ShowDialog(this);
        }
    }

    private async Task ShowExportTermsDialogAsync()
    {
        // Check if index is open
        if (!_indexService.IsOpen || _indexService.CurrentReader == null || string.IsNullOrEmpty(_indexService.CurrentPath))
        {
            _logger.LogWarning("Cannot export terms: no index is currently open");

            var errorDialog = new Window
            {
                Title = "No Index Open",
                Width = 400,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 10,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Please open an index before using the export terms function.",
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
                        }
                    }
                }
            };

            var okButton = (Button)((StackPanel)errorDialog.Content).Children[1];
            okButton.Click += (s, e) => errorDialog.Close();

            await errorDialog.ShowDialog(this);
            return;
        }

        try
        {
            // Create the model with IndexReader
            var model = new IndexToolsModel(_indexService.CurrentReader!, _indexService.CurrentDirectory!, _indexService.IsReadOnly);

            // Create the ViewModel
            var viewModel = new ExportTermsDialogViewModel(model, _indexService.CurrentPath!, this);

            // Create and show the dialog
            var dialog = new ExportTermsDialog
            {
                DataContext = viewModel
            };

            await dialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show export terms dialog");

            var errorDialog = new Window
            {
                Title = "Error",
                Width = 400,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 10,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Failed to open export terms dialog:",
                            FontWeight = Avalonia.Media.FontWeight.SemiBold
                        },
                        new TextBlock
                        {
                            Text = ex.Message,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
                        }
                    }
                }
            };

            var okButton = (Button)((StackPanel)errorDialog.Content).Children[2];
            okButton.Click += (s, e) => errorDialog.Close();

            await errorDialog.ShowDialog(this);
        }
    }

    private async Task ShowThirdPartyNoticesDialogAsync()
    {
        try
        {
            var viewModel = new ThirdPartyNoticesDialogViewModel();
            var dialog = new ThirdPartyNoticesDialog
            {
                DataContext = viewModel
            };

            await dialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show Third-Party Notices dialog");
        }
    }

    private async Task ShowAboutDialogAsync()
    {
        try
        {
            var viewModel = new AboutDialogViewModel();
            var dialog = new AboutDialog
            {
                DataContext = viewModel
            };

            await dialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show About dialog");
        }
    }

    private async Task ShowErrorDialogAsync(string title, string message)
    {
        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        var errorDialog = new Window
        {
            Title = title,
            Width = 400,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Spacing = 10,
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    okButton
                }
            }
        };

        okButton.Click += (s, e) => errorDialog.Close();
        await errorDialog.ShowDialog(this);
    }

    private async void OnIndexOpened(IndexOpenedEvent e)
    {
        _logger.LogInformation("Index opened event received for path: {IndexPath}", e.Info.Path);

        // Update the menu item to reflect current mode
        UpdateReopenToggleModeMenuItem();

        // Update Overview tab
        var overviewModel = new OverviewModel(e.Info.Reader, e.Info.Path, e.Info.Directory);
        var overviewViewModel = OverviewView.DataContext as OverviewViewModel;
        if (overviewViewModel != null)
        {
            await overviewViewModel.LoadIndexInfoAsync(overviewModel);
        }

        // Update Documents tab
        var documentsModel = new DocumentsModel(e.Info.Reader, e.Info.Directory, e.Info.IsReadOnly);
        var documentsViewModel = DocumentsView.DataContext as DocumentsViewModel;
        if (documentsViewModel != null)
        {
            await documentsViewModel.LoadIndexAsync(documentsModel);
        }

        // Update Search tab
        var searchModel = new SearchModel(e.Info.Reader);
        var searchViewModel = SearchView.DataContext as SearchViewModel;
        searchViewModel?.LoadIndex(searchModel);

        // Update More Like This tab
        var mltModel = new MoreLikeThisModel(e.Info.Reader);
        var mltSearchModel = new SearchModel(e.Info.Reader);
        var mltViewModel = MoreLikeThisView.DataContext as MoreLikeThisViewModel;
        mltViewModel?.LoadIndex(mltModel, mltSearchModel);

        // Update Commits tab
        var commitsModel = new CommitsModel(e.Info.Directory);
        var commitsViewModel = CommitsView.DataContext as CommitsViewModel;
        if (commitsViewModel != null)
        {
            await commitsViewModel.LoadIndexAsync(commitsModel);
        }
    }

    private void OnIndexClosed(IndexClosedEvent e)
    {
        _logger.LogInformation("Index closed event received");

        // Clear Overview tab
        var overviewViewModel = OverviewView.DataContext as OverviewViewModel;
        overviewViewModel?.ClearIndexInfo();

        // Clear Documents tab
        var documentsViewModel = DocumentsView.DataContext as DocumentsViewModel;
        documentsViewModel?.ClearIndex();

        // Clear Search tab
        var searchViewModel = SearchView.DataContext as SearchViewModel;
        searchViewModel?.ClearIndex();

        // Clear More Like This tab
        var mltViewModel = MoreLikeThisView.DataContext as MoreLikeThisViewModel;
        mltViewModel?.ClearIndex();

        // Clear Commits tab
        var commitsViewModel = CommitsView.DataContext as CommitsViewModel;
        commitsViewModel?.ClearIndex();
    }

    /// <summary>
    /// Navigates to the Documents tab and selects a specific document
    /// </summary>
    public void NavigateToDocument(int docId)
    {
        _logger.LogInformation("Navigating to Documents tab for doc ID: {DocId}", docId);

        // Switch to Documents tab (index 1)
        MainTabControl.SelectedIndex = 1;

        // Set the document in the DocumentsViewModel
        var documentsViewModel = DocumentsView.DataContext as DocumentsViewModel;
        if (documentsViewModel != null && documentsViewModel.IsIndexLoaded)
        {
            documentsViewModel.CurrentDocId = docId;
            _logger.LogInformation("Document {DocId} selected in Documents tab", docId);
        }
        else
        {
            _logger.LogWarning("Cannot navigate to document: DocumentsViewModel not loaded");
        }
    }

    /// <summary>
    /// Navigates to the More Like This tab and sets the document ID
    /// </summary>
    public void NavigateToMoreLikeThis(int docId)
    {
        _logger.LogInformation("Navigating to More Like This tab for doc ID: {DocId}", docId);

        // Switch to More Like This tab (index 3)
        MainTabControl.SelectedIndex = 3;

        // Set the document ID in the MoreLikeThisViewModel
        var mltViewModel = MoreLikeThisView.DataContext as MoreLikeThisViewModel;
        if (mltViewModel != null && mltViewModel.IsIndexLoaded)
        {
            mltViewModel.SetDocumentId(docId);
            _logger.LogInformation("Document {DocId} set for More Like This", docId);
        }
        else
        {
            _logger.LogWarning("Cannot navigate to More Like This: MoreLikeThisViewModel not loaded");
        }
    }

    /// <summary>
    /// Navigates to the Search tab and executes a field:term search
    /// </summary>
    public void SearchByTerm(string fieldName, string term)
    {
        _logger.LogInformation("Navigating to Search tab for {Field}:{Term}", fieldName, term);

        // Switch to Search tab (index 2)
        MainTabControl.SelectedIndex = 2;

        // Set the search parameters and execute search
        var searchViewModel = SearchView.DataContext as SearchViewModel;
        if (searchViewModel != null && searchViewModel.IsIndexLoaded)
        {
            // Set the field
            searchViewModel.SelectedField = fieldName;

            // Set the query expression as field:term
            searchViewModel.QueryExpression = term;

            // Execute the search
            if (searchViewModel.SearchCommand.CanExecute(null))
            {
                searchViewModel.SearchCommand.Execute(null);
                _logger.LogInformation("Search executed for {Field}:{Term}", fieldName, term);
            }
        }
        else
        {
            _logger.LogWarning("Cannot search: SearchViewModel not loaded");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _logger.LogInformation("MainWindow closing");
        // IndexService will be disposed by the application shutdown
        // Subscriptions are already cleaned up in Closed event handler
        base.OnClosed(e);
    }
}
