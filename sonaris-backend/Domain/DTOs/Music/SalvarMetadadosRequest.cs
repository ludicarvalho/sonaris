namespace Sonaris.Domain.DTOs.Music;

/// <summary>
/// Dados para gravação de metadados ID3 e capa (APIC) em um arquivo MP3.
/// </summary>
public class SalvarMetadadosRequest
{
    public string AbsolutePath { get; set; }

    public string Titulo { get; set; }

    public string Artista { get; set; }

    public string Album { get; set; }

    public string Faixa { get; set; }

    public string Ano { get; set; }

    public byte[] CapaBytes { get; set; }

    public string CapaMimeType { get; set; }

    public bool RemoverCapa { get; set; }
}