using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using NeoLuke.Models.Search;

namespace NeoLuke.ViewModels;

public partial class ExplainDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "Score Explanation";

    [ObservableProperty]
    private ObservableCollection<ExplainNode> _rootNodes = [];

    private readonly string _fullExplanationText = string.Empty;

    public ExplainDialogViewModel()
    {
        // Default constructor for designer
    }

    public ExplainDialogViewModel(int docId, Lucene.Net.Search.Explanation explanation)
    {
        Title = $"Score Explanation for Doc {docId}";

        // Build the tree from the explanation
        var rootNode = ExplainNode.FromExplanation(explanation);
        RootNodes.Add(rootNode);

        // Store the full text for clipboard
        _fullExplanationText = rootNode.ToFormattedString();
    }

    /// <summary>
    /// Copies the explanation text to the clipboard
    /// </summary>
    public async Task CopyToClipboardAsync(IClipboard clipboard)
    {
        if (!string.IsNullOrEmpty(_fullExplanationText))
        {
            await clipboard.SetTextAsync(_fullExplanationText);
        }
    }
}
