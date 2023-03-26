using System.IO.Pipes;

using var namedPipeServer = new NamedPipeServerStream("OwlPipe");
namedPipeServer.WaitForConnection();

using var reader = new StreamReader(namedPipeServer);
var maxLength = 0;

while (reader.ReadLine() is { } line)
{
    if (string.IsNullOrWhiteSpace(line)) continue;

    var isNewLine = line.EndsWith("[nl]");
    line = isNewLine ? line[..^4] : line;
    if (!isNewLine && line.Length < maxLength) continue;

    UpdateConsole(line, isNewLine);
}

void UpdateConsole(string line, bool isNewLine)
{
    maxLength = Math.Max(maxLength, line.Length);

    var text = line.PadRight(maxLength);
    Console.Write($"\r{text}");
    if (!isNewLine) return;

    Console.WriteLine();
    maxLength = 0;
}