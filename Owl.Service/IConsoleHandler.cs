namespace Owl.Service;

public interface IConsoleHandler
{
    Task ConnectAsync();
    
    Task SendLineAsync(string line);

    Task DisconnectAsync();
}