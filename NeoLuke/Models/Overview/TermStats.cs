using System;

namespace NeoLuke.Models.Overview;

/// <summary>
/// Represents statistics for a term in a field
/// </summary>
public class TermStats(string term, int frequency)
{
    /// <summary>
    /// The term text
    /// </summary>
    public string Term { get; init; } = term ?? throw new ArgumentNullException(nameof(term));

    /// <summary>
    /// The number of times this term appears (document frequency)
    /// </summary>
    public int Frequency { get; init; } = frequency;
}
