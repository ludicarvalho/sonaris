using System.Text.RegularExpressions;

namespace Sonaris.Services.Download;

public static partial class FileNameSanitizer
{
    private static readonly Regex InvalidCharsRegex = InvalidCharsPattern();
    private static readonly Regex MultipleSpacesRegex = MultipleSpacesPattern();

    public static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var sanitized = InvalidCharsRegex.Replace(name, " ");
        sanitized = MultipleSpacesRegex.Replace(sanitized, " ");
        sanitized = sanitized.Trim();

        if (sanitized.Length > 200)
            sanitized = sanitized[..200];

        return sanitized;
    }

    public static string GenerateTrackFileName(string title, string artist, string originalFileName)
    {
        if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(artist))
        {
            var sanitizedArtist = Sanitize(artist);
            var sanitizedTitle = Sanitize(title);
            return $"{sanitizedArtist} - {sanitizedTitle}.mp3";
        }

        return originalFileName;
    }

    [GeneratedRegex(@"[\\/:*?""<>|]")]
    private static partial Regex InvalidCharsPattern();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultipleSpacesPattern();
}
