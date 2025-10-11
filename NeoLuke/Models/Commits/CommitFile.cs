namespace NeoLuke.Models.Commits;

/// <summary>
/// Represents a file that is part of a commit
/// </summary>
public class CommitFile
{
    /// <summary>
    /// The filename
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// The file size in bytes
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Human-readable file size
    /// </summary>
    public string HumanReadableSize
    {
        get
        {
            if (Size < 1024)
                return $"{Size} B";
            else if (Size < 1024 * 1024)
                return $"{Size / 1024.0:F2} KB";
            else if (Size < 1024 * 1024 * 1024)
                return $"{Size / (1024.0 * 1024.0):F2} MB";
            else
                return $"{Size / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
    }

    public CommitFile(string fileName, long size)
    {
        FileName = fileName;
        Size = size;
    }
}
