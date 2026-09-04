using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

using Sonaris.Services.Arquivos;
using Sonaris.Services.Auth;
using Sonaris.Services.Download;
using Sonaris.Services.Music;
using Sonaris.Services.Playlists;
using Sonaris.Services.Search;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions((options) =>
        options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddOpenApi();

var jwtSecret = builder.Configuration["Settings:JwtSecret"]
    ?? throw new InvalidOperationException("Settings:JwtSecret não configurado.");

if (Encoding.UTF8.GetBytes(jwtSecret).Length < 16)
    throw new InvalidOperationException(
        "Settings:JwtSecret deve ter pelo menos 16 bytes (128 bits) para o algoritmo HS256. " +
        "Gere um valor forte com, por exemplo: openssl rand -base64 48");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer((options) =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Settings:JwtIssuer"] ?? "sonaris",
            ValidAudience = builder.Configuration["Settings:JwtAudience"] ?? "sonaris",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = (context) =>
            {
                if (string.IsNullOrEmpty(context.Token) &&
                    context.Request.Query.TryGetValue("token", out var token))
                {
                    context.Token = token.ToString();
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization((options) =>
{
    options.AddPolicy("AdminOnly", (policy) =>
        policy.RequireRole("Admin"));
});

builder.Services.AddSingleton<IArquivoService, ArquivoService>();
builder.Services.AddSingleton<IMusicMetadataReader, MusicMetadataReader>();
builder.Services.AddSingleton<IMusicMetadataWriter, MusicMetadataWriter>();
builder.Services.AddSingleton<IMusicSearchService, MusicSearchService>();
builder.Services.AddSingleton<MusicRepository>();
builder.Services.AddSingleton<MusicFileScanner>();
builder.Services.Configure<MusicIndexerOptions>(builder.Configuration.GetSection("Settings"));
builder.Services.AddSingleton<IPlaylistService, PlaylistService>();
builder.Services.AddSingleton<IPlaylistDownloadService, PlaylistDownloadService>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddHostedService<MusicIndexerBackgroundService>();

builder.Services.AddCors((options) =>
    options.AddPolicy("AllowAll", (policy) =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference((opt) =>
    {
        opt.Title = "Sonaris API";
        //opt.Version = "1.0.0";
        //opt.Description = "API do Sonaris - Sistema de gerenciamento de músicas e playlists.";
        opt.Theme = ScalarTheme.Moon;
        opt.Layout = ScalarLayout.Classic;
    });
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

EnsureSchema(app);
SeedAdminUser(app);

app.Run();

static void EnsureSchema(WebApplication app)
{
    var configuration = app.Services.GetRequiredService<IConfiguration>();
    var dbPath = configuration["Settings:DatabasePath"]
        ?? Path.Combine(AppContext.BaseDirectory, "sonaris.db");
    DatabaseSchema.EnsureCreated($"Data Source={dbPath}");
}

static void SeedAdminUser(WebApplication app)
{
    var userService = app.Services.GetRequiredService<IUserService>();
    var configuration = app.Services.GetRequiredService<IConfiguration>();

    var username = configuration["Settings:AdminUsername"] ?? "admin";
    var senha = configuration["Settings:AdminPassword"] ?? "admin";
    var nome = configuration["Settings:AdminNome"] ?? "Administrador";

    userService.SeedAdmin(username, senha, nome);
}
