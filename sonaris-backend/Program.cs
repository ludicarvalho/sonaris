using System.Text.Json;

using Sonaris.Services.Arquivos;
using Sonaris.Services.Music;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions((options) =>
        options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IArquivoService, ArquivoService>();
builder.Services.AddSingleton<IMusicMetadataReader, MusicMetadataReader>();
builder.Services.AddSingleton<IMusicMetadataWriter, MusicMetadataWriter>();

builder.Services.AddCors((options) =>
    options.AddPolicy("AllowAll", (policy) =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
