namespace Sonaris.Domain.DTOs.Music.Metadata.Frames;

using Sonaris.Domain.DTOs.Music.Metadata.Models;

/// <summary>
/// Frame APIC (imagem anexada).
/// </summary>
internal sealed class AttachedPictureFrame : IId3v2Frame
{
    private AttachedPictureFrame(Id3TextEncoding encoding, string mimeType, PictureType pictureType, string description, byte[] data)
    {
        Encoding = encoding;
        MimeType = mimeType;
        PictureType = pictureType;
        Description = description;
        Data = data;
    }

    internal static AttachedPictureFrame TryParse(ReadOnlyMemory<byte> data)
    {
        ReadOnlySpan<byte> span = data.Span;

        if (span.Length < 1)
            return null;

        Id3TextEncoding encoding = (Id3TextEncoding)span[0];
        span = span[1..];

        int mimeEnd = span.IndexOf((byte)0);
        if (mimeEnd < 0)
            return null;

        string mimeType = System.Text.Encoding.GetEncoding("ISO-8859-1").GetString(span[..mimeEnd]);
        span = span[(mimeEnd + 1)..];

        if (span.Length < 1)
            return null;

        PictureType pictureType = (PictureType)span[0];
        span = span[1..];

        ReadOnlySpan<byte> description = Id3v2StringDecoder.FindField(span, encoding, out span);
        string descriptionText = Id3v2StringDecoder.Decode(description, encoding);

        return new AttachedPictureFrame(encoding, mimeType, pictureType, descriptionText, span.ToArray());
    }

    public string FrameId => "APIC";
    internal Id3TextEncoding Encoding { get; }
    internal string MimeType { get; }
    internal PictureType PictureType { get; }
    internal string Description { get; }
    internal byte[] Data { get; }
}
