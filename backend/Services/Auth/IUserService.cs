using Sonaris.Domain.DTOs.Auth;

namespace Sonaris.Services.Auth;

public interface IUserService
{
    UserDto Autenticar(string username, string senha);
    UserDto Registrar(RegistrarUsuarioRequest request);
    UserDto ObterPorId(string id);
    List<UserDto> Listar();
    void AlterarPapel(string id, bool isAdmin);
    void AlterarSenha(string id, string novaSenha);
    void SeedAdmin(string username, string senha, string nomeExibicao);
}
