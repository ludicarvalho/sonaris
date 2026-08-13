using System.Buffers.Binary;

namespace Sonaris.Domain.DTOs.Music.Metadata.Parsers;

using Sonaris.Domain.DTOs.Music.Metadata.Models;
using Sonaris.Domain.DTOs.Music.Metadata.Readers;

/// <summary>
/// Lê o header Xing/Info do primeiro frame de áudio MPEG.
/// </summary>
public sealed class XingHeaderParser
{
    private const uint FramesFlag = 0x00000001;
    private const uint BytesFlag = 0x00000002;

    public XingHeader? Read(FileByteReader reader, Mp3AudioHeader audioHeader, long firstFramePosition)
    {
        int xingOffset = audioHeader.Version == MpegVersion.Mpeg1
            ? audioHeader.IsMono ? 21 : 36
            : audioHeader.IsMono ? 13 : 21;

        if (firstFramePosition + xingOffset + 8 > reader.Length)
            return null;

        reader.Position = firstFramePosition + xingOffset;

        Span<byte> identifier = stackalloc byte[4];
        reader.ReadExactly(identifier);

        bool isXing = identifier.SequenceEqual("Xing"u8);
        bool isInfo = identifier.SequenceEqual("Info"u8);

        if (!isXing && !isInfo)
            return null;

        Span<byte> flagsBuffer = stackalloc byte[4];
        reader.ReadExactly(flagsBuffer);
        uint flags = BinaryPrimitives.ReadUInt32BigEndian(flagsBuffer);

        uint? frameCount = null;
        uint? byteCount = null;

        if ((flags & FramesFlag) != 0)
        {
            Span<byte> framesBuffer = stackalloc byte[4];
            reader.ReadExactly(framesBuffer);
            frameCount = BinaryPrimitives.ReadUInt32BigEndian(framesBuffer);
        }

        if ((flags & BytesFlag) != 0)
        {
            Span<byte> bytesBuffer = stackalloc byte[4];
            reader.ReadExactly(bytesBuffer);
            byteCount = BinaryPrimitives.ReadUInt32BigEndian(bytesBuffer);
        }

        return new XingHeader(isInfo, frameCount, byteCount);
    }
}
