using System.Collections.ObjectModel;

namespace NeoLuke.Models.Search;

/// <summary>
/// Represents a node in the explanation tree
/// </summary>
public class ExplainNode
{
    /// <summary>
    /// The text to display for this node (formatted as "value description")
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Child nodes in the explanation hierarchy
    /// </summary>
    public ObservableCollection<ExplainNode> Children { get; set; } = [];

    /// <summary>
    /// Creates an ExplainNode from a Lucene Explanation object
    /// </summary>
    public static ExplainNode FromExplanation(Lucene.Net.Search.Explanation explanation)
    {
        var node = new ExplainNode
        {
            Text = FormatExplanation(explanation)
        };

        // Recursively add child nodes
        var details = explanation.GetDetails();
        if (details is { Length: > 0 })
        {
            foreach (var detail in details)
            {
                node.Children.Add(FromExplanation(detail));
            }
        }

        return node;
    }

    /// <summary>
    /// Formats an explanation as "value description"
    /// </summary>
    private static string FormatExplanation(Lucene.Net.Search.Explanation explanation)
    {
        return $"{explanation.Value} {explanation.Description}";
    }

    /// <summary>
    /// Converts the entire tree to a formatted string (for clipboard)
    /// </summary>
    public string ToFormattedString(int depth = 0)
    {
        var indent = new string(' ', depth * 2);
        var result = indent + Text + "\n";

        foreach (var child in Children)
        {
            result += child.ToFormattedString(depth + 1);
        }

        return result;
    }
}
