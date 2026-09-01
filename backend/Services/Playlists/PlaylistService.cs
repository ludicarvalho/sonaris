using Microsoft.Data.Sqlite;

namespace Sonaris.Services.Playlists;

using Sonaris.Domain.DTOs.Playlist;
using Sonaris.Domain.Infrastructure;
using Sonaris.Services.Search;

public class PlaylistService : IPlaylistService
{
    private readonly string _connectionString;

    public PlaylistService(IConfiguration configuration)
    {
        var dbPath = configuration["Settings:DatabasePath"]
            ?? Path.Combine(AppContext.BaseDirectory, "sonaris.db");
        _connectionString = $"Data Source={dbPath}";
        DatabaseSchema.EnsureCreated(_connectionString);
    }

    public List<PlaylistDto> GetAll(string userId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, created_at, updated_at FROM playlist
            WHERE user_id = @userId
            ORDER BY name
            """;
        cmd.Parameters.AddWithValue("@userId", userId);

        var playlists = new List<PlaylistDto>();
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                playlists.Add(new PlaylistDto
                {
                    Id = reader.GetString(0),
                    Name = reader.GetString(1),
                    CreatedAt = reader.GetString(2),
                    UpdatedAt = reader.GetString(3)
                });
            }
        }

        foreach (var playlist in playlists)
        {
            playlist.Tracks.AddRange(GetTracks(connection, playlist.Id));
        }

        return playlists;
    }

    public PlaylistDto GetById(string userId, string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, created_at, updated_at FROM playlist
            WHERE id = @id AND user_id = @userId
            """;
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@userId", userId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        var playlist = new PlaylistDto
        {
            Id = reader.GetString(0),
            Name = reader.GetString(1),
            CreatedAt = reader.GetString(2),
            UpdatedAt = reader.GetString(3)
        };

        playlist.Tracks.AddRange(GetTracks(connection, id));
        return playlist;
    }

