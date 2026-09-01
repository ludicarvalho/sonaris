using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Moq;
using Sonaris.Domain.DTOs.Auth;
using Sonaris.Services.Auth;
using Xunit;

namespace Sonaris.Backend.Tests.Services;

public class JwtTokenServiceTests
{
    private static JwtTokenService CriarServico(string secret = "chave-secreta-de-teste-com-mais-de-32-characters-longo")
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Settings:JwtSecret"]).Returns(secret);
        config.Setup(c => c["Settings:JwtIssuer"]).Returns("sonaris");
        config.Setup(c => c["Settings:JwtAudience"]).Returns("sonaris");
        return new JwtTokenService(config.Object);
    }

    [Fact]
    public void GerarToken_RetornaTokenValido()
    {
        var servico = CriarServico();

        var token = servico.GerarToken(new UserDto { Id = "1", Username = "admin", IsAdmin = true });

        Assert.False(string.IsNullOrEmpty(token));
        Assert.Contains(".", token); // JWT tem 3 partes
    }

    [Fact]
    public void ValidarToken_ClaimDeSubEUsernameCorretos()
    {
        var servico = CriarServico();

        var token = servico.GerarToken(new UserDto { Id = "abc123", Username = "maria", IsAdmin = true });

        var principal = servico.ValidarToken(token);

        Assert.Equal("abc123", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("maria", principal.FindFirst(ClaimTypes.Name)?.Value);
        Assert.Equal("Admin", principal.FindFirst(ClaimTypes.Role)?.Value);
    }

    [Fact]
    public void ValidarToken_AdminFalse_ClaimRoleUser()
    {
        var servico = CriarServico();

        var token = servico.GerarToken(new UserDto { Id = "1", Username = "u", IsAdmin = false });

        var principal = servico.ValidarToken(token);

        Assert.Equal("User", principal.FindFirst(ClaimTypes.Role)?.Value);
    }

    [Fact]
    public void ValidarToken_TokenTrocado_LancaExcecao()
    {
        var servico = CriarServico();
        var outro = CriarServico("outra-chave-secreta-diferente-longa-suficiente-ok");

        var token = servico.GerarToken(new UserDto { Id = "1", Username = "u" });

        Assert.ThrowsAny<Exception>(() => outro.ValidarToken(token));
    }

    [Fact]
    public void Construtor_SemSecret_LancaInvalidOperationException()
    {
        var config = new Mock<IConfiguration>();
        Assert.Throws<InvalidOperationException>(() => new JwtTokenService(config.Object));
    }

    [Fact]
    public void Construtor_SecretCurta_LancaInvalidOperationException()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Settings:JwtSecret"]).Returns("curta");
        Assert.Throws<InvalidOperationException>(() => new JwtTokenService(config.Object));
    }
}
