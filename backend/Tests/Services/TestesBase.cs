using System;
using System.Threading;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Moq;
using Sonaris.Services.Search;

namespace Sonaris.Backend.Tests.Services;

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
            for (int i = 0; i < 20; i++)
            {
                try
                {
                    using var connection = new SqliteConnection($"Data Source={DbPath}");
                    connection.Open();
                    using var command = connection.CreateCommand();
                    command.CommandText = "PRAGMA locking_mode = exclusive";
                    command.ExecuteNonQuery();
                    File.Delete(DbPath);
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
}