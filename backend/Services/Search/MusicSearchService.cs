using Microsoft.Data.Sqlite;

namespace Sonaris.Services.Search;

using Sonaris.Domain.DTOs.Music;
using Sonaris.Domain.Infrastructure.Paging;

public class MusicSearchService : IMusicSearchService
{
    private readonly string _connectionString;

    public MusicSearchService(IConfiguration configuration)
    {
        var dbPath = configuration["Settings:DatabasePath"]
            ?? Path.Combine(AppContext.BaseDirectory, "sonaris.db");
        _connectionString = $"Data Source={dbPath}";
    }

    public string ConnectionString => _connectionString;

    public PagedResult<MusicSearchResult> Search(string query, int pageNumber = 1, int pageSize = 30)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new PagedResult<MusicSearchResult>
            {
                PageIndex = pageNumber,
                PageSize = pageSize,
                TotalCount = 0,
                TotalPages = 0,
                Items = []
            };

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sanitizedQuery = SanitizeFtsQuery(query.Trim());

        var countCmd = connection.CreateCommand();
        countCmd.CommandText = """
            SELECT COUNT(*) FROM (
                SELECT m.id FROM music_fts fts
                JOIN music m ON m.id = fts.rowid
                WHERE music_fts MATCH @query
                UNION ALL
                SELECT m.id FROM music_path_fts fts
                JOIN music m ON m.id = fts.rowid
                WHERE music_path_fts MATCH @queryWrapped
            )
            """;
        countCmd.Parameters.AddWithValue("@query", sanitizedQuery);
        countCmd.Parameters.AddWithValue("@queryWrapped", $"\"{query.Trim()}\"");
        var totalCount = Convert.ToInt32(countCmd.ExecuteScalar());

        var searchCmd = connection.CreateCommand();
        var offset = (pageNumber - 1) * pageSize;
        searchCmd.CommandText = """
            SELECT id, title, artist, album, filename, relative_path, rank, snippet, match_source
            FROM (
                SELECT m.id, m.title, m.artist, m.album, m.filename, m.relative_path,
                       fts.rank AS rank,
                       snippet(music_fts, 0, '<b>', '</b>', '...', 32) AS snippet,
                       'metadata' AS match_source
                FROM music_fts fts
                JOIN music m ON m.id = fts.rowid
                WHERE music_fts MATCH @query

                UNION ALL

                SELECT m.id, m.title, m.artist, m.album, m.filename, m.relative_path,
                       0 AS rank,
                       '' AS snippet,
                       'path' AS match_source
                FROM music_path_fts fts
                JOIN music m ON m.id = fts.rowid
                WHERE music_path_fts MATCH @queryWrapped
            )
            ORDER BY rank
            LIMIT @limit OFFSET @offset
            """;
        searchCmd.Parameters.AddWithValue("@query", sanitizedQuery);
        searchCmd.Parameters.AddWithValue("@queryWrapped", $"\"{query.Trim()}\"");
        searchCmd.Parameters.AddWithValue("@limit", pageSize);
        searchCmd.Parameters.AddWithValue("@offset", offset);

        var results = new List<MusicSearchResult>();
        using (var reader = searchCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                results.Add(new MusicSearchResult
                {
                    Id = reader.GetInt64(0),
                    Title = reader.GetString(1),
                    Artist = reader.GetString(2),
                    Album = reader.GetString(3),
                    Filename = reader.GetString(4),
                    RelativePath = reader.GetString(5),
                    Rank = reader.GetDouble(6),
                    Snippet = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    MatchSource = reader.GetString(8)
                });
            }
        }

        return new PagedResult<MusicSearchResult>
        {
            PageIndex = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)Math.Max(1, pageSize)),
            Items = results
        };
    }

    public async Task<int> GetIndexedCountAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM music";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static string SanitizeFtsQuery(string input)
    {
        var cleaned = input
            .Replace("\"", "")
            .Replace("'", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("{", "")
            .Replace("}", "")
            .Replace(":", "")
            .Replace("^", "")
            .Trim();

        if (string.IsNullOrEmpty(cleaned))
            return string.Empty;

        return $"\"{cleaned}\"";
    }
}
