using System.IO.Pipes;

using var namedPipeServer = new NamedPipeServerStream("OwlPipe");
namedPipeServer.WaitForConnection();

using var reader = new StreamReader(namedPipeServer);
var previousText = string.Empty;
var padSize = 0;
while (reader.ReadLine() is { } line)
{
    if (string.IsNullOrWhiteSpace(line)) continue;
    var isFinal = line.StartsWith("[F]");
    line = line.Replace("[F]", string.Empty);
    if (isFinal)
    {
        previousText = string.Empty;
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.WriteLine(line);
    }
    else
    {
        if (line == previousText) continue;
        previousText = line;
        padSize = Math.Max(previousText.Length, line.Length);
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(line.PadRight(padSize, ' '));
    }
}