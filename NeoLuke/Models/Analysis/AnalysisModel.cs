using Lucene.Net.Analysis.TokenAttributes;
using NeoLuke.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NeoLuke.Models.Analysis;

/// <summary>
/// Model for the Analysis tab that handles text analysis using Lucene.NET analyzers
/// </summary>
public class AnalysisModel
{
    /// <summary>
    /// Analyzes input text using the specified analyzer type
    /// </summary>
    /// <param name="analyzerType">The type of analyzer to use</param>
    /// <param name="inputText">The text to analyze</param>
    /// <returns>List of analyzed tokens with their attributes</returns>
    public List<AnalyzedToken> AnalyzeText(Type analyzerType, string inputText)
    {
        if (analyzerType == null)
            throw new ArgumentNullException(nameof(analyzerType));

        if (string.IsNullOrWhiteSpace(inputText))
            return [];

        // Create analyzer instance using shared utility
        using var analyzer = AnalyzerDiscovery.CreateAnalyzer(analyzerType);
        var tokens = new List<AnalyzedToken>();

        // Tokenize the input text
        using var stringReader = new StringReader(inputText);
        using var tokenStream = analyzer.GetTokenStream("field", stringReader);

        // Get token attributes - check if they exist first
        // Some attributes are required, others are optional
        var termAttr = tokenStream.GetAttribute<ICharTermAttribute>();
        var offsetAttr = tokenStream.GetAttribute<IOffsetAttribute>();

        // Optional attributes - not all analyzers provide these
        var hasPositionIncrement = tokenStream.HasAttribute<IPositionIncrementAttribute>();
        var posIncrAttr = hasPositionIncrement
            ? tokenStream.GetAttribute<IPositionIncrementAttribute>()
            : null;

        var hasTypeAttribute = tokenStream.HasAttribute<ITypeAttribute>();
        var typeAttr = hasTypeAttribute
            ? tokenStream.GetAttribute<ITypeAttribute>()
            : null;

        tokenStream.Reset();

        int position = -1;
        while (tokenStream.IncrementToken())
        {
            // Use position increment if available, otherwise default to 1
            position += posIncrAttr?.PositionIncrement ?? 1;

            // Collect all attributes for this token
            var attributes = new Dictionary<string, string>
            {
                ["term"] = termAttr.ToString(),
                ["position"] = position.ToString(),
                ["startOffset"] = offsetAttr.StartOffset.ToString(),
                ["endOffset"] = offsetAttr.EndOffset.ToString(),
                ["type"] = typeAttr?.Type ?? "word"
            };

            // Capture detailed attribute information for the details dialog
            var detailedAttributes = new List<TokenAttributeDetail>();

            // Add all attributes from the token stream
            var attributeTypeEnumerator = tokenStream.GetAttributeClassesEnumerator();
            while (attributeTypeEnumerator.MoveNext())
            {
                var attrType = attributeTypeEnumerator.Current;

                if (attrType == null)
                    continue;

                // Get the actual attribute instance for this type
                // Use reflection to call GetAttribute<T>() where T is the attribute type
                var getAttributeMethod = typeof(Lucene.Net.Util.AttributeSource)
                    .GetMethod("GetAttribute", Type.EmptyTypes);

                if (getAttributeMethod == null)
                    continue;

                var genericMethod = getAttributeMethod.MakeGenericMethod(attrType);
                var attr = genericMethod.Invoke(tokenStream, null);

                if (attr == null)
                    continue;

                var attrTypeName = attrType.Name;

                // Get all properties of this attribute
                var properties = attrType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    try
                    {
                        var value = prop.GetValue(attr);
                        string displayValue;

                        // Special handling for char arrays - convert to string
                        if (value is char[] charArray)
                        {
                            displayValue = new string(charArray);
                        }
                        else
                        {
                            displayValue = value?.ToString() ?? "(null)";
                        }

                        detailedAttributes.Add(new TokenAttributeDetail
                        {
                            AttributeTypeName = attrTypeName,
                            PropertyName = prop.Name,
                            Value = displayValue
                        });
                    }
                    catch
                    {
                        // Skip properties that can't be read
                    }
                }
            }

            tokens.Add(new AnalyzedToken
            {
                Term = termAttr.ToString(),
                Position = position,
                StartOffset = offsetAttr.StartOffset,
                EndOffset = offsetAttr.EndOffset,
                Type = typeAttr?.Type ?? "word",
                Attributes = attributes,
                DetailedAttributes = detailedAttributes
            });
        }

        tokenStream.End();

        return tokens;
    }
}

/// <summary>
/// Represents a single token produced by analysis
/// </summary>
public class AnalyzedToken
{
    public string Term { get; set; } = string.Empty;
    public int Position { get; set; }
    public int StartOffset { get; set; }
    public int EndOffset { get; set; }
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, string> Attributes { get; set; } = new();
    public List<TokenAttributeDetail> DetailedAttributes { get; set; } = [];

    /// <summary>
    /// Returns a formatted string of all attributes for display
    /// </summary>
    public string AttributesDisplay => string.Join(", ",
        Attributes.Select(kvp => $"{kvp.Key}={kvp.Value}"));
}

/// <summary>
/// Represents detailed information about a token attribute
/// </summary>
public class TokenAttributeDetail
{
    public string AttributeTypeName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
