namespace Sonaris.Domain.DTOs.Music.Metadata.Models;

/// <summary>
/// Flags do tag ID3v2 (byte de flags do header de 10 bytes).
/// </summary>
[Flags]
public enum Id3v2TagFlags : byte
{
    None = 0,

    /// <summary>Unsynchronisation aplicada nos dados do tag.</summary>
    Unsync = 0x80,

    /// <summary>Presença de extended header.</summary>
    ExtendedHeader = 0x40,

    /// <summary>Indicador experimental.</summary>
    Experimental = 0x20
}
