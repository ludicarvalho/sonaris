namespace Sonaris.Domain.DTOs.Music;

/// <summary>
/// Metadados de uma música MP3 (ID3v2.3 + MPEG).
/// </summary>
public record MusicMetadata
{
    public string Title { get; init; } = string.Empty;
    public string Artist { get; init; } = string.Empty;
    public string Album { get; init; } = string.Empty;
    public string Track { get; init; } = string.Empty;
    public string Year { get; init; } = string.Empty;
    public TimeSpan? Duration { get; init; }
    public int? Bitrate { get; init; }
}
