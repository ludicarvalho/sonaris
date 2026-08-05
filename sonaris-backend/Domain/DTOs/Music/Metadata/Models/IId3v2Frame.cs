namespace Sonaris.Domain.DTOs.Music.Metadata.Models;

/// <summary>
/// Contrato mínimo dos frames ID3v2 reconhecidos pelo parser.
/// </summary>
public interface IId3v2Frame
{
    string FrameId { get; }
}
