using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sonaris.Controllers;

using Sonaris.Domain.Infrastructure;
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
            if (request == null || request.TrackIds.Count == 0)
                throw new SonarisException("Nenhuma faixa selecionada para download.");

            var result = await downloadService.DownloadTracksAsync(ObterUsuarioIdAtual(), id, request.TrackIds);

            Response.Headers.Append("Access-Control-Expose-Headers", "Content-Disposition");

            return File(result.FileBytes, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex is SonarisException ? ex.Message : "Erro interno ao processar o download.", ErrorDetails = ex is not SonarisException ? ex.ToString() : null });
        }
    }

    private string ObterUsuarioIdAtual()
        => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
           ?? User.FindFirst("sub")?.Value
           ?? throw new SonarisException("Usuário não identificado.");
}

public class DownloadTracksRequest
{
    public List<int> TrackIds { get; set; } = [];
}