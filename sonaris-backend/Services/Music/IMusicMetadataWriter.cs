namespace Sonaris.Services.Music;

/// <summary>
/// Grava metadados ID3 e capa (APIC) em arquivos MP3 usando as ferramentas
/// mid3v2 (campos de texto) e eyeD3 (imagem) disponíveis no container.
/// </summary>
public interface IMusicMetadataWriter
{
    void SalvarMetadados(
        string absolutePath,
        string titulo,
        string artista,
        string album,
        string faixa,
        string ano,
        byte[] capaBytes = null,
        string capaMimeType = null,
        bool removerCapa = false);
}