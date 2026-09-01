using Microsoft.Data.Sqlite;
using Sonaris.Services.Search;
using Xunit;

namespace Sonaris.Backend.Tests.Services;

public class DatabaseSchemaTests : TestesBase
{
    public DatabaseSchemaTests() : base("schema")
    {
    }

    private string ConnectionString => $"Data Source={DbPath}";

    [Fact]
    public void EnsureCreated_CriaTabelas_E_Playlist()
    {
        DatabaseSchema.EnsureCreated(ConnectionString);

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
        var tables = new List<string>();
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
                tables.Add(reader.GetString(0));
        }

        Assert.Contains("music", tables);
        Assert.Contains("playlist", tables);
        Assert.Contains("playlist_track", tables);
        Assert.Contains("usuario", tables);
    }

    [Fact]
    public void EnsureCreated_CriaTabelaUsuario_ComColunas()
    {
        DatabaseSchema.EnsureCreated(ConnectionString);

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM pragma_table_info('usuario')";
        var colunas = new List<string>();
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
                colunas.Add(reader.GetString(0));
        }

        Assert.Contains("id", colunas);
        Assert.Contains("username", colunas);
        Assert.Contains("senha_hash", colunas);
        Assert.Contains("senha_salt", colunas);
        Assert.Contains("is_admin", colunas);
    }

    [Fact]
    public void EnsureCreated_PlaylistTemColunaUserId()
    {
        DatabaseSchema.EnsureCreated(ConnectionString);

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('playlist') WHERE name = 'user_id'";
        Assert.Equal(1, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    [Fact]
    public void EnsureCreated_MigraBancoExistenteSemUserId_AdicionaColuna()
    {
        // Simula um banco criado antes da coluna user_id
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "DROP TABLE IF EXISTS playlist; CREATE TABLE playlist (id TEXT PRIMARY KEY, name TEXT NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL)";
            cmd.ExecuteNonQuery();
        }

        DatabaseSchema.EnsureCreated(ConnectionString);

        using var conn2 = new SqliteConnection(ConnectionString);
        conn2.Open();
        var check = conn2.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM pragma_table_info('playlist') WHERE name = 'user_id'";
        Assert.Equal(1, Convert.ToInt32(check.ExecuteScalar()));
    }

    [Fact]
    public void EnsureCreated_CriaIndiceFTS5_Padro()
    {
        DatabaseSchema.EnsureCreated(ConnectionString);

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='music_fts'";
        var result = cmd.ExecuteScalar();
        Assert.NotNull(result);
    }

    [Fact]
    public void EnsureCreated_CriaIndiceFTS5_Trigram()
    {
        DatabaseSchema.EnsureCreated(ConnectionString);

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='music_path_fts'";
        var result = cmd.ExecuteScalar();
        Assert.NotNull(result);
    }

    [Fact]
    public void EnsureCreated_CriaTriggers_DeSincronizacao()
    {
        DatabaseSchema.EnsureCreated(ConnectionString);

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='trigger' ORDER BY name";
        var triggers = new List<string>();
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
                triggers.Add(reader.GetString(0));
        }

        Assert.Contains("music_fts_insert", triggers);
        Assert.Contains("music_fts_update", triggers);
        Assert.Contains("music_fts_delete", triggers);
        Assert.Contains("music_path_fts_insert", triggers);
        Assert.DoesNotContain("music_path_fts_update", triggers);
        Assert.DoesNotContain("music_path_fts_delete", triggers);
    }

    [Fact]
    public void EnsureCreated_ChamaDuasVezes_NaoLancaErro()
    {
        DatabaseSchema.EnsureCreated(ConnectionString);
        DatabaseSchema.EnsureCreated(ConnectionString);

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table'";
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        Assert.True(count >= 5);
    }

    [Fact]
    public void EnsureCreated_InsertTrigger_SincronizaComFts5()
    {
        DatabaseSchema.EnsureCreated(ConnectionString);

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = """
            INSERT INTO music (title, artist, album, track, year, filename, relative_path, file_size, last_modified, last_scanned)
            VALUES ('Minha Música', 'Artista', 'Álbum', '1', '2024', 'musica.mp3', 'pasta/musica.mp3', 1024, '', '')
            """;
        insertCmd.ExecuteNonQuery();

        var ftsCmd = connection.CreateCommand();
        ftsCmd.CommandText = "SELECT COUNT(*) FROM music_fts WHERE music_fts MATCH '\"Música\"'";
        var ftsCount = Convert.ToInt32(ftsCmd.ExecuteScalar());
        Assert.Equal(1, ftsCount);

        var pathFtsCmd = connection.CreateCommand();
        pathFtsCmd.CommandText = "SELECT COUNT(*) FROM music_path_fts WHERE music_path_fts MATCH '\"pasta/musica.mp3\"'";
        var pathFtsCount = Convert.ToInt32(pathFtsCmd.ExecuteScalar());
        Assert.Equal(1, pathFtsCount);
    }

    [Fact]
    public void EnsureCreated_DeleteTrigger_RemoveDoFts5()
    {
        DatabaseSchema.EnsureCreated(ConnectionString);

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = """
            INSERT INTO music (title, artist, album, track, year, filename, relative_path, file_size, last_modified, last_scanned)
            VALUES ('Música Teste', 'Artista', 'Álbum', '1', '2024', 'teste.mp3', 'teste.mp3', 1024, '', '')
            """;
        insertCmd.ExecuteNonQuery();

        var ftsCountCmd = connection.CreateCommand();
        ftsCountCmd.CommandText = "SELECT COUNT(*) FROM music_fts";
        var ftsBefore = Convert.ToInt32(ftsCountCmd.ExecuteScalar());
        Assert.Equal(1, ftsBefore);

        var deleteCmd = connection.CreateCommand();
        deleteCmd.CommandText = "DELETE FROM music WHERE relative_path = 'teste.mp3'";
        deleteCmd.ExecuteNonQuery();

        var musicCmd = connection.CreateCommand();
        musicCmd.CommandText = "SELECT COUNT(*) FROM music";
        Assert.Equal(0, Convert.ToInt32(musicCmd.ExecuteScalar()));

        var ftsCmd = connection.CreateCommand();
        ftsCmd.CommandText = "SELECT COUNT(*) FROM music_fts";
        Assert.Equal(0, Convert.ToInt32(ftsCmd.ExecuteScalar()));
    }
}