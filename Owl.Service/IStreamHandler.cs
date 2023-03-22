namespace Owl.Service;

public interface IStreamHandler : IAsyncDisposable
{
    Task ConfigureAsync();
    Task ResetStreamAsync();
    Task WriteAudioDataAsync(byte[] buffer, int count);
    AsyncResponseStream<StreamingRecognizeResponse> GetResponseStream();
}
