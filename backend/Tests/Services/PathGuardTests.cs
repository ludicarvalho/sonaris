using Xunit;

namespace Sonaris.Backend.Tests.Services;

using Sonaris.Domain.Infrastructure;

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
