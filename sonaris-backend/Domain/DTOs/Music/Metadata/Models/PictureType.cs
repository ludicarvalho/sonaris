namespace Sonaris.Domain.DTOs.Music.Metadata.Models;

/// <summary>
/// Tipo de imagem do frame APIC, conforme especificação ID3v2.
/// </summary>
internal enum PictureType : byte
{
    Other = 0,
    FileIcon = 1,
    OtherFileIcon = 2,
    FrontCover = 3,
    BackCover = 4,
    Leaflet = 5,
    Media = 6,
    LeadArtist = 7,
    Artist = 8,
    Conductor = 9,
    Band = 10,
    Composer = 11,
    Lyricist = 12,
    RecordingLocation = 13,
    DuringRecording = 14,
    DuringPerformance = 15,
    MovieScreenCapture = 16,
    Illustration = 17,
    BandLogo = 18,
    PublisherLogo = 19
}
