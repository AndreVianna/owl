using System.IO.Pipes;

using var namedPipeServer = new NamedPipeServerStream("OwlPipe");
namedPipeServer.WaitForConnection();

using var reader = new StreamReader(namedPipeServer);
var previousText = string.Empty;
while (reader.ReadLine() is { } line)
{
    if (string.IsNullOrWhiteSpace(line)) continue;

    var isFinal = line.StartsWith("[F]");
    line = isFinal ? line[3..] : line;
    if (!isFinal && line.Length < previousText.Length) continue;

    previousText = UpdateConsole(line, previousText, isFinal);
}

static string UpdateConsole(string line, string previousText1, bool isFinal)
{
    var text = line.PadRight(Math.Max(previousText1.Length, line.Length));
    Console.SetCursorPosition(0, Console.CursorTop);
    if (!isFinal)
    {
        Console.Write(text);
        return line;
    }

    Console.WriteLine(text);
    return string.Empty;
}