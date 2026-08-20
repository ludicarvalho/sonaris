namespace Sonaris.Domain.DTOs.Playlist;

public record PlaylistDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
    public List<PlaylistTrackDto> Tracks { get; init; } = [];
}
