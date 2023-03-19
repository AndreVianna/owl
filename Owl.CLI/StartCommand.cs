namespace Owl.CLI;

class StartCommand : ChildCommand
{
    public StartCommand(string[] args) : base("start", args)
    {
    }

    protected override bool TryExecute()
    {
        if (IsServiceRunning())
        {
            Console.WriteLine("The owl_service process is already running.");
            return false;
        }

        Console.WriteLine($"Starting '{ServiceName}' process...");
        var process = new Process();
        process.StartInfo.FileName = $"{ServiceName}.exe";
        process.StartInfo.Arguments = "";
        process.StartInfo.WorkingDirectory = ".";
        process.Start();

        return true;
    }

    protected override string Usage => """
         Usage: owl start [options]
         Options:
                -h, --help   Show help information for the start command. 
         """;

    private static bool IsServiceRunning()
    {
        try
        {
            var processes = Process.GetProcessesByName(ServiceName);
            return processes.Length > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking if service '{ServiceName}' is running: {ex.Message}");
            return false;
        }
    }
}