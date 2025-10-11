using System;
using System.Reflection;

namespace NeoLuke.Utilities;

/// <summary>
/// Extension methods for working with assemblies and types
/// </summary>
public static class AssemblyExtensions
{
    /// <summary>
    /// Gets the informational version string for a type's assembly.
    /// Looks for AssemblyInformationalVersionAttribute first, then falls back to assembly version.
    /// </summary>
    /// <param name="type">The type whose assembly version to retrieve</param>
    /// <returns>The informational version string, or "Unknown" if not found</returns>
    public static string GetInformationalVersion(this Type? type)
    {
        if (type == null)
            return "Unknown";

        var assembly = type.Assembly;

        // Try to get the informational version attribute
        var infoVersionAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (infoVersionAttr != null && !string.IsNullOrWhiteSpace(infoVersionAttr.InformationalVersion))
        {
            return infoVersionAttr.InformationalVersion;
        }

        // Fall back to assembly version
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "Unknown";
    }
}
