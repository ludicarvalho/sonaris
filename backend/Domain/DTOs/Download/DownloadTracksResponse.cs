namespace Sonaris.Domain.DTOs.Download;

public class DownloadTracksResponse
{
    public byte[] FileBytes { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
}
