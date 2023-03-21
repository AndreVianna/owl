using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Owl.Service;

public class TranscriptionHandler
{
    private readonly ILogger _logger;
    private readonly StringBuilder _recognizedText;
    private StreamWriter? _pipeStreamWriter;

    public TranscriptionHandler(ILogger logger)
    {
        _logger = logger;
        _recognizedText = new();
    }

    public void Handle(ref RecordingState state, string text, float confidence, bool isFinal)
    {
        _logger.LogInformation("Recognized text: {text}", text);

        switch (text.ToLower())
        {
            case "start recording" when isFinal && state == RecordingState.Idle:
                state = RecordingState.Processing;
                StartTranscription();
                state = RecordingState.Recording;
                return;
            case "stop recording" when isFinal && state == RecordingState.Recording:
                state = RecordingState.Processing;
                EndTranscription();
                state = RecordingState.Idle;
                return;
            default:
                if (state != RecordingState.Recording) return;
                ProcessTranscription(text, confidence, isFinal);
                return;
        }
    }

    private void ProcessTranscription(string text, float confidence, bool isFinal)
    {
        if (!isFinal)
        {
            _pipeStreamWriter?.WriteLine(text);
            return;
        }

        _recognizedText.AppendLine(text);
        _pipeStreamWriter?.WriteLine($"[F]{text}");
        _logger.LogInformation("Transcribed: {text}; Confidence {confidence}", text, confidence);
    }

    private void EndTranscription()
    {
        CloseConsole();
        SaveRecognizedText();
        _logger.LogInformation("Transcription ended...");
    }

    private void StartTranscription()
    {
        _logger.LogInformation("Start transcribing speech to text...");
        _recognizedText.Clear();
        OpenConsole();
    }

    private void OpenConsole()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c start owl_display.exe",
            UseShellExecute = false,
            CreateNoWindow = false,
        };
        Process.Start(psi);

        var namedPipeClient = new NamedPipeClientStream(".", "OwlPipe", PipeDirection.Out);
        namedPipeClient.Connect();
        _pipeStreamWriter = new(namedPipeClient) { AutoFlush = true };
    }

    private void CloseConsole()
    {
        if (_pipeStreamWriter == null) return;
        _pipeStreamWriter.Flush();
        _pipeStreamWriter.Dispose();
        _pipeStreamWriter = null;
    }

    private void SaveRecognizedText()
    {
        Directory.CreateDirectory("recordings");
        var fileName = $"recordings/{DateTimeOffset.Now:yyyyMMdd_HHmmss}.txt";
        File.WriteAllText(fileName, _recognizedText.ToString());
        _logger.LogInformation("Recognized text saved to file: {file}", fileName);
    }
}
