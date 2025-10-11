using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;

namespace NeoLuke.Utilities;

/// <summary>
/// Represents information about a discovered analyzer type
/// </summary>
public class AnalyzerInfo
{
    public Type Type { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string SimpleName { get; set; } = null!;
}

/// <summary>
/// Utility class for discovering and working with Lucene.NET analyzers
/// </summary>
public static class AnalyzerDiscovery
{
    private static List<AnalyzerInfo>? _cachedAnalyzers;

    /// <summary>
    /// Static constructor to ensure analyzer assemblies are loaded
    /// </summary>
    static AnalyzerDiscovery()
    {
        var log = new StringBuilder();
        log.AppendLine("AnalyzerDiscovery static initialization:");

        try
        {
            // Force load the analyzer assemblies by explicitly loading them
            // This ensures analyzer types are discoverable via reflection
            var assemblyNames = new[]
            {
                "Lucene.Net.Analysis.Common",
                "Lucene.Net.Analysis.Standard",
                "Lucene.Net.Analysis.Kuromoji",
                "Lucene.Net.Analysis.Morfologik",
                "Lucene.Net.Analysis.SmartCn",
                "Lucene.Net.Analysis.Stempel"
            };

            foreach (var assemblyName in assemblyNames)
            {
                try
                {
                    var assembly = Assembly.Load(assemblyName);
                    log.AppendLine($"  ✓ Loaded assembly: {assembly.GetName().Name} v{assembly.GetName().Version}");
                }
                catch (Exception ex)
                {
                    log.AppendLine($"  ✗ Failed to load assembly {assemblyName}: {ex.Message}");
                }
            }

            // Also reference the common types to ensure they're available
            _ = typeof(StandardAnalyzer);
            _ = typeof(WhitespaceAnalyzer);
            _ = typeof(SimpleAnalyzer);
            _ = typeof(KeywordAnalyzer);
            _ = typeof(StopAnalyzer);

            log.AppendLine("  ✓ Common analyzer types referenced");
            log.Append("  ✓ Static initialization completed");

            var logger = Program.LoggerFactory.CreateLogger(typeof(AnalyzerDiscovery));
            logger.LogDebug(log.ToString());
        }
        catch (Exception ex)
        {
            log.AppendLine($"  ✗ Static initialization failed: {ex}");

            var logger = Program.LoggerFactory.CreateLogger(typeof(AnalyzerDiscovery));
            logger.LogError(log.ToString());
        }
    }

    /// <summary>
    /// Discovers all public, concrete Analyzer implementations via reflection
    /// </summary>
    /// <param name="ignoreCache">If true, bypasses the cache and rediscovers analyzers</param>
    public static List<AnalyzerInfo> DiscoverAnalyzers(bool ignoreCache = false)
    {
        var logger = Program.LoggerFactory.CreateLogger(typeof(AnalyzerDiscovery));
        var log = new StringBuilder();
        log.AppendLine($"Analyzer Discovery Report (ignoreCache={ignoreCache}):");

        if (!ignoreCache && _cachedAnalyzers != null)
        {
            // Return cached analyzers silently - no need to log on every access
            return _cachedAnalyzers;
        }

        var analyzerBaseType = typeof(Analyzer);
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        log.AppendLine($"  📦 Scanning {assemblies.Length} assemblies");

        // Track assembly scan details
        var assemblyScanDetails = new StringBuilder();

        // First find all analyzer types
        var allAnalyzerTypes = assemblies
            .SelectMany(assembly =>
            {
                try
                {
                    var types = assembly.GetTypes();
                    assemblyScanDetails.AppendLine($"    • {assembly.GetName().Name}: {types.Length} types");
                    return types;
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Return types that were successfully loaded
                    var types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
                    assemblyScanDetails.AppendLine($"    ⚠ {assembly.GetName().Name}: ReflectionTypeLoadException, loaded {types.Length} types");
                    return types;
                }
                catch (Exception ex)
                {
                    assemblyScanDetails.AppendLine($"    ✗ {assembly.GetName().Name}: {ex.Message}");
                    return [];
                }
            })
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                (type.IsPublic || type.IsNestedPublic) &&
                analyzerBaseType.IsAssignableFrom(type))
            .ToList();

