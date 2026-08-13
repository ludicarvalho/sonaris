using System.Buffers.Binary;
using System.Text;

namespace Sonaris.Domain.DTOs.Music.Metadata.Parsers;

using Sonaris.Domain.DTOs.Music.Metadata.Frames;
using Sonaris.Domain.DTOs.Music.Metadata.Models;
using Sonaris.Domain.DTOs.Music.Metadata.Readers;

/// <summary>
/// Parseia o tag ID3v2 (v2.3 e v2.4): header, extended header, frames e padding.
/// </summary>
public sealed class Id3v2TagParser
{
    private readonly Id3v2TagHeaderReader tagHeaderReader = new();

    public Id3v2Tag Read(FileByteReader reader)
    {
        Id3v2TagHeader? header = tagHeaderReader.Read(reader);

        if (header is null)
            return null;

        if (header.Value.MajorVersion is not (3 or 4))
        {
            reader.Position = header.Value.TagStartPosition;
            return null;
        }

        List<IId3v2Frame> frames = (header.Value.Flags & Id3v2TagFlags.Unsync) != 0
            ? ReadWithUnsync(reader, header.Value)
            : ReadStreaming(reader, header.Value);

        return new Id3v2Tag(header.Value, frames);
    }

    private static List<IId3v2Frame> ReadWithUnsync(FileByteReader reader, Id3v2TagHeader header)
    {
        byte[] tagData = reader.ReadBytes((int)header.TagSize);
        byte[] cleanData = Id3v2Unsync.Remove(tagData);

        using MemoryStream bufferStream = new(cleanData, writable: false);
        using FileByteReader bufferedReader = new(bufferStream);

        return ReadFrames(bufferedReader, bufferedReader.Length, header.Flags, header.MajorVersion);
    }

    private static List<IId3v2Frame> ReadStreaming(FileByteReader reader, Id3v2TagHeader header)
    {
        long framesEnd = reader.Position + header.TagSize;

        return ReadFrames(reader, framesEnd, header.Flags, header.MajorVersion);
    }

    private static List<IId3v2Frame> ReadFrames(FileByteReader reader, long framesEnd, Id3v2TagFlags flags, byte majorVersion)
    {
        var frames = new List<IId3v2Frame>();

        if ((flags & Id3v2TagFlags.ExtendedHeader) != 0)
            SkipExtendedHeader(reader, majorVersion);

        while (reader.Position + Id3v2FrameHeader.Length <= framesEnd)
        {
            Id3v2FrameHeader? frameHeader = ReadFrameHeader(reader, majorVersion);

            if (frameHeader is null)
                break;

            long available = Math.Max(0, framesEnd - reader.Position);
            uint safeSize = (uint)Math.Min(frameHeader.Value.FrameSize, available);

            IId3v2Frame frame = FrameFactory.Create(reader, frameHeader.Value.WithFrameSize(safeSize));

            if (frame is not null)
                frames.Add(frame);
        }

        return frames;
    }

    private static void SkipExtendedHeader(FileByteReader reader, byte majorVersion)
    {
        Span<byte> sizeBuffer = stackalloc byte[4];
        reader.ReadExactly(sizeBuffer);

        uint extendedSize = majorVersion >= 4
            ? SyncSafeInteger.Read(sizeBuffer)
            : BinaryPrimitives.ReadUInt32BigEndian(sizeBuffer);

        long remaining = majorVersion >= 4 ? (long)extendedSize - 4 : extendedSize;
        reader.Skip(Math.Max(0, remaining));
    }

    private static Id3v2FrameHeader? ReadFrameHeader(FileByteReader reader, byte majorVersion)
    {
        Span<byte> buffer = stackalloc byte[Id3v2FrameHeader.Length];
        reader.ReadExactly(buffer);

        if (buffer[0] == 0 && buffer[1] == 0 && buffer[2] == 0 && buffer[3] == 0)
            return null;

        string frameId = Encoding.ASCII.GetString(buffer[..4]);
        uint frameSize = majorVersion >= 4
            ? SyncSafeInteger.Read(buffer[4..8])
            : BinaryPrimitives.ReadUInt32BigEndian(buffer[4..8]);
        FrameStatusFlags statusFlags = FrameFlagsDecoder.DecodeStatusFlags(buffer[8], majorVersion);
        FrameFormatFlags formatFlags = FrameFlagsDecoder.DecodeFormatFlags(buffer[9], majorVersion);

        return new Id3v2FrameHeader(frameId, frameSize, statusFlags, formatFlags);
    }
}
