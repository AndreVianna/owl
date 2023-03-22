using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Owl.Service;

public sealed class TimestampedFileHandler : ITimestampedFileHandler
{
    private readonly string _folderPath;
    private readonly ILogger<TimestampedFileHandler> _logger;
    private StreamWriter? _fileStream;

    public TimestampedFileHandler(string folderPath, ILoggerFactory loggerFactory)
    {
        _folderPath = folderPath;
        Directory.CreateDirectory(_folderPath);
        _logger = loggerFactory.CreateLogger<TimestampedFileHandler>();
    }

    public async Task CreateAsync()
    {
        await DisposeAsync();
        var fileName = $"{_folderPath}/{DateTimeOffset.Now:yyyyMMdd_HHmmss}.txt";
        _fileStream = File.CreateText(fileName);
        _logger.LogInformation("'{file}' created.", fileName);
    }

    public async Task SaveLineAsync(string line)
    {
        if (_fileStream == null) return;
        await _fileStream.WriteLineAsync(line).ConfigureAwait(false);
    }
    public ValueTask CloseAsync()
    {
        return DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_fileStream == null) return;
        _fileStream.Close();
        await _fileStream.DisposeAsync().ConfigureAwait(false);
        _fileStream = null;
    }
}
