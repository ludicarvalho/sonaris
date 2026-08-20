using Microsoft.AspNetCore.Mvc;
using Moq;
using Sonaris.Controllers;
using Sonaris.Domain.DTOs.Playlist;
using Sonaris.Domain.Infrastructure;
using Sonaris.Domain.Infrastructure.Response;
using Sonaris.Services.Playlists;
using Xunit;

namespace Sonaris.Backend.Tests.Controllers;

public class PlaylistControllerTests
{
    private readonly Mock<IPlaylistService> _playlistService;
    private readonly PlaylistController _controller;

    public PlaylistControllerTests()
    {
        _playlistService = new Mock<IPlaylistService>();
        _controller = new PlaylistController(_playlistService.Object);
    }

    [Fact]
    public void Construtor_ServiceNulo_LancaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PlaylistController(null));
    }

    [Fact]
    public void Listar_Sucesso_RetornaOkComDados()
    {
        var playlists = new List<PlaylistDto>
        {
            new() { Id = "1", Name = "Rock", CreatedAt = "2024-01-01", UpdatedAt = "2024-01-01", Tracks = [] },
            new() { Id = "2", Name = "Pop", CreatedAt = "2024-01-01", UpdatedAt = "2024-01-01", Tracks = [] }
        };
        _playlistService.Setup(s => s.GetAll()).Returns(playlists);

        var resultado = _controller.Listar();

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var response = Assert.IsType<BaseResponse<List<PlaylistDto>>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("Playlists listadas com sucesso.", response.Message);
        Assert.Equal(2, response.Data.Count);
    }

    [Fact]
    public void Listar_ServicoLancaExcecao_RetornaBadRequest()
    {
        _playlistService.Setup(s => s.GetAll()).Throws(new InvalidOperationException("boom"));

        var resultado = _controller.Listar();

        var objectResult = Assert.IsType<ObjectResult>(resultado);
        Assert.Equal(400, objectResult.StatusCode);
        var response = Assert.IsType<BaseResponse<List<PlaylistDto>>>(objectResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public void ObterPorId_PlaylistExistente_RetornaOk()
    {
        var playlist = new PlaylistDto { Id = "1", Name = "Rock", CreatedAt = "2024-01-01", UpdatedAt = "2024-01-01" };
        _playlistService.Setup(s => s.GetById("1")).Returns(playlist);

        var resultado = _controller.ObterPorId("1");

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var response = Assert.IsType<BaseResponse<PlaylistDto>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("Rock", response.Data.Name);
    }

    [Fact]
    public void ObterPorId_PlaylistInexistente_RetornaBadRequest()
    {
        _playlistService.Setup(s => s.GetById("nao-existe")).Returns((PlaylistDto)null);

        var resultado = _controller.ObterPorId("nao-existe");

        var objectResult = Assert.IsType<ObjectResult>(resultado);
        Assert.Equal(400, objectResult.StatusCode);
        var response = Assert.IsType<BaseResponse<PlaylistDto>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Playlist não encontrada.", response.Message);
    }

    [Fact]
    public void Criar_NomeValido_RetornaOkComPlaylist()
    {
        var created = new PlaylistDto { Id = "novo-id", Name = "Nova", CreatedAt = "2024-01-01", UpdatedAt = "2024-01-01" };
        _playlistService.Setup(s => s.Create("Nova")).Returns(created);

        var resultado = _controller.Criar("Nova");

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var response = Assert.IsType<BaseResponse<PlaylistDto>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("Nova", response.Data.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_NomeVazio_RetornaBadRequest(string name)
    {
        var resultado = _controller.Criar(name);

        var objectResult = Assert.IsType<ObjectResult>(resultado);
        Assert.Equal(400, objectResult.StatusCode);
        var response = Assert.IsType<BaseResponse<PlaylistDto>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Nome da playlist é obrigatório.", response.Message);
    }

    [Fact]
    public void Renomear_NomeValido_RetornaOk()
    {
        var renamed = new PlaylistDto { Id = "1", Name = "Renomeada", CreatedAt = "2024-01-01", UpdatedAt = "2024-01-02" };
        _playlistService.Setup(s => s.Rename("1", "Renomeada")).Returns(renamed);

        var resultado = _controller.Renomear("1", "Renomeada");

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var response = Assert.IsType<BaseResponse<PlaylistDto>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("Renomeada", response.Data.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Renomear_NomeVazio_RetornaBadRequest(string name)
    {
        var resultado = _controller.Renomear("1", name);

        var objectResult = Assert.IsType<ObjectResult>(resultado);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Fact]
    public void Deletar_Sucesso_RetornaOk()
    {
        var resultado = _controller.Deletar("1");

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var response = Assert.IsType<BaseResponse<object>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("Playlist deletada com sucesso.", response.Message);
        _playlistService.Verify(s => s.Delete("1"), Times.Once);
    }

    [Fact]
    public void AdicionarFaixa_CaminhoValido_RetornaOk()
    {
        var track = new PlaylistTrackDto { Id = 1, PlaylistId = "1", RelativePath = "musica.mp3", Position = 0 };
        _playlistService.Setup(s => s.AddTrack("1", "musica.mp3")).Returns(track);

        var resultado = _controller.AdicionarFaixa("1", "musica.mp3");

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var response = Assert.IsType<BaseResponse<PlaylistTrackDto>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("musica.mp3", response.Data.RelativePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AdicionarFaixa_CaminhoVazio_RetornaBadRequest(string relativePath)
    {
        var resultado = _controller.AdicionarFaixa("1", relativePath);

        var objectResult = Assert.IsType<ObjectResult>(resultado);
        Assert.Equal(400, objectResult.StatusCode);
        var response = Assert.IsType<BaseResponse<PlaylistTrackDto>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Caminho da música é obrigatório.", response.Message);
    }

    [Fact]
    public void RemoverFaixa_Sucesso_RetornaOk()
    {
        var resultado = _controller.RemoverFaixa("1", 42);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var response = Assert.IsType<BaseResponse<object>>(ok.Value);
        Assert.True(response.Success);
        _playlistService.Verify(s => s.RemoveTrack("1", 42), Times.Once);
    }

    [Fact]
    public void ReordenarFaixa_Sucesso_RetornaOk()
    {
        var resultado = _controller.ReordenarFaixa("1", 42, 2);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var response = Assert.IsType<BaseResponse<object>>(ok.Value);
        Assert.True(response.Success);
        _playlistService.Verify(s => s.ReorderTrack("1", 42, 2), Times.Once);
    }

    [Fact]
    public void Duplicar_NomeValido_RetornaOk()
    {
        _playlistService.Setup(s => s.GetAll()).Returns(new List<PlaylistDto>());

        var resultado = _controller.Duplicar("1", "Cópia");

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var response = Assert.IsType<BaseResponse<PlaylistDto>>(ok.Value);
        Assert.True(response.Success);
        _playlistService.Verify(s => s.Duplicate("1", "Cópia"), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Duplicar_NomeVazio_RetornaBadRequest(string newName)
    {
        var resultado = _controller.Duplicar("1", newName);

        var objectResult = Assert.IsType<ObjectResult>(resultado);
        Assert.Equal(400, objectResult.StatusCode);
    }
}
