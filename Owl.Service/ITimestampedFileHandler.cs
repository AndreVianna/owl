namespace Owl.Service;

public interface ITimestampedFileHandler : IAsyncDisposable
{
    Task CreateAsync();
    ValueTask CloseAsync();
    Task SaveLineAsync(string line);
}
