using System.Net;

namespace Sonaris.Domain.Infrastructure;

/// <summary>
/// Exceção personalizada do sistema.
/// </summary>
public class SonarisException : Exception
{
    private const string MessagePadrao = "Ocorreu um erro inesperado ao processar a sua solicitação.";

    /// <summary>
    /// Codigo de retorno.
    /// </summary>
    public HttpStatusCode? StatusCode { get; private set; }

    /// <summary>
    /// Detalhes da exceção.
    /// </summary>
    public string Details { get; private set; } = string.Empty;

    /// <summary>
    /// Exceção simples com mensagem.
    /// </summary>
    public SonarisException()
        : this(message: MessagePadrao) { }

    /// <summary>
    /// Exceção simples com uma mensagem personalizada e também com a mensagem da exceção.
    /// </summary>
    public SonarisException(string message, Exception innerException = null)
        : this(message, details: string.Empty, innerException) { }

    /// <summary>
    /// Exceção personalizada com a mensagem, o código e a innerException
    /// </summary>
    public SonarisException(string message, HttpStatusCode codResponse, Exception innerException = null)
        : this(message, codResponse, details: string.Empty, innerException) { }

    /// <summary>
    /// Exceção personalizada com a mensagem, o código, os detalhes e a innerException
    /// </summary>
    public SonarisException(string message, HttpStatusCode codResponse, string details, Exception innerException = null)
        : this(message, details, innerException)
    {
        StatusCode = codResponse;
    }

    /// <summary>
    /// Exceção personalizada com a mensagem, os detalhes e a innerException
    /// </summary>
    public SonarisException(string message, string details, Exception innerException = null)
      : base(message ?? MessagePadrao, innerException) => Details = details;
}
