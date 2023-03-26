namespace Owl.Service.Output;

public interface ITimestampedFile
{
    void Open();
    void AppendLine(string text);
    Task SaveAsync(CancellationToken cancellationToken);
}