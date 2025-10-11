using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Lucene.Net.Analysis.Standard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace NeoLuke.Models.Documents;

/// <summary>
/// Model for the Documents tab that provides document browsing functionality
/// </summary>
public class DocumentsModel(IndexReader reader, Directory directory, bool isReadOnly) : IDisposable
{
    private readonly IndexReader _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    private readonly Directory _directory = directory ?? throw new ArgumentNullException(nameof(directory));

    /// <summary>
    /// Gets whether the index is opened in read-only mode
    /// </summary>
    public bool IsReadOnly => isReadOnly;

    /// <summary>
    /// Gets the total number of documents in the index (excluding deleted docs)
    /// </summary>
    public int GetMaxDoc() => _reader.MaxDoc;

    /// <summary>
    /// Gets the number of documents (excluding deleted docs)
    /// </summary>
    public int GetNumDocs() => _reader.NumDocs;

    /// <summary>
    /// Gets a document by its document ID
    /// </summary>
    /// <param name="docId">The document ID (0-based)</param>
    /// <returns>List of field values for the document</returns>
    public List<DocumentField> GetDocument(int docId)
    {
        if (docId < 0 || docId >= _reader.MaxDoc)
        {
            throw new ArgumentOutOfRangeException(nameof(docId),
                $"Document ID must be between 0 and {_reader.MaxDoc - 1}");
        }

        // Check if document is deleted
        var liveDocs = MultiFields.GetLiveDocs(_reader);
        bool isDeleted = liveDocs != null && !liveDocs.Get(docId);

        if (isDeleted)
        {
            return [new DocumentField("(deleted)", string.Empty, "(This document has been deleted)", true)];
        }

        var document = _reader.Document(docId);
        var fields = new List<DocumentField>();

        // Get the leaf reader context for this document to access norms
        var leaves = _reader.Leaves;
        AtomicReaderContext? leafContext = null;
        int docIdInLeaf = docId;

        foreach (var leaf in leaves)
        {
            if (docId >= leaf.DocBase && docId < leaf.DocBase + leaf.Reader.MaxDoc)
            {
                leafContext = leaf;
                docIdInLeaf = docId - leaf.DocBase;
                break;
            }
        }

        foreach (IIndexableField field in document.Fields)
        {
            string fieldName = field.Name;
            string fieldValue = field.GetStringValue() ?? "(binary data)";
            string norm = string.Empty;

            // Try to get norm value for this field
            if (leafContext != null)
            {
                try
                {
                    var norms = leafContext.AtomicReader.GetNormValues(fieldName);
                    if (norms != null)
                    {
                        long normValue = norms.Get(docIdInLeaf);
                        norm = normValue.ToString();
                    }
                }
                catch
                {
                    // Field doesn't have norms or error reading norms
                    norm = string.Empty;
                }
            }

            fields.Add(new DocumentField(fieldName, norm, fieldValue, false));
        }

        return fields.OrderBy(f => f.FieldName).ToList();
    }

    /// <summary>
    /// Checks if a document ID is valid and not deleted
    /// </summary>
    public bool IsValidDocId(int docId)
    {
        if (docId < 0 || docId >= _reader.MaxDoc)
            return false;

        var liveDocs = MultiFields.GetLiveDocs(_reader);
        if (liveDocs != null && !liveDocs.Get(docId))
            return false;

        return true;
    }

    /// <summary>
    /// Finds the first non-deleted document in the index
    /// </summary>
    /// <returns>The document ID of the first non-deleted document, or null if none found</returns>
    public int? FindFirstNonDeletedDocument()
    {
        var liveDocs = MultiFields.GetLiveDocs(_reader);

        for (int docId = 0; docId < _reader.MaxDoc; docId++)
        {
            // If liveDocs is null, all documents are live
            // If liveDocs.Get(docId) is true, the document is live
            if (liveDocs == null || liveDocs.Get(docId))
            {
                return docId;
            }
        }

        return null;
    }

