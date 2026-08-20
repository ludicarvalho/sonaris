namespace Sonaris.Domain.DTOs.Music;

public record MusicSearchResult
{
    public long Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Artist { get; init; } = string.Empty;
    public string Album { get; init; } = string.Empty;
    public string Filename { get; init; } = string.Empty;
    public string RelativePath { get; init; } = string.Empty;
    public double Rank { get; init; }
    public string Snippet { get; init; } = string.Empty;
    public string MatchSource { get; init; } = string.Empty;
}
