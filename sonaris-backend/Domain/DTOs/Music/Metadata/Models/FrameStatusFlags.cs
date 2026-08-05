namespace Sonaris.Domain.DTOs.Music.Metadata.Models;

/// <summary>
/// Flags de status do frame ID3v2 normalizadas para os bits da v2.4,
/// independentes da versão do tag (a decodificação do byte bruto fica no FrameFlagsDecoder).
/// </summary>
[Flags]
public enum FrameStatusFlags : byte
{
    None = 0,

    /// <summary>Não alterar o tag quando o frame for alterado.</summary>
    TagAlterPreservation = 0x01,

    /// <summary>Não alterar o arquivo quando o frame for alterado.</summary>
    FileAlterPreservation = 0x02,

    /// <summary>Apenas leitura.</summary>
    ReadOnly = 0x04
}
