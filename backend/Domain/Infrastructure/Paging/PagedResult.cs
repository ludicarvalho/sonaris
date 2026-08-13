namespace Sonaris.Domain.Infrastructure.Paging;

/// <summary>
/// Resultado paginado genérico.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];

    public int PageIndex { get; set; } = 1;

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }
}
