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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Initializing worker...");
        await InitializeSpeechRecognitionAsync(stoppingToken);
        _logger.LogDebug("Worker initialized.");
    }

    private async Task InitializeSpeechRecognitionAsync(CancellationToken cancellationToken)
    {
        var waveIn = new WaveInEvent { WaveFormat = new WaveFormat(16000, 1) };
        var waveProvider = new BufferedWaveProvider(waveIn.WaveFormat);
        var silenceDetector = new SilenceDetectingSampleProvider(waveProvider.ToSampleProvider());

        await _provider.InitializeAsync(cancellationToken);
        waveIn.DataAvailable += async (_, args) => await _provider.ProcessAudioAsync(args.Buffer, args.BytesRecorded);

        waveIn.StartRecording();

        cancellationToken.Register(() =>
        {
            waveIn.StopRecording();
            waveIn.Dispose();
        });
    }
}