namespace Sonaris.Domain.DTOs.Music;

/// <summary>
/// Capa (imagem) embutida na tag ID3v2 de uma música MP3.
/// </summary>
public record MusicCover(string MimeType, byte[] Data);
