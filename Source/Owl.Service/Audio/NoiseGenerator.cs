namespace Owl.Service.Audio;

public class NoiseGenerator
{
    private readonly SpeechClient.StreamingRecognizeStream _stream;
    private readonly byte[] _buffer;
    private readonly float[] _floatBuffer;
    private readonly BufferedWaveProvider _waveProvider;
    private readonly SilenceDetectingSampleProvider _silenceDetector;
    private readonly ILogger<NoiseGenerator> _logger;
    private CancellationTokenSource? _noiseCancellationTokenSource = null;

    public NoiseGenerator(IWaveIn waveIn, SpeechClient.StreamingRecognizeStream stream, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<NoiseGenerator>();
        _stream = stream;
        _buffer = new byte[waveIn.WaveFormat.AverageBytesPerSecond / 5];
        _waveProvider = new BufferedWaveProvider(waveIn.WaveFormat);
        _floatBuffer = new float[_buffer.Length / 2];
        _silenceDetector = new SilenceDetectingSampleProvider(_waveProvider.ToSampleProvider());
    }

    public void Start()
    {
        _noiseCancellationTokenSource = new CancellationTokenSource();
        var noiseCancellationToken = _noiseCancellationTokenSource.Token;
        Task.Run(async () =>
        {
            await Task.Delay(5000, noiseCancellationToken);
            while (!noiseCancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5000), noiseCancellationToken);
                if (noiseCancellationToken.IsCancellationRequested) return;

                await GenerateNoiseAsync();
            }
        }, noiseCancellationToken);
    }

    public void Stop()
    {
        _noiseCancellationTokenSource?.Cancel();
    }

    public async Task GenerateNoiseAsync()
    {
        _logger.LogDebug("Generating noise...");
        try
        {
            var bytesRead = _waveProvider.Read(_buffer, 0, _buffer.Length);
            var waveBuffer = new WaveBuffer(_buffer);
            var samplesRead = _silenceDetector.Read(_floatBuffer, 0, bytesRead / 2);

            if (samplesRead > 0)
            {
                var audioData = ByteString.CopyFrom(waveBuffer.ByteBuffer, 0, samplesRead * 2);
                await _stream.WriteAsync(new StreamingRecognizeRequest { AudioContent = audioData });
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to generate noise.");
        }
    }
}