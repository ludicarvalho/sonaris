namespace Sonaris.Domain.DTOs.Music.Metadata.Readers;

using Sonaris.Domain.DTOs.Music.Metadata.Models;

/// <summary>
/// Lê o header ID3v2 de 10 bytes ("ID3" + versão + flags + tamanho sync-safe).
/// </summary>
internal sealed class Id3v2TagHeaderReader
{
    internal Id3v2TagHeader? Read(FileByteReader reader)
    {
        if (reader.Length < Id3v2TagHeader.HeaderLength)
            return null;

        long startPosition = reader.Position;

        Span<byte> buffer = stackalloc byte[Id3v2TagHeader.HeaderLength];
        reader.ReadExactly(buffer);

        if (!buffer[..3].SequenceEqual(Id3v2TagHeader.Identifier))
        {
            reader.Position = startPosition;
            return null;
        }

        byte majorVersion = buffer[3];
        byte minorVersion = buffer[4];
        Id3v2TagFlags flags = (Id3v2TagFlags)buffer[5];
        uint tagSize = SyncSafeInteger.Read(buffer[6..]);

        var header = new Id3v2TagHeader(majorVersion, minorVersion, flags, tagSize, startPosition);

        if (header.TagEndPosition > reader.Length)
        {
            reader.Position = startPosition;
            return null;
        }

        return header;
    }
}
