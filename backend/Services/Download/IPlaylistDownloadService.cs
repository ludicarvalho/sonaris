namespace Sonaris.Services.Download;

using Sonaris.Domain.DTOs.Download;

public interface IPlaylistDownloadService
{
    Task<DownloadTracksResponse> DownloadTracksAsync(string userId, string playlistId, IEnumerable<int> trackIds);
}
