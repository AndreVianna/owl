namespace Owl.CLI;

class StopCommand : ChildCommand
{
    public StopCommand(string[] args) : base("stop", args)
    {
    }

    protected override string Usage => """
         Usage: owl stop [options]
 
         Options:
                -h, --help   Show help information for the stop command. 
         """;

    protected override bool TryExecute()
    {
        Console.WriteLine($"Stopping {ServiceName} process...");
        StopService();
        return true;
    }

    private static void StopService()
    {
        try
        {
            var processes = Process.GetProcessesByName(ServiceName);
            foreach (var process in processes)
            {
                process.Kill();
            }

            Console.WriteLine($"'{ServiceName}' process stopped.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping '{ServiceName}' process: {ex.Message}");
        }
    }
}