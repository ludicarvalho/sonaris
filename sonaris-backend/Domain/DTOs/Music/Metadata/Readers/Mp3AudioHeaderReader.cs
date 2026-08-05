namespace Sonaris.Domain.DTOs.Music.Metadata.Readers;

using Sonaris.Domain.DTOs.Music.Metadata.Models;

/// <summary>
/// Localiza o primeiro frame de áudio MPEG válido a partir de uma posição,
/// percorrendo o stream em busca do sync de 11 bits (0xFF 111...).
/// </summary>
public sealed class Mp3AudioHeaderReader
{
    public Mp3AudioHeader? FindFirst(FileByteReader reader, long startPosition)
    {
        reader.Position = startPosition;

        while (reader.Position < reader.Length - 1)
        {
            if (reader.ReadByte() != 0xFF)
                continue;

            long syncPosition = reader.Position - 1;

            if (reader.Position + Mp3AudioHeader.Length - 1 > reader.Length)
                return null;

            Span<byte> remaining = stackalloc byte[Mp3AudioHeader.Length - 1];
            reader.ReadExactly(remaining);

            Mp3AudioHeader? header = Mp3AudioHeader.TryParse(0xFF, remaining[0], remaining[1], remaining[2]);

            if (header is null)
            {
                reader.Position = syncPosition + 1;
                continue;
            }

            reader.Position = syncPosition;
            return header;
        }

        return null;
    }
}
