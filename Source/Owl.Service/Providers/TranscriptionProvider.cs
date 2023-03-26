namespace Owl.Service.Providers;

internal abstract class TranscriptionProvider : ITranscriptionProvider
{
    private readonly IRecorder _recorder;
    private bool _recording;

    protected TranscriptionProvider(IRecorder recorder)
    {
        _recorder = recorder;
        SoundDetector = new WaveInEvent { WaveFormat = new WaveFormat(16000, 1) };
    }

    public WaveInEvent SoundDetector { get; }

    protected abstract Task OnBeforeStartAsync(CancellationToken cancellationToken);

    protected abstract Task OnStartedAsync(CancellationToken cancellationToken);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await OnBeforeStartAsync(cancellationToken).ConfigureAwait(false);
        SoundDetector.DataAvailable += async (_, args) => await ProcessAudioAsync(args.Buffer, args.BytesRecorded);

        cancellationToken.Register(() =>
        {
            if (_recording) SoundDetector.StopRecording();
            SoundDetector.Dispose();
            _recording = false;
        });

        SoundDetector.StartRecording();
        _recording = true;
        await OnStartedAsync(cancellationToken).ConfigureAwait(false);
    }


    protected abstract Task ProcessAudioAsync(byte[] buffer, int count);

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
                await _recorder.RecordAsync(text, isFinal, cancellationToken);
                break;
        }
    }
}