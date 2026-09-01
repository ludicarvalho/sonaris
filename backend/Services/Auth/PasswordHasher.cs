using System.Security.Cryptography;

namespace Sonaris.Services.Auth;

/// <summary>
/// Gera e verifica hash de senha usando PBKDF2 (Rfc2898DeriveBytes) com salt
/// aleatório por usuário. Nunca armazena a senha em texto puro.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public (string Hash, string Salt) HashSenha(string senha)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool Verificar(string senha, string hash, string salt)
    {
        byte[] saltBytes = Convert.FromBase64String(salt);
        byte[] expected = Convert.FromBase64String(hash);

        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(senha, saltBytes, Iterations, HashAlgorithmName.SHA256, KeySize);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
