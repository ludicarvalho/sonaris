namespace Sonaris.Domain.DTOs.Music.Metadata.Parsers;

using Sonaris.Domain.DTOs.Music.Metadata.Models;
using Sonaris.Domain.DTOs.Music.Metadata.Readers;

/// <summary>
/// Calcula a duração da música, preferindo o frame count do header Xing/Info
/// e caindo na estimativa por bitrate (como o TagLib) quando não disponível.
/// </summary>
public static class Mp3DurationCalculator
{
    public static TimeSpan? Calculate(FileByteReader reader, long firstFramePosition, Mp3AudioHeader header, XingHeader? xing)
    {
        if (xing is { } xingHeader && xingHeader.FrameCount is { } frameCount && frameCount > 0)
        {
            double seconds = frameCount * header.SamplesPerFrame / (double)header.SampleRate;
            return TimeSpan.FromSeconds(seconds);
        }

        long audioLength = reader.Length - GetId3v1Length(reader) - firstFramePosition;

        if (audioLength <= 0 || header.Bitrate <= 0)
            return null;

        double estimatedSeconds = audioLength * 8.0 / (header.Bitrate * 1000.0);

        return TimeSpan.FromSeconds(estimatedSeconds);
    }

    public static long GetId3v1Length(FileByteReader reader)
    {
        if (reader.Length < 128)
            return 0;

        long originalPosition = reader.Position;
        reader.Position = reader.Length - 128;

        Span<byte> identifier = stackalloc byte[3];
        reader.ReadExactly(identifier);

        reader.Position = originalPosition;

        return identifier.SequenceEqual("TAG"u8) ? 128 : 0;
    }
}
