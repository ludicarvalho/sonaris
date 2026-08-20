using Microsoft.AspNetCore.Mvc;

namespace Sonaris.Controllers;

using Sonaris.Domain.DTOs.Playlist;
using Sonaris.Domain.Infrastructure;
using Sonaris.Domain.Infrastructure.Response;
using Sonaris.Services.Playlists;

[Route("api/Playlist")]
public class PlaylistController(IPlaylistService playlistService) : BaseController
{
    private readonly IPlaylistService playlistService = playlistService ?? throw new ArgumentNullException(nameof(playlistService));

    [HttpGet]
    public IActionResult Listar()
    {
        BaseResponse<IEnumerable<PlaylistDto>> response = new();

        try
        {
            response.Data = playlistService.GetAll();
            response.Success = true;
            response.Message = "Playlists listadas com sucesso.";
        }
        catch (Exception ex)
        {
            response.MontarErro(ex);
        }

        return Result(response);
    }

    [HttpGet("{id}")]
    public IActionResult ObterPorId(string id)
    {
        BaseResponse<PlaylistDto> response = new();

        try
        {
            response.Data = playlistService.GetById(id)
                ?? throw new SonarisException("Playlist não encontrada.");
            response.Success = true;
            response.Message = "Playlist encontrada com sucesso.";
        }
        catch (Exception ex)
        {
            response.MontarErro(ex);
        }

        return Result(response);
    }

    [HttpPost]
    public IActionResult Criar([FromBody] string name)
    {
        BaseResponse<PlaylistDto> response = new();

        try
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new SonarisException("Nome da playlist é obrigatório.");

            response.Data = playlistService.Create(name);
            response.Success = true;
            response.Message = "Playlist criada com sucesso.";
        }
        catch (Exception ex)
        {
            response.MontarErro(ex);
        }

        return Result(response);
    }

    [HttpPut("{id}")]
    public IActionResult Renomear(string id, [FromBody] string name)
    {
        BaseResponse<PlaylistDto> response = new();

        try
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new SonarisException("Nome da playlist é obrigatório.");

            response.Data = playlistService.Rename(id, name);
            response.Success = true;
            response.Message = "Playlist renomeada com sucesso.";
        }
        catch (Exception ex)
        {
            response.MontarErro(ex);
        }

        return Result(response);
    }

    [HttpDelete("{id}")]
    public IActionResult Deletar(string id)
    {
        BaseResponse<object> response = new();

        try
        {
            playlistService.Delete(id);
            response.Success = true;
            response.Message = "Playlist deletada com sucesso.";
        }
        catch (Exception ex)
        {
            response.MontarErro(ex);
        }

        return Result(response);
    }

    [HttpPost("{id}/tracks")]
    public IActionResult AdicionarFaixa(string id, [FromQuery] string relativePath)
    {
        BaseResponse<PlaylistTrackDto> response = new();

        try
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new SonarisException("Caminho da música é obrigatório.");

            response.Data = playlistService.AddTrack(id, relativePath);
            response.Success = true;
            response.Message = "Faixa adicionada à playlist com sucesso.";
        }
        catch (Exception ex)
        {
            response.MontarErro(ex);
        }

        return Result(response);
    }

    [HttpDelete("{id}/tracks/{trackId}")]
    public IActionResult RemoverFaixa(string id, long trackId)
    {
        BaseResponse<object> response = new();

        try
        {
            playlistService.RemoveTrack(id, trackId);
            response.Success = true;
            response.Message = "Faixa removida da playlist com sucesso.";
        }
        catch (Exception ex)
        {
            response.MontarErro(ex);
        }

        return Result(response);
    }

    [HttpPut("{id}/tracks/{trackId}/reorder")]
    public IActionResult ReordenarFaixa(string id, long trackId, [FromQuery] int newPosition)
    {
        BaseResponse<object> response = new();

        try
        {
            playlistService.ReorderTrack(id, trackId, newPosition);
            response.Success = true;
            response.Message = "Faixa reordenada com sucesso.";
        }
        catch (Exception ex)
        {
            response.MontarErro(ex);
        }

        return Result(response);
    }

    [HttpPost("{id}/duplicate")]
    public IActionResult Duplicar(string id, [FromQuery] string newName)
    {
        BaseResponse<PlaylistDto> response = new();

        try
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new SonarisException("Nome da nova playlist é obrigatório.");

            playlistService.Duplicate(id, newName);
            response.Data = playlistService.GetAll().LastOrDefault(p => p.Name == newName);
            response.Success = true;
            response.Message = "Playlist duplicada com sucesso.";
        }
        catch (Exception ex)
        {
            response.MontarErro(ex);
        }

        return Result(response);
    }
}
