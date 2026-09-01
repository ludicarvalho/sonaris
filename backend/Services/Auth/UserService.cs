using Microsoft.Data.Sqlite;
using Sonaris.Domain.DTOs.Auth;
using Sonaris.Domain.Entities;
using Sonaris.Domain.Infrastructure;

namespace Sonaris.Services.Auth;

public class UserService : IUserService
{
    private readonly string _connectionString;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IConfiguration configuration, IPasswordHasher passwordHasher)
    {
        var dbPath = configuration["Settings:DatabasePath"]
            ?? Path.Combine(AppContext.BaseDirectory, "sonaris.db");
        _connectionString = $"Data Source={dbPath}";
        _passwordHasher = passwordHasher;
    }

    public UserDto Autenticar(string username, string senha)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, username, senha_hash, senha_salt, nome_exibicao, is_admin, created_at
            FROM usuario WHERE username = @username COLLATE NOCASE
            """;
        cmd.Parameters.AddWithValue("@username", username);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            throw new SonarisException("Usuário ou senha inválidos.");

        var hash = reader.GetString(2);
        var salt = reader.GetString(3);

        if (!_passwordHasher.Verificar(senha, hash, salt))
            throw new SonarisException("Usuário ou senha inválidos.");

        return new UserDto
        {
            Id = reader.GetString(0),
            Username = reader.GetString(1),
            NomeExibicao = reader.GetString(4),
            IsAdmin = reader.GetInt32(5) == 1,
            CreatedAt = reader.GetString(6)
        };
    }

    public UserDto Registrar(RegistrarUsuarioRequest request)
    {
        var username = request.Username?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(username))
            throw new SonarisException("Nome de usuário é obrigatório.");
        if (string.IsNullOrEmpty(request.Senha))
            throw new SonarisException("Senha é obrigatória.");

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var check = connection.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM usuario WHERE username = @username COLLATE NOCASE";
        check.Parameters.AddWithValue("@username", username);
        if (Convert.ToInt32(check.ExecuteScalar()) > 0)
            throw new SonarisException("Já existe um usuário com esse nome.");

        var (hash, salt) = _passwordHasher.HashSenha(request.Senha);

        var id = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow.ToString("o");

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO usuario (id, username, senha_hash, senha_salt, nome_exibicao, is_admin, created_at)
            VALUES (@id, @username, @hash, @salt, @nome, @isAdmin, @createdAt)
            """;
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@username", username);
        cmd.Parameters.AddWithValue("@hash", hash);
        cmd.Parameters.AddWithValue("@salt", salt);
        cmd.Parameters.AddWithValue("@nome", request.NomeExibicao?.Trim() ?? string.Empty);
        cmd.Parameters.AddWithValue("@isAdmin", request.IsAdmin ? 1 : 0);
        cmd.Parameters.AddWithValue("@createdAt", now);
        cmd.ExecuteNonQuery();

        return new UserDto
        {
            Id = id,
            Username = username,
            NomeExibicao = request.NomeExibicao?.Trim() ?? string.Empty,
            IsAdmin = request.IsAdmin,
            CreatedAt = now
        };
    }

    public UserDto ObterPorId(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, username, nome_exibicao, is_admin, created_at
            FROM usuario WHERE id = @id
            """;
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            throw new SonarisException("Usuário não encontrado.");

        return new UserDto
        {
            Id = reader.GetString(0),
            Username = reader.GetString(1),
            NomeExibicao = reader.GetString(2),
            IsAdmin = reader.GetInt32(3) == 1,
            CreatedAt = reader.GetString(4)
        };
    }

    public List<UserDto> Listar()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id, username, nome_exibicao, is_admin, created_at FROM usuario ORDER BY username";

        var usuarios = new List<UserDto>();
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                usuarios.Add(new UserDto
                {
                    Id = reader.GetString(0),
                    Username = reader.GetString(1),
                    NomeExibicao = reader.GetString(2),
                    IsAdmin = reader.GetInt32(3) == 1,
                    CreatedAt = reader.GetString(4)
                });
            }
        }
        return usuarios;
    }

    public void AlterarPapel(string id, bool isAdmin)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE usuario SET is_admin = @isAdmin WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@isAdmin", isAdmin ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public void AlterarSenha(string id, string novaSenha)
    {
        if (string.IsNullOrEmpty(novaSenha))
            throw new SonarisException("Nova senha é obrigatória.");

        var (hash, salt) = _passwordHasher.HashSenha(novaSenha);

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE usuario SET senha_hash = @hash, senha_salt = @salt WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@hash", hash);
        cmd.Parameters.AddWithValue("@salt", salt);
        cmd.ExecuteNonQuery();
    }

    public void SeedAdmin(string username, string senha, string nomeExibicao)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var check = connection.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM usuario WHERE is_admin = 1";
        if (Convert.ToInt32(check.ExecuteScalar()) > 0)
            return;

        var (hash, salt) = _passwordHasher.HashSenha(senha);

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO usuario (id, username, senha_hash, senha_salt, nome_exibicao, is_admin, created_at)
            VALUES (@id, @username, @hash, @salt, @nome, 1, @createdAt)
            ON CONFLICT(username) DO UPDATE SET is_admin = 1
            """;
        cmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
        cmd.Parameters.AddWithValue("@username", username);
        cmd.Parameters.AddWithValue("@hash", hash);
        cmd.Parameters.AddWithValue("@salt", salt);
        cmd.Parameters.AddWithValue("@nome", nomeExibicao);
        cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
    }
}
