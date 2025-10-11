using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NeoLuke.Models.Commits;
using Microsoft.Extensions.Logging;

namespace NeoLuke.ViewModels;

public partial class CommitsViewModel : ViewModelBase
{
    private readonly ILogger<CommitsViewModel> _logger = Program.LoggerFactory.CreateLogger<CommitsViewModel>();
    private CommitsModel? _commitsModel;

    [ObservableProperty]
    private ObservableCollection<CommitInfo> _commits = [];

    [ObservableProperty]
    private CommitInfo? _selectedCommit;

    [ObservableProperty]
    private string _generationText = string.Empty;

    [ObservableProperty]
    private string _deletedText = string.Empty;

    [ObservableProperty]
    private string _segmentCountText = string.Empty;

    [ObservableProperty]
    private string _userDataText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<CommitFile> _files = [];

    [ObservableProperty]
    private ObservableCollection<SegmentInfo> _segments = [];

    [ObservableProperty]
    private SegmentInfo? _selectedSegment;

    [ObservableProperty]
    private string _segmentDetailsText = string.Empty;

    [ObservableProperty]
    private bool _isIndexLoaded;

    partial void OnSelectedCommitChanged(CommitInfo? value)
    {
        _logger.LogDebug("Commit selection changed to generation: {Generation}", value?.Generation ?? -1);

        if (value != null)
        {
            UpdateCommitDetails(value);
            _ = LoadCommitDataAsync(value);
        }
        else
        {
            ClearCommitDetails();
        }
    }

    partial void OnSelectedSegmentChanged(SegmentInfo? value)
    {
        _logger.LogDebug("Segment selection changed to: {SegmentName}", value?.Name ?? "null");

        if (value != null)
        {
            _ = LoadSegmentDetailsAsync(value);
        }
        else
        {
            SegmentDetailsText = string.Empty;
        }
    }

    private void UpdateCommitDetails(CommitInfo commit)
    {
        GenerationText = commit.Generation.ToString();
        DeletedText = commit.IsDeleted ? "Yes" : "No";
        SegmentCountText = commit.SegmentCount.ToString();
        UserDataText = commit.UserDataString;
    }

    private void ClearCommitDetails()
    {
        GenerationText = string.Empty;
        DeletedText = string.Empty;
        SegmentCountText = string.Empty;
        UserDataText = string.Empty;
        Files.Clear();
        Segments.Clear();
        SegmentDetailsText = string.Empty;
    }

    private async Task LoadCommitDataAsync(CommitInfo commit)
    {
        if (_commitsModel == null)
        {
            _logger.LogWarning("LoadCommitDataAsync called with null model");
            return;
        }

        _logger.LogInformation("Loading files and segments for generation {Generation}", commit.Generation);

        // Load files and segments in background
        var (files, segments) = await Task.Run(() =>
        {
            var filesList = _commitsModel.GetFiles(commit.Generation);
            var segmentsList = _commitsModel.GetSegments(commit.Generation);
            return (filesList, segmentsList);
        });

        _logger.LogInformation("Loaded {FileCount} files and {SegmentCount} segments",
            files.Count, segments.Count);

        // Update collections on UI thread
        Files.Clear();
        foreach (var file in files)
        {
            Files.Add(file);
        }

        Segments.Clear();
        foreach (var segment in segments)
        {
            Segments.Add(segment);
        }
    }

    private async Task LoadSegmentDetailsAsync(SegmentInfo segment)
    {
        if (_commitsModel == null)
        {
            _logger.LogWarning("LoadSegmentDetailsAsync called with null model");
            return;
        }

        _logger.LogInformation("Loading details for segment: {SegmentName}", segment.Name);

        // Load segment attributes and diagnostics in background
        var (attributes, diagnostics) = await Task.Run(() =>
        {
            var attr = _commitsModel.GetSegmentAttributes(segment.Name);
            var diag = _commitsModel.GetSegmentDiagnostics(segment.Name);
            return (attr, diag);
        });

        // Build details text
        var detailsText = $"Segment: {segment.Name}\n\n";

        detailsText += "Attributes:\n";
        if (attributes.Count > 0)
        {
            foreach (var kvp in attributes)
            {
                detailsText += $"  {kvp.Key}: {kvp.Value}\n";
            }
        }
        else
        {
            detailsText += "  (none)\n";
        }

        detailsText += "\nDiagnostics:\n";
        if (diagnostics.Count > 0)
        {
            foreach (var kvp in diagnostics)
            {
                detailsText += $"  {kvp.Key}: {kvp.Value}\n";
            }
        }
        else
        {
            detailsText += "  (none)\n";
        }

        SegmentDetailsText = detailsText;

        _logger.LogDebug("Segment details loaded with {AttrCount} attributes and {DiagCount} diagnostics",
            attributes.Count, diagnostics.Count);
    }

    public async Task LoadIndexAsync(CommitsModel model)
    {
        _logger.LogInformation("Loading commits for index");
        _commitsModel = model;

        // Load commits list in background
        var commitsList = await Task.Run(() => model.ListCommits());

        _logger.LogInformation("Found {CommitCount} commits", commitsList.Count);

        Commits.Clear();
        foreach (var commit in commitsList)
        {
            Commits.Add(commit);
        }

        // Select the most recent commit (last in the list)
        if (Commits.Count > 0)
        {
            SelectedCommit = Commits.Last();
        }

        IsIndexLoaded = true;
        _logger.LogInformation("Commits tab initialized successfully");
    }

    public void ClearIndex()
    {
        _logger.LogInformation("Clearing commits data");

        _commitsModel = null;
        Commits.Clear();
        SelectedCommit = null;
        ClearCommitDetails();
        IsIndexLoaded = false;
    }
}
