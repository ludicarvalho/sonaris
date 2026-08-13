namespace Sonaris.Domain.DTOs.Music.Metadata.Frames;

using Sonaris.Domain.DTOs.Music.Metadata.Models;

/// <summary>
/// Frame de texto ID3v2 (T***), ex.: TIT2, TPE1, TALB, TRCK, TYER.
/// </summary>
public sealed class TextFrame : IId3v2Frame
{
    public TextFrame(string frameId, Id3TextEncoding encoding, IReadOnlyList<string> fields)
    {
        FrameId = frameId;
        Encoding = encoding;
        Fields = fields;
    }

    internal static TextFrame TryParse(string frameId, ReadOnlyMemory<byte> data)
    {
        if (data.Length < 1)
            return null;

        Id3TextEncoding encoding = (Id3TextEncoding)data.Span[0];
        IReadOnlyList<string> fields = Id3v2StringDecoder.DecodeFields(data.Span[1..], encoding);

        return new TextFrame(frameId, encoding, fields);
    }

    public string FrameId { get; }
    public Id3TextEncoding Encoding { get; }
    public IReadOnlyList<string> Fields { get; }

    public string Value => Fields.Count > 0 ? Fields[0] : string.Empty;
}
