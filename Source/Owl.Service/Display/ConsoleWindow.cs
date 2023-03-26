namespace Owl.Service.Display;

internal sealed class ConsoleWindow : IConsoleWindow
{
    private readonly ILogger<ConsoleWindow> _logger;
    private NamedPipeClientStream? _namedPipeClient;
    private StreamWriter? _pipeStreamWriter;

    public ConsoleWindow(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ConsoleWindow>();
    }

    public Task RewriteAsync(string line) => WriteToConsoleAsync(line);

    public Task WriteLineAsync(string line) => WriteToConsoleAsync($"{line}[nl]");

    private Task WriteToConsoleAsync(string line)
    {
        return _pipeStreamWriter?.WriteLineAsync(line) ?? throw new InvalidOperationException("Console application not connected.");
    }

    public async Task ShowAsync(CancellationToken cancellationToken)
    {
        StartConsoleProcess();
        await ConnectToConsoleAsync(cancellationToken);
        _logger.LogDebug("Console windows opened.");
    }


    private void StartConsoleProcess()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c start owl_display.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            if (Process.Start(psi) == null) throw new SystemException("Failed to start process.");

            _logger.LogDebug("'owl_display' process started.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting 'owl_display' process.");
            throw;
        }
    }

    private async Task ConnectToConsoleAsync(CancellationToken cancellationToken)
    {
        _namedPipeClient = new NamedPipeClientStream(".", "OwlPipe", PipeDirection.Out);
        await _namedPipeClient.ConnectAsync(cancellationToken);
        _pipeStreamWriter = new StreamWriter(_namedPipeClient) { AutoFlush = true };
    }

    private void StopConsoleProcess()
    {
        try
        {
            foreach (var process in Process.GetProcessesByName("owl_display"))
            {
                process.Kill();
            }

            _logger.LogDebug("'owl_display' process stopped.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping 'owl_display' process.");
            throw;
        }
    }

    public void Hide()
    {
        _pipeStreamWriter?.Close();
        _namedPipeClient?.Close();
        StopConsoleProcess();
        _logger.LogInformation("Display window closed.");
    }

    private bool _disposed;
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        Hide();
        if (_pipeStreamWriter is not null) await _pipeStreamWriter.DisposeAsync();
        _pipeStreamWriter = null;
        if (_namedPipeClient is not null) await _namedPipeClient.DisposeAsync();
        _namedPipeClient = null;
        _disposed = true;
    }
}