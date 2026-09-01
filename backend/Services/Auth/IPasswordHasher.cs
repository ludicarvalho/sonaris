namespace Sonaris.Services.Auth;

public interface IPasswordHasher
{
    (string Hash, string Salt) HashSenha(string senha);
    bool Verificar(string senha, string hash, string salt);
}
