namespace Owl.Service;

public class GoogleTranscriptionProvider : TranscriptionProvider
{
    private readonly ILogger<GoogleTranscriptionProvider> _logger;
    private readonly IConfiguration _configuration;
    private SpeechClient.StreamingRecognizeStream _stream;
    //private readonly WaveInEvent _waveIn;
    //private readonly NoiseGenerator _noiseGenerator;

    public GoogleTranscriptionProvider(IConfiguration configuration, IRecorder recorder, ILoggerFactory loggerFactory)
        : base(recorder)
    {
        _logger = loggerFactory.CreateLogger<GoogleTranscriptionProvider>();
        _configuration = configuration;
        _stream = CreateSpeechClient().StreamingRecognize();
        //_waveIn = new WaveInEvent { WaveFormat = new WaveFormat(16000, 1) };
        //_noiseGenerator = new NoiseGenerator(_waveIn, _stream, loggerFactory);
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

    public override async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await ConfigureStream();
        var waveIn = new WaveInEvent { WaveFormat = new WaveFormat(16000, 1) };
        waveIn.DataAvailable += async (_, args) => await ProcessAudioAsync(args.Buffer, args.BytesRecorded);

        cancellationToken.Register(() =>
        {
            waveIn.StopRecording();
            waveIn.Dispose();
        });

        waveIn.StartRecording();

        StartDataProcessingThread(cancellationToken);
        StartResetStreamLoop(cancellationToken);
    }

    private async Task ConfigureStream()
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


    public override async Task ProcessAudioAsync(byte[] buffer, int count)
    {
        var audio = ByteString.CopyFrom(buffer, 0, count);
        await _stream.WriteAsync(new() { AudioContent = audio }).ConfigureAwait(false);
    }

    private void StartResetStreamLoop(CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            await Task.Delay(500, cancellationToken);
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(240), cancellationToken);
                if (cancellationToken.IsCancellationRequested) return;

                await ResetStreamAsync();
            }
        }, cancellationToken);
    }

    private bool _isResetting;

    private async Task ResetStreamAsync()
    {
        _logger.LogDebug("Resetting stream...");
        try
        {
            if (_isResetting) return;
            _isResetting = true;
            _stream = CreateSpeechClient().StreamingRecognize();
            await ConfigureStream().ConfigureAwait(false);
            _logger.LogDebug("Stream reset.");
            _isResetting = false;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to reset stream.");
            throw;
        }
    }

    private void StartDataProcessingThread(CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, cancellationToken);
                _logger.LogInformation("Start listening.");
                await ProcessAudioDataAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Stopped listening.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error happened while listening.");
                throw;
            }
        }, cancellationToken);
    }

    private async Task ProcessAudioDataAsync(CancellationToken cancellationToken)
    {
        var responseStream = GetResponseStream();
        while (await responseStream.MoveNextAsync(cancellationToken))
        {
            await ProcessResponseStreamAsync(responseStream, cancellationToken);
            //await _noiseGenerator.GenerateNoiseAsync();
        }
    }

    private AsyncResponseStream<StreamingRecognizeResponse> GetResponseStream()
    {
        return _stream.GetResponseStream();
    }

    private async Task ProcessResponseStreamAsync(IAsyncEnumerator<StreamingRecognizeResponse> responseStream, CancellationToken cancellationToken)
    {
        foreach (var result in responseStream.Current.Results)
        {
            await ProcessResultAsync(result, cancellationToken);
        }
    }

    private async Task ProcessResultAsync(StreamingRecognitionResult result, CancellationToken cancellationToken)
    {
        var alternatives = result.Alternatives.OrderByDescending(i => i.Confidence).ToArray();
        foreach (var alternative in alternatives)
        {
            _logger.LogDebug("{type}: {text}, Confidence: {confidence:0.##}%", result.IsFinal ? "Final" : "Alternative", alternative.Transcript.Trim(), alternative.Confidence * 100.0);
        }

        await TranscribeAsync(alternatives.First().Transcript.Trim(), result.IsFinal, cancellationToken);
    }
}