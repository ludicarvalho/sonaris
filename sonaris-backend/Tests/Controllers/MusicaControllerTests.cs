namespace Sonaris.Backend.Tests.Controllers;

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Sonaris.Controllers;
using Sonaris.Domain.DTOs.Infrastructure;
using Sonaris.Domain.DTOs.Music;
using Sonaris.Domain.Infrastructure;
using Sonaris.Domain.Infrastructure.Paging;
using Sonaris.Domain.Infrastructure.Response;
using Sonaris.Services.Arquivos;
using Sonaris.Services.Music;
using Xunit;

public class MusicaControllerTests : IDisposable
{
    private const string MensagemGenerica = "Ocorreu um erro inesperado ao processar a sua solicitação. Verifique os dados e tente novamente.";

    private readonly string _musicPath;
    private readonly Mock<IArquivoService> _arquivoService;
    private readonly Mock<IMusicMetadataReader> _musicMetadataReader;
    private readonly Mock<IMusicMetadataWriter> _musicMetadataWriter;
    private readonly Mock<IConfiguration> _configuration;
    private readonly MusicaController _controller;

    public MusicaControllerTests()
    {
        _musicPath = Path.Combine(Path.GetTempPath(), "sonaris-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_musicPath);

        _arquivoService = new Mock<IArquivoService>();
        _musicMetadataReader = new Mock<IMusicMetadataReader>();
        _musicMetadataWriter = new Mock<IMusicMetadataWriter>();

        _configuration = new Mock<IConfiguration>();
        _configuration.Setup(c => c["Settings:MusicPath"]).Returns(_musicPath);

        _controller = new MusicaController(_arquivoService.Object, _musicMetadataReader.Object, _musicMetadataWriter.Object, _configuration.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_musicPath))
            Directory.Delete(_musicPath, recursive: true);
    }

    private string CriarArquivo(string nome, byte[] conteudo = null)
    {
        var caminho = Path.Combine(_musicPath, nome);
        File.WriteAllBytes(caminho, conteudo ?? [0xFF, 0xFB, 0x90, 0x00]);
        return caminho;
    }

    private static ObjectResult AssertBadRequest(IActionResult resultado)
    {
        var objectResult = Assert.IsType<ObjectResult>(resultado);
        Assert.Equal(400, objectResult.StatusCode);
        return objectResult;
    }

