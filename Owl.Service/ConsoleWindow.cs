namespace Owl.Service;

public class ConsoleWindow : IConsoleWindow
{
    private readonly ILogger<ConsoleWindow> _logger;
    private readonly IConnectedConsole _connectedConsole;

    public ConsoleWindow(IConnectedConsole connectedConsole, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ConsoleWindow>();
        _connectedConsole = connectedConsole;
    }

    public Task RewriteSameLine(string line) => WriteToConsoleAsync(line);

    public Task WriteLineAsync(string line) => RewriteSameLine($"[F]{line}");

    private Task WriteToConsoleAsync(string line)
    {
        return _connectedConsole.SendLineAsync(line);
    }

    public async Task ShowAsync()
    {
        await _connectedConsole.ConnectAsync().ConfigureAwait(false);
        _logger.LogDebug("Display windows opened.");
    }

    public async Task HideAsync()
    {
        await _connectedConsole.DisconnectAsync().ConfigureAwait(false);
        _logger.LogDebug("Display window closed.");
    }
}