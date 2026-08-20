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
        Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);

        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Settings:DatabasePath"]).Returns(DbPath);

        DatabaseSchema.EnsureCreated($"Data Source={DbPath}");
    }

    public void Dispose()
    {
        if (File.Exists(DbPath))
        {
            // Tenta deletar com varios intentos e delays para lidar com SQLite em uso
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    File.Delete(DbPath);
                    return;
                }
                catch (IOException)
                {
                    Thread.Sleep(100);
                }
            }
        }
    }
}