using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Sonaris.Domain.DTOs.Playlist;
using Sonaris.Domain.Infrastructure;
using Sonaris.Services.Download;
using Sonaris.Services.Playlists;
using Xunit;

namespace Sonaris.Backend.Tests.Services;

public class PlaylistDownloadServiceTests : IDisposable
{
    private const string UserId = "user-teste";

    private readonly Mock<IPlaylistService> _playlistService;
    private readonly PlaylistDownloadService _service;
    private readonly string _musicPath;

    public PlaylistDownloadServiceTests()
    {
        _playlistService = new Mock<IPlaylistService>();

        _musicPath = Path.Combine(Path.GetTempPath(), "sonaris-download-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_musicPath);

        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Settings:MusicPath"]).Returns(_musicPath);

        var logger = new Mock<ILogger<PlaylistDownloadService>>();

        _service = new PlaylistDownloadService(_playlistService.Object, config.Object, logger.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_musicPath))
            Directory.Delete(_musicPath, recursive: true);
    }

    [Fact]
    public void Construtor_PlaylistServiceNulo_LancaArgumentNullException()
    {
        var config = new Mock<IConfiguration>();
        var logger = new Mock<ILogger<PlaylistDownloadService>>();

        Assert.Throws<ArgumentNullException>(() =>
            new PlaylistDownloadService(null, config.Object, logger.Object));
    }

    [Fact]
    public void Construtor_LoggerNulo_LancaArgumentNullException()
    {
        var config = new Mock<IConfiguration>();

        Assert.Throws<InvalidOperationException>(() =>
            new PlaylistDownloadService(_playlistService.Object, config.Object, null));
    }

    [Fact]
    public async Task DownloadTracksAsync_TrackIdsVazio_LancaSonarisException()
    {
        await Assert.ThrowsAsync<SonarisException>(() =>
            _service.DownloadTracksAsync(UserId, "playlist-1", []));
    }

    [Fact]
    public async Task DownloadTracksAsync_MaisDe100Faixas_LancaSonarisException()
    {
        var ids = Enumerable.Range(1, 101).ToList();

        await Assert.ThrowsAsync<SonarisException>(() =>
            _service.DownloadTracksAsync(UserId, "playlist-1", ids));
    }

    [Fact]
    public async Task DownloadTracksAsync_PlaylistInexistente_LancaSonarisException()
    {
        _playlistService.Setup(s => s.GetById(UserId, "nao-existe"))
            .Returns((PlaylistDto)null);

        await Assert.ThrowsAsync<SonarisException>(() =>
            _service.DownloadTracksAsync(UserId, "nao-existe", [1]));
    }

    [Fact]
    public async Task DownloadTracksAsync_ArquivoInexistente_LancaSonarisException()
    {
        _playlistService.Setup(s => s.GetById(UserId, "playlist-1"))
            .Returns(new PlaylistDto
            {
                Id = "playlist-1",
                Name = "Teste",
                Tracks =
                [
                    new PlaylistTrackDto { Id = 1, RelativePath = "nao-existe.mp3", Title = "Song", Artist = "Artist" }
                ]
            });

        await Assert.ThrowsAsync<SonarisException>(() =>
            _service.DownloadTracksAsync(UserId, "playlist-1", [1]));
    }

    [Fact]
    public async Task DownloadTracksAsync_UmArquivo_RetornaMP3()
    {
        var filePath = Path.Combine(_musicPath, "track.mp3");
        await File.WriteAllBytesAsync(filePath, [0xFF, 0xFB, 0x90, 0x00]);

        var relativePath = Path.GetRelativePath(_musicPath, filePath);

        _playlistService.Setup(s => s.GetById(UserId, "playlist-1"))
            .Returns(new PlaylistDto
            {
                Id = "playlist-1",
                Name = "Minha Playlist",
                Tracks =
                [
                    new PlaylistTrackDto { Id = 1, RelativePath = relativePath, Title = "Imagine", Artist = "John Lennon" }
                ]
            });

        var result = await _service.DownloadTracksAsync(UserId, "playlist-1", [1]);

        Assert.Equal("audio/mpeg", result.ContentType);
        Assert.Equal("John Lennon - Imagine.mp3", result.FileName);
        Assert.Equal(4, result.FileBytes.Length);
    }

    [Fact]
    public async Task DownloadTracksAsync_SemMetatags_RetornaNomeOriginal()
    {
        var filePath = Path.Combine(_musicPath, "minha_musica.mp3");
        await File.WriteAllBytesAsync(filePath, [0xFF, 0xFB, 0x90, 0x00]);

        var relativePath = Path.GetRelativePath(_musicPath, filePath);

        _playlistService.Setup(s => s.GetById(UserId, "playlist-1"))
            .Returns(new PlaylistDto
            {
                Id = "playlist-1",
                Name = "Minha Playlist",
                Tracks =
                [
                    new PlaylistTrackDto { Id = 1, RelativePath = relativePath, Title = "", Artist = "" }
                ]
            });

        var result = await _service.DownloadTracksAsync(UserId, "playlist-1", [1]);

        Assert.Equal("audio/mpeg", result.ContentType);
        Assert.Equal("minha_musica.mp3", result.FileName);
    }

    [Fact]
    public async Task DownloadTracksAsync_VariosArquivos_RetornaZIP()
    {
        for (int i = 1; i <= 3; i++)
        {
            var filePath = Path.Combine(_musicPath, $"track{i}.mp3");
            await File.WriteAllBytesAsync(filePath, [0xFF, 0xFB]);
        }

        var tracks = new List<PlaylistTrackDto>();
        for (int i = 1; i <= 3; i++)
        {
            var relPath = Path.GetRelativePath(_musicPath, Path.Combine(_musicPath, $"track{i}.mp3"));
            tracks.Add(new PlaylistTrackDto
            {
                Id = i,
                RelativePath = relPath,
                Title = $"Song {i}",
                Artist = $"Artist {i}"
            });
        }

        _playlistService.Setup(s => s.GetById(UserId, "playlist-1"))
            .Returns(new PlaylistDto
            {
                Id = "playlist-1",
                Name = "Playlist Teste",
                Tracks = tracks
            });

        var result = await _service.DownloadTracksAsync(UserId, "playlist-1", [1, 2, 3]);

        Assert.Equal("application/zip", result.ContentType);
        Assert.Equal("Playlist Teste.zip", result.FileName);
        Assert.True(result.FileBytes.Length > 0);
    }
}