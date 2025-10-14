# NeoLuke: Lucene.NET Toolbox

[![.NET CI](https://github.com/paulirwin/neoluke/actions/workflows/dotnet.yml/badge.svg)](https://github.com/paulirwin/neoluke/actions/workflows/dotnet.yml)

A modern, cross-platform desktop GUI application for exploring and managing Apache Lucene.NET indexes. 
NeoLuke provides a clean, intuitive interface for developers and search engineers working with Lucene.NET or supported Lucene indexes.

<img width="2696" height="1726" alt="NeoLuke Screenshot" src="https://github.com/user-attachments/assets/69afda4f-52bc-49e1-862c-a0ee7d2c5d1d" />

## Features

- **Index Overview**: View index statistics, segment information, and field details
- **Documents**: Browse and view documents in your index with full field details
- **Search**: Test queries with multiple analyzers and view relevance scores
- **Analysis**: Analyze text with different tokenizers and see token breakdowns
- **Commits**: View index file and segment details
- **More Like This**: Find similar documents using Lucene's MLT query
- **Export Tools**: Export term lists
- **Index Management**: Check and optimize your indexes
- **Cross-Platform**: Runs on Windows, macOS, and Linux

## Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

## Quick Start

### 1. Clone and Build

```bash
git clone https://github.com/paulirwin/neoluke.git
cd neoluke
dotnet build
```

### 2. Generate a Sample Index (Optional)

If you don't have a Lucene index handy, you can generate a demo index with sample data:

```bash
# On Windows, macOS, or Linux with PowerShell installed
pwsh ./generate-demo.ps1
```

This will create a sample index in the `demo/` directory with test documents you can use to explore NeoLuke's features.

### 3. Run the Application

```bash
dotnet run --project NeoLuke/NeoLuke.csproj
```

Or use the convenience script:

```bash
pwsh ./run.ps1
```

### 4. Open an Index

When NeoLuke starts, you'll be prompted to select a Lucene index directory:

1. Click **Browse** to navigate to your index folder (or use the demo index generated above at `demo/`)
2. Choose whether to open in **read-only mode** (recommended for production indexes)
3. Optionally expand **Expert options** to change settings
4. Click **OK** to open the index

## Using NeoLuke

### Overview Tab
View high-level statistics about your index including:
- Number of documents and fields
- Index version and format
- Segment information
- Field term distribution
- Top terms by frequency

Right-click on any term to search for it across your index.

### Documents Tab
Browse documents by document ID:
- View all fields and their values
- See stored vs indexed field information
- Navigate between documents
- Copy field values

### Search Tab
Test queries against your index:
- Enter queries using Lucene query syntax
- Preview the parsed query
- Choose from available analyzers
- View search results with relevance scores
- Right-click results to **Explain** scoring or **Show all fields**

### Analysis Tab
Analyze how text is tokenized:
- Select an analyzer
- Enter text to analyze
- View the token stream with positions and attributes
- Click tokens to see detailed attributes

### More Like This Tab
Find similar documents:
- Enter a document ID
- Select fields to use for similarity
- Adjust parameters (min term frequency, min doc frequency, etc.)
- View similar documents ranked by relevance

### Commits Tab
Explore commit history:
- View all files in the index
- See segment details
- Inspect segment statistics

### Logs Tab
Monitor application activity and debug issues with real-time logging.

## Menu Features

### File Menu
- **Open Index**: Switch to a different index
- **Reopen Current Index**: Reload the current index to see recent changes
- **Reopen Current Index as Read/Write** (or **Read-Only**): Change access mode of the current index
- **Close Index**: Close the current index

### Tools Menu
- **Optimize Index**: Reduce the number of segments (write mode only)
- **Check Index**: Validate index integrity
- **Export Terms**: Export term lists to CSV or text files

## Development

### Running Tests

```bash
dotnet test
```

### Project Structure

```
lukenet/
├── NeoLuke/                # Main application
│   ├── Assets/             # Static assets (icons, images)
│   ├── Models/             # Data models
│   ├── Services/           # Application services
│   ├── ViewModels/         # MVVM view models
│   └── Views/              # Avalonia XAML views
├── NeoLuke.Tests/          # Unit and integration tests
├── NeoLuke.DemoGenerator/  # Demo index generator
├── demo/                   # Sample index (generated)
└── docs/                   # Documentation (mostly for AI agent use)
```

## Architecture

NeoLuke follows the MVVM (Model-View-ViewModel) pattern using:
- **Avalonia UI** - Cross-platform desktop framework
- **CommunityToolkit.Mvvm** - MVVM helpers and observable properties
- **Lucene.Net 4.8.0** - Full-text search engine library

See [CLAUDE.md](CLAUDE.md) for detailed architecture documentation.

This application was mostly vibe-coded using Claude Code.

## Contributing

Contributions are welcome! This project is a "spirit port" of Apache Lucene's Luke tool, modernized for .NET with Avalonia UI.

Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

This project is licensed under the GPL-3.0 License. See the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Inspired by [Apache Lucene's Luke](https://github.com/apache/lucene/tree/main/lucene/luke) tool
- Built with [Avalonia UI](https://avaloniaui.net/)
- Powered by [Lucene.NET](https://lucenenet.apache.org/)

[![Lucene.NET](https://raw.githubusercontent.com/apache/lucenenet/refs/heads/master/branding/logo/lucene-net-badge-180x36.png)](https://github.com/apache/lucenenet)


