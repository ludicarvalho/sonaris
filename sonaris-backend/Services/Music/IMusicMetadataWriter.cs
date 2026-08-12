namespace Sonaris.Services.Music;

using Sonaris.Domain.DTOs.Music;

/// <summary>
/// Grava metadados ID3 e capa (APIC) em arquivos MP3 usando as ferramentas
/// mid3v2 (campos de texto) e eyeD3 (imagem) disponíveis no container.
/// </summary>
public interface IMusicMetadataWriter
{
    void SalvarMetadados(SalvarMetadadosRequest request);
}