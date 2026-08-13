namespace Sonaris.Domain.DTOs.Music.Metadata.Frames;

using System.Text;
using Sonaris.Domain.DTOs.Music.Metadata.Models;

/// <summary>
/// Decodificação das strings dos frames ID3v2 conforme o byte de encoding.
/// </summary>
internal static class Id3v2StringDecoder
{
    internal static string Decode(ReadOnlySpan<byte> data, Id3TextEncoding encoding)
        => encoding switch
        {
            Id3TextEncoding.Utf8 => Encoding.UTF8.GetString(data),
            Id3TextEncoding.Utf16 => DecodeUtf16(data),
            Id3TextEncoding.Utf16BigEndian => Encoding.BigEndianUnicode.GetString(data),
            _ => Encoding.Latin1.GetString(data)
        };

    private static string DecodeUtf16(ReadOnlySpan<byte> data)
    {
        if (data.Length < 2)
            return string.Empty;

        if (data[0] == 0xFF && data[1] == 0xFE)
            return Encoding.Unicode.GetString(data[2..]);

        if (data[0] == 0xFE && data[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(data[2..]);

        return Encoding.Unicode.GetString(data);
    }

    /// <summary>
    /// Separa um campo terminado por null (1 ou 2 bytes, conforme encoding).
    /// </summary>
    internal static ReadOnlySpan<byte> FindField(ReadOnlySpan<byte> data, Id3TextEncoding encoding, out ReadOnlySpan<byte> remainder)
    {
        int terminatorLength = encoding is Id3TextEncoding.Latin1 or Id3TextEncoding.Utf8 ? 1 : 2;
        int index = IndexOfTerminator(data, terminatorLength);

        if (index < 0)
        {
            remainder = default;
            return data;
        }

        remainder = data[(index + terminatorLength)..];
        return data[..index];
    }

    internal static IReadOnlyList<string> DecodeFields(ReadOnlySpan<byte> data, Id3TextEncoding encoding)
    {
        var fields = new List<string>();

        while (data.Length > 0)
        {
            ReadOnlySpan<byte> field = FindField(data, encoding, out data);
            fields.Add(Decode(field, encoding));
        }

        return fields;
    }

    private static int IndexOfTerminator(ReadOnlySpan<byte> data, int terminatorLength)
    {
        if (terminatorLength == 1)
        {
            for (int i = 0; i < data.Length; i++)
                if (data[i] == 0)
                    return i;

            return -1;
        }

        for (int i = 0; i < data.Length - 1; i++)
            if (data[i] == 0 && data[i + 1] == 0)
                return i;

        return -1;
    }
}
