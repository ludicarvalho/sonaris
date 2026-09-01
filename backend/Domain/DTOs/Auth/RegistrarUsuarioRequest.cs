namespace Sonaris.Domain.DTOs.Auth;

public record RegistrarUsuarioRequest
{
    public string Username { get; init; } = string.Empty;
    public string Senha { get; init; } = string.Empty;
    public string NomeExibicao { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
}
