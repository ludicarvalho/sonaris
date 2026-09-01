namespace Sonaris.Domain.DTOs.Auth;

public record UserDto
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string NomeExibicao { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
}
