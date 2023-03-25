namespace Owl.Service;

public interface ITimestampedFile
{
    void Open();
    void AppendLine(string text);
    Task SaveAsync();
}