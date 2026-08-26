using System.Text.Json;
using Microsoft.Data.Sqlite;
using Sonaris.Domain.Entities;

namespace Sonaris.Services.Search;

public class MusicRepository
{
    private readonly string _connectionString;

    private const string UpsertSql = """
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

    private const string DeleteOrphansSql =
        "DELETE FROM music WHERE relative_path NOT IN (SELECT value FROM json_each(@paths))";

    private const string RebuildPathFtsSql = """
        DELETE FROM music_path_fts;
        INSERT INTO music_path_fts(rowid, filename, relative_path)
        SELECT id, filename, relative_path FROM music;
        """;

    public MusicRepository(IMusicSearchService searchService)
    {
        _connectionString = searchService.ConnectionString;
        DatabaseSchema.EnsureCreated(_connectionString);
    }

    public async Task<int> UpsertAndCleanAsync(IReadOnlyList<MusicFileEntry> entries, CancellationToken ct = default)
    {
        var activePaths = new HashSet<string>(
            entries.Select(e => e.RelativePath), StringComparer.OrdinalIgnoreCase);

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var transaction = connection.BeginTransaction();

        var upsertCount = await UpsertEntriesAsync(connection, transaction, entries, ct);
        await DeleteOrphansAsync(connection, transaction, activePaths, ct);

        transaction.Commit();
        return upsertCount;
    }

    public async Task RebuildPathFtsAsync(CancellationToken ct = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);

        var cmd = connection.CreateCommand();
        cmd.CommandText = RebuildPathFtsSql;
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<int> UpsertEntriesAsync(
        SqliteConnection connection, SqliteTransaction transaction,
        IReadOnlyList<MusicFileEntry> entries, CancellationToken ct)
    {
        var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = UpsertSql;

        var count = 0;
        foreach (var entry in entries)
        {
            if (ct.IsCancellationRequested) break;

            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@title", entry.Title);
            cmd.Parameters.AddWithValue("@artist", entry.Artist);
            cmd.Parameters.AddWithValue("@album", entry.Album);
            cmd.Parameters.AddWithValue("@track", entry.Track);
            cmd.Parameters.AddWithValue("@year", entry.Year);
            cmd.Parameters.AddWithValue("@duration", entry.DurationSeconds.HasValue ? (object)entry.DurationSeconds.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@bitrate", entry.Bitrate.HasValue ? (object)entry.Bitrate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@filename", entry.Filename);
            cmd.Parameters.AddWithValue("@relativePath", entry.RelativePath);
            cmd.Parameters.AddWithValue("@fileSize", entry.FileSize);
            cmd.Parameters.AddWithValue("@lastModified", entry.LastModified.ToString("o"));
            cmd.Parameters.AddWithValue("@lastScanned", entry.LastScanned.ToString("o"));

            await cmd.ExecuteNonQueryAsync(ct);
            count++;
        }

        return count;
    }

    private static async Task DeleteOrphansAsync(
        SqliteConnection connection, SqliteTransaction transaction,
        HashSet<string> activePaths, CancellationToken ct)
    {
        var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = DeleteOrphansSql;
        cmd.Parameters.AddWithValue("@paths", JsonSerializer.Serialize(activePaths));
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
