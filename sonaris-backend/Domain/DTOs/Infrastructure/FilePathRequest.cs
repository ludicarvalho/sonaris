using System.ComponentModel.DataAnnotations;

namespace Sonaris.Domain.DTOs.Infrastructure;

public class FilePathRequest
{
    public string Path { get; set; } = string.Empty;

    [Required]
    public int PageNumber { get; set; }

    [Required]
    public int PageSize { get; set; }
}
