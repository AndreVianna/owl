namespace Owl.Service;

public sealed class StreamHandler : IStreamHandler
{
    private SpeechClient.StreamingRecognizeStream _stream;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StreamHandler> _logger;


    public StreamHandler(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger<StreamHandler>();
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
                    //UseEnhanced = true,
                    //Model = "command_and_search",
                    //Model = "latest_short",
                    //SpeechContexts =
                    //{
                    //    new SpeechContext
                    //    {
                    //        Phrases = { "start recording", "stop recording" },
                    //    }
                    //}
                },
                InterimResults = true,
                SingleUtterance = false
            },
        });
    }

    private bool _isResetting;
    public async Task ResetStreamAsync()
    {
        _logger.LogInformation("Resetting stream...");
        try
        {
            if (_isResetting) return;
            _isResetting = true;
            _stream = CreateSpeechClient().StreamingRecognize();
            await ConfigureAsync().ConfigureAwait(false);
            _logger.LogInformation("Stream reset.");
            _isResetting = false;
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
        await _stream.WriteAsync(new() { AudioContent = audioData }).ConfigureAwait(false);
    }

    public AsyncResponseStream<StreamingRecognizeResponse> GetResponseStream()
    {
        return _stream.GetResponseStream();
    }

    private bool _isDisposed;
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        await _stream.WriteCompleteAsync().ConfigureAwait(false);
        _isDisposed = true;
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
