namespace Sonaris.Services.Download;

public interface IPlaylistDownloadService
{
    Task<DownloadResult> DownloadTracksAsync(string userId, string playlistId, List<int> trackIds);
}

public class DownloadResult
{
    public byte[] FileBytes { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}
