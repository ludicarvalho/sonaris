using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sonaris.Controllers;

using Sonaris.Domain.DTOs.Auth;
using Sonaris.Domain.Infrastructure;
using Sonaris.Domain.Infrastructure.Response;
using Sonaris.Services.Auth;

[Route("api/Auth")]
public class AuthController(IUserService userService, IJwtTokenService jwtTokenService) : BaseController
{
    private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));

    [HttpPost("Login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        BaseResponse<LoginResponse> response = new();

        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Senha))
                throw new SonarisException("Usuário e senha são obrigatórios.");

            var user = _userService.Autenticar(request.Username, request.Senha);
            var token = _jwtTokenService.GerarToken(user);

            response.Data = new LoginResponse { Token = token, User = user };
            response.Success = true;
            response.Message = "Autenticado com sucesso.";
        }
        catch (Exception ex)
        {
            response.MontarErro(ex);
        }

        return Result(response);
    }

    [HttpGet("Me")]
    [Authorize]
    public IActionResult Me()
    {
        BaseResponse<UserDto> response = new();

        try
        {
            var id = ObterUsuarioIdAtual();
            response.Data = _userService.ObterPorId(id);
            response.Success = true;
        }
        catch (Exception ex)
        {
            response.MontarErro(ex);
        }

        return Result(response);
    }

    [HttpPost("Registrar")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult Registrar([FromBody] RegistrarUsuarioRequest request)
    {
        BaseResponse<UserDto> response = new();

        try
        {
            response.Data = _userService.Registrar(request);
            response.Success = true;
            response.Message = "Usuário criado com sucesso.";
        }
        catch (Exception ex)
        {
            response.MontarErro(ex);
        }

        return Result(response);
    }

    [HttpGet("Usuarios")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult ListarUsuarios()
    {
        BaseResponse<IEnumerable<UserDto>> response = new();

        try
        {
            response.Data = _userService.Listar();
            response.Success = true;
        }
        catch (Exception ex)
        {
            response.MontarErro(ex);
        }

        return Result(response);
    }

    [HttpPut("Usuarios/{id}/papel")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult AlterarPapel(string id, [FromBody] bool isAdmin)
    {
        BaseResponse<object> response = new();

        try
        {
            _userService.AlterarPapel(id, isAdmin);
            response.Success = true;
            response.Message = "Papel atualizado com sucesso.";
        }
        catch (Exception ex)
        {
            response.MontarErro(ex);
        }

        return Result(response);
    }

    [HttpPut("Usuarios/{id}/senha")]
    [Authorize]
    public IActionResult AlterarSenha(string id, [FromBody] string novaSenha)
    {
        BaseResponse<object> response = new();

        try
        {
            var atual = ObterUsuarioIdAtual();
            var usuario = _userService.ObterPorId(atual);
            if (!usuario.IsAdmin && atual != id)
                throw new SonarisException("Sem permissão para alterar a senha de outro usuário.");

            _userService.AlterarSenha(id, novaSenha);
            response.Success = true;
            response.Message = "Senha alterada com sucesso.";
        }
        catch (Exception ex)
        {
            response.MontarErro(ex);
        }

        return Result(response);
    }

    private string ObterUsuarioIdAtual()
        => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
           ?? User.FindFirst("sub")?.Value
           ?? throw new SonarisException("Usuário não identificado.");
}
