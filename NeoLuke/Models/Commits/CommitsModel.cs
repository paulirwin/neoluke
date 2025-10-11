using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using LuceneDirectory = Lucene.Net.Store.Directory;

namespace NeoLuke.Models.Commits;

/// <summary>
/// Model for the Commits tab that provides commit history and segment information
/// </summary>
public class CommitsModel(LuceneDirectory directory)
{
    private readonly LuceneDirectory _directory = directory ?? throw new ArgumentNullException(nameof(directory));

    /// <summary>
    /// Lists all commits in the index
    /// </summary>
    public List<CommitInfo> ListCommits()
    {
        var commits = new List<CommitInfo>();

        try
        {
            var commitList = DirectoryReader.ListCommits(_directory);

            foreach (var commit in commitList)
            {
                var userData = commit.UserData;
                var userDataDict = userData != null
                    ? userData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                    : null;

                commits.Add(new CommitInfo(
                    generation: commit.Generation,
                    isDeleted: commit.IsDeleted,
                    segmentCount: commit.SegmentCount,
                    userData: userDataDict
                ));
            }
        }
        catch (Exception)
        {
            // If we can't list commits, return empty list
        }

        return commits;
    }

    /// <summary>
    /// Gets the files for a specific commit
    /// </summary>
    public List<CommitFile> GetFiles(long generation)
    {
        var files = new List<CommitFile>();

        try
        {
            var commits = DirectoryReader.ListCommits(_directory);
            var commit = commits.FirstOrDefault(c => c.Generation == generation);

            if (commit != null)
            {
                foreach (var fileName in commit.FileNames)
                {
                    long fileSize;

                    try
                    {
                        // Try to get file size from directory
                        fileSize = _directory.FileLength(fileName);
                    }
                    catch
                    {
                        // If we can't get the size, use 0
                        fileSize = 0;
                    }

                    files.Add(new CommitFile(fileName, fileSize));
                }
            }
        }
        catch (Exception)
        {
            // If we can't get files, return empty list
        }

        return files;
    }

    /// <summary>
    /// Gets the segments for a specific commit
    /// </summary>
    public List<SegmentInfo> GetSegments(long generation)
    {
        var segments = new List<SegmentInfo>();

        try
        {
            var commits = DirectoryReader.ListCommits(_directory);
            var commit = commits.FirstOrDefault(c => c.Generation == generation);

            if (commit != null)
            {
                // Open a reader for this specific commit
                using var reader = DirectoryReader.Open(commit);

                // Iterate through all segment readers
                foreach (var leaf in reader.Leaves)
                {
                    if (leaf.Reader is SegmentReader segReader)
                    {
                        var segInfo = segReader.SegmentInfo;
                        var info = segInfo.Info;

                        // Get segment size by summing all file sizes
                        long segmentSize = 0;
                        try
                        {
                            foreach (var file in segInfo.GetFiles())
                            {
                                try
                                {
                                    segmentSize += _directory.FileLength(file);
                                }
                                catch
                                {
                                    // Skip files we can't read
                                }
                            }
                        }
                        catch
                        {
                            // If we can't calculate size, use 0
                        }

                        segments.Add(new SegmentInfo(
                            name: info.Name,
                            maxDocs: info.DocCount,
                            deletions: segInfo.DelCount,
                            delGen: segInfo.DelGen,
                            version: info.Version ?? "Unknown",
                            codec: info.Codec?.Name ?? "Unknown",
                            size: segmentSize
                        ));
                    }
                }
            }
        }
        catch (Exception)
        {
            // If we can't get segments, return empty list
        }

        return segments;
    }

    /// <summary>
    /// Gets detailed attributes for a segment
    /// </summary>
    public Dictionary<string, string> GetSegmentAttributes(string segmentName)
    {
        var attributes = new Dictionary<string, string>();

        try
        {
            // Open the directory reader
            using var reader = DirectoryReader.Open(_directory);

            // Find the segment
            foreach (var leaf in reader.Leaves)
            {
                if (leaf.Reader is SegmentReader segReader)
                {
                    var segInfo = segReader.SegmentInfo;
                    if (segInfo.Info.Name == segmentName)
                    {
                        // Add basic segment info as attributes
                        attributes["Name"] = segInfo.Info.Name;
                        attributes["DocCount"] = segInfo.Info.DocCount.ToString();
                        attributes["UseCompoundFile"] = segInfo.Info.UseCompoundFile.ToString();
                        attributes["Version"] = segInfo.Info.Version ?? "Unknown";
                        attributes["Codec"] = segInfo.Info.Codec?.Name ?? "Unknown";
                        break;
                    }
                }
            }
        }
        catch (Exception)
        {
            // If we can't get attributes, return empty dictionary
        }

        return attributes;
    }

    /// <summary>
    /// Gets diagnostics for a segment
    /// </summary>
    public Dictionary<string, string> GetSegmentDiagnostics(string segmentName)
    {
        var diagnostics = new Dictionary<string, string>();

        try
        {
            // Open the directory reader
            using var reader = DirectoryReader.Open(_directory);

            // Find the segment
            foreach (var leaf in reader.Leaves)
            {
                if (leaf.Reader is SegmentReader segReader)
                {
                    var segInfo = segReader.SegmentInfo;
                    if (segInfo.Info.Name == segmentName)
                    {
                        // Get diagnostics from the segment info
                        var infoDiagnostics = segInfo.Info.Diagnostics;
                        if (infoDiagnostics != null)
                        {
                            foreach (var kvp in infoDiagnostics)
                            {
                                diagnostics[kvp.Key] = kvp.Value;
                            }
                        }
                        break;
                    }
                }
            }
        }
        catch (Exception)
        {
            // If we can't get diagnostics, return empty dictionary
        }

        return diagnostics;
    }
}
