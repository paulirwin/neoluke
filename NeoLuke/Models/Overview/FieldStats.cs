using System;

namespace NeoLuke.Models.Overview;

/// <summary>
/// Represents statistics for a field in the index
/// </summary>
public class FieldStats(string fieldName, long termCount, double percentage)
{
    /// <summary>
    /// The name of the field
    /// </summary>
    public string FieldName { get; init; } = fieldName ?? throw new ArgumentNullException(nameof(fieldName));

    /// <summary>
    /// The number of terms in this field
    /// </summary>
    public long TermCount { get; init; } = termCount;

    /// <summary>
    /// The percentage of total terms that this field represents
    /// </summary>
    public double Percentage { get; init; } = percentage;
}
