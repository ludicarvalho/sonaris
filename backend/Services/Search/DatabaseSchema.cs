using Microsoft.Data.Sqlite;

namespace Sonaris.Services.Search;

public static class DatabaseSchema
{
    public static void EnsureCreated(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            -- Tabela principal de músicas indexadas
            CREATE TABLE IF NOT EXISTS music (
                id              INTEGER PRIMARY KEY AUTOINCREMENT,
                title           TEXT NOT NULL DEFAULT '',
                artist          TEXT NOT NULL DEFAULT '',
                album           TEXT NOT NULL DEFAULT '',
                track           TEXT NOT NULL DEFAULT '',
                year            TEXT NOT NULL DEFAULT '',
                duration_secs   REAL,
                bitrate         INTEGER,
                filename        TEXT NOT NULL,
                relative_path   TEXT NOT NULL UNIQUE,
                file_size       INTEGER NOT NULL DEFAULT 0,
                last_modified   TEXT NOT NULL DEFAULT '',
                last_scanned    TEXT NOT NULL DEFAULT ''
            );

            -- Tabela de usuários
            CREATE TABLE IF NOT EXISTS usuario (
                id            TEXT PRIMARY KEY,
                username      TEXT NOT NULL UNIQUE,
                senha_hash    TEXT NOT NULL,
                senha_salt    TEXT NOT NULL,
                nome_exibicao TEXT NOT NULL DEFAULT '',
                is_admin      INTEGER NOT NULL DEFAULT 0,
                created_at    TEXT NOT NULL DEFAULT ''
            );

            -- Índice FTS5 padrão (unicode61) — busca por palavras em metadados
            CREATE VIRTUAL TABLE IF NOT EXISTS music_fts USING fts5(
                title, artist, album,
                content='music',
                content_rowid='id'
            );

            -- Índice FTS5 trigram — busca por substring em filename/caminho
            CREATE VIRTUAL TABLE IF NOT EXISTS music_path_fts USING fts5(
                filename, relative_path,
                tokenize='trigram'
            );

            -- Triggers de sincronização automática (music_fts)
            CREATE TRIGGER IF NOT EXISTS music_fts_insert AFTER INSERT ON music BEGIN
                INSERT INTO music_fts(rowid, title, artist, album)
                VALUES (new.id, new.title, new.artist, new.album);
            END;

            CREATE TRIGGER IF NOT EXISTS music_fts_update AFTER UPDATE ON music BEGIN
                INSERT INTO music_fts(music_fts, rowid, title, artist, album)
                VALUES ('delete', old.id, old.title, old.artist, old.album);
                INSERT INTO music_fts(rowid, title, artist, album)
                VALUES (new.id, new.title, new.artist, new.album);
            END;

            CREATE TRIGGER IF NOT EXISTS music_fts_delete AFTER DELETE ON music BEGIN
                INSERT INTO music_fts(music_fts, rowid, title, artist, album)
                VALUES ('delete', old.id, old.title, old.artist, old.album);
            END;

            -- Triggers de sincronização automática (music_path_fts)
            -- Obs: trigram FTS5 não suporta 'delete' incremental.
            -- O indexer faz rebuild completo de music_path_fts a cada scan.
            CREATE TRIGGER IF NOT EXISTS music_path_fts_insert AFTER INSERT ON music BEGIN
                INSERT INTO music_path_fts(rowid, filename, relative_path)
                VALUES (new.id, new.filename, new.relative_path);
            END;

            -- Tabela de playlists
            CREATE TABLE IF NOT EXISTS playlist (
                id          TEXT PRIMARY KEY,
                name        TEXT NOT NULL,
                user_id     TEXT,
                created_at  TEXT NOT NULL,
                updated_at  TEXT NOT NULL
            );

            -- Tabela de faixas da playlist (com ordem)
            CREATE TABLE IF NOT EXISTS playlist_track (
                id              INTEGER PRIMARY KEY AUTOINCREMENT,
                playlist_id     TEXT NOT NULL REFERENCES playlist(id) ON DELETE CASCADE,
                relative_path   TEXT NOT NULL,
                title           TEXT NOT NULL DEFAULT '',
                artist          TEXT NOT NULL DEFAULT '',
                album           TEXT NOT NULL DEFAULT '',
                position        INTEGER NOT NULL DEFAULT 0,
                added_at        TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_playlist_track_playlist_id
                ON playlist_track(playlist_id, position);
            """;

        command.ExecuteNonQuery();

        EnsurePlaylistOwnerColumn(connection);
    }

    /// <summary>
    /// Garante a coluna user_id em bancos existentes (SQLite não suporta
    /// ADD COLUMN IF NOT EXISTS) e o índice correspondente. Migração idempotente.
    /// </summary>
    private static void EnsurePlaylistOwnerColumn(SqliteConnection connection)
    {
        using var check = connection.CreateCommand();
        check.CommandText = """
            SELECT COUNT(*) FROM pragma_table_info('playlist') WHERE name = 'user_id'
            """;

        if (Convert.ToInt32(check.ExecuteScalar()) == 0)
        {
            using var alter = connection.CreateCommand();
            alter.CommandText = "ALTER TABLE playlist ADD COLUMN user_id TEXT";
            alter.ExecuteNonQuery();
        }

        using var index = connection.CreateCommand();
        index.CommandText = "CREATE INDEX IF NOT EXISTS idx_playlist_user_id ON playlist(user_id)";
        index.ExecuteNonQuery();
    }
}
