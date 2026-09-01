using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Sonaris.Controllers;
using Sonaris.Domain.DTOs.Auth;
using Sonaris.Domain.Infrastructure;
using Sonaris.Domain.Infrastructure.Response;
using Sonaris.Services.Auth;
using Xunit;

namespace Sonaris.Backend.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IUserService> _userService;
    private readonly Mock<IJwtTokenService> _jwtTokenService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _userService = new Mock<IUserService>();
        _jwtTokenService = new Mock<IJwtTokenService>();
        _controller = new AuthController(_userService.Object, _jwtTokenService.Object);
    }

    [Fact]
    public void Login_CredenciaisValidas_RetornaOkComToken()
    {
        var user = new UserDto { Id = "1", Username = "admin", NomeExibicao = "Admin", IsAdmin = true };
        _userService.Setup(s => s.Autenticar("admin", "123")).Returns(user);
        _jwtTokenService.Setup(s => s.GerarToken(user)).Returns("jwt-token");

        var resultado = _controller.Login(new LoginRequest { Username = "admin", Senha = "123" });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var response = Assert.IsType<BaseResponse<LoginResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("jwt-token", response.Data.Token);
        Assert.Equal("admin", response.Data.User.Username);
    }

    [Fact]
    public void Login_CredenciaisInvalidas_RetornaBadRequest()
    {
        _userService.Setup(s => s.Autenticar("x", "y")).Throws(new SonarisException("Usuário ou senha inválidos."));

        var resultado = _controller.Login(new LoginRequest { Username = "x", Senha = "y" });

        var objectResult = Assert.IsType<ObjectResult>(resultado);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Theory]
    [InlineData("", "123")]
    [InlineData("user", "")]
    public void Login_CamposVazios_RetornaBadRequest(string username, string senha)
    {
        var resultado = _controller.Login(new LoginRequest { Username = username, Senha = senha });

        var objectResult = Assert.IsType<ObjectResult>(resultado);
        Assert.Equal(400, objectResult.StatusCode);
        var response = Assert.IsType<BaseResponse<LoginResponse>>(objectResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public void Registrar_UsuarioValido_RetornaOk()
    {
        var created = new UserDto { Id = "1", Username = "novo", IsAdmin = false };
        _userService.Setup(s => s.Registrar(It.IsAny<RegistrarUsuarioRequest>())).Returns(created);

        var resultado = _controller.Registrar(new RegistrarUsuarioRequest { Username = "novo", Senha = "123", IsAdmin = false });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var response = Assert.IsType<BaseResponse<UserDto>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("novo", response.Data.Username);
    }
}
