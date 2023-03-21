namespace Owl.Service;

public sealed class StreamHandler : IDisposable
{
    private readonly SpeechClient.StreamingRecognizeStream _stream;
    private readonly IConfiguration _configuration;
    private readonly ILogger<Worker> _logger;


    public StreamHandler(IConfiguration configuration, IWaveIn waveInEvent, ILogger<Worker> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _stream = CreateSpeechClient().StreamingRecognize();
    }

    public async Task ConfigureAsync()
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
    }

    public async Task ResetStreamAsync()
    {
        _logger.LogInformation("Resetting stream...");
        try
        {
            await _stream.WriteCompleteAsync();
            await ConfigureAsync();
            _logger.LogInformation("Stream reset.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to reset stream.");
            throw;
        }
    }

    public async Task WriteAudioDataAsync(byte[] buffer, int count)
    {
        var audioData = ByteString.CopyFrom(buffer, 0, count);
        await _stream.WriteAsync(new() { AudioContent = audioData });
    }

    public AsyncResponseStream<StreamingRecognizeResponse> GetResponseStream()
    {
        return _stream.GetResponseStream();
    }

    public void Dispose()
    {
        _stream.WriteCompleteAsync().Wait();
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
}
