namespace Owl.Service;

public interface IConnectedConsole
{
    Task ConnectAsync();

    Task SendLineAsync(string line);

    Task DisconnectAsync();
}