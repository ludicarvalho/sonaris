namespace Sonaris.Domain.DTOs.Auth;

public record LoginRequest
{
    public string Username { get; init; } = string.Empty;
    public string Senha { get; init; } = string.Empty;
}
