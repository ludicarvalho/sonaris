namespace Sonaris.Services.Music;

using Sonaris.Domain.DTOs.Music;
using Sonaris.Domain.DTOs.Music.Metadata.Models;
using Sonaris.Domain.DTOs.Music.Metadata.Parsers;
using Sonaris.Domain.DTOs.Music.Metadata.Readers;
using Sonaris.Domain.Infrastructure;

/// <summary>
/// Lê os metadados ID3v2 (v2.3/v2.4) e MPEG de uma música MP3 via FileStream incremental.
/// </summary>
public class MusicMetadataReader : IMusicMetadataReader
{
    private readonly Id3v2TagParser id3v2TagParser = new();
    private readonly Mp3AudioHeaderReader audioHeaderReader = new();
    private readonly XingHeaderParser xingHeaderParser = new();

    public MusicMetadata RetornarMusicaMetadata(string absolutePath)
    {
        try
        {
            using var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new FileByteReader(stream);

            Id3v2Tag tag = id3v2TagParser.Read(reader);

            long audioStartPosition = tag?.AudioStartPosition ?? 0;

            Mp3AudioHeader? audioHeader = audioHeaderReader.FindFirst(reader, audioStartPosition);

            if (audioHeader is null)
                throw new SonarisException("Não foi possível localizar o header de áudio MPEG.");

            long firstFramePosition = reader.Position;

            XingHeader? xing = xingHeaderParser.Read(reader, audioHeader.Value, firstFramePosition);

            return new MusicMetadata
            {
                Title = tag?.GetTextField("TIT2") ?? string.Empty,
                Artist = tag?.GetTextField("TPE1") ?? string.Empty,
                Album = tag?.GetTextField("TALB") ?? string.Empty,
                Track = tag?.GetTextField("TRCK") ?? string.Empty,
                Year = tag?.GetYear() ?? string.Empty,
                Duration = Mp3DurationCalculator.Calculate(reader, firstFramePosition, audioHeader.Value, xing),
                Bitrate = audioHeader.Value.Bitrate
            };
        }
        catch (SonarisException) { throw; }
        catch (Exception ex)
        {
            throw new SonarisException("Erro ao tentar ler os metadados da música.", ex);
        }
    }

    public MusicCover RetornarCapaMusica(string absolutePath)
    {
        try
        {
            using var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new FileByteReader(stream);

            Id3v2Tag tag = id3v2TagParser.Read(reader);

            return tag?.GetPictureFrame();
        }
        catch (Exception ex)
        {
            throw new SonarisException("Erro ao tentar ler a capa da música.", ex);
        }
    }
}
