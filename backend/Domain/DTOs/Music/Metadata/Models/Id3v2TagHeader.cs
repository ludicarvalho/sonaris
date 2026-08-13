namespace Sonaris.Domain.DTOs.Music.Metadata.Models;

/// <summary>
/// Header do tag ID3v2 (10 bytes).
/// </summary>
public readonly struct Id3v2TagHeader(byte majorVersion, byte minorVersion, Id3v2TagFlags flags, uint tagSize, long tagStartPosition)
{
    internal const int HeaderLength = 10;

    internal static ReadOnlySpan<byte> Identifier => "ID3"u8;

    internal byte MajorVersion { get; } = majorVersion;
    internal byte MinorVersion { get; } = minorVersion;
    internal Id3v2TagFlags Flags { get; } = flags;
    internal uint TagSize { get; } = tagSize;
    internal long TagStartPosition { get; } = tagStartPosition;

    internal long TagEndPosition => TagStartPosition + HeaderLength + TagSize;

    internal bool IsId3v2 => MajorVersion is 2 or 3 or 4;
}
