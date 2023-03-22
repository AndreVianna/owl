using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Owl.Service;

public class TranscriptionHandler
{
    private readonly ILogger<TranscriptionHandler> _logger;
    private readonly IDisplayWindow _displayWindow;
    private readonly ITimestampedFileHandler _timestampedFileHandler;

    public TranscriptionHandler(IDisplayWindow displayWindow, ITimestampedFileHandler timestampedFileHandler, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<TranscriptionHandler>();
        _timestampedFileHandler = timestampedFileHandler;
        _displayWindow = displayWindow;
        State = RecordingState.Idle;
    }

    public RecordingState State { get; set; }

    public async Task HandleAsync(string text, bool isFinal)
    {
        _logger.LogInformation("---------------------------------------------------------------------------");

        var command = GetCommand(text, isFinal);
        switch (command)
        {
            case TranscriptionCommand.StartRecording:
                _logger.LogInformation("Starting Transcription...");
                State = RecordingState.Processing;
                await StartTranscriptionAsync().ConfigureAwait(false);
                State = RecordingState.Recording;
                break;
            case TranscriptionCommand.StopRecording:
                State = RecordingState.Processing;
                await EndTranscriptionAsync().ConfigureAwait(false);
                State = RecordingState.Idle;
                _logger.LogInformation("Transcription ended.");
                break;
            case TranscriptionCommand.ProcessInput:
                _logger.LogInformation("Processing input...");
                await ProcessInputAsync(text, isFinal).ConfigureAwait(false);
                break;
        }

        _logger.LogInformation("---------------------------------------------------------------------------");
    }

    private TranscriptionCommand GetCommand(string text, bool isFinal)
    {
        return text.ToLower() switch
        {
            "start recording" when isFinal && State == RecordingState.Idle => TranscriptionCommand.StartRecording,
            "stop recording" when isFinal && State == RecordingState.Recording => TranscriptionCommand.StopRecording,
            _ when State == RecordingState.Recording => TranscriptionCommand.ProcessInput,
            _ => TranscriptionCommand.None
        };
    }

    private async Task StartTranscriptionAsync()
    {
        await _displayWindow.ShowAsync().ConfigureAwait(false);
        await _timestampedFileHandler.CreateAsync().ConfigureAwait(false);
    }

    private async Task ProcessInputAsync(string text, bool isFinal)
    {
        if (isFinal)
        {
            await _displayWindow.WriteLineAsync(text).ConfigureAwait(false);
            await _timestampedFileHandler.SaveLineAsync(text).ConfigureAwait(false);
            return;
        }

        await _displayWindow.RewriteSameLine(text).ConfigureAwait(false);
    }

    private async Task EndTranscriptionAsync()
    {
        await _displayWindow.HideAsync().ConfigureAwait(false);
        await _timestampedFileHandler.CloseAsync().ConfigureAwait(false);
        _logger.LogInformation("Transcription ended.");
    }
}

public enum TranscriptionCommand
{
    None,
    StartRecording,
    StopRecording,
    ProcessInput
}
