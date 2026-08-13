namespace Sonaris.Services.Arquivos;

using Sonaris.Domain.DTOs.Infrastructure;
using Sonaris.Domain.Infrastructure;
using Sonaris.Domain.Infrastructure.Paging;

public class ArquivoService(IConfiguration configuration) : IArquivoService
{
    private readonly string MUSIC_PATH = configuration["Settings:MusicPath"] ?? "/Musicas";

    public PagedResult<FileSystemItemDto> RetornarDadosPorPath(string path, int pageNumber, int pageSize)
    {
        try
        {
            var diretorioRaiz = Path.GetFullPath(MUSIC_PATH);

            if (!Directory.Exists(diretorioRaiz))
                throw new DirectoryNotFoundException($"Diretório de músicas não encontrado: {diretorioRaiz}");

            var currentPath = Path.GetFullPath(Path.Combine(diretorioRaiz, path ?? string.Empty));

            if (!currentPath.StartsWith(diretorioRaiz, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException();

            var directory = new DirectoryInfo(currentPath);

            if (!directory.Exists)
                throw new SonarisException("Arquivo/Diretório não encontrado.");

            var directories = new List<FileSystemItemDto>();

            if (pageNumber < 2)
            {
                directories = directory
                    .EnumerateDirectories()
                    .Select(d => new FileSystemItemDto(d.FullName)
                    {
                        Name = d.Name,
                        RelativePath = Path.GetRelativePath(diretorioRaiz, d.FullName),
                        IsDirectory = true,
                        LastModified = d.LastWriteTimeUtc
                    })
                    .OrderBy(d => d.Name)
                    .ToList();
            }

            var files = directory
                .EnumerateFiles()
                .Where(f => f.Extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase))
                .Select(f => new FileSystemItemDto(f.FullName)
                {
                    Name = f.Name,
                    RelativePath = Path.GetRelativePath(diretorioRaiz, f.FullName),
                    IsDirectory = false,
                    Size = f.Length,
                    LastModified = f.LastWriteTimeUtc
                })
                .OrderBy(a => a.Name)
                .ToList();

            var all = directories.Concat(files).ToList();

            var result = new PagedResult<FileSystemItemDto>
            {
                PageIndex = pageNumber,
                PageSize = pageSize,
                TotalCount = all.Count,
                TotalPages = (int)Math.Ceiling(all.Count / (double)Math.Max(1, pageSize)),
                Items = all.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList()
            };

            return result;
        }
        catch (SonarisException) { throw; }
        catch (Exception ex)
        {
            throw new SonarisException("Erro ao tentar recuperar lista de arquivos.", ex);
        }
    }

    public List<FileSystemItemDto> BuscarPorNome(string termo)
    {
        try
        {
            const int limiteResultados = 30;

            var diretorioRaiz = Path.GetFullPath(MUSIC_PATH);

            if (!Directory.Exists(diretorioRaiz))
                throw new DirectoryNotFoundException($"Diretório de músicas não encontrado: {diretorioRaiz}");

            var busca = (termo ?? string.Empty).Trim();

            if (busca.Length == 0)
                return [];

            var opcoes = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            };

            return Directory
                .EnumerateFiles(diretorioRaiz, "*.mp3", opcoes)
                .Select(caminho => new FileInfo(caminho))
                .Where(f => f.Name.Contains(busca, StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f.Name)
                .Take(limiteResultados)
                .Select(f => new FileSystemItemDto(f.FullName)
                {
                    Name = f.Name,
                    RelativePath = Path.GetRelativePath(diretorioRaiz, f.FullName),
                    IsDirectory = false,
                    Size = f.Length,
                    LastModified = f.LastWriteTimeUtc
                })
                .ToList();
        }
        catch (SonarisException) { throw; }
        catch (Exception ex)
        {
            throw new SonarisException("Erro ao tentar buscar músicas pelo nome.", ex);
        }
    }
}
