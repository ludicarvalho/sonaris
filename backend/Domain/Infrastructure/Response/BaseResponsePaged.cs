namespace Sonaris.Domain.Infrastructure.Response;

/// <summary>
/// Objeto de retorno padrão paginado para o controller.
/// </summary>
public class BasePagedResponse<TData> : BaseResponseAbstract where TData : class
{
    public BasePagedResponse() { }

    public new TData[] Data { get; set; }

    public PageInfoRequest PageInfo { get; set; }

    public int Pages { get; set; }

    public int ItemsTotal { get; set; }
}
