using System.Diagnostics;
using System.Text;

namespace Sonaris.Services.Music;

using Sonaris.Domain.DTOs.Music;
using Sonaris.Domain.Infrastructure;

/// <summary>
/// Grava metadados ID3 e capa (APIC) em arquivos MP3 via mutagen (Python).
/// Força ID3 v2.3 e encoding Latin-1 (ISO-8859-1) na capa para compatibilidade
/// com rádios automotivos (ex.: Duster 2016).
/// </summary>
public class MusicMetadataWriter : IMusicMetadataWriter
{
    private const int TimeoutMs = 120000;
    private readonly SemaphoreSlim lockSalvamento = new(1, 1);

    public void SalvarMetadados(SalvarMetadadosRequest request)
    {
        lockSalvamento.Wait();

        try
        {
            ExecutarPython(GerarScript(request));
        }
        catch (SonarisException) { throw; }
        catch (Exception ex)
        {
            throw new SonarisException("Erro ao tentar salvar os metadados.", ex);
        }
        finally
        {
            lockSalvamento.Release();
        }
    }

    private static string GerarScript(SalvarMetadadosRequest request)
    {
        var script = new StringBuilder();
        script.AppendLine("import sys, base64");
        script.AppendLine("from mutagen.id3 import ID3, APIC, TIT2, TPE1, TALB, TRCK, TYER");
        script.AppendLine($"path = {PythonLiteral(request.AbsolutePath)}");

        script.AppendLine("tag = ID3()");

        if (request.CapaBytes is { Length: > 0 } || request.RemoverCapa)
            script.AppendLine("tag.delall('APIC')");

        string titulo = request.Titulo ?? string.Empty;
        string artista = request.Artista ?? string.Empty;
        string album = request.Album ?? string.Empty;
        string faixa = request.Faixa ?? string.Empty;
        string ano = request.Ano ?? string.Empty;

        script.AppendLine($"tag.setall('TIT2', [TIT2(encoding=0, text={PythonList(titulo)})])");
        script.AppendLine($"tag.setall('TPE1', [TPE1(encoding=0, text={PythonList(artista)})])");
        script.AppendLine($"tag.setall('TALB', [TALB(encoding=0, text={PythonList(album)})])");

        if (faixa.Length > 0)
            script.AppendLine($"tag.setall('TRCK', [TRCK(encoding=0, text={PythonList(faixa)})])");

        if (ano.Length > 0)
            script.AppendLine($"tag.setall('TYER', [TYER(encoding=0, text={PythonList(ano)})])");

        if (request.CapaBytes is { Length: > 0 })
        {
            script.AppendLine($"data = base64.b64decode({PythonLiteral(Convert.ToBase64String(request.CapaBytes))})");
            script.AppendLine("tag.add(APIC(encoding=0, mime='image/jpeg', type=3, desc='', data=data))");
        }

        script.AppendLine("tag.save(path, v2_version=3)");
        return script.ToString();
    }

    private static void ExecutarPython(string script)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "python3",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.ArgumentList.Add("-");

        using var process = Process.Start(startInfo) ?? throw new SonarisException("Não foi possível iniciar o python3.");

        process.StandardInput.Write(script);
        process.StandardInput.Close();

        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();

        if (!process.WaitForExit(TimeoutMs))
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ignora */ }
            throw new SonarisException("O tempo de salvamento dos metadados excedeu o limite.");
        }

        if (process.ExitCode != 0)
        {
            string detalhe = (stderr + stdout).Trim();
            throw new SonarisException($"Falha ao salvar os metadados via mutagen: {detalhe}");
        }
    }

    private static string PythonLiteral(string valor)
        => "'" + valor.Replace("\\", "\\\\").Replace("'", "\\'") + "'";

    private static string PythonList(string valor)
        => "[" + PythonLiteral(valor) + "]";
}
