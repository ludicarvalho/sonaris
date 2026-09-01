using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

using Moq;

namespace Sonaris.Backend.Tests.Services;

using Sonaris.Services.Search;

public abstract class TestesBase : IDisposable
{
    protected string DbPath { get; }

    public TestesBase(string pastaPrefixo)
    {
        DbPath = Path.Combine(Path.GetTempPath(), $"sonaris-{pastaPrefixo}-tests", Guid.NewGuid().ToString("N") + ".db");

        var path = Path.GetDirectoryName(DbPath);

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Settings:DatabasePath"]).Returns(DbPath);

        DatabaseSchema.EnsureCreated($"Data Source={DbPath}");
    }

    public void Dispose()
    {
        var diretorio = Path.GetDirectoryName(DbPath);
        if (diretorio == null || !Directory.Exists(diretorio))
            return;

        // Tenta por vários intentos e delays para lidar com o SQLite em uso (pool de conexões)
        for (int i = 0; i < 20; i++)
        {
            try
            {
                Directory.Delete(diretorio, recursive: true);
                return;
            }
            catch (IOException)
            {
                Thread.Sleep(50);
            }
            catch (SqliteException)
            {
                Thread.Sleep(50);
            }
        }
    }
}