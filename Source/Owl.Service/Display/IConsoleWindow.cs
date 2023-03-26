namespace Owl.Service.Display;

internal interface IConsoleWindow : IAsyncDisposable
{
    Task ShowAsync(CancellationToken cancellationToken);
    Task RewriteAsync(string line);
    Task WriteLineAsync(string line);
    void Hide();
}
