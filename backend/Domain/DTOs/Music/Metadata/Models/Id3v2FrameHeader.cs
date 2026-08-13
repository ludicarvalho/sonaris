namespace Sonaris.Domain.DTOs.Music.Metadata.Models;

/// <summary>
/// Header de um frame ID3v2 (10 bytes: id, tamanho e flags).
/// O tamanho é lido conforme a versão do tag (big-endian na v2.3, sync-safe na v2.4).
/// </summary>
public readonly struct Id3v2FrameHeader
{
    public const int Length = 10;

    public Id3v2FrameHeader(string frameId, uint frameSize, FrameStatusFlags statusFlags, FrameFormatFlags formatFlags)
    {
        FrameId = frameId;
        FrameSize = frameSize;
        StatusFlags = statusFlags;
        FormatFlags = formatFlags;
    }

    public string FrameId { get; }
    public uint FrameSize { get; }
    public FrameStatusFlags StatusFlags { get; }
    public FrameFormatFlags FormatFlags { get; }

    public Id3v2FrameHeader WithFrameSize(uint frameSize)
        => new(FrameId, frameSize, StatusFlags, FormatFlags);
}
