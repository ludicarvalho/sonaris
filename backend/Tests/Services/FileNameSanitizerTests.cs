using Xunit;

namespace Sonaris.Backend.Tests.Services;

using Sonaris.Services.Download;

public class FileNameSanitizerTests
{
    [Fact]
    public void GenerateTrackFileName_ComMetatags_RetornaArtistaTitulo()
    {
        var result = FileNameSanitizer.GenerateTrackFileName("Imagine", "John Lennon", "original.mp3");

        Assert.Equal("John Lennon - Imagine.mp3", result);
    }

    [Fact]
    public void GenerateTrackFileName_SemMetatagDeTitulo_RetornaNomeOriginal()
    {
        var result = FileNameSanitizer.GenerateTrackFileName("", "John Lennon", "original.mp3");

        Assert.Equal("original.mp3", result);
    }

    [Fact]
    public void GenerateTrackFileName_SemMetatagDeArtista_RetornaNomeOriginal()
    {
        var result = FileNameSanitizer.GenerateTrackFileName("Imagine", "", "original.mp3");

        Assert.Equal("original.mp3", result);
    }

    [Fact]
    public void GenerateTrackFileName_Branco_RetornaNomeOriginal()
    {
        var result = FileNameSanitizer.GenerateTrackFileName("   ", "  ", "original.mp3");

        Assert.Equal("original.mp3", result);
    }

    [Fact]
    public void Sanitize_RemoveCaracteresInvalidos()
    {
        var result = FileNameSanitizer.Sanitize("a/b\\c:d*e?f\"g<h>i|j");

        Assert.Equal("a b c d e f g h i j", result);
    }

    [Fact]
    public void Sanitize_CollapseEspacosMultiplos()
    {
        var result = FileNameSanitizer.Sanitize("Titulo   Com    Espacos");

        Assert.Equal("Titulo Com Espacos", result);
    }

    [Fact]
    public void Sanitize_TrimEntradas()
    {
        var result = FileNameSanitizer.Sanitize("  espacos laterais  ");

        Assert.Equal("espacos laterais", result);
    }

    [Fact]
    public void Sanitize_NullOuVazio_RetornaVazio()
    {
        Assert.Equal(string.Empty, FileNameSanitizer.Sanitize(null));
        Assert.Equal(string.Empty, FileNameSanitizer.Sanitize(""));
        Assert.Equal(string.Empty, FileNameSanitizer.Sanitize("   "));
    }

    [Fact]
    public void Sanitize_TextoMuitoLongo_TruncaPara200Caracteres()
    {
        var longo = new string('a', 300);
        var result = FileNameSanitizer.Sanitize(longo);

        Assert.Equal(200, result.Length);
    }
}