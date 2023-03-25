namespace Owl.Service;

public interface IConsoleWindow
{
    Task ShowAsync();
    Task HideAsync();
    Task RewriteSameLine(string line);
    Task WriteLineAsync(string line);
}
