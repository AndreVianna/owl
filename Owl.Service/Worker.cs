namespace Owl.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly StringBuilder _recognizedText;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        _recognizedText = new StringBuilder();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

        if (!TryInitializeSpeechRecognition(stoppingToken)) return;

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "It will be used only on Windows.")]
    private bool TryInitializeSpeechRecognition(CancellationToken cancellationToken)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.LogWarning("SpeechRecognitionEngine is only supported on Windows.");
            return false;
        }

        using var recognizer = new SpeechRecognitionEngine();

        // Configure the input device (microphone)
        recognizer.SetInputToDefaultAudioDevice();

        // Configure the speech recognition grammar
        var startStopGrammar = new GrammarBuilder();
        startStopGrammar.Append(new Choices("start message", "send message"));
        var grammar = new Grammar(startStopGrammar);
        recognizer.LoadGrammar(grammar);

        recognizer.SpeechRecognized += (_, e) =>
        {
            _logger.LogInformation("Recognized text: {text}", e.Result.Text);

            switch (e.Result.Text)
            {
                case "start message":
                    _recognizedText.Clear();
                    return;
                case "send message":
                    SaveRecognizedText();
                    return;
                default:
                    _recognizedText.AppendLine(e.Result.Text);
                    return;
            }
        };

        recognizer.SpeechRecognitionRejected += (_, e) =>
            _logger.LogInformation("Rejected text: {text}", e.Result.Text);

        recognizer.RecognizeAsync(RecognizeMode.Multiple);
        cancellationToken.Register(recognizer.RecognizeAsyncCancel);
        return true;
    }

    private void SaveRecognizedText()
    {
        var fileName = $"RecognizedText_{DateTimeOffset.Now:yyyyMMdd_HHmmss}.txt";
        File.WriteAllText(fileName, _recognizedText.ToString());
        _logger.LogInformation("Recognized text saved to file: {file}", fileName);
    }
}