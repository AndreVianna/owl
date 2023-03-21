namespace Owl.CLI;

internal class StartCommand : ChildCommand
{
    public StartCommand(string[] args) : base("start", args)
    {
    }

    protected override bool TryExecute()
    {
        if (IsServiceRunning())
        {
            Console.WriteLine($"The {ServiceName} process is already running.");
            return false;
        }

        Console.WriteLine($"Starting '{ServiceName}' process...");
        using var process = new Process();
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.Arguments = $"/c start {ServiceName}.exe";
        process.StartInfo.WorkingDirectory = ".";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        if (process.Start())
        {
            Console.WriteLine($"'{ServiceName}' started.");
        }

        //var psi = new ProcessStartInfo
        //{
        //    FileName = "cmd.exe",
        //    Arguments = $"/c start {ServiceName}.exe",
        //    UseShellExecute = false,
        //    CreateNoWindow = true,
        //};
        //var process = Process.Start(psi);
        //if (process is not null) 
        //{
        //    Console.WriteLine($"Starting '{ServiceName}' started...");
        //}

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