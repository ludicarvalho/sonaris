using Microsoft.Extensions.Configuration;
using Moq;
using Sonaris.Domain.Infrastructure;
using Sonaris.Services.Arquivos;
using Xunit;

namespace Sonaris.Backend.Tests.Services;

public class ArquivoServiceTests : IDisposable
{
    private readonly string _musicPath;
    private readonly ArquivoService _service;

    public ArquivoServiceTests()
    {
        _musicPath = Path.Combine(Path.GetTempPath(), $"sonaris-arquivos-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_musicPath);

        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Settings:MusicPath"]).Returns(_musicPath);
        _service = new ArquivoService(config.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_musicPath))
            Directory.Delete(_musicPath, recursive: true);
    }

    private string CriarDiretorio(string nome)
    {
        var caminho = Path.Combine(_musicPath, nome);
        Directory.CreateDirectory(caminho);
        return caminho;
    }

    private void CriarMp3(string nome)
    {
        File.WriteAllText(Path.Combine(_musicPath, nome), "mp3 fake");
    }

    [Fact]
    public void RetornarDadosPorPath_MaisDiretoriosQuePageSizeESemMp3_Pagina2RetornaRestante()
    {
        for (int i = 1; i <= 46; i++)
            CriarDiretorio($"album-{i:00}");

        var pagina1 = _service.RetornarDadosPorPath(string.Empty, 1, 30);

        Assert.Equal(46, pagina1.TotalCount);
        Assert.Equal(2, pagina1.TotalPages);
        Assert.Equal(30, pagina1.Items.Count);
        Assert.All(pagina1.Items, i => Assert.True(i.IsDirectory));

        var pagina2 = _service.RetornarDadosPorPath(string.Empty, 2, 30);

        Assert.Equal(16, pagina2.Items.Count);
        Assert.All(pagina2.Items, i => Assert.True(i.IsDirectory));
        Assert.Empty(pagina2.Items.Select(i => i.Name).Intersect(pagina1.Items.Select(i => i.Name)));
        Assert.Equal(46, pagina2.TotalCount);
    }

    [Fact]
    public void RetornarDadosPorPath_DiretoriosEArquivosMistos_NenhumItemPuladoOuRepetido()
    {
        for (int i = 1; i <= 5; i++)
            CriarDiretorio($"pasta-{i:00}");
        for (int i = 1; i <= 20; i++)
            CriarMp3($"musica-{i:00}.mp3");

        var esperados = Enumerable.Range(1, 5).Select(i => $"pasta-{i:00}")
            .Concat(Enumerable.Range(1, 20).Select(i => $"musica-{i:00}.mp3"))
            .ToList();

        var todos = new List<string>();
        var totalPaginasEsperado = (int)Math.Ceiling(25 / 10.0);
        int? totalCount = null;

        for (int pagina = 1; pagina <= totalPaginasEsperado; pagina++)
        {
            var resultado = _service.RetornarDadosPorPath(string.Empty, pagina, 10);

            Assert.Equal(totalCount ?? resultado.TotalCount, resultado.TotalCount);
            totalCount = resultado.TotalCount;
            Assert.Equal(25, resultado.TotalCount);
            Assert.Equal(totalPaginasEsperado, resultado.TotalPages);

            todos.AddRange(resultado.Items.Select(i => i.Name));
        }

        Assert.Equal(esperados, todos);
        Assert.Equal(todos.Count, todos.Distinct().Count());
    }

    [Fact]
    public void RetornarDadosPorPath_CaminhoForaDaRaiz_LancaExcecao()
    {
        Assert.Throws<SonarisException>(() =>
            _service.RetornarDadosPorPath("../fora-da-raiz", 1, 30));
    }

    [Fact]
    public void RetornarDadosPorPath_Subdirectory_RetornaItensDoSubdiretorio()
    {
        CriarDiretorio("Tiao Carreiro");
        CriarMp3("raiz.mp3");

        var resultado = _service.RetornarDadosPorPath("Tiao Carreiro", 1, 30);

        Assert.Empty(resultado.Items);
        Assert.Equal(0, resultado.TotalCount);
        Assert.NotEqual(0, _service.RetornarDadosPorPath(string.Empty, 1, 30).TotalCount);
    }
}

public class PathGuardTests
{
    private static string Caminho(params string[] partes) =>
        Path.GetFullPath(Path.Combine([.. partes]));

    [Fact]
    public void IsUnderRoot_CaminhoDentroDaRaiz_RetornaTrue()
    {
        var raiz = Caminho("/Musicas");
        Assert.True(PathGuard.IsUnderRoot(Caminho("/Musicas", "Sertanejo", "Tiao Carreiro"), raiz));
    }

    [Fact]
    public void IsUnderRoot_PropriaRaiz_RetornaTrue()
    {
        var raiz = Caminho("/Musicas");
        Assert.True(PathGuard.IsUnderRoot(raiz, raiz));
    }

    [Fact]
    public void IsUnderRoot_PrefixoDeIrmao_NaoPassa()
    {
        Assert.False(PathGuard.IsUnderRoot(Caminho("/Musicas", "Tiao Carreiro 2"), Caminho("/Musicas", "Tiao Carreiro")));
    }

    [Fact]
    public void IsUnderRoot_PathTraversal_NaoPassa()
    {
        Assert.False(PathGuard.IsUnderRoot(Caminho("/Musicas", "..", "etc"), Caminho("/Musicas")));
    }
}
