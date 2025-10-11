using System;

namespace NeoLuke.Models.Documents;

/// <summary>
/// Represents a field in a Lucene document
/// </summary>
public class DocumentField(string fieldName, string? norm, string? fieldValue, bool isDeleted = false)
{
    /// <summary>
    /// The name of the field
    /// </summary>
    public string FieldName { get; init; } = fieldName ?? throw new ArgumentNullException(nameof(fieldName));

    /// <summary>
    /// The normalization value for the field
    /// </summary>
    public string Norm { get; init; } = norm ?? string.Empty;

    /// <summary>
    /// The value of the field
    /// </summary>
    public string FieldValue { get; init; } = fieldValue ?? string.Empty;

    /// <summary>
    /// Whether this document is deleted
    /// </summary>
    public bool IsDeleted { get; init; } = isDeleted;
}
