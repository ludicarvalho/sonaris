namespace Sonaris.Services.Implementations.Search;

using Sonaris.Domain.Entities;
using Sonaris.Services.Abstractions.Music;

public class MusicFileScanner(IMusicMetadataReader metadataReader, ILogger<MusicFileScanner> logger)
{
    private readonly IMusicMetadataReader _metadataReader = metadataReader;
    private readonly ILogger<MusicFileScanner> _logger = logger;

    public List<MusicFileEntry> Scan(string musicPath)
    {
        var files = Directory.EnumerateFiles(musicPath, "*.mp3", new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true
        }).ToList();

        _logger.LogInformation("Encontrados {Count} arquivos MP3 em {MusicPath}", files.Count, musicPath);

        var now = DateTime.UtcNow;
        var entries = new List<MusicFileEntry>(files.Count);
        var errorCount = 0;

        foreach (var filePath in files)
        {
            try
            {
                var metadata = _metadataReader.RetornarMusicaMetadata(filePath);
                var fileInfo = new FileInfo(filePath);

                entries.Add(new MusicFileEntry
                {
                    Title = metadata.Title ?? string.Empty,
                    Artist = metadata.Artist ?? string.Empty,
                    Album = metadata.Album ?? string.Empty,
                    Track = metadata.Track ?? string.Empty,
                    Year = metadata.Year ?? string.Empty,
                    DurationSeconds = metadata.Duration?.TotalSeconds,
                    Bitrate = metadata.Bitrate,
                    Filename = fileInfo.Name,
                    RelativePath = Path.GetRelativePath(musicPath, filePath),
                    FileSize = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTimeUtc,
                    LastScanned = now
                });
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Erro ao ler metadata de {FilePath}", filePath);
                errorCount++;
            }
        }

        _logger.LogInformation("Scan concluído: {Success} lidas, {Errors} erros", entries.Count, errorCount);
        return entries;
    }
}
