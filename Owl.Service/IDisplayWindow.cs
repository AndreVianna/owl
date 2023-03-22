namespace Owl.Service;

public interface IDisplayWindow
{
    Task ShowAsync();
    Task HideAsync();
    Task RewriteSameLine(string line);
    Task WriteLineAsync(string line);
}
