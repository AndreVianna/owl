namespace Owl.Service;

public interface IRecorder
{
    Task StartAsync();
    Task RecordAsync(string text);
    Task DisplayAsync(string text);
    Task StopAsync();
}