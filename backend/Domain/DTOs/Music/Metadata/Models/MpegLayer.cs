namespace Sonaris.Domain.DTOs.Music.Metadata.Models;

/// <summary>
/// Camada MPEG extraída dos bits do header de áudio.
/// </summary>
internal enum MpegLayer : byte
{
    Unknown = 0,
    Layer3 = 1,
    Layer2 = 2,
    Layer1 = 3
}
