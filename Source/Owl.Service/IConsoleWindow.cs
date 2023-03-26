namespace Owl.Service;

public interface IConsoleWindow : IAsyncDisposable
{
    Task ShowAsync(CancellationToken cancellationToken);
    Task RewriteAsync(string line);
    Task WriteLineAsync(string line);
}
