using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Sonaris.Domain.DTOs.Auth;

namespace Sonaris.Services.Auth;

public interface IJwtTokenService
{
    string GerarToken(UserDto user);
    ClaimsPrincipal ValidarToken(string token);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiraEmMinutos;

    public JwtTokenService(IConfiguration configuration)
    {
        _secret = configuration["Settings:JwtSecret"]
            ?? throw new InvalidOperationException("Settings:JwtSecret não configurado.");
        if (Encoding.UTF8.GetBytes(_secret).Length < 16)
            throw new InvalidOperationException(
                "Settings:JwtSecret deve ter pelo menos 16 bytes (128 bits) para o algoritmo HS256.");
        _issuer = configuration["Settings:JwtIssuer"] ?? "sonaris";
        _audience = configuration["Settings:JwtAudience"] ?? "sonaris";
        _expiraEmMinutos = int.TryParse(configuration["Settings:JwtExpiraEmMinutos"], out var m) ? m : 1440;
    }

    public string GerarToken(UserDto user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expiraEmMinutos),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal ValidarToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = _issuer,
            ValidAudience = _audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        return tokenHandler.ValidateToken(token, parameters, out _);
    }
}
