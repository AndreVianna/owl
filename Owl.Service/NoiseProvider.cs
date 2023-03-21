using NAudio.Wave;
using System;

namespace Owl.Service;

public class NoiseProvider
{
    private readonly ISampleProvider _source;
    private const float _silenceThreshold = 0.001f;
    private const float _noiseAmplitude = 0.0001f;
    private readonly BufferedWaveProvider _waveProvider;

    private readonly byte[] _buffer;
    private readonly float[] _floatBuffer;

    public NoiseProvider(IWaveIn waveIn)
    {
        _buffer = new byte[waveIn.WaveFormat.AverageBytesPerSecond / 5];
        _waveProvider = new(waveIn.WaveFormat);
        _source = _waveProvider.ToSampleProvider();
        _floatBuffer = new float[_buffer.Length / 2];
    }

    public bool TryGenerateNoise(out byte[] buffer, out int size)
    {
        var bytesRead = _waveProvider.Read(_buffer, 0, _buffer.Length);
        var waveBuffer = new WaveBuffer(_buffer);
        var samplesRead = Read(_floatBuffer, 0, bytesRead / 2);
        if (samplesRead > 0)
        {
            buffer = waveBuffer.ByteBuffer;
            size = samplesRead * 2;
            return true;
        }

        buffer = Array.Empty<byte>();
        size = 0;
        return true;
    }

    private int Read(float[] buffer, int offset, int count)
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