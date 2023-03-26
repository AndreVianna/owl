namespace Owl.Service;

public class Recorder : IRecorder
{
    private readonly ILogger<Recorder> _logger;
    private RecordingState _recordingState = RecordingState.Idle;
    private readonly IConsoleWindow _window;
    private readonly ITimestampedFile _file;

    public Recorder(IConsoleWindow window, ITimestampedFile file, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Recorder>();
        _window = window;
        _file = file;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_recordingState != RecordingState.Idle) return;
        _recordingState = RecordingState.Starting;
        _logger.LogInformation("---------------------------------------------------------------------------");
        _logger.LogInformation("Start recording...");
        _file.Open();
        await _window.ShowAsync(cancellationToken);
        _recordingState = RecordingState.Recording;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_recordingState != RecordingState.Recording) return;
        _recordingState = RecordingState.Stopping;
        await _file.SaveAsync(cancellationToken);
        await _window.DisposeAsync();
        _logger.LogInformation("Recording stopped...");
        _logger.LogInformation("---------------------------------------------------------------------------");
        _recordingState = RecordingState.Idle;
    }

    public async Task RecordAsync(string text, CancellationToken cancellationToken)
    {
        if (_recordingState != RecordingState.Recording) return;
        _file.AppendLine(text);
        await _window.WriteLineAsync(text);
        _logger.LogInformation("Recorded: {text}", text);
    }

    public async Task IgnoreAsync(string text, CancellationToken cancellationToken)
    {
        if (_recordingState != RecordingState.Recording) return;
        await _window.RewriteAsync(text);
    }

    public void Pause()
    {
        if (_recordingState != RecordingState.Recording) return;
        _recordingState = RecordingState.Paused;
    }

    public void Resume()
    {
        if (_recordingState != RecordingState.Paused) return;
        _recordingState = RecordingState.Recording;
    }
}