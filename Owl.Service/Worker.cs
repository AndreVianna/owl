using Timer = System.Threading.Timer;

namespace Owl.Service;

public class Worker : BackgroundService
{
    private enum RecordingState
    {
        Idle,
        Recording,
        Processing
    }

    private readonly ILogger<Worker> _logger;
    private readonly StringBuilder _recognizedText;
    private readonly IConfiguration _configuration;
    private RecordingState _recordingState = RecordingState.Idle;
    private StreamWriter? _pipeStreamWriter;
    private readonly SpeechClient.StreamingRecognizeStream _stream;

    public Worker(IConfiguration configuration, ILogger<Worker> logger)
    {
        _logger = logger;
        _configuration = configuration;
        _recognizedText = new StringBuilder();
        var speechClient = CreateSpeechClient();
        _stream = speechClient.StreamingRecognize();
    }

    private SpeechClient CreateSpeechClient()
    {
        var credentialJson = GetGoogleCredentialAsJson();
        var credentials = GoogleCredential.FromJson(credentialJson).CreateScoped(SpeechClient.DefaultScopes);

        var speechClientBuilder = new SpeechClientBuilder { ChannelCredentials = credentials.ToChannelCredentials() };
        return speechClientBuilder.Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

        await InitializeSpeechRecognitionStreamAsync();
        InitializeSpeechRecognition(stoppingToken);

        await using var resetStreamTimer = new Timer(ResetStreamCallbackAsync, null, TimeSpan.FromSeconds(240), TimeSpan.FromSeconds(240));

    }

    private async Task InitializeSpeechRecognitionStreamAsync()
    {
        await _stream.WriteAsync(new StreamingRecognizeRequest
        {
            StreamingConfig = new StreamingRecognitionConfig
            {
                Config = new RecognitionConfig
                {
                    Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                    SampleRateHertz = 16000,
                    LanguageCode = "en-US",
                },
                InterimResults = true, // Enable interim results
                SingleUtterance = false
            }
        });
    }

    private async void ResetStreamCallbackAsync(object? state)
    {
        // Close the current stream and dispose of the resources
        await _stream.WriteCompleteAsync();

        // Reinitialize the speech recognition stream
        await InitializeSpeechRecognitionStreamAsync();
    }

    private void InitializeSpeechRecognition(CancellationToken cancellationToken)
    {
        // Start the microphone and streaming the audio
        var waveIn = new WaveInEvent { WaveFormat = new WaveFormat(16000, 1) };
        var waveProvider = new BufferedWaveProvider(waveIn.WaveFormat);
        var silenceDetector = new SilenceDetectingSampleProvider(waveProvider.ToSampleProvider());

        waveIn.DataAvailable += async (_, args) => await _stream
            .WriteAsync(new StreamingRecognizeRequest
            {
                AudioContent = ByteString.CopyFrom(args.Buffer, 0, args.BytesRecorded)
            });

        waveIn.StartRecording();

        cancellationToken.Register(() =>
        {
            waveIn.StopRecording();
            waveIn.Dispose();
        });

        _ = Task.Run(async () =>
        {
            var responseStream = _stream.GetResponseStream();
            var buffer = new byte[waveIn.WaveFormat.AverageBytesPerSecond / 5];
            var floatBuffer = new float[buffer.Length / 2];

            while (await responseStream.MoveNextAsync(cancellationToken))
            {
                var response = responseStream.Current;
                foreach (var result in response.Results)
                {
                    var text = result.Alternatives.OrderByDescending(i => i.Confidence).First().Transcript.Trim();
                    // Handle the recognized text
                    HandleRecognizedText(text.Trim(), result.IsFinal);
                }

                var bytesRead = waveProvider.Read(buffer, 0, buffer.Length);
                var waveBuffer = new WaveBuffer(buffer);
                var samplesRead = silenceDetector.Read(floatBuffer, 0, bytesRead / 2);

                if (samplesRead > 0)
                {
                    var audioData = ByteString.CopyFrom(waveBuffer.ByteBuffer, 0, samplesRead * 2);
                    await _stream.WriteAsync(new StreamingRecognizeRequest { AudioContent = audioData });
                }
            }
        }, cancellationToken);
    }

    private void HandleRecognizedText(string text, bool isFinal)
    {
        _logger.LogInformation("Recognized text: {text}", text);

        switch (text.ToLower())
        {
            case "start recording" when isFinal && _recordingState == RecordingState.Idle:
                _recordingState = RecordingState.Processing;
                _recognizedText.Clear();
                _logger.LogInformation("Start transcribing speech to text...");
                OpenConsole();
                _recordingState = RecordingState.Recording;
                return;
            case "stop recording" when isFinal && _recordingState == RecordingState.Recording:
                _recordingState = RecordingState.Processing;
                CloseConsole();
                SaveRecognizedText();
                _logger.LogInformation("Transcription ended...");
                _recordingState = RecordingState.Idle;
                return;
            default:
                if (_recordingState != RecordingState.Recording) return;
                if (!isFinal)
                {
                    _pipeStreamWriter?.WriteLine(text);
                    return;
                }

                _recognizedText.AppendLine(text);
                _pipeStreamWriter?.WriteLine($"[F]{text}");
                _logger.LogInformation("Transcribed: {text}", text);
                return;
        }
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
        _pipeStreamWriter = new StreamWriter(namedPipeClient) { AutoFlush = true };
    }

    private void CloseConsole()
    {
        if (_pipeStreamWriter == null) return;
        _pipeStreamWriter.Dispose();
        _pipeStreamWriter = null;
    }

    private void SaveRecognizedText()
    {
        var fileName = $"RecognizedText_{DateTimeOffset.Now:yyyyMMdd_HHmmss}.txt";
        File.WriteAllText(fileName, _recognizedText.ToString());
        _logger.LogInformation("Recognized text saved to file: {file}", fileName);
    }

    private string GetGoogleCredentialAsJson()
    {
        var section = _configuration.GetSection(nameof(GoogleCredential));
        var json = JObject.FromObject(section.GetChildren().ToDictionary(c => c.Key, c => c.Value));
        return json.ToString();
    }
}