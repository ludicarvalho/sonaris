namespace Sonaris.Domain.DTOs.Music.Metadata.Models;

/// <summary>
/// Versão MPEG extraída dos bits do header de áudio.
/// </summary>
internal enum MpegVersion : byte
{
    Unknown = 1,
    Mpeg25 = 0,
    Mpeg2 = 2,
    Mpeg1 = 3
}
