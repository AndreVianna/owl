using Timer = System.Threading.Timer;

namespace Owl.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly WaveInEvent _audioRecorder;
    private readonly StreamHandler _streamHandler;
    private readonly TranscriptionHandler _transcriptionHandler;

    private static Timer? _resetStreamTimer;

    public Worker(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Worker>();
        try
        {
            _logger.LogInformation("""

                                    ---------------------------------------------------------------------------
                                    Creating Worker...
                                    ---------------------------------------------------------------------------
                                    """);

            _audioRecorder = new() { WaveFormat = new(16000, 1) };
            _streamHandler = new(configuration, loggerFactory);
            var consoleConnection = new ConsoleHandler(loggerFactory);
            var displayWindow = new DisplayWindow(consoleConnection, loggerFactory);
            var fileHandler = new TimestampedFileHandler("recordings", loggerFactory);
            _transcriptionHandler = new(displayWindow, fileHandler, loggerFactory);

            _logger.LogInformation("Worker created.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create Worker.");
            throw;
        }
    }

    private bool _isDisposed;
    public override void Dispose()
    {
        if (_isDisposed) return;
        base.Dispose();
        _resetStreamTimer?.Dispose();
        _audioRecorder.StopRecording();
        _audioRecorder.Dispose();
        _streamHandler.DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        _isDisposed = true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting Worker...");
            await _streamHandler.ConfigureAsync();
            _audioRecorder.DataAvailable += async (_, args) => await _streamHandler.WriteAudioDataAsync(args.Buffer, args.BytesRecorded);

            StartDataProcessingThread(stoppingToken);
            _audioRecorder.StartRecording();

            _logger.LogInformation("Worker started.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to start Worker.");
            throw;
        }

        _ = ResetStreamLoop(stoppingToken);
    }

    private async Task ResetStreamLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(240), cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;

            await ResetAsync();
        }
    }

    private async Task ResetAsync()
    {
        try
        {
            _logger.LogInformation("Resetting stream...");
            _audioRecorder.StopRecording();
            var previousState = _transcriptionHandler.State;
            _transcriptionHandler.State = RecordingState.Resetting;
            await _streamHandler.ResetStreamAsync();
            _transcriptionHandler.State = previousState;
            _audioRecorder.StartRecording();
            _logger.LogInformation("Reset ended.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while resetting the stream.");
        }
    }


        private void StartDataProcessingThread(CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("Listening...");
                await ProcessAudioDataAsync(cancellationToken);
                _logger.LogInformation("Stopped listening.");
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
            await ProcessCurrentResponseStream(responseStream);
        }
    }

    private async Task ProcessCurrentResponseStream(IAsyncEnumerator<StreamingRecognizeResponse> responseStream)
    {
        foreach (var result in responseStream.Current.Results)
        {
            await ProcessResult(result);
        }
    }

    private async Task ProcessResult(StreamingRecognitionResult result)
    {
        var alternatives = result.Alternatives.OrderByDescending(i => i.Confidence).ToArray();
        foreach (var alternative in alternatives)
        {
            _logger.LogInformation("{type}: {text}, Confidence: {confidence:0.##}%", result.IsFinal ? "Final" : "Alternative", alternative.Transcript.Trim(), alternative.Confidence * 100.0);
        }

        await _transcriptionHandler.HandleAsync(alternatives.First().Transcript.Trim(), result.IsFinal);
    }
}
