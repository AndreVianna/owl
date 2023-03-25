namespace Owl.Service;

public class GoogleTranscriptionProvider : TranscriptionProvider
{
    private readonly ILogger<GoogleTranscriptionProvider> _logger;
    private readonly IConfiguration _configuration;
    private readonly SpeechClient.StreamingRecognizeStream _stream;

    public GoogleTranscriptionProvider(IConfiguration configuration, IRecorder recorder, ILoggerFactory loggerFactory)
        : base(recorder)
    {
        _logger = loggerFactory.CreateLogger<GoogleTranscriptionProvider>();
        _configuration = configuration;
        _stream = CreateSpeechClient().StreamingRecognize();
    }

    private SpeechClient CreateSpeechClient()
    {
        var credentialJson = GetGoogleCredentialAsJson();
        var credentials = GoogleCredential.FromJson(credentialJson).CreateScoped(SpeechClient.DefaultScopes);

        var speechClientBuilder = new SpeechClientBuilder { ChannelCredentials = credentials.ToChannelCredentials() };
        return speechClientBuilder.Build();
    }

    private string GetGoogleCredentialAsJson()
    {
        var section = _configuration.GetSection(nameof(GoogleCredential));
        var json = JObject.FromObject(section.GetChildren().ToDictionary(c => c.Key, c => c.Value));
        return json.ToString();
    }

    public override async Task InitializeAsync()
    {
        await _stream.WriteAsync(new()
        {
            StreamingConfig = new()
            {
                Config = new()
                {
                    Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                    SampleRateHertz = 16000,
                    LanguageCode = "en-US",
                },
                InterimResults = true,
                SingleUtterance = false
            },
        });
        StartDataProcessingThread();
    }


    public override async Task ProcessAudioAsync(byte[] buffer, int count)
    {
        var audio = ByteString.CopyFrom(buffer, 0, count);
        await _stream.WriteAsync(new() { AudioContent = audio }).ConfigureAwait(false);
    }

    private void StartDataProcessingThread()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500);
                _logger.LogInformation("Start listening.");
                await ProcessAudioDataAsync();
                _logger.LogInformation("Stopped listening.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error happened while listening.");
                throw;
            }
        });
    }

    private async Task ProcessAudioDataAsync()
    {
        var responseStream = GetResponseStream();
        while (await responseStream.MoveNextAsync())
        {
            await ProcessResponseStream(responseStream);
        }
    }

    private AsyncResponseStream<StreamingRecognizeResponse> GetResponseStream()
    {
        return _stream.GetResponseStream();
    }

    private async Task ProcessResponseStream(IAsyncEnumerator<StreamingRecognizeResponse> responseStream)
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
            _logger.LogDebug("{type}: {text}, Confidence: {confidence:0.##}%", result.IsFinal ? "Final" : "Alternative", alternative.Transcript.Trim(), alternative.Confidence * 100.0);
        }

        await TranscribeAsync(alternatives.First().Transcript.Trim(), result.IsFinal);
    }
}