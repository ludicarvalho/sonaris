namespace Sonaris.Services.Arquivos;

using Sonaris.Domain.DTOs.Infrastructure;
using Sonaris.Domain.Infrastructure.Paging;

public interface IArquivoService
{
    /// <summary>
    /// Retorna a lista de diretórios e arquivos .mp3 de um caminho, paginado.
    /// </summary>
    PagedResult<FileSystemItemDto> RetornarDadosPorPath(string path, int pageNumber, int pageSize);
}
