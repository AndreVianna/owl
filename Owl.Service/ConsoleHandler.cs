using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Owl.Service;

public class ConsoleHandler : IConsoleHandler
{
    private readonly ILogger<ConsoleHandler> _logger;
    private NamedPipeClientStream? _namedPipeClient;
    private StreamWriter? _pipeStreamWriter;

    public ConsoleHandler(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ConsoleHandler>();
    }

    public async Task ConnectAsync()
    {
        StartConsoleProcess();
        await ConnectToConsoleAsync();
        _logger.LogInformation("Connected.");
    }

    public async Task DisconnectAsync()
    {
        await DisconnectFromConsoleAsync();
        StopConsoleProcess();
        _logger.LogInformation("Disconnected.");
    }

    public Task SendLineAsync(string line)
    {
        return _pipeStreamWriter?.WriteLineAsync(line) ?? throw new InvalidOperationException("Console application not connected.");
    }


    private void StartConsoleProcess()
    {
        var hasStarted = ProcessHelper.TryStartProcess("owl_display");
        if (!hasStarted) throw new SystemException("Failed to start 'owl_display' process.");
        _logger.LogInformation("'owl_display' process started.");
    }

    private async Task ConnectToConsoleAsync()
    {
        _namedPipeClient = new NamedPipeClientStream(".", "OwlPipe", PipeDirection.Out);
        await _namedPipeClient.ConnectAsync();
        _pipeStreamWriter = new StreamWriter(_namedPipeClient) { AutoFlush = true };
    }

    private static void StopConsoleProcess()
    {
        var hasStopped = ProcessHelper.TryStopProcess("owl_display");
        if (!hasStopped) throw new SystemException("Failed to stop 'owl_display' process.");
        Console.WriteLine("'owl_display' process stopped.");
    }

    private async Task DisconnectFromConsoleAsync()
    {
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
    }
}