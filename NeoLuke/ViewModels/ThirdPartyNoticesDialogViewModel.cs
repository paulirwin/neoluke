using System;
using System.IO;
using System.Reflection;

namespace NeoLuke.ViewModels;

public class ThirdPartyNoticesDialogViewModel : ViewModelBase
{
    public string NoticesText { get; } = LoadThirdPartyNotices();

    private static string LoadThirdPartyNotices()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "NeoLuke.THIRD-PARTY-NOTICES.TXT";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return "Third-party notices file not found.\n\n" +
                       "The THIRD-PARTY-NOTICES.TXT file should be embedded as a resource in the assembly.";
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            return $"Error loading third-party notices:\n\n{ex.Message}";
        }
    }
}
