namespace Sonaris.Domain.Infrastructure;

/// <summary>
/// Objeto que devolve o erro de model.
/// </summary>
public class ErrorModel
{
    public string Property { get; set; }

    public IEnumerable<string> Message { get; set; }
}
