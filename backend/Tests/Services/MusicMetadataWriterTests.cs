using Sonaris.Domain.DTOs.Music;
using Sonaris.Services.Music;
using Xunit;

namespace Sonaris.Backend.Tests.Services;

public class MusicMetadataWriterTests : IDisposable
{
    private readonly string _pasta;
    private readonly MusicMetadataWriter _writer;

    public MusicMetadataWriterTests()
    {
        _pasta = Path.Combine(Path.GetTempPath(), $"sonaris-metadata-writer-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_pasta);
        _writer = new MusicMetadataWriter();
    }

    public void Dispose()
    {
        if (Directory.Exists(_pasta))
            Directory.Delete(_pasta, recursive: true);
    }

    private string CriarMusica()
    {
        var caminho = Path.Combine(_pasta, "musica.mp3");
        File.WriteAllBytes(caminho, new byte[10240]);
        return caminho;
    }

    private static byte[] CriarJpeg()
        => [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, (byte)'J', (byte)'F', (byte)'I', (byte)'F'];

    private static bool MutagenDisponivel()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("python3", "-c")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            psi.ArgumentList.Add("import mutagen");
            using var processo = System.Diagnostics.Process.Start(psi)!;
            processo.StandardOutput.ReadToEnd();
            string erro = processo.StandardError.ReadToEnd();
            processo.WaitForExit();
            return processo.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    [Fact]
    public void SalvarMetadados_GravaTextoECapaEmId3v23ComEncodingLatin1()
    {
        if (!MutagenDisponivel())
            return;

        var caminho = CriarMusica();

        _writer.SalvarMetadados(new SalvarMetadadosRequest
        {
            AbsolutePath = caminho,
            Titulo = "Canção Título",
            Artista = "Artista João",
            Album = "Álbum",
            Faixa = "5",
            Ano = "2016",
            CapaBytes = CriarJpeg()
        });

        string script = """
            import sys
            from mutagen.id3 import ID3
            tag = ID3(sys.argv[1])
            print('VERSION', '-'.join(str(v) for v in tag.version))
            print('TIT2', tag['TIT2'].text[0])
            print('TPE1', tag['TPE1'].text[0])
            print('TALB', tag['TALB'].text[0])
            print('TRCK', tag['TRCK'].text[0])
            apic = tag.getall('APIC')[0]
            print('APIC_ENC', int(apic.encoding))
            print('APIC_MIME', apic.mime)
            print('APIC_TYPE', int(apic.type))
            print('APIC_LEN', len(apic.data))
            """;

        var output = ExecutarPython(script, caminho);

        Assert.Contains("VERSION 2-3-0", output);
        Assert.Contains("TIT2 Canção Título", output);
        Assert.Contains("TPE1 Artista João", output);
        Assert.Contains("TALB Álbum", output);
        Assert.Contains("TRCK 5", output);
        Assert.Contains("APIC_ENC 0", output);
        Assert.Contains("APIC_MIME image/jpeg", output);
        Assert.Contains("APIC_TYPE 3", output);
        Assert.Contains("APIC_LEN 10", output);
    }

    [Fact]
    public void SalvarMetadados_RemoverCapa_RemoveApic()
    {
        if (!MutagenDisponivel())
            return;

        var caminho = CriarMusica();
        _writer.SalvarMetadados(new SalvarMetadadosRequest
        {
            AbsolutePath = caminho,
            Titulo = "T",
            CapaBytes = CriarJpeg()
        });

        _writer.SalvarMetadados(new SalvarMetadadosRequest
        {
            AbsolutePath = caminho,
            Titulo = "T",
            RemoverCapa = true
        });

        var output = ExecutarPython("import sys; from mutagen.id3 import ID3; tag = ID3(sys.argv[1]); print('APIC_COUNT', len(tag.getall('APIC')))", caminho);
        Assert.Contains("APIC_COUNT 0", output);
    }

    private static string ExecutarPython(string script, string caminho)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("python3")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add(script);
        psi.ArgumentList.Add(caminho);

        using var processo = System.Diagnostics.Process.Start(psi)!;
        var saida = processo.StandardOutput.ReadToEnd();
        var erro = processo.StandardError.ReadToEnd();
        processo.WaitForExit();

        Assert.True(processo.ExitCode == 0, $"python3 falhou: {erro}");

        return saida;
    }
}
