namespace Sonaris.Domain.DTOs.Music.Metadata.Models;

/// <summary>
/// Header Xing/Info encontrado no primeiro frame de áudio MPEG.
/// </summary>
public readonly struct XingHeader(bool isInfo, uint? frameCount, uint? byteCount)
{
    internal bool IsInfo { get; } = isInfo;
    internal uint? FrameCount { get; } = frameCount;
    internal uint? ByteCount { get; } = byteCount;
}
