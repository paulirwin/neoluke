# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NeoLuke is a Lucene.NET Toolbox application built with Avalonia UI (cross-platform desktop framework). It provides tools for working with Lucene.NET indexes, starting with an index directory selection dialog.

**Version:** 0.1.0
**Target Framework:** .NET 9.0
**UI Framework:** Avalonia 11.3.6
**Key Dependencies:** Lucene.Net 4.8.0-beta00017, CommunityToolkit.Mvvm 8.2.1

## Build and Test Commands

```bash
# Build the main application
dotnet build

# Run the application
dotnet run

# Run unit tests
dotnet test NeoLuke.Tests/NeoLuke.Tests.csproj

# Run integration tests
dotnet test NeoLuke.Tests.Integration/NeoLuke.Tests.Integration.csproj

# Run all tests
dotnet test

# Build and run all tests together
dotnet build && dotnet test
```

## Architecture

### MVVM Pattern with Avalonia

The application follows the MVVM (Model-View-ViewModel) pattern:

- **Views/** - AXAML files (Avalonia's XAML) and code-behind (.axaml.cs)
- **ViewModels/** - View models using CommunityToolkit.Mvvm for observable properties and commands
- **Models/** - Domain models (currently empty, prepared for future use)

All ViewModels inherit from `ViewModelBase` which extends `ObservableObject` from CommunityToolkit.Mvvm.

### Key Components

**App Initialization Flow:**
1. `Program.cs` - Entry point, configures Avalonia app builder
2. `App.axaml.cs` - Application class that creates MainWindow and disables duplicate validation plugins
3. `MainWindow` - Shows on startup, immediately triggers `IndexPathDialog`

**Index Path Dialog:**
- Prompts for Lucene index directory on app launch
- Includes "Browse" button using Avalonia's `StorageProvider` API
- "Read-only mode" checkbox (default: checked)
- Expander with "Expert options" containing Directory implementation selector
- Validates path exists before closing, showing styled error dialog on failure

**Directory Implementation Discovery:**
Uses reflection to dynamically find all concrete subclasses of `Lucene.Net.Store.Directory`:
- Scans all loaded assemblies via `AppDomain.CurrentDomain.GetAssemblies()`
- Filters for non-abstract classes assignable to `Lucene.Net.Store.Directory`
- Displays fully qualified type names in dropdown
- Defaults to `SimpleFSDirectory` if available

### Important Implementation Details

**Namespace Conflicts:**
Both `System.IO.Directory` and `Lucene.Net.Store.Directory` exist. Use type alias:
```csharp
using LuceneDirectory = Lucene.Net.Store.Directory;
```
Then use `System.IO.Directory.Exists()` for file system operations and `LuceneDirectory` for Lucene types.

**Avalonia Dialog Patterns:**
- Use `ShowDialog<TResult>()` for modal dialogs that return values
- Window `SizeToContent` property enables auto-resizing
- `WindowStartupLocation.CenterOwner` centers dialogs over parent

**Error Dialog Styling:**
The custom error dialog uses theme-aware colors (respects dark mode) except for intentional red accents:
- Red header bar (#D32F2F) with white text
- White circular icon with red "!" symbol
- Red OK button with white text
- All other text/backgrounds use default theme colors

**Version Display:**
Main window title shows version from assembly: "NeoLuke: Lucene.NET Toolbox Project - v{Major}.{Minor}.{Build}"
Falls back to "v0.0.0" if assembly version unavailable.

**macOS Considerations:**
- `ApplicationName` and `CFBundleName` set to "NeoLuke" for menu bar display
- The `Name` property in App.axaml controls the macOS menu bar app name
- Window `Title` property is separate from app name

**Cross-Platform Menu Configuration:**
NeoLuke uses Avalonia's dual-menu approach for proper cross-platform behavior:
- `NativeMenu.Menu` on the Window - displays in macOS system menu bar
- `NativeMenuBar` control in the content - displays as regular menu bar on Windows, hidden on macOS
- Both menus share the same event handlers in code-behind
- This pattern ensures native menu behavior on macOS while providing a traditional menu bar on Windows

**Search Tab Features:**
- Context menu on search results with two options:
  - **Explain**: Shows hierarchical breakdown of relevance score calculation
    - Uses Lucene's `IndexSearcher.Explain()` to generate score explanations
    - TreeView displays explanation hierarchy with all nodes expanded by default
    - Copy to Clipboard button exports full explanation with indentation
    - Explanation format: `{score_value} {description}` for each node
  - **Show all fields**: Navigates to Documents tab with selected document pre-loaded
    - Switches to Documents tab (MainTabControl.SelectedIndex = 1)
    - Sets DocumentsViewModel.CurrentDocId to display the full document
    - Enables quick navigation from search results to detailed document view

## Test Projects

### Unit Tests (NeoLuke.Tests)

**NeoLuke.Tests** uses xUnit v3 with .NET 10.0:
- Tests Directory implementation discovery via reflection
- Validates that SimpleFSDirectory, MMapDirectory, and NIOFSDirectory are found
- Ensures only concrete (non-abstract) classes are returned
- Uses type alias pattern to avoid namespace conflicts with System.IO.Directory

### Integration Tests (NeoLuke.Tests.Integration)

**NeoLuke.Tests.Integration** provides headless UI and service integration tests:

**Framework:** xUnit 2.9.3 with Avalonia.Headless.XUnit 11.3.7 on .NET 10.0

**Key Dependencies:**
- `Avalonia.Headless.XUnit` - Enables headless testing of Avalonia UI without a display
- `Bogus` 35.6.4 - Generates realistic fake data for test indexes
- `Microsoft.Extensions.Logging.Console` - Provides test logging

**Test Infrastructure:**

**HeadlessTestBase** - Base class for integration tests that provides:
- Automatic test index creation/deletion using TestIndexHelper
- IIndexService instance configured with logging
- OpenTestIndexAsync() helper method
- Proper cleanup in Dispose()

**TestIndexHelper** - Creates realistic test Lucene indexes:
- Uses Bogus with fixed seed (12345) for reproducible test data
- Generates product catalog with 15 fields (name, description, category, price, etc.)
- Default: 20 documents, configurable per test
- Creates indexes in temp directories (neoluke_test_{guid})
- Sample fields: id, name, description, category, price, in_stock, weight, sku, rating, review_count, manufacturer, tags, release_date, is_discounted, discount_percent, final_price

**TestAppBuilder** - Configures Avalonia for headless testing:
- Registers test application using [AvaloniaTestApplication] assembly attribute
- Uses `UseHeadless()` to run without display server

**Test Coverage:**

**MainWindowTests:**
- Window construction and initialization
- Title formatting with version number
- Menu bar presence
- Window lifecycle without showing (avoids triggering IndexPathDialog)

**IndexWorkflowTests:**
- Opening indexes in read-only and read-write modes
- Index state management (IsOpen, CurrentReader, CurrentPath)
- Observable events (IndexOpened, IndexClosed)
- Reopening indexes to refresh reader
- Toggling read-only mode
- Field discovery and document counts

**OverviewTabTests:**
- Overview tab initialization with opened index
- Display of index statistics and metadata

**Running Integration Tests:**
```bash
# Run all integration tests
dotnet test NeoLuke.Tests.Integration/NeoLuke.Tests.Integration.csproj

# Run with detailed logging
dotnet test NeoLuke.Tests.Integration/NeoLuke.Tests.Integration.csproj --logger "console;verbosity=detailed"

# Run specific test class
dotnet test NeoLuke.Tests.Integration/NeoLuke.Tests.Integration.csproj --filter "FullyQualifiedName~IndexWorkflowTests"
```

**Test Attributes:**
- Use `[AvaloniaFact]` instead of `[Fact]` for tests that interact with Avalonia UI
- `[AvaloniaFact]` ensures proper Avalonia dispatcher context for UI operations

**Best Practices:**
- All tests inherit from HeadlessTestBase for consistent setup/teardown
- Tests use realistic indexes generated by Bogus with 20 sample documents
- Each test gets isolated temp index to prevent interference
- Services are tested through their public interfaces (IIndexService)
- Observable events are tested to verify reactive patterns work correctly

---

## Porting Guidelines from Java Luke

This project is a "spirit" port of Apache Lucene's Luke tool (Java) to .NET, not a line-by-line translation. We're modernizing the architecture using Avalonia and MVVM patterns. See `docs/luke-java-analysis.md` for comprehensive analysis.

### Architecture Mapping

**Java Luke → NeoLuke:**

| Java Pattern | .NET Equivalent | Notes |
|--------------|----------------|-------|
| Provider classes | ViewModels | MVVM ViewModels manage view state |
| Observer pattern | INotifyPropertyChanged + ReactiveUI | Modern reactive bindings |
| Operator interfaces | ViewModel Commands/Methods | Direct method calls or ICommand |
| ComponentOperatorRegistry | Dependency Injection | Use .NET DI container |
| MessageBroker | MessageBus (ReactiveUI) | Pub/sub messaging |
| TabSwitcherProxy | ViewModel property binding | Bind to SelectedTab property |
| Factory pattern | Dependency Injection | Let DI create instances |
| Singleton pattern | DI Singleton lifetime | Register as singleton in DI |

### Threading and Lucene.NET

**IMPORTANT:** Lucene.NET does not support async/await. All operations are synchronous and blocking.

**Pattern to Follow:**
```csharp
// ViewModel - wrap blocking operations in Task.Run
private async Task ShowTopTermsAsync()
{
    IsLoading = true;
    try
    {
        // Move blocking Lucene.NET call off UI thread
        var terms = await Task.Run(() =>
            _model.GetTopTerms(SelectedField, NumTerms));

        // Update UI on UI thread
        TopTerms = new ObservableCollection<TermStatsRow>(terms);
    }
    finally
    {
        IsLoading = false;
    }
}
```

**When to use Task.Run():**
- ✅ Opening/closing indexes (I/O bound)
- ✅ Searching indexes (CPU bound)
- ✅ Calculating statistics (CPU bound)
- ✅ Reading large documents
- ❌ Simple property getters (NumDocs, IndexPath, etc.)
- ❌ Operations completing in <50ms

**Model Layer:** Keep synchronous. Models expose synchronous methods that directly call Lucene.NET APIs.

**ViewModel/Service Layer:** Wrap long-running model operations in Task.Run() to keep UI responsive.

### Service Layer Pattern

Create services to manage application state and provide centralized access to Lucene.NET:

```csharp
public class IndexService : IDisposable
{
    private IndexReader _reader;

    // Reactive observables for state changes
    public IObservable<ILukeState> IndexOpened { get; }
    public IObservable<Unit> IndexClosed { get; }

    public async Task OpenAsync(string path, string dirImpl, bool readOnly)
    {
        // Wrap blocking operation
        _reader = await Task.Run(() => IndexUtils.OpenIndex(path, dirImpl));

        // Notify observers
        _indexOpenedSubject.OnNext(new LukeState { ... });
    }
}
```

### Dependency Injection Setup

Register services and ViewModels in `Program.cs` or app startup:

```csharp
services.AddSingleton<IndexService>();
services.AddSingleton<IMessageBus, MessageBus>();
services.AddSingleton<IPreferencesService, PreferencesService>();

services.AddTransient<MainWindowViewModel>();
services.AddTransient<OverviewViewModel>();
// ... other ViewModels
```

### ViewModel Patterns

**Observable Properties:**
```csharp
private string _indexPath;
public string IndexPath
{
    get => _indexPath;
    set => SetProperty(ref _indexPath, value);
}
```

**Commands:**
```csharp
public ICommand OpenIndexCommand { get; }

public MyViewModel()
{
    OpenIndexCommand = new RelayCommand(async () => await OpenIndexAsync());
}
```

**Reactive Subscriptions:**
```csharp
public MyViewModel(IndexService indexService)
{
    // Subscribe to service events
    indexService.IndexOpened
        .Subscribe(state => OnIndexOpened(state));
}
```

### Data Binding in Views

Use XAML data binding instead of manual UI updates:

```xml
<!-- Property binding -->
<TextBlock Text="{Binding IndexPath}" />

<!-- Collection binding -->
<DataGrid ItemsSource="{Binding TermCounts}"
          SelectedItem="{Binding SelectedField}" />

<!-- Command binding -->
<Button Content="Search" Command="{Binding SearchCommand}" />

<!-- Loading state -->
<ProgressBar IsVisible="{Binding IsLoading}" />
```

### Message Bus Pattern

Use for loosely-coupled cross-component communication:

```csharp
// Define messages
public record StatusMessage(string Text);
public record SwitchTabMessage(TabType Tab);
public record BrowseTermMessage(string Field, string Term);

// Publish
_messageBus.Publish(new StatusMessage("Index opened"));

// Subscribe
_messageBus.Listen<BrowseTermMessage>()
    .Subscribe(msg => BrowseTerm(msg.Field, msg.Term));
```

### Code Organization

**Recommended Structure:**
```
NeoLuke/
├── Models/              # Business logic (Lucene.NET interaction)
│   ├── Overview/
│   ├── Documents/
│   ├── Search/
│   └── Common/
├── ViewModels/          # MVVM ViewModels
│   ├── MainWindowViewModel.cs
│   ├── OverviewViewModel.cs
│   └── Dialogs/
├── Views/               # AXAML views
│   ├── MainWindow.axaml
│   ├── OverviewView.axaml
│   └── Dialogs/
├── Services/            # Application services
│   ├── IndexService.cs
│   ├── MessageBus.cs
│   └── PreferencesService.cs
└── Converters/          # XAML value converters
```

### Testing Strategy

**Unit Tests (NeoLuke.Tests):**
- Test ViewModels in isolation (mock services)
- Test Models with real Lucene.NET indexes
- Test Services independently
- Use [Fact] attribute for standard xUnit tests

**Integration Tests (NeoLuke.Tests.Integration):**
- Test ViewModel + Service interaction with real services
- Test Avalonia UI components using headless rendering
- Test with real Lucene indexes generated by Bogus
- Use [AvaloniaFact] attribute for tests requiring Avalonia dispatcher
- Inherit from HeadlessTestBase for automatic index setup/teardown

**Unit Test Example:**
```csharp
[Fact]
public async Task ViewModel_IndexOpened_UpdatesProperties()
{
    var mockIndexService = new Mock<IndexService>();
    var viewModel = new OverviewViewModel(mockIndexService.Object);

    mockIndexService.Raise(x => x.IndexOpened += null, mockState);

    Assert.Equal("/test/path", viewModel.IndexPath);
}
```

**Integration Test Example:**
```csharp
[AvaloniaFact]
public async Task OpenIndex_UpdatesIndexServiceState()
{
    // Arrange - HeadlessTestBase provides TestIndexPath and IndexService

    // Act
    await OpenTestIndexAsync();

    // Assert
    Assert.True(IndexService.IsOpen);
    Assert.NotNull(IndexService.CurrentReader);
    Assert.Equal(TestIndexPath, IndexService.CurrentPath);
}
```

### Best Practices

1. **Separation of Concerns:** Keep business logic in Models, UI logic in ViewModels
2. **Reactive Programming:** Use ReactiveUI for event handling and state changes
3. **Async UI:** Always wrap blocking Lucene.NET calls in Task.Run()
4. **Dependency Injection:** Use DI for all service and ViewModel creation
5. **XAML Bindings:** Prefer data binding over code-behind for UI updates
6. **Cancellation:** Support cancellation for long-running operations via CancellationToken
7. **Error Handling:** Use try/catch in async methods, show user-friendly error dialogs
8. **Loading States:** Show progress indicators during long operations

### Common Patterns

**Opening Index with Loading Indicator:**
```csharp
private async Task OpenIndexAsync()
{
    IsLoading = true;
    StatusMessage = "Opening index...";

    try
    {
        await _indexService.OpenAsync(IndexPath, DirImpl, ReadOnly);
        StatusMessage = "Index opened successfully";
    }
    catch (Exception ex)
    {
        await ShowErrorDialogAsync("Failed to open index", ex.Message);
    }
    finally
    {
        IsLoading = false;
    }
}
```

**Cross-Tab Navigation:**
```csharp
// Publish message instead of direct coupling
_messageBus.Publish(new BrowseTermMessage(field, term));
_messageBus.Publish(new SwitchTabMessage(TabType.Documents));

// Other tab subscribes and handles
_messageBus.Listen<BrowseTermMessage>()
    .Subscribe(msg => {
        CurrentField = msg.Field;
        CurrentTerm = msg.Term;
        LoadTermDetails();
    });
```

### Performance Considerations

1. **Lazy Loading:** Load expensive data only when needed
2. **Virtualization:** Use virtualizing panels for large lists
3. **Caching:** Cache frequently-accessed data from Lucene.NET
4. **Background Work:** Use Task.Run() for all I/O and CPU-intensive operations
5. **Dispose Pattern:** Properly dispose IndexReaders, SearchResults, etc.
