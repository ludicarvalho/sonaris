namespace Sonaris.Domain.DTOs.Infrastructure;

public class FileSystemItemDto
{
    public FileSystemItemDto() { }

    public FileSystemItemDto(string filePath)
        => AbsolutePath = filePath;

    public string Name { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;

    public bool IsDirectory { get; set; }

    public long? Size { get; set; }

    public DateTime LastModified { get; set; }

    private string AbsolutePath { get; set; }

    public string GetAbsolutePath()
        => AbsolutePath;
}
