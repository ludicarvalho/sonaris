namespace Sonaris.Domain.DTOs.Music.Metadata.Models;

/// <summary>
/// Integer SyncSafe usado pelo ID3v2 para o tamanho do tag.
/// Cada byte carrega apenas 7 bits de dados (bit mais significativo ignorado).
/// </summary>
internal static class SyncSafeInteger
{
    private const byte DataMask = 0x7F;

    internal static uint Read(ReadOnlySpan<byte> data)
    {
        uint value = 0;

        for (int i = 0; i < data.Length; i++)
            value = (value << 7) | (uint)(data[i] & DataMask);

        return value;
    }
}
