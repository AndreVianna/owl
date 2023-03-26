namespace Owl.Service;

public interface IConnectedConsole : IAsyncDisposable
{
    Task ConnectAsync(CancellationToken cancellationToken);

    Task SendLineAsync(string line);
}