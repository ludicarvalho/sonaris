using Microsoft.Extensions.Configuration;
using Moq;
using Sonaris.Domain.DTOs.Auth;
using Sonaris.Domain.Infrastructure;
using Sonaris.Services.Auth;
using Sonaris.Services.Search;
using Xunit;

namespace Sonaris.Backend.Tests.Services;

public class UserServiceTests : TestesBase
{
    private readonly string _dbPath;
    private readonly UserService _service;

    public UserServiceTests() : base("user")
    {;
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Settings:DatabasePath"]).Returns(_dbPath);

        DatabaseSchema.EnsureCreated($"Data Source={_dbPath}");
        _service = new UserService(config.Object, new PasswordHasher());
    }

    [Fact]
    public void Registrar_RetornaUsuarioComId()
    {
        var user = _service.Registrar(new RegistrarUsuarioRequest { Username = "joao", Senha = "123456", NomeExibicao = "João" });

        Assert.NotNull(user.Id);
        Assert.NotEmpty(user.Id);
        Assert.Equal("joao", user.Username);
        Assert.Equal("João", user.NomeExibicao);
        Assert.False(user.IsAdmin);
    }

    [Fact]
    public void Registrar_UsernameDuplicado_LancaSonarisException()
    {
        _service.Registrar(new RegistrarUsuarioRequest { Username = "joao", Senha = "123456" });

        Assert.Throws<SonarisException>(() =>
            _service.Registrar(new RegistrarUsuarioRequest { Username = "joao", Senha = "outra" }));
    }

    [Fact]
    public void Registrar_UsernameCaseInsensitive_Duplicado()
    {
        _service.Registrar(new RegistrarUsuarioRequest { Username = "Joao", Senha = "123456" });

        Assert.Throws<SonarisException>(() =>
            _service.Registrar(new RegistrarUsuarioRequest { Username = "joao", Senha = "outra" }));
    }

    [Fact]
    public void Autenticar_CredenciaisCorretas_RetornaUsuario()
    {
        _service.Registrar(new RegistrarUsuarioRequest { Username = "maria", Senha = "senha-secreta" });

        var user = _service.Autenticar("maria", "senha-secreta");

        Assert.NotNull(user);
        Assert.Equal("maria", user.Username);
    }

    [Fact]
    public void Autenticar_SenhaIncorreta_LancaSonarisException()
    {
        _service.Registrar(new RegistrarUsuarioRequest { Username = "maria", Senha = "senha-secreta" });

        Assert.Throws<SonarisException>(() => _service.Autenticar("maria", "errada"));
    }

    [Fact]
    public void Autenticar_UsuarioInexistente_LancaSonarisException()
    {
        Assert.Throws<SonarisException>(() => _service.Autenticar("nao-existe", "x"));
    }

    [Fact]
    public void PasswordHasher_HashDiferentePorSalt_SenhasIguaisGeramHashesDiferentes()
    {
        var hasher = new PasswordHasher();

        var (h1, s1) = hasher.HashSenha("mesma"); 
        var (h2, s2) = hasher.HashSenha("mesma");

        Assert.NotEqual(h1, h2);
        Assert.NotEqual(s1, s2);
        Assert.True(hasher.Verificar("mesma", h1, s1));
        Assert.True(hasher.Verificar("mesma", h2, s2));
        Assert.False(hasher.Verificar("outra", h1, s1));
    }

    [Fact]
    public void SeedAdmin_CriaAdminQuandoNaoExiste()
    {
        _service.SeedAdmin("admin", "admin-pass", "Administrador");

        var admin = _service.Autenticar("admin", "admin-pass");
        Assert.True(admin.IsAdmin);
    }

    [Fact]
    public void SeedAdmin_NaosobrescreveQuandoAdminJaExiste()
    {
        _service.SeedAdmin("admin", "senha1", "Administrador");
        _service.SeedAdmin("admin2", "senha2", "Outro");

        var admins = _service.Listar().Where(u => u.IsAdmin).ToList();
        Assert.Single(admins);
    }

    [Fact]
    public void AlterarPapel_AtualizaPapeL()
    {
        var user = _service.Registrar(new RegistrarUsuarioRequest { Username = "x", Senha = "123456" });

        _service.AlterarPapel(user.Id, true);

        Assert.True(_service.ObterPorId(user.Id).IsAdmin);
    }

    [Fact]
    public void AlterarSenha_ValidaNovaSenha()
    {
        var user = _service.Registrar(new RegistrarUsuarioRequest { Username = "y", Senha = "antiga" });

        _service.AlterarSenha(user.Id, "nova");

        Assert.NotNull(_service.Autenticar("y", "nova"));
        Assert.Throws<SonarisException>(() => _service.Autenticar("y", "antiga"));
    }
}
