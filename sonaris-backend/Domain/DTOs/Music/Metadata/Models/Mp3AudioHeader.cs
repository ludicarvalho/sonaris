namespace Sonaris.Domain.DTOs.Music.Metadata.Models;

/// <summary>
/// Header de um frame de áudio MPEG (4 bytes).
/// </summary>
public readonly struct Mp3AudioHeader
{
    public const int Length = 4;

    private static readonly int[] Mpeg1Layer1Bitrates = [0, 32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448, 0];
    private static readonly int[] Mpeg1Layer2Bitrates = [0, 32, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384, 0];
    private static readonly int[] Mpeg1Layer3Bitrates = [0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0];
    private static readonly int[] Mpeg2Layer1Bitrates = [0, 32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256, 0];
    private static readonly int[] Mpeg2Layer23Bitrates = [0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0];

    private static readonly int[] Mpeg1SampleRates = [44100, 48000, 32000];
    private static readonly int[] Mpeg2SampleRates = [22050, 24000, 16000];
    private static readonly int[] Mpeg25SampleRates = [11025, 12000, 8000];

    public static Mp3AudioHeader? TryParse(ReadOnlySpan<byte> data)
    {
        if (data.Length < Length)
            return null;

        return TryParse(data[0], data[1], data[2], data[3]);
    }

    public static Mp3AudioHeader? TryParse(byte byte0, byte byte1, byte byte2, byte byte3)
    {
        if (byte0 != 0xFF || (byte1 & 0xE0) != 0xE0)
            return null;

        byte versionBits = (byte)(byte1 >> 3 & 0x03);
        byte layerBits = (byte)(byte1 >> 1 & 0x03);
        bool isProtected = (byte1 & 0x01) != 0;
        byte bitrateIndex = (byte)(byte2 >> 4 & 0x0F);
        byte sampleRateIndex = (byte)(byte2 >> 2 & 0x03);
        bool isPadded = (byte2 & 0x02) != 0;
        byte channelMode = (byte)(byte3 >> 6 & 0x03);

        var header = new Mp3AudioHeader(versionBits, layerBits, isProtected, bitrateIndex, sampleRateIndex, isPadded, channelMode);

        return header.IsValid ? header : null;
    }

    public Mp3AudioHeader(byte versionBits, byte layerBits, bool isProtected, byte bitrateIndex, byte sampleRateIndex, bool isPadded, byte channelMode)
    {
        VersionBits = versionBits;
        LayerBits = layerBits;
        IsProtected = isProtected;
        BitrateIndex = bitrateIndex;
        SampleRateIndex = sampleRateIndex;
        IsPadded = isPadded;
        ChannelMode = channelMode;
    }

    public byte VersionBits { get; }
    public byte LayerBits { get; }
    public bool IsProtected { get; }
    public byte BitrateIndex { get; }
    public byte SampleRateIndex { get; }
    public bool IsPadded { get; }
    public byte ChannelMode { get; }
    internal MpegVersion Version => VersionBits switch
    {
        3 => MpegVersion.Mpeg1,
        2 => MpegVersion.Mpeg2,
        0 => MpegVersion.Mpeg25,
        _ => MpegVersion.Unknown
    };

    internal MpegLayer Layer => LayerBits switch
    {
        3 => MpegLayer.Layer1,
        2 => MpegLayer.Layer2,
        1 => MpegLayer.Layer3,
        _ => MpegLayer.Unknown
    };

    internal bool IsValid
        => VersionBits != 1
        && LayerBits != 0
        && BitrateIndex is not 0 and not 15
        && SampleRateIndex != 3;

    internal bool IsMono => ChannelMode == 3;

    public int Bitrate => Layer switch
    {
        MpegLayer.Layer1 => Version == MpegVersion.Mpeg1
            ? Mpeg1Layer1Bitrates[BitrateIndex]
            : Mpeg2Layer1Bitrates[BitrateIndex],
        MpegLayer.Layer2 => Version == MpegVersion.Mpeg1
            ? Mpeg1Layer2Bitrates[BitrateIndex]
            : Mpeg2Layer23Bitrates[BitrateIndex],
        _ => Version == MpegVersion.Mpeg1
            ? Mpeg1Layer3Bitrates[BitrateIndex]
            : Mpeg2Layer23Bitrates[BitrateIndex]
    };

    internal int SampleRate => Version switch
    {
        MpegVersion.Mpeg1 => Mpeg1SampleRates[SampleRateIndex],
        MpegVersion.Mpeg2 => Mpeg2SampleRates[SampleRateIndex],
        _ => Mpeg25SampleRates[SampleRateIndex]
    };

    internal int SamplesPerFrame => Version == MpegVersion.Mpeg1
        ? Layer == MpegLayer.Layer1 ? 384 : 1152
        : Layer == MpegLayer.Layer1 ? 384 : Layer == MpegLayer.Layer2 ? 1152 : 576;

    internal long FrameLength => Layer == MpegLayer.Layer1
        ? (12 * Bitrate * 1000 / SampleRate + (IsPadded ? 1 : 0)) * 4
        : 144L * Bitrate * 1000 / SampleRate + (IsPadded ? 1 : 0);
}
