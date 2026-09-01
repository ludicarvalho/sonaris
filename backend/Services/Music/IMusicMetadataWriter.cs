namespace Sonaris.Services.Music;

using Sonaris.Domain.DTOs.Music;

/// <summary>
/// Grava metadados ID3 e capa (APIC) em arquivos MP3 usando mutagen (Python),
/// forçando ID3 v2.3 e encoding Latin-1 na capa.
/// </summary>
public interface IMusicMetadataWriter
{
    void SalvarMetadados(SalvarMetadadosRequest request);
}