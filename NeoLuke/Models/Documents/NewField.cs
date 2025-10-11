namespace NeoLuke.Models.Documents;

/// <summary>
/// Represents a new field to be added to a document
/// </summary>
public class NewField
{
    /// <summary>
    /// The name of the field
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The value of the field
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// The type of field
    /// </summary>
    public FieldType FieldType { get; set; } = FieldType.TextField;

    /// <summary>
    /// Whether the field should be stored
    /// </summary>
    public bool IsStored { get; set; } = true;

    /// <summary>
    /// Creates a new instance with default values
    /// </summary>
    public static NewField CreateDefault(string name = "", string value = "")
    {
        return new NewField
        {
            Name = name,
            Value = value,
            FieldType = FieldType.TextField,
            IsStored = true
        };
    }
}

/// <summary>
/// Supported Lucene field types for adding documents
/// </summary>
public enum FieldType
{
    /// <summary>
    /// Text field that is tokenized and indexed
    /// </summary>
    TextField,

    /// <summary>
    /// String field that is not tokenized but indexed
    /// </summary>
    StringField,

    /// <summary>
    /// 32-bit integer field for numeric range queries
    /// </summary>
    Int32Field,

    /// <summary>
    /// 64-bit integer field for numeric range queries
    /// </summary>
    Int64Field,

    /// <summary>
    /// Single-precision floating point field for numeric range queries
    /// </summary>
    SingleField,

    /// <summary>
    /// Double-precision floating point field for numeric range queries
    /// </summary>
    DoubleField,

    /// <summary>
    /// Stored field that is only stored, not indexed
    /// </summary>
    StoredField
}
