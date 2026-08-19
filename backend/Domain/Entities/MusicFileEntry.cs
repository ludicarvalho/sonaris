namespace Sonaris.Domain.Entities;

public class MusicFileEntry
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public string Track { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public double? DurationSeconds { get; set; }
    public int? Bitrate { get; set; }
    public string Filename { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime LastModified { get; set; }
    public DateTime LastScanned { get; set; }
}
