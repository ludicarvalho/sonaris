namespace Sonaris.Domain.DTOs.Music.Metadata.Frames;

using Sonaris.Domain.DTOs.Music.Metadata.Models;

/// <summary>
/// Frame COMM (comentário).
/// </summary>
internal sealed class CommentFrame : IId3v2Frame
{
    private CommentFrame(Id3TextEncoding encoding, string language, string description, string text)
    {
        Encoding = encoding;
        Language = language;
        Description = description;
        Text = text;
    }

    internal static CommentFrame TryParse(ReadOnlyMemory<byte> data)
    {
        ReadOnlySpan<byte> span = data.Span;

        if (span.Length < 4)
            return null;

        Id3TextEncoding encoding = (Id3TextEncoding)span[0];
        string language = System.Text.Encoding.ASCII.GetString(span.Slice(1, 3));
        span = span[4..];

        ReadOnlySpan<byte> description = Id3v2StringDecoder.FindField(span, encoding, out span);
        string descriptionText = Id3v2StringDecoder.Decode(description, encoding);
        string text = Id3v2StringDecoder.Decode(span, encoding);

        return new CommentFrame(encoding, language, descriptionText, text);
    }

    public string FrameId => "COMM";
    internal Id3TextEncoding Encoding { get; }
    internal string Language { get; }
    internal string Description { get; }
    internal string Text { get; }
}
