namespace Owl.Service;

public interface ITranscriptionProvider
{
    Task InitializeAsync();

    Task ProcessAudioAsync(byte[] buffer, int count);
}