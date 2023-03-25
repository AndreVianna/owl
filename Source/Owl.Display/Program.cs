using System.IO.Pipes;

using var namedPipeServer = new NamedPipeServerStream("OwlPipe");
namedPipeServer.WaitForConnection();

using var reader = new StreamReader(namedPipeServer);
var previousText = string.Empty;
while (reader.ReadLine() is { } line)
{
    if (string.IsNullOrWhiteSpace(line)) continue;
    var isFinal = line.StartsWith("[F]");
    line = line.Replace("[F]", string.Empty);
    Console.SetCursorPosition(0, Console.CursorTop);
    if (!isFinal && line == previousText) continue;
    previousText = line;
    var padSize = Math.Max(previousText.Length, line.Length);
    Console.Write(line.PadRight(padSize, ' '));
    if (isFinal)
    {
        Console.WriteLine();
        previousText = string.Empty;
    }
}