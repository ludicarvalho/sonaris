namespace Sonaris.Services.Search;

using Sonaris.Domain.DTOs.Music;
using Sonaris.Domain.Infrastructure.Paging;

public interface IMusicSearchService
{
    string ConnectionString { get; }

    /// <summary>
    /// Busca híbrida: FTS5 padrão (metadados) + trigram (filename/caminho).
    /// Resultados combinados e ranqueados por BM25.
    /// </summary>
    PagedResult<MusicSearchResult> Search(string query, int pageNumber = 1, int pageSize = 30);
    Task<int> GetIndexedCountAsync();
}