    /// <summary>
    /// Deletes a document by its document ID
    /// </summary>
    /// <param name="docId">The document ID to delete</param>
    /// <exception cref="InvalidOperationException">Thrown when index is opened in read-only mode</exception>
    public void DeleteDocument(int docId)
    {
        if (isReadOnly)
        {
            throw new InvalidOperationException("Cannot delete documents when index is opened in read-only mode.");
        }

        if (docId < 0 || docId >= _reader.MaxDoc)
        {
            throw new ArgumentOutOfRangeException(nameof(docId),
                $"Document ID must be between 0 and {_reader.MaxDoc - 1}");
        }

        // Check if document is already deleted
        var liveDocs = MultiFields.GetLiveDocs(_reader);
        bool isDeleted = liveDocs != null && !liveDocs.Get(docId);

        if (isDeleted)
        {
            throw new InvalidOperationException($"Document {docId} is already deleted.");
        }

        // Create an IndexWriter to delete the document
        // Note: We need to use the same IndexWriterConfig as when the index was created
        var config = new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, null);

        using var writer = new IndexWriter(_directory, config);
        // Delete by document ID - we need to get a unique term from the document
        // Since we don't have a unique term readily available, we'll use the internal docID
        // through a query. However, Lucene.NET doesn't support deleting by internal docID directly.
        // We need to use a Term that uniquely identifies this document.

        // For now, we'll get all fields from the document and create a query
        // that matches all fields to uniquely identify it
        var document = _reader.Document(docId);

        // Find a unique field to delete by (prefer _id or id fields if they exist)
        IIndexableField? uniqueField = null;
        foreach (var fieldName in new[] { "_id", "id", "uid", "docid" })
        {
            uniqueField = document.GetField(fieldName);
            if (uniqueField != null)
                break;
        }

        if (uniqueField == null)
        {
            // If no standard ID field exists, use the first stored field
            uniqueField = document.Fields.FirstOrDefault();
        }

        if (uniqueField != null)
        {
            var term = new Term(uniqueField.Name, uniqueField.GetStringValue());
            writer.DeleteDocuments(term);
            writer.Commit();
        }
        else
        {
            throw new InvalidOperationException($"Cannot delete document {docId}: no suitable fields found to identify the document.");
        }
    }

    /// <summary>
    /// Adds a new document to the index
    /// </summary>
    /// <param name="fields">List of fields to add</param>
    /// <exception cref="InvalidOperationException">Thrown when index is opened in read-only mode</exception>
    public void AddDocument(List<NewField> fields)
    {
        if (isReadOnly)
        {
            throw new InvalidOperationException("Cannot add documents when index is opened in read-only mode.");
        }

        if (fields == null || fields.Count == 0)
        {
            throw new ArgumentException("At least one field is required to add a document.", nameof(fields));
        }

        // Filter out empty fields
        var validFields = fields.Where(f => !string.IsNullOrWhiteSpace(f.Name) && !string.IsNullOrWhiteSpace(f.Value)).ToList();

        if (validFields.Count == 0)
        {
            throw new ArgumentException("At least one field with name and value is required.", nameof(fields));
        }

        // Create an IndexWriter to add the document
        var config = new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48));

        using var writer = new IndexWriter(_directory, config);
        var document = new Document();

        foreach (var field in validFields)
        {
            document.Add(CreateLuceneField(field));
        }

        writer.AddDocument(document);
        writer.Commit();
    }

    /// <summary>
    /// Converts a NewField to a Lucene IIndexableField
    /// </summary>
    private IIndexableField CreateLuceneField(NewField field)
    {
        var store = field.IsStored ? Field.Store.YES : Field.Store.NO;

        return field.FieldType switch
        {
            FieldType.TextField => new TextField(field.Name, field.Value, store),
            FieldType.StringField => new StringField(field.Name, field.Value, store),
            FieldType.Int32Field => new Int32Field(field.Name, ParseInt(field.Value), store),
            FieldType.Int64Field => new Int64Field(field.Name, ParseLong(field.Value), store),
            FieldType.SingleField => new SingleField(field.Name, ParseFloat(field.Value), store),
            FieldType.DoubleField => new DoubleField(field.Name, ParseDouble(field.Value), store),
            FieldType.StoredField => new StoredField(field.Name, field.Value),
            _ => new TextField(field.Name, field.Value, store)
        };
    }

    private int ParseInt(string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            return result;
        throw new ArgumentException($"Invalid integer value: {value}");
    }

    private long ParseLong(string value)
    {
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result))
            return result;
        throw new ArgumentException($"Invalid long value: {value}");
    }

    private float ParseFloat(string value)
    {
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
            return result;
        throw new ArgumentException($"Invalid float value: {value}");
    }

    private double ParseDouble(string value)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            return result;
        throw new ArgumentException($"Invalid double value: {value}");
    }

    public void Dispose()
    {
        // Nothing to dispose in this model - the reader and directory are managed by MainWindow
    }
}
