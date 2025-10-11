using System.Reflection;
using Lucene.Net.Index;
using NeoLuke.Utilities;

namespace NeoLuke.ViewModels;

public class AboutDialogViewModel : ViewModelBase
{
    public string AppVersion { get; } = GetAppVersion();
    public string LuceneVersion { get; } = GetLuceneVersion();

    private static string GetAppVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;

        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.0.0";
    }

    private static string GetLuceneVersion()
    {
        return typeof(IndexReader).GetInformationalVersion();
    }
}
