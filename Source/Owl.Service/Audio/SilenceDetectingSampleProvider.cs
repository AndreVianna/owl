namespace Owl.Service.Audio;

public class SilenceDetectingSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly float _silenceMaximum;
    private readonly float _silenceMinimum;
    private readonly float _noiseAmplitude;
    private readonly Random _randomNumberGenerator;

    public SilenceDetectingSampleProvider(ISampleProvider source, float silenceThreshold = 0.001f, float noiseAmplitude = 0.0001f)
    {
        _source = source;
        _silenceMinimum = -silenceThreshold;
        _silenceMaximum = silenceThreshold;
        _noiseAmplitude = -noiseAmplitude;
        _randomNumberGenerator = new Random(DateTime.UtcNow.Microsecond);
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    public int Read(float[] buffer, int _, int maxSize)
    {
        var readCount = _source.Read(buffer, 0, maxSize);

        for (var i = 0; i < readCount; i++)
        {
            if (buffer[i] > _silenceMinimum && buffer[i] < _silenceMaximum)
            {
                buffer[i] += _noiseAmplitude * RandomValueBetweenMinus1And1;
            }
        }

        return readCount;
    }

    public float RandomValueBetweenMinus1And1 => (_randomNumberGenerator.NextSingle() * 2.0f) - 1.0f;
}