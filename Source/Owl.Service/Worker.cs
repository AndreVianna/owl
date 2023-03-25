namespace Owl.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ITranscriptionProvider _transcriptionProvider;

    public Worker(IConfiguration configuration, IRecorder recorder, ILoggerFactory loggerFactory, ITranscriptionProvider? provider = null)
    {
        _logger = loggerFactory.CreateLogger<Worker>();
        _logger.LogDebug("Creating worker...");

        var transcriptionProviderType = configuration["TranscriptionProvider:Type"];
        _transcriptionProvider = provider ?? transcriptionProviderType switch
        {
            "Google" => new GoogleTranscriptionProvider(configuration, recorder, loggerFactory),
            "OpenAI" => new OpenAiTranscriptionProvider(configuration, recorder),
            _ => throw new NotImplementedException($"Transcription provider '{transcriptionProviderType}' is not supported.")
        };
        _logger.LogDebug("Worker created.");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Executing worker...");
        InitializeSpeechRecognition(stoppingToken);
        _logger.LogDebug("Worker executed.");
        return Task.CompletedTask;
    }

    private void InitializeSpeechRecognition(CancellationToken cancellationToken)
    {
        var waveIn = new WaveInEvent { WaveFormat = new WaveFormat(16000, 1) };

        _transcriptionProvider.InitializeAsync();
        waveIn.DataAvailable += async (_, args) => await _transcriptionProvider.ProcessAudioAsync(args.Buffer, args.BytesRecorded);

        waveIn.StartRecording();

        cancellationToken.Register(() =>
        {
            waveIn.StopRecording();
            waveIn.Dispose();
        });
    }
}