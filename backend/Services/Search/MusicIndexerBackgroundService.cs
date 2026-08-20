using Microsoft.Data.Sqlite;

namespace Sonaris.Services.Search;

using Sonaris.Services.Music;

public class MusicIndexerBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MusicIndexerBackgroundService> _logger;
    private readonly TimeSpan _rescanInterval = TimeSpan.FromMinutes(5);

    public MusicIndexerBackgroundService(IServiceProvider serviceProvider, ILogger<MusicIndexerBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MusicIndexer iniciado. Aguardando 10 segundos para o primeiro scan...");
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScanAndIndexAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante o scan de músicas.");
            }

            await Task.Delay(_rescanInterval, stoppingToken);
        }
    }

    public async Task ScanAndIndexAsync(CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var metadataReader = scope.ServiceProvider.GetRequiredService<IMusicMetadataReader>();

        var musicPath = configuration["Settings:MusicPath"] ?? "/Musicas";

        if (!Directory.Exists(musicPath))
        {
            _logger.LogWarning("Diretório de músicas não encontrado: {MusicPath}", musicPath);
            return;
        }

        var searchService = scope.ServiceProvider.GetRequiredService<IMusicSearchService>() as MusicSearchService;
        if (searchService == null) return;

        var connectionString = searchService.ConnectionString;
        DatabaseSchema.EnsureCreated(connectionString);

        var files = Directory.EnumerateFiles(musicPath, "*.mp3", new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true
        }).ToList();

        _logger.LogInformation("Encontrados {Count} arquivos MP3 em {MusicPath}", files.Count, musicPath);

        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(ct);

        using var transaction = connection.BeginTransaction();

        var indexedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var upsertCmd = connection.CreateCommand();
        upsertCmd.Transaction = transaction;
        upsertCmd.CommandText = """
            INSERT INTO music (title, artist, album, track, year, duration_secs, bitrate,
                               filename, relative_path, file_size, last_modified, last_scanned)
            VALUES (@title, @artist, @album, @track, @year, @duration, @bitrate,
                    @filename, @relativePath, @fileSize, @lastModified, @lastScanned)
            ON CONFLICT(relative_path) DO UPDATE SET
                title = excluded.title,
                artist = excluded.artist,
                album = excluded.album,
                track = excluded.track,
                year = excluded.year,
                duration_secs = excluded.duration_secs,
                bitrate = excluded.bitrate,
                filename = excluded.filename,
                file_size = excluded.file_size,
                last_modified = excluded.last_modified,
                last_scanned = excluded.last_scanned
            """;

        var now = DateTime.UtcNow.ToString("o");
        var successCount = 0;
        var errorCount = 0;

        foreach (var filePath in files)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var relativePath = Path.GetRelativePath(musicPath, filePath);
                indexedPaths.Add(relativePath);

                var metadata = metadataReader.RetornarMusicaMetadata(filePath);
                var fileInfo = new FileInfo(filePath);

                upsertCmd.Parameters.Clear();
                upsertCmd.Parameters.AddWithValue("@title", metadata.Title ?? string.Empty);
                upsertCmd.Parameters.AddWithValue("@artist", metadata.Artist ?? string.Empty);
                upsertCmd.Parameters.AddWithValue("@album", metadata.Album ?? string.Empty);
                upsertCmd.Parameters.AddWithValue("@track", metadata.Track ?? string.Empty);
                upsertCmd.Parameters.AddWithValue("@year", metadata.Year ?? string.Empty);
                upsertCmd.Parameters.AddWithValue("@duration", metadata.Duration.HasValue ? (object)metadata.Duration.Value.TotalSeconds : DBNull.Value);
                upsertCmd.Parameters.AddWithValue("@bitrate", metadata.Bitrate.HasValue ? (object)metadata.Bitrate.Value : DBNull.Value);
                upsertCmd.Parameters.AddWithValue("@filename", fileInfo.Name);
                upsertCmd.Parameters.AddWithValue("@relativePath", relativePath);
                upsertCmd.Parameters.AddWithValue("@fileSize", fileInfo.Length);
                upsertCmd.Parameters.AddWithValue("@lastModified", fileInfo.LastWriteTimeUtc.ToString("o"));
                upsertCmd.Parameters.AddWithValue("@lastScanned", now);

                await upsertCmd.ExecuteNonQueryAsync(ct);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Erro ao indexar {FilePath}", filePath);
                errorCount++;
            }
        }

        var deleteCmd = connection.CreateCommand();
        deleteCmd.Transaction = transaction;
        deleteCmd.CommandText = "DELETE FROM music WHERE relative_path NOT IN (SELECT value FROM json_each(@paths))";
        deleteCmd.Parameters.AddWithValue("@paths", System.Text.Json.JsonSerializer.Serialize(indexedPaths));
        await deleteCmd.ExecuteNonQueryAsync(ct);

        transaction.Commit();

        await RebuildPathFtsAsync(connection, ct);

        _logger.LogInformation("Scan concluído: {Success} indexadas, {Errors} erros, {Removed} removidas",
            successCount, errorCount, Math.Max(0, files.Count - successCount));
    }

    private static async Task RebuildPathFtsAsync(SqliteConnection connection, CancellationToken ct)
    {
        var rebuildCmd = connection.CreateCommand();
        rebuildCmd.CommandText = """
            DELETE FROM music_path_fts;
            INSERT INTO music_path_fts(rowid, filename, relative_path)
            SELECT id, filename, relative_path FROM music;
            """;
        await rebuildCmd.ExecuteNonQueryAsync(ct);
    }
}
