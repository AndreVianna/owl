namespace Owl.Service;

public sealed class ConnectedConsole : IConnectedConsole
{
    private readonly ILogger<ConnectedConsole> _logger;
    private NamedPipeClientStream? _namedPipeClient;
    private StreamWriter? _pipeStreamWriter;

    public ConnectedConsole(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ConnectedConsole>();
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        StartConsoleProcess();
        await ConnectToConsoleAsync(cancellationToken);
        _logger.LogInformation("Connected.");
    }

    public Task SendLineAsync(string line)
    {
        return _pipeStreamWriter?.WriteLineAsync(line) ?? throw new InvalidOperationException("Console application not connected.");
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

    private bool _disposed;
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        if (_pipeStreamWriter != null)
        {
            _pipeStreamWriter.Close();
            await _pipeStreamWriter.DisposeAsync();
            _pipeStreamWriter = null;
        }

        if (_namedPipeClient != null)
        {
            _namedPipeClient.Close();
            await _namedPipeClient.DisposeAsync();
            _namedPipeClient = null;
        }

        StopConsoleProcess();
        _logger.LogInformation("Disconnected.");
        _disposed = true;
    }
}