using Microsoft.Extensions.Options;

namespace Sonaris.Services.Search;

public class MusicIndexerBackgroundService : BackgroundService
{
    private readonly MusicFileScanner _scanner;
    private readonly MusicRepository _repository;
    private readonly ILogger<MusicIndexerBackgroundService> _logger;
    private readonly TimeSpan _initialDelay;
    private readonly TimeSpan _rescanInterval;
    private readonly string _musicPath;

    public MusicIndexerBackgroundService(
        MusicFileScanner scanner,
        MusicRepository repository,
        IOptions<MusicIndexerOptions> options,
        ILogger<MusicIndexerBackgroundService> logger)
    {
        _scanner = scanner;
        _repository = repository;
        _logger = logger;
        _initialDelay = TimeSpan.FromSeconds(options.Value.InitialDelaySeconds);
        _rescanInterval = TimeSpan.FromMinutes(options.Value.RescanIntervalMinutes);
        _musicPath = options.Value.MusicPath;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MusicIndexer iniciado. Aguardando {Delay} para o primeiro scan...", _initialDelay);
        await Task.Delay(_initialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScanAndIndexAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante o scan de músicas.");
            }

            await Task.Delay(_rescanInterval, stoppingToken);
        }
    }

    private async Task ScanAndIndexAsync(CancellationToken ct)
    {
        if (!Directory.Exists(_musicPath))
        {
            _logger.LogWarning("Diretório de músicas não encontrado: {MusicPath}", _musicPath);
            return;
        }

        var entries = _scanner.Scan(_musicPath);
        var upserted = await _repository.UpsertAndCleanAsync(entries, ct);
        await _repository.RebuildPathFtsAsync(ct);

        _logger.LogInformation("Indexação concluída: {Upserted} atualizadas", upserted);
    }
}
