using System.Diagnostics;

namespace Sonaris.Services.Music;

using Sonaris.Domain.DTOs.Music;
using Sonaris.Domain.Infrastructure;

/// <summary>
/// Grava metadados ID3 e capa (APIC) em arquivos MP3 via subprocesso —
/// mid3v2 para os campos de texto e eyeD3 para a imagem embutida.
/// </summary>
public class MusicMetadataWriter : IMusicMetadataWriter
{
    private const int TimeoutMs = 120000;
    private readonly SemaphoreSlim lockSalvamento = new(1, 1);

    public void SalvarMetadados(SalvarMetadadosRequest request)
    {
        lockSalvamento.Wait();

        string caminhoCapa = null;

        try
        {
            SalvarTextos(request);

            if (request.CapaBytes is { Length: > 0 })
            {
                caminhoCapa = Path.Combine(Path.GetTempPath(), $"sonaris-capa-{Guid.NewGuid():N}.jpg");
                System.IO.File.WriteAllBytes(caminhoCapa, request.CapaBytes);

                Executar("eyeD3", ["--remove-all-images", $"--add-image={caminhoCapa}:FRONT_COVER", request.AbsolutePath]);
            }
            else if (request.RemoverCapa)
            {
                Executar("eyeD3", ["--remove-all-images", request.AbsolutePath]);
            }
        }
        catch (SonarisException) { throw; }
        catch (Exception ex)
        {
            throw new SonarisException("Erro ao tentar salvar os metadados.", ex);
        }
        finally
        {
            if (caminhoCapa != null && System.IO.File.Exists(caminhoCapa))
                System.IO.File.Delete(caminhoCapa);

            lockSalvamento.Release();
        }
    }

    private static void SalvarTextos(SalvarMetadadosRequest request)
    {
        Executar("mid3v2", [
            $"--song={request.Titulo}",
            $"--artist={request.Artista}",
            $"--album={request.Album}",
            $"--track={request.Faixa}",
            $"--year={request.Ano}",
            request.AbsolutePath
        ]);
    }

    private static void Executar(string comando, IReadOnlyList<string> argumentos)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = comando,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (string argumento in argumentos)
            startInfo.ArgumentList.Add(argumento);

        using var process = Process.Start(startInfo) ?? throw new SonarisException($"Não foi possível iniciar o comando '{comando}'.");

        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();

        if (!process.WaitForExit(TimeoutMs))
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ignora */ }
            throw new SonarisException($"O comando '{comando}' excedeu o tempo limite.");
        }

        if (process.ExitCode != 0)
        {
            string detalhe = (stderr + stdout).Trim();
            throw new SonarisException($"Falha ao executar '{comando}': {detalhe}");
        }
    }
}