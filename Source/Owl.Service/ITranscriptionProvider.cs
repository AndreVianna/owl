namespace Owl.Service;

public interface ITranscriptionProvider
{
    Task InitializeAsync(CancellationToken cancellationToken);

    Task ProcessAudioAsync(byte[] buffer, int count);
}