namespace Owl.Service.Output;

public sealed class TimestampedFile : ITimestampedFile
{
    private readonly string _fileNamePrefix;
    private readonly ILogger<TimestampedFile> _logger;
    private readonly StringBuilder _recognizedText;

    public TimestampedFile(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _fileNamePrefix = configuration["SaveFileNamePrefix"]!;
        _recognizedText = new StringBuilder();
        _logger = loggerFactory.CreateLogger<TimestampedFile>();
    }

    public void Open()
    {
        _recognizedText.Clear();
    }

    public void AppendLine(string text)
    {
        _recognizedText.AppendLine(text);
    }

    public async Task SaveAsync(CancellationToken cancellationToken)
    {
        var fileName = $"{_fileNamePrefix}_{DateTimeOffset.Now:yyyyMMdd_HHmmss}.txt";
        await File.WriteAllTextAsync(fileName, _recognizedText.ToString(), cancellationToken);
        _logger.LogInformation("text saved to file: {file}", fileName);
    }
}