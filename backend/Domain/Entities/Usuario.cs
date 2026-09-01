namespace Sonaris.Domain.Entities;

public class Usuario
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string SenhaHash { get; init; } = string.Empty;
    public string SenhaSalt { get; init; } = string.Empty;
    public string NomeExibicao { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
}
