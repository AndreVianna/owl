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

    public async Task RecordAsync(string text)
    {
        if (_recordingState != RecordingState.Recording) return;
        _file.AppendLine(text);
        await _window.WriteLineAsync(text);
        _logger.LogInformation("Recorded: {text}", text);
    }

    public async Task DisplayAsync(string text)
    {
        if (_recordingState != RecordingState.Recording) return;
        await _window.RewriteSameLine(text);
    }

    public async Task StopAsync()
    {
        if (_recordingState != RecordingState.Recording) return;
        _recordingState = RecordingState.Processing;
        await _window.HideAsync();
        await _file.SaveAsync();
        _logger.LogInformation("Recording stopped...");
        _logger.LogInformation("---------------------------------------------------------------------------");
        _recordingState = RecordingState.Idle;
    }

    public async Task StartAsync()
    {
        if (_recordingState != RecordingState.Idle) return;
        _recordingState = RecordingState.Processing;
        _logger.LogInformation("---------------------------------------------------------------------------");
        _logger.LogInformation("Start recording...");
        _file.Open();
        await _window.ShowAsync();
        _recordingState = RecordingState.Recording;
    }
}