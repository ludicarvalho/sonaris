namespace Sonaris.Domain.DTOs.Music.Metadata.Frames;

using Sonaris.Domain.DTOs.Music.Metadata.Models;

/// <summary>
/// Frame TXXX (texto definido pelo usuário).
/// </summary>
public sealed class UserTextFrame : IId3v2Frame
{
    public UserTextFrame(Id3TextEncoding encoding, string description, string value)
    {
        Encoding = encoding;
        Description = description;
        Value = value;
    }

    internal static UserTextFrame TryParse(ReadOnlyMemory<byte> data)
    {
        ReadOnlySpan<byte> span = data.Span;

        if (span.Length < 1)
            return null;

        Id3TextEncoding encoding = (Id3TextEncoding)span[0];
        span = span[1..];

        ReadOnlySpan<byte> description = Id3v2StringDecoder.FindField(span, encoding, out span);
        string descriptionText = Id3v2StringDecoder.Decode(description, encoding);
        string value = Id3v2StringDecoder.Decode(span, encoding);

        return new UserTextFrame(encoding, descriptionText, value);
    }

    public string FrameId => "TXXX";
    public Id3TextEncoding Encoding { get; }
    public string Description { get; }
    public string Value { get; }
}
