using System.Security.Claims;

namespace Sonaris.Services.Interfaces.Auth;

using Sonaris.Domain.DTOs.Auth;

public interface IJwtTokenService
{
    string GerarToken(UserDto user);
    ClaimsPrincipal ValidarToken(string token);
}
