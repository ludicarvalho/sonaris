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
        if (Directory.Exists(DbPath))
            Directory.Delete(DbPath, recursive: true);
    }
}