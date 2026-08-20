using System.Threading;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

using Moq;

using Xunit;

namespace Sonaris.Backend.Tests.Services;

using Sonaris.Services.Search;

public class MusicSearchServiceTests : TestesBase, IDisposable
{
    private readonly MusicSearchService _service;

    public MusicSearchServiceTests() : base("search")
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Settings:DatabasePath"]).Returns(DbPath);

        DatabaseSchema.EnsureCreated($"Data Source={DbPath}");
        _service = new MusicSearchService(config.Object);
    }

    private void InsertMusic(string title, string artist, string album, string filename, string relativePath)
    {
        using var connection = new SqliteConnection(_service.ConnectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO music (title, artist, album, track, year, filename, relative_path, file_size, last_modified, last_scanned)
            VALUES (@title, @artist, @album, '', '', @filename, @relativePath, 1024, '', '')
            """;
        cmd.Parameters.AddWithValue("@title", title);
        cmd.Parameters.AddWithValue("@artist", artist);
        cmd.Parameters.AddWithValue("@album", album);
        cmd.Parameters.AddWithValue("@filename", filename);
        cmd.Parameters.AddWithValue("@relativePath", relativePath);
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public void Search_QueryVazio_RetornaPaginaVazia()
    {
        var result = _service.Search("");

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void Search_QueryNulo_RetornaPaginaVazia()
    {
        var result = _service.Search(null);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public void Search_QueryEspacosEmBranco_RetornaPaginaVazia()
    {
        var result = _service.Search("   ");

        Assert.Empty(result.Items);
    }

    [Fact]
    public void Search_PorTitulo_EncontraMusica()
    {
        InsertMusic("Minha Música", "Artista", "Álbum", "minha_musica.mp3", "pasta/minha_musica.mp3");
        InsertMusic("Outra Música", "Outro", "Álbum2", "outra.mp3", "pasta/outra.mp3");

        var result = _service.Search("Música");

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public void Search_PorArtista_EncontraMusica()
    {
        InsertMusic("Música 1", "Beatles", "Álbum", "b1.mp3", "pasta/b1.mp3");
        InsertMusic("Música 2", "Stones", "Álbum", "s1.mp3", "pasta/s1.mp3");

        var result = _service.Search("Beatles");

        Assert.Single(result.Items);
        Assert.Equal("Beatles", result.Items[0].Artist);
    }

    [Fact]
    public void Search_PorAlbum_EncontraMusica()
    {
        InsertMusic("Música 1", "Artista", "Abbey Road", "ar1.mp3", "pasta/ar1.mp3");

        var result = _service.Search("Abbey Road");

        Assert.Single(result.Items);
        Assert.Equal("Abbey Road", result.Items[0].Album);
    }

    [Fact]
    public void Search_PorFilename_EncontraViaTrigram()
    {
        InsertMusic("Música", "Artista", "Álbum", "rockao.mp3", "pasta/rockao.mp3");

        var result = _service.Search("rockao");

        Assert.Single(result.Items);
        Assert.Equal("path", result.Items[0].MatchSource);
    }

    [Fact]
    public void Search_PorCaminho_EncontraViaTrigram()
    {
        InsertMusic("Música", "Artista", "Álbum", "musica.mp3", "artistas/beatles/musica.mp3");

        var result = _service.Search("beatles");

        Assert.True(result.Items.Count >= 1);
    }

    [Fact]
    public void Search_ResultadosMetadadosTêmRankMaiorQueZero()
    {
        InsertMusic("Teste", "Artista", "Álbum", "teste.mp3", "pasta/teste.mp3");

        var result = _service.Search("Teste");

        Assert.All(result.Items.Where(i => i.MatchSource == "metadata"),
            i => Assert.True(i.Rank < 0));
    }

    [Fact]
    public void Search_ResultadosPathTêmRankZero()
    {
        InsertMusic("Música", "Artista", "Álbum", "beatles_song.mp3", "pasta/beatles_song.mp3");

        var result = _service.Search("beatles");

        var pathResults = result.Items.Where(i => i.MatchSource == "path").ToList();
        Assert.All(pathResults, i => Assert.Equal(0, i.Rank));
    }

    [Fact]
    public void Search_PaginacaoFunciona()
    {
        for (int i = 0; i < 10; i++)
            InsertMusic($"Música {i}", "Artista", "Álbum", $"m{i}.mp3", $"pasta/m{i}.mp3");

        var page1 = _service.Search("Música", pageNumber: 1, pageSize: 3);
        var page2 = _service.Search("Música", pageNumber: 2, pageSize: 3);

        Assert.Equal(3, page1.Items.Count);
        Assert.Equal(3, page2.Items.Count);
        Assert.Equal(10, page1.TotalCount);
        Assert.Equal(4, page1.TotalPages);
        Assert.Equal(1, page1.PageIndex);
        Assert.Equal(2, page2.PageIndex);

        var ids1 = page1.Items.Select(i => i.Id).ToHashSet();
        var ids2 = page2.Items.Select(i => i.Id).ToHashSet();
        Assert.Empty(ids1.Intersect(ids2));
    }

    [Fact]
    public async Task GetIndexedCountAsync_RetornaQuantidadeDeMusicasIndexadas()
    {
        InsertMusic("Música 1", "A", "B", "1.mp3", "1.mp3");
        InsertMusic("Música 2", "A", "B", "2.mp3", "2.mp3");
        InsertMusic("Música 3", "A", "B", "3.mp3", "3.mp3");

        var count = await _service.GetIndexedCountAsync();

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetIndexedCountAsync_BancoVazio_RetornaZero()
    {
        var count = await _service.GetIndexedCountAsync();

        Assert.Equal(0, count);
    }

    [Fact]
    public void Search_SanitizeFtsQuery_RemoveCaracteresEspeciais()
    {
        InsertMusic("Teste", "Artista", "Álbum", "teste.mp3", "pasta/teste.mp3");

        var result = _service.Search("test\"e(");

        Assert.True(result.TotalCount >= 1);
    }

    [Fact]
    public void Search_NenhumResultado_RetornaPaginaVazia()
    {
        InsertMusic("Rock", "Artista", "Álbum", "rock.mp3", "pasta/rock.mp3");

        var result = _service.Search("eletronica");

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }
}