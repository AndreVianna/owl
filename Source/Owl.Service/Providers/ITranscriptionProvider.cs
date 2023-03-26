namespace Owl.Service.Providers;

public interface ITranscriptionProvider
{
    Task StartAsync(CancellationToken cancellationToken);
}