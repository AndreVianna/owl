namespace Owl.Service;

public interface IAudioRecognitionStream : IAsyncDisposable
{
    Task ConfigureAsync();
    Task ResetAsync();
    Task SendAudioAsync(byte[] buffer, int count);
    AsyncResponseStream<StreamingRecognizeResponse> GetResponseStream();
}
