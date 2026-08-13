namespace Sonaris.Domain.DTOs.Music.Metadata.Frames;

using Sonaris.Domain.DTOs.Music.Metadata.Models;
using Sonaris.Domain.DTOs.Music.Metadata.Readers;

/// <summary>
/// Cria o frame tipado a partir do header lido, lendo apenas os bytes necessários.
/// </summary>
public static class FrameFactory
{
    public static IId3v2Frame Create(FileByteReader reader, Id3v2FrameHeader header)
    {
        if ((header.FormatFlags & FrameFormatFlags.Compression) != 0
            || (header.FormatFlags & FrameFormatFlags.Encryption) != 0)
        {
            reader.Skip(header.FrameSize);
            return null;
        }

        if (!ShouldRead(header.FrameId))
        {
            reader.Skip(header.FrameSize);
            return null;
        }

        int dataLength = (int)header.FrameSize;

        if ((header.FormatFlags & FrameFormatFlags.GroupingIdentity) != 0)
        {
            reader.Skip(1);
            dataLength--;
        }

        if ((header.FormatFlags & FrameFormatFlags.DataLengthIndicator) != 0)
        {
            reader.Skip(4);
            dataLength -= 4;
        }

        if (dataLength <= 0)
            return null;

        byte[] data = reader.ReadBytes(dataLength);

        return Parse(header, data);
    }

    public static bool ShouldRead(string frameId)
        => frameId == "TXXX"
        || frameId == "COMM"
        || frameId == "APIC"
        || frameId.Length == 4 && frameId[0] == 'T';

    public static IId3v2Frame Parse(Id3v2FrameHeader header, byte[] data)
    {
        ReadOnlyMemory<byte> memory = data;

        return header.FrameId switch
        {
            "TXXX" => UserTextFrame.TryParse(memory),
            "COMM" => CommentFrame.TryParse(memory),
            "APIC" => AttachedPictureFrame.TryParse(memory),
            _ => TextFrame.TryParse(header.FrameId, memory)
        };
    }
}
