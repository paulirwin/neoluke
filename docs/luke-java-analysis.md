# Luke Java Codebase Analysis

**Document Version:** 1.0
**Analysis Date:** October 2025
**Source:** Apache Lucene Luke 11.0.0-SNAPSHOT
**Purpose:** Analysis for porting to NeoLuke using Avalonia UI and MVVM

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Project Overview](#project-overview)
3. [Architecture Overview](#architecture-overview)
4. [Core Components](#core-components)
5. [Domain Models](#domain-models)
6. [UI Architecture](#ui-architecture)
7. [Key Features by Tab](#key-features-by-tab)
8. [Design Patterns](#design-patterns)
9. [Data Flow & State Management](#data-flow--state-management)
10. [Recommendations for .NET/Avalonia Port](#recommendations-for-netavalonia-port)

---

## Executive Summary

Luke is a sophisticated desktop GUI application for browsing, searching, and maintaining Apache Lucene indexes. Built with Java Swing, it provides a comprehensive toolbox for index inspection, document browsing, search execution, analysis testing, commit management, and index maintenance.

**Key Statistics:**
- **Total Java Files:** 176
- **UI Framework:** Java Swing
- **Architecture Pattern:** Observer Pattern with Provider/Factory patterns
- **Main Tabs:** 6 (Overview, Documents, Search, Analysis, Commits, Logs)
- **Lines of Code:** ~50,000+ (estimated)

**Primary Purpose:** Luke serves as a diagnostic and development tool for Lucene developers and search engineers to understand, debug, and optimize Lucene indexes.

---

## Project Overview

### Directory Structure

```
luke/
├── src/
│   ├── java/org/apache/lucene/luke/
│   │   ├── app/                          # Application core
│   │   │   ├── desktop/                  # Desktop-specific implementation
│   │   │   │   ├── components/           # UI components (panels, tabs)
│   │   │   │   │   ├── fragments/        # Sub-components (search/analysis fragments)
│   │   │   │   │   └── dialog/           # Modal dialogs
│   │   │   │   │       ├── menubar/      # Menu-triggered dialogs
│   │   │   │   │       ├── analysis/     # Analysis-related dialogs
│   │   │   │   │       ├── search/       # Search-related dialogs
│   │   │   │   │       └── documents/    # Document-related dialogs
│   │   │   │   ├── util/                 # Desktop utilities
│   │   │   │   ├── dto/                  # Data transfer objects
│   │   │   │   ├── LukeMain.java         # Application entry point
│   │   │   │   ├── MessageBroker.java    # Status message system
│   │   │   │   └── Preferences.java      # User preferences
│   │   │   ├── IndexHandler.java         # Index lifecycle management
│   │   │   ├── DirectoryHandler.java     # Directory lifecycle management
│   │   │   ├── LukeState.java            # Current state interface
│   │   │   └── Observer.java             # Observer pattern marker
│   │   ├── models/                       # Business logic layer
│   │   │   ├── overview/                 # Overview model
│   │   │   ├── documents/                # Documents model
│   │   │   ├── search/                   # Search model
│   │   │   ├── analysis/                 # Analysis model
│   │   │   ├── commits/                  # Commits model
│   │   │   ├── tools/                    # Index tools model
│   │   │   └── util/                     # Model utilities
│   │   └── util/                         # Core utilities
│   ├── resources/                        # Resource files
│   │   └── org/apache/lucene/luke/app/desktop/
│   │       ├── messages/                 # Localized messages
│   │       └── util/                     # Resource utilities
│   └── test/                             # Unit tests
└── build.gradle                          # Gradle build configuration
```

### Technology Stack

- **Language:** Java 17+
- **UI Framework:** Java Swing
- **Build System:** Gradle
- **Core Library:** Apache Lucene 11.x
- **Logging:** java.util.logging
- **Look & Feel:** System native (Aqua on macOS, Metal on Linux)

---

## Architecture Overview

### High-Level Architecture

Luke follows a layered architecture with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                      Presentation Layer                      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   Providers  │  │   Operators  │  │   Dialogs    │      │
│  │  (UI Build)  │  │(UI Control)  │  │  (Modals)    │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
                           ↕
┌─────────────────────────────────────────────────────────────┐
│                    Communication Layer                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   Observer   │  │   Message    │  │  Component   │      │
│  │   Pattern    │  │   Broker     │  │  Registry    │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
                           ↕
┌─────────────────────────────────────────────────────────────┐
│                      Business Logic Layer                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   Overview   │  │  Documents   │  │    Search    │      │
│  │    Model     │  │    Model     │  │    Model     │      │
│  ├──────────────┤  ├──────────────┤  ├──────────────┤      │
│  │  Analysis    │  │   Commits    │  │     Tools    │      │
│  │    Model     │  │    Model     │  │    Model     │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
                           ↕
┌─────────────────────────────────────────────────────────────┐
│                        Data Layer                            │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              Apache Lucene Index API                  │   │
│  │  IndexReader, Directory, Document, Query, Analyzer    │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Application Entry Point

**File:** `LukeMain.java`

The application starts by:
1. Initializing GUI logging
2. Setting the system Look & Feel
3. Registering custom fonts (ElegantIcon font for icons)
4. Creating the main window via `LukeWindowProvider`
5. Displaying the "Open Index" dialog immediately

```java
public static void main(String[] args) {
    UIManager.setLookAndFeel(lookAndFeelClassName);
    GraphicsEnvironment.registerFont(FontUtils.createElegantIconFont());

    SwingUtilities.invokeLater(() -> {
        createGUI();
        OpenIndexDialogFactory.getInstance().open(...);
    });
}
```

---

## Core Components

### 1. IndexHandler (Singleton)

**Purpose:** Manages the lifecycle of index opening, closing, and state management.

**Key Responsibilities:**
- Opens Lucene IndexReader for a given directory path
- Maintains current index state (path, reader, configuration)
- Notifies all registered observers when index is opened/closed
- Handles error conditions (missing index, corrupted index)

**Important Methods:**
- `open(indexPath, dirImpl, readOnly, useCompound, keepAllCommits)` - Opens an index
- `close()` - Closes the current index
- `reOpen()` - Closes and reopens with same settings
- `getState()` - Returns current LukeState

**State Management:**
```java
private static class LukeStateImpl implements LukeState {
    private boolean closed = false;
    private String indexPath;
    private IndexReader reader;
    private String dirImpl;
    private boolean readOnly;
    private boolean useCompound;
    private boolean keepAllCommits;
}
```

### 2. LukeState Interface

**Purpose:** Read-only interface to access current index state.

**Key Methods:**
- `getIndexPath()` - Returns the path to the index
- `getDirImpl()` - Returns the Directory implementation class name
- `getDirectory()` - Returns the Directory instance
- `getIndexReader()` - Returns the IndexReader instance
- `readOnly()` - Returns whether index is opened read-only
- `hasDirectoryReader()` - Checks if reader is a DirectoryReader

### 3. DirectoryHandler (Singleton)

**Purpose:** Opens a Directory without an IndexReader (expert mode).

**Use Case:** When users want to inspect index files and commits without opening an IndexReader. This is useful for corrupted indexes or when you only want to examine the file structure.

### 4. MessageBroker (Singleton)

**Purpose:** Centralized message broadcasting system for status bar updates.

**Pattern:** Publish-Subscribe

**Usage:**
```java
MessageBroker.getInstance().showStatusMessage("Index opened successfully");
MessageBroker.getInstance().clearStatusMessage();
MessageBroker.getInstance().showUnknownErrorMessage();
```

**Implementation:**
- Maintains a list of `MessageReceiver` implementations
- Broadcasts messages to all registered receivers
- Main window implements `MessageReceiver` to update status bar

### 5. ComponentOperatorRegistry (Singleton)

**Purpose:** Service locator for cross-tab communication.

**Pattern:** Registry/Service Locator

**Usage:**
```java
operatorRegistry.get(DocumentsTabOperator.class)
    .ifPresent(operator -> {
        operator.browseTerm(field, term);
        tabSwitcher.switchTab(Tab.DOCUMENTS);
    });
```

**Purpose:** Allows tabs to call methods on other tabs without tight coupling.

### 6. TabSwitcherProxy

**Purpose:** Programmatic tab switching without direct JTabbedPane reference.

**Implementation:**
```java
public interface TabSwitcher {
    void switchTab(Tab tab);
}

public enum Tab {
    OVERVIEW(0), DOCUMENTS(1), SEARCH(2),
    ANALYZER(3), COMMITS(4);
}
```

### 7. Preferences System

**Purpose:** Persists user preferences to an INI file.

**Storage Location:** `~/.luke.d/preferences.ini`

**Stored Settings:**
- Index path history (last 10 opened indexes)
- Read-only mode preference
- Directory implementation preference
- Color theme
- Index writer configuration (useCompound, keepAllCommits)

**Implementation:** Uses custom `SimpleIniFile` reader/writer.

---

## Domain Models

All models are created using the **Factory Pattern** and implement dedicated interfaces for their respective tabs. Models encapsulate all business logic for interacting with Lucene indexes.

### 1. Overview Model

**Interface:** `org.apache.lucene.luke.models.overview.Overview`
**Factory:** `OverviewFactory`
**Implementation:** `OverviewImpl`

**Purpose:** Provides index-level statistics and field information.

**Key Operations:**
- Get index path, number of fields, documents, terms
- Check for deletions and optimization status
- Get index version, format, and commit information
- Retrieve sorted term counts per field
- Get top N terms for a specific field

**Data Classes:**
- `TermStats` - Statistics for a single term (text, doc frequency)
- `TermCounts` - Term counts per field
- `TermCountsOrder` - Enum for sorting (by name, by count)
- `TopTerms` - Top terms for a field

### 2. Documents Model

**Interface:** `org.apache.lucene.luke.models.documents.Documents`
**Factory:** `DocumentsFactory`
**Implementation:** `DocumentsImpl`

**Purpose:** Provides document-level operations and field browsing.

**Key Operations:**
- Get document by docid
- Iterate through terms in a field
- Get postings (term positions) for a term
- Get term vectors for a document field
- Get doc values for a document field
- Navigate terms (first, next, seek)
- Navigate postings (firstTermDoc, nextTermDoc)

**Data Classes:**
- `DocumentField` - Field name, type, value, flags
- `TermPosting` - Position, offsets, payload for a posting
- `TermVectorEntry` - Term vector information
- `DocValues` - Doc values wrapper (numeric, binary, sorted, etc.)

**Important Features:**
- **Term Iterator:** Maintains state for navigating terms in a field
- **Postings Iterator:** Maintains state for navigating document postings
- Supports all Lucene field types (stored, indexed, doc values, term vectors)

### 3. Search Model

**Interface:** `org.apache.lucene.luke.models.search.Search`
**Factory:** `SearchFactory`
**Implementation:** `SearchImpl`

**Purpose:** Provides search capabilities with query parsing and execution.

**Key Operations:**
- Parse query expressions with QueryParser
- Execute searches with pagination
- Create MoreLikeThis queries
- Explain query scoring for a document
- Handle custom similarity configurations
- Support sorting with SortField
- Manage search result pagination

**Configuration Classes:**
- `QueryParserConfig` - Query parser settings (operators, phrase slop, fuzzy, wildcards)
- `MLTConfig` - MoreLikeThis configuration (min term freq, min doc freq, fields)
- `SimilarityConfig` - Custom similarity settings

**Data Classes:**
- `SearchResults` - Contains hits, total hits, query, page info

**Important Features:**
- Stateful pagination (maintains current page)
- Query rewriting support
- Custom analyzer per search
- Field-specific searches

### 4. Analysis Model

**Interface:** `org.apache.lucene.luke.models.analysis.Analysis`
**Factory:** `AnalysisFactory`
**Implementation:** `AnalysisImpl`

**Purpose:** Provides text analysis capabilities with custom analyzer building.

**Key Operations:**
- List available CharFilters, Tokenizers, TokenFilters
- Create analyzer from class name
- Build custom analyzer with configurable components
- Analyze text and return tokens
- Step-by-step analysis (shows intermediate results)
- Load external JAR files for custom components

**Data Classes:**
- `Token` - Analyzed token with attributes
- `TokenAttribute` - Token attribute (class name, values)
- `NamedTokens` - Tokenizer/filter name with resulting tokens
- `CharfilteredText` - CharFilter name with filtered text
- `StepByStepResult` - Complete analysis breakdown

**Configuration:**
- `CustomAnalyzerConfig` - Configures custom analyzer chain (charfilters, tokenizer, tokenfilters with parameters)

**Important Features:**
- **Reflection-based Discovery:** Scans classpath for analyzer components
- **Step-by-Step Analysis:** Shows output at each stage of analysis chain
- **Dynamic JAR Loading:** Can load custom analyzers at runtime

### 5. Commits Model

**Interface:** `org.apache.lucene.luke.models.commits.Commits`
**Factory:** `CommitsFactory`
**Implementation:** `CommitsImpl`

**Purpose:** Provides commit history and segment information.

**Key Operations:**
- List all commits in the directory
- Get commit by generation number
- List files for a commit
- List segments for a commit
- Get segment attributes and diagnostics
- Get codec information for segments

**Data Classes:**
- `Commit` - Generation, deleted status, segment count, user data
- `File` - Filename, size
- `Segment` - Name, codec, document count, deletions, size, attributes

**Important Features:**
- Works with Directory (doesn't require IndexReader)
- Can inspect commit history even for corrupted indexes
- Shows low-level segment details

### 6. IndexTools Model

**Interface:** `org.apache.lucene.luke.models.tools.IndexTools`
**Factory:** `IndexToolsFactory`
**Implementation:** `IndexToolsImpl`

**Purpose:** Provides index maintenance and modification operations.

**Key Operations:**
- Force merge (optimize) index
- Check index integrity (CheckIndex)
- Repair corrupted index
- Add documents to index
- Delete documents by query
- Create new index (empty or with sample data)
- Export terms from a field to file

**Important Features:**
- Requires write access to index (not read-only mode)
- Uses IndexWriter for modifications
- Provides progress feedback via PrintStream
- Can work with "20 Newsgroups" sample dataset

---

## UI Architecture

### UI Framework: Java Swing

Luke uses Java Swing with custom patterns to organize the UI code.

### Main Window Structure

**Component:** `LukeWindowProvider`

```
┌─────────────────────────────────────────────────────────────┐
│ Luke - v11.0.0                                   [Menu Bar]  │
├─────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────┐ │
│ │                    JTabbedPane                           │ │
│ │ ┌───┬───┬───┬───┬───┬───┐                              │ │
│ │ │Ov │Do │Se │An │Co │Lo │                              │ │
│ │ └───┴───┴───┴───┴───┴───┘                              │ │
│ │                                                          │ │
│ │              [Current Tab Content]                      │ │
│ │                                                          │ │
│ │                                                          │ │
│ └─────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│ Status: Index opened successfully         [Icons]  [Lucene] │
└─────────────────────────────────────────────────────────────┘
```

**Tabs:**
1. **Overview** (icon: 📊) - Index statistics and field information
2. **Documents** (icon: 📄) - Document browsing and field inspection
3. **Search** (icon: 🔍) - Query execution and result browsing
4. **Analysis** (icon: ⚙️) - Text analysis and analyzer testing
5. **Commits** (icon: 📝) - Commit history and segment details
6. **Logs** (icon: 📋) - Application log viewer

**Status Bar Icons:**
- **Multi-reader icon** - Shown when MultiReader is used (not DirectoryReader)
- **Read-only icon** - Shown when index is opened in read-only mode
- **No-reader icon** - Shown when Directory is opened without reader

### Provider Pattern

**Purpose:** Separates UI construction from UI logic.

**Pattern:**
```java
public class XxxPanelProvider {
    private final JPanel panel = new JPanel();
    private final JLabel someLabel = new JLabel();
    private final ListenerFunctions listeners = new ListenerFunctions();
    private XxxModel model;

    public XxxPanelProvider() {
        // Initialize
        IndexHandler.getInstance().addObserver(new Observer());
    }

    public JPanel get() {
        // Build and return UI
        panel.setLayout(new BorderLayout());
        panel.add(initUpperPanel(), BorderLayout.NORTH);
        return panel;
    }

    private JPanel initUpperPanel() {
        // Build sub-panel
        return panel;
    }

    private class ListenerFunctions {
        void buttonClicked(ActionEvent e) {
            // Handle event
        }
    }

    private class Observer implements IndexObserver {
        @Override
        public void openIndex(LukeState state) {
            // Refresh UI with new index
            model = factory.newInstance(state.getIndexReader());
            someLabel.setText(model.getData());
        }

        @Override
        public void closeIndex() {
            // Clear UI
            someLabel.setText("");
        }
    }
}
```

**Key Characteristics:**
- `Provider` class constructs and owns all UI components
- Inner `ListenerFunctions` class contains all event handlers
- Inner `Observer` class handles index state changes
- `get()` method returns the root panel
- Model is created fresh when index opens

### Operator Pattern

**Purpose:** Provides public API for inter-tab communication.

**Pattern:**
```java
public interface DocumentsTabOperator {
    void displayLatestDoc();
    void browseTerm(String field, String term);
}

public class DocumentsPanelProvider implements DocumentsTabOperator {
    public DocumentsPanelProvider() {
        ComponentOperatorRegistry.getInstance()
            .register(DocumentsTabOperator.class, this);
    }

    @Override
    public void browseTerm(String field, String term) {
        // Implementation
    }
}
```

**Usage:** Other tabs can call operator methods via the registry:
```java
operatorRegistry.get(DocumentsTabOperator.class)
    .ifPresent(operator -> operator.browseTerm("title", "lucene"));
```

### Dialog Factory Pattern

**Purpose:** Creates modal dialogs with consistent styling and behavior.

**Pattern:**
```java
public class XxxDialogFactory implements DialogOpener.DialogFactory {
    private JDialog dialog;
    private final JButton okButton = new JButton();
    private final ListenerFunctions listeners = new ListenerFunctions();

    @Override
    public JDialog create(Window owner, String title, int width, int height) {
        dialog = new JDialog(owner, title, Dialog.ModalityType.APPLICATION_MODAL);
        dialog.add(content());
        dialog.setSize(new Dimension(width, height));
        return dialog;
    }

    private JPanel content() {
        // Build dialog content
        okButton.addActionListener(listeners::okClicked);
        return panel;
    }

    private class ListenerFunctions {
        void okClicked(ActionEvent e) {
            // Handle OK
            dialog.dispose();
        }
    }
}
```

**Usage:**
```java
XxxDialogFactory factory = XxxDialogFactory.getInstance();
new DialogOpener<>(factory).open("Dialog Title", 600, 400, (result) -> {
    // Handle result
});
```

### Table Model Pattern

**Purpose:** Provides data for JTable components.

**Pattern:**
```java
static final class MyTableModel extends TableModelBase<MyTableModel.Column> {
    enum Column implements TableColumnInfo {
        NAME("Name", 0, String.class, 150),
        VALUE("Value", 1, String.class, 200);

        private final String colName;
        private final int index;
        private final Class<?> type;
        private final int width;

        // Constructor and getters...
    }

    MyTableModel(List<MyData> data) {
        super(data.size());
        for (int i = 0; i < data.size(); i++) {
            this.data[i] = new Object[] {
                data.get(i).getName(),
                data.get(i).getValue()
            };
        }
    }

    @Override
    protected Column[] columnInfos() {
        return Column.values();
    }
}
```

**Key Features:**
- Extends `TableModelBase` which handles standard table operations
- Uses enum for column metadata (name, index, type, width)
- Immutable data array populated in constructor

---

## Key Features by Tab

### 1. Overview Tab

**Purpose:** Displays index-level statistics and field information.

**UI Components:**
- **Upper Panel:** Index statistics (grid layout)
  - Index path
  - Number of fields, documents, terms
  - Deletions / Optimized status
  - Index version and format
  - Directory implementation
  - Commit point info and user data

- **Lower Panel:** Field browser (split pane)
  - **Left:** Fields table with term counts and percentages
  - **Right:** Top terms display for selected field

**Key Interactions:**
- Select a field → Shows field name in text box
- Click "Show Top Terms" → Displays top N terms for field
- Right-click term → Context menu:
  - "Browse by term" → Switches to Documents tab
  - "Search by term" → Switches to Search tab

**Model Methods Used:**
- `getIndexPath()`, `getNumFields()`, `getNumDocuments()`, `getNumTerms()`
- `hasDeletions()`, `getNumDeletedDocs()`, `isOptimized()`
- `getIndexVersion()`, `getIndexFormat()`, `getDirImpl()`
- `getCommitDescription()`, `getCommitUserData()`
- `getSortedTermCounts(order)` - Returns Map<String, Long>
- `getTopTerms(field, numTerms)` - Returns List<TermStats>

**Tables:**
- **Term Counts Table:** 3 columns (Field Name, Term Count, Percentage)
  - Sortable by clicking column headers
  - Shows all fields in index
- **Top Terms Table:** 3 columns (Rank, Frequency, Term Text)
  - Shows top N terms for selected field
  - Right-click for context menu

### 2. Documents Tab

**Purpose:** Browse documents and explore term postings.

**UI Layout:** Complex split-pane layout with multiple sections

**Sections:**

**A. Document Browser** (upper-left)
- Max doc count display
- Document selector (spinner to navigate by docid)
- "First Doc", "Next Doc", "Prev Doc", "Last Doc" buttons
- Copy button to copy all fields to clipboard
- Fields table showing all fields in the document
- Field detail view with:
  - Stored value viewer
  - Term vector viewer (if available)
  - Doc values viewer (if available)
  - Index options viewer

**B. Term Browser** (lower-left)
- Field selector dropdown
- "First Term" button
- Term input field with "Seek" button
- "Next Term" button
- Term info display (doc frequency)
- Postings browser:
  - Document list for current term
  - Position information for selected posting

**C. Document Fields Table**
Shows for each field:
- Field name
- Field type (Stored, Indexed, DocValues, etc.)
- Stored value (if available)
- Term Vector indicator
- DocValues type

**Model Methods Used:**
- `getMaxDoc()` - Returns max doc ID
- `getFieldNames()` - Returns all field names
- `isLive(docid)` - Checks if document is not deleted
- `getDocumentFields(docid)` - Returns List<DocumentField>
- `firstTerm(field)` - Returns first term in field
- `nextTerm()` - Advances term iterator
- `seekTerm(termText)` - Seeks to specific term
- `firstTermDoc()` - Returns first doc for current term
- `nextTermDoc()` - Advances posting iterator
- `getTermPositions()` - Returns List<TermPosting>
- `getDocFreq()` - Returns document frequency
- `getTermVectors(docid, field)` - Returns term vectors
- `getDocValues(docid, field)` - Returns doc values

**Dialogs:**
- **Stored Value Dialog:** Displays full stored value for large fields
- **Term Vector Dialog:** Shows term vector details (terms, frequencies, positions, offsets)
- **Doc Values Dialog:** Shows doc values (numeric, binary, sorted, etc.)
- **Index Options Dialog:** Shows index options for field
- **Add Document Dialog:** Allows adding new documents (write mode only)

### 3. Search Tab

**Purpose:** Execute queries and browse search results.

**UI Layout:** Split into query builder and results

**Query Builder Section:**
- Query input text area
- Field selector (default search field)
- Analyzer selector or custom analyzer builder
- Query parser configuration (expandable):
  - Default operator (AND/OR)
  - Split on whitespace
  - Enable position increments
  - Enable graph queries
  - Allow leading wildcard
  - Date resolution
  - Phrase slop
  - Fuzzy parameters (min similarity, prefix length)
  - Locale
- "Parse" button (shows parsed query)
- "Search" button

**Advanced Search Options** (tabs):
1. **Sort Tab:** Configure sort fields and order
2. **Field Values Tab:** Select specific fields to load
3. **Similarity Tab:** Configure custom similarity
4. **MLT Tab:** More Like This query builder

**Results Section:**
- Total hits display
- Page navigation (Previous, Next)
- Results table:
  - Document ID
  - Score
  - Field values (for loaded fields)
- Context menu on results:
  - "Show All Fields" → Opens full document in Documents tab
  - "Explain" → Shows scoring explanation dialog

**Model Methods Used:**
- `getFieldNames()`, `getSearchableFieldNames()`, `getSortableFieldNames()`
- `parseQuery(expression, defField, analyzer, config, rewrite)`
- `search(query, simConfig, fieldsToLoad, pageSize, exactHitsCount)`
- `search(query, simConfig, sort, fieldsToLoad, pageSize, exactHitsCount)`
- `nextPage()`, `prevPage()`
- `explain(query, docid)`
- `mltQuery(docid, mltConfig, analyzer)`
- `guessSortTypes(fieldName)`, `getSortType(name, type, reverse)`

**Fragments (Sub-components):**
- **QueryParserPane:** Query parser configuration
- **AnalyzerPane:** Analyzer selection/configuration
- **SortPane:** Sort field configuration
- **FieldValuesPane:** Field selection for loading
- **SimilarityPane:** Similarity configuration
- **MLTPane:** MoreLikeThis configuration

**Dialogs:**
- **Explain Dialog:** Shows query explanation tree for a document

### 4. Analysis Tab

**Purpose:** Test and build text analyzers.

**UI Layout:** Two modes (radio button selection)

**Mode 1: Preset Analyzer**
- Dropdown to select from available analyzers
- Text input area
- "Analyze" button
- Results table showing tokens:
  - Term text
  - Token attributes (offset, position, type, etc.)
- "Token Attributes" button to see all attributes for selected token

**Mode 2: Custom Analyzer**
- CharFilters list (can add multiple)
  - Dropdown to select CharFilter
  - "Add" button
  - Parameters editor
- Tokenizer selector (required, single)
  - Dropdown to select Tokenizer
  - Parameters editor
- TokenFilters list (can add multiple)
  - Dropdown to select TokenFilter
  - "Add" button
  - Parameters editor
- "Clear" button (resets custom analyzer)
- Text input area
- "Analyze" button
- Step-by-step results showing:
  - Original text
  - Text after each CharFilter
  - Tokens after Tokenizer
  - Tokens after each TokenFilter

**Advanced Features:**
- "Load External Jars" button - Load custom analyzer components from JAR files
- "Analysis Chain" button - Shows complete analyzer configuration

**Model Methods Used:**
- `getAvailableCharFilters()`, `getAvailableTokenizers()`, `getAvailableTokenFilters()`
- `createAnalyzerFromClassName(analyzerType)`
- `buildCustomAnalyzer(config)`
- `analyze(text)` - Returns List<Token>
- `analyzeStepByStep(text)` - Returns StepByStepResult
- `addExternalJars(jarFiles)`
- `currentAnalyzer()` - Returns current Analyzer

**Dialogs:**
- **Token Attributes Dialog:** Shows all attributes for a token
- **Analysis Chain Dialog:** Shows complete analyzer configuration
- **Edit Parameters Dialog:** Edit CharFilter/Tokenizer/TokenFilter parameters
- **Edit Filters Dialog:** Manage filter list

### 5. Commits Tab

**Purpose:** Explore commit history and segment details.

**UI Layout:** Multiple sections

**Commit List Section:**
- Table showing all commits:
  - Generation number
  - Commit deleted (yes/no)
  - Segment count
  - User data
- "Segment Details" button (shows segment info for selected commit)

**Commit Details Section:**
- Generation number
- Segment count
- User data map (if present)

**Files Section:**
- Table showing files for selected commit:
  - Filename
  - Size (bytes)

**Segments Section:**
- Table showing segments for selected commit:
  - Segment name
  - Codec
  - Document count
  - Deletions
  - Size
  - Attributes
- "Attributes" button → Shows attributes map
- "Diagnostics" button → Shows diagnostics map
- "Codec" button → Shows codec information

**Model Methods Used:**
- `listCommits()` - Returns List<Commit>
- `getCommit(commitGen)` - Returns Optional<Commit>
- `getFiles(commitGen)` - Returns List<File>
- `getSegments(commitGen)` - Returns List<Segment>
- `getSegmentAttributes(commitGen, name)` - Returns Map<String, String>
- `getSegmentDiagnostics(commitGen, name)` - Returns Map<String, String>
- `getSegmentCodec(commitGen, name)` - Returns Optional<Codec>

**Key Features:**
- Works with DirectoryHandler (doesn't need IndexReader)
- Can inspect commits even if index is corrupted
- Shows low-level segment structure

### 6. Logs Tab

**Purpose:** View application logs in real-time.

**UI Components:**
- Text area showing log messages
- "Clear" button to clear log
- Auto-scrolls to latest message

**Implementation:**
- Uses custom `CircularLogBufferHandler`
- Intercepts java.util.logging messages
- Displays in scrollable text pane

---

## Design Patterns

### 1. Observer Pattern

**Usage:** Index/Directory lifecycle notifications

**Implementation:**
```java
// Subject
public class IndexHandler {
    private List<IndexObserver> observers = new ArrayList<>();

    public void addObserver(IndexObserver observer) {
        observers.add(observer);
    }

    protected void notifyObservers() {
        for (IndexObserver observer : observers) {
            if (state.closed) {
                observer.closeIndex();
            } else {
                observer.openIndex(state);
            }
        }
    }
}

// Observer Interface
public interface IndexObserver extends Observer {
    default void openIndex(LukeState state) {}
    default void closeIndex() {}
}

// Concrete Observer
public class OverviewPanelProvider {
    private class Observer implements IndexObserver {
        @Override
        public void openIndex(LukeState state) {
            // Refresh UI
            model = factory.newInstance(state.getIndexReader());
            updateLabels();
        }

        @Override
        public void closeIndex() {
            // Clear UI
            clearLabels();
        }
    }
}
```

**Observers:**
- Each tab panel observes IndexHandler
- Window observes IndexHandler and DirectoryHandler
- TabbedPane observes IndexHandler and DirectoryHandler
- Tabs are enabled/disabled based on index/directory state

### 2. Factory Pattern

**Usage:** Model creation

**Implementation:**
```java
public class OverviewFactory {
    public Overview newInstance(IndexReader reader, String indexPath) {
        return new OverviewImpl(reader, indexPath);
    }
}
```

**Factories:**
- `OverviewFactory` → `Overview` model
- `DocumentsFactory` → `Documents` model
- `SearchFactory` → `Search` model
- `AnalysisFactory` → `Analysis` model
- `CommitsFactory` → `Commits` model
- `IndexToolsFactory` → `IndexTools` model

**Dialog Factories:**
- All dialogs use factory pattern via `DialogOpener.DialogFactory` interface

### 3. Provider Pattern

**Usage:** UI component construction

**Characteristics:**
- Provider class constructs and owns all UI components
- Provides a `get()` method returning the root component
- Encapsulates all UI construction logic
- Registers itself as observer of relevant subjects

**Examples:**
- `LukeWindowProvider` → Creates main window
- `TabbedPaneProvider` → Creates tabbed pane with all tabs
- `OverviewPanelProvider` → Creates Overview tab panel
- `DocumentsPanelProvider` → Creates Documents tab panel
- `SearchPanelProvider` → Creates Search tab panel
- `AnalysisPanelProvider` → Creates Analysis tab panel
- `CommitsPanelProvider` → Creates Commits tab panel
- `LogsPanelProvider` → Creates Logs tab panel
- `MenuBarProvider` → Creates menu bar

### 4. Singleton Pattern

**Usage:** Global application state and services

**Implementations:**
- `IndexHandler.getInstance()`
- `DirectoryHandler.getInstance()`
- `MessageBroker.getInstance()`
- `ComponentOperatorRegistry.getInstance()`
- `TabSwitcherProxy.getInstance()`
- `PreferencesFactory.getInstance()`

**Rationale:** Single source of truth for application state

### 5. Registry Pattern

**Usage:** Component lookup and cross-tab communication

**Implementation:**
```java
public class ComponentOperatorRegistry {
    private Map<Class<?>, Object> operatorMap = new ConcurrentHashMap<>();

    public <T> void register(Class<T> clazz, T operator) {
        operatorMap.put(clazz, operator);
    }

    public <T> Optional<T> get(Class<T> clazz) {
        return Optional.ofNullable(clazz.cast(operatorMap.get(clazz)));
    }
}
```

**Registered Operators:**
- `LukeWindowOperator` - Main window operations
- `DocumentsTabOperator` - Document browsing operations
- `SearchTabOperator` - Search operations
- `AnalysisTabOperator` - Analysis operations

### 6. Builder Pattern

**Usage:** Complex object construction (Configuration objects)

**Examples:**
- `CustomAnalyzerConfig` - Builds custom analyzer configuration
- `QueryParserConfig` - Builds query parser configuration
- `MLTConfig` - Builds MoreLikeThis configuration
- `SimilarityConfig` - Builds similarity configuration

### 7. Adapter Pattern

**Usage:** Wrapping Lucene APIs

**Examples:**
- `TermVectorsAdapter` - Adapts Lucene TermVectors API
- `DocValuesAdapter` - Adapts Lucene DocValues API

---

## Data Flow & State Management

### Index Opening Flow

```
User clicks "Open Index"
    ↓
OpenIndexDialog collects settings
    ↓
User clicks OK
    ↓
IndexHandler.open(path, settings)
    ↓
IndexUtils.openIndex() [opens Lucene IndexReader]
    ↓
LukeStateImpl created and stored
    ↓
IndexHandler.notifyObservers()
    ↓
┌─────────────────────────────────────┐
│ All IndexObserver implementations   │
│ receive openIndex(state) callback   │
└─────────────────────────────────────┘
    ↓
Each tab's Observer creates its model:
- OverviewPanelProvider → overviewModel = overviewFactory.newInstance(reader)
- DocumentsPanelProvider → documentsModel = documentsFactory.newInstance(reader)
- SearchPanelProvider → searchModel = searchFactory.newInstance(reader)
- etc.
    ↓
Each tab refreshes its UI with model data
    ↓
TabbedPane Observer enables appropriate tabs
    ↓
Window Observer updates status icons
```

### Cross-Tab Navigation Flow

Example: User double-clicks a term in Overview tab to browse it in Documents tab

```
User double-clicks term "lucene" in field "title"
    ↓
OverviewPanelProvider.browseByTerm() called
    ↓
operatorRegistry.get(DocumentsTabOperator.class)
    ↓
documentsOperator.browseTerm("title", "lucene")
    ↓
DocumentsPanelProvider.browseTerm() implementation:
  - Sets field selector to "title"
  - Calls documentsModel.seekTerm("lucene")
  - Updates term display
  - Calls documentsModel.firstTermDoc()
  - Updates document display
    ↓
tabSwitcher.switchTab(Tab.DOCUMENTS)
    ↓
TabbedPane switches to Documents tab
    ↓
User sees Documents tab with term "lucene" in field "title"
```

### Message Broadcasting Flow

```
Action occurs (e.g., index opened)
    ↓
messageBroker.showStatusMessage("Index opened")
    ↓
MessageBroker broadcasts to all registered MessageReceivers
    ↓
LukeWindowProvider.MessageReceiverImpl.showStatusMessage()
    ↓
Status label in window footer updated
```

### Model Lifecycle

**Creation:** Models are created when index opens
```java
// In Observer.openIndex()
overviewModel = overviewFactory.newInstance(state.getIndexReader(), state.getIndexPath());
```

**Usage:** Models are stateless except for iterators
```java
// Models expose query methods
int numDocs = overviewModel.getNumDocuments();
List<TermStats> terms = overviewModel.getTopTerms("title", 50);
```

**Stateful Operations:** Some models maintain iterator state
```java
// Documents model maintains term and posting iterators
documentsModel.firstTerm("title");          // Positions term iterator
Optional<Term> term = documentsModel.nextTerm();  // Advances iterator
```

**Destruction:** Models are discarded when index closes
```java
// In Observer.closeIndex()
overviewModel = null;  // Allow GC
```

---

## Recommendations for .NET/Avalonia Port

### 1. Architecture Mapping

**Java → .NET/Avalonia Equivalents:**

| Java Luke Pattern | .NET/Avalonia Equivalent | Rationale |
|-------------------|-------------------------|-----------|
| Provider classes | ViewModel classes | MVVM pattern - ViewModels create/manage view state |
| Observer pattern | INotifyPropertyChanged + ReactiveUI | Modern reactive bindings |
| Operator interfaces | ViewModelCommands/Methods | Direct ViewModel method calls or Commands |
| ComponentOperatorRegistry | Dependency Injection Container | .NET standard pattern |
| MessageBroker | MessageBus (ReactiveUI) | Loosely-coupled messaging |
| TabSwitcherProxy | ViewModel property binding | Bind to SelectedTab property |
| Dialog factories | View + ViewModel | Avalonia View/ViewModel pairs |
| TableModelBase | ObservableCollection<T> | XAML data binding |
| Factory pattern for models | Dependency Injection | .NET standard pattern |
| Singleton pattern | DI Singleton lifetime | Let DI container manage |

### 2. Recommended .NET Architecture

```
NeoLuke/
├── Models/                           # Domain models (business logic)
│   ├── Overview/
│   │   ├── IOverview.cs
│   │   ├── OverviewModel.cs
│   │   ├── TermStats.cs
│   │   └── TermCounts.cs
│   ├── Documents/
│   ├── Search/
│   ├── Analysis/
│   ├── Commits/
│   ├── Tools/
│   └── Common/
│       ├── ILukeState.cs
│       └── IndexService.cs          # Replaces IndexHandler
├── ViewModels/                       # MVVM ViewModels
│   ├── MainWindowViewModel.cs
│   ├── OverviewViewModel.cs
│   ├── DocumentsViewModel.cs
│   ├── SearchViewModel.cs
│   ├── AnalysisViewModel.cs
│   ├── CommitsViewModel.cs
│   ├── LogsViewModel.cs
│   └── Dialogs/
│       ├── OpenIndexDialogViewModel.cs
│       └── ...
├── Views/                            # Avalonia AXAML views
│   ├── MainWindow.axaml
│   ├── OverviewView.axaml
│   ├── DocumentsView.axaml
│   ├── SearchView.axaml
│   ├── AnalysisView.axaml
│   ├── CommitsView.axaml
│   ├── LogsView.axaml
│   └── Dialogs/
│       ├── OpenIndexDialog.axaml
│       └── ...
├── Services/                         # Application services
│   ├── IndexService.cs               # Replaces IndexHandler
│   ├── IMessageBus.cs                # Status messages
│   ├── IPreferencesService.cs        # User preferences
│   └── IDialogService.cs             # Dialog management
├── Converters/                       # XAML value converters
└── App.axaml.cs                      # Application startup
```

### 3. MVVM Pattern for Tabs

**Example: Overview Tab**

**ViewModel (OverviewViewModel.cs):**
```csharp
public class OverviewViewModel : ViewModelBase, IDisposable
{
    private readonly IOverview _model;
    private readonly IMessageBus _messageBus;
    private readonly IndexService _indexService;
    private IDisposable _indexOpenedSubscription;

    public OverviewViewModel(
        IndexService indexService,
        IMessageBus messageBus)
    {
        _indexService = indexService;
        _messageBus = messageBus;

        // Subscribe to index opened events
        _indexOpenedSubscription = _indexService.IndexOpened
            .Subscribe(OnIndexOpened);

        ShowTopTermsCommand = ReactiveCommand.Create<string>(ShowTopTerms);
        BrowseTermCommand = ReactiveCommand.Create<(string field, string term)>(BrowseTerm);
        SearchTermCommand = ReactiveCommand.Create<(string field, string term)>(SearchTerm);
    }

    // Observable properties (with INotifyPropertyChanged)
    private string _indexPath;
    public string IndexPath
    {
        get => _indexPath;
        set => this.RaiseAndSetIfChanged(ref _indexPath, value);
    }

    private int _numFields;
    public int NumFields
    {
        get => _numFields;
        set => this.RaiseAndSetIfChanged(ref _numFields, value);
    }

    private ObservableCollection<TermCountRow> _termCounts;
    public ObservableCollection<TermCountRow> TermCounts
    {
        get => _termCounts;
        set => this.RaiseAndSetIfChanged(ref _termCounts, value);
    }

    private TermCountRow _selectedField;
    public TermCountRow SelectedField
    {
        get => _selectedField;
        set => this.RaiseAndSetIfChanged(ref _selectedField, value);
    }

    private ObservableCollection<TermStatsRow> _topTerms;
    public ObservableCollection<TermStatsRow> TopTerms
    {
        get => _topTerms;
        set => this.RaiseAndSetIfChanged(ref _topTerms, value);
    }

    // Commands
    public ReactiveCommand<string, Unit> ShowTopTermsCommand { get; }
    public ReactiveCommand<(string, string), Unit> BrowseTermCommand { get; }
    public ReactiveCommand<(string, string), Unit> SearchTermCommand { get; }

    private void OnIndexOpened(ILukeState state)
    {
        _model = new OverviewModel(state.IndexReader, state.IndexPath);

        // Update all properties
        IndexPath = _model.GetIndexPath();
        NumFields = _model.GetNumFields();
        // ... etc

        var termCounts = _model.GetSortedTermCounts(TermCountsOrder.CountDesc);
        TermCounts = new ObservableCollection<TermCountRow>(
            termCounts.Select(kvp => new TermCountRow(kvp.Key, kvp.Value))
        );

        _messageBus.Publish(new StatusMessage("Index opened"));
    }

    private void ShowTopTerms(string fieldName)
    {
        var terms = _model.GetTopTerms(fieldName, 50);
        TopTerms = new ObservableCollection<TermStatsRow>(
            terms.Select((t, i) => new TermStatsRow(i + 1, t.DocFreq, t.DecodedTermText))
        );
    }

    private void BrowseTerm((string field, string term) args)
    {
        _messageBus.Publish(new BrowseTermMessage(args.field, args.term));
        _messageBus.Publish(new SwitchTabMessage(TabType.Documents));
    }

    public void Dispose()
    {
        _indexOpenedSubscription?.Dispose();
    }
}
```

**View (OverviewView.axaml):**
```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:NeoLuke.ViewModels"
             x:DataType="vm:OverviewViewModel">

    <Grid RowDefinitions="Auto,*">
        <!-- Upper Panel: Index Statistics -->
        <Grid Grid.Row="0" ColumnDefinitions="Auto,*" RowDefinitions="Auto,Auto,Auto,Auto,Auto">
            <TextBlock Grid.Row="0" Grid.Column="0" Text="Index Path:" />
            <TextBlock Grid.Row="0" Grid.Column="1" Text="{Binding IndexPath}" />

            <TextBlock Grid.Row="1" Grid.Column="0" Text="Number of Fields:" />
            <TextBlock Grid.Row="1" Grid.Column="1" Text="{Binding NumFields}" />

            <!-- ... more rows ... -->
        </Grid>

        <!-- Lower Panel: Field Browser -->
        <Grid Grid.Row="1" ColumnDefinitions="300,*">
            <!-- Fields Table -->
            <DataGrid Grid.Column="0"
                      ItemsSource="{Binding TermCounts}"
                      SelectedItem="{Binding SelectedField}"
                      AutoGenerateColumns="False">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="Field" Binding="{Binding Name}" />
                    <DataGridTextColumn Header="Term Count" Binding="{Binding Count}" />
                    <DataGridTextColumn Header="%" Binding="{Binding Percentage}" />
                </DataGrid.Columns>
            </DataGrid>

            <!-- Top Terms Panel -->
            <StackPanel Grid.Column="1">
                <TextBlock Text="Selected Field:" />
                <TextBlock Text="{Binding SelectedField.Name}" FontWeight="Bold" />
                <Button Content="Show Top Terms"
                        Command="{Binding ShowTopTermsCommand}"
                        CommandParameter="{Binding SelectedField.Name}" />

                <DataGrid ItemsSource="{Binding TopTerms}"
                          AutoGenerateColumns="False">
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="Rank" Binding="{Binding Rank}" />
                        <DataGridTextColumn Header="Freq" Binding="{Binding Frequency}" />
                        <DataGridTextColumn Header="Term" Binding="{Binding Text}" />
                    </DataGrid.Columns>

                    <DataGrid.ContextMenu>
                        <ContextMenu>
                            <MenuItem Header="Browse by term"
                                      Command="{Binding BrowseTermCommand}" />
                            <MenuItem Header="Search by term"
                                      Command="{Binding SearchTermCommand}" />
                        </ContextMenu>
                    </DataGrid.ContextMenu>
                </DataGrid>
            </StackPanel>
        </Grid>
    </Grid>
</UserControl>
```

### 4. Service Layer Pattern

**IndexService (replaces IndexHandler):**
```csharp
public class IndexService : IDisposable
{
    private IndexReader _reader;
    private ILukeState _state;

    // Observables for reactive subscriptions
    public IObservable<ILukeState> IndexOpened { get; }
    public IObservable<Unit> IndexClosed { get; }

    private Subject<ILukeState> _indexOpenedSubject = new();
    private Subject<Unit> _indexClosedSubject = new();

    public IndexService()
    {
        IndexOpened = _indexOpenedSubject.AsObservable();
        IndexClosed = _indexClosedSubject.AsObservable();
    }

    public async Task OpenAsync(
        string indexPath,
        string dirImpl,
        bool readOnly = true,
        bool useCompound = false,
        bool keepAllCommits = false)
    {
        if (_reader != null)
        {
            await CloseAsync();
        }

        _reader = await Task.Run(() =>
            IndexUtils.OpenIndex(indexPath, dirImpl));

        _state = new LukeState
        {
            IndexPath = indexPath,
            IndexReader = _reader,
            DirImpl = dirImpl,
            ReadOnly = readOnly,
            UseCompound = useCompound,
            KeepAllCommits = keepAllCommits
        };

        _indexOpenedSubject.OnNext(_state);
    }

    public async Task CloseAsync()
    {
        if (_reader != null)
        {
            await Task.Run(() => _reader.Dispose());
            _reader = null;
            _state = null;
        }

        _indexClosedSubject.OnNext(Unit.Default);
    }

    public ILukeState CurrentState => _state;

    public void Dispose()
    {
        _reader?.Dispose();
        _indexOpenedSubject?.Dispose();
        _indexClosedSubject?.Dispose();
    }
}
```

### 5. Message Bus Pattern

**Interface:**
```csharp
public interface IMessageBus
{
    void Publish<TMessage>(TMessage message);
    IObservable<TMessage> Listen<TMessage>();
}
```

**Messages:**
```csharp
public record StatusMessage(string Text);
public record SwitchTabMessage(TabType Tab);
public record BrowseTermMessage(string Field, string Term);
public record SearchTermMessage(string Field, string Term);
```

**Usage in ViewModel:**
```csharp
// Publish
_messageBus.Publish(new StatusMessage("Search completed"));

// Subscribe
_messageBus.Listen<BrowseTermMessage>()
    .Subscribe(msg => BrowseTerm(msg.Field, msg.Term));
```

### 6. Dependency Injection Setup

**Program.cs:**
```csharp
public static void Main(string[] args)
{
    BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
}

public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace()
        .ConfigureServices();

private static AppBuilder ConfigureServices(this AppBuilder builder)
{
    var services = new ServiceCollection();

    // Services (Singletons)
    services.AddSingleton<IndexService>();
    services.AddSingleton<IMessageBus, MessageBus>();
    services.AddSingleton<IPreferencesService, PreferencesService>();
    services.AddSingleton<IDialogService, DialogService>();

    // ViewModels (Transient - created when needed)
    services.AddTransient<MainWindowViewModel>();
    services.AddTransient<OverviewViewModel>();
    services.AddTransient<DocumentsViewModel>();
    services.AddTransient<SearchViewModel>();
    services.AddTransient<AnalysisViewModel>();
    services.AddTransient<CommitsViewModel>();
    services.AddTransient<LogsViewModel>();

    // Dialog ViewModels
    services.AddTransient<OpenIndexDialogViewModel>();

    var serviceProvider = services.BuildServiceProvider();

    // Make service provider available globally
    App.Services = serviceProvider;

    return builder;
}
```

### 7. Modern UI Design Recommendations

**Design Goals:**
- Clean, modern aesthetic (vs Java Swing's dated look)
- Responsive layout that works on different screen sizes
- Dark mode support
- Smooth animations and transitions
- Touch-friendly (larger hit targets)

**UI Framework Features to Leverage:**
- **Data Binding:** Replace manual UI updates with XAML bindings
- **Commands:** Replace ActionListener with ICommand/ReactiveCommand
- **Styles & Themes:** Consistent styling across app
- **Composition:** Use UserControls for reusable components
- **Threading:** Use Task.Run() to move long-running Lucene.NET operations off UI thread

**Specific UI Improvements:**

1. **Main Window:**
   - Use modern tab control with icons
   - Side navigation instead of tabs (optional)
   - Floating toolbar/ribbon for common actions
   - Collapsible side panels

2. **Overview Tab:**
   - Card-based layout for statistics
   - Charts/graphs for term distribution
   - Search/filter in fields table

3. **Documents Tab:**
   - Tree view for nested fields
   - Syntax highlighting for JSON/XML fields
   - Preview pane with rich formatting

4. **Search Tab:**
   - Query builder with visual components
   - Syntax highlighting in query editor
   - Auto-complete for field names
   - Search history

5. **Analysis Tab:**
   - Visual analyzer chain builder (drag-and-drop)
   - Live analysis preview
   - Token visualization (colored boxes)

6. **Commits Tab:**
   - Timeline view for commits
   - Diff view for changes
   - Graph visualization of segment structure

### 8. Threading and UI Responsiveness

**Problem in Java Luke:** Long-running operations block the UI thread

**Important Note: Lucene.NET is Synchronous**

Lucene.NET does not provide async/await APIs. All Lucene operations (opening indexes, searching, reading documents, etc.) are synchronous blocking calls. However, we still use async/await patterns in the UI layer to keep the application responsive.

**Solution: Task.Run() Pattern**

Wrap synchronous Lucene.NET operations in `Task.Run()` to move them off the UI thread:

```csharp
// In ViewModel
private async Task OpenIndexAsync()
{
    IsLoading = true;

    try
    {
        // Lucene.NET operations are synchronous, so wrap in Task.Run
        await _indexService.OpenAsync(IndexPath, DirImpl, ReadOnly);
        // UI updates happen automatically via bindings
    }
    catch (Exception ex)
    {
        await _dialogService.ShowErrorAsync("Error opening index", ex.Message);
    }
    finally
    {
        IsLoading = false;
    }
}

// Command setup - still use async for UI responsiveness
OpenIndexCommand = ReactiveCommand.CreateFromTask(OpenIndexAsync);
```

**IndexService Implementation:**
```csharp
public async Task OpenAsync(
    string indexPath,
    string dirImpl,
    bool readOnly = true,
    bool useCompound = false,
    bool keepAllCommits = false)
{
    if (_reader != null)
    {
        await CloseAsync();
    }

    // Wrap synchronous Lucene.NET call in Task.Run
    _reader = await Task.Run(() =>
        IndexUtils.OpenIndex(indexPath, dirImpl));

    _state = new LukeState
    {
        IndexPath = indexPath,
        IndexReader = _reader,
        DirImpl = dirImpl,
        ReadOnly = readOnly,
        UseCompound = useCompound,
        KeepAllCommits = keepAllCommits
    };

    _indexOpenedSubject.OnNext(_state);
}

public async Task CloseAsync()
{
    if (_reader != null)
    {
        // Wrap synchronous Dispose in Task.Run
        await Task.Run(() => _reader.Dispose());
        _reader = null;
        _state = null;
    }

    _indexClosedSubject.OnNext(Unit.Default);
}
```

**Model Layer - Synchronous Methods:**

Since Lucene.NET is synchronous, the Model layer should expose synchronous methods. Only wrap them in Task.Run() at the service/ViewModel boundary:

```csharp
// Model - synchronous (correct)
public class OverviewModel : IOverview
{
    public int GetNumDocuments() => _reader.NumDocs;
    public long GetNumTerms() => CalculateTermCount();  // May be slow
    public List<TermStats> GetTopTerms(string field, int numTerms) => ...;
}

// ViewModel - wrap slow operations in Task.Run
private async Task ShowTopTermsAsync()
{
    IsLoading = true;
    try
    {
        // Wrap potentially slow synchronous operation
        var terms = await Task.Run(() =>
            _model.GetTopTerms(SelectedField, NumTerms));

        TopTerms = new ObservableCollection<TermStatsRow>(
            terms.Select((t, i) => new TermStatsRow(i + 1, t.DocFreq, t.DecodedTermText))
        );
    }
    finally
    {
        IsLoading = false;
    }
}
```

**When to Use Task.Run():**
- ✅ Opening/closing indexes (I/O bound)
- ✅ Searching large indexes (CPU bound)
- ✅ Calculating statistics (CPU bound)
- ✅ Reading large documents (I/O bound)
- ✅ Building analyzers with reflection
- ❌ Simple property getters (NumDocs, IndexPath, etc.)
- ❌ Operations that complete in <50ms

**Benefits:**
- UI remains responsive during long operations
- Can show loading indicators and progress
- Cancellation support via CancellationToken
- No need for SwingWorker equivalent
- Clean async/await syntax in UI layer

**Example with Cancellation:**
```csharp
private CancellationTokenSource _searchCts;

private async Task SearchAsync()
{
    // Cancel previous search if still running
    _searchCts?.Cancel();
    _searchCts = new CancellationTokenSource();

    IsSearching = true;
    try
    {
        var results = await Task.Run(() =>
            _searchModel.Search(Query, PageSize),
            _searchCts.Token);

        SearchResults = new ObservableCollection<SearchResult>(results);
    }
    catch (OperationCanceledException)
    {
        // Search was cancelled
    }
    finally
    {
        IsSearching = false;
    }
}
```

### 9. Testing Strategy

**Unit Tests:**
- Test ViewModels in isolation (mock services)
- Test Models with real Lucene.NET indexes
- Test Services independently

**Integration Tests:**
- Test ViewModel + Service interaction
- Test with real Lucene.NET indexes

**UI Tests:**
- Use Avalonia's UI testing framework
- Test critical user workflows

**Example ViewModel Test:**
```csharp
[Fact]
public async Task OverviewViewModel_IndexOpened_UpdatesProperties()
{
    // Arrange
    var mockIndexService = new Mock<IndexService>();
    var mockMessageBus = new Mock<IMessageBus>();
    var viewModel = new OverviewViewModel(
        mockIndexService.Object,
        mockMessageBus.Object);

    var state = new LukeState
    {
        IndexReader = CreateTestReader(),
        IndexPath = "/test/path"
    };

    // Act
    mockIndexService.Raise(x => x.IndexOpened += null, state);

    // Assert
    Assert.Equal("/test/path", viewModel.IndexPath);
    Assert.Equal(5, viewModel.NumFields);
}
```

### 10. Migration Strategy

**Phased Approach:**

**Phase 1: Foundation (Current)**
- ✅ Basic Avalonia app setup
- ✅ Index path selector dialog
- ✅ MainWindow with tab control
- 🔄 Directory implementation selector

**Phase 2: Core Features**
- Overview tab (index statistics, field browser)
- Documents tab (document browser, basic term browsing)
- Basic search functionality
- Preferences/settings

**Phase 3: Advanced Features**
- Search tab (query parser, advanced search)
- Analysis tab (preset and custom analyzers)
- Commits tab (commit history, segment details)

**Phase 4: Tools & Polish**
- Index tools (optimize, check, repair)
- Logs viewer
- Dark mode support
- Performance optimization
- Documentation

**Phase 5: Enhancements (Beyond Java Luke)**
- Index diff/comparison
- Query performance analyzer
- Field statistics visualizations
- Index size analyzer
- Export to various formats (CSV, JSON, Excel)

### 11. Key Differences: Java Swing vs Avalonia

| Aspect | Java Swing | Avalonia/WPF |
|--------|------------|--------------|
| UI Definition | Code-based (imperative) | XAML-based (declarative) |
| Data Binding | Manual via listeners | Automatic via binding expressions |
| Layout | Layout managers (GridBagLayout) | Panel types (Grid, StackPanel, DockPanel) |
| Threading | SwingUtilities.invokeLater() | Dispatcher.InvokeAsync() + Task.Run() for blocking operations |
| Styling | Look & Feel + manual styling | CSS-like styles + themes |
| Properties | Java Beans pattern | INotifyPropertyChanged |
| Events | ActionListener interfaces | Commands + routed events |
| Dialogs | JDialog with manual construction | Window/UserControl + ViewModel |
| Tables | JTable + TableModel | DataGrid + ObservableCollection |

### 12. Code Conversion Examples

**Opening a Dialog:**

Java:
```java
OpenIndexDialogFactory factory = OpenIndexDialogFactory.getInstance();
new DialogOpener<>(factory).open("Open Index", 600, 420, (result) -> {
    // Handle result
});
```

.NET:
```csharp
var viewModel = _serviceProvider.GetRequiredService<OpenIndexDialogViewModel>();
var dialog = new OpenIndexDialog { DataContext = viewModel };
var result = await dialog.ShowDialog<bool>(this);
if (result)
{
    // Handle result
}
```

**Updating UI After Index Opens:**

Java:
```java
private class Observer implements IndexObserver {
    @Override
    public void openIndex(LukeState state) {
        overviewModel = factory.newInstance(state.getIndexReader());
        numFieldsLbl.setText(String.valueOf(overviewModel.getNumFields()));
        numDocsLbl.setText(String.valueOf(overviewModel.getNumDocuments()));
    }
}
```

.NET:
```csharp
// In ViewModel constructor
_indexService.IndexOpened
    .Subscribe(OnIndexOpened);

private void OnIndexOpened(ILukeState state)
{
    _model = new OverviewModel(state.IndexReader);
    NumFields = _model.GetNumFields();  // Automatically updates UI via binding
    NumDocuments = _model.GetNumDocuments();
}
```

**Table Population:**

Java:
```java
termCountsTable.setModel(new TermCountsTableModel(numTerms, termCounts));
```

.NET:
```csharp
TermCounts = new ObservableCollection<TermCountRow>(
    termCounts.Select(kvp => new TermCountRow(kvp.Key, kvp.Value))
);
// DataGrid automatically updates via binding
```

### 13. Lucene.NET Considerations

**API Differences:**
- Some class names differ (e.g., `IndexSearcher` methods)
- Check Lucene.NET documentation for equivalent APIs
- Some advanced features may not be ported yet

**Version Targeting:**
- Use latest stable Lucene.NET (currently 4.8.0-beta00017)
- Stay updated with Lucene.NET releases

**Performance:**
- .NET 9 provides excellent performance
- Consider using `Span<T>` and `Memory<T>` for large data operations

### 14. Additional Features to Consider

**Beyond Java Luke:**

1. **Index Comparison Tool**
   - Compare two indexes side-by-side
   - Show differences in schema, documents, terms

2. **Query Performance Analyzer**
   - Profile query execution
   - Show query plan and timing
   - Identify slow components

3. **Field Statistics Visualizations**
   - Charts for term distribution
   - Field type breakdown (pie chart)
   - Document size distribution

4. **Bulk Operations**
   - Batch document export
   - Batch document modification
   - Bulk re-indexing

5. **Advanced Search UI**
   - Visual query builder (no query syntax knowledge required)
   - Query templates
   - Search result highlighting

6. **Index Monitoring**
   - Watch index for changes
   - Show real-time statistics
   - Alert on issues

7. **Integration Features**
   - Export search results to Excel/CSV
   - Import documents from various formats
   - REST API for automation

---

## Conclusion

Luke is a mature, well-architected application with clear separation of concerns. The Observer pattern, Provider pattern, and Factory pattern are used consistently throughout the codebase. The models layer provides a clean abstraction over Lucene APIs, making it relatively straightforward to port to .NET.

**Key Takeaways for .NET Port:**

1. **Embrace MVVM:** Replace Provider/Observer patterns with ViewModels and data binding
2. **Use Dependency Injection:** Replace singletons and factories with DI container
3. **Leverage Reactive Extensions:** Use ReactiveUI for reactive programming
4. **Keep UI Responsive:** Wrap synchronous Lucene.NET calls in Task.Run() to avoid blocking UI thread
5. **Modern UI:** Take advantage of Avalonia's powerful styling and layout system
6. **Maintain Model Layer:** Keep the models layer largely as-is (translate to C#, keep synchronous)
7. **Improve Testability:** ViewModels are easier to unit test than Swing Providers
8. **Add Modern Features:** Go beyond Java Luke with visualizations and advanced features

**Estimated Effort:**
- Foundation: 2-3 weeks ✅
- Core Features: 4-6 weeks
- Advanced Features: 4-6 weeks
- Polish & Testing: 2-3 weeks
- **Total:** 12-18 weeks for feature parity with Java Luke

**Next Steps:**
1. Complete Phase 1 (foundation with all basic dialogs)
2. Implement Overview tab (Phase 2 start)
3. Implement Documents tab
4. Continue with remaining tabs
5. Add tests throughout development
6. Regular refactoring to improve architecture

This analysis provides a comprehensive roadmap for porting Luke to .NET with modern architecture and UI design principles.
