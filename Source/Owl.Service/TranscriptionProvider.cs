namespace Owl.Service;

public abstract class TranscriptionProvider : ITranscriptionProvider
{
    private readonly IRecorder _recorder;

    protected TranscriptionProvider(IRecorder recorder)
    {
        _recorder = recorder;
    }

    public abstract Task InitializeAsync(CancellationToken cancellationToken);

    public abstract Task ProcessAudioAsync(byte[] buffer, int count);

    protected async Task TranscribeAsync(string? text, bool isFinal, CancellationToken cancellationToken)
    {
        if (text is null) return;
        switch (text.ToLower())
        {
            case "start recording" when isFinal:
                await _recorder.StartAsync(cancellationToken);
                break;
            case "stop recording" when isFinal:
                await _recorder.StopAsync(cancellationToken);
                break;
            default:
                if (isFinal) await _recorder.RecordAsync(text, cancellationToken);
                else await _recorder.IgnoreAsync(text, cancellationToken);
                break;
        }
    }
}