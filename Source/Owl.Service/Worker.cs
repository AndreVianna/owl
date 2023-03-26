using Owl.Service.Providers;

namespace Owl.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ITranscriptionProvider _provider;

    public Worker(ILoggerFactory loggerFactory, ITranscriptionProvider provider)
    {
        _logger = loggerFactory.CreateLogger<Worker>();
        _provider = provider;
        _logger.LogDebug("Worker created.");
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Initializing worker...");
        await _provider.StartAsync(cancellationToken);
        _logger.LogDebug("Worker initialized.");
    }
}