namespace Owl.Service;

public abstract class TranscriptionProvider : ITranscriptionProvider
{
    private readonly IRecorder _recorder;

    protected TranscriptionProvider(IRecorder recorder)
    {
        _recorder = recorder;
    }

    public abstract Task InitializeAsync();

    public abstract Task ProcessAudioAsync(byte[] buffer, int count);

    protected async Task TranscribeAsync(string? text, bool isFinal)
    {
        if (text is null) return;
        switch (text.ToLower())
        {
            case "start recording" when isFinal:
                await _recorder.StartAsync();
                break;
            case "stop recording" when isFinal:
                await _recorder.StopAsync();
                break;
            default:
                if (isFinal) await _recorder.RecordAsync(text);
                else await _recorder.DisplayAsync(text);
                break;
        }
    }
}