    public PlaylistDto Create(string userId, string name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed))
            throw new SonarisException("Nome da playlist não pode ser vazio.");

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM playlist WHERE name = @name AND user_id = @userId";
        checkCmd.Parameters.AddWithValue("@name", trimmed);
        checkCmd.Parameters.AddWithValue("@userId", userId);
        if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
            throw new SonarisException("Já existe uma playlist com esse nome.");

        var id = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow.ToString("o");

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO playlist (id, name, user_id, created_at, updated_at)
            VALUES (@id, @name, @userId, @createdAt, @updatedAt)
            """;
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", trimmed);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@createdAt", now);
        cmd.Parameters.AddWithValue("@updatedAt", now);
        cmd.ExecuteNonQuery();

        return new PlaylistDto
        {
            Id = id,
            Name = name,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public PlaylistDto Rename(string userId, string id, string name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed))
            throw new SonarisException("Nome da playlist não pode ser vazio.");

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM playlist WHERE name = @name AND id != @id AND user_id = @userId";
        checkCmd.Parameters.AddWithValue("@name", trimmed);
        checkCmd.Parameters.AddWithValue("@id", id);
        checkCmd.Parameters.AddWithValue("@userId", userId);
        if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
            throw new SonarisException("Já existe uma playlist com esse nome.");

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE playlist SET name = @name, updated_at = @updatedAt
            WHERE id = @id AND user_id = @userId
            """;
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", trimmed);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();

        return GetById(userId, id)!;
    }

    public void Delete(string userId, string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM playlist WHERE id = @id AND user_id = @userId";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.ExecuteNonQuery();
    }

    public PlaylistTrackDto AddTrack(string userId, string playlistId, string relativePath)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        if (GetById(userId, playlistId) == null)
            throw new SonarisException("Playlist não encontrada.");

        var maxPosCmd = connection.CreateCommand();
        maxPosCmd.CommandText = "SELECT COALESCE(MAX(position), -1) + 1 FROM playlist_track WHERE playlist_id = @playlistId";
        maxPosCmd.Parameters.AddWithValue("@playlistId", playlistId);
        var position = Convert.ToInt32(maxPosCmd.ExecuteScalar());

        var now = DateTime.UtcNow.ToString("o");

        var infos = BuscarInfosNoIndice(connection, relativePath);

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO playlist_track (playlist_id, relative_path, title, artist, album, position, added_at)
            VALUES (@playlistId, @relativePath, @title, @artist, @album, @position, @addedAt)
            """;
        cmd.Parameters.AddWithValue("@playlistId", playlistId);
        cmd.Parameters.AddWithValue("@relativePath", relativePath);
        cmd.Parameters.AddWithValue("@title", infos.Title);
        cmd.Parameters.AddWithValue("@artist", infos.Artist);
        cmd.Parameters.AddWithValue("@album", infos.Album);
        cmd.Parameters.AddWithValue("@position", position);
        cmd.Parameters.AddWithValue("@addedAt", now);
                cmd.ExecuteNonQuery();

        var newIdCmd = connection.CreateCommand();
        newIdCmd.CommandText = "SELECT last_insert_rowid()";
        var newTrackId = Convert.ToInt64(newIdCmd.ExecuteScalar());

        var updateCmd = connection.CreateCommand();
        updateCmd.CommandText = "UPDATE playlist SET updated_at = @updatedAt WHERE id = @id";
        updateCmd.Parameters.AddWithValue("@id", playlistId);
        updateCmd.Parameters.AddWithValue("@updatedAt", now);
        updateCmd.ExecuteNonQuery();

        return new PlaylistTrackDto
        {
            Id = newTrackId,
            PlaylistId = playlistId,
            RelativePath = relativePath,
            Title = infos.Title,
            Artist = infos.Artist,
            Album = infos.Album,
            Position = position,
            AddedAt = now
        };
    }

    public void RemoveTrack(string userId, string playlistId, long trackId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        if (GetById(userId, playlistId) == null)
            throw new SonarisException("Playlist não encontrada.");

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM playlist_track WHERE id = @trackId AND playlist_id = @playlistId";
        cmd.Parameters.AddWithValue("@trackId", trackId);
        cmd.Parameters.AddWithValue("@playlistId", playlistId);
        cmd.ExecuteNonQuery();

        var updateCmd = connection.CreateCommand();
        updateCmd.CommandText = "UPDATE playlist SET updated_at = @updatedAt WHERE id = @id";
        updateCmd.Parameters.AddWithValue("@id", playlistId);
        updateCmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
        updateCmd.ExecuteNonQuery();
    }

    public void ReorderTrack(string userId, string playlistId, long trackId, int newPosition)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        if (GetById(userId, playlistId) == null)
            throw new SonarisException("Playlist não encontrada.");

        using var transaction = connection.BeginTransaction();

        var getPos = connection.CreateCommand();
        getPos.Transaction = transaction;
        getPos.CommandText = """
            SELECT position FROM playlist_track
            WHERE id = @trackId AND playlist_id = @playlistId
            """;
        getPos.Parameters.AddWithValue("@trackId", trackId);
        getPos.Parameters.AddWithValue("@playlistId", playlistId);
        var oldPosition = Convert.ToInt32(getPos.ExecuteScalar());

        if (oldPosition == newPosition)
        {
            transaction.Rollback();
            return;
        }

        var shake = connection.CreateCommand();
        shake.Transaction = transaction;
        shake.CommandText = """
             UPDATE playlist_track SET position = position + @offset
             WHERE playlist_id = @playlistId AND id != @trackId
               AND position BETWEEN @minPos AND @maxPos
             """;
        shake.Parameters.AddWithValue("@playlistId", playlistId);
        shake.Parameters.AddWithValue("@trackId", trackId);

        if (oldPosition < newPosition)
        {
            shake.Parameters.AddWithValue("@offset", -1);
            shake.Parameters.AddWithValue("@minPos", oldPosition + 1);
            shake.Parameters.AddWithValue("@maxPos", newPosition);
        }
        else
        {
            shake.Parameters.AddWithValue("@offset", 1);
            shake.Parameters.AddWithValue("@minPos", newPosition);
            shake.Parameters.AddWithValue("@maxPos", oldPosition - 1);
        }
        shake.ExecuteNonQuery();

        var updateTrack = connection.CreateCommand();
        updateTrack.Transaction = transaction;
        updateTrack.CommandText = """
            UPDATE playlist_track SET position = @newPosition
            WHERE id = @trackId AND playlist_id = @playlistId
            """;
        updateTrack.Parameters.AddWithValue("@trackId", trackId);
        updateTrack.Parameters.AddWithValue("@playlistId", playlistId);
        updateTrack.Parameters.AddWithValue("@newPosition", newPosition);
        updateTrack.ExecuteNonQuery();

        var updateCmd = connection.CreateCommand();
        updateCmd.Transaction = transaction;
        updateCmd.CommandText = "UPDATE playlist SET updated_at = @updatedAt WHERE id = @id";
        updateCmd.Parameters.AddWithValue("@id", playlistId);
        updateCmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
        updateCmd.ExecuteNonQuery();

        transaction.Commit();
    }

    public void Duplicate(string userId, string id, string newName)
    {
        var original = GetById(userId, id);
        if (original == null) throw new SonarisException("Playlist não encontrada.");

        var newPlaylist = Create(userId, newName);

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        foreach (var track in original.Tracks)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO playlist_track (playlist_id, relative_path, title, artist, album, position, added_at)
                VALUES (@playlistId, @relativePath, @title, @artist, @album, @position, @addedAt)
                """;
            cmd.Parameters.AddWithValue("@playlistId", newPlaylist.Id);
            cmd.Parameters.AddWithValue("@relativePath", track.RelativePath);
            cmd.Parameters.AddWithValue("@title", track.Title);
            cmd.Parameters.AddWithValue("@artist", track.Artist);
            cmd.Parameters.AddWithValue("@album", track.Album);
            cmd.Parameters.AddWithValue("@position", track.Position);
            cmd.Parameters.AddWithValue("@addedAt", track.AddedAt);
            cmd.ExecuteNonQuery();
        }
    }

    private static List<PlaylistTrackDto> GetTracks(SqliteConnection connection, string playlistId)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, playlist_id, relative_path, title, artist, album, position, added_at
            FROM playlist_track
            WHERE playlist_id = @playlistId
            ORDER BY position
            """;
        cmd.Parameters.AddWithValue("@playlistId", playlistId);

        var tracks = new List<PlaylistTrackDto>();
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var title = reader.GetString(3);
                var artist = reader.GetString(4);
                var album = reader.GetString(5);

                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(album))
                {
                    var infos = BuscarInfosNoIndice(connection, reader.GetString(2));
                    if (string.IsNullOrWhiteSpace(title)) title = infos.Title;
                    if (string.IsNullOrWhiteSpace(artist)) artist = infos.Artist;
                    if (string.IsNullOrWhiteSpace(album)) album = infos.Album;
                }

                tracks.Add(new PlaylistTrackDto
                {
                    Id = reader.GetInt64(0),
                    PlaylistId = reader.GetString(1),
                    RelativePath = reader.GetString(2),
                    Title = title,
                    Artist = artist,
                    Album = album,
                    Position = reader.GetInt32(6),
                    AddedAt = reader.GetString(7)
                });
            }
        }

        return tracks;
    }

    private static InfosIndiceDto BuscarInfosNoIndice(SqliteConnection connection, string relativePath)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT title, artist, album
            FROM music
            WHERE relative_path = @relativePath COLLATE NOCASE
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("@relativePath", relativePath);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return new InfosIndiceDto();

        return new InfosIndiceDto
        {
            Title = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
            Artist = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            Album = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
        };
    }

    private sealed class InfosIndiceDto
    {
        public string Title { get; init; } = string.Empty;
        public string Artist { get; init; } = string.Empty;
        public string Album { get; init; } = string.Empty;
    }
}
