using System.Collections.Generic;
using System.Linq;

namespace NeoLuke.Models.Commits;

/// <summary>
/// Represents information about a commit point in a Lucene index
/// </summary>
public class CommitInfo(long generation, bool isDeleted, int segmentCount, IDictionary<string, string>? userData)
{
    /// <summary>
    /// The commit generation number
    /// </summary>
    public long Generation { get; set; } = generation;

    /// <summary>
    /// Whether this commit has been deleted
    /// </summary>
    public bool IsDeleted { get; set; } = isDeleted;

    /// <summary>
    /// The number of segments in this commit
    /// </summary>
    public int SegmentCount { get; set; } = segmentCount;

    /// <summary>
    /// User data associated with this commit
    /// </summary>
    public IDictionary<string, string>? UserData { get; set; } = userData;

    /// <summary>
    /// Formatted display string for the commit (just the generation number)
    /// </summary>
    public string DisplayString => Generation.ToString();

    /// <summary>
    /// User data formatted as a string
    /// </summary>
    public string UserDataString =>
        UserData == null || UserData.Count == 0
            ? "---"
            : string.Join(", ", UserData.Select(kvp => $"{kvp.Key}={kvp.Value}"));
}
