namespace Owl.Service;

public interface IRecorder
{
    Task StartAsync(CancellationToken cancellationToken);
    void Pause();
    void Resume();
    Task RecordAsync(string text, CancellationToken cancellationToken);
    Task IgnoreAsync(string text, CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}