using System.Net;
using System.Runtime.CompilerServices;

namespace Sonaris.Domain.Infrastructure.Response;

/// <summary>
/// Extenções para o BaseResponse
/// </summary>
public static class BaseResponseExtensions
{
    private const string mensagemPadrao = "Ocorreu um erro inesperado ao processar a sua solicitação. Verifique os dados e tente novamente.";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MontarErro(this BaseResponseAbstract response, Exception ex, string genericMessage = mensagemPadrao)
    {
        response.Success = false;
        response.Data = new { };
        response.StatusCode = HttpStatusCode.BadRequest;

        if (ex is SonarisException exception)
        {
            response.Message = exception.Message;

            if (exception.InnerException != null && !string.IsNullOrWhiteSpace(exception.InnerException.Message))
                response.ErrorDetails = exception.InnerException.Message;

            if (exception.StatusCode.HasValue)
                response.StatusCode = exception.StatusCode.Value;
        }
        else
        {
            response.Message = genericMessage;
            response.ErrorDetails = ex.Message;

            if (ex.InnerException != null)
                response.ErrorDetails += " " + ex.InnerException.Message;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0034:Simplify 'default' expression", Justification = "Type Argument")]
    public static void MontarErro<T>(this BaseResponse<T> response, Exception ex, string mensagem = mensagemPadrao) where T : class
    {
        response.Success = false;
        (response as BaseResponseAbstract).MontarErro(ex, mensagem);
        response.Data = default(T);
    }
}
