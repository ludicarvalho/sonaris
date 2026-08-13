using Microsoft.AspNetCore.Http;

namespace Sonaris.Domain.DTOs.Music;

/// <summary>
/// Requisição para edição de metadados ID3 e capa de um MP3 (multipart/form-data).
/// </summary>
public class EditarMetadadosRequest
{
    public string FileName { get; set; }

    public string Title { get; set; }

    public string Artist { get; set; }

    public string Album { get; set; }

    public string Track { get; set; }

    public string Year { get; set; }

    public bool RemoverCapa { get; set; }

    public IFormFile Capa { get; set; }
}