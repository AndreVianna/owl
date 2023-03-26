namespace Owl.Service;

public sealed class ConsoleWindow : IConsoleWindow
{
    private readonly ILogger<ConsoleWindow> _logger;
    private readonly IConnectedConsole _connectedConsole;

    public ConsoleWindow(IConnectedConsole connectedConsole, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ConsoleWindow>();
        _connectedConsole = connectedConsole;
    }

    public Task RewriteAsync(string line) => WriteToConsoleAsync(line);

    public Task WriteLineAsync(string line) => WriteToConsoleAsync($"[F]{line}");

    private Task WriteToConsoleAsync(string line)
    {
        return _connectedConsole.SendLineAsync(line);
    }

    public async Task ShowAsync(CancellationToken cancellationToken)
    {
        await _connectedConsole.ConnectAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Display windows opened.");
    }

    private bool _disposed;
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await _connectedConsole.DisposeAsync().ConfigureAwait(false);
        _logger.LogDebug("Display window closed.");
        _disposed = true;
    }
}