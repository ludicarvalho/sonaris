namespace Sonaris.Domain.DTOs.Music.Metadata.Parsers;

/// <summary>
/// Remove a unsynchronisation aplicada aos dados do tag ID3v2.
/// O padrão 0xFF 0x00 é convertido de volta para 0xFF (0x00 removido).
/// </summary>
internal static class Id3v2Unsync
{
    internal static byte[] Remove(byte[] data)
    {
        var result = new List<byte>(data.Length);

        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] == 0xFF && i + 1 < data.Length && data[i + 1] == 0x00)
                i++;

            result.Add(data[i]);
        }

        return [.. result];
    }
}
