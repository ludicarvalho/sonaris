using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sonaris.Controllers;

using Sonaris.Domain.DTOs.Download;
using Sonaris.Domain.Infrastructure;
using Sonaris.Domain.Infrastructure.Response;
using Sonaris.Services.Download;

[Route("api/Playlist")]
[Authorize]
public class DownloadController(IPlaylistDownloadService downloadService) : BaseController
{
    private readonly IPlaylistDownloadService downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));

    [HttpPost("{id}/download")]
    public async Task<IActionResult> Download(string id, [FromBody] DownloadTracksRequest request)
    {
        try
        {
            if (request == null || !request.TrackIds.Any())
                throw new SonarisException("Nenhuma faixa selecionada para download.");

            var result = await downloadService.DownloadTracksAsync(ObterUsuarioIdAtual(), id, request.TrackIds);

            Response.Headers.Append("Access-Control-Expose-Headers", "Content-Disposition");

            return File(result.FileBytes, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            var response = new BaseResponse<string>();
            response.MontarErro(ex);
            return Result(response);
        }
    }
}