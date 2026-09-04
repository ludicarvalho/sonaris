using System.IO.Compression;

namespace Sonaris.Services.Download;

using Sonaris.Domain.DTOs.Download;
using Sonaris.Domain.Infrastructure;
using Sonaris.Services.Playlists;

public class PlaylistDownloadService(
      IPlaylistService playlistService
    , IConfiguration configuration
    , ILogger<PlaylistDownloadService> logger)
    : IPlaylistDownloadService
{
    private readonly IPlaylistService playlistService = playlistService ?? throw new ArgumentNullException(nameof(playlistService));
    private readonly string musicPath = configuration["Settings:MusicPath"] ?? throw new InvalidOperationException("Settings:MusicPath não configurado.");
    private readonly ILogger<PlaylistDownloadService> logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<DownloadTracksResponse> DownloadTracksAsync(string userId, string playlistId, IEnumerable<int> trackIds)
    {
        if (trackIds == null || !trackIds.Any())
            throw new SonarisException("Nenhuma faixa selecionada para download.");

        if (trackIds.Count() > 100)
            throw new SonarisException("Máximo de 100 faixas por download.");

        var playlist = playlistService.GetById(userId, playlistId)
            ?? throw new SonarisException("Playlist não encontrada.");

        var selectedTracks = playlist.Tracks
            .Where(t => trackIds.Contains((int)t.Id))
            .ToList();

        if (selectedTracks.Count == 0)
            throw new SonarisException("Nenhuma faixa válida encontrada para download.");

        var tracksWithFiles = new List<TracksWithFilesDto>();

        foreach (var track in selectedTracks)
        {
            var absolutePath = Path.GetFullPath(Path.Combine(musicPath, track.RelativePath));

            if (!PathGuard.IsUnderRoot(absolutePath, musicPath))
            {
                logger.LogWarning("Tentativa de acesso a caminho fora do diretório raiz: {Path}", absolutePath);
                continue;
            }

            if (!File.Exists(absolutePath))
            {
                logger.LogWarning("Arquivo não encontrado: {Path}", absolutePath);
                continue;
            }

            var originalFileName = Path.GetFileName(track.RelativePath);
            var fileName = FileNameSanitizer.GenerateTrackFileName(track.Title, track.Artist, originalFileName);

            tracksWithFiles.Add(new(track, absolutePath, fileName));
        }

        if (tracksWithFiles.Count == 0)
            throw new SonarisException("Nenhum arquivo válido encontrado para download.");

        if (tracksWithFiles.Count == 1)
        {
            var (track, filePath, fileName) = tracksWithFiles[0];
            var fileBytes = await File.ReadAllBytesAsync(filePath);

            return new DownloadTracksResponse
            {
                FileBytes = fileBytes,
                FileName = fileName,
                ContentType = "audio/mpeg"
            };
        }

        return CreateZipDownload(tracksWithFiles, playlist.Name);
    }

    private static DownloadTracksResponse CreateZipDownload(IEnumerable<TracksWithFilesDto> tracks, string playlistName)
    {
        using var memoryStream = new MemoryStream();

        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var fileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (track, filePath, fileName) in tracks)
            {
                var entryName = fileName;

                if (!fileNames.Add(entryName))
                {
                    var counter = 1;
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    var ext = Path.GetExtension(fileName);

                    do
                    {
                        entryName = $"{nameWithoutExt} ({counter}){ext}";
                        counter++;
                    }
                    while (!fileNames.Add(entryName));
                }

                archive.CreateEntryFromFile(filePath, entryName, CompressionLevel.Fastest);
            }
        }

        memoryStream.Position = 0;
        var zipBytes = memoryStream.ToArray();

        var sanitizedPlaylistName = FileNameSanitizer.Sanitize(playlistName);

        return new DownloadTracksResponse
        {
            FileBytes = zipBytes,
            FileName = $"{sanitizedPlaylistName}.zip",
            ContentType = "application/zip"
        };
    }
}
