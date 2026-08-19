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

    public List<PlaylistDto> GetAll()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, created_at, updated_at FROM playlist ORDER BY name";

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

    public PlaylistDto GetById(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, created_at, updated_at FROM playlist WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);

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

    public PlaylistDto Create(string name)
    {
        var id = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow.ToString("o");

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO playlist (id, name, created_at, updated_at)
            VALUES (@id, @name, @createdAt, @updatedAt)
            """;
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", name);
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

    public PlaylistDto Rename(string id, string name)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE playlist SET name = @name, updated_at = @updatedAt
            WHERE id = @id
            """;
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();

        return GetById(id)!;
    }

    public void Delete(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM playlist WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public PlaylistTrackDto AddTrack(string playlistId, string relativePath)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var maxPosCmd = connection.CreateCommand();
        maxPosCmd.CommandText = "SELECT COALESCE(MAX(position), -1) + 1 FROM playlist_track WHERE playlist_id = @playlistId";
        maxPosCmd.Parameters.AddWithValue("@playlistId", playlistId);
        var position = Convert.ToInt32(maxPosCmd.ExecuteScalar());

        var now = DateTime.UtcNow.ToString("o");

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO playlist_track (playlist_id, relative_path, title, artist, album, position, added_at)
            VALUES (@playlistId, @relativePath, '', '', '', @position, @addedAt)
            """;
        cmd.Parameters.AddWithValue("@playlistId", playlistId);
        cmd.Parameters.AddWithValue("@relativePath", relativePath);
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
            Position = position,
            AddedAt = now
        };
    }

    public void RemoveTrack(string playlistId, long trackId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

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

    public void ReorderTrack(string playlistId, long trackId, int newPosition)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE playlist_track SET position = @newPosition
            WHERE id = @trackId AND playlist_id = @playlistId
            """;
        cmd.Parameters.AddWithValue("@trackId", trackId);
        cmd.Parameters.AddWithValue("@playlistId", playlistId);
        cmd.Parameters.AddWithValue("@newPosition", newPosition);
        cmd.ExecuteNonQuery();

        var updateCmd = connection.CreateCommand();
        updateCmd.CommandText = "UPDATE playlist SET updated_at = @updatedAt WHERE id = @id";
        updateCmd.Parameters.AddWithValue("@id", playlistId);
        updateCmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
        updateCmd.ExecuteNonQuery();
    }

    public void Duplicate(string id, string newName)
    {
        var original = GetById(id);
        if (original == null) throw new SonarisException("Playlist não encontrada.");

        var newPlaylist = Create(newName);

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
                tracks.Add(new PlaylistTrackDto
                {
                    Id = reader.GetInt64(0),
                    PlaylistId = reader.GetString(1),
                    RelativePath = reader.GetString(2),
                    Title = reader.GetString(3),
                    Artist = reader.GetString(4),
                    Album = reader.GetString(5),
                    Position = reader.GetInt32(6),
                    AddedAt = reader.GetString(7)
                });
            }
        }

        return tracks;
    }
}
