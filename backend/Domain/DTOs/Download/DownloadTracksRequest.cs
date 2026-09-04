namespace Sonaris.Domain.DTOs.Download;

public class DownloadTracksRequest
{
    public IEnumerable<int> TrackIds { get; set; } = [];
}
