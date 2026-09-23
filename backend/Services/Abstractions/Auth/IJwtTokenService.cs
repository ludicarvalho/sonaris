using System.Security.Claims;

namespace Sonaris.Services.Abstractions.Auth;

using Sonaris.Domain.DTOs.Auth;

public interface IJwtTokenService
{
    string GerarToken(UserDto user);
    ClaimsPrincipal ValidarToken(string token);
}
