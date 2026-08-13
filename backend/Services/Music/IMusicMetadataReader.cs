namespace Sonaris.Services.Music;

using Sonaris.Domain.DTOs.Music;

public interface IMusicMetadataReader
{
    /// <summary>
    /// Lê os metadados ID3v2 (v2.3/v2.4) e do header MPEG de uma música MP3.
    /// </summary>
    MusicMetadata RetornarMusicaMetadata(string absolutePath);

    /// <summary>
    /// Retorna a capa embutida na tag ID3v2 de uma música MP3.
    /// </summary>
    MusicCover RetornarCapaMusica(string absolutePath);
}
