using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Owl.Service;

public class DisplayWindow : IDisplayWindow
{
    private readonly ILogger<DisplayWindow> _logger;
    private readonly IConsoleHandler _consoleHandler;

    public DisplayWindow(IConsoleHandler consoleHandler, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DisplayWindow>();
        _consoleHandler = consoleHandler;
    }

    public Task RewriteSameLine(string line) => WriteToConsoleAsync(line);

    public Task WriteLineAsync(string line) => RewriteSameLine($"[F]{line}");

    private Task WriteToConsoleAsync(string line)
    {
        return _consoleHandler.SendLineAsync(line);
    }

    public async Task ShowAsync()
    {
        await _consoleHandler.ConnectAsync().ConfigureAwait(false);
        _logger.LogInformation("Display windows opened.");
    }

    public async Task HideAsync()
    {
        await _consoleHandler.DisconnectAsync().ConfigureAwait(false);
        _logger.LogInformation("Display window closed.");
    }
}