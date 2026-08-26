namespace Sonaris.Services.Search;

public class MusicIndexerOptions
{
    public string MusicPath { get; set; } = "/Musicas";
    public int RescanIntervalMinutes { get; set; } = 5;
    public int InitialDelaySeconds { get; set; } = 10;
}