    [Fact]
    public void Construtor_ArquivoServiceNulo_LancaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MusicaController(null, _musicMetadataReader.Object, _musicMetadataWriter.Object, _configuration.Object));
    }

    [Fact]
    public void Construtor_MusicMetadataReaderNulo_LancaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MusicaController(_arquivoService.Object, null, _musicMetadataWriter.Object, _configuration.Object));
    }

    [Fact]
    public void Construtor_MusicMetadataWriterNulo_LancaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MusicaController(_arquivoService.Object, _musicMetadataReader.Object, null, _configuration.Object));
    }

    [Fact]
    public void Construtor_ConfigSemMusicPath_UsaPadraoMusicas()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Settings:MusicPath"]).Returns((string)null);

        var controller = new MusicaController(_arquivoService.Object, _musicMetadataReader.Object, _musicMetadataWriter.Object, config.Object);

        var campo = typeof(MusicaController).GetField("MUSIC_PATH", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.Equal("/Musicas", campo.GetValue(controller));
    }

    [Fact]
    public void StreamArquivo_ArquivoExistente_RetornaFileStreamResult()
    {
        CriarArquivo("musica.mp3");

        var resultado = _controller.StreamArquivo("musica.mp3");

        var fileResult = Assert.IsType<FileStreamResult>(resultado);
        Assert.Equal("audio/mpeg", fileResult.ContentType);
        Assert.True(fileResult.EnableRangeProcessing);
        Assert.Equal("musica.mp3", fileResult.FileDownloadName);
        Assert.True(fileResult.FileStream.CanRead);
        fileResult.FileStream.Dispose();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void StreamArquivo_FileNameNuloOuVazio_RetornaBadRequest(string fileName)
    {
        var resultado = _controller.StreamArquivo(fileName);

        var objectResult = AssertBadRequest(resultado);
        var response = Assert.IsType<BaseResponse<string>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Arquivo/Diretório não encontrado.", response.Message);
    }

    [Theory]
    [InlineData("../fora.mp3")]
    [InlineData("../../etc/passwd")]
    public void StreamArquivo_PathTraversal_RetornaBadRequest(string fileName)
    {
        var resultado = _controller.StreamArquivo(fileName);

        var objectResult = AssertBadRequest(resultado);
        var response = Assert.IsType<BaseResponse<string>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Arquivo/Diretório não encontrado.", response.Message);
    }

    [Fact]
    public void StreamArquivo_ArquivoInexistente_RetornaBadRequest()
    {
        var resultado = _controller.StreamArquivo("nao_existe.mp3");

        var objectResult = AssertBadRequest(resultado);
        var response = Assert.IsType<BaseResponse<string>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Arquivo/Diretório não encontrado.", response.Message);
    }

    [Fact]
    public void BuscarMusicas_Sucesso_RetornaOkComDadosPaginados()
    {
        var itens = new List<FileSystemItemDto>
        {
            new("a.mp3")
            {
                Name = "a.mp3",
                RelativePath = "a.mp3",
                IsDirectory = false,
                Size = 100
            },
            new("b.mp3")
            {
                Name = "b.mp3",
                RelativePath = "b.mp3",
                IsDirectory = false,
                Size = 200
            }
        };

        _arquivoService
            .Setup(s => s.RetornarDadosPorPath(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new PagedResult<FileSystemItemDto>
            {
                PageIndex = 2,
                PageSize = 10,
                TotalCount = 25,
                TotalPages = 3,
                Items = itens
            });

        var resultado = _controller.BuscarMusicas(new FilePathRequest { Path = "/", PageNumber = 2, PageSize = 10 });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var response = Assert.IsType<BasePagedResponse<FileSystemItemDto>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("Músicas encontradas com sucesso.", response.Message);
        Assert.Equal(2, response.Data.Length);
        Assert.Equal("a.mp3", response.Data[0].Name);
        Assert.Equal(2, response.PageInfo.PageNumber);
        Assert.Equal(10, response.PageInfo.PageSize);
        Assert.Equal(3, response.Pages);
        Assert.Equal(25, response.ItemsTotal);
    }

    [Fact]
    public void BuscarMusicas_PathNulo_NaoLancaERetornaOk()
    {
        _arquivoService
            .Setup(s => s.RetornarDadosPorPath(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new PagedResult<FileSystemItemDto>());

        var resultado = _controller.BuscarMusicas(new FilePathRequest { Path = null, PageNumber = 1, PageSize = 10 });

        Assert.IsType<OkObjectResult>(resultado);
    }

    [Fact]
    public void BuscarMusicas_ServicoLancaSonarisException_RetornaBadRequest()
    {
        _arquivoService
            .Setup(s => s.RetornarDadosPorPath(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Throws(new SonarisException("Pasta inválida."));

        var resultado = _controller.BuscarMusicas(new FilePathRequest { Path = "x", PageNumber = 1, PageSize = 10 });

        var objectResult = AssertBadRequest(resultado);
        var response = Assert.IsType<BasePagedResponse<FileSystemItemDto>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Pasta inválida.", response.Message);
    }

    [Fact]
    public void BuscarMusicas_ServicoLancaExcecaoGenerica_RetornaBadRequest()
    {
        _arquivoService
            .Setup(s => s.RetornarDadosPorPath(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Throws(new InvalidOperationException("boom"));

        var resultado = _controller.BuscarMusicas(new FilePathRequest { Path = "x", PageNumber = 1, PageSize = 10 });

        var objectResult = AssertBadRequest(resultado);
        var response = Assert.IsType<BasePagedResponse<FileSystemItemDto>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal(MensagemGenerica, response.Message);
        Assert.Equal("boom", response.ErrorDetails);
    }

    [Fact]
    public void BuscarMusicaMetadata_ArquivoExistente_RetornaOkComMetadata()
    {
        CriarArquivo("musica.mp3");

        _musicMetadataReader
            .Setup(r => r.RetornarMusicaMetadata(It.IsAny<string>()))
            .Returns(new MusicMetadata { Title = "Título", Artist = "Artista", Album = "Álbum" });

        var resultado = _controller.BuscarMusicaMetadata("musica.mp3");

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var response = Assert.IsType<BaseResponse<MusicMetadata>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("Metadados encontrados com sucesso.", response.Message);
        Assert.Equal("Título", response.Data.Title);
        Assert.Equal("Artista", response.Data.Artist);
    }

    [Fact]
    public void BuscarMusicaMetadata_ArquivoInexistente_RetornaBadRequest()
    {
        var resultado = _controller.BuscarMusicaMetadata("nao_existe.mp3");

        var objectResult = AssertBadRequest(resultado);
        var response = Assert.IsType<BaseResponse<MusicMetadata>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Arquivo não encontrado.", response.Message);
    }

    [Fact]
    public void BuscarMusicaMetadata_PathTraversal_RetornaBadRequest()
    {
        var resultado = _controller.BuscarMusicaMetadata("../fora.mp3");

        var objectResult = AssertBadRequest(resultado);
        var response = Assert.IsType<BaseResponse<MusicMetadata>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Arquivo não encontrado.", response.Message);
    }

    [Fact]
    public void BuscarMusicaMetadata_ReaderLancaSonarisException_RetornaBadRequest()
    {
        CriarArquivo("musica.mp3");

        _musicMetadataReader
            .Setup(r => r.RetornarMusicaMetadata(It.IsAny<string>()))
            .Throws(new SonarisException("Erro ao ler metadados."));

        var resultado = _controller.BuscarMusicaMetadata("musica.mp3");

        var objectResult = AssertBadRequest(resultado);
        var response = Assert.IsType<BaseResponse<MusicMetadata>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Erro ao ler metadados.", response.Message);
    }

    [Fact]
    public void StreamCapa_CapaExistente_RetornaFileContentResult()
    {
        CriarArquivo("musica.mp3");

        var capa = new MusicCover("image/jpeg", [0xFF, 0xD8, 0xFF, 0xE0]);
        _musicMetadataReader
            .Setup(r => r.RetornarCapaMusica(It.IsAny<string>()))
            .Returns(capa);

        var resultado = _controller.StreamCapa("musica.mp3");

        var fileResult = Assert.IsType<FileContentResult>(resultado);
        Assert.Equal("image/jpeg", fileResult.ContentType);
        Assert.Equal(capa.Data, fileResult.FileContents);
    }

    [Fact]
    public void StreamCapa_SemCapa_RetornaBadRequest()
    {
        CriarArquivo("musica.mp3");

        _musicMetadataReader
            .Setup(r => r.RetornarCapaMusica(It.IsAny<string>()))
            .Returns((MusicCover)null);

        var resultado = _controller.StreamCapa("musica.mp3");

        var objectResult = AssertBadRequest(resultado);
        var response = Assert.IsType<BaseResponse<string>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Capa não encontrada.", response.Message);
    }

    [Theory]
    [InlineData("../fora.mp3")]
    [InlineData("nao_existe.mp3")]
    public void StreamCapa_ArquivoNaoEncontrado_RetornaBadRequest(string fileName)
    {
        var resultado = _controller.StreamCapa(fileName);

        var objectResult = AssertBadRequest(resultado);
        var response = Assert.IsType<BaseResponse<string>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Arquivo não encontrado.", response.Message);
    }

    [Fact]
    public void StreamCapa_ReaderLancaExcecao_RetornaBadRequest()
    {
        CriarArquivo("musica.mp3");

        _musicMetadataReader
            .Setup(r => r.RetornarCapaMusica(It.IsAny<string>()))
            .Throws(new InvalidOperationException("boom"));

        var resultado = _controller.StreamCapa("musica.mp3");

        var objectResult = AssertBadRequest(resultado);
        var response = Assert.IsType<BaseResponse<string>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal(MensagemGenerica, response.Message);
        Assert.Equal("boom", response.ErrorDetails);
    }

    [Fact]
    public async Task EditarMetadados_Sucesso_RetornaOkEChamaWriter()
    {
        CriarArquivo("musica.mp3");

        var request = new EditarMetadadosRequest
        {
            FileName = "musica.mp3",
            Title = "Novo título",
            Artist = "Novo artista",
            Album = "Álbum",
            Track = "3/12",
            Year = "2020"
        };

        var resultado = await _controller.EditarMetadados(request);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var response = Assert.IsType<BaseResponse<string>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("Metadados atualizados com sucesso.", response.Message);
        _musicMetadataWriter.Verify(w => w.SalvarMetadados(It.Is<SalvarMetadadosRequest>(r =>
            r.AbsolutePath != null &&
            r.Titulo == "Novo título" &&
            r.Artista == "Novo artista" &&
            r.Album == "Álbum" &&
            r.Faixa == "3/12" &&
            r.Ano == "2020" &&
            r.CapaBytes == null &&
            r.RemoverCapa == false)), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task EditarMetadados_FileNameNuloOuVazio_RetornaBadRequest(string fileName)
    {
        var resultado = await _controller.EditarMetadados(new EditarMetadadosRequest { FileName = fileName });

        var objectResult = AssertBadRequest(resultado);
        var response = Assert.IsType<BaseResponse<string>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Arquivo não encontrado.", response.Message);
        _musicMetadataWriter.Verify(w => w.SalvarMetadados(It.IsAny<SalvarMetadadosRequest>()), Times.Never);
    }

    [Fact]
    public async Task EditarMetadados_ArquivoInexistente_RetornaBadRequest()
    {
        var resultado = await _controller.EditarMetadados(new EditarMetadadosRequest { FileName = "nao_existe.mp3" });

        var objectResult = AssertBadRequest(resultado);
        var response = Assert.IsType<BaseResponse<string>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Arquivo não encontrado.", response.Message);
    }

    [Fact]
    public async Task EditarMetadados_PathTraversal_RetornaBadRequest()
    {
        var resultado = await _controller.EditarMetadados(new EditarMetadadosRequest { FileName = "../fora.mp3" });

        var objectResult = AssertBadRequest(resultado);
        var response = Assert.IsType<BaseResponse<string>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Arquivo não encontrado.", response.Message);
    }

    [Fact]
    public async Task EditarMetadados_ArquivoNaoMp3_RetornaBadRequest()
    {
        CriarArquivo("musica.txt");

        var resultado = await _controller.EditarMetadados(new EditarMetadadosRequest { FileName = "musica.txt" });

        var objectResult = AssertBadRequest(resultado);
        var response = Assert.IsType<BaseResponse<string>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Apenas arquivos MP3 podem ter metadados editados.", response.Message);
        _musicMetadataWriter.Verify(w => w.SalvarMetadados(It.IsAny<SalvarMetadadosRequest>()), Times.Never);
    }

    [Fact]
    public async Task EditarMetadados_ComCapa_ChamaWriterComBytes()
    {
        CriarArquivo("musica.mp3");

        var capa = new FormFile(new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0]), 0, 4, "capa", "capa.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        var request = new EditarMetadadosRequest
        {
            FileName = "musica.mp3",
            Title = "T",
            Capa = capa
        };

        var resultado = await _controller.EditarMetadados(request);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var response = Assert.IsType<BaseResponse<string>>(ok.Value);
        Assert.True(response.Success);

        _musicMetadataWriter.Verify(w => w.SalvarMetadados(It.Is<SalvarMetadadosRequest>(r =>
            r.CapaBytes != null && r.CapaBytes.Length == 4 && r.CapaMimeType == "image/jpeg" && r.RemoverCapa == false)), Times.Once);
    }

    [Fact]
    public async Task EditarMetadados_WriterLancaSonarisException_RetornaBadRequest()
    {
        CriarArquivo("musica.mp3");

        _musicMetadataWriter
            .Setup(w => w.SalvarMetadados(It.IsAny<SalvarMetadadosRequest>()))
            .Throws(new SonarisException("Falha ao executar 'mid3v2'."));

        var resultado = await _controller.EditarMetadados(new EditarMetadadosRequest { FileName = "musica.mp3", Title = "T" });

        var objectResult = AssertBadRequest(resultado);
        var response = Assert.IsType<BaseResponse<string>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Falha ao executar 'mid3v2'.", response.Message);
    }
}
