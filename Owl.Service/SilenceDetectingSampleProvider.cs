namespace Owl.Service;

public class SilenceDetectingSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly float _silenceThreshold;
    private readonly float _noiseAmplitude;

    public SilenceDetectingSampleProvider(ISampleProvider source, float silenceThreshold = 0.001f, float noiseAmplitude = 0.0001f)
    {
        _source = source;
        _silenceThreshold = silenceThreshold;
        _noiseAmplitude = noiseAmplitude;
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        var samplesRead = _source.Read(buffer, offset, count);

        for (var i = 0; i < samplesRead; i++)
        {
            if (Math.Abs(buffer[offset + i]) < _silenceThreshold)
            {
                buffer[offset + i] += (float)(_noiseAmplitude * ((new Random().NextDouble() * 2.0) - 1.0));
            }
        }

        return samplesRead;
    }
}