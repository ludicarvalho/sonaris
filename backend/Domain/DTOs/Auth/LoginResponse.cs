namespace Sonaris.Domain.DTOs.Auth;

public record LoginResponse
{
    public string Token { get; init; } = string.Empty;
    public UserDto User { get; init; }
}
