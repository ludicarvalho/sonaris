using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sonaris.Controllers;

using Sonaris.Domain.DTOs.Infrastructure;
using Sonaris.Domain.DTOs.Music;
using Sonaris.Domain.Infrastructure;
using Sonaris.Domain.Infrastructure.Response;
using Sonaris.Services.Arquivos;
using Sonaris.Services.Music;
using Sonaris.Services.Search;

[Route("api/Musica/[action]")]
[Authorize]
public class MusicaController(IArquivoService arquivoService, IMusicMetadataReader musicMetadataReader, IMusicMetadataWriter musicMetadataWriter, IConfiguration configuration, IMusicSearchService musicSearchService) : BaseController
{
    private readonly IArquivoService arquivoService = arquivoService ?? throw new ArgumentNullException(nameof(arquivoService));
    private readonly IMusicMetadataReader musicMetadataReader = musicMetadataReader ?? throw new ArgumentNullException(nameof(musicMetadataReader));
    private readonly IMusicMetadataWriter musicMetadataWriter = musicMetadataWriter ?? throw new ArgumentNullException(nameof(musicMetadataWriter));
    private readonly IMusicSearchService musicSearchService = musicSearchService ?? throw new ArgumentNullException(nameof(musicSearchService));
    private readonly string MUSIC_PATH = configuration["Settings:MusicPath"] ?? "/Musicas";

    [HttpGet]
    public IActionResult StreamArquivo([FromQuery] string fileName)
    {
        try
        {
            var absolutePath = Path.GetFullPath(Path.Combine(MUSIC_PATH, fileName ?? string.Empty));

            if (!PathGuard.IsUnderRoot(absolutePath, MUSIC_PATH))
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

            response.Data = [.. paged.Items];
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
    public IActionResult BuscarPorNome([FromQuery] string termo)
    {
        BaseResponse<FileSystemItemDto[]> response = new();

        try
        {
            response.Data = [.. arquivoService.BuscarPorNome(termo)];
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

            if (!PathGuard.IsUnderRoot(absolutePath, MUSIC_PATH))
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

            if (!PathGuard.IsUnderRoot(absolutePath, MUSIC_PATH))
                throw new SonarisException("Arquivo não encontrado.");

            if (!System.IO.File.Exists(absolutePath))
                throw new SonarisException("Arquivo não encontrado.");

            var capa = musicMetadataReader.RetornarCapaMusica(absolutePath);

            return (capa == null)
                ? throw new SonarisException("Capa não encontrada.")
                : (IActionResult)File(capa.Data, capa.MimeType);
        }
        catch (Exception ex)
        {
            BaseResponse<string> response = new();
            response.MontarErro(ex);

            return Result(response);
        }
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> EditarMetadados([FromForm] EditarMetadadosRequest request)
    {
        BaseResponse<string> response = new();

        try
        {
            var absolutePath = Path.GetFullPath(Path.Combine(MUSIC_PATH, request.FileName ?? string.Empty));

            if (!PathGuard.IsUnderRoot(absolutePath, MUSIC_PATH))
                throw new SonarisException("Arquivo não encontrado.");

            if (!System.IO.File.Exists(absolutePath))
                throw new SonarisException("Arquivo não encontrado.");

            if (!Path.GetExtension(absolutePath).Equals(".mp3", StringComparison.OrdinalIgnoreCase))
                throw new SonarisException("Apenas arquivos MP3 podem ter metadados editados.");

            byte[] capaBytes = null;

            if (request.Capa is { Length: > 0 })
            {
                using var stream = new MemoryStream();
                await request.Capa.CopyToAsync(stream);
                capaBytes = stream.ToArray();
            }

            musicMetadataWriter.SalvarMetadados(new SalvarMetadadosRequest
            {
                AbsolutePath = absolutePath,
                Titulo = request.Title ?? string.Empty,
                Artista = request.Artist ?? string.Empty,
                Album = request.Album ?? string.Empty,
                Faixa = request.Track ?? string.Empty,
                Ano = request.Year ?? string.Empty,
                CapaBytes = capaBytes,
                CapaMimeType = request.Capa?.ContentType,
                RemoverCapa = request.RemoverCapa
            });

            response.Success = true;
            response.Message = "Metadados atualizados com sucesso.";
        }
        catch (Exception ex)
        {
            response.MontarErro(ex);
        }

        return Result(response);
    }

    [HttpGet]
    public IActionResult BuscarFullText([FromQuery] string termo, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 30)
    {
        BasePagedResponse<MusicSearchResult> response = new();

        try
        {
            var paged = musicSearchService.Search(termo, pageNumber, pageSize);

            response.Data = [.. paged.Items];
            response.PageInfo = new PageInfoRequest(paged.PageIndex, paged.PageSize);
            response.Pages = paged.TotalPages;
            response.ItemsTotal = paged.TotalCount;
            response.Success = true;
            response.Message = "Busca realizada com sucesso.";
        }
        catch (Exception ex)
        {
            response.MontarErro(ex);
        }

        return Result(response);
    }
}
