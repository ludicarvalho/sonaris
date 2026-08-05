namespace Sonaris.Domain.DTOs.Music.Metadata.Models;

using Sonaris.Domain.DTOs.Music;
using Sonaris.Domain.DTOs.Music.Metadata.Frames;

/// <summary>
/// Tag ID3v2 (v2.3/v2.4) já parseado, com acesso aos frames de interesse.
/// </summary>
public sealed class Id3v2Tag(Id3v2TagHeader header, IReadOnlyList<IId3v2Frame> frames)
{
    internal Id3v2TagHeader Header { get; } = header;
    internal IReadOnlyList<IId3v2Frame> Frames { get; } = frames;

    /// <summary>
    /// Posição onde o áudio MPEG inicia (logo após o tag).
    /// </summary>
    public long AudioStartPosition => Header.TagEndPosition;

    public string GetTextField(string frameId)
        => Frames.OfType<TextFrame>().FirstOrDefault(f => f.FrameId == frameId)?.Value ?? string.Empty;

    /// <summary>
    /// Retorna o ano, priorizando o frame TYER (v2.3) e caindo para o TDRC (v2.4).
    /// </summary>
    public string GetYear()
    {
        string year = GetTextField("TYER");

        if (year.Length > 0)
            return year;

        string recordingTime = GetTextField("TDRC");

        if (recordingTime.Length < 4)
            return recordingTime;

        string prefix = recordingTime[..4];

        return prefix.All(char.IsDigit) ? prefix : recordingTime;
    }

    /// <summary>
    /// Retorna a capa (frame APIC), priorizando a de capa frontal (FrontCover).
    /// </summary>
    public MusicCover? GetPictureFrame()
    {
        var pictures = Frames.OfType<AttachedPictureFrame>().ToList();

        if (pictures.Count == 0)
            return null;

        var picture = pictures.FirstOrDefault(p => p.PictureType == PictureType.FrontCover) ?? pictures[0];

        return new MusicCover(picture.MimeType, picture.Data);
    }
}
