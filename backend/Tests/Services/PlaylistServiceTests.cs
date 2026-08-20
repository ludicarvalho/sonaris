using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Moq;
using Sonaris.Domain.Infrastructure;
using Sonaris.Services.Playlists;
using Sonaris.Services.Search;
using Xunit;

namespace Sonaris.Backend.Tests.Services;

public class PlaylistServiceTests : TestesBase, IDisposable
{
    private readonly PlaylistService _service;

    public PlaylistServiceTests() : base("playlist")
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Settings:DatabasePath"]).Returns(DbPath);

        DatabaseSchema.EnsureCreated($"Data Source={DbPath}");
        _service = new PlaylistService(config.Object);
    }

    [Fact]
    public void Create_RetornaPlaylistComIdEName()
    {
        var result = _service.Create("Minha Playlist");

        Assert.NotNull(result.Id);
        Assert.NotEmpty(result.Id);
        Assert.Equal("Minha Playlist", result.Name);
        Assert.NotNull(result.CreatedAt);
        Assert.NotNull(result.UpdatedAt);
        Assert.Empty(result.Tracks);
    }

    [Fact]
    public void Create_SalvaNoBanco()
    {
        var created = _service.Create("Rock");

        var all = _service.GetAll();
        Assert.Single(all);
        Assert.Equal(created.Id, all[0].Id);
        Assert.Equal("Rock", all[0].Name);
    }

    [Fact]
    public void GetAll_RetornaPlaylistsOrdenadasPorNome()
    {
        _service.Create("Zebra");
        _service.Create("Alpha");
        _service.Create("Beta");

        var all = _service.GetAll();

        Assert.Equal(3, all.Count);
        Assert.Equal("Alpha", all[0].Name);
        Assert.Equal("Beta", all[1].Name);
        Assert.Equal("Zebra", all[2].Name);
    }

    [Fact]
    public void GetAll_ComFaixas_RetornaFaixasNaPlaylist()
    {
        var playlist = _service.Create("Teste");
        _service.AddTrack(playlist.Id, "musica1.mp3");
        _service.AddTrack(playlist.Id, "musica2.mp3");

        var all = _service.GetAll();

        Assert.Single(all);
        Assert.Equal(2, all[0].Tracks.Count);
    }

    [Fact]
    public void GetById_PlaylistExistente_RetornaPlaylist()
    {
        var created = _service.Create("Existente");

        var found = _service.GetById(created.Id);

        Assert.NotNull(found);
        Assert.Equal(created.Id, found.Id);
        Assert.Equal("Existente", found.Name);
    }

    [Fact]
    public void GetById_PlaylistInexistente_RetornaNull()
    {
        var found = _service.GetById("nao-existe");

        Assert.Null(found);
    }

    [Fact]
    public void Rename_AlteraNomeDaPlaylist()
    {
        var playlist = _service.Create("Original");

        _service.Rename(playlist.Id, "Renomeada");

        var found = _service.GetById(playlist.Id);
        Assert.Equal("Renomeada", found.Name);
    }

    [Fact]
    public void Delete_RemovePlaylist()
    {
        var playlist = _service.Create("Para Deletar");

        _service.Delete(playlist.Id);

        var all = _service.GetAll();
        Assert.Empty(all);
    }

    [Fact]
    public void Delete_PlaylistInexistente_NaoLancaErro()
    {
        _service.Delete("nao-existe");
    }

    [Fact]
    public void AddTrack_AdicionaFaixaComPosicaoCorreta()
    {
        var playlist = _service.Create("Coom Faixas");

        var track1 = _service.AddTrack(playlist.Id, "musica1.mp3");
        var track2 = _service.AddTrack(playlist.Id, "musica2.mp3");
        var track3 = _service.AddTrack(playlist.Id, "musica3.mp3");

        Assert.Equal(0, track1.Position);
        Assert.Equal(1, track2.Position);
        Assert.Equal(2, track3.Position);
    }

    [Fact]
    public void AddTrack_RetornaFaixaComId()
    {
        var playlist = _service.Create("Teste");

        var track = _service.AddTrack(playlist.Id, "musica.mp3");

        Assert.True(track.Id > 0);
        Assert.Equal(playlist.Id, track.PlaylistId);
        Assert.Equal("musica.mp3", track.RelativePath);
    }

    [Fact]
    public void AddTrack_AtualizaUpdatedAtDaPlaylist()
    {
        var playlist = _service.Create("Teste");
        var before = playlist.UpdatedAt;

        Thread.Sleep(10);
        _service.AddTrack(playlist.Id, "musica.mp3");

        var after = _service.GetById(playlist.Id);
        Assert.True(DateTime.Parse(after.UpdatedAt) >= DateTime.Parse(before));
    }

    [Fact]
    public void RemoveTrack_RemoveFaixaDaPlaylist()
    {
        var playlist = _service.Create("Com Faixas");
        var track1 = _service.AddTrack(playlist.Id, "musica1.mp3");
        var track2 = _service.AddTrack(playlist.Id, "musica2.mp3");

        _service.RemoveTrack(playlist.Id, track1.Id);

        var found = _service.GetById(playlist.Id);
        Assert.Single(found.Tracks);
        Assert.Equal("musica2.mp3", found.Tracks[0].RelativePath);
    }

    [Fact]
    public void RemoveTrack_FaixaDeOutraPlaylist_NaoRemove()
    {
        var playlist1 = _service.Create("Playlist 1");
        var playlist2 = _service.Create("Playlist 2");
        var track1 = _service.AddTrack(playlist1.Id, "musica1.mp3");
        _service.AddTrack(playlist2.Id, "musica2.mp3");

        _service.RemoveTrack(playlist2.Id, track1.Id);

        var found1 = _service.GetById(playlist1.Id);
        Assert.Single(found1.Tracks);
    }

    [Fact]
    public void ReorderTrack_AlteraPosicaoDaFaixa()
    {
        var playlist = _service.Create("Reorder");
        var track1 = _service.AddTrack(playlist.Id, "a.mp3");
        var track2 = _service.AddTrack(playlist.Id, "b.mp3");

        _service.ReorderTrack(playlist.Id, track2.Id, 99);

        using var connection = new SqliteConnection($"Data Source={DbPath}");
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT position FROM playlist_track WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", track2.Id);
        var pos = Convert.ToInt32(cmd.ExecuteScalar());
        Assert.Equal(99, pos);
    }

    [Fact]
    public void Duplicate_CriaNovaPlaylistComMesmasFaixas()
    {
        var original = _service.Create("Original");
        _service.AddTrack(original.Id, "musica1.mp3");
        _service.AddTrack(original.Id, "musica2.mp3");

        _service.Duplicate(original.Id, "Cópia");

        var all = _service.GetAll();
        Assert.Equal(2, all.Count);

        var copia = all.First(p => p.Name == "Cópia");
        Assert.NotNull(copia);
        Assert.Equal(2, copia.Tracks.Count);
        Assert.Equal("musica1.mp3", copia.Tracks[0].RelativePath);
        Assert.Equal("musica2.mp3", copia.Tracks[1].RelativePath);
    }

    [Fact]
    public void Duplicate_PlaylistInexistente_LancaSonarisException()
    {
        Assert.Throws<SonarisException>(() => _service.Duplicate("nao-existe", "Cópia"));
    }

    [Fact]
    public void GetAll_PlaylistsSemFaixas_RetornaListaVaziaDeFaixas()
    {
        _service.Create("Vazia");
        _service.Create("Também Vazia");

        var all = _service.GetAll();

        Assert.Equal(2, all.Count);
        Assert.All(all, p => Assert.Empty(p.Tracks));
    }

    [Fact]
    public void Create_MultiplosCreates_GeraIdsDiferentes()
    {
        var p1 = _service.Create("Playlist 1");
        var p2 = _service.Create("Playlist 2");

        Assert.NotEqual(p1.Id, p2.Id);
    }
}
