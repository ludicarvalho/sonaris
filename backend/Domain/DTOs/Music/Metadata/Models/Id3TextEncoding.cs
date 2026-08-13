namespace Sonaris.Domain.DTOs.Music.Metadata.Models;

/// <summary>
/// Tipo de encoding usado nos frames ID3v2 (primeiro byte do frame).
/// </summary>
public enum Id3TextEncoding : byte
{
    /// <summary>ISO-8859-1</summary>
    Latin1 = 0,

    /// <summary>UTF-16 com BOM</summary>
    Utf16 = 1,

    /// <summary>UTF-16BE sem BOM</summary>
    Utf16BigEndian = 2,

    /// <summary>UTF-8</summary>
    Utf8 = 3
}
