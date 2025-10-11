using NeoLuke.Utilities;
using Avalonia;
using Lucene.Net.Index;

namespace NeoLuke.Tests;

public class AssemblyExtensionsTests
{
    [Fact]
    public void GetInformationalVersion_WithValidType_ReturnsVersion()
    {
        // Test with Avalonia Application type
        var avaloniaVersion = typeof(Application).GetInformationalVersion();

        Assert.NotNull(avaloniaVersion);
        Assert.NotEqual("Unknown", avaloniaVersion);
        Assert.False(string.IsNullOrWhiteSpace(avaloniaVersion));
    }

    [Fact]
    public void GetInformationalVersion_WithLuceneType_ReturnsVersion()
    {
        // Test with Lucene IndexReader type
        var luceneVersion = typeof(IndexReader).GetInformationalVersion();

        Assert.NotNull(luceneVersion);
        Assert.NotEqual("Unknown", luceneVersion);
        Assert.False(string.IsNullOrWhiteSpace(luceneVersion));
    }

    [Fact]
    public void GetInformationalVersion_WithNullType_ReturnsUnknown()
    {
        Type? nullType = null;
        var version = nullType!.GetInformationalVersion();

        Assert.Equal("Unknown", version);
    }

    [Fact]
    public void GetInformationalVersion_ReturnsConsistentValue()
    {
        // Calling multiple times should return the same value
        var version1 = typeof(Application).GetInformationalVersion();
        var version2 = typeof(Application).GetInformationalVersion();

        Assert.Equal(version1, version2);
    }

    [Fact]
    public void GetInformationalVersion_ForThisAssembly_ReturnsVersion()
    {
        // Test with a type from this assembly
        var version = typeof(AssemblyExtensionsTests).GetInformationalVersion();

        Assert.NotNull(version);
        Assert.NotEqual("Unknown", version);
        // Our test assembly should have version information
        Assert.False(string.IsNullOrWhiteSpace(version));
    }
}
