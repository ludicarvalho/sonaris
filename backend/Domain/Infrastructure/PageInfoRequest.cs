namespace Sonaris.Domain.Infrastructure;

/// <summary>
/// Struct de PageInfo
/// </summary>
public struct PageInfoRequest
{
    public PageInfoRequest(int pageNumber = 1, int itemsPerPage = 10)
    {
        PageNumber = pageNumber;
        PageSize = itemsPerPage;
    }

    public int PageNumber { get; set; }

    public int PageSize { get; set; }
}