        log.Append(assemblyScanDetails);
        log.AppendLine($"  🔍 Found {allAnalyzerTypes.Count} analyzer types");

        // Track excluded analyzers
        var excludedAnalyzers = new List<string>();

        // Filter to only those with compatible constructors
        var analyzers = allAnalyzerTypes
            .Where(type =>
            {
                var hasCompatible = HasCompatibleConstructor(type);
                if (!hasCompatible)
                {
                    excludedAnalyzers.Add(type.Name);
                }
                return hasCompatible;
            })
            .Select(type => new AnalyzerInfo
            {
                Type = type,
                SimpleName = type.Name,
                DisplayName = type.FullName ?? type.Name
            })
            .OrderBy(info => info.SimpleName)
            .ToList();

        if (excludedAnalyzers.Any())
        {
            log.AppendLine($"  ⊘ Excluded {excludedAnalyzers.Count} analyzers without compatible constructors:");
            foreach (var excluded in excludedAnalyzers)
            {
                log.AppendLine($"    - {excluded}");
            }
        }

        log.AppendLine($"  ✓ Discovered {analyzers.Count} compatible analyzers:");
        foreach (var analyzer in analyzers)
        {
            log.AppendLine($"    ✓ {analyzer.SimpleName}");
        }

        logger.LogInformation(log.ToString());

        _cachedAnalyzers = analyzers;
        return analyzers;
    }

    /// <summary>
    /// Checks if an analyzer type has a compatible constructor
    /// (either parameterless or taking LuceneVersion)
    /// </summary>
    private static bool HasCompatibleConstructor(Type analyzerType)
    {
        // Check for LuceneVersion constructor
        var versionCtor = analyzerType.GetConstructor([typeof(LuceneVersion)]);
        if (versionCtor != null)
            return true;

        // Check for parameterless constructor
        var parameterlessCtor = analyzerType.GetConstructor(Type.EmptyTypes);
        if (parameterlessCtor != null)
            return true;

        return false;
    }

    /// <summary>
    /// Creates an analyzer instance from the specified type
    /// </summary>
    public static Analyzer CreateAnalyzer(Type analyzerType)
    {
        if (analyzerType == null)
            throw new ArgumentNullException(nameof(analyzerType));

        if (!typeof(Analyzer).IsAssignableFrom(analyzerType))
            throw new ArgumentException($"Type {analyzerType.FullName} is not an Analyzer", nameof(analyzerType));

        try
        {
            // Try to create with LuceneVersion.LUCENE_48 parameter
            var versionCtor = analyzerType.GetConstructor([typeof(LuceneVersion)]);
            if (versionCtor != null)
            {
                return (Analyzer)Activator.CreateInstance(analyzerType, LuceneVersion.LUCENE_48)!;
            }

            // Try parameterless constructor
            var parameterlessCtor = analyzerType.GetConstructor(Type.EmptyTypes);
            if (parameterlessCtor != null)
            {
                return (Analyzer)Activator.CreateInstance(analyzerType)!;
            }

            throw new InvalidOperationException(
                $"Analyzer type {analyzerType.FullName} does not have a compatible constructor");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create analyzer instance of type {analyzerType.FullName}", ex);
        }
    }

    /// <summary>
    /// Gets the display name for an analyzer type
    /// </summary>
    public static string GetDisplayName(Type? analyzerType)
    {
        return analyzerType?.Name ?? "Unknown";
    }

    /// <summary>
    /// Finds an analyzer by simple name (e.g., "StandardAnalyzer")
    /// </summary>
    /// <param name="name">The simple name of the analyzer to find</param>
    /// <param name="ignoreCache">If true, bypasses the cache and rediscovers analyzers</param>
    public static AnalyzerInfo? FindByName(string name, bool ignoreCache = false)
    {
        var analyzers = DiscoverAnalyzers(ignoreCache);
        return analyzers.FirstOrDefault(a =>
            a.SimpleName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
