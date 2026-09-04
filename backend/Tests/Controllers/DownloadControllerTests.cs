using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

using Xunit;

using System.Security.Claims;

namespace Sonaris.Backend.Tests.Controllers;

using Sonaris.Controllers;
using Sonaris.Domain.DTOs.Download;
using Sonaris.Domain.Infrastructure;
using Sonaris.Services.Download;

public class DownloadControllerTests
{
    private const string UserId = "user-teste";

    private readonly Mock<IPlaylistDownloadService> _downloadService;
    private readonly DownloadController _controller;

    public DownloadControllerTests()
    {
        _downloadService = new Mock<IPlaylistDownloadService>();
        _controller = new DownloadController(_downloadService.Object);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, UserId),
            new Claim("sub", UserId)
        ], "Test"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public void Construtor_ServiceNulo_LancaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DownloadController(null));
    }

    [Fact]
    public async Task Download_Sucesso_RetornaFileResult()
    {
        var expectedBytes = new byte[] { 0xFF, 0xFB, 0x90, 0x00 };
        _downloadService
            .Setup(s => s.DownloadTracksAsync(UserId, "playlist-1", new List<int> { 1 }))
            .ReturnsAsync(new DownloadTracksResponse
            {
                FileBytes = expectedBytes,
                FileName = "John Lennon - Imagine.mp3",
                ContentType = "audio/mpeg"
            });

        var resultado = await _controller.Download("playlist-1", new DownloadTracksRequest { TrackIds = [1] });

        var fileResult = Assert.IsType<FileContentResult>(resultado);
        Assert.Equal("audio/mpeg", fileResult.ContentType);
        Assert.Equal("John Lennon - Imagine.mp3", fileResult.FileDownloadName);
        Assert.Equal(expectedBytes, fileResult.FileContents);
    }

    [Fact]
    public async Task Download_RequestNulo_RetornaBadRequest()
    {
        var resultado = await _controller.Download("playlist-1", null);

        var badRequest = Assert.IsType<ObjectResult>(resultado);
        Assert.NotNull(badRequest.Value);

        Assert.True(badRequest.StatusCode == StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Download_ListaVazia_RetornaBadRequest()
    {
        var resultado = await _controller.Download("playlist-1", new DownloadTracksRequest { TrackIds = [] });

        var badRequest = Assert.IsType<ObjectResult>(resultado);
        Assert.NotNull(badRequest.Value);

        Assert.True(badRequest.StatusCode == StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Download_ServicoLancaSonarisException_RetornaBadRequest()
    {
        _downloadService
            .Setup(s => s.DownloadTracksAsync(UserId, "playlist-1", It.IsAny<List<int>>()))
            .ThrowsAsync(new SonarisException("Nenhuma faixa selecionada para download."));

        var resultado = await _controller.Download("playlist-1", new DownloadTracksRequest { TrackIds = [1] });

        var badRequest = Assert.IsType<ObjectResult>(resultado);
        Assert.NotNull(badRequest.Value);

        Assert.True(badRequest.StatusCode == StatusCodes.Status400BadRequest);
    }
}