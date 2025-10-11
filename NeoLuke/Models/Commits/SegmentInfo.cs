namespace NeoLuke.Models.Commits;

/// <summary>
/// Represents information about a segment in a commit
/// </summary>
public class SegmentInfo(string name, int maxDocs, int deletions, long delGen, string version, string codec, long size)
{
    /// <summary>
    /// The segment name
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// Maximum number of documents
    /// </summary>
    public int MaxDocs { get; set; } = maxDocs;

    /// <summary>
    /// Number of deleted documents
    /// </summary>
    public int Deletions { get; set; } = deletions;

    /// <summary>
    /// Deletion generation number
    /// </summary>
    public long DelGen { get; set; } = delGen;

    /// <summary>
    /// Lucene version string
    /// </summary>
    public string Version { get; set; } = version;

    /// <summary>
    /// Codec name
    /// </summary>
    public string Codec { get; set; } = codec;

    /// <summary>
    /// Size in bytes
    /// </summary>
    public long Size { get; set; } = size;

    /// <summary>
    /// Human-readable size
    /// </summary>
    public string HumanReadableSize =>
        Size switch
        {
            < 1024 => $"{Size} B",
            < 1024 * 1024 => $"{Size / 1024.0:F2} KB",
            < 1024 * 1024 * 1024 => $"{Size / (1024.0 * 1024.0):F2} MB",
            _ => $"{Size / (1024.0 * 1024.0 * 1024.0):F2} GB"
        };
}
