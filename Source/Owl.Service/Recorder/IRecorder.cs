namespace Owl.Service.AudioRecorder;

public interface IRecorder
{
    Task StartAsync(CancellationToken cancellationToken);
    void Pause();
    void Resume();
    Task RecordAsync(string text, bool save, CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}