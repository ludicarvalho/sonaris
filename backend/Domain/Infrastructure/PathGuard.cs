namespace Sonaris.Domain.Infrastructure;

/// <summary>
/// Proteções contra acesso a caminhos fora do diretório raiz permitido.
/// </summary>
public static class PathGuard
{
    /// <summary>
    /// Verifica se o caminho absoluto está dentro da raiz (ou é a própria raiz),
    /// sem sofrer bypass por prefixo de caminho irmão.
    /// </summary>
    public static bool IsUnderRoot(string absolutePath, string rootPath)
    {
        var root = Path.GetFullPath(rootPath);

        return absolutePath.Equals(root, StringComparison.OrdinalIgnoreCase)
            || absolutePath.StartsWith(EnsureTrailingSeparator(root), StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
}
