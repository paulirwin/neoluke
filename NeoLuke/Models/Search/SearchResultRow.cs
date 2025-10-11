namespace NeoLuke.Models.Search;

/// <summary>
/// Represents a single search result row in the search results DataGrid.
/// </summary>
public class SearchResultRow
{
    /// <summary>
    /// The Lucene document ID.
    /// </summary>
    public int DocId { get; set; }

    /// <summary>
    /// The search score for this result.
    /// </summary>
    public float Score { get; set; }

    /// <summary>
    /// Field values for this document, displayed as key-value pairs.
    /// This will be formatted as a string for display in the DataGrid.
    /// </summary>
    public string FieldValues { get; set; } = string.Empty;
}
