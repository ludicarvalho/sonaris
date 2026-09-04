using Sonaris.Domain.DTOs.Playlist;

namespace Sonaris.Domain.DTOs.Download;

public record TracksWithFilesDto(PlaylistTrackDto Track, string FilePath, string FileName);
