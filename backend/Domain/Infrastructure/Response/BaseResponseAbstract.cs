using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Text.Json.Serialization;

namespace Sonaris.Domain.Infrastructure.Response;

/// <summary>
/// Objeto de retorno padrão para o controller.
/// </summary>
public abstract class BaseResponseAbstract(bool success = false, string message = "", object data = null, HttpStatusCode? statusCode = null)
{
    public bool Success { get; set; } = success;

    public string Message { get; set; } = message;

    public string ErrorDetails { get; set; } = "";

    [JsonIgnore, NotMapped]
    public HttpStatusCode? StatusCode { get; set; } = statusCode;

    public virtual object Data { get; set; } = data;

    public IEnumerable<ErrorModel> Errors { get; set; } = [];

    public bool IsError => !Success;
}

/// <summary>
/// Objeto de retorno padrão para o controller.
/// </summary>
public class BaseResponse<TData> : BaseResponseAbstract where TData : class
{
    public BaseResponse(bool success = false, string message = "", TData data = null, HttpStatusCode? statusCode = null)
        : base(success, message, data, statusCode) { }

    public virtual new TData Data { get; set; }
}
