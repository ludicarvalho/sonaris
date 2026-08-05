using Microsoft.AspNetCore.Mvc;

namespace Sonaris.Controllers;

using Sonaris.Domain.DTOs.Infrastructure;
using Sonaris.Domain.DTOs.Music;
using Sonaris.Domain.Infrastructure;
using Sonaris.Domain.Infrastructure.Response;
using Sonaris.Services.Arquivos;
using Sonaris.Services.Music;

[Route("api/Musica/[action]")]
public class MusicaController(IArquivoService arquivoService, IMusicMetadataReader musicMetadataReader, IConfiguration configuration) : BaseController
{
    private readonly IArquivoService arquivoService = arquivoService ?? throw new ArgumentNullException(nameof(arquivoService));
    private readonly IMusicMetadataReader musicMetadataReader = musicMetadataReader ?? throw new ArgumentNullException(nameof(musicMetadataReader));
    private readonly string MUSIC_PATH = configuration["Settings:MusicPath"] ?? "/Musicas";

    [HttpGet]
    public IActionResult StreamArquivo([FromQuery] string fileName)
    {
        try
        {
            var absolutePath = Path.GetFullPath(Path.Combine(MUSIC_PATH, fileName ?? string.Empty));

            if (!absolutePath.StartsWith(MUSIC_PATH, StringComparison.OrdinalIgnoreCase))
                throw new SonarisException("Arquivo/Diretório não encontrado.");

            if (!System.IO.File.Exists(absolutePath))
                throw new SonarisException("Arquivo/Diretório não encontrado.");

            var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            return File(fileStream: stream, contentType: "audio/mpeg", fileDownloadName: fileName, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            BaseResponse<string> response = new();
            response.MontarErro(ex);

            return Result(response);
        }
    }

    [HttpGet]
    public IActionResult BuscarMusicas([FromQuery] FilePathRequest request)
    {
        BasePagedResponse<FileSystemItemDto> response = new();

        try
        {
            var paged = arquivoService.RetornarDadosPorPath(request.Path, request.PageNumber, request.PageSize);

            response.Data = paged.Items.ToArray();
            response.PageInfo = new PageInfoRequest(paged.PageIndex, paged.PageSize);
            response.Pages = paged.TotalPages;
            response.ItemsTotal = paged.TotalCount;
            response.Success = true;
            response.Message = "Músicas encontradas com sucesso.";
        }
        catch (Exception ex)
        {
            response.MontarErro(ex);
        }

        return Result(response);
    }

    [HttpGet]
    public IActionResult BuscarMusicaMetadata([FromQuery] string fileName)
    {
        BaseResponse<MusicMetadata> response = new();

        try
        {
            var absolutePath = Path.GetFullPath(Path.Combine(MUSIC_PATH, fileName ?? string.Empty));

            if (!absolutePath.StartsWith(MUSIC_PATH, StringComparison.OrdinalIgnoreCase))
                throw new SonarisException("Arquivo não encontrado.");

            if (!System.IO.File.Exists(absolutePath))
                throw new SonarisException("Arquivo não encontrado.");

            response.Data = musicMetadataReader.RetornarMusicaMetadata(absolutePath);
            response.Success = true;
            response.Message = "Metadados encontrados com sucesso.";
        }
        catch (Exception ex)
        {
            response.MontarErro(ex);
        }

        return Result(response);
    }

    [HttpGet]
    public IActionResult StreamCapa([FromQuery] string fileName)
    {
        try
        {
            var absolutePath = Path.GetFullPath(Path.Combine(MUSIC_PATH, fileName ?? string.Empty));

            if (!absolutePath.StartsWith(MUSIC_PATH, StringComparison.OrdinalIgnoreCase))
                throw new SonarisException("Arquivo não encontrado.");

            if (!System.IO.File.Exists(absolutePath))
                throw new SonarisException("Arquivo não encontrado.");

            var capa = musicMetadataReader.RetornarCapaMusica(absolutePath);

            if (capa == null)
                throw new SonarisException("Capa não encontrada.");

            return File(capa.Data, capa.MimeType);
        }
        catch (Exception ex)
        {
            BaseResponse<string> response = new();
            response.MontarErro(ex);

            return Result(response);
        }
    }
}
