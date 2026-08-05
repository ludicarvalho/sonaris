namespace Sonaris.Domain.DTOs.Music.Metadata.Models;

/// <summary>
/// Decodifica os bytes de flags do frame ID3v2 conforme a versão do tag,
/// normalizando para o enum independente de versão.
/// </summary>
internal static class FrameFlagsDecoder
{
    internal static FrameStatusFlags DecodeStatusFlags(byte raw, byte majorVersion)
        => majorVersion >= 4
            ? DecodeV4StatusFlags(raw)
            : DecodeV3StatusFlags(raw);

    internal static FrameFormatFlags DecodeFormatFlags(byte raw, byte majorVersion)
        => majorVersion >= 4
            ? DecodeV4FormatFlags(raw)
            : DecodeV3FormatFlags(raw);

    private static FrameStatusFlags DecodeV3StatusFlags(byte raw)
    {
        var flags = FrameStatusFlags.None;

        if ((raw & 0x80) != 0) flags |= FrameStatusFlags.TagAlterPreservation;
        if ((raw & 0x40) != 0) flags |= FrameStatusFlags.FileAlterPreservation;
        if ((raw & 0x20) != 0) flags |= FrameStatusFlags.ReadOnly;

        return flags;
    }

    private static FrameStatusFlags DecodeV4StatusFlags(byte raw)
    {
        var flags = FrameStatusFlags.None;

        if ((raw & 0x40) != 0) flags |= FrameStatusFlags.TagAlterPreservation;
        if ((raw & 0x20) != 0) flags |= FrameStatusFlags.FileAlterPreservation;
        if ((raw & 0x10) != 0) flags |= FrameStatusFlags.ReadOnly;

        return flags;
    }

    private static FrameFormatFlags DecodeV3FormatFlags(byte raw)
    {
        var flags = FrameFormatFlags.None;

        if ((raw & 0x80) != 0) flags |= FrameFormatFlags.Compression;
        if ((raw & 0x40) != 0) flags |= FrameFormatFlags.Encryption;
        if ((raw & 0x20) != 0) flags |= FrameFormatFlags.GroupingIdentity;

        return flags;
    }

    private static FrameFormatFlags DecodeV4FormatFlags(byte raw)
    {
        var flags = FrameFormatFlags.None;

        if ((raw & 0x40) != 0) flags |= FrameFormatFlags.GroupingIdentity;
        if ((raw & 0x08) != 0) flags |= FrameFormatFlags.Compression;
        if ((raw & 0x04) != 0) flags |= FrameFormatFlags.Encryption;
        if ((raw & 0x02) != 0) flags |= FrameFormatFlags.Unsynchronisation;
        if ((raw & 0x01) != 0) flags |= FrameFormatFlags.DataLengthIndicator;

        return flags;
    }
}
