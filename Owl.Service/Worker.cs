using System.IO;

using Timer = System.Threading.Timer;

namespace Owl.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly WaveInEvent _audioRecorder;
    private readonly StreamHandler _streamHandler;
    private readonly TranscriptionHandler _transcriptionHandler;

    private readonly NoiseProvider _noiseProvider;
    private static Timer? _resetStreamTimer;
    private RecordingState _recordingState = RecordingState.Idle;


    public Worker(IConfiguration configuration, ILogger<Worker> logger)
    {
        _logger = logger;
        try
        {
            _logger.LogInformation("Creating Worker...");

            _audioRecorder = new() { WaveFormat = new(16000, 1) };
            _noiseProvider = new(_audioRecorder);
            _streamHandler = new(configuration, _audioRecorder, logger);
            _transcriptionHandler = new(logger);

            _logger.LogInformation("Worker created.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create Worker.");
            throw;
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _audioRecorder.StopRecording();
        _audioRecorder.Dispose();
        _streamHandler.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting Worker...");

            await _streamHandler.ConfigureAsync();

            stoppingToken.Register(_audioRecorder.StopRecording);
            
            _audioRecorder.DataAvailable += async (_, args) => await _streamHandler.WriteAudioDataAsync(args.Buffer, args.BytesRecorded);
            
            StartDataProcessing(stoppingToken);
            _audioRecorder.StartRecording();

            StartResetStreamTimer();

            _logger.LogInformation("Worker started.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to execute Worker.");
            throw;
        }
    }

    private void StartResetStreamTimer()
    {
        _resetStreamTimer = new(ResetStreamAsync, null, TimeSpan.FromSeconds(240), TimeSpan.FromSeconds(240));
    }

    private async void ResetStreamAsync(object? _)
    {
        var previousState = _recordingState;
        _recordingState = RecordingState.Resetting;
        await _streamHandler.ResetStreamAsync();
        _recordingState = previousState;
    }


    private void StartDataProcessing(CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("Listening...");
                await ProcessAudioDataAsync(cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error happened while listening.");
                throw;
            }
        }, cancellationToken);
    }

    private async Task ProcessAudioDataAsync(CancellationToken cancellationToken)
    {
        var responseStream = _streamHandler.GetResponseStream();
        while (await responseStream.MoveNextAsync(cancellationToken))
        {
            await ProcessCurrentResponseStreamAsync(responseStream);
        }
    }

    private async Task ProcessCurrentResponseStreamAsync(IAsyncEnumerator<StreamingRecognizeResponse> responseStream)
    {
        foreach (var result in responseStream.Current.Results)
        {
            ProcessResult(result);
        }

        await SendNoiseAsync();
    }

    private async Task SendNoiseAsync()
    {
        if (!_noiseProvider.TryGenerateNoise(out var noiseBuffer, out var noiseBufferSize)) return;
        await _streamHandler.WriteAudioDataAsync(noiseBuffer, noiseBufferSize);
    }

    private void ProcessResult(StreamingRecognitionResult result)
    {
        var alternative = result.Alternatives.OrderByDescending(i => i.Confidence).First();
        _transcriptionHandler.Handle(ref _recordingState, alternative.Transcript.Trim(), alternative.Confidence, result.IsFinal);
    }
}