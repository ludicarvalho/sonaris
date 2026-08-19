namespace Sonaris.Domain.DTOs.Playlist;

public record PlaylistTrackDto
{
    public long Id { get; init; }
    public string PlaylistId { get; init; } = string.Empty;
    public string RelativePath { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Artist { get; init; } = string.Empty;
    public string Album { get; init; } = string.Empty;
    public int Position { get; init; }
    public string AddedAt { get; init; } = string.Empty;
}
