namespace Sonaris.Domain.DTOs.Music.Metadata.Models;

/// <summary>
/// Flags de formato do frame ID3v2 normalizadas para os bits da v2.4,
/// independentes da versão do tag (a decodificação do byte bruto fica no FrameFlagsDecoder).
/// </summary>
[Flags]
public enum FrameFormatFlags : byte
{
    None = 0,

    /// <summary>Dados comprimidos.</summary>
    Compression = 0x01,

    /// <summary>Dados criptografados.</summary>
    Encryption = 0x02,

    /// <summary>Identificador de grupo presente antes dos dados.</summary>
    GroupingIdentity = 0x04,

    /// <summary>Indicador de tamanho (4 bytes sync-safe) presente antes dos dados.</summary>
    DataLengthIndicator = 0x08,

    /// <summary>Unsynchronisation aplicada no frame.</summary>
    Unsynchronisation = 0x10
}
