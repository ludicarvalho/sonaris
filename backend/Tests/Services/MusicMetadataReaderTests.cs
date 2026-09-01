using Xunit;

namespace Sonaris.Backend.Tests.Services;

using Sonaris.Services.Music;

public class MusicMetadataReaderTests : TestesBase
{
    private readonly MusicMetadataReader _reader;

    public MusicMetadataReaderTests() : base("metadata")
    {
        _reader = new MusicMetadataReader();
    }

    private string CriarMusica(string nomeDiretorio = "album dir")
    {
        var diretorio = Path.Combine(Path.GetDirectoryName(DbPath), nomeDiretorio);
        Directory.CreateDirectory(diretorio);

        var caminho = Path.Combine(diretorio, "musica.mp3");
        File.WriteAllBytes(caminho, [0xFF, 0xFB, 0x90, 0x00]);
        return caminho;
    }

    private void CriarImagem(string nome, string diretorio = null)
    {
        var baseDir = diretorio == null
            ? Path.GetDirectoryName(CriarMusica())
            : Path.Combine(Path.GetDirectoryName(DbPath), diretorio);
        
        File.WriteAllBytes(Path.Combine(baseDir, nome), [1, 2, 3]);
    }

    [Fact]
    public void RetornarCapaMusica_SemCapaEmbutida_UsaPrimeiraImagemAlfabetica()
    {
        var musica = CriarMusica();
        CriarImagem("zzz-foto.jpg");

        var capa = _reader.RetornarCapaMusica(musica);

        Assert.NotNull(capa);
        Assert.Equal("image/jpeg", capa.MimeType);
        Assert.Equal([1, 2, 3], capa.Data);
    }

    [Fact]
    public void RetornarCapaMusica_FolderTemPrioridadeSobreOutras()
    {
        var musica = CriarMusica();
        CriarImagem("aaa.jpg");
        CriarImagem("folder.png");

        var capa = _reader.RetornarCapaMusica(musica);

        Assert.Equal("image/png", capa.MimeType);
    }

    [Fact]
    public void RetornarCapaMusica_NomeFolderCaseInsensitive()
    {
        var musica = CriarMusica();
        CriarImagem("Folder.JPG");
        CriarImagem("album.jpeg");
        CriarImagem("aaa.jpg");

        var capa = _reader.RetornarCapaMusica(musica);

        Assert.Equal("image/jpeg", capa.MimeType);
    }

    [Fact]
    public void RetornarCapaMusica_AlbumVenceAlfabeticaQuandoSemFolder()
    {
        var musica = CriarMusica();
        CriarImagem("z.png");
        CriarImagem("album.jpg");

        var capa = _reader.RetornarCapaMusica(musica);

        Assert.NotNull(capa);
        Assert.Equal("image/jpeg", capa.MimeType);
    }

    [Fact]
    public void RetornarCapaMusica_DiretorioSemImagem_RetornaNull()
    {
        var musica = CriarMusica();

        var capa = _reader.RetornarCapaMusica(musica);

        Assert.Null(capa);
    }
}
